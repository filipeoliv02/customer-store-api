using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using CustomerStoreApi.Controllers;
using CustomerStoreApi.Managers;
using CustomerStoreApi.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomerStoreApi.Tests.Controllers;

/// <summary>
/// Provides integration tests for the <see cref="CustomersController"/> type.
/// The database is reset before each test, so tests are independent of each other.
/// </summary>
public sealed class CustomersControllerTests : IClassFixture<CustomerStoreApiFactory>, IAsyncLifetime, IDisposable
{
    // Ignore Spelling: api

    private const string CustomersPath = "/api/customers";

    private readonly CustomerStoreApiFactory factory;
    private readonly HttpClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public CustomersControllerTests(CustomerStoreApiFactory factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.client = factory.CreateClient();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await this.factory.ResetAsync();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public void Dispose() => this.client.Dispose();

    /// <summary>
    /// Ensures that creating a customer requires name and email.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateRequiresNameAndEmailAsync()
    {
        HttpResponseMessage response = await this.PostCustomerAsync(new CreateCustomerRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails? error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        error!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["Name"] = ["The Name field is required."],
            ["Email"] = ["The Email field is required."],
        });
    }

    /// <summary>
    /// Ensures that creating a customer requires a valid email address.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateRequiresValidEmailAsync()
    {
        HttpResponseMessage response = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "A customer", Email = "An invalid email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails? error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        error!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["Email"] = ["The Email field is not a valid e-mail address."],
        });
    }

    /// <summary>
    /// Ensures that creating a customer requires a valid VAT number.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateRequiresValidVatNumberAsync()
    {
        HttpResponseMessage response = await this.PostCustomerAsync(
            new CreateCustomerRequest { Name = "A customer", Email = "customer@server.pt", VatNumber = "1234567890" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails? error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        error!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["VatNumber"] = ["The field VAT Number must match the regular expression '^[0-9]{9}$'."],
        });
    }

    /// <summary>
    /// Ensures that creating a customer rejects values longer than the storage limit.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateRejectsTooLongNameAsync()
    {
        HttpResponseMessage response = await this.PostCustomerAsync(
            new CreateCustomerRequest { Name = new string('a', CreateCustomerRequest.MaxFieldLength + 1), Email = "customer@server.pt" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemDetails? error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        error!.Errors.Should().ContainKey("Name");
    }

    /// <summary>
    /// Ensures that creating a customer requires a unique email address (case-insensitive).
    /// </summary>
    /// <param name="secondEmail">The email of the second customer.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData("customer@server.pt")]
    [InlineData("Customer@Server.PT")]
    public async Task CreateRequiresUniqueEmailAsync(string secondEmail)
    {
        HttpResponseMessage first = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "A customer", Email = "customer@server.pt" });
        HttpResponseMessage second = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "Another customer", Email = secondEmail });

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? error = await second.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        error!.Title.Should().Be(ErrorCodes.CustomerAlreadyExists);
    }

    /// <summary>
    /// Ensures that creating a customer returns its identifier and a valid location header.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateSucceedsAsync()
    {
        HttpResponseMessage response = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "My customer", Email = "mycustomer@server.pt" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = await response.Content.ReadFromJsonAsync<Guid>(CancellationToken);
        id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"{CustomersPath}/{id}");

        HttpResponseMessage get = await this.client.GetAsync(response.Headers.Location, CancellationToken);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Ensures that deleting a customer removes it.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteSucceedsAsync()
    {
        Guid id = await this.CreateCustomerAsync("My customer", "email@example.com");

        HttpResponseMessage response = await this.client.DeleteAsync(new Uri($"{CustomersPath}/{id}", UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage get = await this.GetAsync($"{CustomersPath}/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Ensures that deleting an inexistent customer returns not found.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteNotFoundAsync()
    {
        HttpResponseMessage response = await this.client.DeleteAsync(new Uri($"{CustomersPath}/{Guid.NewGuid()}", UriKind.Relative), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ProblemDetails? error = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        error!.Title.Should().Be(ErrorCodes.CustomerDoesNotExist);
    }

    /// <summary>
    /// Ensures that getting a customer returns all the information available.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetSucceedsAsync()
    {
        HttpResponseMessage created = await this.PostCustomerAsync(new CreateCustomerRequest
        {
            Name = "My customer",
            Email = "mail@example.com",
            Address = "Rua do Ouro, Porto, Portugal",
            VatNumber = "123456789",
        });
        Guid id = await created.Content.ReadFromJsonAsync<Guid>(CancellationToken);

        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomerDetailsDto? customer = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(CancellationToken);
        customer.Should().Be(new CustomerDetailsDto(id, "My customer", "mail@example.com", "Rua do Ouro, Porto, Portugal", "123456789"));
    }

    /// <summary>
    /// Ensures that getting an inexistent or malformed customer identifier returns not found.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("not-a-guid")]
    public async Task GetNotFoundAsync(string id)
    {
        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Ensures that the geolocation of a customer's address is returned.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetGeolocationSucceedsAsync()
    {
        const string Address = "Rua do Ouro, Porto, Portugal";
        this.factory.GeoLocationService.Response = new GeolocationResponse
        {
            Data = [new GeolocationDto { Address = Address, Latitude = 41.14, Longitude = -8.64, Country = "Portugal" }],
        };
        HttpResponseMessage created = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "My customer", Email = "123@example.com", Address = Address });
        Guid id = await created.Content.ReadFromJsonAsync<Guid>(CancellationToken);

        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{id}/geolocation");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GeolocationResponse? geolocation = await response.Content.ReadFromJsonAsync<GeolocationResponse>(CancellationToken);
        geolocation!.Data.Should().ContainSingle().Which.Should().BeEquivalentTo(this.factory.GeoLocationService.Response.Data[0]);
        this.factory.GeoLocationService.RequestedAddresses.Should().Equal(Address);
    }

    /// <summary>
    /// Ensures that the geolocation endpoint returns not found for an inexistent customer.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetGeolocationNotFoundAsync()
    {
        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{Guid.NewGuid()}/geolocation");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ProblemDetails? error = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        error!.Title.Should().Be(ErrorCodes.CustomerDoesNotExist);
    }

    /// <summary>
    /// Ensures that the geolocation endpoint returns not found when the customer has no address.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetGeolocationNoAddressAsync()
    {
        Guid id = await this.CreateCustomerAsync("My customer", "123@test.com");

        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{id}/geolocation");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ProblemDetails? error = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        error!.Title.Should().Be(ErrorCodes.CustomerDoesNotHaveAnAddress);
        this.factory.GeoLocationService.RequestedAddresses.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures that the geolocation endpoint returns bad gateway when the geolocation provider fails.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetGeolocationProviderFailureAsync()
    {
        HttpResponseMessage created = await this.PostCustomerAsync(new CreateCustomerRequest { Name = "My customer", Email = "geo@test.com", Address = "Nowhere" });
        Guid id = await created.Content.ReadFromJsonAsync<Guid>(CancellationToken);

        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}/{id}/geolocation");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        ProblemDetails? error = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        error!.Title.Should().Be(ErrorCodes.CouldNotGetGeolocation);
    }

    /// <summary>
    /// Ensures that all customers are listed.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ListSucceedsAsync()
    {
        await this.Create2ValidCustomersAsync();

        IReadOnlyList<CustomerDto> customers = await this.ListAsync(string.Empty);

        customers.Should().HaveCount(2);
        customers.Should().OnlyContain(c => c.Id != Guid.Empty && c.Name.Length > 0 && c.Email.Length > 0);
    }

    /// <summary>
    /// Ensures that an empty list is returned when there are no customers.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ListEmptyAsync()
    {
        IReadOnlyList<CustomerDto> customers = await this.ListAsync(string.Empty);

        customers.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures that customers can be filtered by name and/or email (case-insensitive).
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="expectedCount">The expected number of customers.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData("?name=Test Customer 1", 1)]
    [InlineData("?name=test customer 1", 1)]
    [InlineData("?email=mail1@example.com", 1)]
    [InlineData("?email=MAIL2@example.com", 1)]
    [InlineData("?name=Test Customer 1&email=mail1@example.com", 1)]
    [InlineData("?name=Test Customer 1&email=mail2@example.com", 0)]
    [InlineData("?name=Unknown", 0)]
    public async Task ListFiltersAsync(string query, int expectedCount)
    {
        await this.Create2ValidCustomersAsync();

        IReadOnlyList<CustomerDto> customers = await this.ListAsync(query);

        customers.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Ensures that the health check endpoint reports a healthy application.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task HealthCheckIsHealthyAsync()
    {
        HttpResponseMessage response = await this.GetAsync(ApplicationSetup.HealthCheckPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().Be("Healthy");
    }

    private Task<HttpResponseMessage> GetAsync(string path) =>
        this.client.GetAsync(new Uri(path, UriKind.Relative), CancellationToken);

    private Task<HttpResponseMessage> PostCustomerAsync(CreateCustomerRequest customer) =>
        this.client.PostAsJsonAsync(new Uri(CustomersPath, UriKind.Relative), customer, CancellationToken);

    private async Task<IReadOnlyList<CustomerDto>> ListAsync(string query)
    {
        HttpResponseMessage response = await this.GetAsync($"{CustomersPath}{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerDto>? customers = await response.Content.ReadFromJsonAsync<List<CustomerDto>>(CancellationToken);
        return customers!;
    }

    private async Task<Guid> CreateCustomerAsync(string name, string email)
    {
        HttpResponseMessage response = await this.PostCustomerAsync(new CreateCustomerRequest { Name = name, Email = email });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>(CancellationToken);
    }

    private async Task Create2ValidCustomersAsync()
    {
        await this.CreateCustomerAsync("Test Customer 1", "mail1@example.com");
        await this.CreateCustomerAsync("Test Customer 2", "mail2@example.com");
    }
}

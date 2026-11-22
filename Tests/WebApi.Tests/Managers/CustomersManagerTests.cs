using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using CustomerStoreApi.Managers;
using CustomerStoreApi.Models;
using CustomerStoreApi.Storage;
using CustomerStoreApi.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerStoreApi.Tests.Managers;

/// <summary>
/// Provides unit tests for the <see cref="CustomersManager"/> type.
/// </summary>
public sealed class CustomersManagerTests : IDisposable
{
    private readonly ApplicationDbContext context;
    private readonly FakeGeoLocationService geoLocationService = new();
    private readonly CustomersManager manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersManagerTests"/> class.
    /// </summary>
    public CustomersManagerTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CustomersManagerTests-{Guid.NewGuid()}")
            .Options;

        this.context = new ApplicationDbContext(options);
        this.manager = new CustomersManager(this.context, this.geoLocationService, NullLogger<CustomersManager>.Instance);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public void Dispose() => this.context.Dispose();

    /// <summary>
    /// Ensures that a customer is created with trimmed values.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateStoresCustomerAsync()
    {
        Result<Guid> result = await this.manager.CreateCustomerAsync(
            new CreateCustomerRequest { Name = "  Jane  ", Email = " jane@example.com ", Address = "   ", VatNumber = "123456789" },
            CancellationToken);

        result.Failed.Should().BeFalse();
        Entities.Customer? stored = await this.context.Customers.FindAsync([result.Value], CancellationToken);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Jane");
        stored.Email.Should().Be("jane@example.com");
        stored.Address.Should().BeNull();
        stored.VatNumber.Should().Be("123456789");
    }

    /// <summary>
    /// Ensures that duplicate emails are rejected regardless of casing.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CreateRejectsDuplicateEmailIgnoringCaseAsync()
    {
        await this.CreateAsync("Jane", "jane@example.com");

        Result<Guid> result = await this.manager.CreateCustomerAsync(
            new CreateCustomerRequest { Name = "Other", Email = "JANE@EXAMPLE.COM" },
            CancellationToken);

        result.Failed.Should().BeTrue();
        result.ErrorCode.Should().Be(ErrorCodes.CustomerAlreadyExists);
    }

    /// <summary>
    /// Ensures that a null customer is rejected.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public Task CreateThrowsForNullCustomerAsync()
    {
        Func<Task<Result<Guid>>> act = () => this.manager.CreateCustomerAsync(null!, CancellationToken);

        return act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Ensures that a customer without name or email is rejected even if model validation was bypassed.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="email">The email.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(null, "jane@example.com")]
    [InlineData("Jane", " ")]
    public Task CreateThrowsForMissingRequiredFieldsAsync(string? name, string? email)
    {
        Func<Task<Result<Guid>>> act = () => this.manager.CreateCustomerAsync(new CreateCustomerRequest { Name = name, Email = email }, CancellationToken);

        return act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Ensures that deleting removes the customer and that a second deletion fails.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteRemovesCustomerAsync()
    {
        Guid id = await this.CreateAsync("Jane", "jane@example.com");

        Result<bool> first = await this.manager.DeleteCustomerAsync(id, CancellationToken);
        Result<bool> second = await this.manager.DeleteCustomerAsync(id, CancellationToken);

        first.Failed.Should().BeFalse();
        second.FailedWith(ErrorCodes.CustomerDoesNotExist).Should().BeTrue();
        (await this.context.Customers.CountAsync(CancellationToken)).Should().Be(0);
    }

    /// <summary>
    /// Ensures that getting a customer returns all its details.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetReturnsDetailsAsync()
    {
        Result<Guid> created = await this.manager.CreateCustomerAsync(
            new CreateCustomerRequest { Name = "Jane", Email = "jane@example.com", Address = "Porto", VatNumber = "123456789" },
            CancellationToken);

        Result<CustomerDetailsDto> result = await this.manager.GetCustomerAsync(created.Value, CancellationToken);

        result.Failed.Should().BeFalse();
        result.Value.Should().Be(new CustomerDetailsDto(created.Value, "Jane", "jane@example.com", "Porto", "123456789"));
    }

    /// <summary>
    /// Ensures that getting an inexistent customer fails.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetFailsForUnknownCustomerAsync()
    {
        Result<CustomerDetailsDto> result = await this.manager.GetCustomerAsync(Guid.NewGuid(), CancellationToken);

        result.FailedWith(ErrorCodes.CustomerDoesNotExist).Should().BeTrue();
    }

    /// <summary>
    /// Ensures that the geolocation is not requested for an inexistent customer.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GeolocationFailsForUnknownCustomerAsync()
    {
        Result<GeolocationResponse> result = await this.manager.GetCustomerGeolocationAsync(Guid.NewGuid(), CancellationToken);

        result.FailedWith(ErrorCodes.CustomerDoesNotExist).Should().BeTrue();
        this.geoLocationService.RequestedAddresses.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures that the geolocation is not requested when the customer has no address.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GeolocationFailsWithoutAddressAsync()
    {
        Guid id = await this.CreateAsync("Jane", "jane@example.com");

        Result<GeolocationResponse> result = await this.manager.GetCustomerGeolocationAsync(id, CancellationToken);

        result.FailedWith(ErrorCodes.CustomerDoesNotHaveAnAddress).Should().BeTrue();
        this.geoLocationService.RequestedAddresses.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures that a provider failure is reported.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GeolocationFailsWhenProviderReturnsNothingAsync()
    {
        Guid id = await this.CreateAsync("Jane", "jane@example.com", "Porto");

        Result<GeolocationResponse> result = await this.manager.GetCustomerGeolocationAsync(id, CancellationToken);

        result.FailedWith(ErrorCodes.CouldNotGetGeolocation).Should().BeTrue();
        this.geoLocationService.RequestedAddresses.Should().Equal("Porto");
    }

    /// <summary>
    /// Ensures that the provider response is returned.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GeolocationReturnsProviderResponseAsync()
    {
        Guid id = await this.CreateAsync("Jane", "jane@example.com", "Porto");
        GeolocationResponse expected = new() { Data = [new GeolocationDto { Address = "Porto", Latitude = 41.15, Longitude = -8.61 }] };
        this.geoLocationService.Response = expected;

        Result<GeolocationResponse> result = await this.manager.GetCustomerGeolocationAsync(id, CancellationToken);

        result.Failed.Should().BeFalse();
        result.Value.Should().BeSameAs(expected);
    }

    /// <summary>
    /// Ensures that customers are listed ordered by name and can be filtered.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ListOrdersAndFiltersAsync()
    {
        await this.CreateAsync("Zoe", "zoe@example.com");
        await this.CreateAsync("Adam", "adam@example.com");

        Result<IReadOnlyList<CustomerDto>> all = await this.manager.ListCustomersAsync(null, null, CancellationToken);
        Result<IReadOnlyList<CustomerDto>> byName = await this.manager.ListCustomersAsync(" zoe ", null, CancellationToken);
        Result<IReadOnlyList<CustomerDto>> byEmail = await this.manager.ListCustomersAsync(null, "ADAM@example.com", CancellationToken);

        all.Value!.Select(c => c.Name).Should().Equal("Adam", "Zoe");
        byName.Value.Should().ContainSingle().Which.Email.Should().Be("zoe@example.com");
        byEmail.Value.Should().ContainSingle().Which.Name.Should().Be("Adam");
    }

    private async Task<Guid> CreateAsync(string name, string email, string? address = null)
    {
        Result<Guid> result = await this.manager.CreateCustomerAsync(
            new CreateCustomerRequest { Name = name, Email = email, Address = address },
            CancellationToken);

        result.Failed.Should().BeFalse();
        return result.Value;
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RocketStoreApi.Controllers;
using RocketStoreApi.Managers;
using RocketStoreApi.Models;
using Xunit;

namespace RocketStoreApi.Tests
{
    /// <summary>
    /// Provides integration tests for the <see cref="CustomersController"/> type.
    /// </summary>
    public partial class CustomersControllerTests : TestsBase, IClassFixture<CustomersFixture>
    {
        // Ignore Spelling: api

        #region Fields

        private readonly CustomersFixture fixture;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomersControllerTests"/> class.
        /// </summary>
        /// <param name="fixture">The fixture.</param>
        public CustomersControllerTests(CustomersFixture fixture)
        {
            this.fixture = fixture;
        }
        #endregion

        #region Test Methods

        /// <summary>
        /// Tests the <see cref="CustomersController.CreateCustomerAsync(Customer)"/> method
        /// to ensure that it requires name and email.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task CreateRequiresNameAndEmailAsync()
        {
            // Arrange

            IDictionary<string, string[]> expectedErrors = new Dictionary<string, string[]>
            {
                { "Name", new string[] { "The Name field is required." } },
                { "Email", new string[] { "The Email field is required." } }
            };

            Customer customer = new Customer();

            // Act

            HttpResponseMessage httpResponse = await this.fixture.PostAsync("api/customers", customer).ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            ValidationProblemDetails error = await this.GetResponseContentAsync<ValidationProblemDetails>(httpResponse).ConfigureAwait(false);
            error.Should().NotBeNull();
            error.Errors.Should().BeEquivalentTo(expectedErrors);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.CreateCustomerAsync(Customer)"/> method
        /// to ensure that it requires a valid email address.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task CreateRequiresValidEmailAsync()
        {
            // Arrange

            IDictionary<string, string[]> expectedErrors = new Dictionary<string, string[]>
            {
                { "Email", new string[] { "The Email field is not a valid e-mail address." } }
            };

            Customer customer = new Customer()
            {
                Name = "A customer",
                Email = "An invalid email"
            };

            // Act

            HttpResponseMessage httpResponse = await this.fixture.PostAsync("api/customers", customer).ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            ValidationProblemDetails error = await this.GetResponseContentAsync<ValidationProblemDetails>(httpResponse).ConfigureAwait(false);
            error.Should().NotBeNull();
            error.Errors.Should().BeEquivalentTo(expectedErrors);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.CreateCustomerAsync(Customer)"/> method
        /// to ensure that it requires a valid VAT number.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task CreateRequiresValidVatNumberAsync()
        {
            // Arrange

            IDictionary<string, string[]> expectedErrors = new Dictionary<string, string[]>
            {
                { "VatNumber", new string[] { "The field VAT Number must match the regular expression '^[0-9]{9}$'." } }
            };

            Customer customer = new Customer()
            {
                Name = "A customer",
                Email = "customer@server.pt",
                VatNumber = "1234567890" // Invalid VAT number
            };

            // Act

            HttpResponseMessage httpResponse = await this.fixture.PostAsync("api/customers", customer).ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            ValidationProblemDetails error = await this.GetResponseContentAsync<ValidationProblemDetails>(httpResponse).ConfigureAwait(false);
            error.Should().NotBeNull();
            error.Errors.Should().BeEquivalentTo(expectedErrors);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.CreateCustomerAsync(Customer)"/> method
        /// to ensure that it requires a unique email address.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task CreateRequiresUniqueEmailAsync()
        {
            // Arrange

            Customer customer1 = new Customer()
            {
                Name = "A customer",
                Email = "customer@server.pt"
            };

            Customer customer2 = new Customer()
            {
                Name = "Another customer",
                Email = "customer@server.pt"
            };

            // Act

            HttpResponseMessage httpResponse1 = await this.fixture.PostAsync("api/customers", customer1).ConfigureAwait(false);

            HttpResponseMessage httpResponse2 = await this.fixture.PostAsync("api/customers", customer2).ConfigureAwait(false);

            // Assert

            httpResponse1.StatusCode.Should().Be(HttpStatusCode.Created);

            httpResponse2.StatusCode.Should().Be(HttpStatusCode.Conflict);

            ProblemDetails error = await this.GetResponseContentAsync<ProblemDetails>(httpResponse2).ConfigureAwait(false);
            error.Should().NotBeNull();
            error.Title.Should().Be(ErrorCodes.CustomerAlreadyExists);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.CreateCustomerAsync(Customer)"/> method
        /// to ensure that it requires a valid VAT number.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task CreateSucceedsAsync()
        {
            // Arrange

            Customer customer = new Customer()
            {
                Name = "My customer",
                Email = "mycustomer@server.pt"
            };

            // Act

            HttpResponseMessage httpResponse = await this.fixture.PostAsync("api/customers", customer).ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            Guid? id = await this.GetResponseContentAsync<Guid?>(httpResponse).ConfigureAwait(false);
            id.Should().NotBeNull();

            httpResponse.Headers.Location.Should().NotBeNull();
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.ListCustomersAsync(string, string)"/> method
        /// to ensure that it lists all customers.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ListSucceedsAsync()
        {
            // Arrange

            await this.Create2ValidCustomersAsync().ConfigureAwait(false);

            // Act

            HttpResponseMessage httpResponse = await this.fixture.GetAsync("api/customers").ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            IEnumerable<CustomerDto> customers = await this.GetResponseContentAsync<IEnumerable<CustomerDto>>(httpResponse).ConfigureAwait(false);
            customers.Should().NotBeNull();
            customers.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.ListCustomersAsync(string, string)"/> method
        /// to ensure that it does not list customers if they do not exist.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ListEmptyAsync()
        {
            // Arrange

            // Act

            HttpResponseMessage httpResponse = await this.fixture.GetAsync("api/customers").ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            IEnumerable<CustomerDto> customers = await this.GetResponseContentAsync<IEnumerable<CustomerDto>>(httpResponse).ConfigureAwait(false);
            customers.Should().NotBeNull();
            customers.Should().HaveCount(0);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.ListCustomersAsync(string, string)"/> method
        /// to ensure that it filters customers by name.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ListFilterNameAsync()
        {
            // Arrange

            await this.Create2ValidCustomersAsync().ConfigureAwait(false);

            // Act

            HttpResponseMessage httpResponse = await this.fixture.GetAsync("api/customers?name=Test Customer 1").ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            IEnumerable<CustomerDto> customers = await this.GetResponseContentAsync<IEnumerable<CustomerDto>>(httpResponse).ConfigureAwait(false);
            customers.Should().NotBeNull();
            customers.Should().HaveCount(1);
        }

        /// <summary>
        /// Tests the <see cref="CustomersController.ListCustomersAsync(string, string)"/> method
        /// to ensure that it filters customers by email.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation.
        /// </returns>
        [Fact]
        public async Task ListFilterEmailAsync()
        {
            // Arrange

            await this.Create2ValidCustomersAsync().ConfigureAwait(false);

            // Act

            HttpResponseMessage httpResponse = await this.fixture.GetAsync("api/customers?email=mail1@example.com").ConfigureAwait(false);

            // Assert

            httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            IEnumerable<CustomerDto> customers = await this.GetResponseContentAsync<IEnumerable<CustomerDto>>(httpResponse).ConfigureAwait(false);
            customers.Should().NotBeNull();
            customers.Should().HaveCount(1);
        }

        #endregion

        #region Private Methods

        private async Task Create2ValidCustomersAsync()
        {
            Customer customer1 = new Customer()
            {
                Name = "Test Customer 1",
                Email = "mail1@example.com",
            };

            Customer customer2 = new Customer()
            {
                Name = "Test Customer 2",
                Email = "mail2@example.com",
            };

            await this.fixture.PostAsync("api/customers", customer1).ConfigureAwait(false);
            await this.fixture.PostAsync("api/customers", customer2).ConfigureAwait(false);
        }

        #endregion
    }
}
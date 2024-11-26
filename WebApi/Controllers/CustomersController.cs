using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CustomerStoreApi.Managers;
using CustomerStoreApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerStoreApi.Controllers
{
    /// <summary>
    /// Defines the customers controller.
    /// This controller provides actions on customers.
    /// </summary>
    [ControllerName("Customers")]
    [ApiController]
    public partial class CustomersController : ControllerBase
    {
        // Ignore Spelling: api

        #region Public Methods

        /// <summary>
        /// Creates the specified customer.
        /// </summary>
        /// <param name="customer">The customer that should be created.</param>
        /// <returns>
        /// The new customer identifier.
        /// </returns>
        [HttpPost("api/customers")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateCustomerAsync(Customer customer)
        {
            Result<Guid> result = await this.HttpContext.RequestServices.GetRequiredService<ICustomersManager>()
                .CreateCustomerAsync(customer).ConfigureAwait(false);

            if (result.FailedWith(ErrorCodes.CustomerAlreadyExists))
            {
                return this.Conflict(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.Conflict,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }
            else if (result.Failed)
            {
                return this.BadRequest(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }

            return this.Created(
                this.GetUri("customers", result.Value),
                result.Value);
        }

        /// <summary>
        /// Deletes the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>
        /// The result of the deletion.
        /// </returns>
        [HttpDelete("api/customers/{customerId}")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteCustomerAsync(string customerId)
        {
            // Result requires a type, but we don't need it here.
            Result<bool> result = await this.HttpContext.RequestServices.GetRequiredService<ICustomersManager>()
                .DeleteCustomerAsync(customerId).ConfigureAwait(false);

            if (result.FailedWith(ErrorCodes.CustomerDoesNotExist))
            {
                return this.NotFound(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.NotFound,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }
            else if (result.Failed)
            {
                return this.BadRequest(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }

            return this.NoContent();
        }

        /// <summary>
        /// Gets the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>
        /// The customer.
        /// </returns>
        [HttpGet("api/customers/{customerId}")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(Customer), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCustomerAsync(string customerId)
        {
            Result<Customer> result = await this.HttpContext.RequestServices.GetRequiredService<ICustomersManager>()
                .GetCustomerAsync(customerId).ConfigureAwait(false);

            if (result.FailedWith(ErrorCodes.CustomerDoesNotExist))
            {
                return this.NotFound(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.NotFound,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }
            else if (result.Failed)
            {
                return this.BadRequest(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }

            return this.Ok(result.Value);
        }

        /// <summary>
        /// Get the geolocation information about an existing customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>
        /// The geolocation information.
        /// </returns>
        [HttpGet("api/customers/{customerId}/geolocation")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(GeolocationData), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetGeolocationAsync(string customerId)
        {
            Result<GeolocationData> result = await this.HttpContext.RequestServices.GetRequiredService<ICustomersManager>()
                .GetCustomerGeolocationAsync(customerId).ConfigureAwait(false);

            if (result.FailedWith(ErrorCodes.CustomerDoesNotExist) || result.FailedWith(ErrorCodes.CustomerDoesNotHaveAnAddress))
            {
                return this.NotFound(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.NotFound,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }
            else if (result.Failed)
            {
                return this.BadRequest(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }

            return this.Ok(result.Value);
        }

        /// <summary>
        /// Lists all customers.
        /// </summary>
        /// <param name="name">The name of the customers.</param>
        /// <param name="email">The email of the customer(since the email is unique we can only get a customer,if there is a match).</param>
        /// <returns>
        /// The list of all customers.
        /// </returns>
        [HttpGet("api/customers")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(CustomerDto[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListCustomersAsync([FromQuery] string name = null, [FromQuery] string email = null)
        {
            Result<List<CustomerDto>> result = await this.HttpContext.RequestServices.GetRequiredService<ICustomersManager>()
                .ListCustomersAsync(name, email).ConfigureAwait(false);

            if (result.Failed)
            {
                return this.BadRequest(
                    new ProblemDetails()
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = result.ErrorCode,
                        Detail = result.ErrorDescription
                    });
            }

            return this.Ok(result.Value);
        }

        #endregion

        #region Private Methods

        private Uri GetUri(params object[] parameters)
        {
            string result = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";

            foreach (object pathParam in parameters)
            {
                result = $"{result}/{pathParam}";
            }

            return new Uri(result);
        }

        #endregion
    }
}

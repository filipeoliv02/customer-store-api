using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RocketStoreApi.Managers;
using RocketStoreApi.Models;

namespace RocketStoreApi.Controllers
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
        /// Lists all customers.
        /// </summary>
        /// <returns>
        /// <param name="name">The name of the customers.</param>
        /// <param name="email">The email of the customer(since the email is unique we can only get a customer,if there is a match).</param>
        /// The list of all customers.
        /// </returns>
        [HttpGet("api/customers")]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Customer[]), (int)HttpStatusCode.OK)]
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

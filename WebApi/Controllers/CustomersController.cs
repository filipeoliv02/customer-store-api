using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Managers;
using CustomerStoreApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CustomerStoreApi.Controllers;

/// <summary>
/// Defines the customers controller.
/// This controller provides actions on customers.
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private const string GetCustomerRouteName = "GetCustomer";

    private readonly ICustomersManager customersManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersController"/> class.
    /// </summary>
    /// <param name="customersManager">The customers manager.</param>
    public CustomersController(ICustomersManager customersManager)
    {
        this.customersManager = customersManager ?? throw new ArgumentNullException(nameof(customersManager));
    }

    /// <summary>
    /// Creates the specified customer.
    /// </summary>
    /// <param name="customer">The customer that should be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new customer identifier.</returns>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateCustomerAsync(CreateCustomerRequest customer, CancellationToken cancellationToken)
    {
        Result<Guid> result = await this.customersManager
            .CreateCustomerAsync(customer, cancellationToken)
            .ConfigureAwait(false);

        if (result.Failed)
        {
            return this.ToProblem(result);
        }

        return this.CreatedAtRoute(GetCustomerRouteName, new { customerId = result.Value }, result.Value);
    }

    /// <summary>
    /// Deletes the specified customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the customer was deleted.</returns>
    [HttpDelete("{customerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        Result<bool> result = await this.customersManager
            .DeleteCustomerAsync(customerId, cancellationToken)
            .ConfigureAwait(false);

        return result.Failed ? this.ToProblem(result) : this.NoContent();
    }

    /// <summary>
    /// Gets the specified customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All the information available for the customer.</returns>
    [HttpGet("{customerId:guid}", Name = GetCustomerRouteName)]
    [ProducesResponseType<CustomerDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailsDto>> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        Result<CustomerDetailsDto> result = await this.customersManager
            .GetCustomerAsync(customerId, cancellationToken)
            .ConfigureAwait(false);

        return result.Failed ? this.ToProblem(result) : result.Value;
    }

    /// <summary>
    /// Gets the geolocation information about the address of an existing customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The geolocation information.</returns>
    [HttpGet("{customerId:guid}/geolocation")]
    [ProducesResponseType<GeolocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GeolocationResponse>> GetGeolocationAsync(Guid customerId, CancellationToken cancellationToken)
    {
        Result<GeolocationResponse> result = await this.customersManager
            .GetCustomerGeolocationAsync(customerId, cancellationToken)
            .ConfigureAwait(false);

        return result.Failed ? this.ToProblem(result) : result.Value;
    }

    /// <summary>
    /// Lists the customers, optionally filtered by name and/or email (case-insensitive exact match).
    /// </summary>
    /// <param name="name">The optional name filter.</param>
    /// <param name="email">The optional email filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of customers.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> ListCustomersAsync(
        [FromQuery] string? name,
        [FromQuery] string? email,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CustomerDto>> result = await this.customersManager
            .ListCustomersAsync(name, email, cancellationToken)
            .ConfigureAwait(false);

        return result.Failed ? this.ToProblem(result) : this.Ok(result.Value);
    }

    private ObjectResult ToProblem<T>(Result<T> result)
    {
        int statusCode = result.ErrorCode switch
        {
            ErrorCodes.CustomerAlreadyExists => StatusCodes.Status409Conflict,
            ErrorCodes.CustomerDoesNotExist => StatusCodes.Status404NotFound,
            ErrorCodes.CustomerDoesNotHaveAnAddress => StatusCodes.Status404NotFound,
            ErrorCodes.CouldNotGetGeolocation => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest,
        };

        return this.Problem(detail: result.ErrorDescription, statusCode: statusCode, title: result.ErrorCode);
    }
}

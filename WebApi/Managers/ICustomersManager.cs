using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;

namespace CustomerStoreApi.Managers;

/// <summary>
/// Defines the interface of the customers manager.
/// The customers manager allows retrieving, creating, and deleting customers.
/// </summary>
public interface ICustomersManager
{
    /// <summary>
    /// Creates the specified customer.
    /// </summary>
    /// <param name="customer">The customer that should be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}" /> with the new customer identifier.</returns>
    Task<Result<Guid>> CreateCustomerAsync(CreateCustomerRequest customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}" /> that describes the result.</returns>
    Task<Result<bool>> DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}" /> with the customer details.</returns>
    Task<Result<CustomerDetailsDto>> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the geolocation of a specific customer's address.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}" /> with the geolocation of the customer.</returns>
    Task<Result<GeolocationResponse>> GetCustomerGeolocationAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the customers, optionally filtered by name and/or email (case-insensitive exact match).
    /// </summary>
    /// <param name="name">The optional name filter.</param>
    /// <param name="email">The optional email filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}" /> with the list of customers.</returns>
    Task<Result<IReadOnlyList<CustomerDto>>> ListCustomersAsync(string? name, string? email, CancellationToken cancellationToken = default);
}

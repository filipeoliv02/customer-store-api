using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;

namespace CustomerStoreApi.Managers
{
    /// <summary>
    /// Defines the interface of the customers manager.
    /// The customers manager allows retrieving, creating, and deleting customers.
    /// </summary>
    public partial interface ICustomersManager
    {
        #region Methods

        /// <summary>
        /// Creates the specified customer.
        /// </summary>
        /// <param name="customer">The customer that should be created.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="Result{T}" /> that describes the result.
        /// The new customer identifier.
        /// </returns>
        Task<Result<Guid>> CreateCustomerAsync(Models.Customer customer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="Result{T}" /> that describes the result.
        /// </returns>
        Task<Result<bool>> DeleteCustomerAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="Result{T}" /> that describes the result.
        /// The customer.
        /// </returns>
        Task<Result<Customer>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the geolocation of a specific customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="Result{T}" /> that describes the result.
        /// The geolocation of the customer.
        /// </returns>
        Task<Result<GeolocationData>> GetCustomerGeolocationAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists the customers.
        /// </summary>
        /// <param name="name">The name of the customers.</param>
        /// <param name="email">The email of the customer(since the email is unique we can only get a customer,if there is a match).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="Result{T}" /> that describes the result.
        /// The list of customers.
        /// </returns>
        Task<Result<List<CustomerDto>>> ListCustomersAsync(string name, string email, CancellationToken cancellationToken = default);
        #endregion
    }
}

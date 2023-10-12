using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RocketStoreApi.Managers
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
        Task<Result<List<Models.CustomerDto>>> ListCustomersAsync(string name, string email, CancellationToken cancellationToken = default);
        #endregion
    }
}

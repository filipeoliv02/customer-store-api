using System;

namespace RocketStoreApi.Models
{
    /// <summary>
    /// Defines a customer dto.
    /// </summary>
    public class CustomerDto
    {
        /// <summary>
        /// Gets or sets the id of the customer.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the customer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the email of the customer.
        /// </summary>
        public string Email { get; set; }
    }
}

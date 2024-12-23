﻿namespace CustomerStoreApi.Entities
{
    /// <summary>
    /// Defines a customer.
    /// </summary>
    public partial class Customer
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer email address.
        /// </summary>
        public string Email
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer address.
        /// </summary>
        public string Address
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer vat number.
        /// </summary>
        public string VatNumber
        {
            get;
            set;
        }

        #endregion
    }
}

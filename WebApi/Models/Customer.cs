﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CustomerStoreApi.Models
{
    /// <summary>
    /// Defines a customer.
    /// </summary>
    public partial class Customer
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [DisplayName("Name")]
        [JsonPropertyName("name")]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer email address.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [EmailAddress]
        [DisplayName("Email")]
        [JsonPropertyName("emailAddress")]
        public string Email
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer address.
        /// </summary>
        [DisplayName("Address")]
        [JsonPropertyName("address")]
        public string Address
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the customer vat number.
        /// </summary>
        [RegularExpression("^[0-9]{9}$")]
        [DisplayName("VAT Number")]
        [JsonPropertyName("vatNumber")]
        public string VatNumber
        {
            get;
            set;
        }

        #endregion
    }
}

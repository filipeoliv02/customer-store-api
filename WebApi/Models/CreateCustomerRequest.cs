using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CustomerStoreApi.Models;

/// <summary>
/// Defines the payload used to create a customer.
/// </summary>
public sealed class CreateCustomerRequest
{
    /// <summary>
    /// The maximum length of the name, email and address fields.
    /// </summary>
    public const int MaxFieldLength = 200;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(MaxFieldLength)]
    [DisplayName("Name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(MaxFieldLength)]
    [DisplayName("Email")]
    [JsonPropertyName("emailAddress")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer address.
    /// </summary>
    [StringLength(MaxFieldLength)]
    [DisplayName("Address")]
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the customer VAT number (9 digits).
    /// </summary>
    [RegularExpression("^[0-9]{9}$")]
    [DisplayName("VAT Number")]
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }
}

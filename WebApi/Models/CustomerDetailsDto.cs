using System;
using System.Text.Json.Serialization;

namespace CustomerStoreApi.Models;

/// <summary>
/// Defines all the information available for a customer.
/// </summary>
/// <param name="Id">The customer identifier.</param>
/// <param name="Name">The customer name.</param>
/// <param name="Email">The customer email address.</param>
/// <param name="Address">The customer address.</param>
/// <param name="VatNumber">The customer VAT number.</param>
public sealed record CustomerDetailsDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("emailAddress")] string Email,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("vatNumber")] string? VatNumber);

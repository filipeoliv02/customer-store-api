using System;

namespace CustomerStoreApi.Entities;

/// <summary>
/// Defines a customer, as persisted in the database.
/// </summary>
public sealed class Customer
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the customer address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the customer VAT number.
    /// </summary>
    public string? VatNumber { get; set; }
}

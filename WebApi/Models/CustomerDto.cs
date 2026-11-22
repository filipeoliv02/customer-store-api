using System;

namespace CustomerStoreApi.Models;

/// <summary>
/// Defines the summary of a customer, as returned when listing customers.
/// </summary>
/// <param name="Id">The customer identifier.</param>
/// <param name="Name">The customer name.</param>
/// <param name="Email">The customer email address.</param>
public sealed record CustomerDto(Guid Id, string Name, string Email);

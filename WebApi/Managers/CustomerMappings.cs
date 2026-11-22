using System;
using CustomerStoreApi.Models;

namespace CustomerStoreApi.Managers;

/// <summary>
/// Provides mappings between customer entities and API models.
/// </summary>
internal static class CustomerMappings
{
    /// <summary>
    /// Maps a creation request to a new entity.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <returns>The new entity, with a newly generated identifier.</returns>
    /// <exception cref="ArgumentException">The name or email is missing.</exception>
    public static Entities.Customer ToEntity(this CreateCustomerRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email, nameof(request));

        return new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            VatNumber = request.VatNumber,
        };
    }

    /// <summary>
    /// Maps an entity to its summary representation.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The summary representation.</returns>
    public static CustomerDto ToDto(this Entities.Customer entity) =>
        new(entity.Id, entity.Name, entity.Email);

    /// <summary>
    /// Maps an entity to its detailed representation.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The detailed representation.</returns>
    public static CustomerDetailsDto ToDetailsDto(this Entities.Customer entity) =>
        new(entity.Id, entity.Name, entity.Email, entity.Address, entity.VatNumber);
}

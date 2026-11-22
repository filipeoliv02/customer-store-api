using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;
using CustomerStoreApi.Services;
using CustomerStoreApi.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerStoreApi.Managers;

/// <summary>
/// Defines the default implementation of <see cref="ICustomersManager"/>.
/// </summary>
/// <remarks>
/// Log messages intentionally only contain customer identifiers (never names, emails or addresses)
/// to avoid writing personal data to the logs.
/// </remarks>
internal sealed partial class CustomersManager : ICustomersManager
{
    private readonly ApplicationDbContext context;
    private readonly IGeoLocationService geoLocationService;
    private readonly ILogger<CustomersManager> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersManager" /> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="geoLocationService">The geolocation service.</param>
    /// <param name="logger">The logger.</param>
    public CustomersManager(ApplicationDbContext context, IGeoLocationService geoLocationService, ILogger<CustomersManager> logger)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.geoLocationService = geoLocationService ?? throw new ArgumentNullException(nameof(geoLocationService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateCustomerAsync(CreateCustomerRequest customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        Entities.Customer entity = customer.ToEntity();
        string normalizedEmail = entity.Email.ToUpperInvariant();

        bool emailInUse = await this.context.Customers
            .AnyAsync(c => c.Email.ToUpperInvariant() == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (emailInUse)
        {
            this.LogCustomerAlreadyExists();

            return Result<Guid>.Failure(
                ErrorCodes.CustomerAlreadyExists,
                $"A customer with email '{entity.Email}' already exists.");
        }

        this.context.Customers.Add(entity);

        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogCustomerCreated(entity.Id);

        return Result<Guid>.Success(entity.Id);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Entities.Customer? entity = await this.context.Customers
            .FindAsync([customerId], cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return this.NotFound<bool>(customerId);
        }

        this.context.Customers.Remove(entity);

        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.LogCustomerDeleted(customerId);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task<Result<CustomerDetailsDto>> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Entities.Customer? entity = await this.FindCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);

        return entity is null
            ? this.NotFound<CustomerDetailsDto>(customerId)
            : Result<CustomerDetailsDto>.Success(entity.ToDetailsDto());
    }

    /// <inheritdoc />
    public async Task<Result<GeolocationResponse>> GetCustomerGeolocationAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Entities.Customer? entity = await this.FindCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            return this.NotFound<GeolocationResponse>(customerId);
        }

        if (string.IsNullOrWhiteSpace(entity.Address))
        {
            this.LogCustomerHasNoAddress(customerId);

            return Result<GeolocationResponse>.Failure(
                ErrorCodes.CustomerDoesNotHaveAnAddress,
                $"Customer with id '{customerId}' does not have an address.");
        }

        GeolocationResponse? geolocation = await this.geoLocationService
            .GetGeolocationAsync(entity.Address, cancellationToken)
            .ConfigureAwait(false);

        if (geolocation is null)
        {
            this.LogGeolocationUnavailable(customerId);

            return Result<GeolocationResponse>.Failure(
                ErrorCodes.CouldNotGetGeolocation,
                $"Could not get geolocation data for the address of customer '{customerId}'.");
        }

        return Result<GeolocationResponse>.Success(geolocation);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerDto>>> ListCustomersAsync(string? name, string? email, CancellationToken cancellationToken = default)
    {
        IQueryable<Entities.Customer> query = this.context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(email))
        {
            string normalizedEmail = email.Trim().ToUpperInvariant();
            query = query.Where(c => c.Email.ToUpperInvariant() == normalizedEmail);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = name.Trim().ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpperInvariant() == normalizedName);
        }

        List<CustomerDto> customers = await query
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<CustomerDto>>.Success(customers);
    }

    private Task<Entities.Customer?> FindCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        this.context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

    private Result<T> NotFound<T>(Guid customerId)
    {
        this.LogCustomerNotFound(customerId);

        return Result<T>.Failure(
            ErrorCodes.CustomerDoesNotExist,
            $"Customer with id '{customerId}' not found.");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Customer {CustomerId} created.")]
    private partial void LogCustomerCreated(Guid customerId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Customer {CustomerId} deleted.")]
    private partial void LogCustomerDeleted(Guid customerId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "A customer with the same email address already exists.")]
    private partial void LogCustomerAlreadyExists();

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Customer {CustomerId} not found.")]
    private partial void LogCustomerNotFound(Guid customerId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Customer {CustomerId} does not have an address.")]
    private partial void LogCustomerHasNoAddress(Guid customerId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Could not get geolocation data for customer {CustomerId}.")]
    private partial void LogGeolocationUnavailable(Guid customerId);
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RocketStoreApi.Models;
using RocketStoreApi.Services;
using RocketStoreApi.Storage;

namespace RocketStoreApi.Managers
{
    /// <summary>
    /// Defines the default implementation of <see cref="ICustomersManager"/>.
    /// </summary>
    /// <seealso cref="ICustomersManager" />
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Created via dependency injection.")]
    internal partial class CustomersManager : ICustomersManager
    {
        #region Private Properties

        private ApplicationDbContext Context
        {
            get;
        }

        private IMapper Mapper
        {
            get;
        }

        private ILogger Logger
        {
            get;
        }

        private readonly IGeoLocationService geoLocationService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomersManager" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="geoLocationService">The geo location service.</param>
        public CustomersManager(ApplicationDbContext context, IMapper mapper, ILogger<CustomersManager> logger, IGeoLocationService geoLocationService)
        {
            this.Context = context;
            this.Mapper = mapper;
            this.Logger = logger;
            this.geoLocationService = geoLocationService;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async Task<Result<Guid>> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            customer = customer ?? throw new ArgumentNullException(nameof(customer));

            Entities.Customer entity = this.Mapper.Map<Customer, Entities.Customer>(customer);

            if (this.Context.Customers.Any(i => i.Email == entity.Email))
            {
                this.Logger.LogWarning($"A customer with email '{entity.Email}' already exists.");

                return Result<Guid>.Failure(
                    ErrorCodes.CustomerAlreadyExists,
                    $"A customer with email '{entity.Email}' already exists.");
            }

            this.Context.Customers.Add(entity);

            await this.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this.Logger.LogInformation($"Customer '{customer.Name}' created successfully.");

            return Result<Guid>.Success(
                new Guid(entity.Id));
        }

        /// <inheritdoc />
        public async Task<Result<bool>> DeleteCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            Entities.Customer entity = await this.Context.Customers.FindAsync(new object[] { customerId }, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                this.Logger.LogWarning($"Customer with id '{customerId}' not found.");

                return Result<bool>.Failure(
                    ErrorCodes.CustomerDoesNotExist,
                    $"Customer with id '{customerId}' not found.");
            }

            this.Context.Customers.Remove(entity);

            await this.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this.Logger.LogInformation($"Customer '{entity.Name}' deleted successfully.");

            return Result<bool>.Success(true);
        }

        /// <inheritdoc/>
        public async Task<Result<Customer>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            Entities.Customer entity = await this.Context.Customers.FindAsync(new object[] { customerId }, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                this.Logger.LogWarning($"Customer with id '{customerId}' not found.");

                return Result<Customer>.Failure(
                    ErrorCodes.CustomerDoesNotExist,
                    $"Customer with id '{customerId}' not found.");
            }

            Customer customer = this.Mapper.Map<Entities.Customer, Customer>(entity);

            return Result<Customer>.Success(customer);
        }

        /// <inheritdoc />
        public async Task<Result<GeolocationData>> GetCustomerGeolocationAsync(string customerId, CancellationToken cancellationToken = default)
        {
            Entities.Customer entity = await this.Context.Customers.FindAsync(new object[] { customerId }, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                this.Logger.LogWarning($"Customer with id '{customerId}' not found.");

                return Result<GeolocationData>.Failure(
                    ErrorCodes.CustomerDoesNotExist,
                    $"Customer with id '{customerId}' not found.");
            }

            if (string.IsNullOrWhiteSpace(entity.Address))
            {
                this.Logger.LogWarning($"Customer with id '{customerId}' does not have an address.");

                return Result<GeolocationData>.Failure(
                    ErrorCodes.CustomerDoesNotHaveAnAddress,
                    $"Customer with id '{customerId}' does not have an address.");
            }

            GeolocationData geolocationData = await this.geoLocationService.GetGeolocationAsync(entity.Address, cancellationToken).ConfigureAwait(false);

            if (geolocationData == null)
            {
                this.Logger.LogWarning($"Could not get geolocation data for address '{entity.Address}'.");

                return Result<GeolocationData>.Failure(
                    ErrorCodes.CouldNotGetGeolocation,
                    $"Could not get geolocation data for address '{entity.Address}'.");
            }

            return Result<GeolocationData>.Success(geolocationData);
        }

        /// <inheritdoc />
        public async Task<Result<List<CustomerDto>>> ListCustomersAsync(string name, string email, CancellationToken cancellationToken = default)
        {
            List<Entities.Customer> entities = await this.Context.Customers.ToListAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(email))
            {
                // This could be a FirstOrDefault(). Since the email is unique we can only get one customer,if there is a match. But that would add an extra creation of a list and reduce performance for a small list of customers.
                entities = entities.Where(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Optionally filter the customers by name and/or email
            if (!string.IsNullOrEmpty(name))
            {
                entities = entities.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            List<CustomerDto> customers = this.Mapper.Map<List<Entities.Customer>, List<CustomerDto>>(entities);

            return Result<List<CustomerDto>>.Success(customers);
        }

        #endregion
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;

namespace CustomerStoreApi.Managers
{
    /// <summary>
    /// Defines the mapping profile used by the application.
    /// </summary>
    /// <seealso cref="Profile" />
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Created via dependency injection.")]
    internal partial class MappingProfile : Profile
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingProfile"/> class.
        /// </summary>
        public MappingProfile()
        {
            this.CreateMap<Models.Customer, Entities.Customer>()
                .AfterMap(
                    (source, target) =>
                    {
                        target.Id = Guid.NewGuid().ToString();
                    });

            this.CreateMap<Entities.Customer, Models.CustomerDto>();

            this.CreateMap<Entities.Customer, Models.Customer>();
        }

        #endregion
    }
}

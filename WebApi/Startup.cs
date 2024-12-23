﻿using CustomerStoreApi.Managers;
using CustomerStoreApi.Services;
using CustomerStoreApi.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CustomerStoreApi
{
    /// <summary>
    /// Defines the startup of the application.
    /// </summary>
    public partial class Startup
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Configures the services required by the application.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("CustomerStoreApiDb"));

            services.AddControllers();

            // change lifetime of Customers manager to scoped
            services.AddTransient<IGeoLocationService, PositionStackGeolocationService>();
            services.AddScoped<ICustomersManager, CustomersManager>();

            services.AddOpenApiDocument(
                (options) =>
                {
                    options.DocumentName = "Version 1";
                    options.Title = "CustomerStore API";
                    options.Description = "REST API for the CustomerStore Web Application";
                });

            services
                .AddAutoMapper(
                    (provider, options) =>
                    {
                        foreach (Profile profile in provider.GetServices<Profile>())
                        {
                            options.AddProfile(profile);
                        }
                    },
                    assemblies: null);

            services.AddSingleton<Profile, MappingProfile>();
        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseOpenApi();

            app.UseSwaggerUi3();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        #endregion
    }
}

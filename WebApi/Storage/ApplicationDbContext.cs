using CustomerStoreApi.Entities;
using CustomerStoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerStoreApi.Storage;

/// <summary>
/// Defines the application database context.
/// </summary>
/// <seealso cref="DbContext" />
internal sealed class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the customers database set.
    /// </summary>
    public DbSet<Customer> Customers => this.Set<Customer>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.Id);
            customer.Property(c => c.Name).HasMaxLength(CreateCustomerRequest.MaxFieldLength).IsRequired();
            customer.Property(c => c.Email).HasMaxLength(CreateCustomerRequest.MaxFieldLength).IsRequired();
            customer.Property(c => c.Address).HasMaxLength(CreateCustomerRequest.MaxFieldLength);
            customer.Property(c => c.VatNumber).HasMaxLength(9);
            customer.HasIndex(c => c.Email).IsUnique();
        });
    }
}

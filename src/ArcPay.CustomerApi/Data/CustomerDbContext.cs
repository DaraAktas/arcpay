using ArcPay.CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.CustomerApi.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("customer_number_seq")
            .StartsAt(1_000_000_001);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.CustomerNumber)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ARC-' || nextval('customer_number_seq')::text")
                .ValueGeneratedOnAdd();
            entity.HasIndex(customer => customer.CustomerNumber).IsUnique();

            entity.Property(customer => customer.FullName).HasMaxLength(150);
            entity.Property(customer => customer.Email).HasMaxLength(320);
            entity.Property(customer => customer.NormalizedEmail).HasMaxLength(320);
            entity.HasIndex(customer => customer.NormalizedEmail).IsUnique();
            entity.Property(customer => customer.PasswordHash).HasMaxLength(100);
        });
    }
}

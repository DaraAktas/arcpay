using ArcPay.CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.CustomerApi.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; }
}
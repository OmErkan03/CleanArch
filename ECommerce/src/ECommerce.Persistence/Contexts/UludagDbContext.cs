using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Persistence.Contexts
{
    public class UludagDbContext : DbContext
    {
        public UludagDbContext(DbContextOptions options) : base(options)
        {

        }
        protected UludagDbContext()
        {

        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UludagDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}

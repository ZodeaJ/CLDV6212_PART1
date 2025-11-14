using System.Collections.Generic;
using ABCRetailers.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Cart> Cart => Set<Cart>();

        public DbSet<Order> Orders => Set<Order>();
    }
}

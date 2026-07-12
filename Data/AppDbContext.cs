using HubClub.Models; // adjust if your models are in a different namespace
using Microsoft.EntityFrameworkCore;

namespace HubClub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<UserPackage> UserPackages { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionProduct> SessionProducts { get; set; }
        public DbSet<PricingSetting> PricingSettings { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<DailyClosing> DailyClosings { get; set; }
    }
}
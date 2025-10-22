using CarRepairGame.Models;
using ISIP123_Soldatov_bd;
using System.Data.Entity;

namespace CarRepairGame.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<Purchase> Purchases { get; set; }

    }
}
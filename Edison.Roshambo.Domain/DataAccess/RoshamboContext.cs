using System.Data.Entity;
using Edison.Roshambo.Domain.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.Roshambo.Domain.DataAccess
{
    public class RoshamboContext :
        IdentityDbContext
            <CustomUser, CustomIdentityRole, int, CustomIdentityUserLogin, CustomIdentityUserRole,
                CustomIdentityUserClaim>
    {
        public RoshamboContext() : base("DefaultConnection")
        {
            Players = Set<Competitor>();
            Lobbies = Set<Lobby>();
            Games = Set<Game>();
        }

        public DbSet<Lobby> Lobbies { get; set; }
        public DbSet<Competitor> Players { get; set; }
        public DbSet<Game> Games { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new LobbyStateConfiguration());
            modelBuilder.Configurations.Add(new LobbyConfiguration());
            modelBuilder.Configurations.Add(new CompetitorConfiguration());
            modelBuilder.Configurations.Add(new CustomUserConfiguration());
            modelBuilder.Configurations.Add(new GameConfiguration());
            modelBuilder.Configurations.Add(new GameShapeConfiguration());

            modelBuilder.Entity<CustomIdentityRole>().ToTable("Roles");
            modelBuilder.Entity<CustomIdentityUserClaim>().ToTable("UsersToClaims");
            modelBuilder.Entity<CustomIdentityUserRole>().ToTable("UsersToRoles");
            modelBuilder.Entity<CustomIdentityUserLogin>().ToTable("UserToLogins");
        }
    }
}
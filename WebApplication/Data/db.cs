using Microsoft.EntityFrameworkCore;
using WebApplication.Models; // Brings entity classes (User, Match, UserStats) into scope

namespace WebApplication.Data //Logical grouping for data-layer code (the ChessHubContext lives here).
{
    public class ChessHubContext : DbContext //ChessHubContext is your Entity Framework Core context — it represents the database session
                                             //and is the central point for querying and saving model instances.
    {
        public ChessHubContext(DbContextOptions<ChessHubContext> options) : base(options) { }
        //Constructor that accepts configuration (connection string, provider options)
        //Entity Framework Core uses this to configure the context at runtime.

        public DbSet<User> Users { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<UserStats> UserStats { get; set; }

        //Exposes the UsersTable and everything else as a queryable collection.
        //Each DbSet<T> corresponds to a table in the database and lets you query/add/update/delete entities.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //maps the entities to the tablesto specific tables and schemas in the database,
            //and configures primary keys and relationships.
            modelBuilder.Entity<User>().ToTable("UsersTable", "UsersSchema");
            modelBuilder.Entity<Match>().ToTable("MatchesTable", "MatchesSchema");
            modelBuilder.Entity<UserStats>().ToTable("UserStatsTable", "StatsSchema");

            //configure primary keys
            modelBuilder.Entity<User>().HasKey(u => u.UserID);
            modelBuilder.Entity<Match>().HasKey(m => m.MatchID);
            modelBuilder.Entity<UserStats>().HasKey(s => s.UserID);

            //configure relationships
            modelBuilder.Entity<UserStats>()
                .HasOne<User>()
                .WithOne()
                .HasForeignKey<UserStats>(s => s.UserID);
        }
    }
}
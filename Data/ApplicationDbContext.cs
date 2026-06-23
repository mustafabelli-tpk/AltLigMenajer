using Microsoft.EntityFrameworkCore;
using AltLigMenajer.Models;

namespace AltLigMenajer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<GameSetting> GameSettings => Set<GameSetting>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PendingContractOffer> PendingContractOffers => Set<PendingContractOffer>();
    public DbSet<Fixture> Fixtures => Set<Fixture>();
    public DbSet<ScoutReport> ScoutReports => Set<ScoutReport>();
    public DbSet<TransferOffer> TransferOffers => Set<TransferOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One Team → Many Players
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Team)
            .WithMany(t => t.Players)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Manager → One Team
        modelBuilder.Entity<Manager>()
            .HasOne(m => m.ManagedTeam)
            .WithOne(t => t.Manager)
            .HasForeignKey<Manager>(m => m.ManagedTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        // Fixture -> Teams
        modelBuilder.Entity<Fixture>()
            .HasOne(f => f.HomeTeam)
            .WithMany()
            .HasForeignKey(f => f.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Fixture>()
            .HasOne(f => f.AwayTeam)
            .WithMany()
            .HasForeignKey(f => f.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

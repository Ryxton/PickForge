using Microsoft.EntityFrameworkCore;
using PickForge.Api.Models;

namespace PickForge.Api.Data;

public class PickForgeDbContext : DbContext
{
    public PickForgeDbContext(DbContextOptions<PickForgeDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<Prediction> Predictions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Prediction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GameId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HomeTeam).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AwayTeam).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PredictedWinner).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            // Indexes for common queries
            entity.HasIndex(e => new { e.Week, e.SeasonYear });
            entity.HasIndex(e => e.GameId);
            entity.HasIndex(e => e.CreatedUtc);
        });
    }
}
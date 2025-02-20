using DndMasterCover.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Toolbelt.ComponentModel.DataAnnotations;

namespace DndMasterCover.DataAccess.Context;

public class DatabaseContext : DbContext
{
    public const string DefaultSchema = "dnd";
    public const string DefaultMigrationHistoryTableName = "__MigrationsHistory";

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }
    
    public DbSet<EnemySearch> EnemySearches { get; set; } = null!;
    public DbSet<Enemy> Enemies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.BuildIndexesFromAnnotations();
        modelBuilder.Entity<EnemySearch>().HasIndex(x => x.Name).HasMethod("GIN").IsTsVectorExpressionIndex("russian");
        modelBuilder.Entity<Enemy>()
                    .OwnsMany(e => e.Abilities, a =>
                    {
                        a.ToJson();
                    });
    }
}
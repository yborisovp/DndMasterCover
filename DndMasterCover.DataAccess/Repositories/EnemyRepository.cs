using DndMasterCover.DataAccess.Context;
using DndMasterCover.DataAccess.Interfaces;
using DndMasterCover.DataAccess.Models;
using FR.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace DndMasterCover.DataAccess.Repositories;

public class EnemyRepository: BaseRepository, IEnemyRepository
{
    public EnemyRepository(IDatabaseContextFactory<DatabaseContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task UpdateEnemiesAsync(IEnumerable<EnemySearch> enemySearches, CancellationToken ct = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await context.EnemySearches.ExecuteDeleteAsync(ct);
        await context.EnemySearches.AddRangeAsync(enemySearches, ct);
        await context.SaveChangesAsync(ct);
    }

    
    public async Task<IList<EnemySearch>> SearchForEnemiesAsync(
    string searchString,
    int? lowerLevel,
    int? upperLevel,
    SortLevel? sortByDanger,
    CancellationToken ct = default)
    {
        await using var context = ContextFactory.CreateDbContext();

        // Build the base query and apply filters.
        var baseQuery = ApplyFilters(context.EnemySearches.AsQueryable());

        // Apply full-text search with ranking.
        var fullTextQuery = baseQuery
                            .Where(e => EF.Functions.ToTsVector("russian", e.Name)
                                          .Matches(EF.Functions.ToTsQuery("russian", searchString)))
                            .Select(e => new
                            {
                                Enemy = e,
                                Rank = EF.Functions.ToTsVector("russian", e.Name)
                                         .Rank(EF.Functions.PhraseToTsQuery("russian", $"%{searchString}%"))
                            })
                            .OrderByDescending(x => x.Rank)
                            .Select(x => x.Enemy);

        var enemies = await fullTextQuery.ToListAsync(ct);

        // Fallback to ILike search if no enemies were found.
        if (!enemies.Any())
        {
            var fallbackQuery = ApplyFilters(
                                             context.EnemySearches.Where(e => EF.Functions.ILike(e.Name, $"%{searchString}%"))
                                            );
            enemies = await fallbackQuery.ToListAsync(ct);
        }

        return enemies;

        // Helper function to apply level filtering and sorting.
        IQueryable<EnemySearch> ApplyFilters(IQueryable<EnemySearch> query)
        {
            if (lowerLevel.HasValue)
            {
                query = query.Where(e => e.Danger >= lowerLevel.Value);
            }
            if (upperLevel.HasValue)
            {
                query = query.Where(e => e.Danger <= upperLevel.Value);
            }
            if (sortByDanger.HasValue)
            {
                query = sortByDanger.Value switch
                        {
                            SortLevel.Asc => query.OrderBy(e => e.Danger),
                            SortLevel.Desc => query.OrderByDescending(e => e.Danger),
                            _ => query
                        };
            }
            return query;
        }
    }


    public async Task<Enemy?> GetEnemyById(string externalId, CancellationToken ct = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var enemy = await context.Enemies.SingleOrDefaultAsync(e => e.ExternalId == externalId, ct);
        return enemy;
    }

    public async Task<Enemy> CreateEnemyAsync(Enemy enemy, CancellationToken ct = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var enemyEntity = await context.AddAsync(enemy, ct);
        await context.SaveChangesAsync(ct);
        return enemyEntity.Entity;
    }
}
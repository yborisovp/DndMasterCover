using DndMasterCover.DataAccess.Models;

namespace DndMasterCover.DataAccess.Interfaces;

public interface IEnemyRepository
{
    Task UpdateEnemiesAsync(IEnumerable<EnemySearch> enemySearches, CancellationToken ct = default);
    Task<IList<EnemySearch>> SearchForEnemiesAsync(string searchString, int? lowerLevel, int? upperLevel, SortLevel? sortByDanger,  CancellationToken ct = default);
    Task<Enemy?> GetEnemyById(string externalId, CancellationToken ct = default);
    Task<Enemy> CreateEnemyAsync(Enemy enemy, CancellationToken ct = default);
}

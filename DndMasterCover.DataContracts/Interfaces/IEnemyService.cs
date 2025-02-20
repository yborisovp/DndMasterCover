namespace DndMasterCover.DataContracts.Interfaces;

public interface IEnemyService
{
    Task UpdateEnemiesAsync(CancellationToken ct = default);
    Task<IEnumerable<EnemySearchDto>> SearchForEnemiesAsync(string? searchString, string? dangerLevel, SortTypeDto? sort, CancellationToken ct = default);
    Task<EnemyDto> GetEnemyAync(string link, CancellationToken ct);
}
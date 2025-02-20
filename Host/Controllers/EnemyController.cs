//Mock for file structure

using DndMasterCover.DataContracts;
using DndMasterCover.DataContracts.Interfaces;
using DndMasterCover.Parsers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class EnemyController: ControllerBase
{
    private readonly IEnemyService _enemyService;

    public EnemyController(IEnemyService enemyService)
    {
        _enemyService = enemyService;
    }

    [HttpGet("search")]
    public async Task<IEnumerable<EnemySearchDto>> SearchForEnemiesAsync(string? q, string? dangerLevel, SortTypeDto? sort, CancellationToken ct = default)
    {
        return await _enemyService.SearchForEnemiesAsync(q, dangerLevel, sort,  ct);
    }

    [HttpGet]
    public async Task<EnemyDto> GetEnemy(string link, CancellationToken ct = default)
    {
        return await _enemyService.GetEnemyAync(link, ct);
    }
    
#if DEBUG
    [HttpGet("update_enemies")]
    public async Task UpdateDatabase(CancellationToken ct = default)
    {
        await _enemyService.UpdateEnemiesAsync(ct);
    }
#endif
}
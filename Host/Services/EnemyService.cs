using DndMasterCover.DataAccess.Interfaces;
using DndMasterCover.DataContracts;
using DndMasterCover.DataContracts.Interfaces;
using DndMasterCover.Mappers;
using DndMasterCover.Parsers;

namespace DndMasterCover.Services;

public class EnemyService: IEnemyService
{
    private readonly ILogger<EnemyService> _logger;
    private readonly IEnemyParser _enemyParser;
    private readonly IEnemyRepository _enemyRepository;

    public EnemyService(ILogger<EnemyService> logger, IEnemyParser enemyParser, IEnemyRepository enemyRepository)
    {
        _logger = logger;
        _enemyParser = enemyParser;
        _enemyRepository = enemyRepository;
    }

    public async Task UpdateEnemiesAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Updating enemies. This is a complex task.");
        var enemies = await _enemyParser.Parse(ct);
        await _enemyRepository.UpdateEnemiesAsync(enemies.ToEntity(), ct);
    }


    public async Task<IEnumerable<EnemySearchDto>> SearchForEnemiesAsync(string? searchString, string? dangerLevel, SortTypeDto? sort, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching for enemies."); 
        if (string.IsNullOrEmpty(searchString))
        {
            _logger.LogDebug("Search string is empty");
            return [];
        }

        int? lowerLevel = null;
        int? upperLevel = null;

        if (dangerLevel is not null)
        {
            if (dangerLevel.Contains('-'))
            {
                lowerLevel = int.Parse(dangerLevel.Split("-")[0]);
                upperLevel = int.Parse(dangerLevel.Split("-")[1]);
            }
            else if (dangerLevel.Contains('<'))
            {
                upperLevel = int.Parse(dangerLevel.Split("<")[1]);
            }
            else if (dangerLevel.Contains('>'))
            {
                lowerLevel = int.Parse(dangerLevel.Split(">")[1]);
            }
            else
            {
                lowerLevel = int.Parse(dangerLevel);
                upperLevel = int.Parse(dangerLevel);
            }
            
        }
        var enemies = await _enemyRepository.SearchForEnemiesAsync(searchString, lowerLevel, upperLevel, sort.ToEntity(), ct);
        return enemies.ToDto();
    }

    public async Task<EnemyDto> GetEnemyAync(string link, CancellationToken ct)
    {
        _logger.LogDebug("Get enemy ${enemy link}", link);
        var externalId = link.Split("-")[0];
        var enemy = (await _enemyRepository.GetEnemyById(externalId, ct)).ToDto();
        if (enemy is null)
        {
            enemy = await _enemyParser.ParseEnemy(link, ct);
            var enemyEntity = await _enemyRepository.CreateEnemyAsync(enemy.ToEntity(), ct);
            enemy = enemyEntity.ToDto();
        }

        return enemy ?? throw new NullReferenceException("Error in link. Cannot parse enemy");
    }
}
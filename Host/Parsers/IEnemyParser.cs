using DndMasterCover.DataContracts;

namespace DndMasterCover.Parsers;

public interface IEnemyParser
{
    Task<IList<EnemySearchDto>> Parse(CancellationToken ct = default);
    Task<EnemyDto> ParseEnemy(string link, CancellationToken ct);
}
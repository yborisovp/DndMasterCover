using System.ComponentModel.DataAnnotations;

namespace DndMasterCover.DataContracts;

public class EnemyDto
{
    public int Id { get; set; }
    [MaxLength(150)]
    public string ExternalId { get; set; } = string.Empty;
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    public float DangerLevel { get; set; }
    public int Hp { get; set; }
    public int? MaxHp { get; set; } // If not provided, we treat hp as max.
    [MaxLength(150)]
    public string Class { get; set; } = string.Empty;// For example, "15 (природный доспех)"
    [MaxLength(3000)]
    public string Description { get; set; } = string.Empty;
    public IList<AbilityDto> Abilities { get; set; } = [];
}
namespace DndMasterCover.DataAccess.Models;

public class Ability
{
    /// <summary>
    /// Here WeaponType holds the ability name.
    /// </summary>
    public string WeaponType { get; set; } = string.Empty;
    public string AttackDiceRoll { get; set; }
    public string HitDiceRoll { get; set; }
    public string DamageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
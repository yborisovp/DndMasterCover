namespace DndMasterCover.DataContracts;

public class AbilityDto
{
    // Here WeaponType holds the ability name.
    public string WeaponType { get; set; }
    public string AttackDiceRoll { get; set; }
    public string HitDiceRoll { get; set; }
    public string DamageType { get; set; }
    public string Description { get; set; }
}
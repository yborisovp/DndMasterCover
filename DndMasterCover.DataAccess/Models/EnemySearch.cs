namespace DndMasterCover.DataAccess.Models;

public class EnemySearch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required float Danger { get; set; }
    public required string Link { get; set; }
}
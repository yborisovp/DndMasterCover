using System.Text.Json;

namespace DndMasterCover.Helpers;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var newName = new System.Text.StringBuilder();
        newName.Append(char.ToLowerInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                newName.Append('_');
                newName.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                newName.Append(name[i]);
            }
        }

        return newName.ToString().ToLowerInvariant();
    }
}
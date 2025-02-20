using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DndMasterCover.DataContracts;
using DndMasterCover.Helpers;
using HtmlAgilityPack;

namespace DndMasterCover.Parsers;

public class EnemiesParser : IEnemyParser
{
    private readonly string _bestiaryUrl = "https://dnd.su/bestiary/";
    private readonly ILogger<EnemiesParser> _logger;

    public EnemiesParser(ILogger<EnemiesParser> logger)
    {
        _logger = logger;
    }

    public async Task<IList<EnemySearchDto>> Parse(CancellationToken ct = default)
    {
        // Download the page HTML.
        using var httpClient = new HttpClient();

        var html = await httpClient.GetStringAsync(_bestiaryUrl, ct);

        // Load the HTML into HtmlAgilityPack.
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Select enemy nodes.
        // Assuming each enemy is contained in an <a> element with a class that includes "list-item"
        var enemyNodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'list-item-wrapper')]");
        var enemyList = new List<EnemySearchDto>();

        if (enemyNodes != null)
        {
            foreach (var node in enemyNodes)
            {
                // Extract the enemy name.
                var enemyNameNode = node.SelectSingleNode(".//div[contains(@class, 'list-item-title')]");

                // Extract danger level from span.list-mark__danger > span
                var dangerNode = node.SelectSingleNode(".//span[contains(@class, 'list-mark__danger')]/span");

                if (enemyNameNode == null || dangerNode == null) continue;

                var enemyName = enemyNameNode.InnerText.Trim();
                var danger = dangerNode.InnerText.Trim();

                // Get the link from the <a> tag's href attribute.
                var link = node.GetAttributeValue("href", "");
                link = link?.Split("/")[2];

                enemyList.Add(new EnemySearchDto
                {
                    Name = enemyName,
                    Danger = ParseFraction(danger),
                    Link = link
                });
            }
        }
        else
        {
            _logger.LogError("No enemy nodes found. Check the HTML structure and XPath query.");
        }

        return enemyList;
    }

    public async Task<EnemyDto> ParseEnemy(string link, CancellationToken ct)
    {
        using var httpClient = new HttpClient();
        var url = _bestiaryUrl + link + "/";
        var html = await httpClient.GetStringAsync(url, ct);

        // Load the HTML into HtmlAgilityPack.
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Create the EnemyDto instance.
        var enemy = new EnemyDto();
        var rootNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'card')]");

        // 1. Parse the enemy name.
        var nameNode = rootNode.SelectSingleNode(".//h2[@class='card-title']//span[@data-copy]");
        if (nameNode != null)
        {
            enemy.Name = nameNode.InnerText.Trim();
            _logger.LogDebug("[LOG] Parsed enemy name: {Name}", enemy.Name);
        }
        else
        {
            throw new AggregateException("Cannot retrieve name. Probably an error.");
        }

        // 2. Parse HP.
        var hpLi = rootNode.SelectSingleNode("//li[strong[contains(text(),'Хиты')]]");
        var hpNode = hpLi?.SelectSingleNode(".//span[@data-type='middle']");
        if (hpNode != null && int.TryParse(hpNode.InnerText.Trim(), out int hp))
        {
            enemy.Hp = hp;
            enemy.MaxHp = hp;
            _logger.LogDebug("[LOG] Parsed HP: {Hp}", enemy.Hp);
        }

        // 3. Parse the enemy "class" (Armor Class info).
        var classLi = rootNode.SelectSingleNode(".//li[strong[contains(text(),'Класс Доспеха')]]");
        if (classLi != null)
        {
            enemy.Class = classLi.InnerText.Replace("Класс Доспеха", "").Trim();
            _logger.LogDebug("[LOG] Parsed Armor Class: {Class}", enemy.Class);
        }

        // 4. Parse Danger.
        var dangerLi = rootNode.SelectSingleNode(".//li[strong[contains(text(),'Опасность')]]");
        if (dangerLi != null)
        {
            var dangerText = dangerLi.InnerText.Replace("Опасность", "").Trim();
            var parts = dangerText.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[0], out int dangerValue))
            {
                enemy.DangerLevel = dangerValue;
                _logger.LogDebug("[LOG] Parsed Danger Level: {DangerLevel}", enemy.DangerLevel);
            }
        }

        // 5. Parse Description.
        var descNode =
            rootNode.SelectSingleNode(".//li[contains(@class, 'subsection') and .//h3[contains(text(),'Описание')]]//div");
        if (descNode != null)
        {
            enemy.Description = descNode.InnerText.Trim();
            _logger.LogDebug("[LOG] Parsed Description.");
        }

        // 6. Parse Abilities from action/effect sections.
        // We consider any subsection with class "subsection desc" (except those with "Описание") as an ability section.
        var actionsNodes = rootNode.SelectNodes(".//li[contains(@class, 'subsection desc')]");
        if (actionsNodes != null)
        {
            foreach (var actionNode in actionsNodes)
            {
                // Get the section header.
                var headerNode = actionNode.SelectSingleNode(".//h3");
                if (headerNode == null)
                {
                    _logger.LogDebug("[LOG] Ability section without header encountered.");
                    continue;
                }

                var sectionTitle = headerNode.InnerText.Trim();
                if (sectionTitle.Contains("Описание"))
                {
                    // Skip description sections.
                    continue;
                }

                _logger.LogDebug("[LOG] Processing ability section: {SectionTitle}", sectionTitle);

                // Get the content div that holds both <p> and <ul> elements.
                var contentDiv = actionNode.SelectSingleNode(".//div");
                if (contentDiv == null)
                {
                    continue;
                }

                AbilityDto? currentAbility = null;

                // Iterate over the child elements in document order.
                foreach (var child in contentDiv.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element))
                {
                    if (child.Name.Equals("p", StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip empty paragraphs.
                        if (string.IsNullOrWhiteSpace(child.InnerText))
                        {
                            _logger.LogDebug("[LOG] Skipping empty <p> tag.");
                            continue;
                        }

                        // Create a new ability from this <p>.
                        var ability = new AbilityDto();

                        // --- Ability Name Extraction ---
                        string abilityName = string.Empty;
                        var nodeStrongEm = child.SelectSingleNode(".//strong/em");
                        if (nodeStrongEm != null)
                        {
                            abilityName = nodeStrongEm.InnerText.Trim();
                        }
                        else
                        {
                            var nodeEmStrong = child.SelectSingleNode(".//em/strong");
                            if (nodeEmStrong != null)
                            {
                                abilityName = nodeEmStrong.InnerText.Trim();
                            }
                        }

                        if (string.IsNullOrEmpty(abilityName))
                        {
                            var nodeStrong = child.SelectSingleNode(".//strong");
                            if (nodeStrong != null)
                            {
                                abilityName = nodeStrong.InnerText.Trim().TrimEnd('.', ':');
                            }
                        }

                        if (string.IsNullOrEmpty(abilityName))
                        {
                            var text = child.InnerText.Trim();
                            int periodIndex = text.IndexOf('.');
                            abilityName = periodIndex > 0 ? text.Substring(0, periodIndex).Trim() : text;
                        }

                        // Prepend section title to indicate source.
                        ability.WeaponType = $"{sectionTitle} - {abilityName}";
                        _logger.LogDebug("[LOG] Parsed ability name: {AbilityName}", ability.WeaponType);

                        // --- Attack Dice Roll ---
                        var attackDiceSpan =
                            child.SelectSingleNode(".//span[contains(@class, 'dice') and contains(., 'к попаданию')]");
                        if (attackDiceSpan != null)
                        {
                            string onclick = attackDiceSpan.GetAttributeValue("onclick", "");
                            var diceParams = ParseDiceParameters(onclick);
                            ability.AttackDiceRoll = $"{diceParams.NumberOfDice}d{diceParams.DiceType} + {diceParams.Modifier}";
                            _logger.LogDebug("[LOG] Parsed AttackDiceRoll: {AttackDice}", ability.AttackDiceRoll);
                        }

                        // --- Hit Dice Roll ---
                        var hitDiceSpans = child.SelectNodes(".//span[contains(@class, 'dice') and not(contains(., 'к попаданию'))]"); // Fixed typo here
                        if (hitDiceSpans != null)
                        {
                            var hitDiceValues = new List<string>();
                            foreach (HtmlNode hitSpan in hitDiceSpans)
                            {
                                var hitValue = hitSpan.SelectSingleNode("./span[1]")?.InnerText.Trim();
                                if (!string.IsNullOrEmpty(hitValue))
                                {
                                    hitDiceValues.Add(hitValue);
                                }
                            }
                            ability.HitDiceRoll = string.Join(" и ", hitDiceValues);
                            _logger.LogDebug("[LOG] Parsed HitDiceRoll: {HitDice}", ability.HitDiceRoll);
                        }

                        // --- Damage Type ---
                        if (child.InnerText.ToLower(CultureInfo.InvariantCulture).Contains("ядом"))
                        {
                            ability.DamageType = "яд";
                            _logger.LogDebug("[LOG] Damage type set to 'яд'.");
                        }

                        // --- Full Description ---
                        ability.Description = child.InnerText.Trim();
                        _logger.LogDebug("[LOG] Ability description: {Desc}",
                                         ability.Description.Substring(0, Math.Min(50, ability.Description.Length)));

                        enemy.Abilities.Add(ability);
                        currentAbility = ability; // Set current ability for potential UL appending.
                    }
                    else if (child.Name.Equals("ul", StringComparison.OrdinalIgnoreCase))
                    {
                        // Process the <ul> as part of the current ability description.
                        if (currentAbility == null)
                        {
                            _logger.LogDebug("[LOG] Found <ul> but no current ability to attach it to.");
                            continue;
                        }

                        var markdown = ConvertUlToMarkdown(child);
                        _logger.LogDebug("[LOG] Converting <ul> to markdown:\n{Markdown}", markdown);
                        // Append the markdown (with a newline) to the current ability description.
                        currentAbility.Description += "\n" + markdown;
                    }
                }
            }
        }

        // Set external ID.
        var externalId = link.Split("-")[0];
        enemy.ExternalId = externalId;
        _logger.LogDebug("[LOG] Parsed External ID: {ExternalId}", enemy.ExternalId);
        return enemy;
    }

    /// <summary>
    /// Converts a <ul> node to Markdown bullet list format.
    /// </summary>
    private string ConvertUlToMarkdown(HtmlNode ulNode)
    {
        var markdownLines = new List<string>();
        var liNodes = ulNode.SelectNodes(".//li");
        if (liNodes != null)
        {
            markdownLines.AddRange(from li in liNodes select li.InnerText.Trim() into text where !string.IsNullOrEmpty(text) select $"- {text}");
        }

        return string.Join("\n", markdownLines);
    }


    public static float ParseFraction(object value)
    {
        // If value is already a numeric type, return its float representation.
        if (value is float f)
            return f;
        if (value is double d)
            return (float)d;
        if (value is int i)
            return i;

        // If the value is a string, try to parse it.
        if (value is string s)
        {
            if (s.Contains("/"))
            {
                string[] parts = s.Split('/');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out float numerator) &&
                    float.TryParse(parts[1], out float denominator) &&
                    denominator != 0)
                {
                    return numerator / denominator;
                }
            }

            // Otherwise, try to parse the string as a float.
            if (float.TryParse(s, out float result))
            {
                return result;
            }
        }

        return 0f;
    }
    private DiceParams ParseDiceParameters(string onclick)
    {
        if (string.IsNullOrEmpty(onclick))
            return null;

        var regex = new Regex(@"new\s+Dice\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(-?\d+)\s*");
        var match = regex.Match(onclick);
        if (match.Success)
        {
            return new DiceParams
            {
                DiceType = int.Parse(match.Groups[1].Value),
                NumberOfDice = int.Parse(match.Groups[2].Value),
                Modifier = int.Parse(match.Groups[3].Value)
            };
        }
        return null;
    }

    // Helper class to hold parsed dice parameters
    private class DiceParams
    {
        public int DiceType { get; set; }
        public int NumberOfDice { get; set; }
        public int Modifier { get; set; }
    }
}

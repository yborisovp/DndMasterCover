
using System.Collections;
using DndMasterCover.DataAccess.Models;
using DndMasterCover.DataContracts;

namespace DndMasterCover.Mappers;

public static class EnemyMapper
{
    public static IEnumerable<EnemySearchDto> ToDto(this IList<EnemySearch> enemies)
    {
        return enemies.Select(enemy => enemy.ToDto());
    }

    public static EnemySearchDto ToDto(this EnemySearch enemy)
    {
        return new EnemySearchDto
        {
            Name = enemy.Name,
            Danger = enemy.Danger,
            Link = enemy.Link
        };

    }

    public static IEnumerable<EnemySearch> ToEntity(this IList<EnemySearchDto> enemies)
    {
        return enemies.Select(e => e.ToEntity());
    }
    
    public static EnemySearch ToEntity(this EnemySearchDto enemy)
    {
        return new EnemySearch
        {
            Name = enemy.Name,
            Danger = enemy.Danger,
            Link = enemy.Link
        };
    }

    public static EnemyDto? ToDto(this Enemy? enemy)
    {
        if (enemy is null)
        {
            return null;
        }
        return new EnemyDto
        {
            Id = enemy.Id,
            ExternalId = enemy.ExternalId,
            Name = enemy.Name,
            DangerLevel = enemy.DangerLevel,
            Hp = enemy.Hp,
            MaxHp = enemy.MaxHp,
            Class = enemy.Class,
            Description = enemy.Description,
            Abilities = enemy.Abilities.ToDto(),
        };
    }

    public static IList<AbilityDto> ToDto(this IList<Ability> abilities)
    {
        return abilities.Select(e => e.ToDto()).ToList();
    }
    public static AbilityDto ToDto(this Ability ability)
    {
        return new AbilityDto
        {
            WeaponType = ability.WeaponType,
            AttackDiceRoll = ability.AttackDiceRoll,
            HitDiceRoll = ability.HitDiceRoll,
            DamageType = ability.DamageType,
            Description = ability.Description
        };
    }

    public static Enemy ToEntity(this EnemyDto enemy)
    {
        return new()
        {
            Id = enemy.Id,
            ExternalId = enemy.ExternalId,
            Name = enemy.Name,
            DangerLevel = enemy.DangerLevel,
            Hp = enemy.Hp,
            MaxHp = enemy.MaxHp,
            Class = enemy.Class,
            Description = enemy.Description,
            Abilities = enemy.Abilities.ToEntity(),
        };
    }
    
    public static IList<Ability> ToEntity(this IList<AbilityDto> abilities)
    {
        return abilities.Select(e => e.ToEntity()).ToList();
    }
    public static Ability ToEntity(this AbilityDto ability)
    {
        return new Ability
        {
            WeaponType = ability.WeaponType,
            AttackDiceRoll = ability.AttackDiceRoll,
            HitDiceRoll = ability.HitDiceRoll,
            DamageType = ability.DamageType,
            Description = ability.Description
        };
    }

    public static SortLevel? ToEntity(this SortTypeDto? sortType)
    {
        if (sortType is null)
        {
            return null;
        }
        return sortType switch
               {
                   SortTypeDto.Asc => SortLevel.Asc,
                   SortTypeDto.Desc => SortLevel.Desc,
                   _ => throw new Exception(),
               };
    }
}

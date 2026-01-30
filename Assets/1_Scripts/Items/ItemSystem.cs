using UnityEngine;

/// <summary>
/// Result of applying a consumable effect. Used by UI to log and update state.
/// </summary>
public struct ApplyConsumableResult
{
    public bool success;
    public int healedAmount;
}

public class ItemSystem : MonoBehaviour
{
    /// <summary>
    /// Converts SkillTargetType to UnitTargetType (for selection/targeting).
    /// </summary>
    public static UnitTargetType ConvertTargetType(SkillTargetType skillTargetType)
    {
        switch (skillTargetType)
        {
            case SkillTargetType.Self:
                return UnitTargetType.Self;
            case SkillTargetType.Ally:
                return UnitTargetType.AllAllies;
            case SkillTargetType.Enemy:
                return UnitTargetType.AllEnemies;
            case SkillTargetType.Any:
                return UnitTargetType.Any;
            default:
                return UnitTargetType.Any;
        }
    }

    /// <summary>
    /// Checks if a unit is a valid target for an item based on target type.
    /// </summary>
    public static bool IsValidItemTarget(Unit unit, Unit caster, SkillTargetType targetType)
    {
        if (unit == null || caster == null) return false;

        switch (targetType)
        {
            case SkillTargetType.Self:
                return unit == caster;
            case SkillTargetType.Ally:
                return caster.IsPlayerUnit == unit.IsPlayerUnit;
            case SkillTargetType.Enemy:
                return caster.IsPlayerUnit != unit.IsPlayerUnit;
            case SkillTargetType.Any:
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Checks if a healing item can be used (at least one valid target needs healing).
    /// </summary>
    public static bool CanUseHealingItem(Item item, Unit caster)
    {
        if (item == null || caster == null) return true;

        if (item.itemType != ItemType.Consumable || item.consumableSubtype != ConsumableSubtype.Heal)
            return true;

        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null) return true;

        foreach (Unit unit in allUnits)
        {
            if (unit == null || !unit.IsAlive()) continue;
            if (!IsValidItemTarget(unit, caster, item.targetType)) continue;
            if (unit.CurrentHP < unit.MaxHP)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a reason why the item cannot be used (e.g. "All allies are at full HP"), or empty string if usable.
    /// </summary>
    public static string GetItemUnusableReason(Item item, Unit currentUnit)
    {
        if (item == null || currentUnit == null) return "";

        if (item.itemType == ItemType.Consumable && item.consumableSubtype == ConsumableSubtype.Heal &&
            !CanUseHealingItem(item, currentUnit))
            return "All allies are at full HP";

        return "";
    }

    /// <summary>
    /// Applies consumable effect to the target. Does not remove from inventory or update UI.
    /// </summary>
    public static ApplyConsumableResult ApplyConsumableEffect(Item item, Unit target)
    {
        var result = new ApplyConsumableResult();

        if (item == null || target == null || item.itemType != ItemType.Consumable)
            return result;

        switch (item.consumableSubtype)
        {
            case ConsumableSubtype.Heal:
                if (target.IsAlive() && item.healAmount > 0)
                {
                    int oldHP = target.CurrentHP;
                    target.Heal(item.healAmount);
                    result.healedAmount = target.CurrentHP - oldHP;
                    result.success = true;
                }
                break;
            case ConsumableSubtype.Damage:
                Debug.Log($"Damage consumable not yet implemented: {item.itemName}");
                break;
            case ConsumableSubtype.Status:
                Debug.Log($"Status consumable not yet implemented: {item.itemName}");
                break;
        }

        return result;
    }
}

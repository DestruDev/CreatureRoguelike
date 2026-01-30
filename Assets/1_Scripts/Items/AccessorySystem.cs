using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One stat modifier from an equipped accessory (inline config on Item). Use with AccessorySystem.GetStatModifiers.
/// </summary>
public struct StatModifierEntry
{
    public StatType stat;
    public StatModifierDirection direction;
    public StatModifierValueType valueType;
    public float amount;
}

/// <summary>
/// Manages equipped accessories per unit and dispatches battle/turn/combat events to their effects.
/// Call EquipAccessory / UnequipAccessory from loadout; call OnBattleStart, OnTurnStart, etc. from your battle flow.
/// </summary>
public class AccessorySystem : MonoBehaviour
{
    private Dictionary<Unit, List<Item>> equippedByUnit = new Dictionary<Unit, List<Item>>();

    /// <summary>
    /// Equips an accessory on a unit. Ignores non-accessory items. Calls OnEquip on the item's effect.
    /// </summary>
    public void EquipAccessory(Item item, Unit unit)
    {
        if (item == null || unit == null) return;
        if (item.itemType != ItemType.Accessories) return;

        if (!equippedByUnit.TryGetValue(unit, out List<Item> list))
        {
            list = new List<Item>();
            equippedByUnit[unit] = list;
        }

        if (list.Contains(item)) return;

        list.Add(item);
        item.accessoryEffect?.OnEquip(unit);
    }

    /// <summary>
    /// Unequips an accessory from a unit. Calls OnUnequip on the item's effect.
    /// </summary>
    public void UnequipAccessory(Item item, Unit unit)
    {
        if (item == null || unit == null) return;
        if (!equippedByUnit.TryGetValue(unit, out List<Item> list)) return;

        if (!list.Remove(item)) return;

        item.accessoryEffect?.OnUnequip(unit);
        if (list.Count == 0)
            equippedByUnit.Remove(unit);
    }

    /// <summary>
    /// Returns a read-only list of accessories currently equipped on the unit. Do not modify.
    /// </summary>
    public IReadOnlyList<Item> GetEquippedAccessories(Unit unit)
    {
        if (unit == null || !equippedByUnit.TryGetValue(unit, out List<Item> list))
            return null;
        return list;
    }

    /// <summary>
    /// Returns stat modifiers from equipped accessories that use inline Stat Modifier config (no ScriptableObject effect).
    /// Use this to compute effective ATK/DEF/SPD/HP when resolving stats.
    /// </summary>
    public List<StatModifierEntry> GetStatModifiers(Unit unit)
    {
        var result = new List<StatModifierEntry>();
        if (unit == null || !equippedByUnit.TryGetValue(unit, out List<Item> list)) return result;
        foreach (Item item in list)
        {
            if (item == null || item.itemType != ItemType.Accessories) continue;
            if (item.accessoryEffectType != AccessoryEffectType.StatModifier) continue;
            result.Add(new StatModifierEntry
            {
                stat = item.statModifierStat,
                direction = item.statModifierDirection,
                valueType = item.statModifierValueType,
                amount = item.statModifierAmount
            });
        }
        return result;
    }

    /// <summary>
    /// Call when a battle starts for the given unit. Dispatches OnBattleStart to all equipped effects.
    /// </summary>
    public void OnBattleStart(Unit unit)
    {
        DispatchToEffects(unit, effect => effect.OnBattleStart(unit));
    }

    /// <summary>
    /// Call when a battle ends for the given unit. Dispatches OnBattleEnd to all equipped effects.
    /// </summary>
    public void OnBattleEnd(Unit unit)
    {
        DispatchToEffects(unit, effect => effect.OnBattleEnd(unit));
    }

    /// <summary>
    /// Call at turn start for the given unit. Dispatches OnTurnStart to all equipped effects.
    /// </summary>
    public void OnTurnStart(Unit unit)
    {
        DispatchToEffects(unit, effect => effect.OnTurnStart(unit));
    }

    /// <summary>
    /// Call at turn end for the given unit. Dispatches OnTurnEnd to all equipped effects.
    /// </summary>
    public void OnTurnEnd(Unit unit)
    {
        DispatchToEffects(unit, effect => effect.OnTurnEnd(unit));
    }

    /// <summary>
    /// Call when the attacker hits the target. Dispatches OnAttack to the attacker's equipped effects.
    /// </summary>
    public void OnAttack(Unit attacker, Unit target)
    {
        if (attacker == null) return;
        DispatchToEffects(attacker, effect => effect.OnAttack(attacker, target));
    }

    /// <summary>
    /// Call when the unit takes damage. Each effect can modify damage via ref. Dispatch in order.
    /// </summary>
    public void OnDamaged(Unit unit, ref int damage)
    {
        if (unit == null || !equippedByUnit.TryGetValue(unit, out List<Item> list)) return;
        foreach (Item item in list)
        {
            if (item?.accessoryEffect == null) continue;
            item.accessoryEffect.OnDamaged(unit, ref damage);
        }
    }

    /// <summary>
    /// Call when the unit would receive fatal damage. Effects can reduce damage and set cancelDeath to true to cheat death.
    /// </summary>
    public void OnReceiveFatalDamage(Unit unit, ref int damage, ref bool cancelDeath)
    {
        if (unit == null || !equippedByUnit.TryGetValue(unit, out List<Item> list)) return;
        foreach (Item item in list)
        {
            if (item?.accessoryEffect == null) continue;
            item.accessoryEffect.OnReceiveFatalDamage(unit, ref damage, ref cancelDeath);
        }
    }

    private void DispatchToEffects(Unit unit, System.Action<AccessoryEffect> call)
    {
        if (unit == null || !equippedByUnit.TryGetValue(unit, out List<Item> list)) return;
        foreach (Item item in list)
        {
            if (item?.accessoryEffect == null) continue;
            call(item.accessoryEffect);
        }
    }
}

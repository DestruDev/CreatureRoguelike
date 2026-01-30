using UnityEngine;

public enum AccessoryEffectType
{
    StatModifier,     // ATK/DEF/HP/speed/etc
    ResourceModifier, // Gold, XP, drops, shop prices
    Survival,         // Cheat death, shields, damage reduction
    ProcEffect,       // On hit, on kill, on turn start stuff
    Utility           // Weird stuff: move order, draw extra, rerolls, etc
}

public abstract class AccessoryEffect : ScriptableObject
{
    public AccessoryEffectType effectType;

    public virtual void OnEquip(Unit unit) { }
    public virtual void OnUnequip(Unit unit) { }
    public virtual void OnBattleStart(Unit unit) { }
    public virtual void OnBattleEnd(Unit unit) { }
    public virtual void OnTurnStart(Unit unit) { }
    public virtual void OnTurnEnd(Unit unit) { }
    public virtual void OnAttack(Unit attacker, Unit target) { }
    public virtual void OnDamaged(Unit unit, ref int damage) { }
    public virtual void OnReceiveFatalDamage(Unit unit, ref int damage, ref bool cancelDeath) { }
}

using UnityEngine;

/// <summary>
/// Stat modifier accessory: increase/decrease a stat by a flat amount or percentage.
/// Configure direction, value type, and which stat in the Inspector.
/// </summary>
public enum StatModifierDirection
{
    Increase,
    Decrease
}

public enum StatModifierValueType
{
    Flat,
    Percentage
}

public enum StatType
{
    Atk,
    Def,
    Spd,
    Hp
}

[CreateAssetMenu(fileName = "New Stat Modifier", menuName = "Accessory Effects/Stat Modifier")]
public class AccessoryStatModifier : AccessoryEffect
{
    [Header("Stat Modifier")]
    [Tooltip("Whether to increase or decrease the stat")]
    public StatModifierDirection direction = StatModifierDirection.Increase;

    [Tooltip("Flat value (e.g. +5) or percentage (e.g. 0.1 = 10%)")]
    public StatModifierValueType valueType = StatModifierValueType.Flat;

    [Tooltip("Which stat to modify")]
    public StatType stat = StatType.Atk;

    [Tooltip("Flat amount (e.g. 5) or percentage as decimal (e.g. 0.1 = 10%)")]
    public float amount = 5f;
}

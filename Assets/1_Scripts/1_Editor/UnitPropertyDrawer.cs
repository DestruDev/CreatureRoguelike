using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    private SerializedProperty creatureDataProperty;
    
    private void OnEnable()
    {
        creatureDataProperty = serializedObject.FindProperty("creatureUnitData");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        Unit unit = (Unit)target;
        
        EditorGUILayout.HelpBox("Both player units and enemy units use CreatureUnitData. Team assignment is determined by spawn area.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Show CreatureUnitData field
        EditorGUILayout.PropertyField(creatureDataProperty, new GUIContent("Unit Data (CreatureUnitData)"));
        
        EditorGUILayout.Space();
        
        // Show validation message
        if (creatureDataProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Please assign CreatureUnitData.", MessageType.Warning);
        }
        else
        {
            // Show read-only team info
            EditorGUILayout.LabelField("Team Assignment:", unit.IsPlayerUnit ? "Player Unit" : "Enemy Unit");
            
            // Display runtime stats during gameplay
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
                EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);
                
                // Unit Info
                EditorGUILayout.LabelField("Unit Name:", unit.UnitName);
                EditorGUILayout.LabelField("Unit ID:", !string.IsNullOrEmpty(unit.UnitID) ? unit.UnitID : "N/A");
                EditorGUILayout.LabelField("Is Alive:", unit.IsAlive() ? "Yes" : "No");
                
                EditorGUILayout.Space();
                
                // Combat Stats
                EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("HP:", $"{unit.CurrentHP} / {unit.MaxHP}");
                
                // HP Progress Bar
                if (unit.MaxHP > 0)
                {
                    float hpPercent = (float)unit.CurrentHP / unit.MaxHP;
                    Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                    EditorGUI.ProgressBar(rect, hpPercent, $"{Mathf.RoundToInt(hpPercent * 100)}%");
                }
                
                // EditorGUILayout.LabelField("Attack Damage:", unit.AttackDamage.ToString());
                EditorGUILayout.LabelField("Defense:", unit.Defense.ToString());
                EditorGUILayout.LabelField("Speed:", unit.Speed.ToString());
                
                EditorGUILayout.Space();
                
                // Action Gauge
                EditorGUILayout.LabelField("Action Gauge:", EditorStyles.boldLabel);
                float actionGauge = unit.GetActionGauge();
                EditorGUILayout.LabelField("Current Gauge:", $"{actionGauge:F1} / 100.0");
                
                // Action Gauge Progress Bar
                float gaugePercent = actionGauge / 100f;
                Rect gaugeRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(gaugeRect, Mathf.Clamp01(gaugePercent), $"{Mathf.RoundToInt(Mathf.Clamp01(gaugePercent) * 100)}%");
                
                EditorGUILayout.Space();
                
                // Skills
                EditorGUILayout.LabelField("Skills:", EditorStyles.boldLabel);
                if (unit.Skills.Length > 0)
                {
                    for (int i = 0; i < unit.Skills.Length; i++)
                    {
                        Skill skill = unit.Skills[i];
                        if (skill != null)
                        {
                            bool canUse = unit.CanUseSkill(i);
                            string cooldownInfo = canUse ? "Ready" : $"Cooldown: {unit.GetSkillCooldown(i)} turns";
                            EditorGUILayout.LabelField($"{i + 1}. {skill.skillName}", canUse ? "✓ " + cooldownInfo : cooldownInfo);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No skills assigned");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Team assignment can be overridden at runtime based on spawn area.", MessageType.Info);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}

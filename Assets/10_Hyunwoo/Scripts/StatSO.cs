using System;
using UnityEngine;

namespace _10_Hyunwoo.Scripts
{
    public enum UpgradeEffect
    {
        None, CoilTurns, CoilThickness, CoilMaterial, MagnetStrength,
        MagnetSize, BonusDuration, AutomaticShake, AutomaticShakeSpeed, Ending
    }
    [CreateAssetMenu(fileName = "StatSO", menuName = "Scriptable Objects/StatSO")]
    public class StatSO : ScriptableObject
    {
        public string statName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public int maxLevel;
        public int[] upgradeCosts;
        public int[] upgradeStats;
        public UpgradeEffect effect;

        public string GetLevelLabel(int level) => effect == UpgradeEffect.Ending
            ? (level > 0 ? "엔딩 달성" : "최종 목표")
            : effect == UpgradeEffect.CoilTurns ? $"Lv. {GetStatAtLevel(level)} / 7" : $"Lv. {level} / {maxLevel}";

        public string GetDescriptionAtLevel(int level)
        {
            var current = GetEffectLabel(level);
            if (string.IsNullOrEmpty(current)) return description;
            return level >= maxLevel ? $"{description}\n현재 {current} (최대)"
                : $"{description}\n{current} → {GetEffectLabel(level + 1)}";
        }

        private string GetEffectLabel(int level)
        {
            float value = GetStatAtLevel(level) / 100f;
            return effect switch
            {
                UpgradeEffect.CoilTurns => $"{BasePowerAtLevel(GetStatAtLevel(level)):N0}W",
                UpgradeEffect.CoilThickness => $"높이 {GetStatAtLevel(level)}%",
                UpgradeEffect.CoilMaterial => $"발전 ×{value:0.##}",
                UpgradeEffect.MagnetStrength or UpgradeEffect.MagnetSize => $"발전 ×{value * value:0.##}",
                UpgradeEffect.BonusDuration => $"지속 {300f * value:N0}초",
                UpgradeEffect.AutomaticShake => level == 0 ? "꺼짐" : "켜짐",
                UpgradeEffect.AutomaticShakeSpeed => $"속도 ×{value:0.##}",
                _ => string.Empty
            };
        }

        public static float BasePowerAtLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 7);
            return level * (level + 1) / 2f;
        }
        
        private int _currentStatLevel;

        public int GetCost()
        {
            return GetCostAtLevel(_currentStatLevel);
        }

        public int GetStat()
        {
            return GetStatAtLevel(_currentStatLevel);
        }

        public int GetCostAtLevel(int level)
        {
            if (level < 0 || level >= maxLevel || upgradeCosts == null || level >= upgradeCosts.Length)
                return int.MaxValue;

            return upgradeCosts[level];
        }

        public int GetStatAtLevel(int level)
        {
            if (upgradeStats == null || upgradeStats.Length == 0)
                return 0;

            return upgradeStats[Mathf.Clamp(level, 0, upgradeStats.Length - 1)];
        }

        public void PlusLevel()
        {
            if (_currentStatLevel >= maxLevel) 
                return;
            
            _currentStatLevel++;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(statName))
                statName = name;
        }
    }
}

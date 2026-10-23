using System;
using UnityEngine;

namespace _10_Hyunwoo.Scripts
{
    [CreateAssetMenu(fileName = "StatSO", menuName = "Scriptable Objects/StatSO")]
    public class StatSO : ScriptableObject
    {
        public string statName;
        public int maxLevel;
        public int[] upgradeCosts;
        public int[] upgradeStats;
        
        private int _currentStatLevel;

        public int GetCost()
        {
            if (_currentStatLevel >= maxLevel)
                return int.MaxValue;
            
            return upgradeCosts[_currentStatLevel];
        }

        public int GetStat()
        {
            if (_currentStatLevel > maxLevel)
                return upgradeStats[_currentStatLevel];
            
            return upgradeStats[_currentStatLevel];
        }

        public void PlusLevel()
        {
            if (_currentStatLevel >= maxLevel) 
                return;
            
            _currentStatLevel++;
        }

        private void OnValidate()
        {
            statName = name;
        }
    }
}

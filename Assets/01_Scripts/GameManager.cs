using System;
using System.Collections.Generic;
using _10_Hyunwoo.Scripts;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class GameManager : MonoBehaviour
{
    [Serializable]
    private sealed class UpgradeProgress
    {
        public StatSO stat;
        [Min(0)] public int level;
    }

    public static GameManager Instance { get; private set; }

    [SerializeField, Min(0)] private long currentEnergy = 0;
    [SerializeField] private List<StatSO> upgradeDefinitions = new();
    [SerializeField] private List<UpgradeProgress> upgradeProgress = new();
    [SerializeField] private MagnetDragGenerator generationSource;
    [SerializeField, Min(1)] private long endingCost = 250000;

    public long CurrentEnergy => currentEnergy;
    public double TotalEnergy => currentEnergy + fractionalEnergy;
    public float WattsPerSecond { get; private set; }

    private double fractionalEnergy;
    [SerializeField, Min(1f)] private float baseBonusDuration = 300f;
    private float bonusEndsAt;

    public float BonusDuration => baseBonusDuration * GetUpgradeValue(UpgradeEffect.BonusDuration, 100) / 100f;
    public float BonusSecondsRemaining => Mathf.Max(0f, bonusEndsAt - Time.time);
    public bool IsPowerBonusActive => BonusSecondsRemaining > 0f;
    public float BaseGenerationPowerWatts => StatSO.BasePowerAtLevel(GetUpgradeValue(UpgradeEffect.CoilTurns, 1));
    public float CoilHeightMultiplier => GetUpgradeValue(UpgradeEffect.CoilThickness, 100) / 100f;
    public float GenerationMultiplier
    {
        get
        {
            var material = GetUpgradeValue(UpgradeEffect.CoilMaterial, 100) / 100f;
            var field = GetUpgradeValue(UpgradeEffect.MagnetStrength, 100) / 100f;
            var flux = GetUpgradeValue(UpgradeEffect.MagnetSize, 100) / 100f;
            return material * field * field * flux * flux
                * (IsPowerBonusActive ? 2f : 1f);
        }
    }
    public bool AutomaticShakeEnabled => GetUpgradeValue(UpgradeEffect.AutomaticShake, 0) > 0;
    public float AutomaticSpeedMultiplier => GetUpgradeValue(UpgradeEffect.AutomaticShakeSpeed, 100) / 100f;

    public event Action<long> EnergyChanged;
    public event Action EndingOpened;
    public bool HasReachedEnding { get; private set; }

    public float EndingReferenceReward => generationSource == null ? 0f
        : generationSource.GetMaximumUpgradeReferencePassEnergy(this);
    public long EndingCost => endingCost;
    public event Action<float> GenerationRateChanged;
    public event Action<float> GenerationStrokeCompleted;
    public event Action<StatSO, int> UpgradeLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (generationSource == null) generationSource = FindFirstObjectByType<MagnetDragGenerator>();
        EnsureProgressEntries();
        HasReachedEnding = upgradeProgress.Exists(entry => entry.stat != null
            && entry.stat.effect == UpgradeEffect.Ending && entry.level > 0);
    }

    /// <summary>Credits a settled reward, retaining sub-unit amounts between strokes.</summary>
    public void AddGeneratedEnergy(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        fractionalEnergy += amount;
        var available = Math.Min(Math.Floor(fractionalEnergy), long.MaxValue - currentEnergy);
        var whole = available >= long.MaxValue ? long.MaxValue : (long)available;
        fractionalEnergy -= Math.Floor(fractionalEnergy);
        if (whole > 0)
        {
            currentEnergy += whole;
        }
        EnergyChanged?.Invoke(currentEnergy);
    }

    /// <summary>Publish one completed traversal; WPS stays at this result until the next.</summary>
    public void CompleteGenerationStroke(float amount, float duration)
    {
        if (amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount)
            || duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) return;
        AddGeneratedEnergy(amount);
        WattsPerSecond = amount / duration;
        GenerationRateChanged?.Invoke(WattsPerSecond);
        if (amount > 0f) GenerationStrokeCompleted?.Invoke(amount);
    }

    public int GetUpgradeValue(UpgradeEffect effect, int fallback)
    {
        var stat = upgradeDefinitions.Find(item => item != null && item.effect == effect);
        return stat == null ? fallback : stat.GetStatAtLevel(GetLevel(stat));
    }

    public int GetMaximumUpgradeValue(UpgradeEffect effect, int fallback)
    {
        var stat = upgradeDefinitions.Find(item => item != null && item.effect == effect);
        return stat == null ? fallback : stat.GetStatAtLevel(stat.maxLevel);
    }

    /// <summary>Call after the power-bonus quiz succeeds. Reapplying refreshes, never stacks.</summary>
    public void ActivatePowerBonus() => bonusEndsAt = Time.time + BonusDuration;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetLevel(StatSO stat)
    {
        var entry = FindProgress(stat);
        return entry == null ? 0 : entry.level;
    }

    public bool CanUpgrade(StatSO stat)
    {
        if (stat == null) return false;
        if (stat.effect == UpgradeEffect.Ending && EndingCost <= 0) return false;
        if (stat.effect == UpgradeEffect.AutomaticShakeSpeed && !AutomaticShakeEnabled) return false;
        var level = GetLevel(stat);
        var cost = GetUpgradeCost(stat);
        return level < stat.maxLevel && cost != long.MaxValue && currentEnergy >= cost;
    }

    public long GetUpgradeCost(StatSO stat)
    {
        if (stat == null) return long.MaxValue;
        if (stat.effect == UpgradeEffect.Ending) return EndingCost;
        var cost = stat.GetCostAtLevel(GetLevel(stat));
        return cost == int.MaxValue ? long.MaxValue : cost;
    }

    public void OpenEnding()
    {
        if (HasReachedEnding) EndingOpened?.Invoke();
    }

    public bool TryUpgrade(StatSO stat)
    {
        if (!CanUpgrade(stat)) return false;

        var entry = GetOrCreateProgress(stat);
        var oldBonusDuration = BonusDuration;
        currentEnergy -= GetUpgradeCost(stat);
        entry.level++;
        if (stat.effect == UpgradeEffect.Ending) HasReachedEnding = true;
        if (stat.effect == UpgradeEffect.BonusDuration && IsPowerBonusActive)
            bonusEndsAt += BonusDuration - oldBonusDuration;
        EnergyChanged?.Invoke(currentEnergy);
        UpgradeLevelChanged?.Invoke(stat, entry.level);
        if (stat.effect == UpgradeEffect.Ending) OpenEnding();
        return true;
    }

    private void EnsureProgressEntries()
    {
        foreach (var stat in upgradeDefinitions)
            GetOrCreateProgress(stat);
    }

    private UpgradeProgress GetOrCreateProgress(StatSO stat)
    {
        var entry = FindProgress(stat);
        if (entry != null) return entry;

        entry = new UpgradeProgress { stat = stat, level = 0 };
        upgradeProgress.Add(entry);
        return entry;
    }

    private UpgradeProgress FindProgress(StatSO stat)
    {
        return upgradeProgress.Find(entry => entry.stat == stat);
    }
}

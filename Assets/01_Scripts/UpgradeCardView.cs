using _10_Hyunwoo.Scripts;
using TMPro;
using UnityEngine;

public sealed class UpgradeCardView : MonoBehaviour
{
    [SerializeField] private StatSO stat;
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private UnityEngine.UI.Button purchaseButton;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = GameManager.Instance;
        if (purchaseButton != null) purchaseButton.onClick.AddListener(Purchase);
        Refresh();
    }

    private void OnEnable()
    {
        gameManager ??= GameManager.Instance;
        if (gameManager == null) return;
        gameManager.EnergyChanged += HandleEnergyChanged;
        gameManager.UpgradeLevelChanged += HandleLevelChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.EnergyChanged -= HandleEnergyChanged;
        gameManager.UpgradeLevelChanged -= HandleLevelChanged;
    }

    private void Purchase()
    {
        if (stat != null && stat.effect == UpgradeEffect.Ending && gameManager != null && gameManager.HasReachedEnding)
        {
            gameManager.OpenEnding();
            return;
        }
        if (gameManager != null && gameManager.TryUpgrade(stat)) Refresh();
    }

    private void HandleEnergyChanged(long _) => Refresh();

    private void HandleLevelChanged(StatSO changedStat, int _)
    {
        if (changedStat == stat) Refresh();
    }

    private void Refresh()
    {
        if (stat == null) return;
        var level = gameManager == null ? 0 : gameManager.GetLevel(stat);
        if (iconImage != null) iconImage.sprite = stat.icon;
        if (nameText != null) nameText.text = stat.statName;
        if (levelText != null) levelText.text = stat.GetLevelLabel(level);
        if (descriptionText != null) descriptionText.text = stat.GetDescriptionAtLevel(level);

        var isMax = level >= stat.maxLevel;
        var canReplayEnding = stat.effect == UpgradeEffect.Ending && gameManager != null && gameManager.HasReachedEnding;
        if (costText != null) costText.text = canReplayEnding ? "다시 보기" : isMax ? "MAX"
            : CompactNumber.Format(gameManager != null ? gameManager.GetUpgradeCost(stat) : stat.GetCostAtLevel(level));
        if (purchaseButton != null) purchaseButton.interactable = canReplayEnding || (!isMax && gameManager != null && gameManager.CanUpgrade(stat));
    }
}

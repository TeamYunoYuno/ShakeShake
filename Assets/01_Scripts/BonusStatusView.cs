using TMPro;
using UnityEngine;

public sealed class BonusStatusView : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text durationText;
    private float nextRefresh;

    private void OnEnable()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager != null) gameManager.UpgradeLevelChanged += HandleUpgrade;
        Refresh();
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.UpgradeLevelChanged -= HandleUpgrade;
    }

    private void Update()
    {
        if (Time.time < nextRefresh) return;
        Refresh();
    }

    private void HandleUpgrade(_10_Hyunwoo.Scripts.StatSO _, int level) => Refresh();

    private void Refresh()
    {
        nextRefresh = Time.time + 1f;
        if (gameManager == null || durationText == null) return;
        durationText.text = gameManager.IsPowerBonusActive
            ? $"발전량 ×2 · {Mathf.CeilToInt(gameManager.BonusSecondsRemaining)}초 남음"
            : $"퀴즈 정답 보너스: 발전량 ×2 · {gameManager.BonusDuration:N0}초";
    }
}

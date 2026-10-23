using TMPro;
using UnityEngine;

public sealed class TopStatusBarView : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text wpsText;

    private void OnEnable()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager == null) return;
        gameManager.EnergyChanged += RefreshPower;
        gameManager.GenerationRateChanged += RefreshRate;
        RefreshPower(gameManager.CurrentEnergy);
        RefreshRate(gameManager.WattsPerSecond);
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.EnergyChanged -= RefreshPower;
        gameManager.GenerationRateChanged -= RefreshRate;
    }

    private void RefreshPower(long value)
    {
        if (powerText != null)
            powerText.text = CompactNumber.Format(gameManager.TotalEnergy) + " W";
    }

    private void RefreshRate(float value)
    {
        if (wpsText != null) wpsText.text = CompactNumber.Format(value, 1) + " W/s";
    }
}

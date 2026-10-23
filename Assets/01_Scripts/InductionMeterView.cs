using TMPro;
using UnityEngine;

public sealed class InductionMeterView : MonoBehaviour
{
    [SerializeField] private MagnetDragGenerator generator;
    [SerializeField] private InductionWaveformGraphic waveform;
    [SerializeField] private TMP_Text voltageText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField, Min(0.05f)] private float meterInterval = 0.15f;
    private double voltageSquares;
    private double currentSquares;
    private float sampleTime;
    private float nextRefresh;

    private void OnEnable()
    {
        if (generator != null) generator.CircuitSampled += Sample;
        SetReadout(0f, 0f);
    }

    private void OnDisable()
    {
        if (generator != null) generator.CircuitSampled -= Sample;
        voltageSquares = currentSquares = 0;
        sampleTime = 0f;
    }

    private void Sample(float voltage, float current, float duration)
    {
        if (waveform != null) waveform.AddSample(current, duration);
        voltageSquares += (double)voltage * voltage * duration;
        currentSquares += (double)current * current * duration;
        sampleTime += duration;
        if (sampleTime < meterInterval) return;
        SetReadout(Mathf.Sqrt((float)(voltageSquares / sampleTime)), Mathf.Sqrt((float)(currentSquares / sampleTime)));
        voltageSquares = currentSquares = 0;
        sampleTime = 0f;
    }

    private void SetReadout(float voltage, float current)
    {
        if (voltageText != null) voltageText.text = $"{voltage:0.00} V";
        if (currentText != null) currentText.text = $"{current:0.00} A";
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1f / 30f;
        if (waveform != null)
        {
            waveform.Refresh();
            if (rangeText != null) rangeText.text = $"±{waveform.CurrentRange:0.##} A · 4초";
        }
    }
}

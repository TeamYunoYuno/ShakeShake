using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Vertical magnet movement converts changing coil flux into game currency.</summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class MagnetDragGenerator : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Camera inputCamera;
    [SerializeField] private SpriteRenderer coilBack;
    [SerializeField] private SpriteRenderer coilFront;
    [SerializeField] private InductionArea inductionArea;
    [SerializeField, Min(0.01f)] private float inductionHalfHeight = 1.5f;
    [Tooltip("Global reward scale: 0.1 = one tenth, 0.02 = one fiftieth.")]
    [SerializeField, Min(0f)] private float rewardMultiplier = 0.1f;
    [SerializeField, Min(0.01f)] private float referenceSpeed = 2f;
    [SerializeField, Range(0f, 0.95f)] private float uniformFluxCoreRatio = 0.4f;
    [SerializeField] private float minimumY = 0f;
    [SerializeField] private float maximumY = 2.8f;
    [Tooltip("Equivalent resistive circuit used by the voltage/current meter, in ohms.")]
    [SerializeField, Min(0.01f)] private float circuitResistanceOhms = 10f;

    public float InducedVoltage { get; private set; }
    public float InducedCurrent { get; private set; }
    public float CircuitResistanceOhms => circuitResistanceOhms;
    public event System.Action<float, float, float> CircuitSampled;

    private SpriteRenderer magnet;
    private PolygonCollider2D hitArea;
    private bool dragging;
    private float pointerOffsetY;
    private Vector3 fixedPosition;
    private bool automaticMovingUp = true;
    private readonly InductionStrokeTracker strokeTracker = new();
    private Vector3 initialBackScale;
    private Vector3 initialFrontScale;
    private Vector2 initialAreaSize;
    private BoxCollider2D areaCollider;

    private void Awake()
    {
        magnet = GetComponent<SpriteRenderer>();
        hitArea = GetComponent<PolygonCollider2D>();
        hitArea.isTrigger = true;
        fixedPosition = transform.position;
        fixedPosition.y = Mathf.Clamp(fixedPosition.y, minimumY, maximumY);
        transform.position = fixedPosition;
        if (inputCamera == null) inputCamera = Camera.main;
        if (gameManager == null) gameManager = GameManager.Instance;
        if (coilBack != null) initialBackScale = coilBack.transform.localScale;
        if (coilFront != null) initialFrontScale = coilFront.transform.localScale;
        if (inductionArea != null)
        {
            areaCollider = inductionArea.GetComponent<BoxCollider2D>();
            initialAreaSize = areaCollider.size;
        }
        if (coilBack != null && coilFront != null)
        {
            magnet.sortingLayerID = coilBack.sortingLayerID;
            magnet.sortingOrder = coilBack.sortingOrder + 1;
            coilFront.sortingLayerID = coilBack.sortingLayerID;
            coilFront.sortingOrder = magnet.sortingOrder + 1;
        }
    }

    private void OnEnable()
    {
        if (gameManager != null) gameManager.UpgradeLevelChanged += HandleUpgrade;
        RefreshCoilHeight();
    }

    private void HandleUpgrade(_10_Hyunwoo.Scripts.StatSO stat, int level)
    {
        if (stat.effect == _10_Hyunwoo.Scripts.UpgradeEffect.CoilThickness) RefreshCoilHeight();
    }

    private void RefreshCoilHeight()
    {
        if (gameManager == null) return;
        var ratio = gameManager.CoilHeightMultiplier;
        if (coilBack != null) coilBack.transform.localScale = new Vector3(initialBackScale.x, initialBackScale.y * ratio, initialBackScale.z);
        if (coilFront != null) coilFront.transform.localScale = new Vector3(initialFrontScale.x, initialFrontScale.y * ratio, initialFrontScale.z);
        if (areaCollider != null) areaCollider.size = new Vector2(initialAreaSize.x, initialAreaSize.y * ratio);
        strokeTracker.Reset();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (inputCamera == null || coilBack == null || Time.timeScale == 0f)
        {
            dragging = false;
            return;
        }
        if (mouse == null)
        {
            dragging = false;
            UpdateAutomaticMovement();
            return;
        }

        var ray = inputCamera.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, fixedPosition.z));
        if (!plane.Raycast(ray, out var distance)) return;
        var pointer = ray.GetPoint(distance);

        if (mouse.leftButton.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
            hitArea.OverlapPoint(pointer))
        {
            dragging = true;
            pointerOffsetY = transform.position.y - pointer.y;
        }

        if (!mouse.leftButton.isPressed) dragging = false;
        if (dragging) MoveToY(pointer.y + pointerOffsetY);
        else UpdateAutomaticMovement();
    }

    private void UpdateAutomaticMovement()
    {
        if (gameManager == null || !gameManager.AutomaticShakeEnabled)
        {
            MoveToY(transform.position.y);
            return;
        }
        var target = automaticMovingUp ? maximumY : minimumY;
        var speed = referenceSpeed * gameManager.AutomaticSpeedMultiplier;
        MoveToY(Mathf.MoveTowards(transform.position.y, target, speed * Time.deltaTime));
        if (Mathf.Abs(transform.position.y - target) < 0.001f) automaticMovingUp = !automaticMovingUp;
    }

    public void MoveToY(float targetY)
    {
        var previous = transform.position.y;
        var next = Mathf.Clamp(targetY, minimumY, maximumY);
        transform.position = new Vector3(fixedPosition.x, next, fixedPosition.z);
        if (coilBack == null || gameManager == null) return;
        var bounds = inductionArea != null ? inductionArea.Bounds
            : new Bounds(coilBack.bounds.center, new Vector3(coilBack.bounds.size.x, inductionHalfHeight * 2f, 0f));
        if (fixedPosition.x < bounds.min.x || fixedPosition.x > bounds.max.x)
        {
            strokeTracker.Reset();
            EmitCircuitSample(0f, Time.deltaTime);
            return;
        }
        PublishCircuitSamples(previous, next, bounds, Time.deltaTime);
        var clippedFrom = Mathf.Clamp(previous, bounds.min.y, bounds.max.y);
        var clippedTo = Mathf.Clamp(next, bounds.min.y, bounds.max.y);
        var distance = Mathf.Abs(next - previous);
        var insideTime = distance > 0.00001f
            ? Time.deltaTime * Mathf.Abs(clippedTo - clippedFrom) / distance : Time.deltaTime;
        var generated = CalculateGeneratedEnergy(clippedFrom, clippedTo,
            bounds.center.y, bounds.extents.y, uniformFluxCoreRatio,
            insideTime, referenceSpeed, gameManager.BaseGenerationPowerWatts)
            * gameManager.GenerationMultiplier * rewardMultiplier;
        var bottomEndpoint = Mathf.Max(bounds.min.y, minimumY);
        var topEndpoint = Mathf.Min(bounds.max.y, maximumY);
        if (strokeTracker.Advance(previous, next, Time.deltaTime, bottomEndpoint, topEndpoint,
                generated, out var energy, out var duration))
            gameManager.CompleteGenerationStroke(energy, duration);
    }

    private void PublishCircuitSamples(float fromY, float toY, Bounds bounds, float deltaTime)
    {
        var distance = Mathf.Abs(toY - fromY);
        if (distance < 0.00001f || deltaTime <= 0f)
        {
            EmitCircuitSample(0f, deltaTime);
            return;
        }
        // Split at flux-profile boundaries so even a one-frame full crossing
        // produces both electrical polarities instead of cancelling to zero.
        var core = bounds.extents.y * uniformFluxCoreRatio;
        var cursor = fromY;
        for (var i = 0; i < 5; i++)
        {
            var index = toY > fromY ? i : 4 - i;
            var boundary = index switch
            {
                0 => bounds.min.y,
                1 => bounds.center.y - core,
                2 => bounds.center.y,
                3 => bounds.center.y + core,
                _ => bounds.max.y
            };
            if (boundary <= Mathf.Min(fromY, toY) || boundary >= Mathf.Max(fromY, toY)) continue;
            PublishCircuitSegment(cursor, boundary, deltaTime * Mathf.Abs(boundary - cursor) / distance, bounds);
            cursor = boundary;
        }
        PublishCircuitSegment(cursor, toY, deltaTime * Mathf.Abs(toY - cursor) / distance, bounds);
    }

    private void PublishCircuitSegment(float fromY, float toY, float duration, Bounds bounds)
    {
        var voltage = CalculateInducedVoltage(fromY, toY, bounds.center.y, bounds.extents.y,
            uniformFluxCoreRatio, duration, referenceSpeed,
            gameManager.BaseGenerationPowerWatts * gameManager.GenerationMultiplier, circuitResistanceOhms);
        EmitCircuitSample(voltage, duration);
    }

    private void EmitCircuitSample(float voltage, float duration)
    {
        InducedVoltage = voltage;
        InducedCurrent = voltage / Mathf.Max(0.01f, circuitResistanceOhms);
        CircuitSampled?.Invoke(InducedVoltage, InducedCurrent, Mathf.Max(0f, duration));
    }

    /// <summary>One complete traversal at reference speed with every permanent upgrade maxed.</summary>
    public float GetMaximumUpgradeReferencePassEnergy(GameManager manager)
    {
        var baseHeight = inductionArea != null ? inductionArea.Bounds.size.y : inductionHalfHeight * 2f;
        if (inductionArea != null && initialAreaSize.y > 0f)
            baseHeight = initialAreaSize.y * Mathf.Abs(inductionArea.transform.lossyScale.y);
        var halfHeight = baseHeight * manager.GetMaximumUpgradeValue(_10_Hyunwoo.Scripts.UpgradeEffect.CoilThickness, 100) / 200f;
        var nominalPower = _10_Hyunwoo.Scripts.StatSO.BasePowerAtLevel(
            manager.GetMaximumUpgradeValue(_10_Hyunwoo.Scripts.UpgradeEffect.CoilTurns, 1));
        var material = manager.GetMaximumUpgradeValue(_10_Hyunwoo.Scripts.UpgradeEffect.CoilMaterial, 100) / 100f;
        var field = manager.GetMaximumUpgradeValue(_10_Hyunwoo.Scripts.UpgradeEffect.MagnetStrength, 100) / 100f;
        var flux = manager.GetMaximumUpgradeValue(_10_Hyunwoo.Scripts.UpgradeEffect.MagnetSize, 100) / 100f;
        return CalculateGeneratedEnergy(-halfHeight, halfHeight, 0f, halfHeight, uniformFluxCoreRatio,
            halfHeight * 2f / referenceSpeed, referenceSpeed, nominalPower)
            * material * field * field * flux * flux * rewardMultiplier;
    }

    // Equivalent signed RMS voltage over one same-polarity segment. Reward
    // balancing is deliberately excluded from this electrical measurement.
    public static float CalculateInducedVoltage(float fromY, float toY, float centerY,
        float halfHeight, float coreRatio, float duration, float referenceSpeed,
        float nominalPower, float resistanceOhms)
    {
        if (duration <= 0f || halfHeight <= 0f || resistanceOhms <= 0f) return 0f;
        float Flux(float y)
        {
            var core = halfHeight * Mathf.Clamp(coreRatio, 0f, 0.95f);
            var t = Mathf.Clamp01((Mathf.Abs(y - centerY) - core) / (halfHeight - core));
            return 1f - t * t * (3f - 2f * t);
        }
        var change = Flux(toY) - Flux(fromY);
        if (Mathf.Abs(change) < 0.000001f) return 0f;
        var energy = CalculateGeneratedEnergy(fromY, toY, centerY, halfHeight, coreRatio,
            duration, referenceSpeed, nominalPower);
        return -Mathf.Sign(change) * Mathf.Sqrt(Mathf.Max(0f, energy / duration * resistanceOhms));
    }

    // Faraday: emf = -N dFlux/dt; resistive power = emf^2/R. Flux is flat in the
    // core and follows smoothstep at the entrances. Integrating its squared
    // gradient prevents cancellation and keeps fast passes frame-rate independent.
    // Positions and field strength are normalized for gameplay, not measured SI inputs.
    public static float CalculateGeneratedEnergy(float fromY, float toY, float centerY,
        float halfHeight, float coreRatio, float deltaTime, float referenceSpeed,
        float baseWatts = 1f)
    {
        var distance = Mathf.Abs(toY - fromY);
        if (deltaTime <= 0f || distance < 0.00001f || halfHeight <= 0f || referenceSpeed <= 0f) return 0f;
        var low = Mathf.Min(fromY, toY) - centerY;
        var high = Mathf.Max(fromY, toY) - centerY;
        var core = halfHeight * Mathf.Clamp(coreRatio, 0f, 0.95f);
        var edgeWidth = halfHeight - core;
        var integral = EdgeIntegral(low, high, core, edgeWidth)
            + EdgeIntegral(-high, -low, core, edgeWidth);
        var relativeSpeed = distance / deltaTime / referenceSpeed;
        // Integrating v^2 power over a fixed path gives energy proportional to v.
        // A power cap here would make faster crossings pay LESS once capped.
        return Mathf.Max(0f, baseWatts) * relativeSpeed * (integral / referenceSpeed);
    }

    private static float EdgeIntegral(float low, float high, float core, float width)
    {
        var a = Mathf.Clamp01((low - core) / width);
        var b = Mathf.Clamp01((high - core) / width);
        return width * (GradientPrimitive(b) - GradientPrimitive(a));
    }

    // Integral of (4t(1-t))^2, the normalized squared smoothstep derivative.
    private static float GradientPrimitive(float t) =>
        16f * (t * t * t / 3f - t * t * t * t / 2f + t * t * t * t * t / 5f);

    private void OnDisable()
    {
        if (gameManager != null) gameManager.UpgradeLevelChanged -= HandleUpgrade;
        dragging = false;
        strokeTracker.Reset();
        EmitCircuitSample(0f, 0f);
    }
    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) dragging = false; }
}

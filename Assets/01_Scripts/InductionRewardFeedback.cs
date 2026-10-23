using TMPro;
using UnityEngine;

/// <summary>Reuses a small pool of non-interactive floating reward labels.</summary>
public sealed class InductionRewardFeedback : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform coil;
    [SerializeField] private BoxCollider2D popupArea;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = new Color32(255, 232, 100, 255);
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(0.1f)] private float lifetime = 1.1f;
    [SerializeField] private float riseDistance = 150f;
    [SerializeField] private float fontSize = 76f;

    private sealed class Popup
    {
        public TextMeshProUGUI text;
        public RectTransform rect;
        public Vector2 origin;
        public float age;
        public bool active;
    }

    private readonly Popup[] pool = new Popup[8];
    private RectTransform popupRoot;
    private int nextPopup;

    private void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (worldCamera == null) worldCamera = Camera.main;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var root = new GameObject("GenerationPopups", typeof(RectTransform));
        popupRoot = root.GetComponent<RectTransform>();
        popupRoot.SetParent(canvas.transform, false);
        popupRoot.anchorMin = Vector2.zero;
        popupRoot.anchorMax = Vector2.one;
        popupRoot.offsetMin = popupRoot.offsetMax = Vector2.zero;

        for (var i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("GenerationReward", typeof(RectTransform), typeof(TextMeshProUGUI));
            var text = go.GetComponent<TextMeshProUGUI>();
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(popupRoot, false);
            rect.sizeDelta = new Vector2(600f, 120f);
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            go.SetActive(false);
            pool[i] = new Popup { text = text, rect = rect };
        }
    }

    private void OnEnable()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager != null) gameManager.GenerationStrokeCompleted += Show;
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.GenerationStrokeCompleted -= Show;
        foreach (var popup in pool)
        {
            if (popup == null) continue;
            popup.active = false;
            popup.text.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (popupRoot != null) Destroy(popupRoot.gameObject);
    }

    private void Show(float amount)
    {
        if (popupRoot == null || worldCamera == null || (popupArea == null && coil == null)) return;
        var spawnPoint = coil != null ? coil.position + worldOffset : Vector3.zero;
        if (popupArea != null)
        {
            var half = popupArea.size * 0.5f;
            var local = popupArea.offset + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            spawnPoint = popupArea.transform.TransformPoint(local);
        }
        var screen = worldCamera.WorldToScreenPoint(spawnPoint);
        if (screen.z <= 0f) return;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRoot, screen, uiCamera, out var position)) return;
        var popup = pool[nextPopup];
        nextPopup = (nextPopup + 1) % pool.Length;
        popup.origin = position;
        popup.age = 0f;
        popup.active = true;
        popup.text.text = "+" + CompactNumber.Format(amount) + "W";
        popup.text.color = textColor;
        popup.rect.anchoredPosition = popup.origin;
        popup.rect.localScale = Vector3.one * 0.85f;
        popup.text.gameObject.SetActive(true);
    }

    private void Update()
    {
        foreach (var popup in pool)
        {
            if (popup == null || !popup.active) continue;
            popup.age += Time.deltaTime;
            var t = Mathf.Clamp01(popup.age / lifetime);
            popup.rect.anchoredPosition = popup.origin + Vector2.up * (riseDistance * (1f - (1f - t) * (1f - t)));
            popup.rect.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.85f, 1.08f, t / 0.15f) : Mathf.Lerp(1.08f, 1f, (t - 0.15f) / 0.85f));
            var color = textColor;
            color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t));
            popup.text.color = color;
            if (t < 1f) continue;
            popup.active = false;
            popup.text.gameObject.SetActive(false);
        }
    }
}

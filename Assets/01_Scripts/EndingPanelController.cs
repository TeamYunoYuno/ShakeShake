using UnityEngine;

public sealed class EndingPanelController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject endingOverlay;
    [SerializeField] private UnityEngine.UI.Button continueButton;

    private void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (continueButton != null) continueButton.onClick.AddListener(Close);
        Close();
    }

    private void OnEnable()
    {
        if (gameManager != null) gameManager.EndingOpened += Show;
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.EndingOpened -= Show;
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Close);
    }

    public void Show()
    {
        if (endingOverlay == null) return;
        endingOverlay.transform.SetAsLastSibling();
        endingOverlay.SetActive(true);
    }

    public void Close()
    {
        if (endingOverlay != null) endingOverlay.SetActive(false);
    }
}

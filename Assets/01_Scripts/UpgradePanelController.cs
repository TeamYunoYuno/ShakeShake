using UnityEngine;

public sealed class UpgradePanelController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button coilTabButton;
    [SerializeField] private UnityEngine.UI.Button magnetTabButton;
    [SerializeField] private UnityEngine.UI.Button automationTabButton;
    [SerializeField] private UnityEngine.UI.Image coilTabBackground;
    [SerializeField] private UnityEngine.UI.Image magnetTabBackground;
    [SerializeField] private UnityEngine.UI.Image automationTabBackground;
    [SerializeField] private GameObject coilContent;
    [SerializeField] private GameObject magnetContent;
    [SerializeField] private GameObject automationContent;

    private static readonly Color SelectedColor = new Color32(255, 153, 52, 255);
    private static readonly Color UnselectedColor = new Color32(226, 231, 241, 255);

    private void Awake()
    {
        if (coilTabButton != null) coilTabButton.onClick.AddListener(ShowCoil);
        if (magnetTabButton != null) magnetTabButton.onClick.AddListener(ShowMagnet);
        if (automationTabButton != null) automationTabButton.onClick.AddListener(ShowAutomation);
        ShowCoil();
    }

    public void ShowCoil()
    {
        Show(coilContent);
    }

    public void ShowMagnet()
    {
        Show(magnetContent);
    }

    public void ShowAutomation() => Show(automationContent);

    private void Show(GameObject selected)
    {
        if (coilContent != null) coilContent.SetActive(coilContent == selected);
        if (magnetContent != null) magnetContent.SetActive(magnetContent == selected);
        if (automationContent != null) automationContent.SetActive(automationContent == selected);
        if (coilTabBackground != null) coilTabBackground.color = coilContent == selected ? SelectedColor : UnselectedColor;
        if (magnetTabBackground != null) magnetTabBackground.color = magnetContent == selected ? SelectedColor : UnselectedColor;
        if (automationTabBackground != null) automationTabBackground.color = automationContent == selected ? SelectedColor : UnselectedColor;
    }
}

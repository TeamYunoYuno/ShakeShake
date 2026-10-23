using TMPro;
using UnityEngine;

public sealed class MainTabsController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button upgradeButton;
    [SerializeField] private UnityEngine.UI.Button bonusButton;
    [SerializeField] private UnityEngine.UI.Button educationButton;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject bonusPanel;
    [SerializeField] private GameObject educationPanel;

    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(() => Show(upgradePanel));
        if (bonusButton != null) bonusButton.onClick.AddListener(() => Show(bonusPanel));
        if (educationButton != null) educationButton.onClick.AddListener(() => Show(educationPanel));
        HideAll();
    }

    public void HideAll()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (bonusPanel != null) bonusPanel.SetActive(false);
        if (educationPanel != null) educationPanel.SetActive(false);
    }

    private void Show(GameObject target)
    {
        HideAll();
        if (target == null) return;

        target.SetActive(true);
        Canvas.ForceUpdateCanvases();
        foreach (var text in target.GetComponentsInChildren<TMP_Text>(true))
            text.ForceMeshUpdate(true, true);
    }
}

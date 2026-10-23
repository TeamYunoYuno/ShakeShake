using System;
using TMPro;
using UnityEngine;

public sealed class EducationPanelController : MonoBehaviour
{
    [Serializable]
    public sealed class Page
    {
        public string title;
        [TextArea(4, 16)] public string content;
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private UnityEngine.UI.Button previousButton;
    [SerializeField] private UnityEngine.UI.Button nextButton;
    [SerializeField] private Page[] pages = Array.Empty<Page>();
    [SerializeField] private LearningContent learningContent;
    private Page[] Pages => learningContent != null ? learningContent.educationPages : pages;
    private int pageIndex;

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(Previous);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(Previous);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
    }

    private void OnEnable() => Refresh();
    public void Previous() { if (pageIndex > 0) { pageIndex--; Refresh(); } }
    public void Next() { if (pageIndex + 1 < Pages.Length) { pageIndex++; Refresh(); } }

    private void Refresh()
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, Pages.Length - 1));
        var page = Pages.Length > 0 ? Pages[pageIndex] : null;
        if (titleText != null) titleText.text = page?.title ?? string.Empty;
        if (contentText != null) contentText.text = page?.content ?? string.Empty;
        if (previousButton != null) previousButton.gameObject.SetActive(pageIndex > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(pageIndex + 1 < Pages.Length);
    }
}

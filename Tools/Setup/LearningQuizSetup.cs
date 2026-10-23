using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LearningQuizSetup
{
    private static TMP_Text template;
    private static readonly Color Ink = new Color32(46, 63, 87, 255);
    private static readonly Color Cream = new Color32(250, 247, 242, 255);

    private static Transform Find(string name) => UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        .GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).First(t => t.name == name);

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static GameObject Box(Transform parent, string name, Color color, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, x, y, w, h);
        go.GetComponent<Image>().color = color;
        var rounded = go.AddComponent<Nobi.UiRoundedCorners.ImageWithRoundedCorners>();
        rounded.radius = 18f;
        return go;
    }

    private static TMP_Text Text(Transform parent, string name, string value, float size, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, x, y, w, h);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = template.font;
        text.fontSharedMaterial = template.fontSharedMaterial;
        text.fontSize = size;
        text.color = Ink;
        text.text = value;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static Button Button(Transform parent, string name, string label, float x, float y, float w, float h)
    {
        var go = Box(parent, name, new Color32(225, 233, 245, 255), x, y, w, h);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var text = Text(go.transform, "Label", label, 34, 0, 0, w, h);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static LearningContent Import(string sourcePath)
    {
        var source = File.ReadAllText(sourcePath).Replace("\r\n", "\n");
        var pages = Regex.Matches(source, @"(?ms)^### 1-\d+\. (.+?)\n\n(.*?)(?=^### |^---)")
            .Cast<Match>().Select(m => new EducationPanelController.Page {
                title = m.Groups[1].Value.Trim(),
                content = m.Groups[2].Value.Trim().Replace("와전류(맴돌이 전류가", "와전류(맴돌이 전류)가")
            }).ToArray();
        var answers = Regex.Matches(source, @"\|\s*(\d+)\s*\|\s*([OX])\s*(?=\|)")
            .Cast<Match>().ToDictionary(m => int.Parse(m.Groups[1].Value), m => m.Groups[2].Value == "O");
        var corrections = Regex.Matches(source, @"(?m)^(\d+)번: (.+)$")
            .Cast<Match>().ToDictionary(m => int.Parse(m.Groups[1].Value), m => m.Groups[2].Value.Trim());
        var questions = Regex.Matches(source, @"(?m)^(\d+)\. (.+?)\s*\( O / X \)")
            .Cast<Match>().Select(m => {
                int number = int.Parse(m.Groups[1].Value);
                int pageIndex = number <= 6 ? 0 : number <= 8 ? 1 : number <= 12 ? 2 : number <= 16 ? 3 : number <= 19 ? 4 : number <= 21 ? 5 : 6;
                return new LearningContent.Question {
                    prompt = m.Groups[2].Value.Trim(), answer = answers[number],
                    explanation = corrections.TryGetValue(number, out var explanation) ? explanation : pages[pageIndex].content
                };
            }).ToArray();
        if (pages.Length != 7 || questions.Length != 22 || answers.Count != 22)
            throw new InvalidOperationException("Expected 7 topics and 22 questions/answers.");
        // Keep quiz explanations concise; the complete discussion is in the education pages.
        foreach (var q in questions)
            if (q.explanation.Length > 220) q.explanation = q.explanation.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
        var assetPath = "Assets/06_Data/ScriptableObjects/ElectromagnetismLearning.asset";
        var content = AssetDatabase.LoadAssetAtPath<LearningContent>(assetPath);
        if (content == null) { content = ScriptableObject.CreateInstance<LearningContent>(); AssetDatabase.CreateAsset(content, assetPath); }
        content.educationPages = pages;
        content.questions = questions;
        EditorUtility.SetDirty(content);
        return content;
    }

    public static string Build(string sourcePath)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before setup.");
        if (Find("Canvas").Find("QuizBonusSystem") != null) throw new InvalidOperationException("Quiz already exists.");
        var content = Import(sourcePath);
        var canvas = Find("Canvas");
        var bonus = Find("BonusPanel");
        bonus.Find("BonusList/EnergyReward/Info/Subtitle").GetComponent<TMP_Text>().text = "정답을 맞히면 전력 1,000 W를 받습니다.";
        template = bonus.Find("Header/Title").GetComponent<TMP_Text>();
        var education = Find("EducationPanel").GetComponent<EducationPanelController>();
        var edu = new SerializedObject(education);
        edu.FindProperty("learningContent").objectReferenceValue = content;
        edu.ApplyModifiedPropertiesWithoutUndo();
        var paragraph = Find("EducationPanel").Find("ConceptContent/ConceptParagraph").GetComponent<TMP_Text>();
        paragraph.enableAutoSizing = true;
        paragraph.fontSizeMin = 22; paragraph.fontSizeMax = 30;
        // Seed the visible first page as well as the runtime source.
        Find("EducationPanel").Find("ConceptContent/ConceptTitle").GetComponent<TMP_Text>().text = content.educationPages[0].title;
        paragraph.text = content.educationPages[0].content;

        var system = new GameObject("QuizBonusSystem", typeof(RectTransform));
        system.transform.SetParent(canvas, false);
        var controller = system.AddComponent<QuizBonusController>();
        var modal = Box(canvas, "QuizModal", new Color(0, 0, 0, .45f), 0, 0, 0, 0);
        var overlay = (RectTransform)modal.transform;
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        var dialog = Box(modal.transform, "QuizDialog", Cream, 0, 0, 650, 1240);
        var dialogRect = (RectTransform)dialog.transform;
        var sourceRect = (RectTransform)bonus;
        dialogRect.anchorMin = sourceRect.anchorMin; dialogRect.anchorMax = sourceRect.anchorMax;
        dialogRect.pivot = sourceRect.pivot; dialogRect.anchoredPosition = sourceRect.anchoredPosition;
        Text(dialog.transform, "Title", "O / X 퀴즈", 42, 32, 32, 480, 70);
        var close = Button(dialog.transform, "CloseButton", "×", 542, 26, 80, 80);
        var reward = Text(dialog.transform, "Reward", "정답 보너스", 28, 32, 130, 586, 90);
        var question = Text(dialog.transform, "Question", "문제", 36, 32, 248, 586, 220);
        question.enableAutoSizing = true; question.fontSizeMin = 28; question.fontSizeMax = 36;
        var yes = Button(dialog.transform, "TrueButton", "O", 32, 490, 278, 110);
        var no = Button(dialog.transform, "FalseButton", "X", 340, 490, 278, 110);
        var feedback = Text(dialog.transform, "Feedback", "맞으면 O, 틀리면 X를 선택하세요.", 29, 32, 640, 586, 340);
        feedback.enableAutoSizing = true; feedback.fontSizeMin = 22; feedback.fontSizeMax = 29;
        var action = Button(dialog.transform, "ActionButton", "다른 문제 풀기", 32, 1070, 586, 100);
        action.gameObject.SetActive(false);
        var countdown = Box(canvas, "PowerBonusCountdown", new Color32(36, 54, 82, 245), 0, 0, 210, 115);
        var countdownRect = (RectTransform)countdown.transform;
        countdownRect.anchorMin = countdownRect.anchorMax = new Vector2(1, 1);
        countdownRect.pivot = new Vector2(1, 1);
        countdownRect.anchoredPosition = new Vector2(-18, -505);
        countdown.GetComponent<Image>().raycastTarget = false;
        var countdownText = Text(countdown.transform, "RemainingTime", "발전량 ×2\n05:00", 31, 8, 8, 194, 99);
        countdownText.color = Color.white; countdownText.alignment = TextAlignmentOptions.Center;
        var so = new SerializedObject(controller);
        Action<string, Object> assign = (key, value) => so.FindProperty(key).objectReferenceValue = value;
        assign("content", content); assign("gameManager", Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include));
        assign("modal", modal); assign("rewardText", reward); assign("questionText", question); assign("feedbackText", feedback);
        assign("trueButton", yes); assign("falseButton", no); assign("actionButton", action); assign("closeButton", close);
        assign("actionText", action.GetComponentInChildren<TMP_Text>());
        assign("powerQuizButton", bonus.Find("BonusList/PowerBoost/QuizButton").GetComponent<Button>());
        assign("energyQuizButton", bonus.Find("BonusList/EnergyReward/QuizButton").GetComponent<Button>());
        assign("countdownRoot", countdownRect); assign("countdownText", countdownText);
        so.ApplyModifiedPropertiesWithoutUndo();
        modal.SetActive(false); countdown.SetActive(false);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Created SO: 7 education topics, 22 O/X questions and explanations. Two bonus quiz entry buttons and top-right countdown connected.";
    }
}

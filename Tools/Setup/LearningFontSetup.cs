using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class LearningFontSetup
{
    private static void SaveFont(TMP_FontAsset font, string path)
    {
        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(font))) return;
        AssetDatabase.CreateAsset(font, path);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures.Where(t => t != null)) AssetDatabase.AddObjectToAsset(texture, font);
        EditorUtility.SetDirty(font);
    }

    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_UI/Fonts/LearningKorean.asset") != null)
            return "Learning font already exists; new characters use its dynamic fallback.";
        var source = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_UI/Fonts/TopStatusKorean.asset").sourceFontFile;
        var content = AssetDatabase.LoadAssetAtPath<LearningContent>("Assets/06_Data/ScriptableObjects/ElectromagnetismLearning.asset");
        var text = string.Join("\n", content.educationPages.Select(p => p.title + p.content)
            .Concat(content.questions.Select(q => q.prompt + q.explanation))) +
            "O X 퀴즈 정답 보너스 발전량 전력 맞으면 틀리면 선택하세요 아쉬워요 입니다 받았습니다 다른 문제 풀기 닫기 ×2 · 0123456789: W";
        text += new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());
        var font = TMP_FontAsset.CreateFontAsset(source, 40, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = "LearningKorean";
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters(text, out string missing);
        if (!string.IsNullOrEmpty(missing.Trim('\r', '\n', '\t'))) throw new System.Exception("Missing font characters: " + new string(missing.Distinct().ToArray()));
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        SaveFont(font, "Assets/05_UI/Fonts/LearningKorean.asset");
        // Editable future content can use a bounded dynamic fallback.
        var fallback = TMP_FontAsset.CreateFontAsset(source, 40, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fallback.name = "LearningKoreanFallback";
        var serialized = new SerializedObject(fallback);
        var clear = serialized.FindProperty("m_ClearDynamicDataOnBuild");
        if (clear != null) { clear.boolValue = true; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        SaveFont(fallback, "Assets/05_UI/Fonts/LearningKoreanFallback.asset");
        font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var t in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
        {
            if (t.GetComponentsInParent<Transform>(true).Any(p => p.name == "QuizModal" || p.name == "EducationPanel" || p.name == "PowerBonusCountdown"))
            {
                t.font = font; t.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(t);
            }
        }
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Baked " + font.characterTable.Count + " glyphs into " + font.atlasTextures.Length + " atlas(es); education/quiz/countdown font assigned with dynamic fallback.";
    }
}

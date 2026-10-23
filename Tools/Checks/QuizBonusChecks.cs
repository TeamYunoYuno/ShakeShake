using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class QuizBonusChecks
{
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Tick(object target) => target.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    public static string Run()
    {
        Check(Application.isPlaying, "Run in play mode.");
        var quiz = UnityEngine.Object.FindFirstObjectByType<QuizBonusController>();
        var manager = GameManager.Instance;
        var content = (LearningContent)Get(quiz, "content");
        Check(content.questions.Length == 22 && content.educationPages.Length == 7, "Content counts");
        int[] falseNumbers = { 3, 5, 8, 12, 15, 21 };
        for (int i = 0; i < 22; i++)
            Check(content.questions[i].answer == !falseNumbers.Contains(i + 1), "Answer table: " + (i + 1));
        var energy = manager.TotalEnergy;
        ((Button)Get(quiz, "energyQuizButton")).onClick.Invoke();
        Set(quiz, "questionIndex", 0);
        quiz.AnswerFalse();
        Check(manager.TotalEnergy == energy, "Wrong answer must not grant reward");
        ((Button)Get(quiz, "actionButton")).onClick.Invoke();
        Check(!(bool)Get(quiz, "answered"), "Wrong answer retry");
        Set(quiz, "questionIndex", 0);
        quiz.AnswerTrue();
        Check(manager.TotalEnergy == energy + 1000, "Correct answer reward");
        quiz.AnswerTrue(); quiz.AnswerFalse();
        Check(manager.TotalEnergy == energy + 1000, "Duplicate answer protection");
        quiz.Close();
        ((Button)Get(quiz, "powerQuizButton")).onClick.Invoke();
        Set(quiz, "questionIndex", 0);
        float before = manager.GenerationMultiplier;
        quiz.AnswerTrue();
        Check(manager.IsPowerBonusActive && Mathf.Approximately(manager.GenerationMultiplier, before * 2), "Power multiplier");
        Check(Mathf.Abs(manager.BonusSecondsRemaining - manager.BonusDuration) < 1, "Bonus duration");
        Tick(quiz);
        Check(((RectTransform)Get(quiz, "countdownRoot")).gameObject.activeSelf, "HUD appears");
        manager.ActivatePowerBonus();
        Check(Mathf.Approximately(manager.GenerationMultiplier, before * 2), "Bonus cannot stack");
        Set(manager, "bonusEndsAt", Time.time - 1);
        Tick(quiz);
        Check(!manager.IsPowerBonusActive && Mathf.Approximately(manager.GenerationMultiplier, before), "Bonus expiry");
        Check(!((RectTransform)Get(quiz, "countdownRoot")).gameObject.activeSelf, "Expired HUD hidden");
        quiz.Close();
        ((Button)Get(quiz, "energyQuizButton")).onClick.Invoke();
        quiz.Close();
        Check(manager.TotalEnergy == energy + 1000, "Close without answer grants nothing");
        manager.ActivatePowerBonus(); Tick(quiz);
        quiz.OpenPowerQuiz();
        return "PASS: 7 topics, all 22 answer keys, wrong/retry, both bonus buttons, one-time reward, multiplier, refresh without stacking, expiry, HUD visibility, cancel without reward.";
    }
}

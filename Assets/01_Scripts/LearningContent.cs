using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Learning/Learning Content")]
public sealed class LearningContent : ScriptableObject
{
    [Serializable]
    public sealed class Question
    {
        [TextArea(2, 5)] public string prompt;
        public bool answer;
        [TextArea(2, 8)] public string explanation;
    }
    public EducationPanelController.Page[] educationPages = Array.Empty<EducationPanelController.Page>();
    public Question[] questions = Array.Empty<Question>();
}

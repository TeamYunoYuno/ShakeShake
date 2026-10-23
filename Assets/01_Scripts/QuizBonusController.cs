using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuizBonusController : MonoBehaviour
{
    [SerializeField] private LearningContent content;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject modal;
    [SerializeField] private TMP_Text rewardText, questionText, feedbackText;
    [SerializeField] private Button trueButton, falseButton, actionButton, closeButton;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button powerQuizButton, energyQuizButton;
    [SerializeField] private RectTransform countdownRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField, Min(0)] private float energyReward = 1000f;
    private bool powerReward, answered, open;
    private bool rewardGranted;
    private int questionIndex = -1;
    private int lastCountdown = -1;

    private void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        powerQuizButton.onClick.AddListener(OpenPowerQuiz);
        energyQuizButton.onClick.AddListener(OpenEnergyQuiz);
        trueButton.onClick.AddListener(AnswerTrue);
        falseButton.onClick.AddListener(AnswerFalse);
        actionButton.onClick.AddListener(Continue);
        closeButton.onClick.AddListener(Close);
        modal.SetActive(false);
    }

    public void OpenPowerQuiz() => Begin(true);
    public void OpenEnergyQuiz() => Begin(false);
    private void Begin(bool power)
    {
        if (open || content == null || content.questions.Length == 0 || gameManager == null) return;
        powerReward = power;
        open = true;
        modal.SetActive(true);
        modal.transform.SetAsLastSibling();
        rewardText.text = power ? $"정답 보상: 발전량 2배 · {gameManager.BonusDuration:N0}초" : $"정답 보상: 전력 {energyReward:N0} W";
        NewQuestion();
    }

    private void NewQuestion()
    {
        int count = content.questions.Length;
        // Choose another question when more than one is available.
        int next = Random.Range(0, count > 1 && questionIndex >= 0 ? count - 1 : count);
        if (count > 1 && questionIndex >= 0 && next >= questionIndex) next++;
        questionIndex = next;
        answered = false;
        rewardGranted = false;
        questionText.text = content.questions[questionIndex].prompt;
        feedbackText.text = "맞으면 O, 틀리면 X를 선택하세요.";
        trueButton.interactable = falseButton.interactable = true;
        actionButton.gameObject.SetActive(false);
    }

    public void AnswerTrue() => Answer(true);
    public void AnswerFalse() => Answer(false);
    private void Answer(bool value)
    {
        if (!open || answered) return;
        answered = true;
        trueButton.interactable = falseButton.interactable = false;
        var question = content.questions[questionIndex];
        bool correct = value == question.answer;
        rewardGranted = correct;
        if (correct)
        {
            if (powerReward) gameManager.ActivatePowerBonus();
            else gameManager.AddGeneratedEnergy(energyReward);
        }
        feedbackText.text = (correct ? "정답! 보너스를 받았습니다." : $"아쉬워요! 정답은 {(question.answer ? "O" : "X")}입니다.")
            + "\n\n" + question.explanation;
        actionText.text = correct ? "닫기" : "다른 문제 풀기";
        actionButton.gameObject.SetActive(true);
    }

    private void Continue()
    {
        if (!open || !answered) return;
        if (rewardGranted) Close();
        else NewQuestion();
    }

    public void Close() { open = false; modal.SetActive(false); }

    private void Update()
    {
        if (gameManager == null) return;
        int seconds = Mathf.CeilToInt(gameManager.BonusSecondsRemaining);
        countdownRoot.gameObject.SetActive(seconds > 0);
        if (seconds == lastCountdown) return;
        lastCountdown = seconds;
        countdownText.text = $"발전량 ×2\n{seconds / 60:00}:{seconds % 60:00}";
    }
}

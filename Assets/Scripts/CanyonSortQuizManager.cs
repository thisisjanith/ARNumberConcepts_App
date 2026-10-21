using System.Collections;
using UnityEngine;
using TMPro;

public class CanyonSortQuizManager : MonoBehaviour
{
    [Header("Settings")]
    public int totalNumbers = 4;
    private int placedCorrectly = 0;

    [Header("UI References")]
    public GameObject explanationPanel;
    public UnityEngine.UI.Button startButton; // "Got it!" button on the explanation panel
    public GameObject quizPanel;
    public GameObject successPanel;
    public UnityEngine.UI.Button nextChapterButton; // BTN_NxtChapter — hidden until every number is sorted

    [Header("Success Subtitle")]
    public TextMeshProUGUI storyText; // SuccessPanel/SubtitleBox/StoryText
    public GameObject subtitleBox;

    [Header("Typewriter Speed Settings")]
    [Tooltip("Lower value = Faster typing speed (0.02 - 0.035 works well).")]
    public float timePerCharacter = 0.05f;
    [Tooltip("Pause duration in seconds between subtitle pages.")]
    public float pauseBetweenPages = 0.8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip voSuccess;

    // Split into pages so each fits comfortably inside the subtitle box
    private readonly string[] successPages = new string[]
    {
        "As the last number locked into place, Dinal safely crossed the wide gap in the tall red cliffs.",
        "The air suddenly grew cool and strange as the path climbed through a crack in the rock.",
        "Stepping inside, Dinal's breath was taken away by a cave where giant purple, blue, and ice-white crystals sparkled on every surface!"
    };

    private Coroutine typewriterCoroutine;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartQuiz);
        }

        if (nextChapterButton != null)
        {
            nextChapterButton.onClick.AddListener(ShowSuccessPanel);
        }

        ResetQuiz();
    }

    public void ResetQuiz()
    {
        placedCorrectly = 0;

        if (successPanel != null) successPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (explanationPanel != null) explanationPanel.SetActive(true);
        if (nextChapterButton != null) nextChapterButton.gameObject.SetActive(false);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        if (subtitleBox != null) subtitleBox.SetActive(false);
        if (storyText != null) storyText.text = "";
    }

    public void StartQuiz()
    {
        if (explanationPanel != null) explanationPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        // Analytics: record total numbers for sort quiz
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.AddQuizQuestions(totalNumbers);
            AnalyticsManager.Instance.StartQuizTimer();
        }
    }

    public void OnNumberPlacedCorrectly()
    {
        placedCorrectly++;

        if (placedCorrectly >= totalNumbers)
        {
            OnSectionComplete();
        }
    }

    /// <summary>
    /// Every number has been sorted into the right bin — reveal the Next Chapter button
    /// so the child taps through to the success panel at their own pace.
    /// </summary>
    private void OnSectionComplete()
    {
        // Analytics: stop quiz timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopQuizTimer();

        if (nextChapterButton != null) nextChapterButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Wired to BTN_NxtChapter.onClick
    /// </summary>
    public void ShowSuccessPanel()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(true);

        PlaySuccessSequence();
    }

    private void PlaySuccessSequence()
    {
        if (audioSource != null && voSuccess != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(voSuccess);
        }

        if (subtitleBox != null) subtitleBox.SetActive(true);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(MultiPageTypewriterRoutine(successPages));
    }

    private IEnumerator MultiPageTypewriterRoutine(string[] pages)
    {
        if (storyText == null) yield break;

        for (int p = 0; p < pages.Length; p++)
        {
            string pageText = pages[p];
            storyText.text = "";

            for (int i = 0; i <= pageText.Length; i++)
            {
                storyText.text = pageText.Substring(0, i);
                yield return new WaitForSeconds(timePerCharacter);
            }

            if (p < pages.Length - 1)
            {
                yield return new WaitForSeconds(pauseBetweenPages);
            }
        }

        typewriterCoroutine = null;
    }
}

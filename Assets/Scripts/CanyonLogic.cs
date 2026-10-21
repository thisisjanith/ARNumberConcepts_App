using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CanyonLogic : MonoBehaviour
{
    [Header("Puzzle Sequence")]
    // Bottom-to-top order of the "safe" (odd) tiles the child must tap
    public int[] targetSequence = new int[] { 77, 67, 43, 23 };
    private int nextIndex = 0;

    [Header("References")]
    public DinalHopper dinalHopper;
    public Transform finalDestination; // where Dinal hops to once the puzzle is solved
    public MissionLogic missionLogic; // optional, matches existing chapters' Finish-button pattern
    public UIManager uiManager;       // optional, drag your LogicManager here to call ShowSuccessPanel()

    [Header("UI References")]
    public TextMeshProUGUI progressText;
    public GameObject completionPanel;
    public GameObject retryButton;
    public GameObject startQuizButton; // BTN_StartQuiz — revealed once the success voiceover plays

    [Header("Subtitle Settings")]
    public TextMeshProUGUI subtitleText;
    public GameObject subtitlePanel;

    [Header("Typewriter Speed Settings")]
    [Tooltip("Lower value = Faster typing speed (0.02 - 0.035 works well).")]
    public float timePerCharacter = 0.05f;
    [Tooltip("Pause duration in seconds between subtitle pages.")]
    public float pauseBetweenPages = 0.8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip arSpawnVO;
    public AudioClip voCorrectTap;
    public AudioClip voWrongTap;
    public AudioClip voSuccess;

    // Intro instructions, split into pages shown one after another
    private readonly string[] subtitlePages = new string[]
    {
        "Floating over the gap were eight flat stones, each carved with a number.",
        "Only odd numbers make safe stepping stones — even numbers will crack!",
        "Tap the odd stones in order, from the bottom to the top, to help Dinal cross safely."
    };

    // Shown once Dinal finishes hopping to the final destination
    private readonly string[] successPages = new string[]
    {
        "You did it! Dinal safely crossed the Echoing Canyon by stepping only on odd numbers.",
        "The canyon falls quiet once more."
    };

    private Coroutine typewriterCoroutine;
    private bool hasInitialized = false; // Prevents counting initial Start() reset as a retry

    private void Start()
    {
        ResetActivity();
        hasInitialized = true;
    }

    public void ResetActivity()
    {
        // Analytics: record retry (skip the initial call from Start)
        if (hasInitialized && AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.RecordRetry();

        nextIndex = 0;

        if (completionPanel != null) completionPanel.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (startQuizButton != null) startQuizButton.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        CanyonTile[] tiles = GetComponentsInChildren<CanyonTile>(true);
        foreach (CanyonTile tile in tiles)
        {
            tile.ResetTile();
        }

        UpdateUI();
    }

    /// <summary>
    /// Call this method from Vuforia's OnTargetFound() event on ImageTarget_Canyon
    /// </summary>
    public void PlaySpawnVoiceover()
    {
        // Analytics: start AR interaction timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StartARTimer();

        if (audioSource != null && arSpawnVO != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(arSpawnVO);
        }

        if (subtitlePanel != null) subtitlePanel.SetActive(true);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(MultiPageTypewriterRoutine(subtitlePages));
    }

    private IEnumerator MultiPageTypewriterRoutine(string[] pages)
    {
        if (subtitleText == null) yield break;

        for (int p = 0; p < pages.Length; p++)
        {
            string pageText = pages[p];
            subtitleText.text = "";

            for (int i = 0; i <= pageText.Length; i++)
            {
                subtitleText.text = pageText.Substring(0, i);
                yield return new WaitForSeconds(timePerCharacter);
            }

            if (p < pages.Length - 1)
            {
                yield return new WaitForSeconds(pauseBetweenPages);
            }
        }

        typewriterCoroutine = null;
    }

    private void StopSpawnTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    public bool TrySelectTile(int tileNumber, CanyonTile tile)
    {
        if (nextIndex >= targetSequence.Length) return false;

        // Stop intro typewriter on first tap
        StopSpawnTypewriter();

        if (tileNumber == targetSequence[nextIndex])
        {
            nextIndex++;
            UpdateUI();
            PlayAudioFeedback(voCorrectTap);

            bool puzzleSolved = nextIndex >= targetSequence.Length;

            if (dinalHopper != null)
            {
                if (puzzleSolved && finalDestination != null)
                {
                    // Hop onto the last tile first, then hop across to the final destination
                    dinalHopper.HopTo(tile.transform, () =>
                    {
                        dinalHopper.HopTo(finalDestination, PlaySuccessVoiceover, applyLandingOffset: false);
                    });
                }
                else
                {
                    dinalHopper.HopTo(tile.transform);
                }
            }

            if (puzzleSolved)
            {
                OnActivityComplete(playImmediateSuccessVoiceover: finalDestination == null || dinalHopper == null);
            }

            return true;
        }

        PlayAudioFeedback(voWrongTap);
        return false;
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Safe Stones: {nextIndex} / {targetSequence.Length}";
        }
    }

    private void OnActivityComplete(bool playImmediateSuccessVoiceover)
    {
        Debug.Log("Echoing Canyon Activity Completed!");

        // Analytics: stop AR interaction timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopARTimer();

        if (completionPanel != null) completionPanel.SetActive(true);
        if (retryButton != null) retryButton.SetActive(true);

        // If Dinal isn't hopping to a final destination, play the success feedback right away.
        // Otherwise it's deferred until the hop lands (see PlaySuccessVoiceover).
        if (playImmediateSuccessVoiceover)
        {
            PlaySuccessVoiceover();
        }
    }

    private void PlaySuccessVoiceover()
    {
        PlayAudioFeedback(voSuccess);
        HapticFeedback.Success();

        if (subtitlePanel != null) subtitlePanel.SetActive(true);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(MultiPageTypewriterRoutine(successPages));

        if (startQuizButton != null) startQuizButton.SetActive(true);
    }

    /// <summary>
    /// Wired to BTN_StartQuiz.onClick
    /// </summary>
    public void GoToQuiz()
    {
        PlayerPrefs.SetString("TargetQuiz", "Canyon");
        SceneManager.LoadScene("QuizScene");
    }

    private void PlayAudioFeedback(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }
}

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Canyon chapter AR activity (odd and even numbers). Eight numbered stones float over a
/// canyon; the child must tap only the odd ones, in order from bottom to top (77, 67, 43, 23).
/// Each correct tap makes Dinal hop onto that stone; a wrong tap plays a hint voiceover. When
/// finished, Dinal hops to the far side, the success narration plays and the Start Quiz button
/// appears.
/// </summary>
public class CanyonLogic : MonoBehaviour
{
    [Header("Puzzle Sequence")]
    // Bottom-to-top order of the "safe" (odd) tiles the child must tap
    public int[] targetSequence = new int[] { 77, 67, 43, 23 };
    // Index into targetSequence of the next stone the child must tap.
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

    // Running typewriter coroutine (null when none is running).
    private Coroutine typewriterCoroutine;
    private bool hasInitialized = false; // Prevents counting initial Start() reset as a retry

    /// <summary>Sets up a fresh puzzle when the scene loads.</summary>
    private void Start()
    {
        ResetActivity();
        hasInitialized = true;
    }

    /// <summary>
    /// Resets the puzzle: tiles, progress and UI. Also wired to the Retry button.
    /// </summary>
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

        // Passing 'true' also finds inactive tiles, so every tile gets reset.
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

    /// <summary>
    /// Coroutine: types each subtitle page one character at a time, pausing between pages.
    /// </summary>
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

    /// <summary>Stops the intro subtitles (once the child starts tapping).</summary>
    private void StopSpawnTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    /// <summary>
    /// Called by CanyonTile when tapped. Returns true if this was the next correct stone. Keeping
    /// the rules here, not in each tile, means the sequence is defined in one place.
    /// </summary>
    public bool TrySelectTile(int tileNumber, CanyonTile tile)
    {
        // Puzzle already finished: ignore extra taps.
        if (nextIndex >= targetSequence.Length) return false;

        // Stop intro typewriter on first tap
        StopSpawnTypewriter();

        // Correct stone: advance progress, play feedback and move Dinal.
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
                    // The lambda () => { ... } is a callback that runs when the first hop lands.
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

        // Wrong stone: play the "try again" voiceover (the tile flashes red itself).
        PlayAudioFeedback(voWrongTap);
        return false;
    }

    /// <summary>Updates the "Safe Stones: x / 4" progress text.</summary>
    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Safe Stones: {nextIndex} / {targetSequence.Length}";
        }
    }

    /// <summary>Puzzle solved: stop the AR timer and show the completion UI.</summary>
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

    /// <summary>
    /// Plays the success audio, vibration and subtitles, and reveals the Start Quiz button.
    /// </summary>
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
        // Tell QuizScene to open the Canyon quiz panel (read by MasterQuizController).
        PlayerPrefs.SetString("TargetQuiz", "Canyon");
        SceneManager.LoadScene("QuizScene");
    }

    /// <summary>
    /// Plays one feedback clip, stopping any previous clip so voiceovers never overlap.
    /// </summary>
    private void PlayAudioFeedback(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }
}

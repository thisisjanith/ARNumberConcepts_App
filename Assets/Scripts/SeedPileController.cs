using System.Collections;
using UnityEngine;
using TMPro;

public class SeedPileController : MonoBehaviour
{
    [Header("Target Number Configuration")]
    public int targetNumber = 2305;
    private int remainingSeeds;

    [Header("Place Value Tracker")]
    public int countThousands = 0;
    public int countHundreds = 0;
    public int countTens = 0;
    public int countOnes = 0;

    [Header("UI References")]
    public TextMeshProUGUI remainingSeedsText;
    public TextMeshProUGUI equationDisplayText; // e.g. "2,000 + 300 + 0 + 5 = 2,305"
    public GameObject completionPanel;
    public GameObject retryButton;

    [Header("Subtitle Settings")]
    public TextMeshProUGUI subtitleText; // Reference to your Subtitle UI Text
    public GameObject subtitlePanel;     // Parent container/panel for subtitles

    [Header("Typewriter Speed Settings")]
    [Tooltip("Lower value = Faster typing speed (0.02 - 0.035 works well).")]
    public float timePerCharacter = 0.05f;
    [Tooltip("Pause duration in seconds between subtitle pages.")]
    public float pauseBetweenPages = 0.8f;

    [Header("Audio Settings - Voiceovers")]
    public AudioSource audioSource;
    public AudioClip arSpawnVO; // Kingfisher spawn VO clip

    [Header("Audio Settings - Tap Feedback VO")]
    public AudioClip voThousands;    // "One thousand seeds added!"
    public AudioClip voHundreds;     // "One hundred seeds added!"
    public AudioClip voTens;         // "Ten seeds added!"
    public AudioClip voOnes;         // "One seed added!"
    public AudioClip voZeroTens;     // "Zero tens needed!"
    public AudioClip voFinalSuccess; // Special VO for the very last remaining seed!

    [Header("Audio Settings - Overfill / Limit Reached VO")]
    public AudioClip voLimitThousands; // "We already have 2 thousands! No more chests needed."
    public AudioClip voLimitHundreds;  // "We already have 3 hundreds! No more baskets needed."
    public AudioClip voLimitOnes;      // "We already have 5 ones! No more seeds needed."

    private Coroutine typewriterCoroutine;
    private bool hasInitialized = false; // Prevents counting initial Start() reset as a retry

    // Subtitle split into two distinct pages for spawn VO
    private readonly string[] subtitlePages = new string[]
    {
        "Look! The sacred containers have appeared before you. We must sort two thousand, three hundred, and five seeds.",
        "Tap the Stone Chests for thousands, Baskets for hundreds, Pouches for tens, and Lotus Seeds for ones. Let's begin!"
    };

    private void Start()
    {
        // Aggressively find the Retry button if not assigned, no matter where it is
        if (retryButton == null)
        {
            UnityEngine.UI.Button[] allButtons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
            foreach (UnityEngine.UI.Button btn in allButtons)
            {
                if (btn.name == "BTN_RetryMission" && btn.transform.parent != null && btn.transform.parent.name.Contains("River_UI_Group"))
                {
                    retryButton = btn.gameObject;
                    Debug.Log("[SeedPileController] Successfully auto-located BTN_RetryMission!");
                    break;
                }
            }

            if (retryButton == null)
            {
                Debug.LogError("[SeedPileController] CRITICAL: BTN_RetryMission could not be found anywhere in the River_UI_Group!");
            }
        }

        ResetActivity();
        hasInitialized = true;
    }

    public void ResetActivity()
    {
        // Analytics: record retry (skip the initial call from Start)
        if (hasInitialized && AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.RecordRetry();

        remainingSeeds = targetNumber;
        countThousands = 0;
        countHundreds = 0;
        countTens = 0;
        countOnes = 0;

        if (completionPanel != null) completionPanel.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (remainingSeedsText != null) remainingSeedsText.gameObject.SetActive(false);
        UpdateUI();
    }

    /// <summary>
    /// Call this method from Vuforia's OnTargetFound() event on ImageTarget_River
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
        if (remainingSeedsText != null) remainingSeedsText.gameObject.SetActive(true);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(MultiPageTypewriterRoutine());
    }

    private IEnumerator MultiPageTypewriterRoutine()
    {
        if (subtitleText == null) yield break;

        for (int p = 0; p < subtitlePages.Length; p++)
        {
            string pageText = subtitlePages[p];
            subtitleText.text = "";

            for (int i = 0; i <= pageText.Length; i++)
            {
                subtitleText.text = pageText.Substring(0, i);
                yield return new WaitForSeconds(timePerCharacter);
            }

            if (p < subtitlePages.Length - 1)
            {
                yield return new WaitForSeconds(pauseBetweenPages);
            }
        }

        typewriterCoroutine = null;
    }

    public bool TryFillContainer(int value, string containerType)
    {
        int maxAllowed = 0;
        switch (value)
        {
            case 1000: maxAllowed = (targetNumber / 1000) % 10; break;
            case 100:  maxAllowed = (targetNumber / 100) % 10; break;
            case 10:   maxAllowed = (targetNumber / 10) % 10; break;
            case 1:    maxAllowed = (targetNumber / 1) % 10; break;
        }

        int currentCount = GetCountForValue(value);

        // Stop intro typewriter on first tap
        StopSpawnTypewriter();

        // 1. Zero-placeholder handling (e.g. Tens = 0)
        if (maxAllowed == 0)
        {
            Debug.Log($"Zero Placeholder Hit! No {containerType} needed.");
            ShowTapSubtitle("Zero tens needed for 2,305!");
            PlayAudioFeedback(voZeroTens);
            return false;
        }

        // 2. Limit Reached handling (e.g. child taps Thousands a 3rd time)
        if (currentCount >= maxAllowed)
        {
            Debug.Log($"Limit Reached for {containerType}!");
            PlayLimitReachedFeedback(value, maxAllowed);
            return false;
        }

        // 3. Successful fill step
        if (remainingSeeds >= value)
        {
            remainingSeeds -= value;
            IncrementCount(value);
            UpdateUI();

            if (remainingSeeds == 0)
            {
                PlayFinalSuccessAudioAndSubtitle();
                OnActivityComplete();
            }
            else
            {
                PlayTapAudioAndSubtitle(value);
            }

            return true;
        }

        return false;
    }

    private void PlayLimitReachedFeedback(int value, int maxAllowed)
    {
        AudioClip clipToPlay = null;
        string limitSubtitle = "";

        switch (value)
        {
            case 1000:
                clipToPlay = voLimitThousands;
                limitSubtitle = $"We already have {maxAllowed} thousands! No more chests needed.";
                break;
            case 100:
                clipToPlay = voLimitHundreds;
                limitSubtitle = $"We already have {maxAllowed} hundreds! No more baskets needed.";
                break;
            case 1:
                clipToPlay = voLimitOnes;
                limitSubtitle = $"We already have {maxAllowed} ones! No more seeds needed.";
                break;
        }

        ShowTapSubtitle(limitSubtitle);
        PlayAudioFeedback(clipToPlay);
    }

    private void StopSpawnTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    private void PlayTapAudioAndSubtitle(int value)
    {
        AudioClip clipToPlay = null;
        string tapSubtitle = "";

        switch (value)
        {
            case 1000:
                clipToPlay = voThousands;
                tapSubtitle = "One thousand seeds added!";
                break;
            case 100:
                clipToPlay = voHundreds;
                tapSubtitle = "One hundred seeds added!";
                break;
            case 10:
                clipToPlay = voTens;
                tapSubtitle = "Ten seeds added!";
                break;
            case 1:
                clipToPlay = voOnes;
                tapSubtitle = "One seed added!";
                break;
        }

        ShowTapSubtitle(tapSubtitle);
        PlayAudioFeedback(clipToPlay);
    }

    private void PlayFinalSuccessAudioAndSubtitle()
    {
        string finalSubtitle = "All 2,305 seeds are sorted! Excellent job!";
        ShowTapSubtitle(finalSubtitle);
        
        AudioClip finalClip = voFinalSuccess != null ? voFinalSuccess : voOnes;
        PlayAudioFeedback(finalClip);
    }

    private void ShowTapSubtitle(string message)
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        if (subtitleText != null) subtitleText.text = message;
    }

    private void PlayAudioFeedback(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
    }

    private int GetCountForValue(int val) => val switch
    {
        1000 => countThousands,
        100  => countHundreds,
        10   => countTens,
        1    => countOnes,
        _    => 0
    };

    private void IncrementCount(int val)
    {
        if (val == 1000) countThousands++;
        else if (val == 100) countHundreds++;
        else if (val == 10) countTens++;
        else if (val == 1) countOnes++;
    }

    private void UpdateUI()
    {
        if (remainingSeedsText != null)
            remainingSeedsText.text = $"Seeds Remaining: {remainingSeeds:N0}";
    }

    private void OnActivityComplete()
    {
        Debug.Log("Place Value Activity Completed!");

        // Analytics: stop AR interaction timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopARTimer();

        if (equationDisplayText != null)
        {
            equationDisplayText.text = $"{countThousands * 1000} + {countHundreds * 100} + {countTens * 10} + {countOnes} = {targetNumber:N0}";
        }

        if (completionPanel != null) completionPanel.SetActive(true);
        if (retryButton != null) retryButton.SetActive(true);
        if (remainingSeedsText != null) remainingSeedsText.gameObject.SetActive(false);

        // Haptic success feedback for completing the activity
        HapticFeedback.Success();
    }
}
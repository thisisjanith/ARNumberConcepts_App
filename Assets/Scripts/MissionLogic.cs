using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections; // Required for the Typewriter Coroutine

/// <summary>
/// Gate chapter AR mission controller. Counts the Sun-Stone pieces placed; once all 4 are in,
/// it shows the "Start Quiz" and Retry buttons. It then plays the victory narration and sends
/// the child to the quiz. It also reports AR time, retries and the end of the session to
/// AnalyticsManager.
/// </summary>
public class MissionLogic : MonoBehaviour 
{
    [Header("UI References")]
    public Button finishButton;      
    public Button retryButton;
    public GameObject successPanel;  
    public TMP_Text storyTextDisplay; 
    public UIManager uiManager; // DRAG YOUR LOGICMANAGER HERE

    [Header("Audio")]
    public AudioSource voiceOverSource; 
    public AudioClip victoryVoiceClipEng; 
    public AudioClip victoryVoiceClipSin; 

    [Header("Mission Settings")]
    // Pieces placed so far (private, so it does not appear in the Inspector).
    private int piecesCollected = 0;
    public int totalPieces = 4; 
    public float typingSpeed = 0.04f; 

    [TextArea(3, 10)]
    public string storyContent = "Amazing work! As the final stone clicked into place, a surge of golden light filled the air. By mastering the numbers, you have successfully repaired the ancient mechanism and unlocked the legendary Wild Gate. Look! Nimal is stepping through the portal into the deep, mysterious jungle beyond.";

    // Cached language flag: true = Sinhala.
    private bool isSinhala;

    /// <summary>Buttons and success panel stay hidden until the mission is complete.</summary>
    void Start()
    {
        // Retrieve language choice from LoginScene
        isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);

        if(finishButton) finishButton.gameObject.SetActive(false);
        if(retryButton) retryButton.gameObject.SetActive(false);
        if(successPanel) successPanel.SetActive(false);
    }

    /// <summary>Called by StonePiece each time a piece locks into place.</summary>
    public void PiecePlaced()
    {
        // Track piece placement and show finish button when all collected
        piecesCollected++;
        if (piecesCollected >= totalPieces)
        {
            ShowFinishButton();
        }
    }

    /// <summary>
    /// Shows the Start Quiz and Retry buttons with text in the chosen language.
    /// </summary>
    void ShowFinishButton()
    {
        // Display button with bilingual text
        if(finishButton)
        {
            finishButton.gameObject.SetActive(true);
            if (retryButton) retryButton.gameObject.SetActive(true);

            TMP_Text btnText = finishButton.GetComponentInChildren<TMP_Text>();
            if(btnText != null)
            {
                // The ternary operator (condition ? a : b) picks the text for the chosen language.
                btnText.text = isSinhala ? "ප්‍රශ්නාවලිය අරඹන්න" : "Start Quiz";
            }
        }
    }

    /// <summary>
    /// Wired to the Start Quiz button. First press plays the victory sequence; the second press
    /// (success panel already open) goes to the quiz.
    /// </summary>
    public void OnFinishButtonClicked()
    {
        // Analytics: stop AR interaction timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopARTimer();

        // Stop any playing voiceover before transition
        if (voiceOverSource != null && voiceOverSource.isPlaying)
        {
            voiceOverSource.Stop();
        }

        // If success panel already shown, go directly to quiz; otherwise show victory sequence first
        if (successPanel != null && successPanel.activeSelf)
        {
            GoToQuiz();
        }
        else
        {
            if(finishButton) finishButton.gameObject.SetActive(false);
            if(retryButton) retryButton.gameObject.SetActive(false);
            if(successPanel) successPanel.SetActive(true);

            StartCoroutine(PlayVictorySequence());
        }
    }

    /// <summary>
    /// Coroutine: plays the victory voiceover; in English the story is typed out at a speed matched
    /// to the audio length.
    /// </summary>
    IEnumerator PlayVictorySequence()
    {
        // Play bilingual victory narration with typewriter effect (English only)
        storyTextDisplay.text = "";

        AudioClip activeClip = isSinhala ? victoryVoiceClipSin : victoryVoiceClipEng;

        if (voiceOverSource != null && activeClip != null)
        {
            voiceOverSource.clip = activeClip;
            voiceOverSource.Play();

            if (!isSinhala)
            {
                // Sync typewriter speed to audio duration
                float calculatedSpeed = activeClip.length / storyContent.Length;

                foreach (char c in storyContent.ToCharArray())
                {
                    storyTextDisplay.text += c;

                    // Wrap text to new line if it exceeds display bounds
                    if (storyTextDisplay.preferredHeight > storyTextDisplay.rectTransform.rect.height)
                    {
                        storyTextDisplay.text = c.ToString();
                    }

                    yield return new WaitForSeconds(calculatedSpeed);
                }
            }
            else
            {
                // Sinhala text displays instantly for readability
                // The text is cleared because in Sinhala mode the subtitle box is hidden (see
                // ARSceneLanguageManager) and the story is told by the voiceover alone.
                storyTextDisplay.text = "";
            }
        }

        // Show button again after narration completes
        yield return new WaitForSeconds(1f);
        if(finishButton)
        {
            finishButton.gameObject.SetActive(true);
            if (retryButton) retryButton.gameObject.SetActive(true);
        }
    }

    /// <summary>Hides the success panel and asks UIManager to open the quiz.</summary>
    public void GoToQuiz()
    {
        // Transition to quiz scene
        if(successPanel) successPanel.SetActive(false);
        if(uiManager != null) uiManager.OpenQuiz();
    }

    /// <summary>
    /// Wired to the Retry button: reloads the current scene for a fresh attempt.
    /// </summary>
    public void ScanAgain()
    {
        // Analytics: record retry
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.RecordRetry();

        // Reload AR scene for retry
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Wired to the Home button: submits the analytics session, then returns to LoginScene.
    /// </summary>
    public void GoToMainMenu()
    {
        // Analytics: fire the submit and move on immediately - AnalyticsManager is
        // DontDestroyOnLoad so the request keeps running through the scene change.
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.SubmitSession();

        SceneManager.LoadScene("LoginScene");
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections; // Required for the Typewriter Coroutine

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
    private int piecesCollected = 0;
    public int totalPieces = 4; 
    public float typingSpeed = 0.04f; 

    [TextArea(3, 10)]
    public string storyContent = "Amazing work! As the final stone clicked into place, a surge of golden light filled the air. By mastering the numbers, you have successfully repaired the ancient mechanism and unlocked the legendary Wild Gate. Look! Nimal is stepping through the portal into the deep, mysterious jungle beyond.";

    private bool isSinhala;

    void Start()
    {
        // Retrieve language choice from LoginScene
        isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);

        if(finishButton) finishButton.gameObject.SetActive(false);
        if(retryButton) retryButton.gameObject.SetActive(false);
        if(successPanel) successPanel.SetActive(false);
    }

    public void PiecePlaced()
    {
        // Track piece placement and show finish button when all collected
        piecesCollected++;
        if (piecesCollected >= totalPieces)
        {
            ShowFinishButton();
        }
    }

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
                btnText.text = isSinhala ? "ප්‍රශ්නාවලිය අරඹන්න" : "Start Quiz";
            }
        }
    }

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

    public void GoToQuiz()
    {
        // Transition to quiz scene
        if(successPanel) successPanel.SetActive(false);
        if(uiManager != null) uiManager.OpenQuiz();
    }

    public void ScanAgain()
    {
        // Analytics: record retry
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.RecordRetry();

        // Reload AR scene for retry
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        // Analytics: fire the submit and move on immediately - AnalyticsManager is
        // DontDestroyOnLoad so the request keeps running through the scene change.
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.SubmitSession();

        SceneManager.LoadScene("LoginScene");
    }
}
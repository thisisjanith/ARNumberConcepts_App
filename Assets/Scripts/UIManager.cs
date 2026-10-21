using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections; // Required for the Typewriter effect

/// <summary>
/// Central UI navigation script reused in several scenes (LoginScene, the AR scene SampleScene
/// and QuizScene). It switches panels on and off and loads scenes. Because the same script is
/// used in different scenes, Start() checks the current scene name and sets up the right
/// panels. Scene flow: OnboardingScene -> LoginScene -> SampleScene (AR) -> QuizScene -> back
/// to LoginScene.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Login Scene Panels")]
    // Panel for initial login/language selection
    public GameObject loginPanel;
    // Main menu with scan and lessons buttons
    public GameObject mainMenuPanel;
    // Tutorial carousel for first-time users
    public GameObject tutorialPanel;
    // Language selection panel (first screen on fresh launch)
    public GameObject languagePanel;

    [Header("AR/Quiz Scene Panels")]
    // Victory/success panel shown after AR mission completion
    public GameObject successPanel;
    // Quiz drag-and-drop interface
    public GameObject quizPanel;
    // Results panel (currently unused per design)
    public GameObject quizResultPanel;

    [Header("AR Content Control")]
    // Parent GameObject containing AR markers and 3D content; hidden during quiz
    public GameObject arContentHolder;

    [Header("Audio & Narrative")]
    // Audio source for victory voiceover narration
    public AudioSource voiceOverSource;
    // English victory narration clip
    public AudioClip victoryVoiceClipEng;
    // Sinhala victory narration clip
    public AudioClip victoryVoiceClipSin;
    // Text display for typewriter victory sequence
    public TMP_Text storyTextDisplay;
    // Speed of character-by-character text display
    public float typingSpeed = 0.04f;

    [Header("Quiz Results")]
    // English score display text
    public TMP_Text scoreTextEng;
    // Sinhala score display text
    public TMP_Text scoreTextSin;

    // Victory narration text (English version)
    [TextArea(3, 10)]
    public string storyContent = "Amazing work! As the final stone clicked into place, a surge of golden light filled the air. By mastering the numbers, you have successfully repaired the ancient mechanism and unlocked the legendary Wild Gate. Look! Nimal is stepping through the portal into the deep, mysterious jungle beyond.";

    // Language state: true = Sinhala, false = English
    private bool isSinhala;

    /// <summary>
    /// Runs when the scene loads: reads the language and sets up the panels for this scene.
    /// </summary>
    void Start()
    {
        // Detect language selection from previous scene
        isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);

        string currentSceneName = SceneManager.GetActiveScene().name;

        // Configure UI visibility based on current scene
        if (currentSceneName == "QuizScene")
        {
            // Quiz scene: show quiz panel, hide others
            if (quizPanel) quizPanel.SetActive(true);
            if (successPanel) successPanel.SetActive(false);
            if (quizResultPanel) quizResultPanel.SetActive(false);
            if (arContentHolder) arContentHolder.SetActive(false);
        }
        else
        {
            // Other scenes: hide quiz-related panels
            if (successPanel) successPanel.SetActive(false);
            if (quizPanel) quizPanel.SetActive(false);
            if (quizResultPanel) quizResultPanel.SetActive(false);

            if (currentSceneName == "LoginScene")
            {
                // Check if we navigated here specifically to see the tutorial
                if (PlayerPrefs.GetInt("ShowTutorialOnLoad", 0) == 1)
                {
                    PlayerPrefs.SetInt("ShowTutorialOnLoad", 0); // Reset the flag

                    // Hide the language panel and login panel so they don't flash
                    if (languagePanel) languagePanel.SetActive(false);
                    if (loginPanel) loginPanel.SetActive(false);
                    if (mainMenuPanel) mainMenuPanel.SetActive(false);

                    ShowTutorial();
                }
            }
        }
    }

    // --- LOGIN & MAIN MENU METHODS ---

    /// <summary>
    /// Reloads LoginScene and opens the tutorial there. A PlayerPrefs flag is used because it must
    /// survive the scene load.
    /// </summary>
    public void GoToTutorial()
    {
        // Set a flag to indicate we want to open the tutorial panel when LoginScene loads
        PlayerPrefs.SetInt("ShowTutorialOnLoad", 1);
        SceneManager.LoadScene("LoginScene");
    }

    /// <summary>Login -> main menu.</summary>
    public void ShowMainMenu()
    {
        // Transition from login to main menu
        if (loginPanel) loginPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (tutorialPanel) tutorialPanel.SetActive(false);
    }

    /// <summary>Main menu -> tutorial.</summary>
    public void ShowTutorial()
    {
        // Show tutorial carousel for first-time users
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (tutorialPanel) tutorialPanel.SetActive(true);
    }

    /// <summary>Back to the login panel.</summary>
    public void ShowLoginPanel()
    {
        // Return to login/language selection
        if (loginPanel) loginPanel.SetActive(true);
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
    }

    /// <summary>Opens the AR scene (called when the tutorial finishes).</summary>
    public void LoadARScene()
    {
        // Load the AR mission scene
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>Opens QuizScene showing the Gate chapter quiz.</summary>
    public void LoadQuizScene()
    {
        // Specifically tell the next scene to open the Gate quiz
        PlayerPrefs.SetString("TargetQuiz", "Gate");
        // Load the quiz scene
        SceneManager.LoadScene("QuizScene");
    }

    // --- AR & QUIZ METHODS ---

    /// <summary>Shows the victory panel and starts the narration.</summary>
    public void ShowSuccessPanel()
    {
        // Show victory screen after mission completion
        if (quizPanel) quizPanel.SetActive(false);
        if (quizResultPanel) quizResultPanel.SetActive(false);
        if (successPanel) successPanel.SetActive(true);

        // Start voiceover and typewriter text sequence
        StartCoroutine(PlayVictorySequence());
    }

    /// <summary>Coroutine: plays the victory voiceover and the matching story text.</summary>
    IEnumerator PlayVictorySequence()
    {
        // Clear text before starting sequence
        if (storyTextDisplay != null) storyTextDisplay.text = "";

        // Select audio clip based on language
        AudioClip activeClip = isSinhala ? victoryVoiceClipSin : victoryVoiceClipEng;

        if (voiceOverSource != null && activeClip != null)
        {
            // Play voiceover
            voiceOverSource.clip = activeClip;
            voiceOverSource.Play();

            // English: typewriter effect synced to audio duration
            if (!isSinhala && storyTextDisplay != null)
            {
                // Calculate character display speed based on audio length
                float calculatedSpeed = activeClip.length / storyContent.Length;

                foreach (char c in storyContent.ToCharArray())
                {
                    storyTextDisplay.text += c;

                    // Handle text overflow: start new page if text exceeds display bounds
                    if (storyTextDisplay.preferredHeight > storyTextDisplay.rectTransform.rect.height)
                    {
                        storyTextDisplay.text = c.ToString();
                    }

                    yield return new WaitForSeconds(calculatedSpeed);
                }
            }
            else if (isSinhala && storyTextDisplay != null)
            {
                // Sinhala: display full text instantly for readability
                storyTextDisplay.text = "විශිෂ්ටයි! ඔබ අංක ඉගෙන ගැනීමෙන් වනගත දොරටුව විවෘත කළා. බලන්න! නිමල් දැන් කැලෑවට ඇතුළු වෙනවා.";
            }
        }
    }

    /// <summary>Leaves AR and shows the quiz panel in the same scene.</summary>
    public void OpenQuiz()
    {
        // Stop any playing voiceover before transitioning to quiz
        if (voiceOverSource != null && voiceOverSource.isPlaying)
        {
            voiceOverSource.Stop();
        }

        // Hide AR content and show quiz panel
        if (arContentHolder) arContentHolder.SetActive(false);
        if (successPanel) successPanel.SetActive(false);
        if (quizPanel) quizPanel.SetActive(true);
    }

    /// <summary>
    /// Kept so existing button and script references still work; the results panel was removed from
    /// the design.
    /// </summary>
    public void ShowQuizResults(int correct, int total)
    {
        // Result panel intentionally left empty per design requirements
    }

    /// <summary>Reloads the AR scene for another attempt.</summary>
    public void ScanAgain()
    {
        // Reload AR scene for retry
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// Wired to the Home button: submits the analytics session, then returns to LoginScene.
    /// </summary>
    public void GoHome()
    {
        // Analytics: fire the submit and move on immediately - AnalyticsManager is
        // DontDestroyOnLoad so the request keeps running through the scene change.
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.SubmitSession();

        SceneManager.LoadScene("LoginScene");
    }
}
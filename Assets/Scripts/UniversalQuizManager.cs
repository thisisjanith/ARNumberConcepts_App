using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UniversalQuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    public List<QuizQuestion> questionList;
    private int currentQuestionIndex = 0;
    private int score = 0;

    [Header("UI References - Question Panel")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons; // Drag your 4 buttons here
    public TextMeshProUGUI feedbackText;
    public Button nextButton;

    [Header("UI References - Completion Panel")]
    public GameObject completionPanel;
    public TextMeshProUGUI scoreText;

    [Header("UI References - Explanation Panel (Optional)")]
    [Tooltip("If assigned, shown before the quiz starts (e.g. 'What is odd/even?'). Leave empty to start the quiz immediately, as before.")]
    public GameObject explanationPanel;

    [Header("Success Subtitle (Optional)")]
    [Tooltip("If assigned, plays a multi-page typewriter subtitle + voiceover when the completion panel is shown.")]
    public TextMeshProUGUI storyText;
    public GameObject subtitleBox;

    [Tooltip("Lower value = Faster typing speed (0.02 - 0.035 works well).")]
    public float timePerCharacter = 0.05f;
    [Tooltip("Pause duration in seconds between subtitle pages.")]
    public float pauseBetweenPages = 0.8f;

    public AudioSource audioSource;
    public AudioClip voSuccess;

    [Tooltip("Split into pages so each fits comfortably inside the subtitle box.")]
    public string[] successPages;

    private Coroutine typewriterCoroutine;

    private void Start()
    {
        // Hook up the button click events automatically
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }

        nextButton.onClick.AddListener(NextQuestion);

        if (explanationPanel != null)
        {
            // Show the explanation first; StartQuiz() is called from the panel's "Continue" button
            quizPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);
            explanationPanel.SetActive(true);
        }
        else
        {
            // Tells the quiz to actually begin when the panel turns on!
            StartQuiz();
        }
    }

    // Call this from your MasterQuizController to start the specific quiz
    public void StartQuiz()
    {
        // --- Guard: Inspector references ---
        if (completionPanel == null)
        {
            Debug.LogError("[UniversalQuizManager] 'completionPanel' is NOT assigned in the Inspector! Please drag the panel GameObject into the field.", this);
            return;
        }
        if (quizPanel == null)
        {
            Debug.LogError("[UniversalQuizManager] 'quizPanel' is NOT assigned in the Inspector!", this);
            return;
        }

        // --- Guard: question data ---
        if (questionList == null || questionList.Count == 0)
        {
            Debug.LogError("[UniversalQuizManager] 'questionList' is empty! Add QuizQuestion entries in the Inspector.", this);
            return;
        }

        currentQuestionIndex = 0;
        score = 0;
        if (explanationPanel != null) explanationPanel.SetActive(false);
        completionPanel.SetActive(false);
        quizPanel.SetActive(true);

        // Analytics: record quiz total for this chapter's MCQ
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.AddQuizQuestions(questionList.Count);
            AnalyticsManager.Instance.StartQuizTimer();
        }

        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        if (currentQuestionIndex >= questionList.Count)
        {
            ShowCompletionPanel();
            return;
        }

        QuizQuestion currentQ = questionList[currentQuestionIndex];
        questionText.text = currentQ.questionText;
        feedbackText.text = "";
        feedbackText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Hide next button at the start of every question
        nextButton.gameObject.SetActive(false); 

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentQ.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                
                // Reset the TEXT color back to white for the new question!
                TextMeshProUGUI buttonText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = currentQ.options[i];
                buttonText.color = Color.white; 
            }
            else
            {
                // Hide the 4th button if a question only has 3 options
                optionButtons[i].gameObject.SetActive(false); 
            }
        }
    }

    public void OnOptionSelected(int selectedIndex)
    {
        // --- Guard: question list must be populated ---
        if (questionList == null || questionList.Count == 0)
        {
            Debug.LogError("[UniversalQuizManager] OnOptionSelected called but 'questionList' is empty! Assign QuizQuestion entries in the Inspector.", this);
            return;
        }

        // --- Guard: index must be in range ---
        if (currentQuestionIndex < 0 || currentQuestionIndex >= questionList.Count)
        {
            Debug.LogError($"[UniversalQuizManager] currentQuestionIndex ({currentQuestionIndex}) is out of range (list size: {questionList.Count}).", this);
            return;
        }

        // --- Guard: selectedIndex must be in range ---
        if (selectedIndex < 0 || selectedIndex >= optionButtons.Length)
        {
            Debug.LogError($"[UniversalQuizManager] selectedIndex ({selectedIndex}) is out of range (button count: {optionButtons.Length}).", this);
            return;
        }

        QuizQuestion currentQ = questionList[currentQuestionIndex];

        // Reset ALL button texts to white before applying any new color
        foreach (Button btn in optionButtons)
        {
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = Color.white;
        }
        
        // Get the text component of the specific button they just clicked
        TextMeshProUGUI selectedText = optionButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
        if (selectedText == null)
        {
            Debug.LogError($"[UniversalQuizManager] Button at index {selectedIndex} has no TextMeshProUGUI child!", this);
            return;
        }

        if (selectedIndex == currentQ.correctAnswerIndex)
        {
            // === CORRECT ANSWER ===
            selectedText.color = Color.green;
            
            if (feedbackText != null)
            {
                feedbackText.text = "Excellent work!";
                feedbackText.color = new Color(0.1f, 0.6f, 0.1f); // Dark green for readability
            }
            score++;
            
            // Lock all buttons so they can't click anything else
            foreach (Button btn in optionButtons) 
            {
                PreserveButtonColor(btn); // keep original blue, don't go gray
                btn.interactable = false;
            }

            // Reveal the Next button!
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
        else
        {
            // === WRONG ANSWER ===
            selectedText.color = Color.red; // Make text red
            
            if (feedbackText != null)
            {
                feedbackText.text = $"Hint: {currentQ.hintText}";
                feedbackText.color = Color.red;
            }
            
            // Disable ONLY the button they clicked, so they can try another option
            PreserveButtonColor(optionButtons[selectedIndex]); // keep original blue, don't go gray
            optionButtons[selectedIndex].interactable = false;
            
            // Analytics: record quiz error
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.RecordQuizError();

            // Ensure the Next button stays hidden
            if (nextButton != null) nextButton.gameObject.SetActive(false);
        }
    }

    public void NextQuestion()
    {
        currentQuestionIndex++;
        DisplayQuestion();
    }

    private void ShowCompletionPanel()
    {
        // Analytics: stop quiz timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopQuizTimer();

        quizPanel.SetActive(false);
        completionPanel.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

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

        if (successPages != null && successPages.Length > 0)
        {
            typewriterCoroutine = StartCoroutine(MultiPageTypewriterRoutine(successPages));
        }
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

    /// <summary>
    /// Sets a button's disabled color to its normal color so it keeps its
    /// original appearance when interactable = false (prevents the gray tint).
    /// </summary>
    private void PreserveButtonColor(Button btn)
    {
        ColorBlock cb = btn.colors;
        cb.disabledColor = cb.normalColor; // match disabled tint to normal color
        btn.colors = cb;
    }
}
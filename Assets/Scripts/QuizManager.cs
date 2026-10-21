using UnityEngine;
using TMPro;

/// <summary>
/// Gate chapter quiz manager (fraction drag-and-drop). Counts correct placements, and when all
/// totalQuestions are correct it reveals the Next Chapter button. It also reports the number of
/// questions and the quiz time to AnalyticsManager for the research data.
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("Settings")]
    // Total number of questions/pieces required to complete the quiz
    public int totalQuestions = 3;
    // Tracks correct answers placed in slots
    private int correctAnswers = 0;
    // Prevents showing next chapter button before quiz completion
    private bool isQuizFinished = false;
    // Prevents double-scoring when multiple handlers call OnAnswerCorrect in same frame
    private float lastScoreTime = 0f;

    [Header("References")]
    // Reference to UIManager for scene transitions
    public UIManager uiManager;

    [Header("UI Controls")]
    // Button that appears when all questions are answered correctly
    public GameObject nextChapterButton;

    /// <summary>
    /// Resets the quiz and starts the analytics quiz timer when the scene loads.
    /// </summary>
    void Start()
    {
        // Reset quiz state for fresh session
        isQuizFinished = false;
        correctAnswers = 0;
        lastScoreTime = 0f;

        // Analytics: record total questions for drag-and-drop quiz
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.AddQuizQuestions(totalQuestions);
            AnalyticsManager.Instance.StartQuizTimer();
        }

        // Hide next chapter button until quiz is complete
        if (nextChapterButton != null)
        {
            nextChapterButton.SetActive(false);
        }
    }

    /// <summary>
    /// Called by QuizDraggable / QuizSlot when a fraction is dropped into the correct slot.
    /// </summary>
    public void OnAnswerCorrect()
    {
        // Ignore if quiz already completed
        if (isQuizFinished) return;

        // Prevent double-scoring: ignore calls within 0.2 seconds of the last correct answer
        // This handles multiple event handlers (QuizDraggable and QuizSlot) firing simultaneously
        if (Time.time - lastScoreTime < 0.2f) return;

        // Record timestamp of this correct answer
        lastScoreTime = Time.time;
        correctAnswers++;

        Debug.Log("Correct Answer! Current Score: " + correctAnswers);

        // Check if all questions have been answered
        if (correctAnswers >= totalQuestions)
        {
            FinishQuiz();
        }
    }

    /// <summary>
    /// All answers correct: stop the quiz timer and show the Next Chapter button.
    /// </summary>
    void FinishQuiz()
    {
        // Mark quiz as complete to prevent further scoring
        isQuizFinished = true;
        Debug.Log("Quiz Complete!");

        // Analytics: stop quiz timer
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StopQuizTimer();

        // Show next chapter button for progression
        if (nextChapterButton != null)
        {
            nextChapterButton.SetActive(true);
        }
    }

    /// <summary>Public so a Retry button can reset the quiz.</summary>
    public void ResetQuiz()
    {
        // Reset all quiz state for retry or new session
        correctAnswers = 0;
        isQuizFinished = false;
        lastScoreTime = 0f;
        // Hide next chapter button until quiz is completed again
        if (nextChapterButton != null)
        {
            nextChapterButton.SetActive(false);
        }
    }
}
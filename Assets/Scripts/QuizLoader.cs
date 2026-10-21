using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small helper used by a UI button to open the River chapter's quiz. It stores which quiz to
/// show in PlayerPrefs (key "TargetQuiz") and then loads QuizScene, where MasterQuizController
/// reads that key and enables the matching quiz panel. PlayerPrefs is used because normal
/// variables are lost when Unity unloads a scene.
/// </summary>
public class QuizLoader : MonoBehaviour
{
    /// <summary>Wired to the River chapter's "Start Quiz" button (OnClick).</summary>
    public void GoToRiverQuiz()
    {
        PlayerPrefs.SetString("TargetQuiz", "River");
        SceneManager.LoadScene("QuizScene"); // Make sure this perfectly matches your actual scene's name!
    }
}
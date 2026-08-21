using UnityEngine;
using UnityEngine.SceneManagement;

public class QuizLoader : MonoBehaviour
{
    public void GoToRiverQuiz()
    {
        PlayerPrefs.SetString("TargetQuiz", "River");
        SceneManager.LoadScene("QuizScene"); // Make sure this perfectly matches your actual scene's name!
    }
}
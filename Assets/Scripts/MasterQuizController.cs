using UnityEngine;

public class MasterQuizController : MonoBehaviour
{
    public GameObject gateQuizPanel;
    public GameObject riverQuizPanel;

    private void Start()
    {
        // 1. Turn both off just to be safe (if they are assigned)
        if (gateQuizPanel != null) gateQuizPanel.SetActive(false);
        else Debug.LogError("[MasterQuizController] gateQuizPanel is NOT assigned in the Inspector!", this);

        if (riverQuizPanel != null) riverQuizPanel.SetActive(false);
        else Debug.LogError("[MasterQuizController] riverQuizPanel is NOT assigned in the Inspector!", this);

        // 2. Ask the Taxi Driver (PlayerPrefs) which quiz we wanted
        string quizToLoad = PlayerPrefs.GetString("TargetQuiz", "Gate"); 

        // 3. Turn on the correct room!
        if (quizToLoad == "Gate")
        {
            if (gateQuizPanel != null) gateQuizPanel.SetActive(true);
        }
        else if (quizToLoad == "River")
        {
            if (riverQuizPanel != null) riverQuizPanel.SetActive(true);
        }
    }
}
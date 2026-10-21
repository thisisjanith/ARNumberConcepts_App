using UnityEngine;

/// <summary>
/// Lives in QuizScene. All three chapters (Gate, River, Canyon) share one quiz scene, and each
/// chapter's quiz is a separate panel inside it. On start, this script reads which chapter the
/// child came from (PlayerPrefs "TargetQuiz", set just before the scene was loaded) and shows
/// only that panel. This avoids building and maintaining three separate quiz scenes.
/// </summary>
public class MasterQuizController : MonoBehaviour
{
    // One panel per chapter quiz, assigned in the Inspector.
    public GameObject gateQuizPanel;
    public GameObject riverQuizPanel;
    public GameObject canyonQuizPanel;

    /// <summary>Unity calls Start once, before the first frame, after the scene loads.</summary>
    private void Start()
    {
        // 1. Turn all off just to be safe (if they are assigned)
        if (gateQuizPanel != null) gateQuizPanel.SetActive(false);
        else Debug.LogError("[MasterQuizController] gateQuizPanel is NOT assigned in the Inspector!", this);

        if (riverQuizPanel != null) riverQuizPanel.SetActive(false);
        else Debug.LogError("[MasterQuizController] riverQuizPanel is NOT assigned in the Inspector!", this);

        if (canyonQuizPanel != null) canyonQuizPanel.SetActive(false);
        else Debug.LogError("[MasterQuizController] canyonQuizPanel is NOT assigned in the Inspector!", this);

        // 2. Ask the Taxi Driver (PlayerPrefs) which quiz we wanted
        // "Gate" is the default value in case the key was never saved.
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
        else if (quizToLoad == "Canyon")
        {
            if (canyonQuizPanel != null) canyonQuizPanel.SetActive(true);
        }
    }
}
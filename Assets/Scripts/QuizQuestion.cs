using UnityEngine;

/// <summary>
/// Plain data class (not a MonoBehaviour) describing one multiple-choice question.
/// [System.Serializable] lets Unity show a list of these in the Inspector, so questions can be
/// written and edited without changing code. Used by UniversalQuizManager.
/// </summary>
[System.Serializable]
public class QuizQuestion
{
    // Question text shown at the top of the quiz panel. [TextArea] gives a multi-line box in the
    // Inspector.
    [TextArea(2, 4)]
    public string questionText;

    // Answer choices, one per option button (3 or 4 entries).
    public string[] options;

    // Index into 'options' of the correct answer (0 = first option).
    public int correctAnswerIndex;

    // Hint shown when the child picks a wrong answer, so they are guided instead of just being
    // marked wrong.
    [TextArea(1, 3)]
    public string hintText;
}

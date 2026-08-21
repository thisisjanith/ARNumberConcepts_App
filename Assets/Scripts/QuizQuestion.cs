using UnityEngine;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string questionText;

    public string[] options;

    public int correctAnswerIndex;

    [TextArea(1, 3)]
    public string hintText;
}

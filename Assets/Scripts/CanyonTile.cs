using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CanyonTile : MonoBehaviour
{
    [Header("Tile Configuration")]
    public int tileNumber;

    [Header("Visual State Variants")]
    public GameObject stateDefault;
    public GameObject stateCorrect;
    public GameObject stateWrong;

    [Header("Number Label")]
    public TextMeshPro numberLabel;
    public Color labelDefaultColor = Color.white;
    public Color labelCorrectColor = Color.green;
    public Color labelWrongColor = Color.red;

    [Header("References")]
    public CanyonLogic controller;

    [Header("Wrong Flash Timing")]
    public float wrongFlashDuration = 0.5f;

    private bool isSolved = false;
    private Coroutine wrongFlashCoroutine;

    private void Start()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<CanyonLogic>();
        }

        ShowDefault();
    }

    private void OnMouseDown()
    {
        if (controller == null || isSolved) return;

        bool success = controller.TrySelectTile(tileNumber, this);

        if (success)
        {
            isSolved = true;
            if (wrongFlashCoroutine != null) StopCoroutine(wrongFlashCoroutine);
            ShowCorrect();
            HapticFeedback.LightTap();
        }
        else
        {
            if (wrongFlashCoroutine != null) StopCoroutine(wrongFlashCoroutine);
            wrongFlashCoroutine = StartCoroutine(FlashWrong());
            HapticFeedback.Error();
        }
    }

    public void ShowDefault()
    {
        if (stateDefault != null) stateDefault.SetActive(true);
        if (stateCorrect != null) stateCorrect.SetActive(false);
        if (stateWrong != null) stateWrong.SetActive(false);
        SetLabelColor(labelDefaultColor);
    }

    public void ShowCorrect()
    {
        if (stateDefault != null) stateDefault.SetActive(false);
        if (stateCorrect != null) stateCorrect.SetActive(true);
        if (stateWrong != null) stateWrong.SetActive(false);
        SetLabelColor(labelCorrectColor);
    }

    private System.Collections.IEnumerator FlashWrong()
    {
        if (stateDefault != null) stateDefault.SetActive(false);
        if (stateWrong != null) stateWrong.SetActive(true);
        SetLabelColor(labelWrongColor);

        yield return new WaitForSeconds(wrongFlashDuration);

        if (stateWrong != null) stateWrong.SetActive(false);
        if (stateDefault != null) stateDefault.SetActive(true);
        SetLabelColor(labelDefaultColor);

        wrongFlashCoroutine = null;
    }

    private void SetLabelColor(Color color)
    {
        if (numberLabel != null) numberLabel.color = color;
    }

    public void ResetTile()
    {
        isSolved = false;
        if (wrongFlashCoroutine != null)
        {
            StopCoroutine(wrongFlashCoroutine);
            wrongFlashCoroutine = null;
        }
        ShowDefault();
    }
}

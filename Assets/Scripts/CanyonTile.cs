using UnityEngine;
using TMPro;

/// <summary>
/// Canyon chapter AR activity: one of the eight numbered stepping stones floating over the
/// canyon. When tapped, it asks CanyonLogic whether it is the next correct (odd) stone in the
/// sequence and shows the matching visual state: Default, Correct (stays) or Wrong (flashes
/// briefly).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CanyonTile : MonoBehaviour
{
    [Header("Tile Configuration")]
    // Number carved on this stone; compared with CanyonLogic.targetSequence.
    public int tileNumber;

    [Header("Visual State Variants")]
    // Three child objects, one per look; exactly one is active at a time.
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

    // Once solved, further taps on this tile are ignored.
    private bool isSolved = false;
    // Running red-flash coroutine, kept so it can be stopped early.
    private Coroutine wrongFlashCoroutine;

    /// <summary>
    /// Finds the CanyonLogic controller if not assigned and shows the default look.
    /// </summary>
    private void Start()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<CanyonLogic>();
        }

        ShowDefault();
    }

    /// <summary>Called by Unity when the child taps this tile's collider.</summary>
    private void OnMouseDown()
    {
        if (controller == null || isSolved) return;

        // CanyonLogic decides whether the tap is correct; the tile only handles the visuals.
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

    /// <summary>Normal (untapped) look.</summary>
    public void ShowDefault()
    {
        if (stateDefault != null) stateDefault.SetActive(true);
        if (stateCorrect != null) stateCorrect.SetActive(false);
        if (stateWrong != null) stateWrong.SetActive(false);
        SetLabelColor(labelDefaultColor);
    }

    /// <summary>Correct look, kept for the rest of the activity.</summary>
    public void ShowCorrect()
    {
        if (stateDefault != null) stateDefault.SetActive(false);
        if (stateCorrect != null) stateCorrect.SetActive(true);
        if (stateWrong != null) stateWrong.SetActive(false);
        SetLabelColor(labelCorrectColor);
    }

    /// <summary>Coroutine: show the wrong look briefly, then return to default.</summary>
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

    /// <summary>Colours the number label.</summary>
    private void SetLabelColor(Color color)
    {
        if (numberLabel != null) numberLabel.color = color;
    }

    /// <summary>Called by CanyonLogic.ResetActivity when the child retries.</summary>
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

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gate chapter quiz: a drop target for the fraction drag-and-drop quiz. Each slot expects one
/// fraction value; QuizDraggable compares its fractionType with this slot's expectedValue when
/// dropped.
/// </summary>
public class QuizSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    // The expected fraction value that should be dropped here (e.g., "1/2", "3/4")
    public string expectedValue;

    [Header("Visual Feedback")]
    // Default grey color for idle state
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    // Yellow highlight when a draggable hovers over this slot
    public Color highlightColor = Color.yellow;
    // Green confirmation when correct answer is placed
    public Color correctColor = Color.green;
    // Red flash for incorrect placement attempts
    public Color wrongColor = Color.red;

    // Cached Image component used for colour feedback.
    private Image slotImage;

    void Awake()
    {
        // Tag this GameObject so QuizDraggable can identify it as a valid drop target
        gameObject.tag = "QuizSlot";

        // Cache the Image component for color changes
        slotImage = GetComponent<Image>();

        if (slotImage != null)
        {
            // Ensure visibility in Play Mode (handles editor visibility issues)
            slotImage.enabled = true;
            slotImage.color = normalColor;

            // Force full opacity (alpha = 1)
            Color c = slotImage.color;
            c.a = 1f;
            slotImage.color = c;
        }
    }

    /// <summary>Called while a draggable hovers over or leaves this slot.</summary>
    public void SetHighlight(bool isHighlighted)
    {
        // Toggle visual feedback when draggable enters/exits this slot
        if (slotImage == null) return;
        slotImage.color = isHighlighted ? highlightColor : normalColor;
    }

    /// <summary>Called by QuizDraggable after a correct drop.</summary>
    public void MarkAsCorrect()
    {
        // Visual confirmation: change to green
        if (slotImage == null) return;

        slotImage.color = correctColor;
        // Disable raycast so no more draggables can be dropped here
        slotImage.raycastTarget = false;

        // Notify quiz manager to increment the score (research data tracking)
        // QuizDraggable also calls OnAnswerCorrect; QuizManager ignores the duplicate thanks to its
        // 0.2 second guard.
        QuizManager qm = FindObjectOfType<QuizManager>();
        if(qm != null) qm.OnAnswerCorrect();
    }

    /// <summary>Called by QuizDraggable after a wrong drop.</summary>
    public void MarkAsWrong()
    {
        // Trigger brief red flash feedback for incorrect placement
        if (slotImage == null) return;
        StartCoroutine(FlashWrong());
    }

    IEnumerator FlashWrong()
    {
        // Flash red for 0.5 seconds, then return to normal
        slotImage.color = wrongColor;
        yield return new WaitForSeconds(0.5f);
        slotImage.color = normalColor;
    }
}
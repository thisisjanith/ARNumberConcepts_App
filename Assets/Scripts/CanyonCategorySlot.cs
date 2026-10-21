using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Canyon chapter quiz: a drop "bin" (Even or Odd) for the number-sorting drag-and-drop
/// activity. CanyonNumberDraggable compares its own category with this bin's expectedCategory
/// when it is dropped here. The bin gives colour feedback: highlighted while hovered, red flash
/// when wrong.
/// </summary>
public class CanyonCategorySlot : MonoBehaviour
{
    [Header("Bin Settings")]
    // "Even" or "Odd" — must match CanyonNumberDraggable.category exactly
    public string expectedCategory;

    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color highlightColor = Color.yellow;
    public Color wrongColor = Color.red;

    [Header("Drop Container")]
    // Where correctly-dropped numbers get reparented (should have a Layout Group)
    public RectTransform contentHolder;

    // Cached Image component used to change the bin's colour.
    private Image slotImage;

    /// <summary>
    /// Awake runs before Start, so the tag and colour are ready before any drag can happen.
    /// </summary>
    void Awake()
    {
        // The draggable scripts recognise drop targets by this tag.
        gameObject.tag = "QuizSlot";

        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            slotImage.enabled = true;
            // Force full opacity so the bin is clearly visible.
            Color c = normalColor;
            c.a = 1f;
            slotImage.color = c;
        }

        // Fall back to this object's own RectTransform if no content holder was set.
        if (contentHolder == null)
        {
            contentHolder = GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Turns the highlight colour on or off while a number hovers over the bin.
    /// </summary>
    public void SetHighlight(bool isHighlighted)
    {
        if (slotImage == null) return;
        slotImage.color = isHighlighted ? highlightColor : normalColor;
    }

    /// <summary>
    /// Correct drop: moves the number card into this bin so the Layout Group arranges it neatly.
    /// </summary>
    public void AcceptDrop(RectTransform draggable)
    {
        if (slotImage != null) slotImage.color = normalColor;

        // worldPositionStays = false so the card takes its position from the layout instead of where
        // it was dropped.
        draggable.SetParent(contentHolder, false);
    }

    /// <summary>Wrong drop: briefly flashes the bin red.</summary>
    public void MarkAsWrong()
    {
        if (slotImage == null) return;
        StartCoroutine(FlashWrong());
    }

    /// <summary>Coroutine: red for 0.5 seconds, then back to the normal colour.</summary>
    IEnumerator FlashWrong()
    {
        slotImage.color = wrongColor;
        yield return new WaitForSeconds(0.5f);
        slotImage.color = normalColor;
    }
}

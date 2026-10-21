using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    private Image slotImage;

    void Awake()
    {
        gameObject.tag = "QuizSlot";

        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            slotImage.enabled = true;
            Color c = normalColor;
            c.a = 1f;
            slotImage.color = c;
        }

        if (contentHolder == null)
        {
            contentHolder = GetComponent<RectTransform>();
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (slotImage == null) return;
        slotImage.color = isHighlighted ? highlightColor : normalColor;
    }

    public void AcceptDrop(RectTransform draggable)
    {
        if (slotImage != null) slotImage.color = normalColor;

        draggable.SetParent(contentHolder, false);
    }

    public void MarkAsWrong()
    {
        if (slotImage == null) return;
        StartCoroutine(FlashWrong());
    }

    IEnumerator FlashWrong()
    {
        slotImage.color = wrongColor;
        yield return new WaitForSeconds(0.5f);
        slotImage.color = normalColor;
    }
}

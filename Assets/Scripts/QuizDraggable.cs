using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class QuizDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Identifies the fraction value this draggable represents (e.g., "1/2", "3/4")
    public string fractionType;
    private Vector3 startPos;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    // Prevents re-scoring and re-placement after a successful match
    private bool isPlaced = false; 

    void Awake()
    {
        // Cache UI components for drag operations
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Prevent dragging if already placed in correct slot
        if (isPlaced) return;
        // Store original position for potential snap-back on wrong drop
        startPos = transform.localPosition;
        // Allow raycast to pass through to detect drop targets
        canvasGroup.blocksRaycasts = false;
        // Visual feedback: fade to semi-transparent while dragging
        canvasGroup.alpha = 0.7f;
        // Haptic feedback when the child picks up a draggable
        HapticFeedback.MediumTap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Prevent movement if already placed
        if (isPlaced) return;
        // Update position based on pointer movement, accounting for canvas scale
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Prevent re-processing if already placed
        if (isPlaced) return;

        // Restore raycast blocking and full opacity
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // Detect which UI element the draggable was dropped on
        GameObject droppedOn = eventData.pointerEnter;

        if (droppedOn != null && droppedOn.CompareTag("QuizSlot"))
        {
            QuizSlot slot = droppedOn.GetComponent<QuizSlot>();

            // Validate: slot exists, matches expected value, and not already placed
            if (slot != null && slot.expectedValue == fractionType && !isPlaced)
            {
                // Lock immediately to prevent double-scoring from multiple event handlers
                isPlaced = true;
                // Snap to slot position for visual confirmation
                transform.position = droppedOn.transform.position;

                // Haptic success feedback for correct placement
                HapticFeedback.Success();

                // Disable further interaction
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                // Notify slot of correct placement
                slot.MarkAsCorrect();

                // Notify quiz manager to increment score
                QuizManager manager = FindObjectOfType<QuizManager>();
                if(manager != null)
                {
                    manager.OnAnswerCorrect();
                }

                // Disable this script to prevent any further drag events
                this.enabled = false;
                return;
            }
            else if (slot != null && slot.expectedValue != fractionType)
            {
                // Wrong slot: provide visual feedback (red flash)
                slot.MarkAsWrong();
                // Haptic error feedback
                HapticFeedback.Error();
            }
        }

        // Snap back to original position if drop was invalid
        transform.localPosition = startPos;
    }
}
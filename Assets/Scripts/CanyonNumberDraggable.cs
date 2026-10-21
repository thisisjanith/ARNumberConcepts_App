using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Canyon chapter quiz: a draggable number card that the child sorts into the Even or Odd bin.
/// It implements Unity's EventSystem drag interfaces (IBeginDragHandler, IDragHandler,
/// IEndDragHandler), so the UI system calls these methods automatically during a drag. A
/// CanvasGroup is required to control raycasts and transparency while dragging.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CanyonNumberDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Number Settings")]
    // The number printed on the card.
    public int numberValue;
    // Set to "Even" or "Odd" — must match the target CanyonCategorySlot.expectedCategory
    public string category;

    [Header("References")]
    public CanyonSortQuizManager quizManager;

    // Saved when the drag starts so the card can snap back after a wrong drop.
    private Transform originalParent;
    private Vector3 startPos;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    // Becomes true once the card is correctly sorted; locks it in place.
    private bool isPlaced = false;

    /// <summary>
    /// Cache components once here instead of calling GetComponent every frame.
    /// </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Finds the quiz manager automatically if it was not assigned in the Inspector.
    /// </summary>
    void Start()
    {
        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<CanyonSortQuizManager>();
        }
    }

    /// <summary>Called by the EventSystem when the child starts dragging the card.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        originalParent = transform.parent;
        startPos = transform.localPosition;

        // Stop the card blocking raycasts so the pointer can "see" the bin underneath it; this is
        // needed to detect the drop target.
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        // Short vibration as pick-up feedback.
        HapticFeedback.MediumTap();
    }

    /// <summary>Called every frame while the card is being dragged.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        // Move by the pointer's movement divided by the canvas scale, so the card follows the finger
        // exactly on any screen size.
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    /// <summary>Called when the child releases the card.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // pointerEnter is the UI object under the finger at the moment of release.
        GameObject droppedOn = eventData.pointerEnter;

        if (droppedOn != null && droppedOn.CompareTag("QuizSlot"))
        {
            CanyonCategorySlot slot = droppedOn.GetComponent<CanyonCategorySlot>();

            // Correct bin: lock the card into the bin and tell the quiz manager.
            if (slot != null && slot.expectedCategory == category)
            {
                isPlaced = true;

                slot.AcceptDrop(rectTransform);

                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                HapticFeedback.Success();

                if (quizManager != null)
                {
                    quizManager.OnNumberPlacedCorrectly();
                }

                // Disable this script so no further drag events are processed.
                this.enabled = false;
                return;
            }
            // Wrong bin: red flash, error vibration and count the mistake.
            else if (slot != null)
            {
                slot.MarkAsWrong();
                HapticFeedback.Error();

                // Analytics: record quiz error
                if (AnalyticsManager.Instance != null)
                    AnalyticsManager.Instance.RecordQuizError();
            }
        }

        // Snap back to original spot on invalid/wrong drop
        transform.SetParent(originalParent, false);
        transform.localPosition = startPos;
    }
}

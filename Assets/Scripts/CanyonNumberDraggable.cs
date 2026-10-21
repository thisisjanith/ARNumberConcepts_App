using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CanyonNumberDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Number Settings")]
    public int numberValue;
    // Set to "Even" or "Odd" — must match the target CanyonCategorySlot.expectedCategory
    public string category;

    [Header("References")]
    public CanyonSortQuizManager quizManager;

    private Transform originalParent;
    private Vector3 startPos;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isPlaced = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<CanyonSortQuizManager>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        originalParent = transform.parent;
        startPos = transform.localPosition;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        HapticFeedback.MediumTap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        GameObject droppedOn = eventData.pointerEnter;

        if (droppedOn != null && droppedOn.CompareTag("QuizSlot"))
        {
            CanyonCategorySlot slot = droppedOn.GetComponent<CanyonCategorySlot>();

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

                this.enabled = false;
                return;
            }
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

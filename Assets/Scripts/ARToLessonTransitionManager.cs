using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the switch from the AR activity to the in-app (non-AR) practice screen: shows a
/// "well done" overlay, hides the AR content, then fades in the practice panel. Fades use
/// CanvasGroup.alpha because it fades a whole UI panel and all of its children at once.
/// </summary>
public class ARToLessonTransitionManager : MonoBehaviour
{
    [Header("AR Activity References")]
    [Tooltip("The parent GameObject holding the AR 3D models (containers, seeds).")]
    public GameObject arContentParent;
    [Tooltip("The parent UI panel holding the AR screen elements (remaining counter, overlay UI).")]
    public GameObject arUIGroup;

    [Header("In-App Practice UI References")]
    [Tooltip("The main parent panel for the in-app follow-up practice screen.")]
    public GameObject inAppPracticePanel;
    [Tooltip("UI CanvasGroup attached to the practice panel for smooth fading.")]
    public CanvasGroup practicePanelCanvasGroup;

    [Header("Transition Overlay UI")]
    [Tooltip("Optional transition screen/overlay (e.g., screen fade or badge popup).")]
    public GameObject transitionOverlay;
    public CanvasGroup overlayCanvasGroup;
    public TextMeshProUGUI transitionMessageText;

    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;
    public float overlayDisplayDelay = 1.5f;

    /// <summary>
    /// Call this method when the SeedPileController finishes the AR activity!
    /// </summary>
    public void StartTransitionToInAppLesson()
    {
        StartCoroutine(TransitionSequence());
    }

    /// <summary>
    /// Coroutine that runs the steps in order; each 'yield return' waits for that step (e.g. a
    /// fade) to finish before moving on.
    /// </summary>
    private IEnumerator TransitionSequence()
    {
        // 1. Show the transition overlay (e.g., "Great Job! Moving to Practice...")
        if (transitionOverlay != null)
        {
            transitionOverlay.SetActive(true);
            if (transitionMessageText != null)
            {
                transitionMessageText.text = "Bridge Restored!\nLet's test your skills!";
            }
            yield return StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 0f, 1f, fadeDuration));
        }

        // 2. Pause briefly so the child can read/enjoy the completion moment
        yield return new WaitForSeconds(overlayDisplayDelay);

        // 3. Turn off AR-specific 3D content and AR Canvas UI
        if (arContentParent != null) arContentParent.SetActive(false);
        if (arUIGroup != null) arUIGroup.SetActive(false);

        // 4. Enable and fade in the In-App Practice Screen
        if (inAppPracticePanel != null)
        {
            inAppPracticePanel.SetActive(true);
            if (practicePanelCanvasGroup != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(practicePanelCanvasGroup, 0f, 1f, fadeDuration));
            }
        }

        // 5. Hide the transition overlay
        if (transitionOverlay != null && overlayCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 1f, 0f, fadeDuration));
            transitionOverlay.SetActive(false);
        }

        // 6. Trigger initial setup on your In-App Practice Manager (if present)
        InitializePracticeLesson();
    }

    /// <summary>Placeholder hook for setting up the follow-up practice lesson.</summary>
    private void InitializePracticeLesson()
    {
        Debug.Log("In-App Practice Lesson Initialized!");
        // Call your Practice Lesson Manager's setup method here (e.g., PracticeManager.Instance.LoadNextNumber();)
    }

    /// <summary>
    /// Fades a CanvasGroup's alpha from startAlpha to endAlpha over 'duration' seconds.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null) yield break;

        float elapsedTime = 0f;
        group.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        group.alpha = endAlpha;
    }
}
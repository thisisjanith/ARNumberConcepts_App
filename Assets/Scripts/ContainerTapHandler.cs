using UnityEngine;

/// <summary>
/// River chapter (place value). Attached to each tappable 3D container in AR: Stone Chest
/// (1000), Basket (100), Pouch (10) and Lotus Seed (1). On tap it asks SeedPileController
/// whether that container may be filled, then gives visual and haptic feedback.
/// [RequireComponent(typeof(Collider))] guarantees a collider, because OnMouseDown only fires
/// on objects that have one.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ContainerTapHandler : MonoBehaviour
{
    [Header("Container Configuration")]
    public int unitValue = 1000; // Set to 1000, 100, 10, or 1 in Inspector
    // Display name used in log messages.
    public string containerName = "Stone Chest";

    [Header("References")]
    public SeedPileController controller;

    // Remembered so the pulse animation can return to the original size.
    private Vector3 originalScale;

    /// <summary>
    /// Caches the starting scale and finds the controller automatically if it was not assigned in
    /// the Inspector.
    /// </summary>
    private void Start()
    {
        originalScale = transform.localScale;
        if (controller == null)
        {
            controller = FindFirstObjectByType<SeedPileController>();
        }
    }

    /// <summary>
    /// Unity calls OnMouseDown when this object's collider is clicked; on mobile it is also raised
    /// for touches, which lets the child tap AR objects seen through the camera.
    /// </summary>
    private void OnMouseDown()
    {
        if (controller == null) return;

        // The controller owns the rules (how many of each container are allowed); this script only
        // handles input and feedback.
        bool success = controller.TryFillContainer(unitValue, containerName);

        if (success)
        {
            // Visual scale pop feedback (runs first — critical)
            StopAllCoroutines();
            StartCoroutine(AnimatePulse(originalScale * 1.2f));
            // Haptic feedback (runs after — non-critical)
            HapticFeedback.LightTap();
        }
        else
        {
            // Visual shake feedback (runs first — critical)
            StopAllCoroutines();
            StartCoroutine(AnimateShake());
            // Haptic error feedback (runs after — non-critical)
            HapticFeedback.Error();
        }
    }

    /// <summary>
    /// Coroutine: jump to a bigger scale, then ease back to normal (a "pop" effect).
    /// </summary>
    private System.Collections.IEnumerator AnimatePulse(Vector3 targetScale)
    {
        transform.localScale = targetScale;
        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            // Moving a fraction of the remaining distance each frame gives a smooth ease-out.
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Coroutine: jiggle the container randomly for about 0.15 seconds to signal "not allowed".
    /// </summary>
    private System.Collections.IEnumerator AnimateShake()
    {
        Vector3 startPos = transform.localPosition;
        for (int i = 0; i < 5; i++)
        {
            transform.localPosition = startPos + (Vector3)Random.insideUnitCircle * 0.05f;
            yield return new WaitForSeconds(0.03f);
        }
        transform.localPosition = startPos;
    }
}
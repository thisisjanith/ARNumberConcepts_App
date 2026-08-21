using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ContainerTapHandler : MonoBehaviour
{
    [Header("Container Configuration")]
    public int unitValue = 1000; // Set to 1000, 100, 10, or 1 in Inspector
    public string containerName = "Stone Chest";

    [Header("References")]
    public SeedPileController controller;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
        if (controller == null)
        {
            controller = FindFirstObjectByType<SeedPileController>();
        }
    }

    private void OnMouseDown()
    {
        if (controller == null) return;

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

    private System.Collections.IEnumerator AnimatePulse(Vector3 targetScale)
    {
        transform.localScale = targetScale;
        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
            yield return null;
        }
        transform.localScale = originalScale;
    }

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
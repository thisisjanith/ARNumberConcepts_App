using UnityEngine;
using System.Collections;

public class DinalHopper : MonoBehaviour
{
    [Header("Hop Motion")]
    public float hopDuration = 0.5f;
    public float hopHeight = 0.05f;
    public float rotationSpeed = 10f;

    [Header("Landing Offset")]
    [Tooltip("Added to the target's Y position so Dinal's feet land on top of the tile surface instead of sinking into it. Increase if he still sinks, decrease/negative if he floats above the tile.")]
    public float landingHeightOffset = 0.02f;

    private Coroutine hopCoroutine;

    public void HopTo(Transform target, System.Action onComplete = null, bool applyLandingOffset = true)
    {
        if (target == null) return;

        if (hopCoroutine != null) StopCoroutine(hopCoroutine);
        float offset = applyLandingOffset ? landingHeightOffset : 0f;
        Vector3 landingPos = target.position + Vector3.up * offset;
        hopCoroutine = StartCoroutine(HopArc(landingPos, target.rotation, onComplete));
    }

    public void HopTo(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete = null)
    {
        if (hopCoroutine != null) StopCoroutine(hopCoroutine);
        hopCoroutine = StartCoroutine(HopArc(targetPosition, targetRotation, onComplete));
    }

    private IEnumerator HopArc(Vector3 endPos, Quaternion endRot, System.Action onComplete)
    {
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = pos;

            transform.rotation = Quaternion.Slerp(transform.rotation, endRot, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        hopCoroutine = null;
        onComplete?.Invoke();
    }
}

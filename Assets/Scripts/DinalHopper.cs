using UnityEngine;
using System.Collections;

/// <summary>
/// Moves Dinal (the story's guide character) from one point to another with a hop: a straight-
/// line move plus a sine-curve height so it looks like a jump. Used by CanyonLogic to hop Dinal
/// across the stepping stones. It is a coroutine, so the movement is spread over many frames
/// instead of happening instantly.
/// </summary>
public class DinalHopper : MonoBehaviour
{
    [Header("Hop Motion")]
    // Hop duration (seconds), arc height (metres at AR scale) and how fast Dinal turns to face the
    // landing direction.
    public float hopDuration = 0.5f;
    public float hopHeight = 0.05f;
    public float rotationSpeed = 10f;

    [Header("Landing Offset")]
    [Tooltip("Added to the target's Y position so Dinal's feet land on top of the tile surface instead of sinking into it. Increase if he still sinks, decrease/negative if he floats above the tile.")]
    public float landingHeightOffset = 0.02f;

    // The hop currently running, so a new hop can cancel the old one.
    private Coroutine hopCoroutine;

    /// <summary>
    /// Hop onto a target Transform (e.g. a canyon tile). onComplete is a callback run when Dinal
    /// lands, which lets hops be chained. applyLandingOffset lifts him slightly so his feet sit on
    /// top of the tile.
    /// </summary>
    public void HopTo(Transform target, System.Action onComplete = null, bool applyLandingOffset = true)
    {
        if (target == null) return;

        // Cancel any hop in progress so two coroutines never fight over the position.
        if (hopCoroutine != null) StopCoroutine(hopCoroutine);
        float offset = applyLandingOffset ? landingHeightOffset : 0f;
        Vector3 landingPos = target.position + Vector3.up * offset;
        hopCoroutine = StartCoroutine(HopArc(landingPos, target.rotation, onComplete));
    }

    /// <summary>
    /// Overload: hop to an exact world position and rotation instead of a Transform.
    /// </summary>
    public void HopTo(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete = null)
    {
        if (hopCoroutine != null) StopCoroutine(hopCoroutine);
        hopCoroutine = StartCoroutine(HopArc(targetPosition, targetRotation, onComplete));
    }

    /// <summary>Coroutine that animates the hop frame by frame.</summary>
    private IEnumerator HopArc(Vector3 endPos, Quaternion endRot, System.Action onComplete)
    {
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        // Each frame, t goes from 0 to 1 over hopDuration seconds.
        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);

            // Lerp gives the straight path; Sin(t * PI) is 0 at the start and end and 1 in the middle, so
            // adding it to Y creates a smooth arc.
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = pos;

            // Slerp smoothly rotates toward the landing rotation.
            transform.rotation = Quaternion.Slerp(transform.rotation, endRot, rotationSpeed * Time.deltaTime);

            // Pause here until the next frame, then continue the loop.
            yield return null;
        }

        // Snap exactly to the final pose to remove any small rounding error.
        transform.position = endPos;
        transform.rotation = endRot;

        hopCoroutine = null;
        // Tell the caller we have landed (e.g. start the next hop or play the success voiceover).
        onComplete?.Invoke();
    }
}

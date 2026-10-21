using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// Lives in the first scene the app opens (OnboardingScene).
// Warms up the AR mission scene (SampleScene) in the background while showing
// a logo/progress bar: loading it forces Vuforia/asset init and script JIT to
// happen here instead of after the user taps "Let's Go" in LoginScene. Once
// warm-up finishes, SampleScene is unloaded again and LoginScene loads normally
// via its existing flow, but the expensive cold-start work is already paid for
// (OS file cache, Vuforia engine init, shader/script compilation), so the real
// LoadScene("SampleScene") triggered later by "Let's Go" is much faster.
public class OnboardingLoader : MonoBehaviour
{
    [Header("UI")]
    // Optional fill (Slider or Image with Filled type) showing preload progress 0-1
    public Image progressFill;
    // Optional text readout, e.g. "Loading... 42%"
    public TMPro.TMP_Text progressLabel;

    [Header("Timing")]
    // Keeps the splash on screen at least this long so it doesn't flash by,
    // even if warm-up finishes almost instantly.
    public float minimumDisplayTime = 2f;

    void Start()
    {
        StartCoroutine(RunOnboarding());
    }

    IEnumerator RunOnboarding()
    {
        float startTime = Time.realtimeSinceStartup;

        // Load SampleScene additively so its Awake/Start (Vuforia init, AR asset
        // loading) runs now, on top of this splash screen.
        AsyncOperation warmupLoad = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
        while (!warmupLoad.isDone)
        {
            UpdateProgressUI(warmupLoad.progress);
            yield return null;
        }

        UpdateProgressUI(1f);

        // Give Vuforia/AR components a couple of frames to finish initializing
        // after their Awake/Start before we tear the scene back down.
        yield return null;
        yield return null;

        AsyncOperation warmupUnload = SceneManager.UnloadSceneAsync("SampleScene");
        yield return warmupUnload;

        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minimumDisplayTime)
        {
            yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);
        }

        SceneManager.LoadScene("LoginScene", LoadSceneMode.Single);
    }

    void UpdateProgressUI(float progress)
    {
        if (progressFill != null) progressFill.fillAmount = progress;
        if (progressLabel != null) progressLabel.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
    }
}

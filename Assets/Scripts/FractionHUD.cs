using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gate chapter on-screen HUD that shows fraction progress: the numerator (pieces placed) over
/// the denominator 4, plus a circular ring that fills as pieces are added. It briefly shows the
/// "Numerator" / "Denominator" labels and plays narration with subtitles at each step, linking
/// the AR action to fraction vocabulary.
/// </summary>
public class FractionHUD : MonoBehaviour
{
    [Header("UI Elements")]
    // Text showing the current numerator (0 to 4).
    public TextMeshProUGUI numeratorText;
    // Circular image that fills clockwise (Radial360) to show the fraction visually.
    public Image radialFill;
    public RectTransform container; // This is your FractionHolder

    [Header("Naming Animation")]
    public GameObject numeratorLabel;   // Drag NumeratorLabel here
    public GameObject denominatorLabel; // Drag DenominatorLabel here
    public float labelDisplayTime = 1.5f;

    [Header("Transcript UI")]
    public GameObject subtitlePanel; 
    public TextMeshProUGUI subtitleText;

    [Header("Audio Settings")]
    // Narration index 0 is the intro; index n is played after the n-th piece is placed.
    public AudioSource audioSource;
    [Tooltip("Put English audio clips here")]
    public AudioClip[] narrationClipsEng;
    [Tooltip("Put Sinhala audio clips here")]
    public AudioClip[] narrationClipsSin;
    
    [Header("Transcripts (English Only)")]
    [TextArea(3, 10)]
    public string[] transcripts;

    [Header("Settings")]
    public int totalPieces = 4;
    // Pieces placed so far.
    private int currentPieces = 0;
    // Subtitles longer than this are split into several pages.
    public int maxCharsPerPage = 100;
    // Makes sure the intro narration plays only once, even if the marker is lost and found again.
    private bool introPlayed = false; 
    
    [Header("Animation")]
    public float pulseScale = 1.2f;
    public float animationDuration = 0.5f;
    public Color completionColor = Color.cyan;

    private Coroutine subtitleCoroutine;
    private bool isSinhala;

    /// <summary>
    /// Finds any missing references, sets up the radial fill and bilingual labels, and hides the
    /// HUD until the AR marker is detected.
    /// </summary>
    void Start()
    {
        // Detect language selection from LoginScene
        isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);

        // Auto-locate CircularProgress if not manually assigned
        if (radialFill == null)
        {
            Transform circularProgress = transform.Find("FractionHolder/CircularProgress");
            if (circularProgress == null)
            {
                Canvas canvas = GetComponentInChildren<Canvas>();
                if (canvas != null)
                    circularProgress = canvas.transform.Find("FractionHolder/CircularProgress");
            }
            if (circularProgress != null)
            {
                radialFill = circularProgress.GetComponent<Image>();
                Debug.Log("[FractionHUD] Auto-assigned CircularProgress Image.");
            }
            else
            {
                Debug.LogWarning("[FractionHUD] Could not find CircularProgress! Please assign radialFill in the Inspector.");
            }
        }

        // Configure radial fill for 360° progress display
        if (radialFill != null)
        {
            radialFill.type = Image.Type.Filled;
            radialFill.fillMethod = Image.FillMethod.Radial360;
            radialFill.fillOrigin = (int)Image.Origin360.Top;
            radialFill.fillClockwise = true;
        }

        // Set bilingual labels for fraction terminology
        if (numeratorLabel != null)
            numeratorLabel.GetComponent<TextMeshProUGUI>().text = isSinhala ? "ලවය" : "Numerator";

        if (denominatorLabel != null)
            denominatorLabel.GetComponent<TextMeshProUGUI>().text = isSinhala ? "හරය" : "Denominator";

        // Auto-locate FractionHolder if not assigned
        if (container == null)
        {
            Transform holder = transform.Find("FractionHolder");
            if (holder == null && transform.parent != null)
                holder = transform.parent.Find("FractionHolder");
            
            if (holder != null) container = holder.GetComponent<RectTransform>();
        }

        // Hide HUD elements until AR marker is detected
        if(numeratorLabel) numeratorLabel.SetActive(false);
        if(denominatorLabel) denominatorLabel.SetActive(false);
        if (container != null) container.gameObject.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        UpdateUI(false);
    }

    // --- AR TRACKING INTERFACE (Link to Image Target Events) ---

    /// <summary>Hooked to the Vuforia Image Target's "On Target Found" event.</summary>
    public void OnTargetFound()
    {
        // Show HUD when AR marker enters view
        if (container != null) container.gameObject.SetActive(true);
        TriggerIntro();

        // Analytics: start AR interaction timer (only records if not already running)
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StartARTimer();
    }

    /// <summary>Hooked to the Vuforia Image Target's "On Target Lost" event.</summary>
    public void OnTargetLost()
    {
        // Hide HUD when marker leaves view
        if (container != null) container.gameObject.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    // --- RESET LOGIC (Called by MissionLogic for Retry) ---

    /// <summary>Clears the HUD for a retry.</summary>
    public void ResetHUD()
    {
        // Clear piece count and replay state for retry flow
        currentPieces = 0;
        introPlayed = false;
        if(numeratorText) numeratorText.color = Color.white;
        UpdateUI(false);
    }

    // --- CORE HUD LOGIC ---

    /// <summary>Shows the HUD and plays the intro narration the first time.</summary>
    public void TriggerIntro()
    {
        // Show HUD when AR marker enters view
        if (container != null) container.gameObject.SetActive(true);

        // Play intro narration once per session
        if (!introPlayed)
        {
            PlayNarration(0);
            introPlayed = true;
        }
    }

    /// <summary>Called by StonePiece when a piece is placed.</summary>
    public void AddPiece()
    {
        // Increment counter and trigger animation + narration
        currentPieces++;
        UpdateUI(true);
        PlayNarration(currentPieces);
    }

    /// <summary>
    /// Updates the numerator text and the ring; animate = true plays the pulse animation.
    /// </summary>
    void UpdateUI(bool animate)
    {
        // Update numerator text and radial fill
        if (numeratorText != null) numeratorText.text = currentPieces.ToString();
        // Cast to float, otherwise integer division would give 0 (e.g. 1 / 4 = 0 with ints).
        float targetFill = (float)currentPieces / (float)totalPieces;
        Debug.Log($"[FractionHUD] UpdateUI -> pieces: {currentPieces}/{totalPieces}, targetFill: {targetFill}, animate: {animate}, radialFill assigned: {radialFill != null}");

        if (animate)
        {
            StartCoroutine(PulseEffect(targetFill));
        }
        else
        {
            if (radialFill != null) radialFill.fillAmount = targetFill;
        }
    }

    /// <summary>
    /// Plays the narration clip for 'index' in the chosen language, with English subtitles.
    /// </summary>
    void PlayNarration(int index)
    {
        // Play audio clip and display subtitles (English only)
        if (audioSource == null) return;
        if (audioSource.isPlaying) audioSource.Stop();

        AudioClip[] activeClips = isSinhala ? narrationClipsSin : narrationClipsEng;

        // Guard against a missing clip or an index beyond the array.
        if (index < activeClips.Length && activeClips[index] != null)
        {
            audioSource.clip = activeClips[index];
            audioSource.Play();

            if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);

            // English-only subtitles per original logic
            if (!isSinhala && index < transcripts.Length)
            {
                subtitleCoroutine = StartCoroutine(TypewriterSubtitle(transcripts[index], audioSource.clip.length));
            }
            else
            {
                if (subtitlePanel != null) subtitlePanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Coroutine: types the subtitle so it finishes in time with the audio clip.
    /// </summary>
    IEnumerator TypewriterSubtitle(string fullText, float audioLength)
    {
        // Display text character-by-character synced to audio duration
        if (subtitlePanel == null) yield break;
        subtitlePanel.SetActive(true);

        string[] pages = SplitTextIntoPages(fullText, maxCharsPerPage);
        // Share the audio time equally between the pages.
        float timePerPage = audioLength / pages.Length;

        foreach (string pageText in pages)
        {
            subtitleText.text = "";
            // Type during 85% of each page's time, leaving a short pause before the next page.
            float timePerChar = (timePerPage * 0.85f) / pageText.Length;

            foreach (char letter in pageText.ToCharArray())
            {
                subtitleText.text += letter;
                yield return new WaitForSeconds(timePerChar);
            }
            yield return new WaitForSeconds(0.6f);
        }

        yield return new WaitForSeconds(1.0f);
        subtitlePanel.SetActive(false);
    }

    /// <summary>
    /// Coroutine: pulses the HUD and animates the ring to the new fill amount.
    /// </summary>
    IEnumerator PulseEffect(float targetFill)
    {
        // Animate container scale and radial fill on piece placement
        if (container == null) yield break;
        if (radialFill == null)
        {
            Debug.LogError("[FractionHUD] radialFill is NULL — cannot animate CircularProgress! Please assign it in the Inspector.");
            yield break;
        }

        // Show terminology labels during animation
        if(numeratorLabel) numeratorLabel.SetActive(true);
        if(denominatorLabel) denominatorLabel.SetActive(true);

        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * pulseScale;
        float elapsed = 0;
        float startFill = radialFill.fillAmount;

        Debug.Log($"[FractionHUD] PulseEffect: startFill={startFill}, targetFill={targetFill}");

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;
            // The sine curve goes 0 -> 1 -> 0, so the HUD grows and then shrinks back in one smooth pulse.
            float curve = Mathf.Sin(percent * Mathf.PI);
            container.localScale = Vector3.Lerp(originalScale, targetScale, curve);
            radialFill.fillAmount = Mathf.Lerp(startFill, targetFill, percent);
            yield return null;
        }

        container.localScale = originalScale;
        radialFill.fillAmount = targetFill;

        Debug.Log($"[FractionHUD] PulseEffect complete. Final fillAmount={radialFill.fillAmount}");

        // Hide labels after brief display
        yield return new WaitForSeconds(labelDisplayTime);
        if(numeratorLabel) numeratorLabel.SetActive(false);
        if(denominatorLabel) denominatorLabel.SetActive(false);

        // Change numerator color to cyan when all pieces collected
        if (currentPieces >= totalPieces)
        {
            if (numeratorText != null) numeratorText.color = completionColor;
        }
    }

    /// <summary>
    /// Splits text into pages of at most maxChars characters, breaking only between words.
    /// </summary>
    string[] SplitTextIntoPages(string text, int maxChars)
    {
        // Break long text into pages for readable subtitle display
        List<string> pages = new List<string>();
        string[] words = text.Split(' ');
        string currentPage = "";

        foreach (string word in words)
        {
            if ((currentPage + word).Length > maxChars)
            {
                pages.Add(currentPage.Trim());
                currentPage = word + " ";
            }
            else
            {
                currentPage += word + " ";
            }
        }

        if (!string.IsNullOrEmpty(currentPage))
            pages.Add(currentPage.Trim());

        return pages.ToArray();
    }
}
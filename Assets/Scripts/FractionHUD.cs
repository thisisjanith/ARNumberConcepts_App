using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FractionHUD : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI numeratorText;
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
    private int currentPieces = 0;
    public int maxCharsPerPage = 100;
    private bool introPlayed = false; 
    
    [Header("Animation")]
    public float pulseScale = 1.2f;
    public float animationDuration = 0.5f;
    public Color completionColor = Color.cyan;

    private Coroutine subtitleCoroutine;
    private bool isSinhala;

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

    public void OnTargetFound()
    {
        // Show HUD when AR marker enters view
        if (container != null) container.gameObject.SetActive(true);
        TriggerIntro();

        // Analytics: start AR interaction timer (only records if not already running)
        if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.StartARTimer();
    }

    public void OnTargetLost()
    {
        // Hide HUD when marker leaves view
        if (container != null) container.gameObject.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    // --- RESET LOGIC (Called by MissionLogic for Retry) ---

    public void ResetHUD()
    {
        // Clear piece count and replay state for retry flow
        currentPieces = 0;
        introPlayed = false;
        if(numeratorText) numeratorText.color = Color.white;
        UpdateUI(false);
    }

    // --- CORE HUD LOGIC ---

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

    public void AddPiece()
    {
        // Increment counter and trigger animation + narration
        currentPieces++;
        UpdateUI(true);
        PlayNarration(currentPieces);
    }

    void UpdateUI(bool animate)
    {
        // Update numerator text and radial fill
        if (numeratorText != null) numeratorText.text = currentPieces.ToString();
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

    void PlayNarration(int index)
    {
        // Play audio clip and display subtitles (English only)
        if (audioSource == null) return;
        if (audioSource.isPlaying) audioSource.Stop();

        AudioClip[] activeClips = isSinhala ? narrationClipsSin : narrationClipsEng;

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

    IEnumerator TypewriterSubtitle(string fullText, float audioLength)
    {
        // Display text character-by-character synced to audio duration
        if (subtitlePanel == null) yield break;
        subtitlePanel.SetActive(true);

        string[] pages = SplitTextIntoPages(fullText, maxCharsPerPage);
        float timePerPage = audioLength / pages.Length;

        foreach (string pageText in pages)
        {
            subtitleText.text = "";
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
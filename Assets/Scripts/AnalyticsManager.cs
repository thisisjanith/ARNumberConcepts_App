using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton analytics manager that persists across all scenes.
/// Sends session data to Firebase Realtime Database via REST API.
/// Also saves a local CSV backup on the device.
///
/// Setup: Paste your Firebase Realtime Database URL in the Inspector.
/// Example: https://your-project-id-default-rtdb.firebaseio.com/
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────
    public static AnalyticsManager Instance { get; private set; }

    // ─── Configuration ───────────────────────────────────────────
    [Header("Firebase Settings")]
    [Tooltip("Your Firebase Realtime Database URL (e.g. https://xxx-default-rtdb.firebaseio.com/)")]
    public string firebaseDatabaseURL = "PASTE_YOUR_FIREBASE_URL_HERE";

    // ─── Session Data ────────────────────────────────────────────
    private string sessionLanguage = "English";

    // 1. AR Interaction Time
    private float arTimerStart = 0f;
    private float arTotalTime = 0f;
    private bool arTimerRunning = false;

    // 1b. Quiz Interaction Time
    private float quizTimerStart = 0f;
    private float quizTotalTime = 0f;
    private bool quizTimerRunning = false;

    // 2. Quiz Error Rate
    private int totalQuizErrors = 0;
    private int totalQuizQuestions = 0;

    // 3. Session Duration
    private float sessionStartTime = 0f;

    // 4. Retry Count
    private int totalRetries = 0;

    // Guards
    private bool sessionSubmitted = false;
    private string csvFilePath;

    // Device identity (lets us tell which device an entry came from)
    private string deviceId;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        deviceId = SystemInfo.deviceUniqueIdentifier;

        // Local CSV backup
        csvFilePath = Path.Combine(Application.persistentDataPath, "AR_NumberConcepts_analytics.csv");
        if (!File.Exists(csvFilePath))
        {
            File.WriteAllText(csvFilePath,
                "Timestamp,Device ID,Language,AR Time (s),Quiz Time (s),Interaction Time (s),Quiz Errors,Quiz Total Questions,Quiz Error Rate,Session Duration (s),Retry Count\n");
        }

        ResetSession();
    }

    private void Start()
    {
        // On startup, flush any Firebase data that was saved offline (e.g. if the app closed unexpectedly)
        string cached = PlayerPrefs.GetString("FirebaseOfflineCache", "");
        if (!string.IsNullOrEmpty(cached))
        {
            string[] entries = cached.Split(new string[] { "|||" }, System.StringSplitOptions.RemoveEmptyEntries);
            Debug.Log($"[Analytics] Flushing {entries.Length} offline Firebase sessions...");

            // Clear cache before uploading; PostToFirebase re-saves offline anything
            // that fails to send, so failed entries aren't lost, only successful ones are dropped from the cache.
            PlayerPrefs.DeleteKey("FirebaseOfflineCache");
            PlayerPrefs.Save();

            foreach (string entry in entries)
            {
                StartCoroutine(PostToFirebase(entry));
            }
        }
    }

    // ─── Public API ──────────────────────────────────────────────

    public void SetLanguage(int languageID)
    {
        sessionLanguage = (languageID == 0) ? "English" : "Sinhala";
        sessionStartTime = Time.realtimeSinceStartup;
        Debug.Log($"[Analytics] Language: {sessionLanguage} | Session started.");
    }

    public void StartARTimer()
    {
        if (arTimerRunning) return;
        arTimerStart = Time.realtimeSinceStartup;
        arTimerRunning = true;
    }

    public void StopARTimer()
    {
        if (!arTimerRunning) return;
        float elapsed = Time.realtimeSinceStartup - arTimerStart;
        arTotalTime += elapsed;
        arTimerRunning = false;
    }

    public void StartQuizTimer()
    {
        if (quizTimerRunning) return;
        quizTimerStart = Time.realtimeSinceStartup;
        quizTimerRunning = true;
    }

    public void StopQuizTimer()
    {
        if (!quizTimerRunning) return;
        float elapsed = Time.realtimeSinceStartup - quizTimerStart;
        quizTotalTime += elapsed;
        quizTimerRunning = false;
    }

    public void RecordQuizError()
    {
        totalQuizErrors++;
    }

    public void AddQuizQuestions(int count)
    {
        totalQuizQuestions += count;
    }

    public void RecordRetry()
    {
        totalRetries++;
    }

    public void SubmitSession(System.Action onComplete = null)
    {
        if (sessionSubmitted)
        {
            onComplete?.Invoke();
            return;
        }
        if (arTimerRunning) StopARTimer();
        if (quizTimerRunning) StopQuizTimer();
        sessionSubmitted = true;

        if (HasActivity())
        {
            // Save locally as backup
            SaveToCSV();

            // Send to Firebase
            string json = BuildJSON();
            StartCoroutine(PostToFirebase(json, onComplete));
        }
        else
        {
            Debug.Log("[Analytics] Session had no interaction, skipping submit.");
            onComplete?.Invoke();
        }

        // Reset the session so another activity can be played immediately
        // but preserve the selected language for the new session
        string currentLang = sessionLanguage;
        ResetSession();
        sessionLanguage = currentLang;
    }

    // Sessions where the user left before any real interaction happened
    // (e.g. backed out before the AR target tracked or the quiz loaded) are
    // just noise and not worth a Firebase/CSV entry.
    private bool HasActivity()
    {
        return arTotalTime > 0f || quizTotalTime > 0f
            || totalQuizQuestions > 0 || totalRetries > 0;
    }

    public void ResetSession()
    {
        sessionLanguage = "English";
        arTimerStart = 0f;
        arTotalTime = 0f;
        arTimerRunning = false;
        quizTimerStart = 0f;
        quizTotalTime = 0f;
        quizTimerRunning = false;
        totalQuizErrors = 0;
        totalQuizQuestions = 0;
        totalRetries = 0;
        sessionStartTime = Time.realtimeSinceStartup;
        sessionSubmitted = false;
    }

    // ─── Firebase POST ───────────────────────────────────────────

    private string BuildJSON()
    {
        float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
        float interactionTime = arTotalTime + quizTotalTime;
        float errorRate = (totalQuizQuestions > 0)
            ? (float)totalQuizErrors / totalQuizQuestions
            : 0f;
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        return "{"
            + $"\"timestamp\":\"{timestamp}\","
            + $"\"device_id\":\"{deviceId}\","
            + $"\"language\":\"{sessionLanguage}\","
            + $"\"ar_time\":{arTotalTime:F1},"
            + $"\"quiz_time\":{quizTotalTime:F1},"
            + $"\"interaction_time\":{interactionTime:F1},"
            + $"\"quiz_errors\":{totalQuizErrors},"
            + $"\"quiz_total_questions\":{totalQuizQuestions},"
            + $"\"quiz_error_rate\":{errorRate:F2},"
            + $"\"session_duration\":{sessionDuration:F1},"
            + $"\"retry_count\":{totalRetries}"
            + "}";
    }

    private IEnumerator PostToFirebase(string json, System.Action onComplete = null)
    {
        Debug.Log($"[Analytics] Sending to Firebase:\n{json}");

        if (string.IsNullOrEmpty(firebaseDatabaseURL)
            || firebaseDatabaseURL == "PASTE_YOUR_FIREBASE_URL_HERE")
        {
            Debug.LogWarning("[Analytics] Firebase URL not set!");
            onComplete?.Invoke();
            yield break;
        }

        // Firebase REST API: POST to /sessions.json creates a new entry
        string url = firebaseDatabaseURL.TrimEnd('/') + "/sessions.json";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Analytics] Firebase OK: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogWarning($"[Analytics] Firebase failed: {request.error}. Saving offline.");
                SaveFirebaseOffline(json);
            }
        }

        onComplete?.Invoke();
    }

    private void SaveFirebaseOffline(string json)
    {
        string existing = PlayerPrefs.GetString("FirebaseOfflineCache", "");
        if (!string.IsNullOrEmpty(existing)) existing += "|||";
        existing += json;
        PlayerPrefs.SetString("FirebaseOfflineCache", existing);
        PlayerPrefs.Save();
        Debug.Log("[Analytics] Firebase payload saved offline.");
    }

    // ─── Local CSV Backup ────────────────────────────────────────

    private void SaveToCSV()
    {
        float sessionDuration = Time.realtimeSinceStartup - sessionStartTime;
        float interactionTime = arTotalTime + quizTotalTime;
        float errorRate = (totalQuizQuestions > 0)
            ? (float)totalQuizErrors / totalQuizQuestions
            : 0f;
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string row = $"{timestamp},{deviceId},{sessionLanguage},{arTotalTime:F1},{quizTotalTime:F1},{interactionTime:F1},{totalQuizErrors},{totalQuizQuestions},{errorRate:F2},{sessionDuration:F1},{totalRetries}\n";

        try { File.AppendAllText(csvFilePath, row); }
        catch (System.Exception ex) { Debug.LogError($"[Analytics] CSV error: {ex.Message}"); }
    }

    private void OnApplicationQuit()
    {
        if (!sessionSubmitted)
        {
            if (arTimerRunning) StopARTimer();
            if (quizTimerRunning) StopQuizTimer();
            sessionSubmitted = true;

            if (HasActivity())
            {
                SaveToCSV();

                // Cannot run a Coroutine here because the app is closing. Save offline immediately.
                string json = BuildJSON();
                SaveFirebaseOffline(json);
                Debug.Log("[Analytics] App Quit! Session saved offline for next launch.");
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !sessionSubmitted)
        {
            if (arTimerRunning) StopARTimer();
            if (quizTimerRunning) StopQuizTimer();
            sessionSubmitted = true;

            if (HasActivity())
            {
                SaveToCSV();

                // Save offline immediately as a guarantee in case the OS kills the app
                // before the request below can finish (backgrounded apps can be
                // suspended at any time, with no further callbacks).
                string json = BuildJSON();
                SaveFirebaseOffline(json);
                Debug.Log("[Analytics] App Paused! Session saved offline.");

                // Also try to send it right now - Unity keeps running for a short
                // while after pausing, so this often succeeds before the OS suspends
                // the app, meaning the data reaches Firebase without waiting for the
                // user to relaunch. A duplicate send if the app resumes is fine since
                // Firebase push IDs never overwrite existing entries.
                StartCoroutine(PostToFirebase(json));
            }
        }
    }
}

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
    // Singleton: one shared instance that any script can reach through AnalyticsManager.Instance.
    public static AnalyticsManager Instance { get; private set; }

    // ─── Configuration ───────────────────────────────────────────
    [Header("Firebase Settings")]
    [Tooltip("Your Firebase Realtime Database URL (e.g. https://xxx-default-rtdb.firebaseio.com/)")]
    public string firebaseDatabaseURL = "PASTE_YOUR_FIREBASE_URL_HERE";

    // ─── Session Data ────────────────────────────────────────────
    // Language of the current session ("English" or "Sinhala").
    private string sessionLanguage = "English";

    // 1. AR Interaction Time
    // Each timer stores its start time, the accumulated total and whether it is running.
    // Time.realtimeSinceStartup is used so the timing is not affected by Time.timeScale.
    private float arTimerStart = 0f;
    private float arTotalTime = 0f;
    private bool arTimerRunning = false;

    // 1b. Quiz Interaction Time
    private float quizTimerStart = 0f;
    private float quizTotalTime = 0f;
    private bool quizTimerRunning = false;

    // 2. Quiz Error Rate
    // Error rate = errors / questions, calculated when the session is submitted.
    private int totalQuizErrors = 0;
    private int totalQuizQuestions = 0;

    // 3. Session Duration
    private float sessionStartTime = 0f;

    // 4. Retry Count
    private int totalRetries = 0;

    // Guards
    // sessionSubmitted stops the same session being sent twice; csvFilePath is the local backup
    // file.
    private bool sessionSubmitted = false;
    private string csvFilePath;

    // Device identity (lets us tell which device an entry came from)
    private string deviceId;

    // ─── Unity Lifecycle ─────────────────────────────────────────

    /// <summary>
    /// Awake: enforce the singleton and keep this object alive across scene changes.
    /// </summary>
    private void Awake()
    {
        // If another AnalyticsManager already exists (e.g. LoginScene loaded again), destroy this
        // duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad keeps this GameObject alive when scenes change, so the timers keep
        // counting for the whole session.
        DontDestroyOnLoad(gameObject);

        // Device identifier (not the child's name), so entries from different test devices can be told
        // apart.
        deviceId = SystemInfo.deviceUniqueIdentifier;

        // Local CSV backup
        csvFilePath = Path.Combine(Application.persistentDataPath, "AR_NumberConcepts_analytics.csv");
        // Write the CSV header row only the first time the file is created.
        if (!File.Exists(csvFilePath))
        {
            File.WriteAllText(csvFilePath,
                "Timestamp,Device ID,Language,AR Time (s),Quiz Time (s),Interaction Time (s),Quiz Errors,Quiz Total Questions,Quiz Error Rate,Session Duration (s),Retry Count\n");
        }

        ResetSession();
    }

    /// <summary>Start: resend any sessions that were saved offline earlier.</summary>
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

    /// <summary>
    /// Called by LanguageSwitcher: records the language and starts the session clock.
    /// </summary>
    public void SetLanguage(int languageID)
    {
        sessionLanguage = (languageID == 0) ? "English" : "Sinhala";
        sessionStartTime = Time.realtimeSinceStartup;
        Debug.Log($"[Analytics] Language: {sessionLanguage} | Session started.");
    }

    /// <summary>
    /// Starts timing AR interaction. Ignored if already running, so repeated target-found events do
    /// not reset it.
    /// </summary>
    public void StartARTimer()
    {
        if (arTimerRunning) return;
        arTimerStart = Time.realtimeSinceStartup;
        arTimerRunning = true;
    }

    /// <summary>Stops the AR timer and adds the elapsed time to the total.</summary>
    public void StopARTimer()
    {
        if (!arTimerRunning) return;
        float elapsed = Time.realtimeSinceStartup - arTimerStart;
        arTotalTime += elapsed;
        arTimerRunning = false;
    }

    /// <summary>Starts timing quiz interaction (ignored if already running).</summary>
    public void StartQuizTimer()
    {
        if (quizTimerRunning) return;
        quizTimerStart = Time.realtimeSinceStartup;
        quizTimerRunning = true;
    }

    /// <summary>Stops the quiz timer and adds the elapsed time to the total.</summary>
    public void StopQuizTimer()
    {
        if (!quizTimerRunning) return;
        float elapsed = Time.realtimeSinceStartup - quizTimerStart;
        quizTotalTime += elapsed;
        quizTimerRunning = false;
    }

    /// <summary>Called by the quizzes on every wrong answer.</summary>
    public void RecordQuizError()
    {
        totalQuizErrors++;
    }

    /// <summary>Called by each quiz when it starts, with its number of questions.</summary>
    public void AddQuizQuestions(int count)
    {
        totalQuizQuestions += count;
    }

    /// <summary>Called when the child presses Retry in an activity.</summary>
    public void RecordRetry()
    {
        totalRetries++;
    }

    /// <summary>
    /// Ends the session: stops the timers, saves a CSV row, sends the data to Firebase, then resets
    /// for the next session. Called from the Home buttons.
    /// </summary>
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

    /// <summary>
    /// Builds the JSON body by hand with string interpolation. :F1 and :F2 format numbers to 1 or 2
    /// decimal places.
    /// </summary>
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

    /// <summary>
    /// Coroutine: sends the JSON with an HTTP POST using UnityWebRequest. A coroutine is used so
    /// waiting for the network does not freeze the app. If sending fails, the data is cached
    /// offline and retried on the next launch.
    /// </summary>
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
            // Attach the JSON body, a buffer for the server's reply and a 10 second timeout.
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            // Wait (without blocking the game) until the request finishes.
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

    /// <summary>
    /// Adds a JSON payload to an offline queue stored in PlayerPrefs. Entries are separated by
    /// "|||" and are sent from Start() on the next launch.
    /// </summary>
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

    /// <summary>
    /// Appends one row to the local CSV file in the app's persistent data folder.
    /// </summary>
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

    /// <summary>
    /// Called by Unity when the app is closing: saves an unsent session offline.
    /// </summary>
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

    /// <summary>
    /// Called by Unity when the app goes to the background (pauseStatus = true) or comes back.
    /// Mobile operating systems may kill background apps, so the session is saved here.
    /// </summary>
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

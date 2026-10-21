using UnityEngine;

/// <summary>
/// Centralized haptic feedback utility for AR interactions.
/// 
/// Uses Handheld.Vibrate() (Unity's built-in) which automatically adds
/// the VIBRATE permission to the Android manifest at build time.
/// No custom AndroidManifest.xml needed.
/// 
/// On Android 8.0+ (API 26+), also uses the modern VibrationEffect API
/// for custom vibration durations. Falls back to Handheld.Vibrate() if
/// the native API is unavailable.
/// 
/// All calls are wrapped in try-catch so they NEVER break game logic.
/// </summary>
public static class HapticFeedback
{
#if UNITY_ANDROID && !UNITY_EDITOR
    // Cached Android Vibrator system service (a Java object reached through JNI).
    private static AndroidJavaObject _vibrator;
    // Init runs only once per app session.
    private static bool _initialized;
    private static bool _hasNativeVibrator;
    // Android API level (e.g. 26 = Android 8.0); -1 until it has been read.
    private static int _sdkInt = -1;

    /// <summary>
    /// Reads the Android version and gets the Vibrator system service through Unity's
    /// AndroidJavaClass / AndroidJavaObject bridge. Done lazily on the first vibration.
    /// </summary>
    private static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            // Read Build.VERSION.SDK_INT to know which vibration API is available.
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _sdkInt = version.GetStatic<int>("SDK_INT");
            }

            // Get the current Android Activity, then ask it for the "vibrator" system service.
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                _hasNativeVibrator = _vibrator != null && _vibrator.Call<bool>("hasVibrator");
            }

            Debug.Log($"[HapticFeedback] Ready — SDK:{_sdkInt}, hasVibrator:{_hasNativeVibrator}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticFeedback] Native init failed, using Handheld.Vibrate(): " + e.Message);
            _hasNativeVibrator = false;
        }
    }
#endif

    /// <summary>Light haptic — tapping AR objects.</summary>
    public static void LightTap()
    {
        TriggerVibration(25);
    }

    /// <summary>Medium haptic — drag interactions.</summary>
    public static void MediumTap()
    {
        TriggerVibration(40);
    }

    /// <summary>Strong haptic — correct answers and completions.</summary>
    public static void Success()
    {
        TriggerVibration(80);
    }

    /// <summary>Short haptic — wrong answers or rejected actions.</summary>
    public static void Error()
    {
        TriggerVibration(30);
    }

    /// <summary>
    /// Shared implementation. The #if blocks choose platform code at compile time, so Android, iOS
    /// and the Unity Editor each get their own version; in the Editor it only logs a message.
    /// </summary>
    private static void TriggerVibration(long milliseconds)
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Init();

            // Try modern VibrationEffect API first (Android 8.0+ / API 26+)
            if (_hasNativeVibrator && _vibrator != null && _sdkInt >= 26)
            {
                try
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", milliseconds, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                        return; // Success — don't fall through
                    }
                }
                catch (System.Exception)
                {
                    // VibrationEffect failed — fall through to Handheld.Vibrate()
                }
            }

            // Fallback: Unity built-in (always works, triggers permission auto-add)
            Handheld.Vibrate();

#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            Debug.Log($"[HapticFeedback] Vibrate {milliseconds}ms (Editor)");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticFeedback] " + e.Message);
        }
    }
}

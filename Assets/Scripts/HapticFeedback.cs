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
    private static AndroidJavaObject _vibrator;
    private static bool _initialized;
    private static bool _hasNativeVibrator;
    private static int _sdkInt = -1;

    private static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _sdkInt = version.GetStatic<int>("SDK_INT");
            }

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

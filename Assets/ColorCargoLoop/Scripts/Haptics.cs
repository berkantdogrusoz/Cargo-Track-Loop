using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Android haptik (titresim) yardimcisi.
    /// SADECE GERCEK CIHAZDA calisir - Unity editorunde no-op (hissedilmez).
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdk;
        private static bool _init;

        private static void EnsureInit()
        {
            if (_init) return;
            _init = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using (var ver = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _sdk = ver.GetStatic<int>("SDK_INT");
                }
            }
            catch { _vibrator = null; }
        }
#endif

        /// <summary>Tek seferlik titresim. amplitude: -1 = varsayilan, 1..255 = guc.</summary>
        public static void Vibrate(long milliseconds, int amplitude = -1)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInit();
            if (_vibrator == null) return;
            try
            {
                if (_sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        int amp = amplitude < 0 ? -1 : Mathf.Clamp(amplitude, 1, 255);
                        var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amp);
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch { }
#endif
        }

        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInit();
            if (_vibrator == null) return;
            try { _vibrator.Call("cancel"); } catch { }
#endif
        }
    }
}

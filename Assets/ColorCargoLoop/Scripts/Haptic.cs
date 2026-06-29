using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// MEKANIK: kisa telefon titresimi ("tik"). Kup dolarken vb. anlarda cagrilir.
    /// - Android: Vibrator servisi ile KISA titresim (API 26+ VibrationEffect, altinda klasik vibrate).
    /// - Editor / desteklenmeyen platform: sessiz (no-op).
    /// Kod sadece mekanik; sahneye obje EKLEMEZ.
    /// </summary>
    public static class Haptic
    {
        static float lastTime = -10f;

        // Ayarlar paneli: titresim ac-kapa (PlayerPrefs'e kalici yazilir)
        const string PrefHapticsOn = "set_haptics_on";
        static bool? _enabled;
        public static bool Enabled
        {
            get { if (_enabled == null) _enabled = PlayerPrefs.GetInt(PrefHapticsOn, 1) == 1; return _enabled.Value; }
        }
        public static void SetEnabled(bool on) { _enabled = on; PlayerPrefs.SetInt(PrefHapticsOn, on ? 1 : 0); PlayerPrefs.Save(); }
        public static bool Toggle() { SetEnabled(!Enabled); return Enabled; }

        /// <summary>Kisa tik titresim. minInterval: ardarda cagrilarda en kisa aralik (sn) -> spam onler.</summary>
        public static void Light(float minInterval = 0.06f, long ms = 24, int amplitude = 135)
        {
            if (!Enabled) return;
            if (Time.unscaledTime - lastTime < minInterval) return;
            lastTime = Time.unscaledTime;
            Vibrate(ms, amplitude);
        }

        static void Vibrate(long ms, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;

                    int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                    if (sdk >= 26)
                    {
                        using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            int amp = Mathf.Clamp(amplitude, 1, 255);
                            using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                            {
                                vibrator.Call("vibrate", effect);
                            }
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", ms);
                    }
                }
            }
            catch
            {
                // cihaz titresimi desteklemiyor olabilir -> sessizce gec
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS'ta hafif haptic icin plugin gerekir; Handheld.Vibrate kaba ama calisir (Android hedefimiz oldugu icin yedek).
            Handheld.Vibrate();
#else
            // Editor / PC: titresim yok (sessiz)
#endif
        }
    }
}

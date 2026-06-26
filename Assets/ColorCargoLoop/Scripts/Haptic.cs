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

        /// <summary>Kisa tik titresim. minInterval: ardarda cagrilarda en kisa aralik (sn) -> spam onler.</summary>
        public static void Light(float minInterval = 0.06f, long ms = 24, int amplitude = 135)
        {
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

using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// TikTok Business (Events) SDK baslatici - UA reklam algoritmasi icin.
    /// Baslatilinca SDK kurulum (install) ve acilis (launch) event'lerini OTOMATIK gonderir;
    /// TikTok Ads Manager oyunculari kampanyalara baglayabilir ve optimize eder.
    ///
    /// Kurulum: TikTok App ID = 7661665765558337556, App ID = paket adi (com.Altare.CandyCargo).
    /// App Secret client'a GOMULMEZ (o sunucu Events API icindir).
    /// Bagimliliklar EDM4U ile: Assets/ColorCargoLoop/Editor/TikTokDependencies.xml.
    ///
    /// Ozel event gondermek icin: TikTokTracking.Track("purchase") gibi cagir.
    /// </summary>
    public static class TikTokTracking
    {
        const string AppId = "com.Altare.CandyCargo";     // uygulama paket adi (TikTok "App ID")
        const string TTAppId = "7661665765558337556";     // TikTok Uygulama Kimligi
        const string SdkClass = "com.tiktok.TikTokBusinessSdk";

        static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (initialized) return;
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = up.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) { Debug.LogWarning("[TikTok] activity yok, init atlandi"); return; }
                    using (var appContext = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    using (var config = new AndroidJavaObject(SdkClass + "$TTConfig", appContext))
                    {
                        // fluent builder: donen TTConfig ayni obje -> sarmalayicilari birak
                        config.Call<AndroidJavaObject>("setAppId", AppId)?.Dispose();
                        config.Call<AndroidJavaObject>("setTTAppId", TTAppId)?.Dispose();
                        using (var sdk = new AndroidJavaClass(SdkClass))
                        {
                            sdk.CallStatic("initializeSdk", config);
                            sdk.CallStatic("startTrack"); // otomatik baslamadiysa ag islemlerini baslat (idempotent)
                        }
                    }
                }
                initialized = true;
                Debug.Log("[TikTok] SDK baslatildi (TTAppId=" + TTAppId + ") - install/launch otomatik izlenir");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TikTok] init hatasi (oyun etkilenmez): " + e.Message);
            }
#endif
        }

        /// <summary>
        /// Ozel event gonderir (or. "purchase", "level_complete"). Editorde/Android disinda no-op.
        /// (SDK'nin deprecated trackEvent(String) yolu; ileride TTBaseEvent'e gecilebilir.)
        /// </summary>
        public static void Track(string eventName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrEmpty(eventName)) return;
            try
            {
                using (var sdk = new AndroidJavaClass(SdkClass))
                    sdk.CallStatic("trackEvent", eventName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TikTok] track hatasi ('" + eventName + "'): " + e.Message);
            }
#endif
        }
    }
}

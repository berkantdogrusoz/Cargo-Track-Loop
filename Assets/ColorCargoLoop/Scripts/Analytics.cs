using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Decoupled analytics/crash facade. Oyun kodu SADECE Analytics.LevelWin(...) gibi semantik metotlari cagirir;
    /// Firebase detayini bilmez. Firebase hazir degilse (editor / init bitmemis) sessizce no-op.
    /// Init otomatik (RuntimeInitializeOnLoadMethod) -> sahneye obje eklemeye gerek yok.
    /// RemoteConfig ayri pakette; import edilince ayri servis eklenecek.
    /// </summary>
    public static class Analytics
    {
        static bool ready;
        public static bool Ready { get { return ready; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[Analytics] Firebase init hata: " + task.Exception);
                    return;
                }
                if (task.Result == DependencyStatus.Available)
                {
                    ready = true;
                    Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
                    Debug.Log("[Analytics] Firebase HAZIR");
                    RemoteConfigService.Initialize();   // Firebase hazir -> remote config fetch+activate
                }
                else
                {
                    Debug.LogWarning("[Analytics] Firebase bagimliliklari hazir degil: " + task.Result);
                }
            });
        }

        // ---- Oyun-yuzu semantik event'ler (oyun BUNLARI cagirir) ----
        public static void LevelStart(int level)
        {
            if (!ready) return;
            FirebaseAnalytics.LogEvent("level_start", "level", level);
        }

        public static void LevelWin(int level, int moves)
        {
            if (!ready) return;
            FirebaseAnalytics.LogEvent("level_win", new[]
            {
                new Parameter("level", level),
                new Parameter("moves", moves)
            });
        }

        public static void LevelLose(int level, int moves)
        {
            if (!ready) return;
            FirebaseAnalytics.LogEvent("level_lose", new[]
            {
                new Parameter("level", level),
                new Parameter("moves", moves)
            });
        }

        public static void BoosterUsed(string boosterName, int level)
        {
            if (!ready) return;
            FirebaseAnalytics.LogEvent("booster_used", new[]
            {
                new Parameter("booster", string.IsNullOrEmpty(boosterName) ? "unknown" : boosterName),
                new Parameter("level", level)
            });
        }

        // Esnek genel event (parametresiz)
        public static void Event(string name)
        {
            if (!ready || string.IsNullOrEmpty(name)) return;
            FirebaseAnalytics.LogEvent(name);
        }

        // Crashlytics breadcrumb (cokme oncesi iz birakir)
        public static void Breadcrumb(string message)
        {
            if (ready && !string.IsNullOrEmpty(message)) Crashlytics.Log(message);
        }
    }
}

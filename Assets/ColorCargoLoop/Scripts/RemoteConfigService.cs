using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Decoupled Remote Config servisi. Firebase HAZIR olunca (Analytics.Init cagirir) defaults set + fetch+activate yapar.
    /// Oyun kodu RemoteConfigService.GetInt("anahtar", varsayilan) gibi okur; Firebase yoksa/cekemezse varsayilan doner.
    /// Anahtarlari Firebase Console > Remote Config'de tanimla; burada Defaults'a koymak sart degil (def parametresi yeter).
    /// </summary>
    public static class RemoteConfigService
    {
        static bool ready;
        public static bool Ready { get { return ready; } }

        // Istege bagli baslangic varsayilanlari (Console'dan deger gelene kadar bunlar kullanilir)
        static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
        {
            // ornek: { "ad_interval", 3 }, { "starting_coins", 625 }
        };

        // Analytics.Init, Firebase HAZIR olunca cagirir
        public static void Initialize()
        {
            FirebaseRemoteConfig rc = FirebaseRemoteConfig.DefaultInstance;
            // Varsayilan fetch araligi 12 SAAT: zorunlu guncelleme gibi degisikliklerin oyunculara
            // ulasmasi cok gecikiyordu -> 1 saate cekildi (kota-guvenli, Firebase onerilen araliklarda).
            ConfigSettings settings = new ConfigSettings { MinimumFetchIntervalInMilliseconds = 3600000 };
            rc.SetConfigSettingsAsync(settings).ContinueWithOnMainThread(__ =>
            {
                rc.SetDefaultsAsync(Defaults).ContinueWithOnMainThread(_ =>
                {
                    rc.FetchAndActivateAsync().ContinueWithOnMainThread(task =>
                    {
                        ready = true; // fetch basarisiz olsa bile defaults ile devam
                        if (task.IsFaulted || task.IsCanceled)
                            Debug.LogWarning("[RemoteConfig] fetch hata (defaults ile devam): " + task.Exception);
                        else
                            Debug.Log("[RemoteConfig] HAZIR (activated=" + task.Result + ")");
                    });
                });
            });
        }

        // ---- Okuyucular: anahtar remote/default'ta yoksa veya hazir degilse 'def' doner ----
        public static int GetInt(string key, int def)
        {
            ConfigValue v;
            return TryGet(key, out v) ? (int)v.LongValue : def;
        }

        public static float GetFloat(string key, float def)
        {
            ConfigValue v;
            return TryGet(key, out v) ? (float)v.DoubleValue : def;
        }

        public static bool GetBool(string key, bool def)
        {
            ConfigValue v;
            return TryGet(key, out v) ? v.BooleanValue : def;
        }

        public static string GetString(string key, string def)
        {
            ConfigValue v;
            if (!TryGet(key, out v)) return def;
            string s = v.StringValue;
            return string.IsNullOrEmpty(s) ? def : s;
        }

        static bool TryGet(string key, out ConfigValue value)
        {
            value = default(ConfigValue);
            if (!ready || string.IsNullOrEmpty(key)) return false;
            value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            // StaticValue = ne remote'ta ne default'ta var -> caller'in 'def'i kullanilsin
            return value.Source != ValueSource.StaticValue;
        }
    }
}

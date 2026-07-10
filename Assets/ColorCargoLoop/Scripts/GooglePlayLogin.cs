using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace ColorCargoLoop
{
    /// <summary>
    /// Google Play Games girisi. Oyun ACILISINDA otomatik cagrilir (sahneye obje eklemeye gerek yok;
    /// RuntimeInitializeOnLoadMethod kendisi tetikler).
    /// - Cihazda Play Games yuklu + oturum acik -> SESSIZ giris (OS kendi "hos geldin" baloncugunu gosterir).
    /// - Degilse sessizce basarisiz olur, oyuncu rahatsiz edilmez, oyun normal devam eder.
    /// - Istersen ayarlar paneline buton koyup ManualSignIn() baglarsin (giris UI'ini acar).
    /// KURULUM NOTU: Play Console'da Play Games Services yapilandirmasi + Unity'de
    /// Window > Google Play Games > Setup > Android Setup (resources XML yapistir) yapilmadan cihazda calismaz.
    /// </summary>
    public static class GooglePlayLogin
    {
        public static bool IsSignedIn { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSignIn()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate(OnAuth);
#else
            Debug.Log("[GPGS] Editor/diger platform: giris atlandi (sadece Android cihazda calisir).");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void OnAuth(SignInStatus status)
        {
            IsSignedIn = status == SignInStatus.Success;
            if (IsSignedIn)
                Debug.Log("[GPGS] Giris OK: " + PlayGamesPlatform.Instance.GetUserDisplayName());
            else
                Debug.LogWarning("[GPGS] Otomatik giris olmadi: " + status + " (oyuncu Play Games hesabina girmemis olabilir; oyun normal akar)");
        }
#endif

        /// <summary>Ayarlardaki bir "Google ile giris" butonuna baglanabilir; giris UI'ini acar.</summary>
        public static void ManualSignIn()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Instance.ManuallyAuthenticate(OnAuth);
#else
            Debug.Log("[GPGS] Elle giris sadece Android cihazda.");
#endif
        }
    }
}

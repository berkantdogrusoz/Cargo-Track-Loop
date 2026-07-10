using System;
using UnityEngine;

namespace ColorCargoLoop
{
    public sealed class AdsManager : MonoBehaviour
    {
        const string DefaultInterstitialAndroid = "ca-app-pub-1985873646762305/9394460668";
        const string DefaultRewardedAndroid = "ca-app-pub-1985873646762305/9250000759";
        const string DefaultBannerAndroid = "ca-app-pub-1985873646762305/8471313113";

        static AdsManager instance;

        public static AdsManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindObjectOfType<AdsManager>();
                if (instance != null) return instance;

                GameObject go = new GameObject("AdsManager");
                instance = go.AddComponent<AdsManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        [Header("Reklam Ayarlari")]
        [SerializeField] private bool adsEnabled = true;
        [SerializeField] private bool editorAutoReward = true;
        [SerializeField] private string androidInterstitialAdUnitId = DefaultInterstitialAndroid;
        [SerializeField] private string androidRewardedAdUnitId = DefaultRewardedAndroid;
        [SerializeField] private string androidBannerAdUnitId = DefaultBannerAndroid;

        bool initialized;
        bool bannerVisible;
#if UNITY_ANDROID && !UNITY_EDITOR
        GoogleMobileAds.Api.BannerView bannerView;
        bool gmaInitialized;
#endif
        bool rewardedEarned;
        Action interstitialFinished;
        Action<bool> rewardedFinished;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass adsClass;
#endif

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!adsEnabled) return;
            try
            {
                adsClass = new AndroidJavaClass("com.altare.candycargo.ads.CandyCargoAds");
                adsClass.CallStatic(
                    "initialize",
                    gameObject.name,
                    string.IsNullOrEmpty(androidInterstitialAdUnitId) ? DefaultInterstitialAndroid : androidInterstitialAdUnitId,
                    string.IsNullOrEmpty(androidRewardedAdUnitId) ? DefaultRewardedAndroid : androidRewardedAdUnitId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ads] Android init hata: " + ex.Message);
            }
#else
            Debug.Log("[Ads] Editor/unsupported platform: reklamlar simule ediliyor.");
#endif
        }

        public void ShowInterstitial(Action onFinished)
        {
            if (!adsEnabled)
            {
                onFinished?.Invoke();
                return;
            }

            Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            interstitialFinished = onFinished;
            try
            {
                if (adsClass == null) adsClass = new AndroidJavaClass("com.altare.candycargo.ads.CandyCargoAds");
                adsClass.CallStatic("showInterstitial");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ads] Interstitial gosterilemedi: " + ex.Message);
                CompleteInterstitial();
            }
#else
            onFinished?.Invoke();
#endif
        }

        public void ShowRewarded(Action<bool> onFinished)
        {
            if (!adsEnabled)
            {
                onFinished?.Invoke(false);
                return;
            }

            Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            rewardedEarned = false;
            rewardedFinished = onFinished;
            try
            {
                if (adsClass == null) adsClass = new AndroidJavaClass("com.altare.candycargo.ads.CandyCargoAds");
                adsClass.CallStatic("showRewarded");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ads] Rewarded gosterilemedi: " + ex.Message);
                CompleteRewarded(false);
            }
#else
            onFinished?.Invoke(editorAutoReward);
#endif
        }

        // ================== BANNER (Google Mobile Ads C# API - alt bant) ==================
        // Interstitial/rewarded native Java koprusunden gider; banner Unity plugin'inden.
        // ArrowsPixelGame level esigine gore cagirir (yeni oyuncu ilk levellerde reklamsiz).

        public void ShowBanner()
        {
            if (!adsEnabled) { bannerVisible = false; return; }
            if (bannerVisible) return;
            Initialize();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!gmaInitialized)
                {
                    gmaInitialized = true;
                    GoogleMobileAds.Api.MobileAds.Initialize(_ => { }); // native SDK zaten Java tarafinda init; bu cagri idempotent
                }
                if (bannerView == null)
                {
                    var size = GoogleMobileAds.Api.AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                        GoogleMobileAds.Api.AdSize.FullWidth);
                    bannerView = new GoogleMobileAds.Api.BannerView(
                        string.IsNullOrEmpty(androidBannerAdUnitId) ? DefaultBannerAndroid : androidBannerAdUnitId,
                        size,
                        GoogleMobileAds.Api.AdPosition.Bottom);
                    bannerView.LoadAd(new GoogleMobileAds.Api.AdRequest());
                }
                else
                {
                    bannerView.Show();
                }
                bannerVisible = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ads] Banner gosterilemedi: " + ex.Message);
            }
#else
            bannerVisible = true;
            Debug.Log("[Ads] (Editor) Banner GOSTER - simule");
#endif
        }

        public void HideBanner()
        {
            if (!bannerVisible) return;
            bannerVisible = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { if (bannerView != null) bannerView.Hide(); }
            catch (Exception ex) { Debug.LogWarning("[Ads] Banner gizlenemedi: " + ex.Message); }
#else
            Debug.Log("[Ads] (Editor) Banner GIZLE - simule");
#endif
        }

        void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (bannerView != null) { bannerView.Destroy(); bannerView = null; }
#endif
        }

        public void OnAdsInitialized(string message)
        {
            Debug.Log("[Ads] Hazir: " + message);
        }

        public void OnAdLog(string message)
        {
            Debug.Log("[Ads] " + message);
        }

        public void OnInterstitialClosed(string message)
        {
            CompleteInterstitial();
        }

        public void OnInterstitialUnavailable(string message)
        {
            Debug.Log("[Ads] Interstitial hazir degil: " + message);
            CompleteInterstitial();
        }

        public void OnInterstitialFailed(string message)
        {
            Debug.LogWarning("[Ads] Interstitial hata: " + message);
            CompleteInterstitial();
        }

        public void OnRewardedEarned(string message)
        {
            rewardedEarned = true;
        }

        public void OnRewardedClosed(string message)
        {
            CompleteRewarded(rewardedEarned);
        }

        public void OnRewardedUnavailable(string message)
        {
            Debug.Log("[Ads] Rewarded hazir degil: " + message);
            CompleteRewarded(false);
        }

        public void OnRewardedFailed(string message)
        {
            Debug.LogWarning("[Ads] Rewarded hata: " + message);
            CompleteRewarded(false);
        }

        void CompleteInterstitial()
        {
            Action cb = interstitialFinished;
            interstitialFinished = null;
            cb?.Invoke();
        }

        void CompleteRewarded(bool granted)
        {
            Action<bool> cb = rewardedFinished;
            rewardedFinished = null;
            rewardedEarned = false;
            cb?.Invoke(granted);
        }
    }
}

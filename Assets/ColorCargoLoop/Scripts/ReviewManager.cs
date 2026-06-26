using System.Collections;
using UnityEngine;
#if USE_PLAY_REVIEW
using Google.Play.Review;
#endif

namespace ColorCargoLoop
{
    /// <summary>
    /// Google Play "yildiz ver / yorum yap" (In-App Review) yoneticisi.
    /// - Google Play In-App Review plugin KURULU + Scripting Define'a USE_PLAY_REVIEW eklenmisse -> uygulama ICINDE overlay panel gelir.
    /// - Plugin yoksa -> Play Store rating sayfasini acar (fallback, plugin'siz calisir).
    /// - RequestReviewOnce: CIHAZ basina BIR KEZ gosterir (Google da spam istemiyor).
    /// Sahneye eklemek zorunda degilsin; cagrilinca otomatik olusur.
    /// </summary>
    public sealed class ReviewManager : MonoBehaviour
    {
        const string ShownKey = "ccl_review_shown";
        static ReviewManager instance;

        public static ReviewManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindObjectOfType<ReviewManager>();
                if (instance != null) return instance;
                GameObject go = new GameObject("ReviewManager");
                instance = go.AddComponent<ReviewManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        // Cihaz basina BIR KEZ: yildiz/yorum panelini gosterir. Tekrar cagrilirsa hicbir sey yapmaz.
        public void RequestReviewOnce()
        {
            if (PlayerPrefs.GetInt(ShownKey, 0) == 1) return;
            PlayerPrefs.SetInt(ShownKey, 1);
            PlayerPrefs.Save();
            ShowReview();
        }

        void ShowReview()
        {
#if USE_PLAY_REVIEW
            StartCoroutine(InAppReviewFlow());
#else
            OpenStorePage(); // plugin yok -> Play Store rating sayfasi
#endif
        }

#if USE_PLAY_REVIEW
        IEnumerator InAppReviewFlow()
        {
            var rm = new Google.Play.Review.ReviewManager();
            var req = rm.RequestReviewFlow();
            yield return req;
            if (req.Error != ReviewErrorCode.NoError) { OpenStorePage(); yield break; }
            var launch = rm.LaunchReviewFlow(req.GetResult());
            yield return launch;
        }
#endif

        void OpenStorePage()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Application.OpenURL("market://details?id=" + Application.identifier); }
            catch { Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier); }
#else
            Debug.Log("[Review] (editor) in-app review istegi simule edildi");
#endif
        }
    }
}

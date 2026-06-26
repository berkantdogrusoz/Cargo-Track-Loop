using UnityEngine;
#if USE_MOBILE_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace ColorCargoLoop
{
    /// <summary>
    /// Yerel bildirim: oyuncu oyundan cikinca (arka plan) belirli araliklarla "geri dön" hatirlaticisi planlar,
    /// oyuncu geri gelince iptal eder.
    /// Kurulum: Window > Package Manager > Mobile Notifications (Install) + Player Settings > Scripting Define Symbols'a
    ///          USE_MOBILE_NOTIFICATIONS ekle. (Bildirim ikonu: Project Settings > Mobile Notifications > Small Icon "icon_0")
    /// Sahneye eklemene gerek yok; Instance cagrilinca otomatik olusur.
    /// NOT: repeatHours=1 (saat basi) AGRESIF olabilir -> oyuncular uninstall edebilir. 3-6 saat daha guvenli.
    /// </summary>
    public sealed class NotificationManager : MonoBehaviour
    {
        [Tooltip("Bildirimler arasi saat. 1 = saat basi (agresif). Onerilen 3-6.")]
        [SerializeField] private int repeatHours = 1;
        [SerializeField] private string title = "Pixel Pour";
        [SerializeField] private string body = "Seni bekleyen bulmacalar var, geri dön! 🎨";

        const string ChannelId = "ccl_reminder";
        static NotificationManager instance;

        public static NotificationManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindObjectOfType<NotificationManager>();
                if (instance != null) return instance;
                GameObject go = new GameObject("NotificationManager");
                instance = go.AddComponent<NotificationManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        // Oyun arka plana gidince hatirlatici planla, geri gelince iptal et (otomatik).
        void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleReminders();
            else CancelReminders();
        }

        public void ScheduleReminders()
        {
#if USE_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.RegisterNotificationChannel(
                new AndroidNotificationChannel(ChannelId, "Hatirlatici", "Oyuna geri dön", Importance.Default));
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            int hrs = Mathf.Max(1, repeatHours);
            var n = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = System.DateTime.Now.AddHours(hrs),
                RepeatInterval = System.TimeSpan.FromHours(hrs), // her 'hrs' saatte tekrar
                SmallIcon = "icon_0",
            };
            AndroidNotificationCenter.SendNotification(n, ChannelId);
#else
            Debug.Log("[Notif] (editor/paket yok) saat basi hatirlatici simule edildi");
#endif
        }

        public void CancelReminders()
        {
#if USE_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
        }
    }
}

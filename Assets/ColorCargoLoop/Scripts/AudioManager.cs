using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Merkezi ses yoneticisi. Klipleri Inspector'dan SEN atarsin; oyun kodu sadece
    /// AudioManager.Play(AudioManager.Sfx.X) cagirir -> ses, oyun mantigina binmez.
    /// Kurulum: sahneye bos bir "AudioManager" GameObject ekle, bu componenti tak, klipleri ata.
    /// (Sahnede yoksa Play(...) sessizce hicbir sey yapmaz -> hata vermez.)
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public enum Sfx
        {
            BasketGrab,     // sepeti tutunca
            BasketDrop,     // sepeti birakinca (istege bagli)
            BasketExtract,  // sepet kapidan cikinca
            CubeFill,       // kup sepete inerken (throttle'li)
            Win,
            Lose,
            Button,
            Fireworks       // win panelinde havai fisek patlamasi
        }

        public static AudioManager Instance { get; private set; }

        [Header("Cikislar (bos -> kod AudioSource ekler)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("SFX Klipleri (Inspector'dan ata)")]
        [SerializeField] private AudioClip basketGrab;
        [SerializeField] private AudioClip basketDrop;
        [SerializeField] private AudioClip basketExtract;
        [SerializeField] private AudioClip cubeFill;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip fireworks;
        [SerializeField] private AudioClip lose;
        [SerializeField] private AudioClip button;

        [Header("Throttle (kup sesi cok sik calmasin, sn)")]
        [SerializeField] private float cubeFillMinInterval = 0.05f;
        float lastCubeFill = -10f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // Oyun kodunun cagirdigi kisa yol (Instance yoksa sessiz).
        public static void Play(Sfx id) { if (Instance != null) Instance.PlaySfx(id); }

        // Calan tum SFX'i kes (orn. win panel kapaninca havai fisek sesi sonraki sahneye sizmasin).
        public static void StopSfx() { if (Instance != null && Instance.sfxSource != null) Instance.sfxSource.Stop(); }

        // Havai fisek sesi: AYRI kanal -> partikul bitince bagimsiz KESILEBILIR (diger sfx'i etkilemez).
        AudioSource fireworksSource;
        public static void PlayFireworks() { if (Instance != null) Instance.PlayFireworksInternal(); }
        public static void StopFireworks() { if (Instance != null && Instance.fireworksSource != null) Instance.fireworksSource.Stop(); }
        void PlayFireworksInternal()
        {
            if (fireworks == null) return;
            if (fireworksSource == null) { fireworksSource = gameObject.AddComponent<AudioSource>(); fireworksSource.playOnAwake = false; }
            fireworksSource.PlayOneShot(fireworks, sfxVolume);
        }

        public void PlaySfx(Sfx id)
        {
            if (id == Sfx.CubeFill)
            {
                if (Time.unscaledTime - lastCubeFill < cubeFillMinInterval) return;
                lastCubeFill = Time.unscaledTime;
            }
            AudioClip clip = ClipFor(id);
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic() { if (musicSource != null) musicSource.Stop(); }

        AudioClip ClipFor(Sfx id)
        {
            switch (id)
            {
                case Sfx.BasketGrab: return basketGrab;
                case Sfx.BasketDrop: return basketDrop;
                case Sfx.BasketExtract: return basketExtract;
                case Sfx.CubeFill: return cubeFill;
                case Sfx.Win: return win;
                case Sfx.Lose: return lose;
                case Sfx.Button: return button;
                case Sfx.Fireworks: return fireworks;
            }
            return null;
        }
    }
}

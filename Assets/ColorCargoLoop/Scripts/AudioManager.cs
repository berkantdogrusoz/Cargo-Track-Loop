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

        // Ayarlar paneli: SFX / muzik ac-kapa (PlayerPrefs'e kalici yazilir)
        const string PrefSfxOn = "set_sfx_on";
        const string PrefMusicOn = "set_music_on";
        bool sfxMuted;
        bool musicMuted;

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

            // kayitli ses ayarlarini yukle ve uygula
            sfxMuted = PlayerPrefs.GetInt(PrefSfxOn, 1) == 0;
            musicMuted = PlayerPrefs.GetInt(PrefMusicOn, 1) == 0;
            sfxSource.mute = sfxMuted;
            musicSource.mute = musicMuted;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // Oyun kodunun cagirdigi kisa yol (Instance yoksa sessiz).
        public static void Play(Sfx id) { if (Instance != null) Instance.PlaySfx(id); }

        // Serbest klip cal (orn. win coin sesi). SFX kanalindan -> SFX kapaliyken susar.
        public static void PlayClip(AudioClip clip) { if (Instance != null) Instance.PlayClipInternal(clip); }
        void PlayClipInternal(AudioClip clip) { if (clip == null || sfxSource == null) return; sfxSource.PlayOneShot(clip, sfxVolume); }

        // Calan tum SFX'i kes (orn. win panel kapaninca havai fisek sesi sonraki sahneye sizmasin).
        public static void StopSfx() { if (Instance != null && Instance.sfxSource != null) Instance.sfxSource.Stop(); }

        // === Ayarlar paneli API'si (SFX / muzik ac-kapa) ===
        public static bool IsSfxOn => Instance == null || !Instance.sfxMuted;
        public static bool IsMusicOn => Instance == null || !Instance.musicMuted;
        public static bool ToggleSfx() { if (Instance == null) return true; Instance.SetSfxMuted(!Instance.sfxMuted); return !Instance.sfxMuted; }
        public static bool ToggleMusic() { if (Instance == null) return true; Instance.SetMusicMuted(!Instance.musicMuted); return !Instance.musicMuted; }
        void SetSfxMuted(bool m) { sfxMuted = m; if (sfxSource != null) sfxSource.mute = m; if (fireworksSource != null) fireworksSource.mute = m; PlayerPrefs.SetInt(PrefSfxOn, m ? 0 : 1); PlayerPrefs.Save(); }
        void SetMusicMuted(bool m) { musicMuted = m; if (musicSource != null) musicSource.mute = m; PlayerPrefs.SetInt(PrefMusicOn, m ? 0 : 1); PlayerPrefs.Save(); }

        // Havai fisek sesi: AYRI kanal -> partikul bitince bagimsiz KESILEBILIR (diger sfx'i etkilemez).
        AudioSource fireworksSource;
        public static void PlayFireworks() { if (Instance != null) Instance.PlayFireworksInternal(); }
        public static void StopFireworks() { if (Instance != null && Instance.fireworksSource != null) Instance.fireworksSource.Stop(); }
        void PlayFireworksInternal()
        {
            if (fireworks == null) return;
            if (fireworksSource == null) { fireworksSource = gameObject.AddComponent<AudioSource>(); fireworksSource.playOnAwake = false; fireworksSource.mute = sfxMuted; }
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
            musicSuspended = false;
        }

        public void StopMusic() { if (musicSource != null) musicSource.Stop(); }

        // Panel acikken muzigi gecici DURDUR/DEVAM (ayarlar mute'undan bagimsiz; pause/unpause).
        bool musicSuspended;
        public static void SetMusicSuspended(bool suspend)
        {
            if (Instance == null || Instance.musicSource == null) return;
            if (suspend == Instance.musicSuspended) return;
            Instance.musicSuspended = suspend;
            if (suspend) Instance.musicSource.Pause();
            else Instance.musicSource.UnPause();
        }

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

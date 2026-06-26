using System.Collections;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Bu obje aktif olunca (win panel acilinca) havai fisek SESINI calar.
    /// Ses AYRI kanaldan calar (AudioManager.PlayFireworks) -> PARTIKUL BITINCE kesilir, sonraki sahneye sizmaz.
    /// - Klibi AudioManager'daki "Fireworks" slotuna ata.
    /// - Bu component'i fisek Particle System'ine (ya da win panele) tak.
    /// - fireworksParticles atanmazsa bu objedeki/cocuktaki ilk ParticleSystem izlenir.
    /// Kod sadece "ses" yonetir; partikulu OYNATMAZ (onu Play On Awake / sahne yapar).
    /// </summary>
    public sealed class FireworksSfx : MonoBehaviour
    {
        [Tooltip("Izlenecek partikul (bos -> bu objedeki/cocuktaki ilk ParticleSystem). Partikul bitince ses kesilir.")]
        [SerializeField] private ParticleSystem fireworksParticles;
        [Tooltip("Patlama dalgalari arasi sure (sn). Partikul Duration'i ile ayni yap.")]
        [SerializeField] private float interval = 2f;
        [Tooltip("Tek sefer mi (false) yoksa partikul bitene kadar tekrar mi (true).")]
        [SerializeField] private bool repeat = true;
        [Tooltip("Acilinca ilk sesten once gecikme (sn).")]
        [SerializeField] private float startDelay = 0f;

        void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(Loop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            AudioManager.StopFireworks();   // panel kapaninca da kes (sizmasin)
        }

        IEnumerator Loop()
        {
            if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);
            ParticleSystem ps = fireworksParticles != null ? fireworksParticles : GetComponentInChildren<ParticleSystem>();
            AudioManager.PlayFireworks();

            if (!repeat)
            {
                // Tek sefer: partikul bitene kadar bekle, sonra sesi KES.
                if (ps != null) { while (ps.IsAlive(true)) yield return null; AudioManager.StopFireworks(); }
                yield break;
            }

            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(Mathf.Max(0.1f, interval));
            while (true)
            {
                yield return wait;
                if (ps != null && !ps.IsAlive(true)) { AudioManager.StopFireworks(); yield break; } // PARTIKUL BITTI -> sesi kes
                AudioManager.PlayFireworks();
            }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// MEKANIK: win panel acilinca SAHNEDEKI havai fisek ParticleSystem'lerini oynatir, kapaninca durdurur.
    /// Partikul sistemleri SAHNEDE gercek obje olarak durur (Berkant elle ekler/konumlandirir/ayarlar);
    /// kod sadece play/stop yapar. (Partikulleri kod URETMEZ.)
    /// </summary>
    public sealed class WinFireworks : MonoBehaviour
    {
        [Tooltip("Sahnedeki havai fisek ParticleSystem'leri. Hiyerarsiden surukleyip ata; konum/ayar sende.")]
        [SerializeField] private ParticleSystem[] fireworks;

        [Tooltip("Sirayla mi patlasin (true) yoksa hepsi ayni anda mi (false).")]
        [SerializeField] private bool sequential = true;
        [SerializeField] private float sequenceDelay = 0.35f; // sirali patlama araligi (sn)
        [Tooltip("Panel pop-in animasyonu bitene kadar bekle (partikuller scale~0'da patlayip kaybolmasin).")]
        [SerializeField] private float startDelay = 0.4f;

        void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(PlayRoutine());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            StopAll();
        }

        void PlayAll()
        {
            if (fireworks == null) return;
            foreach (ParticleSystem ps in fireworks)
                if (ps != null) { ps.Clear(); ps.Play(); }
        }

        void StopAll()
        {
            if (fireworks == null) return;
            foreach (ParticleSystem ps in fireworks)
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        IEnumerator PlayRoutine()
        {
            if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay); // pop-in bitsin, sonra patlat
            if (fireworks == null) yield break;
            if (!sequential) { PlayAll(); yield break; }
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(sequenceDelay);
            foreach (ParticleSystem ps in fireworks)
            {
                if (ps != null) { ps.Clear(); ps.Play(); }
                yield return wait;
            }
        }
    }
}

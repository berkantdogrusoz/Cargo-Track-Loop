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

        void OnEnable()
        {
            StopAllCoroutines();
            if (sequential) StartCoroutine(PlaySequence());
            else PlayAll();
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

        IEnumerator PlaySequence()
        {
            if (fireworks == null) yield break;
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(sequenceDelay);
            foreach (ParticleSystem ps in fireworks)
            {
                if (ps != null) { ps.Clear(); ps.Play(); }
                yield return wait;
            }
        }
    }
}

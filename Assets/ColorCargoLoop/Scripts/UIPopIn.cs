using System.Collections;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Obje/panel aktif olunca minikten -> tam boya OVERSHOOT ("pop") ile buyur.
    /// Win panel logosuna takilir: panel acilinca logo en kucukten buyuyup sonda ziplayip oturur.
    /// </summary>
    public sealed class UIPopIn : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 1.2f)] private float duration = 0.5f;
        [SerializeField, Range(0f, 0.5f)] private float startScale = 0.0f;   // baslangic boyu (0 = yok'tan buyur)
        [SerializeField, Range(0f, 4f)] private float overshoot = 2.2f;       // pop siddeti (buyukse daha cok zipla)
        [SerializeField] private bool useUnscaledTime = true;                 // oyun durdurulsa bile calissin

        Vector3 target;
        bool captured;

        void Awake() { Capture(); }

        void Capture()
        {
            if (captured) return;
            target = transform.localScale;
            captured = true;
        }

        void OnEnable()
        {
            Capture();
            StopAllCoroutines();
            StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            float e = 0f;
            Vector3 from = target * startScale;
            transform.localScale = from;
            while (e < duration)
            {
                e += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(e / duration);
                float s = EaseOutBack(u, overshoot);
                transform.localScale = Vector3.LerpUnclamped(from, target, s);
                yield return null;
            }
            transform.localScale = target;
        }

        // EaseOutBack: 1'i asip geri oturan overshoot ("pop") egrisi
        static float EaseOutBack(float x, float c1)
        {
            float c3 = c1 + 1f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Board duvar segmentlerinde "MEKSIKA DALGASI": dalga board cevresinde dolasir,
    /// her duvar sirayla kalkip iner (biri kalkip inerken sirasi gelen yandaki kalkar).
    /// Bir tur bitince pauseBetween kadar bekler -> belli araliklarla tekrarlar.
    /// Runtime'da board kurulduktan sonra Setup(...) ile segmentler verilir.
    /// </summary>
    public sealed class BoardWallWave : MonoBehaviour
    {
        public float amplitude = 0.16f;    // segment ne kadar kalkar (lokal-Y)
        public float waveSpeed = 5f;       // segment/saniye (dalganin ilerleme hizi)
        public float waveWidth = 1.5f;     // ayni anda kac segment havada (dar = tek tek belirgin)
        public float pauseBetween = 1.0f;  // bir tur bitince bekleme (belli aralik)
        public bool useUnscaledTime = true;

        Transform[] segs;
        float[] baseY;
        int n;
        float clock;

        public void Setup(List<Transform> ordered)
        {
            n = ordered != null ? ordered.Count : 0;
            segs = new Transform[n];
            baseY = new float[n];
            for (int i = 0; i < n; i++) { segs[i] = ordered[i]; baseY[i] = ordered[i].localPosition.y; }
            clock = 0f;
        }

        void OnDisable() { ResetHeights(); }

        void ResetHeights()
        {
            if (segs == null) return;
            for (int i = 0; i < n; i++)
            {
                if (segs[i] == null) continue;
                Vector3 p = segs[i].localPosition; p.y = baseY[i]; segs[i].localPosition = p;
            }
        }

        void Update()
        {
            if (segs == null || n == 0) return;
            clock += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float travel = n + waveWidth;                                   // dalga tum segmentleri gecince
            float cycle = travel / Mathf.Max(0.01f, waveSpeed) + Mathf.Max(0f, pauseBetween);
            float t = clock % cycle;
            float head = t * waveSpeed;                                     // dalga basinin konumu (segment birimi)

            for (int i = 0; i < n; i++)
            {
                if (segs[i] == null) continue;
                float h = 0f;
                if (head <= travel)
                {
                    float d = Mathf.Abs(i - head);
                    if (d < waveWidth) h = amplitude * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * d / waveWidth)); // yumusak tepe
                }
                Vector3 p = segs[i].localPosition; p.y = baseY[i] + h; segs[i].localPosition = p;
            }
        }
    }
}

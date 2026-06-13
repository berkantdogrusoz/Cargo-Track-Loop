using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Win panel acilinca havai fisek partikul patlamalari oynatir (panel aktif olunca calisir).
    /// Patlama/sacilma AYARLARI kod icinde tutturuldu; sadece KONUMLARI 'burstPoints' ile ayarla (Berkant).
    /// Partikuller runtime kurulur (yumusak yuvarlak nokta + additive) -> ayar her zaman dogru gelir.
    /// </summary>
    public sealed class WinFireworks : MonoBehaviour
    {
        [Header("Patlama KONUMLARI (canvas-px, panel merkezine gore) - Berkant ayarlar")]
        [SerializeField] private Vector2[] burstPoints =
        {
            new Vector2(-230f, 170f), new Vector2(235f, 240f), new Vector2(0f, -40f),
            new Vector2(-140f, -180f), new Vector2(170f, -150f)
        };

        [Header("Zamanlama")]
        [SerializeField] private float burstInterval = 0.45f; // patlamalar arasi (sirayla)
        [SerializeField] private float repeatEvery = 2.2f;    // tum seri bitince tekrar bekleme

        [Header("Patlama gorunumu (ayar tutturuldu)")]
        [SerializeField] private int particlesPerBurst = 150;
        [SerializeField] private float spread = 520f;         // sacilma hizi (px/s)
        [SerializeField] private float particleSize = 55f;    // px
        [SerializeField] private float lifetime = 1.1f;
        [SerializeField] private float gravity = 0.08f;       // patlama sonrasi hafif dusus

        readonly List<ParticleSystem> systems = new List<ParticleSystem>();
        [Header("Render (dokunma; px->dunya cevrimi)")]
        [SerializeField] private float pixelsToWorld = 0.0061f; // canvas-px -> dunya (orto 5.85 / ~960 yari-yukseklik)
        [SerializeField] private float frontDistance = 5f;       // kameranin onunde mesafe (UI'nin onunde ciz)

        Material mat;
        Texture2D dot;
        Transform fwRoot;
        bool built;

        void OnEnable()
        {
            if (!built) Build();
            if (fwRoot != null) fwRoot.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(PlayLoop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (fwRoot != null) fwRoot.gameObject.SetActive(false); // panel kapaninca fisek de kapansin
        }

        // Partikuller CANVAS yerine KAMERANIN ONUNDE dunya-uzayinda kurulur (Screen Space - Camera
        // canvas partikulleri yutuyordu). Mesafe ile UI'nin onunde garantili render.
        void Build()
        {
            mat = BuildAdditiveMaterial();
            systems.Clear();
            Camera cam = Camera.main;
            fwRoot = new GameObject("FW_Root").transform;
            if (cam != null)
            {
                fwRoot.SetParent(cam.transform, false);
                fwRoot.localRotation = Quaternion.identity;
                fwRoot.localPosition = new Vector3(0f, 0f, frontDistance);
                fwRoot.localScale = Vector3.one;
            }
            foreach (var p in burstPoints)
                systems.Add(CreateSystem(p * pixelsToWorld));
            built = true;
        }

        ParticleSystem CreateSystem(Vector2 worldOffset)
        {
            GameObject go = new GameObject("FW");
            go.transform.SetParent(fwRoot, false);
            go.transform.localPosition = new Vector3(worldOffset.x, worldOffset.y, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            float sp = spread * pixelsToWorld;
            float sz = particleSize * pixelsToWorld;
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = lifetime + 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.8f, lifetime * 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(sp * 0.55f, sp);
            main.startSize = new ParticleSystem.MinMaxCurve(sz * 0.7f, sz * 1.2f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 4000;
            main.startColor = BrightGradient();

            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particlesPerBurst) });

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere; // her yone sacilma
            sh.radius = 6f * pixelsToWorld; // kucuk merkez (dunya birimi)

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 0.85f), new Keyframe(1f, 0.05f)));

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingLayerID = 0;           // Default sorting layer (canvas ile ayni)
            r.sortingOrder = 32000;         // UI'nin cok uzerinde ciz
            r.alignment = ParticleSystemRenderSpace.View;
            return ps;
        }

        ParticleSystem.MinMaxGradient BrightGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.35f, 0.42f), 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(0.5f, 0.95f, 0.55f), 0.5f),
                    new GradientColorKey(new Color(0.45f, 0.75f, 1f), 0.75f),
                    new GradientColorKey(new Color(0.88f, 0.55f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        }

        Material BuildAdditiveMaterial()
        {
            // Sprites/Default: URP'de guvenilir, _MainTex * vertex-renk (alpha blend). Parlak renkli yumusak noktalar.
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("UI/Default");
            Material m = new Material(sh) { name = "FireworkDot" };
            m.mainTexture = SoftDot();
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            m.renderQueue = 3100;
            return m;
        }

        Texture2D SoftDot()
        {
            if (dot != null) return dot;
            const int s = 64;
            dot = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "SoftDot" };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    float a = Mathf.Pow(1f - d, 1.5f); // parlak merkez + yumusak kenar
                    dot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            dot.Apply();
            return dot;
        }

        IEnumerator PlayLoop()
        {
            WaitForSecondsRealtime stepWait = new WaitForSecondsRealtime(burstInterval);
            WaitForSecondsRealtime seriesWait = new WaitForSecondsRealtime(repeatEvery);
            while (isActiveAndEnabled)
            {
                for (int i = 0; i < systems.Count; i++)
                {
                    if (systems[i] != null) { systems[i].Clear(); systems[i].Play(); }
                    yield return stepWait;
                }
                yield return seriesWait;
            }
        }
    }
}

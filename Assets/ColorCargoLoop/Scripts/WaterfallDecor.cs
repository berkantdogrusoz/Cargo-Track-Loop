using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Arka plan selale dekoru. Kurulum: "Color Cargo Loop > Dekor > Selale Olustur" menusu
    /// (materyal + noise dokusu ASSET olarak uretilir -> edit modunda da gorunur, sahne temiz kaydolur).
    /// Animasyon shader _Time ile akar -> Update yok; maliyet tek quad + 2 texture okuma (A21s dostu).
    /// Renk/hiz ayarlari buradan; play modunda degisiklik aninda gorunur ve materyal asset'ine islenir (kalir).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class WaterfallDecor : MonoBehaviour
    {
        [Header("Renkler (referans: turkuaz selale)")]
        [SerializeField] private Color waterLight = new Color(0.42f, 0.90f, 0.96f);
        [SerializeField] private Color waterDark = new Color(0.10f, 0.58f, 0.78f);
        [SerializeField] private Color foamColor = Color.white;

        [Header("Akis")]
        [SerializeField, Range(0.05f, 1.5f)] private float flowSpeed = 0.35f;
        [SerializeField, Range(1f, 8f)] private float patchScale = 3.0f;
        [SerializeField, Range(0f, 1f)] private float patchCut = 0.52f;
        [SerializeField, Range(0f, 0.6f)] private float bottomFoam = 0.20f;
        [SerializeField, Range(0f, 0.3f)] private float topFoam = 0.06f;
        [SerializeField, Range(0f, 0.1f)] private float sway = 0.03f;

        const string ShaderName = "Color Cargo Loop/Toon Waterfall";
        Material mat;
        static Texture2D runtimeNoise; // asset yoksa yedek (runtime uretim)

        void Start()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            Material shared = mr.sharedMaterial;
            if (shared != null && shared.shader != null && shared.shader.name == ShaderName)
            {
                mat = shared; // menu araciyla kurulmus materyal asseti
                if (mat.GetTexture("_NoiseTex") == null) mat.SetTexture("_NoiseTex", GetRuntimeNoise());
                Push();
                return;
            }
            // fallback: elle quad'a eklendiyse materyali runtime kur
            Shader sh = Shader.Find(ShaderName);
            if (sh == null) { Debug.LogWarning("[Selale] '" + ShaderName + "' shader bulunamadi."); return; }
            mat = new Material(sh) { name = "MAT_Selale_Runtime" };
            mat.SetTexture("_NoiseTex", GetRuntimeNoise());
            Push();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

#if UNITY_EDITOR
        void OnValidate() { if (Application.isPlaying && mat != null) Push(); }
#endif

        void Push()
        {
            mat.SetColor("_ColorA", waterLight);
            mat.SetColor("_ColorB", waterDark);
            mat.SetColor("_FoamColor", foamColor);
            mat.SetFloat("_FlowSpeed", flowSpeed);
            mat.SetFloat("_PatchScale", patchScale);
            mat.SetFloat("_PatchCut", patchCut);
            mat.SetFloat("_FoamBottom", bottomFoam);
            mat.SetFloat("_TopFoam", topFoam);
            mat.SetFloat("_WobbleAmp", sway);
        }

        // 2 oktav Perlin noise (yedek). Mirror wrap = kayarken dikis gorunmez.
        static Texture2D GetRuntimeNoise()
        {
            if (runtimeNoise != null) return runtimeNoise;
            const int S = 256;
            runtimeNoise = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Mirror,
                filterMode = FilterMode.Bilinear,
                name = "SelaleNoiseRuntime"
            };
            Color32[] px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.035f, y * 0.035f) * 0.6f
                            + Mathf.PerlinNoise(x * 0.09f + 31.7f, y * 0.09f + 11.3f) * 0.4f;
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(n * 255f), 0, 255);
                    px[y * S + x] = new Color32(b, b, b, 255);
                }
            }
            runtimeNoise.SetPixels32(px);
            runtimeNoise.Apply(false, false);
            return runtimeNoise;
        }
    }
}

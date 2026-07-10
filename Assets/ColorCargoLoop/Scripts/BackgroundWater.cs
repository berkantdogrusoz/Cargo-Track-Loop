using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// ARKA PLAN su zemini: kameranin tum gorusunu kaplayan, her seyin arkasinda yavasca akan su dokusu.
    /// Kurulum: "Color Cargo Loop > Dekor > Arkaplan Su Zemini Kur" menusu.
    /// Kendini kameraya gore boyutlandirir (orto/perspektif farketmez). Animasyon shader _Time ile.
    /// Not: arka plan SAKIN olmali diye varsayilanlar yavas/dusuk kontrast; renkleri istedigin gibi boya.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class BackgroundWater : MonoBehaviour
    {
        [Header("Yerlesim")]
        [Tooltip("Bos = Camera.main. Zemin bu kameranin gorusune tam oturur.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Kameradan uzaklik - oyundaki her seyin ARKASINDA kalacak kadar buyuk olsun.")]
        [SerializeField] private float distance = 30f;
        [Tooltip("Ekran kenarlarindan tasma payi (1.15 = %15 tasar; kenar acigi kalmasin).")]
        [SerializeField] private float overscan = 1.15f;

        [Header("Renkler (pastel su - UI ile uyumlu tut)")]
        [SerializeField] private Color waterLight = new Color(0.62f, 0.88f, 0.93f);
        [SerializeField] private Color waterDark = new Color(0.45f, 0.76f, 0.87f);
        [SerializeField] private Color foamColor = Color.white;

        [Header("Akis (arka plan = sakin)")]
        [SerializeField, Range(0.02f, 1.0f)] private float flowSpeed = 0.10f;
        [SerializeField, Range(1f, 10f)] private float patchScale = 4.5f;
        [SerializeField, Range(0f, 1f)] private float patchCut = 0.50f;
        [SerializeField, Range(0f, 0.6f)] private float bottomFoam = 0f;   // arka planda kopuk yok
        [SerializeField, Range(0f, 0.3f)] private float topFoam = 0f;

        const string ShaderName = "Color Cargo Loop/Toon Waterfall";
        Material mat;

        void Start()
        {
            Fit();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            Material shared = mr.sharedMaterial;
            if (shared != null && shared.shader != null && shared.shader.name == ShaderName)
            {
                mat = shared;
                Push();
                return;
            }
            Shader sh = Shader.Find(ShaderName);
            if (sh == null) { Debug.LogWarning("[ArkaplanSu] '" + ShaderName + "' shader bulunamadi."); return; }
            mat = new Material(sh) { name = "MAT_ArkaplanSu_Runtime" };
            Push();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

#if UNITY_EDITOR
        void OnValidate() { if (Application.isPlaying && mat != null) { Push(); Fit(); } }
#endif

        // Kameranin gorusune tam otur (kamera duzlemine paralel, arkada)
        void Fit()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            float d = Mathf.Min(distance, cam.farClipPlane * 0.9f);
            transform.position = cam.transform.position + cam.transform.forward * d;
            transform.rotation = cam.transform.rotation;
            float h = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float w = h * cam.aspect;
            transform.localScale = new Vector3(w * overscan, h * overscan, 1f);
        }

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
            mat.SetFloat("_WobbleAmp", 0f); // zemin kenarlari sallanmasin (acik kenar olusmasin)
        }
    }
}

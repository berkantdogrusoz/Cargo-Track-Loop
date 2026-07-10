using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Dekor araclari. "Selale Olustur": noise dokusunu + materyali ASSET olarak uretir,
    /// sahneye WaterfallDecor'lu bir quad ekler (kameranin baktigi yonde, arka planda).
    /// Konum/olcek Scene'de elle ayarlanir; renk/hiz WaterfallDecor Inspector'indan.
    /// </summary>
    public static class DecorTools
    {
        const string TexPath = "Assets/Art/selale_noise.png";
        const string MatPath = "Assets/Art/SelaleMat.mat";
        const string ShaderName = "Color Cargo Loop/Toon Waterfall";

        const string BgMatPath = "Assets/Art/ArkaplanSuMat.mat";

        // ARKA PLAN SU ZEMINI: kameranin tum gorusunu kaplayan, arkada yavasca akan su dokusu.
        // (Berkant'in istedigi: ayri selale objesi degil, arka planin kendisi su efekti.)
        [MenuItem("Color Cargo Loop/Dekor/Arkaplan Su Zemini Kur")]
        static void CreateBackgroundWater()
        {
            EnsureNoiseTexture();

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(BgMatPath);
            if (mat == null)
            {
                Shader sh = Shader.Find(ShaderName);
                if (sh == null) { Debug.LogError("[ArkaplanSu] '" + ShaderName + "' shader'i derlenmemis/bulunamadi."); return; }
                mat = new Material(sh) { name = "ArkaplanSuMat" };
                mat.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath));
                // arka plan varsayilanlari: sakin pastel su, kopuk yok
                mat.SetColor("_ColorA", new Color(0.62f, 0.88f, 0.93f));
                mat.SetColor("_ColorB", new Color(0.45f, 0.76f, 0.87f));
                mat.SetFloat("_FlowSpeed", 0.10f);
                mat.SetFloat("_PatchScale", 4.5f);
                mat.SetFloat("_FoamBottom", 0f);
                mat.SetFloat("_TopFoam", 0f);
                mat.SetFloat("_WobbleAmp", 0f);
                AssetDatabase.CreateAsset(mat, BgMatPath);
                AssetDatabase.SaveAssets();
            }

            GameObject go = GameObject.Find("ArkaplanSu");
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "ArkaplanSu";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<BackgroundWater>();
            }
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // edit modunda da kabaca yerlestir (Play'de BackgroundWater kendini tam oturtur)
            Camera cam = Camera.main;
            if (cam != null)
            {
                float d = Mathf.Min(30f, cam.farClipPlane * 0.9f);
                go.transform.position = cam.transform.position + cam.transform.forward * d;
                go.transform.rotation = cam.transform.rotation;
                float h = cam.orthographic ? cam.orthographicSize * 2f : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                go.transform.localScale = new Vector3(h * cam.aspect * 1.15f, h * 1.15f, 1f);
            }
            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("[ArkaplanSu] Kuruldu. Play'e bas - zemin yavasca akar. Renk/hiz: ArkaplanSu objesindeki BackgroundWater. Kaydet (Ctrl+S).");
        }

        static void EnsureNoiseTexture()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath) != null) return;
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];
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
            tex.SetPixels32(px);
            tex.Apply(false, false);
            File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath);
            var ti = AssetImporter.GetAtPath(TexPath) as TextureImporter;
            if (ti != null)
            {
                ti.wrapMode = TextureWrapMode.Mirror;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
        }

        // Ayri dikey selale objesi (kenar dekoru vb. istenirse). Arka plan icin USTTEKI menuyu kullan.
        [MenuItem("Color Cargo Loop/Dekor/Selale Olustur (sahneye ekle)")]
        static void CreateWaterfall()
        {
            // 1) Noise dokusu asseti (yoksa uret)
            EnsureNoiseTexture();

            // 2) Materyal asseti (yoksa uret)
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                Shader sh = Shader.Find(ShaderName);
                if (sh == null) { Debug.LogError("[Selale] '" + ShaderName + "' shader'i derlenmemis/bulunamadi."); return; }
                mat = new Material(sh) { name = "SelaleMat" };
                mat.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath));
                AssetDatabase.CreateAsset(mat, MatPath);
                AssetDatabase.SaveAssets();
            }

            // 3) Sahneye quad + WaterfallDecor
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Selale";
            Object.DestroyImmediate(go.GetComponent<Collider>()); // tiklama/raycast'e karismasin
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<WaterfallDecor>();

            Camera cam = Camera.main;
            if (cam != null)
            {
                // kameranin baktigi yonde, oyun alaninin arkasina; kamera duzlemine paralel
                go.transform.position = cam.transform.position + cam.transform.forward * 14f;
                go.transform.rotation = cam.transform.rotation;
                go.transform.localScale = new Vector3(4f, 8f, 1f);
            }
            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("[Selale] Sahneye eklendi. Konum/olcek: Scene view'da tasi; renk/hiz: WaterfallDecor Inspector'i. Kaydetmeyi unutma (Ctrl+S).");
        }
    }
}

using System.Collections.Generic;
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

        // COL TEMASI ZEMINI: kum dokusunu ISIKSIZ materyalle kameraya tam oturan quad olarak kurar.
        // Eski su arka planini (ArkaplanSu) kapatir - istenirse tekrar aktiflestirilebilir (vaha vb.).
        const string SandTexPath = "Assets/Art/Dekor/kum_zemin.png";
        const string SandMatPath = "Assets/Art/Dekor/MAT_KumZemin.mat";

        [MenuItem("Color Cargo Loop/Dekor/Col Kum Zemini Kur")]
        static void CreateSandGround()
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SandTexPath);
            if (tex == null) { Debug.LogError("[KumZemin] '" + SandTexPath + "' bulunamadi - kum dokusunu oraya koy."); return; }

            // dokuyu tekrarlanabilir yap (Mirror = dikis izi gizlenir, ChatGPT dokusu seamless olmayabilir)
            var ti = AssetImporter.GetAtPath(SandTexPath) as TextureImporter;
            if (ti != null && (ti.wrapMode != TextureWrapMode.Mirror || ti.maxTextureSize < 1024))
            {
                ti.wrapMode = TextureWrapMode.Mirror;
                ti.maxTextureSize = 1024; // arka plan icin fazlasi israf (bellek/boyut)
                ti.SaveAndReimport();
            }

            Shader unlit = Shader.Find(UnlitShaderName);
            if (unlit == null) { Debug.LogError("[KumZemin] '" + UnlitShaderName + "' shader'i bulunamadi/derlenmemis."); return; }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SandMatPath);
            if (mat == null)
            {
                mat = new Material(unlit) { name = "MAT_KumZemin" };
                mat.SetTexture("_MainTex", tex);
                mat.SetColor("_Color", Color.white);
                // portre ekran ~9:16 -> dalgalar kare kalsin diye dikeyde daha sik tekrar
                mat.SetTextureScale("_MainTex", new Vector2(1.25f, 2.2f));
                AssetDatabase.CreateAsset(mat, SandMatPath);
                AssetDatabase.SaveAssets();
            }

            GameObject go = GameObject.Find("ColKumZemini");
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "ColKumZemini";
                Object.DestroyImmediate(go.GetComponent<Collider>()); // tiklama/raycast'e karismasin
                go.AddComponent<GroundBackdrop>();
            }
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // eski su arka plani kapansin (silinmez - vaha olarak geri acilabilir)
            GameObject water = GameObject.Find("ArkaplanSu");
            if (water != null && water.activeSelf) { Undo.RecordObject(water, "Su Kapat"); water.SetActive(false); }

            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("[KumZemin] Kuruldu (ArkaplanSu kapatildi). Dalga sikligi: MAT_KumZemin > Tiling. Kaydet (Ctrl+S).");
        }

        // COL TEMASI: secili objelerin (ve altindakilerin) TUM materyallerini ISIKSIZ yapar.
        // Texture + renk korunur, ayni (texture, renk) ikilisi TEK materyali paylasir (SetPass sismez).
        // URP/magenta materyalleri de duzeltir (shader komple degistigi icin). Golge/probe da kapatilir.
        // Akis: indirdigin objeleri SAHNEYE at -> sec -> bu menu. Geri almak icin Ctrl+Z.
        const string UnlitShaderName = "Color Cargo Loop/Unlit Texture (Tint)";
        const string UnlitMatFolder = "Assets/Art/Dekor/UnlitMats";

        [MenuItem("Color Cargo Loop/Dekor/Secilenleri Isiksiz Yap (Unlit)")]
        static void MakeSelectionUnlit()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            { Debug.LogWarning("[Isiksiz] Once sahnede obje(ler) sec, sonra bu menuyu calistir."); return; }

            Shader unlit = Shader.Find(UnlitShaderName);
            if (unlit == null) { Debug.LogError("[Isiksiz] '" + UnlitShaderName + "' shader'i bulunamadi/derlenmemis."); return; }

            if (!AssetDatabase.IsValidFolder(UnlitMatFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art/Dekor")) AssetDatabase.CreateFolder("Assets/Art", "Dekor");
                AssetDatabase.CreateFolder("Assets/Art/Dekor", "UnlitMats");
            }

            var cache = new System.Collections.Generic.Dictionary<string, Material>();
            int rendererCount = 0, newMatCount = 0;

            foreach (GameObject root in Selection.gameObjects)
            {
                if (root == null) continue;
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = r.sharedMaterials;
                    bool changed = false;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        Material src = mats[m];
                        if (src == null) continue;
                        string sn = src.shader != null ? src.shader.name : "";
                        // Kendi ozel shaderlarimiz (su/selale/buz/toon) ve UI/text oldugu gibi kalsin
                        if (sn == UnlitShaderName || sn.StartsWith("Color Cargo Loop/") ||
                            sn.Contains("TextMeshPro") || sn.StartsWith("UI/") || sn.StartsWith("Sprites/")) continue;

                        Texture tex = FindAnyTexture(src);
                        Color col = FindAnyColor(src);
                        string key = (tex != null ? tex.GetInstanceID().ToString() : "duz") + "_" + ColorUtility.ToHtmlStringRGBA(col);

                        Material dst;
                        if (!cache.TryGetValue(key, out dst))
                        {
                            string texName = tex != null ? tex.name : "Duz";
                            foreach (char bad in Path.GetInvalidFileNameChars()) texName = texName.Replace(bad, '_');
                            string path = UnlitMatFolder + "/MAT_Unlit_" + texName + "_" + ColorUtility.ToHtmlStringRGB(col) + ".mat";
                            dst = AssetDatabase.LoadAssetAtPath<Material>(path); // onceki calistirmadan varsa yeniden kullan
                            if (dst == null)
                            {
                                dst = new Material(unlit);
                                if (tex != null) dst.SetTexture("_MainTex", tex);
                                dst.SetColor("_Color", col);
                                AssetDatabase.CreateAsset(dst, AssetDatabase.GenerateUniqueAssetPath(path));
                                newMatCount++;
                            }
                            cache[key] = dst;
                        }
                        mats[m] = dst;
                        changed = true;
                    }
                    if (!changed) continue;
                    Undo.RecordObject(r, "Isiksiz Materyal");
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    EditorUtility.SetDirty(r);
                    rendererCount++;
                    if (r.gameObject.scene.IsValid())
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Isiksiz] " + rendererCount + " renderer cevrildi, " + newMatCount + " yeni materyal uretildi (" + UnlitMatFolder + "). Kaydet (Ctrl+S).");
        }

        // Kaynak materyalin ana dokusunu bul (Standard _MainTex, URP _BaseMap, kirik shader varyantlari dahil)
        static Texture FindAnyTexture(Material m)
        {
            if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) return m.GetTexture("_MainTex");
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return m.GetTexture("_BaseMap");
            if (m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap") != null) return m.GetTexture("_BaseColorMap");
            return m.mainTexture; // son care (shader [MainTexture] isaretliyse bunu da yakalar)
        }

        static Color FindAnyColor(Material m)
        {
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            return Color.white;
        }

        // COL KENAR DEKORU: kaya/piramit/kaktus/kemik'i oyun alaninin DISINA, KIYI/KOSElere dizer.
        // GUVENLIK: her dekorun IC KENARI x=Xsafe cizgisine sabitlenir -> potre (adaptif genisleyebilir)
        // ve slotlar (ekstra slot ile buyuyebilir) ile ASLA cakismaz. Piramitler ust koseye (yuksek z,
        // oyunun ustunde) konur. Boyutlar PANDA boyuna gore: piramit EN IRI, kaya orta, kaktus+kemik
        // en kucuk (~panda). Collider'lar sokulur (tiklama yemez). Begenmezsen menuyu tekrar tikla.
        const float DecorXsafe = 2.9f;   // oyun alani |x|<~2.5 (potre/slot/kuyruk) + buyume payi
        const float DecorInnerSafe = 2.55f; // merkez-hizali dekorda ic kenar bu cizgiyi gecmez (oyun alanina tasmasin)
        const float DecorZbottom = -6.0f, DecorZtop = 8.8f; // gorunur derinlik (~-7.2..9.4 icinde)

        [MenuItem("Color Cargo Loop/Dekor/Col Kenar Dekoru Kur (kaya-kaktus-piramit-kemik)")]
        static void ScatterEdgeDecor()
        {
            GameObject kaya = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/kaya/kaya prefab materyalli.prefab");
            GameObject piramid = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/kaya/piramid/piramid materyalli.prefab");
            GameObject kaktus = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/kaya/kaktüs/kaktüs materyalli.prefab");
            GameObject kemik = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/kaya/kemik iskelet/kemik materyal.prefab");
            if (kaya == null || piramid == null || kaktus == null || kemik == null)
            { Debug.LogError("[KenarDekor] prefablardan biri bulunamadi (kaya/piramid/kaktus/kemik yollari)."); return; }

            GameObject holder = GameObject.Find("ColKenarDekoru");
            if (holder != null) Undo.DestroyObjectImmediate(holder);
            holder = new GameObject("ColKenarDekoru");
            Undo.RegisterCreatedObjectUndo(holder, "Kenar Dekor Kur");

            float H = MeasuredPandaHeight();        // kucuk dekor referans boyu
            var rnd = new System.Random();
            const float groundY = 0f;
            GameObject[] edgeOthers = { kaya, kaktus }; // kemik AYRI (merkez-hizali, tam gorunur konur)

            for (int s = 0; s < 2; s++)
            {
                int sign = (s == 0) ? -1 : 1;

                // 1) PIRAMIT: ust kose landmark. EN IRI ama IC KENAR x~1.8'e sabit + cok yuksek z (8.2+)
                //    -> potre (z<~5.5) hem X hem Z'de altta kalir, adaptif buyuse bile cakisma olmaz.
                PlaceDecor(holder.transform, piramid, rnd,
                    sign * (1.75f + (float)rnd.NextDouble() * 0.3f),
                    DecorZtop - (float)rnd.NextDouble() * 0.6f,
                    H * (1.9f + (float)rnd.NextDouble() * 0.4f),
                    groundY, 1, 0f);                                             // 1 = IC KENAR hizala, en sinirsiz

                // 2) KEMIK: garanti 1 adet, MERKEZ-hizali. Kemik prefabinin pivotu merkez-disi oldugu
                //    icin ic-kenar hizasi onu ekran disina itip GORUNMEZ yapiyordu -> merkezden hizala
                //    (tam kadrajda), ic kenar guvenligi yine korunur. En kucuk boy (~panda).
                PlaceDecor(holder.transform, kemik, rnd,
                    sign * 3.3f,                                                 // gorunur bant ortasi (2.9..3.9)
                    Mathf.Lerp(DecorZbottom + 1f, DecorZtop - 3f, (float)rnd.NextDouble()),
                    H * (0.95f + (float)rnd.NextDouble() * 0.25f),
                    groundY, 0, 1.6f);                                           // 0 = MERKEZ hizala, en<=1.6 (tam gorunur kucuk kemik)

                // 3) kenar boyunca 3 kaya/kaktus: IC KENAR Xsafe'e sabit -> oyun alanina tasmaz
                const int m = 3;
                for (int k = 0; k < m; k++)
                {
                    GameObject pf = edgeOthers[rnd.Next(edgeOthers.Length)];
                    bool isKaya = pf.name.ToLowerInvariant().Contains("kaya");
                    float targetH = isKaya
                        ? H * (1.3f + (float)rnd.NextDouble() * 0.3f)             // kaya = orta
                        : H * (0.9f + (float)rnd.NextDouble() * 0.3f);           // kaktus = kucuk
                    float z = Mathf.Lerp(DecorZbottom, DecorZtop - 1.8f, (k + 0.15f + (float)rnd.NextDouble() * 0.6f) / m);
                    PlaceDecor(holder.transform, pf, rnd,
                        sign * (DecorXsafe + (float)rnd.NextDouble() * 0.25f),
                        z, targetH, groundY, 1, 0f);                             // 1 = IC KENAR hizala, en sinirsiz
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(holder.scene);
            Selection.activeGameObject = holder;
            Debug.Log("[KenarDekor] 10 dekor (yan basi: 1 piramit kose + 1 kemik merkez + 3 kaya/kaktus kenar). Panda boyu=" + H.ToString("0.00")
                + ", Xsafe=" + DecorXsafe + " (oyun alanina tasmaz). Begenmezsen tekrar calistir. Kaydet (Ctrl+S).");
        }

        // Bir dekoru yerlestirir: hedef yukseklige normalize + zemine otur.
        // anchorMode 1 = IC KENARI sign*|targetX| cizgisine sabitle (dar/pivot-ortali objeler; oyun alanina tasmaz).
        // anchorMode 0 = bounds MERKEZINI targetX'e otur (TAM GORUNUR; pivotu merkez-disi objeler -kemik gibi-
        //               kenardan kaybolmaz), sonra ic kenar DecorInnerSafe'i gecmeyecek sekilde disari clamp'lenir.
        static void PlaceDecor(Transform parent, GameObject prefab, System.Random rnd,
                               float targetX, float z, float targetHeight, float groundY, int anchorMode, float maxWidth)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (inst == null) return;
            inst.transform.position = new Vector3(targetX, groundY, z);
            inst.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            NormalizeAndGround(inst, targetHeight, groundY);

            // GENISLIK SINIRI: kemik gibi yatik/genis prefablar yukseklige normalize edilince COK genisliyor
            // (en 4.5 birim) -> ekrani asip gorunmez oluyor. maxWidth>0 ise ene gore kucult + tekrar zemine otur.
            if (maxWidth > 0f)
            {
                Renderer[] wr = inst.GetComponentsInChildren<Renderer>(true);
                if (wr.Length > 0)
                {
                    Bounds wb = wr[0].bounds; for (int i = 1; i < wr.Length; i++) wb.Encapsulate(wr[i].bounds);
                    if (wb.size.x > maxWidth)
                    {
                        inst.transform.localScale *= maxWidth / wb.size.x;
                        wr = inst.GetComponentsInChildren<Renderer>(true);
                        wb = wr[0].bounds; for (int i = 1; i < wr.Length; i++) wb.Encapsulate(wr[i].bounds);
                        inst.transform.position += new Vector3(0f, groundY - wb.min.y - 0.05f, 0f);
                    }
                }
            }

            Renderer[] rr = inst.GetComponentsInChildren<Renderer>(true);
            if (rr.Length > 0)
            {
                Bounds b = rr[0].bounds;
                for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);
                float sign = targetX < 0f ? -1f : 1f;
                if (anchorMode == 1)
                {
                    // IC KENAR (merkeze en yakin nokta) targetX'e otur
                    float innerEdge = sign < 0f ? b.max.x : b.min.x;
                    inst.transform.position += new Vector3(targetX - innerEdge, 0f, 0f);
                }
                else
                {
                    // bounds MERKEZI targetX'e otur -> obje tam gorunur
                    inst.transform.position += new Vector3(targetX - b.center.x, 0f, 0f);
                    // guvenlik: ic kenar DecorInnerSafe'i gecerse disari it (oyun alanina girmesin)
                    rr = inst.GetComponentsInChildren<Renderer>(true);
                    b = rr[0].bounds; for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);
                    float innerSigned = (sign < 0f ? b.max.x : b.min.x) * sign; // merkeze uzaklik (disari = +)
                    if (innerSigned < DecorInnerSafe)
                        inst.transform.position += new Vector3(sign * (DecorInnerSafe - innerSigned), 0f, 0f);
                }
            }

            foreach (Collider col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // Panda (pandaThrowerPrefab) dunya boyunu olcer -> kucuk dekor referansi. Bulunamazsa 0.95 (olculen).
        static float MeasuredPandaHeight()
        {
            var game = Object.FindObjectOfType<ArrowsPixelGame>();
            if (game != null)
            {
                var so = new SerializedObject(game);
                var pp = so.FindProperty("pandaThrowerPrefab");
                var prefab = pp != null ? pp.objectReferenceValue as GameObject : null;
                if (prefab != null)
                {
                    GameObject inst = Object.Instantiate(prefab);
                    inst.transform.position = Vector3.zero;
                    Renderer[] rr = inst.GetComponentsInChildren<Renderer>(true);
                    float h = 0f;
                    if (rr.Length > 0)
                    {
                        Bounds b = rr[0].bounds;
                        for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);
                        h = b.size.y;
                    }
                    Object.DestroyImmediate(inst);
                    if (h > 0.05f) return h;
                }
            }
            return 0.95f; // olculen varsayilan (Anubis panda)
        }

        // Instance'i hedef yukseklige olcekle + tabani zemine otur (yariya gomulmesin).
        static void NormalizeAndGround(GameObject inst, float targetHeight, float groundY)
        {
            Renderer[] rends = inst.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = b.size.y;
            if (h > 0.0001f) inst.transform.localScale *= targetHeight / h;

            rends = inst.GetComponentsInChildren<Renderer>(true);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            inst.transform.position += new Vector3(0f, groundY - b.min.y - 0.05f, 0f); // hafif goom
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

using ColorCargoLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCargoLoopEditor
{
    /// <summary>
    /// Arrows x Pixel Flow: oyun ALANLARINI (resim alani, slotlar, tir puzzle alani)
    /// sahnede GERCEK, duzenlenebilir GameObject olarak kurar ve ArrowsPixelGame'e baglar.
    /// Kupler/tirlar Play'de bu alanlara gore doldurulur. Alanlari hiyerarsiden tasiyabilirsin.
    /// </summary>
    public static class ArrowsPixelSceneBuilder
    {
        const string LayoutName = "ArrowsPixelLayout";

        // Candy palet (ArrowsPixelGame ile ayni aile)
        static readonly Color C_CREAM = new Color(0.99f, 0.92f, 0.80f);
        static readonly Color C_FLOOR = new Color(0.95f, 0.71f, 0.74f);
        static readonly Color C_PAD   = new Color(0.99f, 0.86f, 0.74f);
        const float TruckGroundY = 0.075f;   // ArrowsPixelGame.TruckGroundY ile ayni
        const float GridStepZ    = 1.20f;    // ArrowsPixelGame.GridStepZ ile ayni (ExitGate konumu icin)

        [MenuItem("Color Cargo Loop/Build Arrows Scene")]
        public static void BuildArrowsScene()
        {
            // 1) ArrowsPixelGame component'ini bul (yoksa olustur)
            ArrowsPixelGame game = Object.FindFirstObjectByType<ArrowsPixelGame>();
            if (game == null)
            {
                GameObject go = new GameObject("ArrowsPixelGame");
                game = go.AddComponent<ArrowsPixelGame>();
                Undo.RegisterCreatedObjectUndo(go, "Create ArrowsPixelGame");
            }

            // 2) Eski layout'u temizle (varsa)
            GameObject existing = GameObject.Find(LayoutName);
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject layout = new GameObject(LayoutName);
            Undo.RegisterCreatedObjectUndo(layout, "Build Arrows Scene");

            // 3) Arka plan zemini
            Box("Background", layout.transform, new Vector3(0f, -0.2f, 0.5f), new Vector3(14f, 0.2f, 22f), C_FLOOR);

            // 4) UST: PictureArea (kup-resim bu noktaya kurulur; tasi/dondur -> resim onu takip eder)
            GameObject pic = new GameObject("PictureArea");
            pic.transform.SetParent(layout.transform, false);
            pic.transform.localPosition = new Vector3(0f, 0.10f, 3.7f);
            Pad("PictureBackboard", pic.transform, new Vector3(0f, -0.07f, 0f), 3.3f, 2.6f, C_CREAM, 0.06f); // zonu gormek icin pano

            // 5) ORTA: 3 slot (her biri tek tek tasinabilir; tir buralara park eder)
            GameObject slots = new GameObject("Slots");
            slots.transform.SetParent(layout.transform, false);
            var slotPts = new Transform[3];
            float gap = 1.65f, slotZ = 1.0f;
            for (int i = 0; i < 3; i++)
            {
                GameObject s = new GameObject("Slot_" + i);
                s.transform.SetParent(slots.transform, false);
                s.transform.localPosition = new Vector3((i - 1) * gap, TruckGroundY, slotZ);
                Pad("Pad", s.transform, new Vector3(0f, 0.04f - TruckGroundY, 0f), 1.45f, 1.25f, C_CREAM, 0.10f);
                Pad("Rim", s.transform, new Vector3(0f, 0.02f - TruckGroundY, 0f), 1.62f, 1.42f, C_PAD, 0.06f);
                slotPts[i] = s.transform;
            }

            // 6) ALT: ParkingArea (tir puzzle grid merkezi; tasi -> tum grid kayar)
            GameObject park = new GameObject("ParkingArea");
            park.transform.SetParent(layout.transform, false);
            park.transform.localPosition = new Vector3(0f, TruckGroundY, -3.0f);
            Box("ParkGround", park.transform, new Vector3(0f, 0.02f - TruckGroundY, 0f), new Vector3(5.75f, 0.10f, 4.25f), C_PAD);
            Pad("ExitGate", park.transform, new Vector3(0f, 0.01f - TruckGroundY, GridStepZ + GridStepZ * 0.62f), 1.20f, 0.42f, C_CREAM, 0.08f);
            BuildParkingWalls(park.transform);

            // 7) ArrowsPixelGame'e referanslari bagla (SerializedObject)
            SerializedObject so = new SerializedObject(game);
            SetRef(so, "pictureArea", pic.transform);
            SetRef(so, "parkingArea", park.transform);
            SerializedProperty sp = so.FindProperty("slotPoints");
            if (sp != null)
            {
                sp.arraySize = 3;
                for (int i = 0; i < 3; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = slotPts[i];
            }
            SerializedProperty bos = so.FindProperty("buildOnStart");
            if (bos != null) bos.boolValue = true;
            SerializedProperty cam = so.FindProperty("gameCamera");
            if (cam != null && cam.objectReferenceValue == null && Camera.main != null)
                cam.objectReferenceValue = Camera.main;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Selection.activeGameObject = layout;
            Debug.Log("[Arrows] Alanlar kuruldu ve ArrowsPixelGame'e baglandi: PictureArea / Slot_0..2 / ParkingArea. " +
                      "Hiyerarsiden tasiyabilirsin; Play'de kupler resme, tirlar parking grid'ine dolar. " +
                      "UI (Hamle / Coin / Win / Lose) alanlarini Canvas'tan sen baglayacaksin.");
        }

        // ----------------------------------------------------------------------------------
        // BOOSTER UI: butonlari Canvas'ta KALICI GameObject olarak kurar (Berkant gorsel verir),
        // onClick koddan baglanir. Aspect uyumu icin Canvas Scaler'i da ayarlar.
        // ----------------------------------------------------------------------------------
        [MenuItem("Color Cargo Loop/Build Booster UI")]
        public static void BuildBoosterUI()
        {
            ArrowsPixelGame game = Object.FindFirstObjectByType<ArrowsPixelGame>();
            if (game == null) { Debug.LogError("[Booster UI] ArrowsPixelGame sahnede yok."); return; }
            Canvas cv = Object.FindFirstObjectByType<Canvas>();
            if (cv == null) { Debug.LogError("[Booster UI] Canvas sahnede yok."); return; }

            // Aspect: yalnizca dogru ayarli DEGILSE degistir (mevcut UI'yi bozmamak icin)
            CanvasScaler scaler = cv.GetComponentInParent<CanvasScaler>();
            if (scaler == null) scaler = cv.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = cv.gameObject.AddComponent<CanvasScaler>();
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                Debug.Log("[Booster UI] Canvas Scaler -> Scale With Screen Size 1080x1920 (match 0.5) ayarlandi.");
            }

            Transform old = cv.transform.Find("BoosterBar");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject bar = new GameObject("BoosterBar", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(bar, "Build Booster UI");
            bar.transform.SetParent(cv.transform, false);
            RectTransform brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 90f);   // ekran altindan yukari (safe-area payi)
            brt.sizeDelta = new Vector2(1000f, 230f);

            Button b0 = CreateUIButton(bar.transform, "Booster_Destroy",  "YOK ET",   0);
            Button b1 = CreateUIButton(bar.transform, "Booster_ExtraExit","+KAPI",    1);
            Button b2 = CreateUIButton(bar.transform, "Booster_Shuffle",  "KARISTIR", 2);

            SerializedObject so = new SerializedObject(game);
            SetRef(so, "destroyFillerButton", b0);
            SetRef(so, "extraExitButton", b1);
            SetRef(so, "shuffleButton", b2);
            SerializedProperty sbb = so.FindProperty("showBoosterButtons");
            if (sbb != null) sbb.boolValue = false; // artik sahnedeki kalici butonlar kullanilacak
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Selection.activeGameObject = bar;
            Debug.Log("[Booster UI] BoosterBar kuruldu ve ArrowsPixelGame'e baglandi. " +
                      "Her butonun Image'ina kendi gorselini ver; onClick Play'de koddan baglanir.");
        }

        // Bar genisligine gore oransal anchorlanmis buton (aspect degisince butonlar da olceklenir)
        static Button CreateUIButton(Transform parent, string name, string label, int index)
        {
            float[] mins = { 0.00f, 0.34f, 0.68f };
            float[] maxs = { 0.32f, 0.66f, 1.00f };
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(mins[index], 0f);
            rt.anchorMax = new Vector2(maxs[index], 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.99f, 0.90f, 0.70f, 0.98f); // gecici placeholder rengi (sprite verince degisir)

            GameObject lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            TMPro.TextMeshProUGUI tm = lbl.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = label;
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.fontSize = 44f;
            tm.color = new Color(0.45f, 0.30f, 0.15f);
            tm.raycastTarget = false;
            RectTransform lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        // ----------------------------------------------------------------------------------
        // ADET YAZILARI: mevcut booster butonlarinin altina "CountText" TMP olusturur ve
        // ArrowsPixelGame'in adet-text slotlarina ATAR. Gorseller/sprite'lar bozulmaz.
        // Olusan CountText objelerini Berkant istedigi yere tasir / stiller / kendi TMP'siyle degistirir.
        // ----------------------------------------------------------------------------------
        [MenuItem("Color Cargo Loop/Wire Booster Count Texts")]
        public static void WireBoosterCountTexts()
        {
            ArrowsPixelGame game = Object.FindFirstObjectByType<ArrowsPixelGame>();
            if (game == null) { Debug.LogError("[Booster] ArrowsPixelGame sahnede yok."); return; }
            SerializedObject so = new SerializedObject(game);
            int n = 0;
            n += WireOneCountText(so, "destroyFillerButton", "destroyFillerCountText") ? 1 : 0;
            n += WireOneCountText(so, "extraExitButton", "extraExitCountText") ? 1 : 0;
            n += WireOneCountText(so, "shuffleButton", "shuffleCountText") ? 1 : 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Debug.Log("[Booster] " + n + " adet yazisi (CountText) olusturuldu/atandi. Konum & stil sende - CountText objelerini tasi.");
        }

        static bool WireOneCountText(SerializedObject so, string btnProp, string txtProp)
        {
            SerializedProperty bp = so.FindProperty(btnProp);
            if (bp == null || bp.objectReferenceValue == null) return false;
            Button btn = bp.objectReferenceValue as Button;
            if (btn == null) return false;

            Transform existing = btn.transform.Find("CountText");
            TMPro.TextMeshProUGUI tm;
            if (existing == null)
            {
                GameObject go = new GameObject("CountText", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Wire Count Text");
                go.transform.SetParent(btn.transform, false);
                tm = go.AddComponent<TMPro.TextMeshProUGUI>();
                tm.text = "x3";
                tm.alignment = TMPro.TextAlignmentOptions.BottomRight;
                tm.fontSize = 40f;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.color = new Color(1f, 0.93f, 0.30f);
                tm.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else tm = existing.GetComponent<TMPro.TextMeshProUGUI>();

            SerializedProperty tp = so.FindProperty(txtProp);
            if (tp != null) tp.objectReferenceValue = tm;
            return true;
        }

        static void SetRef(SerializedObject so, string prop, Object value)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning("[Arrows] ArrowsPixelGame'de '" + prop + "' alani bulunamadi.");
        }

        static void BuildParkingWalls(Transform parent)
        {
            Color wall = new Color(0.78f, 0.56f, 0.62f);
            const float sx = 5.75f;
            const float sz = 4.25f;
            const float wallH = 0.34f;
            const float wallT = 0.14f;
            const float exitGap = 1.35f;
            float halfX = sx * 0.5f;
            float halfZ = sz * 0.5f;
            float wallY = 0.20f - TruckGroundY;
            float topZ = halfZ - wallT * 0.5f;
            float sideLen = (sx - exitGap) * 0.5f;
            float sideCenterX = exitGap * 0.5f + sideLen * 0.5f;

            Box("ParkWall_Left", parent, new Vector3(-halfX + wallT * 0.5f, wallY, 0f), new Vector3(wallT, wallH, sz), wall);
            Box("ParkWall_Right", parent, new Vector3(halfX - wallT * 0.5f, wallY, 0f), new Vector3(wallT, wallH, sz), wall);
            Box("ParkWall_Back", parent, new Vector3(0f, wallY, -halfZ + wallT * 0.5f), new Vector3(sx, wallH, wallT), wall);
            Box("ParkWall_TopLeft", parent, new Vector3(-sideCenterX, wallY, topZ), new Vector3(sideLen, wallH, wallT), wall);
            Box("ParkWall_TopRight", parent, new Vector3(sideCenterX, wallY, topZ), new Vector3(sideLen, wallH, wallT), wall);
        }

        // ---------- gorsel yardimcilar (ArrowsPixelGame.Box / Pad ile ayni sekil) ----------
        static Material Mat(Color c)
        {
            Shader sh = Shader.Find("Color Cargo Loop/Toon Plastic");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh) { name = "APXBuild_" + ColorUtility.ToHtmlStringRGB(c) };
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0f);
            m.color = c;
            return m;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return g;
        }

        // Yuvarlak kose pad (arti + 4 disk) - h: pad yuksekligi
        static void Pad(string name, Transform parent, Vector3 center, float sx, float sz, Color color, float h)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = center;
            float r = Mathf.Min(sx, sz) * 0.28f;
            Box("body", p.transform, Vector3.zero, new Vector3(sx, h, sz - 2 * r), color);
            Box("bodyX", p.transform, Vector3.zero, new Vector3(sx - 2 * r, h, sz), color);
            for (int i = 0; i < 4; i++)
            {
                float ox = (i % 2 == 0 ? 1 : -1) * (sx * 0.5f - r);
                float oz = (i < 2 ? 1 : -1) * (sz * 0.5f - r);
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = "corner"; c.transform.SetParent(p.transform, false);
                c.transform.localPosition = new Vector3(ox, 0f, oz);
                c.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.GetComponent<Renderer>().sharedMaterial = Mat(color);
            }
        }
    }
}

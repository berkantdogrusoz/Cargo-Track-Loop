using ColorCargoLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

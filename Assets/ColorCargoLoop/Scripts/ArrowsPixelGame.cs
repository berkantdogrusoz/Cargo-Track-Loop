using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// REDESIGN: Arrows (Puzzle Escape) x Pixel Flow karmasi.
    /// Faz 1 = STATIK LAYOUT mockup:
    ///   - UST: kuplerden pixel-art resim (candy renk) - kup kaynagi
    ///   - ORTA: 3 slot (park alani)
    ///   - ALT: yatay dikdortgen alanda KARISIK dizili tirlar, her birinde OK (kafa yonu)
    /// Mekanik (Faz 2+) sonra eklenecek.
    /// </summary>
    public sealed class ArrowsPixelGame : MonoBehaviour
    {
        [SerializeField] private GameObject truckPrefab;   // ToyTruck.glb (yoksa kutu stand-in)
        [SerializeField] private Texture2D arrowTexture;   // Assets/Art/Ok.png (default saga = +X = kafa yonu)
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool buildOnStart = true;

        Sprite arrowSprite;

        // Candy pastel palet (CargoColorPalette ile ayni aile)
        static readonly Color C_PINK   = new Color(0.99f, 0.48f, 0.54f);
        static readonly Color C_BLUE   = new Color(0.56f, 0.81f, 1.00f);
        static readonly Color C_YELLOW = new Color(1.00f, 0.83f, 0.43f);
        static readonly Color C_GREEN  = new Color(0.57f, 0.90f, 0.68f);
        static readonly Color C_PURPLE = new Color(0.77f, 0.63f, 1.00f);
        static readonly Color C_CREAM  = new Color(0.99f, 0.92f, 0.80f);
        static readonly Color C_FLOOR  = new Color(0.95f, 0.71f, 0.74f);
        static readonly Color C_PAD    = new Color(0.99f, 0.86f, 0.74f);

        Transform root;
        ColorCargoLoopGame oldGame;   // birebir uçan kup mesh/material kaynagi
        readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        void Start()
        {
            // Eski oyun ayni sahnedeyse: REFERANSINI AL (kup uretici icin), sonra devre disi birak (loop kurmasin)
            oldGame = FindObjectOfType<ColorCargoLoopGame>();
            if (oldGame != null) oldGame.gameObject.SetActive(false);

            if (buildOnStart) BuildLayout();
        }

        // Pixel-art tile = BIREBIR uçan kargo kupu (GetRoundedCargoMesh + candy toon), basik+sik tile olarak
        void PicCube(Transform parent, string name, Vector3 pos, Vector3 scale, char ch)
        {
            if (oldGame != null)
            {
                GameObject cube = oldGame.CreateCargoBlockObject(CharToCargo(ch), name);
                cube.transform.SetParent(parent, false);
                cube.transform.localPosition = pos;
                cube.transform.localScale = scale;
                cube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // resim golge atmasin (blob olmasin)
                // Kup gorunumu (kullanici tarifi): toon outline + ON/KENAR isik yansimasi (rim+highlight) + ust hafif golge
                var pm = cube.GetComponent<Renderer>().sharedMaterial;
                if (pm != null)
                {
                    if (pm.HasProperty("_OutlineWidth")) pm.SetFloat("_OutlineWidth", 0.032f);
                    if (pm.HasProperty("_RimStrength")) pm.SetFloat("_RimStrength", 0.12f);             // HAFIF dogal kenar isigi (beyaz patlama yok)
                    if (pm.HasProperty("_RimColor")) pm.SetColor("_RimColor", new Color(1f, 0.95f, 0.88f));
                    if (pm.HasProperty("_HighlightStrength")) pm.SetFloat("_HighlightStrength", 0.26f); // dogal glint
                    if (pm.HasProperty("_HighlightColor")) pm.SetColor("_HighlightColor", new Color(1f, 0.98f, 0.92f));
                    if (pm.HasProperty("_ShadeStrength")) pm.SetFloat("_ShadeStrength", 0.42f);          // ust hafif golge
                }
            }
            else
            {
                var g = RoundedCube(name, parent, pos, 1f, CharColor(ch), 0.010f);
                g.transform.localScale = scale;
            }
        }

        static CargoColor CharToCargo(char ch)
        {
            switch (ch)
            {
                case 'B': return CargoColor.Blue;
                case 'Y': return CargoColor.Yellow;
                case 'G': return CargoColor.Green;
                case 'U': return CargoColor.Purple;
                default:  return CargoColor.Red; // P = candy pembe-kirmizi
            }
        }

        static Color CharColor(char ch)
        {
            return ch == 'B' ? C_BLUE : ch == 'Y' ? C_YELLOW : ch == 'G' ? C_GREEN : ch == 'U' ? C_PURPLE : C_PINK;
        }

        public void BuildLayout()
        {
            if (root != null) DestroyImmediate(root.gameObject);
            root = new GameObject("ArrowsPixelRoot").transform;

            BuildBackground();
            BuildPictureGrid(5.0f);     // ust (yuksek Z)
            BuildSlots(0.9f);           // orta
            BuildParking(-2.7f);        // alt
            SetupCamera();
        }

        // ---------- Materyal ----------
        Material Mat(Color c)
        {
            string key = c.r.ToString("0.00") + c.g.ToString("0.00") + c.b.ToString("0.00");
            Material m;
            if (matCache.TryGetValue(key, out m)) return m;
            Shader sh = Shader.Find("Color Cargo Loop/Toon Plastic");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(sh) { name = "APX_" + key };
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.color = c;
            matCache[key] = m;
            return m;
        }

        GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Color color, float outline = -1f)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            DestroyImmediate(g.GetComponent<Collider>());
            var rend = g.GetComponent<Renderer>();
            var m = Mat(color);
            if (outline >= 0f && m.HasProperty("_OutlineWidth"))
            {
                // tekil kopya olustur ki outline degisikligi paylasilani bozmasin
                m = new Material(m); m.SetFloat("_OutlineWidth", outline);
            }
            rend.sharedMaterial = m;
            return g;
        }

        static Mesh _cubeMesh;
        // Pixel-art kup: TEMIZ primitive kup + toon (MeshUtils.RoundedCube geometri artifact/z-fight veriyordu)
        GameObject RoundedCube(string name, Transform parent, Vector3 pos, float size, Color color, float outline)
        {
            if (_cubeMesh == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(temp);
            }
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = Vector3.one * size;
            var mf = g.AddComponent<MeshFilter>(); mf.sharedMesh = _cubeMesh;
            var mr = g.AddComponent<MeshRenderer>();
            var m = Mat(color);
            if (outline >= 0f)
            {
                m = new Material(m);
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
                if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", 0.30f); // picture net okunsun (yumusak golge)
            }
            mr.sharedMaterial = m;
            return g;
        }

        // Yuvarlak kose zemin pad (arti + 4 disk)
        void Pad(string name, Transform parent, Vector3 center, float sx, float sz, Color color, float h = 0.08f)
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
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = "corner"; c.transform.SetParent(p.transform, false);
                c.transform.localPosition = new Vector3(ox, 0f, oz);
                c.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
                DestroyImmediate(c.GetComponent<Collider>());
                c.GetComponent<Renderer>().sharedMaterial = Mat(color);
            }
        }

        // ---------- Arka plan ----------
        void BuildBackground()
        {
            Box("BG", root, new Vector3(0f, -0.2f, 0.5f), new Vector3(14f, 0.2f, 22f), C_FLOOR);
        }

        // ---------- UST: pixel-art resim ----------
        void BuildPictureGrid(float topZ)
        {
            // P=pembe B=mavi Y=sari G=yesil U=mor .=bos  (basit kalp + ic karisik renkler)
            string[] pic =
            {
                ".PPP...PPP.",
                "PPPPP.PPPPP",
                "PPGPPBPPUPP",
                "PPPPYPYPPPP",
                ".PPPPPPPPP.",
                "..PPPPPPP..",
                "...PPPPP...",
                "....PPP....",
                ".....P.....",
            };
            float stepX = 0.30f, stepZ = 0.36f;                  // DIP DIBE (bosluk yok) - step = tile boyu
            Vector3 tileScale = new Vector3(0.31f, 0.30f, 0.37f); // biraz daha UZUN (Z), dip dibe icin tile>step
            GameObject grid = new GameObject("PictureGrid");
            grid.transform.SetParent(root, false);
            int cols = pic[0].Length, rows = pic.Length;

            // Pixel Flow gibi: tahtayi KAMERAYA dik tilt et -> kup TEPE yuzleri kameraya bakar -> kup belli olur
            grid.transform.localPosition = new Vector3(0f, 1.2f, 3.4f);
            grid.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = pic[r][c];
                    if (ch == '.') continue;
                    Vector3 lp = new Vector3((c - (cols - 1) * 0.5f) * stepX, 0f, ((rows - 1) * 0.5f - r) * stepZ);
                    PicCube(grid.transform, "Px_" + r + "_" + c, lp, tileScale, ch);
                }
            }
        }

        // ---------- ORTA: 3 slot ----------
        void BuildSlots(float z)
        {
            GameObject slots = new GameObject("Slots");
            slots.transform.SetParent(root, false);
            float gap = 1.65f;
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * gap;
                Pad("Slot_" + i, slots.transform, new Vector3(x, 0.04f, z), 1.45f, 1.25f, C_CREAM, 0.10f);
                // ince cerceve hissi icin koyu pad alt
                Pad("SlotRim_" + i, slots.transform, new Vector3(x, 0.02f, z), 1.62f, 1.42f, C_PAD, 0.06f);
            }
        }

        // ---------- ALT: park alani + karisik oklu tirlar ----------
        void BuildParking(float centerZ)
        {
            GameObject park = new GameObject("Parking");
            park.transform.SetParent(root, false);
            Pad("ParkGround", park.transform, new Vector3(0f, 0.02f, centerZ), 5.2f, 3.6f, C_PAD, 0.10f);

            // Karisik dizilim: (xOffset, zOffset, yawDeg, color)
            var defs = new (float x, float z, float yaw, Color col)[]
            {
                (-1.6f,  1.0f,   0f, C_BLUE),
                ( 0.2f,  1.1f,  90f, C_PINK),
                ( 1.7f,  0.7f, 180f, C_YELLOW),
                (-1.5f, -0.6f,  45f, C_GREEN),
                ( 0.5f, -0.5f, 270f, C_PURPLE),
                ( 1.6f, -0.9f, 135f, C_PINK),
            };
            foreach (var d in defs)
            {
                Vector3 pos = new Vector3(d.x, 0.12f, centerZ + d.z);
                BuildTruck(park.transform, pos, d.yaw, d.col);
            }
        }

        void BuildTruck(Transform parent, Vector3 pos, float yawDeg, Color color)
        {
            GameObject t = new GameObject("Truck");
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos;
            t.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            if (truckPrefab != null)
            {
                GameObject body = Instantiate(truckPrefab, t.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // kabin -> +X (ok yonu ile ayni)
                body.transform.localScale = Vector3.one * 0.55f;
                // govde tint
                foreach (var rnd in body.GetComponentsInChildren<Renderer>())
                {
                    var mats = rnd.materials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        if (mats[j] == null) continue;
                        string n = mats[j].name.ToLowerInvariant();
                        if (n.Contains("wheel") || n.Contains("glass") || n.Contains("hub")) continue;
                        if (!n.Contains("body")) continue;
                        if (mats[j].HasProperty("baseColorFactor")) mats[j].SetColor("baseColorFactor", color);
                        if (mats[j].HasProperty("_BaseColor")) mats[j].SetColor("_BaseColor", color);
                        if (mats[j].HasProperty("_Color")) mats[j].SetColor("_Color", color);
                    }
                    rnd.materials = mats;
                }
            }
            else
            {
                // stand-in: yuvarlak govde kutusu
                Box("body", t.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.05f, 0.42f, 0.66f), color, 0.012f);
                Box("cabin", t.transform, new Vector3(0.32f, 0.34f, 0f), new Vector3(0.42f, 0.40f, 0.6f), color, 0.012f);
            }

            BuildArrow(t.transform, new Vector3(0.30f, 0.55f, 0f)); // KAFA (+X) tarafina
        }

        // Tir uzerinde DUZ YATAN ok sprite'i (Assets/Art/Ok.png) - saga(+X)=kafa yonu, yaw ile doner
        void BuildArrow(Transform parent, Vector3 localPos)
        {
            if (arrowTexture == null) return;
            if (arrowSprite == null)
                arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, arrowTexture.width, arrowTexture.height),
                                            new Vector2(0.5f, 0.5f), arrowTexture.width);
            GameObject a = new GameObject("Arrow");
            a.transform.SetParent(parent, false);
            a.transform.localPosition = localPos;
            a.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // yere yatir (yukari bakar), ok local +X'e isaret eder
            a.transform.localScale = Vector3.one * 0.62f;
            var sr = a.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.color = Color.white; // Ok.png kendi rengi (tint YOK)
            sr.sortingOrder = 50;
        }

        // ---------- Kamera ----------
        void SetupCamera()
        {
            Camera cam = gameCamera != null ? gameCamera : Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);   // eski candy egik aci (duz tepeden DEGIL)
            Vector3 fwd = cam.transform.forward;
            Vector3 lookAt = new Vector3(0f, 0.3f, 0.25f);            // layout merkezi
            cam.transform.position = lookAt - fwd * 13f;
            cam.orthographicSize = 7.0f;
            cam.backgroundColor = C_FLOOR;

            // Isik TIRLARIN tarafindan (on-ust, yaw 0) -> kup onleri parlar, golge yukari/arkaya (blob yok)
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type != LightType.Directional || !l.gameObject.activeInHierarchy) continue;
                l.transform.eulerAngles = new Vector3(50f, 0f, 0f);
                l.intensity = 1.05f;
            }
        }
    }
}

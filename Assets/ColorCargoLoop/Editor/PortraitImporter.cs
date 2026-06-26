using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// PNG pixel-art -> potre grid (6 oyun rengine quantize) donusturucu.
    /// Kullanim: pixel-art PNG'lerini Assets/Art/Portraits/ klasorune koy (6 renk + saydam = bos),
    /// menu: "Color Cargo Loop/Import Portraits (PNG -> Potre)". Cikan PortraitSet.asset'i
    /// ArrowsPixelGame > Portrait Set alanina ata. Icerik dikeyse cerceve otomatik dikey olur.
    /// </summary>
    public static class PortraitImporter
    {
        const string PortraitFolder = "Assets/Art/Portraits";
        const string SetPath = "Assets/Art/Portraits/PortraitSet.asset";
        const int TargetHeight = 32;       // potre yuksekligi (hucre); 32x32 item pack'leri icin 1:1 (~1024 kup tam-resim)
        const float AlphaThreshold = 0.5f;
        const bool FullScene = true;        // saydam zemini renkli doldur -> Pixel Flow tam-resim (item bir renkli sahnede). false = ikon stili

        struct Pal { public char ch; public Color col; public Pal(char c, Color k) { ch = c; col = k; } }

        // CargoColorPalette ile AYNI 6 renk + char (P/B/Y/G/U/O)
        static readonly Pal[] Palette =
        {
            new Pal('P', new Color(0.99f, 0.48f, 0.54f)), // Red
            new Pal('B', new Color(0.56f, 0.81f, 1.00f)), // Blue
            new Pal('Y', new Color(1.00f, 0.83f, 0.43f)), // Yellow
            new Pal('G', new Color(0.57f, 0.90f, 0.68f)), // Green
            new Pal('U', new Color(0.77f, 0.63f, 1.00f)), // Purple
            new Pal('O', new Color(1.00f, 0.69f, 0.43f)), // Orange
        };

        [MenuItem("Color Cargo Loop/Import Portraits (PNG -> Potre)")]
        public static void ImportAll()
        {
            if (!Directory.Exists(PortraitFolder))
            {
                Directory.CreateDirectory(PortraitFolder);
                AssetDatabase.Refresh();
                Debug.LogWarning("Portre klasoru olusturuldu: " + PortraitFolder + " -> pixel-art PNG'lerini buraya koy, menuyu tekrar calistir.");
                return;
            }

            var set = AssetDatabase.LoadAssetAtPath<ArrowsPixelPortraitSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ArrowsPixelPortraitSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.portraits.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PortraitFolder });
            int n = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.ToLower().EndsWith(".png")) continue;
                if (Path.GetFileName(path).StartsWith("sample_")) continue; // procedural yer tutuculari atla
                string[] rows = ConvertTexture(path);
                if (rows == null) continue;
                if (FullScene) rows = ApplyFullScene(rows, n);   // item + renkli zemin (tam-resim)
                set.portraits.Add(new ArrowsPixelPortraitSet.Entry
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    rows = rows,
                    preview = string.Join("\n", rows)
                });
                n++;
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Potre importu bitti: " + n + " gorsel -> " + SetPath + " . Bu asset'i ArrowsPixelGame > 'Portrait Set' alanina ata.");
            Selection.activeObject = set;
        }

        static string[] ConvertTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                // Pixel-art'i BOZMADAN oku: okunabilir + sikistirmasiz + NPOT olceklemeden + mipmapsiz (boyut birebir korunur)
                bool need = !importer.isReadable
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.npotScale != TextureImporterNPOTScale.None
                    || importer.mipmapEnabled
                    || importer.maxTextureSize < 2048;
                if (need)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;

            int sw = tex.width, sh = tex.height;
            int th = Mathf.Clamp(TargetHeight, 8, 48);
            int tw = Mathf.Max(4, Mathf.RoundToInt((float)sw / sh * th));
            tw = Mathf.Min(tw, th); // dikey kalsin (genislik <= yukseklik)

            Color[] src = tex.GetPixels();
            string[] rows = new string[th];
            var sb = new StringBuilder();
            for (int ry = 0; ry < th; ry++)
            {
                sb.Length = 0;
                for (int rx = 0; rx < tw; rx++)
                {
                    int px = Mathf.Clamp(Mathf.FloorToInt((rx + 0.5f) / tw * sw), 0, sw - 1);
                    int py = Mathf.Clamp(Mathf.FloorToInt((th - 1 - ry + 0.5f) / th * sh), 0, sh - 1); // y flip (texture bottom-up)
                    Color c = src[py * sw + px];
                    sb.Append(c.a < AlphaThreshold ? '.' : Nearest(c));
                }
                rows[ry] = sb.ToString();
            }
            return rows;
        }

        static char Nearest(Color c)
        {
            float best = float.MaxValue; char bch = 'P';
            foreach (var p in Palette)
            {
                float dr = c.r - p.col.r, dg = c.g - p.col.g, db = c.b - p.col.b;
                float d = dr * dr + dg * dg + db * db;
                if (d < best) { best = d; bch = p.ch; }
            }
            return bch;
        }

        // --- 8 farkli dikey pixel-art PNG uretir (kalp/ay/yildiz/kedi/balik/hayalet/cicek/mantar).
        //     Gercek sanat gelene kadar her level farkli + daha kaliteli sekil icin. ---
        [MenuItem("Color Cargo Loop/Create Sample Portrait PNGs (8 sekil)")]
        public static void CreateSample()
        {
            if (!Directory.Exists(PortraitFolder)) Directory.CreateDirectory(PortraitFolder);
            // Eski sample_*.png'leri temizle (gercek sanat baska isimde -> dokunulmaz)
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { PortraitFolder }))
            {
                string pp = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileName(pp).StartsWith("sample_")) AssetDatabase.DeleteAsset(pp);
            }

            Color P = Palette[0].col, B = Palette[1].col, Y = Palette[2].col, G = Palette[3].col, U = Palette[4].col, O = Palette[5].col;
            const int W = 36, HH = 46; float cx = W * 0.5f - 0.5f; int icx = Mathf.RoundToInt(cx);

            // 1) AY: mor gece zemini + sari hilal + arti yildizlar (Pixel Flow referansi)
            SavePng("sample_moon", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, U);
                FillCircle(t, H, cx - 2f, 23f, 12f, Y);
                FillCircle(t, H, cx + 5f, 20f, 11f, U);   // hilal oyugu (zemin rengiyle oy)
                Plus(t, H, 7, 9, 2, O); Plus(t, H, 29, 12, 2, Y); Plus(t, H, 31, 33, 2, O);
                Plus(t, H, 6, 36, 2, Y); Plus(t, H, 26, 39, 1, O); Plus(t, H, 12, 6, 1, Y);
            });
            // 2) KALP: mavi zemin + kirmizi kalp + sari sparkle
            SavePng("sample_heart", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, B);
                FillCircle(t, H, cx - 6f, 17f, 7f, P);
                FillCircle(t, H, cx + 6f, 17f, 7f, P);
                FillTri(t, H, cx - 12.5f, 19f, cx + 12.5f, 19f, cx, 37f, P);
                FillCircle(t, H, cx - 7f, 14f, 2.2f, O);
                Plus(t, H, 6, 9, 2, Y); Plus(t, H, 30, 12, 2, Y); Plus(t, H, 28, 39, 1, Y); Plus(t, H, 7, 40, 1, Y);
            });
            // 3) YILDIZ: mor zemin + buyuk sari yildiz + turuncu yildizlar
            SavePng("sample_star", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, U);
                FillDiamond(t, H, cx, 23f, 5f, 18f, Y);
                FillDiamond(t, H, cx, 23f, 18f, 5f, Y);
                FillDiamond(t, H, cx, 23f, 10f, 10f, Y);
                FillCircle(t, H, cx, 23f, 3.5f, O);
                Plus(t, H, 6, 8, 2, O); Plus(t, H, 30, 10, 1, Y); Plus(t, H, 7, 40, 1, O); Plus(t, H, 30, 40, 2, Y);
            });
            // 4) KEDI: sari zemin + mor kedi + turuncu yildiz
            SavePng("sample_cat", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, Y);
                FillTri(t, H, cx - 11f, 16f, cx - 3f, 7f, cx - 3f, 17f, U);
                FillTri(t, H, cx + 11f, 16f, cx + 3f, 7f, cx + 3f, 17f, U);
                FillCircle(t, H, cx, 25f, 12f, U);
                FillTri(t, H, cx - 8f, 15f, cx - 4f, 10f, cx - 4f, 16f, P);
                FillTri(t, H, cx + 8f, 15f, cx + 4f, 10f, cx + 4f, 16f, P);
                FillCircle(t, H, cx - 5f, 23f, 2.2f, G);
                FillCircle(t, H, cx + 5f, 23f, 2.2f, G);
                FillTri(t, H, cx - 2f, 27f, cx + 2f, 27f, cx, 30f, P);
                FillCircle(t, H, cx - 8f, 28f, 1.6f, O);
                FillCircle(t, H, cx + 8f, 28f, 1.6f, O);
                Plus(t, H, 6, 8, 2, O); Plus(t, H, 30, 40, 2, O);
            });
            // 5) BALIK: mavi su + turuncu balik + sari kabarciklar
            SavePng("sample_fish", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, B);
                FillEllipse(t, H, cx - 1f, 24f, 13f, 8f, O);
                FillTri(t, H, cx + 9f, 24f, cx + 17f, 17f, cx + 17f, 31f, O);
                FillEllipse(t, H, cx + 3f, 24f, 2f, 7f, Y);
                FillCircle(t, H, cx - 7f, 22f, 2f, P);
                FillCircle(t, H, 28f, 10f, 1.6f, Y); FillCircle(t, H, 31f, 14f, 1.2f, Y); FillCircle(t, H, 6f, 36f, 1.4f, Y);
            });
            // 6) HAYALET: mor gece + mavi hayalet + sari yildiz
            SavePng("sample_ghost", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, U);
                FillCircle(t, H, cx, 17f, 11f, B);
                FillRect(t, H, icx - 11, 17, icx + 11, 33, B);
                FillCircle(t, H, cx - 7f, 33f, 3.2f, U);
                FillCircle(t, H, cx, 33f, 3.2f, U);
                FillCircle(t, H, cx + 7f, 33f, 3.2f, U);
                FillEllipse(t, H, cx - 4f, 16f, 2f, 3f, U);
                FillEllipse(t, H, cx + 4f, 16f, 2f, 3f, U);
                FillCircle(t, H, cx - 7f, 21f, 1.8f, O);
                FillCircle(t, H, cx + 7f, 21f, 1.8f, O);
                Plus(t, H, 6, 9, 2, Y); Plus(t, H, 30, 12, 1, Y); Plus(t, H, 29, 40, 2, Y);
            });
            // 7) CICEK: mavi gokyuzu + sari gunes + kirmizi cicek + yesil sap
            SavePng("sample_flower", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, B);
                FillCircle(t, H, 31f, 8f, 4f, Y);
                FillCircle(t, H, cx, 11f, 5f, P);
                FillCircle(t, H, cx - 7f, 16f, 5f, P);
                FillCircle(t, H, cx + 7f, 16f, 5f, P);
                FillCircle(t, H, cx - 4f, 22f, 5f, P);
                FillCircle(t, H, cx + 4f, 22f, 5f, P);
                FillCircle(t, H, cx, 16.5f, 4.5f, Y);
                FillRect(t, H, icx - 1, 23, icx + 1, 41, G);
                FillEllipse(t, H, cx - 5f, 31f, 4f, 2f, G);
                FillEllipse(t, H, cx + 5f, 35f, 4f, 2f, G);
            });
            // 8) MANTAR: yesil cimen + kirmizi sapka + sari sap
            SavePng("sample_mushroom", W, HH, (t, H) =>
            {
                FillRect(t, H, 0, 0, W - 1, HH - 1, G);
                FillEllipse(t, H, cx, 18f, 13f, 8f, P);
                FillRect(t, H, icx - 13, 18, icx + 13, 21, P);
                FillRect(t, H, icx - 5, 21, icx + 5, 37, Y);
                FillEllipse(t, H, cx, 37f, 5f, 3f, Y);
                FillCircle(t, H, cx - 5f, 14f, 2.5f, Y);
                FillCircle(t, H, cx + 4f, 17f, 2f, Y);
                FillCircle(t, H, cx, 11f, 1.8f, Y);
                FillCircle(t, H, 6f, 40f, 1.6f, O); FillCircle(t, H, 30f, 41f, 1.4f, O);
            });

            AssetDatabase.Refresh();
            Debug.Log("8 TAM RESIM potre olusturuldu (Pixel Flow stili: dolu zemin + sekil + yildiz). Simdi 'Import Portraits' menusunu calistir.");
        }

        // Saydam zemini renkli doldur + kenarlara serpme yildiz -> Pixel Flow "tam resim" (item bir renkli sahnede)
        static readonly char[] SceneBg = { 'U', 'B', 'G', 'P', 'O', 'Y' };
        static string[] ApplyFullScene(string[] rows, int idx)
        {
            if (rows == null || rows.Length == 0) return rows;
            char bg = SceneBg[idx % SceneBg.Length];
            char acc = (bg == 'Y') ? 'O' : 'Y';
            int H = rows.Length, W = 0;
            char[][] g = new char[H][];
            for (int y = 0; y < H; y++) { g[y] = (rows[y] ?? "").ToCharArray(); if (g[y].Length > W) W = g[y].Length; }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < g[y].Length; x++)
                    if (g[y][x] == '.') g[y][x] = bg;
            var rng = new System.Random(idx * 7919 + 3);
            for (int i = 0; i < 5; i++)
            {
                bool left = rng.Next(2) == 0;
                int x = left ? rng.Next(Mathf.Max(1, W / 5)) : W - 1 - rng.Next(Mathf.Max(1, W / 5));
                int y = 2 + rng.Next(Mathf.Max(1, H - 4));
                SetStar(g, x, y, acc);
            }
            string[] outR = new string[H];
            for (int y = 0; y < H; y++) outR[y] = new string(g[y]);
            return outR;
        }

        static void SetStar(char[][] g, int x, int y, char c)
        {
            int H = g.Length;
            System.Action<int, int> P = (xx, yy) => { if (yy >= 0 && yy < H && xx >= 0 && xx < g[yy].Length) g[yy][xx] = c; };
            P(x, y); P(x - 1, y); P(x + 1, y); P(x, y - 1); P(x, y + 1);
        }

        static void SavePng(string name, int W, int H, System.Action<Texture2D, int> draw)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < W * H; i++) tex.SetPixel(i % W, i / W, clear);
            draw(tex, H);
            tex.Apply();
            File.WriteAllBytes(PortraitFolder + "/" + name + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // yardimcilar: image-coord (yTop=0 ust) ile cizer, texture'a flip'leyerek yazar
        static void FillRect(Texture2D t, int H, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && x < t.width && y >= 0 && y < H) t.SetPixel(x, H - 1 - y, c);
        }

        static void FillEllipse(Texture2D t, int H, float cx, float cy, float rx, float ry, Color c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < t.width; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) t.SetPixel(x, H - 1 - y, c);
                }
        }

        static void FillCircle(Texture2D t, int H, float cx, float cy, float r, Color c)
        {
            FillEllipse(t, H, cx, cy, r, r, c);
        }

        // kucuk arti yildiz (Pixel Flow sparkle), 2px kalin kollar (downscale'de kaybolmasin)
        static void Plus(Texture2D t, int H, int cx, int cy, int r, Color c)
        {
            FillRect(t, H, cx, cy - r, cx + 1, cy + r, c);
            FillRect(t, H, cx - r, cy, cx + r, cy + 1, c);
        }

        static void FillDiamond(Texture2D t, int H, float cx, float cy, float rx, float ry, Color c)
        {
            if (rx <= 0f || ry <= 0f) return;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < t.width; x++)
                {
                    float dx = Mathf.Abs(x - cx) / rx, dy = Mathf.Abs(y - cy) / ry;
                    if (dx + dy <= 1f) t.SetPixel(x, H - 1 - y, c);
                }
        }

        // ucgen (barycentric): koordinatlar image-coord (yTop=0), texture'a flip'lenerek yazilir
        static void FillTri(Texture2D t, int H, float ax, float ay, float bx, float by, float cx2, float cy2, Color col)
        {
            float minX = Mathf.Min(ax, Mathf.Min(bx, cx2)), maxX = Mathf.Max(ax, Mathf.Max(bx, cx2));
            float minY = Mathf.Min(ay, Mathf.Min(by, cy2)), maxY = Mathf.Max(ay, Mathf.Max(by, cy2));
            float d = (by - cy2) * (ax - cx2) + (cx2 - bx) * (ay - cy2);
            if (Mathf.Abs(d) < 1e-6f) return;
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                {
                    if (x < 0 || x >= t.width || y < 0 || y >= H) continue;
                    float a = ((by - cy2) * (x - cx2) + (cx2 - bx) * (y - cy2)) / d;
                    float b = ((cy2 - ay) * (x - cx2) + (ax - cx2) * (y - cy2)) / d;
                    float cc = 1f - a - b;
                    if (a >= -0.02f && b >= -0.02f && cc >= -0.02f) t.SetPixel(x, H - 1 - y, col);
                }
        }
    }
}

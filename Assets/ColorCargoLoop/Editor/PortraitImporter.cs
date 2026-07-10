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
        const int TargetHeight = 32;       // potre yuksekligi (hucre); 32 = eski iri kup boyutu/orani (kullanici tercihi). Box-filter+canlandirma ile sade okunur.
        const float AlphaThreshold = 0.5f;
        const bool FullScene = true;        // saydam zemini renkli doldur -> Pixel Flow tam-resim (item bir renkli sahnede). false = ikon stili

        struct Pal { public char ch; public Color col; public Pal(char c, Color k) { ch = c; col = k; } }

        // CargoColorPalette ile AYNI renkler + char (P/B/Y/G/U/O + K/C/T/L/W/N). Obstacle (siyah) BURADA YOK -> portreye girmez.
        static readonly Pal[] Palette =
        {
            new Pal('P', new Color(0.99f, 0.48f, 0.54f)), // Red
            new Pal('B', new Color(0.56f, 0.81f, 1.00f)), // Blue
            new Pal('Y', new Color(1.00f, 0.83f, 0.43f)), // Yellow
            new Pal('G', new Color(0.57f, 0.90f, 0.68f)), // Green
            new Pal('U', new Color(0.77f, 0.63f, 1.00f)), // Purple
            new Pal('O', new Color(1.00f, 0.69f, 0.43f)), // Orange
            new Pal('K', new Color(0.97f, 0.55f, 0.80f)), // Pink
            new Pal('C', new Color(0.45f, 0.87f, 0.92f)), // Cyan
            new Pal('T', new Color(0.22f, 0.68f, 0.62f)), // Teal
            new Pal('L', new Color(0.74f, 0.92f, 0.38f)), // Lime
            new Pal('W', new Color(0.68f, 0.50f, 0.37f)), // Brown
            new Pal('N', new Color(0.46f, 0.45f, 0.85f)), // Indigo
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
                    importer.filterMode = FilterMode.Bilinear;
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
                    // BOX FILTER: bu hucrenin kapsadigi kaynak blogunun ORTALAMASI -> detayli resim "rastgele" degil temsil edilir
                    int sx0 = Mathf.Clamp(Mathf.FloorToInt((float)rx / tw * sw), 0, sw - 1);
                    int sx1 = Mathf.Clamp(Mathf.CeilToInt((float)(rx + 1) / tw * sw), sx0 + 1, sw);
                    int sy0 = Mathf.Clamp(Mathf.FloorToInt((float)(th - 1 - ry) / th * sh), 0, sh - 1); // y flip (texture bottom-up)
                    int sy1 = Mathf.Clamp(Mathf.CeilToInt((float)(th - ry) / th * sh), sy0 + 1, sh);
                    float ar = 0f, ag = 0f, ab = 0f, aa = 0f; int cnt = 0;
                    for (int yy = sy0; yy < sy1; yy++)
                        for (int xx = sx0; xx < sx1; xx++)
                        {
                            Color c = src[yy * sw + xx];
                            ar += c.r; ag += c.g; ab += c.b; aa += c.a; cnt++;
                        }
                    if (cnt > 0) { ar /= cnt; ag /= cnt; ab /= cnt; aa /= cnt; }
                    if (aa < AlphaThreshold) { sb.Append('.'); }
                    else
                    {
                        // CANLANDIR: doygunluk + kontrast artir -> bolgeler Pixel Flow gibi daha net/parlak ayrissin
                        float hh, ss, vv; Color.RGBToHSV(new Color(ar, ag, ab), out hh, out ss, out vv);
                        ss = Mathf.Clamp01(ss * 1.55f + 0.06f);       // doygunluk
                        vv = Mathf.Clamp01((vv - 0.5f) * 1.22f + 0.5f); // kontrast (acik/koyu ayrissin)
                        sb.Append(Nearest(Color.HSVToRGB(hh, ss, vv)));
                    }
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

        // ============================================================
        // ADAPTIVE: gorselin KENDI ~12 baskin rengini cikarir (median-cut + k-means) ve hucreleri ONA quantize eder.
        // Donen rows char'lari slot 0..11; gercek renkler 'palette' ile gelir (runtime CargoColorPalette.Override).
        // YOGUN AI gorselleri (diamond-art vb.) icin: yukseklik 64'e kadar, arka plan temizleme OPSIYONEL.
        // ============================================================
        const int PaletteCount = 12;
        static readonly char[] SlotChars = { 'P', 'B', 'Y', 'G', 'U', 'O', 'K', 'C', 'T', 'L', 'W', 'N' };

        // HD import ayarlari (PortraitImporterHdWindow yazar)
        public static int HdTargetHeight = 64;
        public static int HdColorCount = 12;
        public static bool HdRemoveBackground = false; // tam-sahne AI resimlerinde KAPALI kalsin (gokyuzu/zemin resmin parcasi)
        public static float HdVivid = 0f;           // 0 = renklere dokunma, 1 = agresif canlandirma

        [MenuItem("Color Cargo Loop/Import Portraits HD (Adaptive - resmin kendi renkleri)")]
        public static void ImportAllAdaptive()
        {
            if (!Directory.Exists(PortraitFolder))
            {
                Directory.CreateDirectory(PortraitFolder);
                AssetDatabase.Refresh();
                Debug.LogWarning("Portre klasoru olusturuldu: " + PortraitFolder + " -> AI gorsellerini (PNG) buraya koy, menuyu tekrar calistir.");
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
            int n = 0; int maxCells = 0; var report = new StringBuilder();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLower();
                if (!lower.EndsWith(".png") && !lower.EndsWith(".jpg") && !lower.EndsWith(".jpeg")) continue;
                if (Path.GetFileName(path).StartsWith("sample_")) continue;
                Color[] palette;
                string[] rows = ConvertTextureAdaptive(path, HdTargetHeight, HdColorCount, HdRemoveBackground, HdVivid, out palette);
                if (rows == null) continue;
                if (FullScene) rows = ApplyFullScene(rows, n); // sadece saydam hucre varsa dolgu yapar
                int cells = 0; foreach (var r in rows) foreach (var c in r) if (c != '.') cells++;
                if (cells > maxCells) maxCells = cells;
                set.portraits.Add(new ArrowsPixelPortraitSet.Entry
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    rows = rows,
                    palette = palette, // runtime CargoColorPalette.Override bunu basar -> kupler resmin GERCEK renginde
                    sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                    preview = string.Join("\n", rows) + "\n[HD adaptive: " + (palette != null ? palette.Length : 0) + " renk, " + cells + " kup]"
                });
                report.AppendLine(Path.GetFileName(path) + " -> " + rows[0].Length + "x" + rows.Length + " grid, " + cells + " kup, " + (palette != null ? palette.Length : 0) + " renk");
                n++;
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HD ADAPTIVE potre importu bitti: " + n + " gorsel -> " + SetPath + "\n" + report
                + (maxCells > 3200 ? "\nUYARI: " + maxCells + " kup tek portrede COK YOGUN - mobilde FPS'i test et (48 yukseklik ~2300 kup onerilir)." : ""));
            Selection.activeObject = set;
        }

        static string[] ConvertTextureAdaptive(string path, int targetHeight, int colorCount, bool removeBg, float vivid, out Color[] palette)
        {
            palette = null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool need = !importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled || importer.maxTextureSize < 2048 || importer.filterMode != FilterMode.Bilinear;
                if (need)
                {
                    importer.isReadable = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048; importer.SaveAndReimport();
                }
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;
            int sw = tex.width, sh = tex.height;
            int th = Mathf.Clamp(targetHeight, 8, 64);
            int tw = Mathf.Max(4, Mathf.RoundToInt((float)sw / sh * th));
            tw = Mathf.Min(tw, Mathf.RoundToInt(th * 1.1f)); // kare serbest; asiri yatay tasmasin
            Color[] src = tex.GetPixels();

            // 1) her hucre rengi: box-filter + hafif canlandirma
            Color[,] cell = new Color[th, tw];
            bool[,] solid = new bool[th, tw];
            for (int ry = 0; ry < th; ry++)
                for (int rx = 0; rx < tw; rx++)
                {
                    int sx0 = Mathf.Clamp(Mathf.FloorToInt((float)rx / tw * sw), 0, sw - 1);
                    int sx1 = Mathf.Clamp(Mathf.CeilToInt((float)(rx + 1) / tw * sw), sx0 + 1, sw);
                    int sy0 = Mathf.Clamp(Mathf.FloorToInt((float)(th - 1 - ry) / th * sh), 0, sh - 1);
                    int sy1 = Mathf.Clamp(Mathf.CeilToInt((float)(th - ry) / th * sh), sy0 + 1, sh);
                    float aa;
                    Color sampled = SampleCellColorPreserveDetail(src, sw, sx0, sx1, sy0, sy1, out aa);
                    if (aa < AlphaThreshold) { solid[ry, rx] = false; continue; }
                    float hh, ss, vv; Color.RGBToHSV(sampled, out hh, out ss, out vv);
                    ss = Mathf.Clamp01(ss * (1f + 0.9f * vivid) + 0.04f * vivid);            // canlandirma dozu ayarli
                    vv = Mathf.Clamp01((vv - 0.5f) * (1f + 0.35f * vivid) + 0.5f);
                    cell[ry, rx] = Color.HSVToRGB(hh, ss, vv); solid[ry, rx] = true;
                }

            // 1b) ARKA PLAN TEMIZLE (OPSIYONEL): kenarlardan flood-fill, KOSE rengine (bg) yakin hucreleri BOS yap.
            //     Tam-sahne AI resimlerinde KAPALI olmali (gokyuzu/zemin resmin parcasi, silinmemeli).
            if (removeBg)
            {
                Color bg = new Color(0, 0, 0); int bgn = 0;
                int[][] corners = { new[] { 0, 0 }, new[] { 0, tw - 1 }, new[] { th - 1, 0 }, new[] { th - 1, tw - 1 } };
                foreach (var co in corners) if (solid[co[0], co[1]]) { bg += cell[co[0], co[1]]; bgn++; }
                if (bgn > 0)
                {
                    bg /= bgn;
                    float bgT2 = 0.17f * 0.17f;
                    bool[,] isBg = new bool[th, tw];
                    var q = new System.Collections.Generic.Queue<int>();
                    System.Action<int, int> seed = (ry, rx) =>
                    {
                        if (ry < 0 || ry >= th || rx < 0 || rx >= tw || !solid[ry, rx] || isBg[ry, rx]) return;
                        float dr = cell[ry, rx].r - bg.r, dg = cell[ry, rx].g - bg.g, db = cell[ry, rx].b - bg.b;
                        if (dr * dr + dg * dg + db * db <= bgT2) { isBg[ry, rx] = true; q.Enqueue(ry * tw + rx); }
                    };
                    for (int rx = 0; rx < tw; rx++) { seed(0, rx); seed(th - 1, rx); }
                    for (int ry = 0; ry < th; ry++) { seed(ry, 0); seed(ry, tw - 1); }
                    while (q.Count > 0) { int ix = q.Dequeue(); int ry = ix / tw, rx = ix % tw; seed(ry - 1, rx); seed(ry + 1, rx); seed(ry, rx - 1); seed(ry, rx + 1); }
                    for (int ry = 0; ry < th; ry++) for (int rx = 0; rx < tw; rx++) if (isBg[ry, rx]) solid[ry, rx] = false;
                }
            }

            // 2) palette: KALAN (ozne) hucrelerden median-cut + k-means rafine (yogun resimde tonlar daha isabetli)
            var sample = new System.Collections.Generic.List<Color>(th * tw);
            for (int ry = 0; ry < th; ry++) for (int rx = 0; rx < tw; rx++) if (solid[ry, rx]) sample.Add(cell[ry, rx]);
            palette = MedianCut(sample, Mathf.Clamp(colorCount, 2, SlotChars.Length));
            if (palette == null || palette.Length == 0) palette = new[] { Color.gray };
            KMeansRefine(sample, palette, 4);
            EnsureSalientColors(cell, solid, th, tw, palette); // goz/kontur gibi NADIR ama KRITIK renkler slot kazansin (yoksa yuze/bg'ye erirdi)

            // 3) her hucreyi cikarilan palette'e EN YAKIN slot'a esle (algisal agirlikli mesafe)
            string[] rows = new string[th];
            var sb = new StringBuilder();
            for (int ry = 0; ry < th; ry++)
            {
                sb.Length = 0;
                for (int rx = 0; rx < tw; rx++)
                {
                    if (!solid[ry, rx]) { sb.Append('.'); continue; }
                    int best = 0; float bd = float.MaxValue;
                    for (int p = 0; p < palette.Length; p++)
                    {
                        float d = ColorDist(cell[ry, rx], palette[p]);
                        if (d < bd) { bd = d; best = p; }
                    }
                    sb.Append(best < SlotChars.Length ? SlotChars[best] : 'P');
                }
                rows[ry] = sb.ToString();
            }
            return rows;
        }

        static Color SampleCellColorPreserveDetail(Color[] src, int sw, int sx0, int sx1, int sy0, int sy1, out float alpha)
        {
            float ar = 0f, ag = 0f, ab = 0f, aa = 0f;
            int cnt = 0;
            for (int yy = sy0; yy < sy1; yy++)
            {
                for (int xx = sx0; xx < sx1; xx++)
                {
                    Color c = src[yy * sw + xx];
                    ar += c.r; ag += c.g; ab += c.b; aa += c.a; cnt++;
                }
            }
            if (cnt <= 0) { alpha = 0f; return Color.clear; }

            Color average = new Color(ar / cnt, ag / cnt, ab / cnt, aa / cnt);
            alpha = average.a;
            Color best = average;
            float bestScore = -1f;
            for (int yy = sy0; yy < sy1; yy++)
            {
                for (int xx = sx0; xx < sx1; xx++)
                {
                    Color c = src[yy * sw + xx];
                    if (c.a < AlphaThreshold) continue;
                    float h, s, v;
                    Color.RGBToHSV(c, out h, out s, out v);
                    float dr = c.r - average.r, dg = c.g - average.g, db = c.b - average.b;
                    float score = (dr * dr + dg * dg + db * db) * 1.35f + s * 0.28f + c.a * 0.08f;
                    if (score > bestScore) { bestScore = score; best = c; }
                }
            }
            return Color.Lerp(average, best, 0.55f);
        }
        // KRITIK RENK GARANTISI: palet, kucuk-ama-onemli renkleri (goz, kontur, minik vurgu) kacirmis olabilir.
        // En KOTU temsil edilen hucre rengini bulur, paletteki EN AZ kullanilan slotu onunla degistirir (2 tur).
        // Boylece resimdeki her belirgin detay renk mutlaka bir slota oturur -> "goz atlaniyor/renk degisiyor" biter.
        static void EnsureSalientColors(Color[,] cell, bool[,] solid, int th, int tw, Color[] palette)
        {
            if (palette == null || palette.Length < 3) return;
            for (int round = 0; round < 2; round++)
            {
                int[] usage = new int[palette.Length];
                float worst = 0f; Color worstColor = Color.black;
                for (int ry = 0; ry < th; ry++)
                    for (int rx = 0; rx < tw; rx++)
                    {
                        if (!solid[ry, rx]) continue;
                        int best = 0; float bd = float.MaxValue;
                        for (int p = 0; p < palette.Length; p++)
                        {
                            float d = ColorDist(cell[ry, rx], palette[p]);
                            if (d < bd) { bd = d; best = p; }
                        }
                        usage[best]++;
                        if (bd > worst) { worst = bd; worstColor = cell[ry, rx]; }
                    }
                if (worst < 0.018f) return; // her renk zaten yeterince iyi temsil ediliyor

                // worstColor'a benzeyen hucre sayisi: gercek bir detay mi, yoksa tek-pixel gurultu mu?
                int worstCount = 0;
                for (int ry = 0; ry < th; ry++)
                    for (int rx = 0; rx < tw; rx++)
                        if (solid[ry, rx] && ColorDist(cell[ry, rx], worstColor) < 0.010f) worstCount++;
                if (worstCount < 2) return; // tek pixel -> gurultu, palete alma

                int leastUsed = 0; int leastN = int.MaxValue;
                for (int p = 0; p < palette.Length; p++) if (usage[p] < leastN) { leastN = usage[p]; leastUsed = p; }
                palette[leastUsed] = new Color(worstColor.r, worstColor.g, worstColor.b, 1f);
            }
        }

        // Algisal agirlikli renk uzakligi (goz yesile duyarli): quantize hatalari daha az goze batar
        static float ColorDist(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return 0.30f * dr * dr + 0.59f * dg * dg + 0.11f * db * db;
        }

        // K-means rafine: median-cut paletini hucre orneklerine gore 'iter' tur oturtur (bos kume eski merkezini korur)
        static void KMeansRefine(System.Collections.Generic.List<Color> samples, Color[] pal, int iters)
        {
            if (samples == null || samples.Count == 0 || pal == null || pal.Length == 0) return;
            int k = pal.Length;
            var sumR = new float[k]; var sumG = new float[k]; var sumB = new float[k]; var cnt = new int[k];
            for (int it = 0; it < iters; it++)
            {
                for (int i = 0; i < k; i++) { sumR[i] = sumG[i] = sumB[i] = 0f; cnt[i] = 0; }
                foreach (var s in samples)
                {
                    int best = 0; float bd = float.MaxValue;
                    for (int p = 0; p < k; p++) { float d = ColorDist(s, pal[p]); if (d < bd) { bd = d; best = p; } }
                    sumR[best] += s.r; sumG[best] += s.g; sumB[best] += s.b; cnt[best]++;
                }
                for (int i = 0; i < k; i++)
                    if (cnt[i] > 0) pal[i] = new Color(sumR[i] / cnt[i], sumG[i] / cnt[i], sumB[i] / cnt[i], 1f);
            }
        }

        static Color[] MedianCut(System.Collections.Generic.List<Color> colors, int count)
        {
            if (colors == null || colors.Count == 0) return null;
            var boxes = new System.Collections.Generic.List<System.Collections.Generic.List<Color>>();
            boxes.Add(new System.Collections.Generic.List<Color>(colors));
            while (boxes.Count < count)
            {
                int bi = -1, bch = 0; float bestRange = -1f;
                for (int i = 0; i < boxes.Count; i++)
                {
                    var bx = boxes[i]; if (bx.Count < 2) continue;
                    float mnr = 1, mxr = 0, mng = 1, mxg = 0, mnb = 1, mxb = 0;
                    foreach (var c in bx) { if (c.r < mnr) mnr = c.r; if (c.r > mxr) mxr = c.r; if (c.g < mng) mng = c.g; if (c.g > mxg) mxg = c.g; if (c.b < mnb) mnb = c.b; if (c.b > mxb) mxb = c.b; }
                    float rr = mxr - mnr, rg = mxg - mng, rb = mxb - mnb;
                    float range = Mathf.Max(rr, Mathf.Max(rg, rb));
                    if (range > bestRange) { bestRange = range; bi = i; bch = (rr >= rg && rr >= rb) ? 0 : (rg >= rb ? 1 : 2); }
                }
                if (bi < 0) break;
                var box = boxes[bi];
                box.Sort((a, b) => (bch == 0 ? a.r : bch == 1 ? a.g : a.b).CompareTo(bch == 0 ? b.r : bch == 1 ? b.g : b.b));
                int mid = box.Count / 2;
                var b1 = box.GetRange(0, mid);
                var b2 = box.GetRange(mid, box.Count - mid);
                boxes.RemoveAt(bi); boxes.Add(b1); boxes.Add(b2);
            }
            var pal = new Color[boxes.Count];
            for (int i = 0; i < boxes.Count; i++)
            {
                float r = 0, g = 0, b = 0; foreach (var c in boxes[i]) { r += c.r; g += c.g; b += c.b; }
                int n = Mathf.Max(1, boxes[i].Count); pal[i] = new Color(r / n, g / n, b / n, 1f);
            }
            return pal;
        }

        // ============================================================
        // HD IMPORT AYAR PENCERESI: yogunluk + renk sayisi + canlandirma + arka plan
        // Menu: Color Cargo Loop / Portrait Importer HD (Ayarli)...
        // ============================================================
        sealed class PortraitImporterHdWindow : EditorWindow
        {
            [MenuItem("Color Cargo Loop/Portrait Importer HD (Ayarli)...")]
            static void Open()
            {
                var w = GetWindow<PortraitImporterHdWindow>(true, "Portre HD Import", true);
                w.minSize = new Vector2(380, 260);
            }

            void OnGUI()
            {
                EditorGUILayout.HelpBox("AI gorsellerini (PNG/JPG) su klasore koy:\n" + PortraitFolder +
                    "\nImport her calistiginda PortraitSet.asset bastan yazilir.", MessageType.Info);

                HdTargetHeight = EditorGUILayout.IntSlider(new GUIContent("Yukseklik (hucre)", "Portre kac satir olsun. 48 = ~2300 kup (onerilen), 64 = ~4100 kup (AGIR, mobilde test et)"), HdTargetHeight, 24, 64);
                int estimate = Mathf.RoundToInt(HdTargetHeight * HdTargetHeight * 1.0f);
                EditorGUILayout.LabelField(" ", "~" + estimate + " kup / kare resim", EditorStyles.miniLabel);
                HdColorCount = EditorGUILayout.IntSlider(new GUIContent("Renk sayisi", "Resimden kac baskin renk cikarilsin (= oyundaki panda renk grubu sayisi). Max 12."), HdColorCount, 6, 12);
                HdVivid = EditorGUILayout.Slider(new GUIContent("Canlandirma", "0 = resmin renklerine dokunma, 1 = doygunluk/kontrast agresif artir"), HdVivid, 0f, 1f);
                HdRemoveBackground = EditorGUILayout.Toggle(new GUIContent("Arka plani temizle", "Saydam olmayan duz zemini silmeye calisir (kenar flood-fill). Tam-sahne AI resimlerinde KAPALI birak!"), HdRemoveBackground);

                EditorGUILayout.Space(8);
                if (GUILayout.Button("IMPORT (HD Adaptive)", GUILayout.Height(34)))
                {
                    ImportAllAdaptive();
                }
                EditorGUILayout.HelpBox("Sonuc Console'a yazilir. PortraitSet.asset zaten ArrowsPixelGame'e ataliysa ekstra bir sey yapmana gerek yok.", MessageType.None);
            }
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
            // TAM-OPAK resim (saydam '.' yok): zaten dolu -> zemin/yildiz EKLEME (gereksiz gurultu olmasin)
            bool anyEmpty = false;
            foreach (var rr in rows) if (rr != null && rr.IndexOf('.') >= 0) { anyEmpty = true; break; }
            if (!anyEmpty) return rows;
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

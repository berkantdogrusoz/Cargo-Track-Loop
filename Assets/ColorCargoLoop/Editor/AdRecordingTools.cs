using UnityEditor;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Reklam videosu cekim araclari:
    /// - PlayerPrefs Sifirla: fresh-install durumu (level/coin/boost/tutorial/ses ayarlari HEPSI silinir).
    /// - Potre Listesini Logla: PortraitSet'teki isimleri index sirasiyla konsola yazar
    ///   (ArrowsPixelGame > Reklam Modu > adPortraitNames alanina yazilacak isimler).
    /// </summary>
    public static class AdRecordingTools
    {
        // Level 11-30 arasindaki potre sirasini rastgele karistirir (Fisher-Yates).
        // Ilk 10 (1..10 Misir potreleri) ve 30 sonrasi DOKUNULMAZ. Her tiklama yeni bir karisim.
        [MenuItem("Color Cargo Loop/Level 11-30 Potrelerini Karistir (rastgele)")]
        static void ShuffleLevels10to30()
        {
            var set = AssetDatabase.LoadAssetAtPath<ArrowsPixelPortraitSet>("Assets/Art/Portraits/PortraitSet.asset");
            if (set == null || !set.HasPortraits) { Debug.LogWarning("[Karistir] PortraitSet yok/bos."); return; }
            int from = 10;                                         // level 11 = index 10 (ilk 10 sabit)
            int to = Mathf.Min(29, set.portraits.Count - 1);       // level 30 = index 29
            if (to <= from) { Debug.LogWarning("[Karistir] 11-30 araliginda yeterli potre yok (toplam " + set.portraits.Count + ")."); return; }

            for (int i = to; i > from; i--)
            {
                int j = Random.Range(from, i + 1);
                var tmp = set.portraits[i]; set.portraits[i] = set.portraits[j]; set.portraits[j] = tmp;
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            var sb = new System.Text.StringBuilder("[Karistir] Yeni sira (level 10-30): ");
            for (int i = from; i <= to; i++) sb.Append((i + 1) + ":" + (set.portraits[i] != null ? set.portraits[i].name : "?") + "  ");
            Debug.Log(sb.ToString());
        }

        // ============================================================
        // OGRETICI TESTI: tum ogretme anlarini LEVEL 1'de pespese test etmek icin.
        // KUR: PlayerPrefs sifirlanir (tutorial+tipler gelir), esikler level 1'e cekilir,
        //      donma sansi yukseltilir, reklam modu KAPATILIR (ogreticiler reklam modunda cikmaz).
        //      Onceki degerler EditorPrefs'e saklanir. GERI AL: aynen geri yukler.
        // ============================================================
        [MenuItem("Color Cargo Loop/Test/Ogretici Testini KUR (level 1)")]
        static void SetupTutorialTest()
        {
            var game = Object.FindObjectOfType<ArrowsPixelGame>();
            if (game == null) { Debug.LogError("[Test] Sahnede ArrowsPixelGame yok."); return; }
            var so = new SerializedObject(game);

            // onceki degerleri sakla
            EditorPrefs.SetInt("ccl_test_adMode", so.FindProperty("adRecordingMode").boolValue ? 1 : 0);
            EditorPrefs.SetInt("ccl_test_fpTip", so.FindProperty("firePandaTipLevel").intValue);
            EditorPrefs.SetInt("ccl_test_slotTip", so.FindProperty("extraSlotTipLevel").intValue);
            EditorPrefs.SetInt("ccl_test_frLvl", so.FindProperty("freezeFromLevel").intValue);
            EditorPrefs.SetFloat("ccl_test_frCh", so.FindProperty("freezeChance").floatValue);
            EditorPrefs.SetInt("ccl_test_saved", 1);

            // test degerleri
            so.FindProperty("adRecordingMode").boolValue = false; // ogreticiler gorunsun
            so.FindProperty("firePandaTipLevel").intValue = 1;    // dev kup tanitimi level 1
            so.FindProperty("extraSlotTipLevel").intValue = 1;    // dev kupun hemen ardindan level 1
            so.FindProperty("freezeFromLevel").intValue = 1;      // donma level 1'den itibaren
            so.FindProperty("freezeChance").floatValue = 0.5f;    // sik donsun (test)
            so.ApplyModifiedProperties();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Debug.Log("[Test] OGRETICI TESTI HAZIR: Play'e bas. Sira: hikaye tutoriali -> el ilk pandayi gosterir -> dev kup balonu -> panda gonder (yuksek sans donma) -> cekic ogreticisi. Ardindan ekstra slot balonu. Bitince 'Ogretici Testini GERI AL'.");
        }

        [MenuItem("Color Cargo Loop/Test/Ogretici Testini GERI AL")]
        static void RestoreTutorialTest()
        {
            if (EditorPrefs.GetInt("ccl_test_saved", 0) != 1) { Debug.LogWarning("[Test] Saklanmis deger yok (once KUR calistirilmali)."); return; }
            var game = Object.FindObjectOfType<ArrowsPixelGame>();
            if (game == null) { Debug.LogError("[Test] Sahnede ArrowsPixelGame yok."); return; }
            var so = new SerializedObject(game);
            so.FindProperty("adRecordingMode").boolValue = EditorPrefs.GetInt("ccl_test_adMode", 0) == 1;
            so.FindProperty("firePandaTipLevel").intValue = EditorPrefs.GetInt("ccl_test_fpTip", 1);
            so.FindProperty("extraSlotTipLevel").intValue = EditorPrefs.GetInt("ccl_test_slotTip", 1);
            so.FindProperty("freezeFromLevel").intValue = EditorPrefs.GetInt("ccl_test_frLvl", 4);
            so.FindProperty("freezeChance").floatValue = EditorPrefs.GetFloat("ccl_test_frCh", 0.05f);
            so.ApplyModifiedProperties();
            EditorPrefs.SetInt("ccl_test_saved", 0);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Test] Degerler GERI YUKLENDI ve sahne kaydedildi (adRecordingMode=" + (EditorPrefs.GetInt("ccl_test_adMode", 0) == 1) + ", tipler=" + EditorPrefs.GetInt("ccl_test_fpTip", 1) + "/" + EditorPrefs.GetInt("ccl_test_slotTip", 1) + ", donma=" + EditorPrefs.GetInt("ccl_test_frLvl", 4) + ").");
        }

        [MenuItem("Color Cargo Loop/Reklam/PlayerPrefs Sifirla (fresh install)")]
        static void ResetPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog("PlayerPrefs Sifirla",
                "TUM PlayerPrefs silinecek: level, coin, boost, tutorial, ses/titresim ayarlari.\nReklam cekimi icin temiz kurulum durumu.\n\nEmin misin?",
                "Evet, sil", "Vazgec")) return;
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[ReklamModu] PlayerPrefs SIFIRLANDI (fresh install). Play'e basinca Level 1 + tutorial + baslangic coin.");
        }

        // ============================================================================
        // KESKIN POTRE CEVIRIMI (zengin renk): ilk 9 level potresini kaynak PNG'lerden
        // yeniden cevirir. Kup derz cizgilerini otokorelasyonla bulur (dogru izgara),
        // hucre MERKEZINDEN ornekler (bulasma yok), ince ton ayrimiyla 12 renge kadar
        // kumeler (resmin golge/damali dokusu korunur). Sira ve isimler DEGISMEZ.
        // EDIT modunda calistir; sonra Play. FullScene dolgusu YOK (arka plan bos kalir).
        // ============================================================================
        const float RichGreedyDist = 0.085f;  // ilk kumeleme esigi (kucuk = daha cok ton ayrimi)
        const float RichMergeDist = 0.105f;   // son birlestirme esigi (benzer tonlar yine tek grup)

        [MenuItem("Color Cargo Loop/Reklam/Potrelerin Tumunu Sadik Renkle Yeniden Cevir")]
        public static void ReconvertPortraitsRich()
        {
            const string setPath = "Assets/Art/Portraits/PortraitSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<ArrowsPixelPortraitSet>(setPath);
            if (set == null || !set.HasPortraits)
            {
                Debug.LogError("[SadikCevirim] PortraitSet yok veya bos: " + setPath);
                return;
            }

            var entries = new System.Collections.Generic.List<ArrowsPixelPortraitSet.Entry>();
            foreach (var entry in set.portraits)
                if (entry != null) entries.Add(entry);

            int processed = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== SADIK RENK CEVIRIMI === mevcut PortraitSet, 56x56, max 12 renk");

            foreach (var entry in entries)
            {
                string path = entry.sourceTexture != null ? AssetDatabase.GetAssetPath(entry.sourceTexture) : null;
                if (string.IsNullOrEmpty(path))
                {
                    report.AppendLine(entry.name + ": kaynak texture referansi yok, atlandi");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    report.AppendLine(entry.name + ": texture importer bulunamadi (" + path + ")");
                    continue;
                }

                Color[] palette;
                string[] rows = PortraitImporter.ConvertTextureAdaptive(path, 56, 12, false, 0f, out palette);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null || rows == null || rows.Length == 0)
                {
                    report.AppendLine(entry.name + ": HD adaptive cevirim basarisiz (" + path + ")");
                    continue;
                }

                entry.rows = rows;
                entry.palette = palette;
                entry.sourceTexture = texture;
                int cubes = 0;
                foreach (string row in rows)
                    foreach (char cell in row)
                        if (cell != '.') cubes++;
                entry.preview = string.Join("\n", rows) + "\n[HD sadik cevirim: " + palette.Length +
                    " renk, " + cubes + " kup, arka plan bos]";
                processed++;
                report.AppendLine(entry.name + ": " + rows[0].Length + "x" + rows.Length + ", " +
                    palette.Length + " renk, " + cubes + " kup");
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            // Reklam modu ilk 10 PortraitSet kaydini kullanir; level sirasi degismez.
            var game = Object.FindObjectOfType<ArrowsPixelGame>();
            if (game != null)
            {
                var serializedGame = new SerializedObject(game);
                var namesProperty = serializedGame.FindProperty("adPortraitNames");
                if (namesProperty != null && namesProperty.isArray)
                {
                    int adCount = Mathf.Min(10, entries.Count);
                    namesProperty.arraySize = adCount;
                    for (int i = 0; i < adCount; i++)
                        namesProperty.GetArrayElementAtIndex(i).stringValue = entries[i].name;
                    serializedGame.ApplyModifiedProperties();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                }
            }

            if (processed != entries.Count)
                Debug.LogError("[SadikCevirim] Eksik donusum: " + processed + "/" + entries.Count + "\n" + report);
            else
                Debug.Log("[SadikCevirim] Tum portreler tamam: " + processed + "/" + entries.Count + "\n" + report);
        }
        // Kup derz tespiti + merkez ornekleme + ince kumeleme. true = basarili.
        static bool ConvertSharp(Texture2D tex, char[] slots, out string[] outRows, out Color[] outPal, out string info)
        {
            outRows = null; outPal = null; info = "";
            int sw = tex.width, sh = tex.height;
            Color[] src = tex.GetPixels();

            int minX = sw, maxX = -1, minY = sh, maxY = -1;
            for (int y = 0; y < sh; y++) for (int x = 0; x < sw; x++)
                if (src[y * sw + x].a > 0.5f) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            if (maxX < 0) { info = "opak piksel yok"; return false; }
            float bw = maxX - minX + 1, bh = maxY - minY + 1;

            // 1) derz periyodu: yatay gradyan profili + otokorelasyon (en kucuk guclu yerel tepe)
            int W0 = (int)bw;
            double[] g = new double[W0];
            for (int px = minX; px < maxX; px++)
            {
                double s = 0;
                for (int py = minY; py <= maxY; py += 3)
                {
                    Color a = src[py * sw + px], b = src[py * sw + px + 1];
                    if (a.a < 0.5f || b.a < 0.5f) continue;
                    s += System.Math.Abs((a.r + a.g + a.b) - (b.r + b.g + b.b));
                }
                g[px - minX] = s;
            }
            double mean = 0; for (int i = 0; i < W0; i++) mean += g[i]; mean /= W0;
            for (int i = 0; i < W0; i++) g[i] -= mean;
            double maxCorr = double.MinValue;
            double[] corrByP = new double[121];
            for (int P = 25; P <= 120 && P < W0 / 3; P++)
            {
                double c = 0; int n = 0;
                for (int x = 0; x + P < W0; x++) { c += g[x] * g[x + P]; n++; }
                corrByP[P] = c / n;
                if (corrByP[P] > maxCorr) maxCorr = corrByP[P];
            }
            int pitchPx = -1;
            for (int P = 26; P <= 119 && P < W0 / 3; P++)
                if (corrByP[P] >= maxCorr * 0.88 && corrByP[P] >= corrByP[P - 1] && corrByP[P] >= corrByP[P + 1]) { pitchPx = P; break; }
            if (pitchPx < 0) { info = "derz periyodu bulunamadi"; return false; }
            int colsC = Mathf.Max(6, Mathf.RoundToInt(bw / pitchPx));

            // 2) +-1 kolon: yeniden-kurma hatasiyla ince ayar
            int colsF = colsC; float bestErr = float.MaxValue;
            for (int cols = colsC - 1; cols <= colsC + 1; cols++)
            {
                if (cols < 6) continue;
                float pitch = bw / cols;
                int rowsA = Mathf.RoundToInt(bh / pitch);
                if (rowsA < 5 || rowsA > 45) continue;
                float pitchY = bh / rowsA;
                Color[,] cc = new Color[rowsA, cols];
                bool[,] cs = new bool[rowsA, cols];
                for (int ry = 0; ry < rowsA; ry++)
                    for (int rx = 0; rx < cols; rx++)
                    {
                        int ix = Mathf.Clamp(Mathf.RoundToInt(minX + (rx + 0.5f) * pitch), 0, sw - 1);
                        int iy = Mathf.Clamp(Mathf.RoundToInt(minY + bh - (ry + 0.5f) * pitchY), 0, sh - 1);
                        Color c = src[iy * sw + ix];
                        if (c.a >= 0.5f) { cc[ry, rx] = c; cs[ry, rx] = true; }
                    }
                double err = 0; int cnt = 0;
                for (int py = minY; py <= maxY; py += 4)
                    for (int px = minX; px <= maxX; px += 4)
                    {
                        Color p = src[py * sw + px];
                        int rx2 = Mathf.Clamp((int)((px - minX) / pitch), 0, cols - 1);
                        int ry2 = Mathf.Clamp((int)((minY + bh - 1 - py) / pitchY), 0, rowsA - 1);
                        bool ps = p.a >= 0.5f;
                        if (ps != cs[ry2, rx2]) { err += 0.06; cnt++; continue; }
                        if (!ps) continue;
                        float dr = p.r - cc[ry2, rx2].r, dg = p.g - cc[ry2, rx2].g, db = p.b - cc[ry2, rx2].b;
                        err += dr * dr + dg * dg + db * db; cnt++;
                    }
                if (cnt == 0) continue;
                float e = (float)(err / cnt);
                if (e < bestErr) { bestErr = e; colsF = cols; }
            }
            float pitchF = bw / colsF;
            int rowsF = Mathf.RoundToInt(bh / pitchF);
            float pitchYF = bh / rowsF;

            // 3) merkez ornekleme + INCE kumeleme (12'ye kadar; tasan en yakina katilir)
            var clusters = new System.Collections.Generic.List<Color>();
            var counts = new System.Collections.Generic.List<int>();
            int[,] cellSlot = new int[rowsF, colsF];
            float greedy2 = RichGreedyDist * RichGreedyDist;
            for (int ry = 0; ry < rowsF; ry++)
                for (int rx = 0; rx < colsF; rx++)
                {
                    cellSlot[ry, rx] = -1;
                    int ix = Mathf.Clamp(Mathf.RoundToInt(minX + (rx + 0.5f) * pitchF), 0, sw - 1);
                    int iy = Mathf.Clamp(Mathf.RoundToInt(minY + bh - (ry + 0.5f) * pitchYF), 0, sh - 1);
                    // MEDYAN ORNEKLEME: tek merkez piksel yerine hucre ic bolgesinin luminans-medyani ->
                    // boncuk speküler parlamasi (parlak nokta) ve aradaki koyu derz OUTLIER olarak atlanir,
                    // gercek boncuk rengi gelir (siyah govdedeki altin serpisti + gri bulaniklik biter).
                    bool solid;
                    Color c = SampleBeadMedian(src, sw, sh, ix, iy, pitchF, pitchYF, out solid);
                    if (!solid) continue;
                    c.a = 1f; // KIRLI ALPHA GIRMESIN (toon/transparan yolunu bozuyordu)
                    int best = -1; float bestD = greedy2;
                    for (int k = 0; k < clusters.Count; k++)
                    {
                        float dr = c.r - clusters[k].r, dg = c.g - clusters[k].g, db = c.b - clusters[k].b;
                        float d = dr * dr + dg * dg + db * db;
                        if (d < bestD) { bestD = d; best = k; }
                    }
                    if (best < 0)
                    {
                        if (clusters.Count >= 12)
                        {
                            float nd = float.MaxValue;
                            for (int k = 0; k < clusters.Count; k++)
                            {
                                float dr = c.r - clusters[k].r, dg = c.g - clusters[k].g, db = c.b - clusters[k].b;
                                float d = dr * dr + dg * dg + db * db;
                                if (d < nd) { nd = d; best = k; }
                            }
                        }
                        else { clusters.Add(c); counts.Add(0); best = clusters.Count - 1; }
                    }
                    else
                    {
                        Color merged0 = Color.Lerp(clusters[best], c, 1f / (counts[best] + 1));
                        merged0.a = 1f;
                        clusters[best] = merged0;
                    }
                    counts[best]++;
                    cellSlot[ry, rx] = best;
                }

            // 4) son birlestirme
            int[] remap = new int[clusters.Count];
            var merged = new System.Collections.Generic.List<Color>();
            float merge2 = RichMergeDist * RichMergeDist;
            for (int i = 0; i < clusters.Count; i++)
            {
                int hit = -1;
                for (int j = 0; j < merged.Count; j++)
                {
                    float dr = clusters[i].r - merged[j].r, dg = clusters[i].g - merged[j].g, db = clusters[i].b - merged[j].b;
                    if (dr * dr + dg * dg + db * db < merge2) { hit = j; break; }
                }
                if (hit < 0) { var mc = clusters[i]; mc.a = 1f; merged.Add(mc); remap[i] = merged.Count - 1; }
                else remap[i] = hit;
            }

            // 5) olcek: hedef ~52 satir, genislik <= 56, yukseklik <= 64
            int scale = Mathf.Clamp(Mathf.RoundToInt(52f / rowsF), 1, 4);
            while (scale > 1 && colsF * scale > 56) scale--;
            while (scale > 1 && rowsF * scale > 64) scale--;
            int H = rowsF * scale, W = colsF * scale, cubes = 0;
            outRows = new string[H];
            for (int y = 0; y < H; y++)
            {
                var sb2 = new System.Text.StringBuilder(W);
                for (int x = 0; x < W; x++)
                {
                    int s = cellSlot[y / scale, x / scale];
                    if (s < 0) sb2.Append('.');
                    else { sb2.Append(slots[remap[s]]); cubes++; }
                }
                outRows[y] = sb2.ToString();
            }
            outPal = merged.ToArray();
            info = colsF + "x" + rowsF + " art (derz " + pitchPx + "px) -> x" + scale + " = " + W + "x" + H + ", " + merged.Count + " renk, " + cubes + " kup";
            return true;
        }

        // Bir boncuk hucresinin RENGINI robust olarak orneklor: merkez cevresi ~%30 patch, opak
        // pikselleri luminansa gore siralar, MEDYAN pikselin rengini dondur. Boylece parlak speküler
        // nokta (en ust) ve koyu derz (en alt) outlier kalir, boncuk govde rengi secilir.
        static Color SampleBeadMedian(Color[] src, int sw, int sh, int cx, int cy, float pitchX, float pitchY, out bool solid)
        {
            int rx = Mathf.Max(1, Mathf.RoundToInt(pitchX * 0.30f));
            int ry = Mathf.Max(1, Mathf.RoundToInt(pitchY * 0.30f));
            var cols = new System.Collections.Generic.List<Color>();
            var lum = new System.Collections.Generic.List<float>();
            int total = 0, opaque = 0;
            for (int yy = cy - ry; yy <= cy + ry; yy++)
                for (int xx = cx - rx; xx <= cx + rx; xx++)
                {
                    if (xx < 0 || xx >= sw || yy < 0 || yy >= sh) continue;
                    total++;
                    Color c = src[yy * sw + xx];
                    if (c.a < 0.5f) continue;
                    opaque++;
                    cols.Add(c);
                    lum.Add(0.299f * c.r + 0.587f * c.g + 0.114f * c.b);
                }
            solid = cols.Count > 0 && opaque >= Mathf.Max(1, total / 3); // hucrenin >=1/3'u opak -> dolu (kenar anti-alias'i eleme)
            if (cols.Count == 0) return Color.clear;
            var idx = new int[cols.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            System.Array.Sort(idx, (a, b) => lum[a].CompareTo(lum[b]));
            return cols[idx[idx.Length / 2]]; // luminans-medyan pixelin rengi
        }

        // Onizleme seridi: 9 potreyi yanyana PNG'ye cizer (gorsel kontrol icin)
        static void RenderPreviewStrip(ArrowsPixelPortraitSet set, string[] names, char[] slots)
        {
            var list = new System.Collections.Generic.List<ArrowsPixelPortraitSet.Entry>();
            foreach (string nm in names) { var e = set.portraits.Find(p => p != null && p.name == nm); if (e != null && e.rows != null) list.Add(e); }
            if (list.Count == 0) return;
            int px = 5, gap = 10, maxH = 0, totW = gap;
            foreach (var e in list)
            {
                int h = e.rows.Length, w = 0;
                foreach (var r in e.rows) if (r != null && r.Length > w) w = r.Length;
                if (h > maxH) maxH = h;
                totW += w * px + gap;
            }
            var tex = new Texture2D(totW, maxH * px + 20, TextureFormat.RGBA32, false);
            Color bgc = new Color(0.13f, 0.12f, 0.16f, 1f);
            for (int i = 0; i < tex.width * tex.height; i++) tex.SetPixel(i % tex.width, i / tex.width, bgc);
            int ox = gap;
            foreach (var e in list)
            {
                int h = e.rows.Length, w = 0;
                foreach (var r in e.rows) if (r != null && r.Length > w) w = r.Length;
                int oy = 10 + (maxH - h) * px / 2;
                for (int y = 0; y < h; y++)
                {
                    string row = e.rows[y] ?? "";
                    for (int x = 0; x < row.Length; x++)
                    {
                        char c = row[x];
                        if (c == '.') continue;
                        int sl = System.Array.IndexOf(slots, c);
                        Color col = (e.palette != null && sl >= 0 && sl < e.palette.Length) ? e.palette[sl] : Color.magenta;
                        col.a = 1f;
                        for (int dy = 0; dy < px; dy++) for (int dx = 0; dx < px; dx++)
                            tex.SetPixel(ox + x * px + dx, oy + (h - 1 - y) * px + dy, col);
                    }
                }
                ox += w * px + gap;
            }
            tex.Apply();
            try
            {
                string dir = @"C:\Users\berka\AppData\Local\Temp\claude\C--UnityProjects-COLOR-CARGO-LOOP-CLEAN\25a8ef58-5a89-4710-87d8-f52331c92bf1\scratchpad";
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "ilk10_serit_zengin.png"), tex.EncodeToPNG());
                Debug.Log("[ZenginCevirim] Onizleme seridi yazildi: ilk10_serit_zengin.png");
            }
            catch (System.Exception ex) { Debug.LogWarning("[ZenginCevirim] Serit yazilamadi: " + ex.Message); }
            Object.DestroyImmediate(tex);
        }

        // PLAY MODUNDA calistir: sahnedeki TUM renderer'lari tarar, pembe/magenta gorunen ve
        // hatali shader'li olanlari detayli raporlar (Console + dosya). Renk hatasi avi icin.
        [MenuItem("Color Cargo Loop/Reklam/Magenta Avcisi (Play modunda calistir)")]
        static void HuntMagenta()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== MAGENTA AVCISI === " + System.DateTime.Now.ToString("HH:mm:ss") + " | playMode=" + Application.isPlaying);

            // 1) Adaptive palet durumu
            var ov = CargoColorPalette.Override;
            sb.AppendLine("CargoColorPalette.Override: " + (ov == null ? "NULL (candy sabit palet aktif!)" : ov.Length + " renk"));
            if (ov != null)
                for (int i = 0; i < ov.Length; i++)
                    sb.AppendLine("  Override[" + i + "] = #" + ColorUtility.ToHtmlStringRGBA(ov[i]));

            // 2) Oyun durumu (reflection ile ozel alanlar)
            var game = Object.FindObjectOfType<ArrowsPixelGame>();
            if (game != null)
            {
                var t = typeof(ArrowsPixelGame);
                var f = t.GetField("currentLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) sb.AppendLine("currentLevel = " + f.GetValue(game));
                f = t.GetField("adRecordingMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) sb.AppendLine("adRecordingMode = " + f.GetValue(game));
                f = t.GetField("activeLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                object lvl = f != null ? f.GetValue(game) : null;
                if (lvl != null)
                {
                    var pf = lvl.GetType().GetField("palette");
                    var pal = pf != null ? pf.GetValue(lvl) as Color[] : null;
                    sb.AppendLine("activeLevel.palette: " + (pal == null ? "NULL" : pal.Length + " renk"));
                }
            }
            else sb.AppendLine("ArrowsPixelGame bulunamadi!");

            // 3) TUM renderer taramasi: materyal envanteri + supheli pembe/magenta
            int total = 0;
            var census = new System.Collections.Generic.Dictionary<string, int>();
            var suspects = new System.Collections.Generic.List<string>();
            foreach (var rend in Object.FindObjectsOfType<Renderer>(false))
            {
                if (rend is TrailRenderer || rend is ParticleSystemRenderer) continue;
                total++;
                var m = rend.sharedMaterial;
                string shName = (m == null) ? "MAT_NULL" : (m.shader != null ? m.shader.name : "SHADER_NULL");
                Color col = Color.black; string colSrc = "-";
                if (m != null)
                {
                    if (m.HasProperty("_Color")) { col = m.GetColor("_Color"); colSrc = "_Color"; }
                    else if (m.HasProperty("_BaseColor")) { col = m.GetColor("_BaseColor"); colSrc = "_BaseColor"; }
                }
                string key = shName + " | " + colSrc + "=#" + ColorUtility.ToHtmlStringRGB(col);
                if (!census.ContainsKey(key)) census[key] = 0;
                census[key]++;

                bool errShader = m == null || m.shader == null || shName.Contains("InternalError");
                // pembe/magenta yakala: R yuksek, B orta-yuksek, G dusuk-orta (candy pink, hot pink, saf magenta)
                bool pinkish = col.r > 0.80f && col.b > 0.50f && col.g < 0.68f && (col.r - col.g) > 0.25f;
                if (errShader || pinkish)
                {
                    string path = rend.gameObject.name;
                    Transform p = rend.transform.parent;
                    int depth = 0;
                    while (p != null && depth++ < 4) { path = p.name + "/" + path; p = p.parent; }
                    string extra = "";
                    if (m != null)
                    {
                        if (m.HasProperty("_BaseColor")) extra += " _BaseColor=#" + ColorUtility.ToHtmlStringRGBA(m.GetColor("_BaseColor"));
                        if (m.HasProperty("_Color")) extra += " _Color=#" + ColorUtility.ToHtmlStringRGBA(m.GetColor("_Color"));
                        extra += " q=" + m.renderQueue + " mat=" + m.name;
                    }
                    suspects.Add((errShader ? "[HATALI SHADER] " : "[PEMBE] ") + path + " | shader=" + shName + extra);
                }
            }
            sb.AppendLine("Toplam renderer: " + total);
            sb.AppendLine("--- SUPHELILER (" + suspects.Count + ") ---");
            foreach (var s in suspects) sb.AppendLine("  " + s);
            sb.AppendLine("--- MATERYAL ENVANTERI ---");
            foreach (var kv in census) sb.AppendLine("  " + kv.Value + "x  " + kv.Key);

            string report = sb.ToString();
            Debug.Log(report);
            try
            {
                string dir = @"C:\Users\berka\AppData\Local\Temp\claude\C--UnityProjects-COLOR-CARGO-LOOP-CLEAN\25a8ef58-5a89-4710-87d8-f52331c92bf1\scratchpad";
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "magenta_rapor.txt"), report);
                Debug.Log("[MagentaAvcisi] Rapor dosyaya da yazildi.");
            }
            catch (System.Exception ex) { Debug.LogWarning("[MagentaAvcisi] Dosyaya yazilamadi: " + ex.Message); }
        }

        [MenuItem("Color Cargo Loop/Reklam/Potre Listesini Logla")]
        static void LogPortraitList()
        {
            string[] guids = AssetDatabase.FindAssets("t:ArrowsPixelPortraitSet");
            if (guids.Length == 0) { Debug.LogWarning("[ReklamModu] Projede PortraitSet asset'i bulunamadi."); return; }
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ArrowsPixelPortraitSet set = AssetDatabase.LoadAssetAtPath<ArrowsPixelPortraitSet>(path);
                if (set == null || !set.HasPortraits) continue;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("[ReklamModu] PortraitSet: " + path + " (" + set.portraits.Count + " potre). adPortraitNames'e ISIM yazilir:");
                for (int i = 0; i < set.portraits.Count; i++)
                    sb.AppendLine("  " + i + ": " + (set.portraits[i] != null ? set.portraits[i].name : "(bos)"));
                Debug.Log(sb.ToString());
            }
        }
    }
}

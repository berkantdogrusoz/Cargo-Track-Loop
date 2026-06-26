using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    public enum ArrowsPixelExitDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [System.Serializable]
    public sealed class ArrowsPixelExitGate
    {
        public int x = 1;
        public int z = 2;
        public ArrowsPixelExitDirection direction = ArrowsPixelExitDirection.Up;
    }

    [System.Serializable]
    public sealed class ArrowsPixelTruckSpawn
    {
        public int x;
        public int z;
        public float yaw;
        public CargoColor color;
        public int capacity = 1;
    }

    [System.Serializable]
    public sealed class ArrowsPixelLevelDefinition
    {
        public string levelName = "Level";
        [Min(1)] public int moveLimit = 30;

        [Min(2)] public int gridWidth = 3;
        [Min(2)] public int gridHeight = 3;
        [Min(0.2f)] public float gridStepX = 1.64f;
        [Min(0.2f)] public float gridStepZ = 1.20f;
        [Min(0.2f)] public float truckModelScale = 0.68f;
        [Min(1f)] public float cameraOrthographicSize = 6.5f;

        [Min(0.01f)] public float portraitStepX = 0.078f;
        [Min(0.01f)] public float portraitStepZ = 0.078f;
        public Vector3 portraitTileScale = new Vector3(0.074f, 0.13f, 0.074f);

        public string[] portraitRows;
        [Tooltip("Opsiyonel hucre maskesi, USTTEN alta satirlar: '#'=hucre var, '.'=hucre yok. Bos birakirsan tam dikdortgen. T/L grid formlari icin.")]
        public string[] cellMask;
        public ArrowsPixelTruckSpawn[] trucks;
        public ArrowsPixelExitGate[] exits;
    }

    public static class ArrowsPixelLevelLibrary
    {
        // Importer'in urettigi potreler (PortraitSet -> ArrowsPixelGame baslangicta atar).
        // Doluysa geometrik potreler yerine BUNLAR kullanilir; truck/kapasite otomatik uyarlanir.
        public static string[][] PortraitOverride;

        // ============================================================
        // ZORLUK SABLONLARI (el-tasarimi, KANITLANMIS cozulebilir dizilimler)
        // Konum/ok/kapi sabit (puzzle yapisi); renkler runtime'da potreye gore atanir.
        // Hareket renkten bagimsiz -> bir dizilim cozulebilirse her renk atamasiyla cozulebilir.
        // ============================================================
        sealed class LevelTemplate
        {
            public int gridW, gridH;
            public float cameraSize, truckScale;
            public ArrowsPixelExitGate[] exits;
            public int[] sx, sz;     // sepet konumlari
            public float[] syaw;     // sepet ok yonleri
            public int moveLimit;
        }

        static ArrowsPixelExitGate Gate(int x, int z, ArrowsPixelExitDirection d)
        {
            return new ArrowsPixelExitGate { x = x, z = z, direction = d };
        }

        // Zorluk ARTAN sirada (T0 en kolay/seyrek -> T4 en zor/yogun). Hepsi >=6 slot -> her potre (<=6 renk) oturur.
        static readonly LevelTemplate[] Templates = new[]
        {
            // T0 KOLAY: 4x3 genis/seyrek, 2 ust kapi (6 slot)
            new LevelTemplate {
                gridW=4, gridH=3, cameraSize=6.8f, truckScale=0.62f,
                exits = new[]{ Gate(1,2,ArrowsPixelExitDirection.Up), Gate(2,2,ArrowsPixelExitDirection.Up) },
                sx = new[]{1,2,1,2,0,3}, sz = new[]{2,2,1,1,0,1}, syaw = new[]{180f,180f,0f,0f,90f,180f},
                moveLimit = 40
            },
            // T1: 4x3 yan kapi (7 slot)
            new LevelTemplate {
                gridW=4, gridH=3, cameraSize=6.8f, truckScale=0.62f,
                exits = new[]{ Gate(3,1,ArrowsPixelExitDirection.Right) },
                sx = new[]{3,2,1,0,0,2,3}, sz = new[]{1,1,1,1,0,0,2}, syaw = new[]{90f,90f,90f,90f,0f,180f,270f},
                moveLimit = 38
            },
            // T2 ORTA: 3x3 ust kapi (6 slot)
            new LevelTemplate {
                gridW=3, gridH=3, cameraSize=6.5f, truckScale=0.68f,
                exits = new[]{ Gate(1,2,ArrowsPixelExitDirection.Up) },
                sx = new[]{1,1,1,0,2,2}, sz = new[]{2,1,0,1,0,1}, syaw = new[]{180f,0f,0f,90f,270f,180f},
                moveLimit = 34
            },
            // T3 ZOR: 3x3 yogun (7 slot)
            new LevelTemplate {
                gridW=3, gridH=3, cameraSize=6.5f, truckScale=0.68f,
                exits = new[]{ Gate(1,2,ArrowsPixelExitDirection.Up) },
                sx = new[]{1,1,0,1,2,0,2}, sz = new[]{2,1,1,0,0,0,1}, syaw = new[]{180f,0f,90f,0f,270f,90f,180f},
                moveLimit = 32
            },
            // T4 COK ZOR: 3x3 cok yogun, 8 sepet/1 bos hucre (en zor manevra)
            new LevelTemplate {
                gridW=3, gridH=3, cameraSize=6.5f, truckScale=0.68f,
                exits = new[]{ Gate(1,2,ArrowsPixelExitDirection.Up) },
                sx = new[]{0,1,2,0,1,2,0,2}, sz = new[]{2,2,2,1,1,1,0,0}, syaw = new[]{0f,180f,270f,90f,0f,180f,270f,90f},
                moveLimit = 30
            },
        };

        // Level -> sablon (zorluk egrisi): ilk birkac level kolay, sonra kademeli zorlasir, 9+ en zorda kalir.
        static int TierForLevel(int level)
        {
            int l = Mathf.Max(1, level);
            if (l <= 2) return 0;   // 1-2 kolay
            if (l <= 4) return 1;   // 3-4
            if (l <= 6) return 2;   // 5-6 orta
            if (l <= 8) return 3;   // 7-8 zor
            return 4;               // 9+ cok zor
        }

        const int NewBoardPreviewLevels = 10;
        const int LegacyBoardShapeCount = 5;
        const int BoardShapeVariantCount = 15;

        static int ForcedPreviewShapeForLevel(int level)
        {
            int index = Mathf.Max(1, level) - 1;
            return index < NewBoardPreviewLevels ? LegacyBoardShapeCount + index : -1;
        }

        static void PreviewBoardSizeForShape(int shape, out int gw, out int gh)
        {
            gw = shape < 9 ? 4 : 5;
            gh = 4;
        }

        // PROCEDURAL level: board boyutu / sepet sayisi / dizilim / kapilar her level (seed) DEGISIR.
        // Cozulebilirlik: hareket 1-hucre kaydirma (15-puzzle); >=1 bos hucre birakilir -> her dizilim cozulebilir.
        public static ArrowsPixelLevelDefinition CreateGeneratedLevel(int oneBasedLevel)
        {
            int level = Mathf.Max(1, oneBasedLevel);
            System.Random rng = new System.Random(level * 1013904223 + 7);
            string[] portrait = PortraitForLevel(level);
            int[] pcounts = CountPortraitColors(portrait);
            int present = 0; for (int i = 0; i < 6; i++) if (pcounts[i] > 0) present++;
            if (present == 0) present = 1;

            // BOARD: ilk 10 level yeni board formlarini test ettirir; eski buyume/random akis 11. levelden sonra baslar.
            int forcedShape = ForcedPreviewShapeForLevel(level);
            int gw, gh;
            if (forcedShape >= 0)
            {
                PreviewBoardSizeForShape(forcedShape, out gw, out gh);
            }
            else
            {
                int designLevel = Mathf.Max(1, level - NewBoardPreviewLevels);
                int prog = Mathf.Min(designLevel - 1, 24);
                int grow = prog / 8;                                       // 0..3
                gw = Mathf.Clamp(3 + grow + rng.Next(0, 2), 3, 5);          // genislik
                gh = Mathf.Clamp(3 + (grow >> 1) + rng.Next(0, 2), 3, 4);   // derinlik (slotlara tasmasin diye sinirli)
            }
            while (gw * gh < present + 3) { if (gw < 5) gw++; else if (gh < 4) gh++; else break; }
            // YARATICI SEKIL: eski 5 forma ek olarak 10 yeni form. null = klasik dikdortgen. Bagli + yeterli hucre garanti.
            bool[,] alive = GenerateShape(gw, gh, present + 4, rng, forcedShape);
            string[] cellMask = alive != null ? ShapeToMask(alive, gw, gh) : null;

            // VAR OLAN hucreler (sekle gore)
            System.Collections.Generic.List<int> existing = new System.Collections.Generic.List<int>();
            for (int z = 0; z < gh; z++) for (int x = 0; x < gw; x++) if (alive == null || alive[x, z]) existing.Add(x + z * gw);
            int cells = existing.Count;

            // KAPILAR: var olan KENAR hucrelerde (disa acik yon), 1..3
            int gateCount = Mathf.Clamp(1 + (level / 6) + rng.Next(0, 2), 1, 3);
            ArrowsPixelExitGate[] gates = GenerateGates(alive, gw, gh, gateCount, rng);

            // ZORLUK: board DOLU ama manevra-edilebilir (bos %32 -> %16). Bos hucreler engel sepetle dolar.
            float looseness = Mathf.Lerp(0.32f, 0.16f, Mathf.Clamp01((level - 1) / 20f));
            int emptyCells = Mathf.Max(1, Mathf.RoundToInt(cells * looseness));
            int basketCount = Mathf.Clamp(cells - emptyCells, Mathf.Min(present + 1, cells - 1), cells - 1);

            // KONUM: VAR OLAN hucreleri karistir, basketCount al; yaw rastgele
            for (int i = existing.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int tmp = existing[i]; existing[i] = existing[j]; existing[j] = tmp; }
            float[] yawOpts = { 0f, 90f, 180f, 270f };
            ArrowsPixelTruckSpawn[] trucks = new ArrowsPixelTruckSpawn[basketCount];
            for (int k = 0; k < basketCount; k++)
            {
                int c = existing[k];
                trucks[k] = new ArrowsPixelTruckSpawn { x = c % gw, z = c / gw, yaw = yawOpts[rng.Next(4)], color = CargoColor.Red, capacity = 1 };
            }

            float camSize = 6.0f + (gh - 3) * 0.70f + (gw - 3) * 0.16f;
            float tScale = Mathf.Clamp(0.70f - (Mathf.Max(gw, gh) - 3) * 0.06f, 0.46f, 0.70f);
            // HAMLE: makul. Sepet basina ~4.5 (kolay/affedici) -> ~2.8 (zor/kisik, verimli oyna). Board gevsek oldugu icin yeterli.
            int moveLimit = Mathf.Max(12, Mathf.RoundToInt(basketCount * Mathf.Lerp(4.5f, 2.8f, Mathf.Clamp01((level - 1) / 20f))));

            ArrowsPixelLevelDefinition def = new ArrowsPixelLevelDefinition
            {
                levelName = "Level " + level,
                moveLimit = moveLimit,
                gridWidth = gw,
                gridHeight = gh,
                gridStepX = 1.00f,
                gridStepZ = 1.00f,
                truckModelScale = tScale,
                cameraOrthographicSize = camSize,
                portraitStepX = 0.078f,
                portraitStepZ = 0.078f,
                portraitTileScale = new Vector3(0.074f, 0.13f, 0.074f),
                portraitRows = portrait,
                exits = gates,
                trucks = trucks,
                cellMask = cellMask,
            };

            AssignColorsByGateDistance(def, gates, level);   // BOLME her level + matching kapidan UZAK, engel kapida
            ApplyTruckCapacitiesFromPortrait(def);           // ayni-renk sepetlere kapasite otomatik bolunur
            return def;
        }

        // Kapilar: VAR OLAN (alive) hucrelerin disa-acik kenarlarinda (sekil ne olursa olsun gecerli)
        static ArrowsPixelExitGate[] GenerateGates(bool[,] alive, int gw, int gh, int count, System.Random rng)
        {
            int[] dxA = { 0, 0, -1, 1 }, dzA = { 1, -1, 0, 0 };
            ArrowsPixelExitDirection[] dirs = { ArrowsPixelExitDirection.Up, ArrowsPixelExitDirection.Down, ArrowsPixelExitDirection.Left, ArrowsPixelExitDirection.Right };
            var cands = new System.Collections.Generic.List<ArrowsPixelExitGate>();
            for (int x = 0; x < gw; x++)
                for (int z = 0; z < gh; z++)
                {
                    if (alive != null && !alive[x, z]) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dxA[d], nz = z + dzA[d];
                        bool neighborAlive = nx >= 0 && nx < gw && nz >= 0 && nz < gh && (alive == null || alive[nx, nz]);
                        if (!neighborAlive) cands.Add(new ArrowsPixelExitGate { x = x, z = z, direction = dirs[d] });
                    }
                }
            for (int i = cands.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = cands[i]; cands[i] = cands[j]; cands[j] = t; }
            var result = new System.Collections.Generic.List<ArrowsPixelExitGate>();
            var usedCells = new System.Collections.Generic.HashSet<int>();
            foreach (var g in cands)
            {
                if (result.Count >= count) break;
                int key = g.x + g.z * gw;
                if (usedCells.Contains(key)) continue;
                usedCells.Add(key);
                result.Add(g);
            }
            if (result.Count == 0) result.Add(new ArrowsPixelExitGate { x = gw / 2, z = gh - 1, direction = ArrowsPixelExitDirection.Up });
            return result.ToArray();
        }

        // YARATICI BOARD SEKLI: bazi hucreleri cikar. null = dikdortgen. Bagli (tek parca) + >= minCells garanti.
        static bool[,] GenerateShape(int gw, int gh, int minCells, System.Random rng, int forcedShape = -1)
        {
            if (forcedShape < 0 && rng.Next(100) < 25) return null; // klasik dikdortgen de kalsin ama daha az gelsin
            bool[,] a = new bool[gw, gh];
            for (int x = 0; x < gw; x++) for (int z = 0; z < gh; z++) a[x, z] = true;
            int shape = forcedShape >= 0 ? forcedShape : rng.Next(BoardShapeVariantCount);

            if (shape == 0) // koseleri kes (oktagon/elmas)
            {
                ClearShapeCell(a, gw, gh, 0, 0); ClearShapeCell(a, gw, gh, gw - 1, 0); ClearShapeCell(a, gw, gh, 0, gh - 1); ClearShapeCell(a, gw, gh, gw - 1, gh - 1);
            }
            else if (shape == 1) // tek centik (L / notch)
            {
                int qw = Mathf.Max(1, gw / 2), qh = Mathf.Max(1, gh / 2);
                int x0 = rng.Next(2) == 0 ? 0 : gw - qw;
                int z0 = rng.Next(2) == 0 ? 0 : gh - qh;
                for (int x = x0; x < x0 + qw; x++) for (int z = z0; z < z0 + qh; z++) ClearShapeCell(a, gw, gh, x, z);
            }
            else if (shape == 2) // arti / cross (4 kose blok kes)
            {
                int bw = Mathf.Max(1, (gw - 1) / 2), bh = Mathf.Max(1, (gh - 1) / 2);
                for (int x = 0; x < bw; x++) for (int z = 0; z < bh; z++)
                { ClearShapeCell(a, gw, gh, x, z); ClearShapeCell(a, gw, gh, gw - 1 - x, z); ClearShapeCell(a, gw, gh, x, gh - 1 - z); ClearShapeCell(a, gw, gh, gw - 1 - x, gh - 1 - z); }
            }
            else if (shape == 3) // T / kademe: ust satirin kenarlarini kes
            {
                int trim = Mathf.Max(1, gw / 3);
                for (int x = 0; x < trim; x++) { ClearShapeCell(a, gw, gh, x, gh - 1); ClearShapeCell(a, gw, gh, gw - 1 - x, gh - 1); }
            }
            else if (shape == 4) // U: alt-orta blok kes
            {
                int uw = Mathf.Max(1, gw / 3), uh = Mathf.Max(1, gh / 2);
                int x0 = (gw - uw) / 2;
                for (int x = x0; x < x0 + uw; x++) for (int z = 0; z < uh; z++) ClearShapeCell(a, gw, gh, x, z);
            }
            else if (shape == 5) // yeni: sol merdiven
            {
                for (int z = 0; z < gh; z++) for (int x = 0; x < Mathf.Clamp(gh - 1 - z, 0, 2); x++) ClearShapeCell(a, gw, gh, x, z);
            }
            else if (shape == 6) // yeni: sag merdiven
            {
                for (int z = 0; z < gh; z++) for (int x = 0; x < Mathf.Clamp(gh - 1 - z, 0, 2); x++) ClearShapeCell(a, gw, gh, gw - 1 - x, z);
            }
            else if (shape == 7) // yeni: ust orta agiz
            {
                int bw = Mathf.Max(1, gw / 3), x0 = (gw - bw) / 2;
                for (int x = x0; x < x0 + bw; x++) ClearShapeCell(a, gw, gh, x, gh - 1);
                ClearShapeCell(a, gw, gh, x0, gh - 2);
            }
            else if (shape == 8) // yeni: alt orta agiz
            {
                int bw = Mathf.Max(1, gw / 3), x0 = (gw - bw) / 2;
                for (int x = x0; x < x0 + bw; x++) ClearShapeCell(a, gw, gh, x, 0);
                ClearShapeCell(a, gw, gh, x0, 1);
            }
            else if (shape == 9) // yeni: sol yan cep
            {
                int h = Mathf.Max(1, gh / 2), z0 = (gh - h) / 2;
                for (int z = z0; z < z0 + h; z++) ClearShapeCell(a, gw, gh, 0, z);
                ClearShapeCell(a, gw, gh, 1, z0);
            }
            else if (shape == 10) // yeni: sag yan cep
            {
                int h = Mathf.Max(1, gh / 2), z0 = (gh - h) / 2;
                for (int z = z0; z < z0 + h; z++) ClearShapeCell(a, gw, gh, gw - 1, z);
                ClearShapeCell(a, gw, gh, gw - 2, z0);
            }
            else if (shape == 11) // yeni: capraz kose kiriklari
            {
                ClearShapeCell(a, gw, gh, 0, 0); ClearShapeCell(a, gw, gh, 0, 1);
                ClearShapeCell(a, gw, gh, gw - 1, gh - 1); ClearShapeCell(a, gw, gh, gw - 2, gh - 1);
            }
            else if (shape == 12) // yeni: ters capraz kose kiriklari
            {
                ClearShapeCell(a, gw, gh, gw - 1, 0); ClearShapeCell(a, gw, gh, gw - 1, 1);
                ClearShapeCell(a, gw, gh, 0, gh - 1); ClearShapeCell(a, gw, gh, 1, gh - 1);
            }
            else if (shape == 13) // yeni: S akisi
            {
                int mid = Mathf.Max(1, gw / 2);
                for (int x = 0; x < mid; x++) ClearShapeCell(a, gw, gh, x, 0);
                for (int x = mid + 1; x < gw; x++) ClearShapeCell(a, gw, gh, x, gh - 1);
            }
            else // yeni: iki yandan kirpilmis arena
            {
                ClearShapeCell(a, gw, gh, 0, 0); ClearShapeCell(a, gw, gh, gw - 1, 0);
                ClearShapeCell(a, gw, gh, 0, gh - 1); ClearShapeCell(a, gw, gh, gw - 1, gh - 1);
                ClearShapeCell(a, gw, gh, gw / 2, gh - 1);
            }

            if (ConnectedAndEnough(a, gw, gh, minCells)) return a;
            if (forcedShape < 0) return null;

            // Zorunlu test leveli dikdortgene dusmesin diye hafif ama farkli bir fallback.
            for (int x = 0; x < gw; x++) for (int z = 0; z < gh; z++) a[x, z] = true;
            ClearShapeCell(a, gw, gh, 0, 0);
            ClearShapeCell(a, gw, gh, gw - 1, gh - 1);
            return ConnectedAndEnough(a, gw, gh, minCells) ? a : null;
        }

        static void ClearShapeCell(bool[,] a, int gw, int gh, int x, int z)
        {
            if (x >= 0 && x < gw && z >= 0 && z < gh) a[x, z] = false;
        }
        // 4-yonlu tek parca mi + >= minCells hucre mi?
        static bool ConnectedAndEnough(bool[,] a, int gw, int gh, int minCells)
        {
            int total = 0, sx = -1, sz = -1;
            for (int x = 0; x < gw; x++) for (int z = 0; z < gh; z++) if (a[x, z]) { total++; if (sx < 0) { sx = x; sz = z; } }
            if (total < minCells || sx < 0) return false;
            bool[,] seen = new bool[gw, gh];
            var stack = new System.Collections.Generic.Stack<int>();
            stack.Push(sx + sz * gw); seen[sx, sz] = true; int reached = 0;
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                int c = stack.Pop(); reached++;
                int cx = c % gw, cz = c / gw;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d], nz = cz + dz[d];
                    if (nx >= 0 && nx < gw && nz >= 0 && nz < gh && a[nx, nz] && !seen[nx, nz]) { seen[nx, nz] = true; stack.Push(nx + nz * gw); }
                }
            }
            return reached == total;
        }

        static string[] ShapeToMask(bool[,] a, int gw, int gh)
        {
            string[] mask = new string[gh];
            for (int gz = 0; gz < gh; gz++)
            {
                var sb = new System.Text.StringBuilder();
                for (int x = 0; x < gw; x++) sb.Append(a[x, gz] ? '#' : '.');
                mask[gh - 1 - gz] = sb.ToString(); // mask[0] = en ust (gz=gh-1)
            }
            return mask;
        }

        // Renk ata: eslesen sepetler kapidan UZAK (kolay cikis yok), engeller kapiya YAKIN. Bolme HER level (orantili).
        static void AssignColorsByGateDistance(ArrowsPixelLevelDefinition def, ArrowsPixelExitGate[] gates, int level)
        {
            int[] counts = CountPortraitColors(def.portraitRows);
            System.Collections.Generic.List<int> order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 6; i++) order.Add(i);
            order.Sort((a, b) => counts[b].CompareTo(counts[a]));
            System.Collections.Generic.List<CargoColor> present = new System.Collections.Generic.List<CargoColor>();
            System.Collections.Generic.List<int> presentCounts = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<CargoColor> absent = new System.Collections.Generic.List<CargoColor>();
            foreach (int i in order)
            {
                if (counts[i] > 0) { present.Add(IndexToCargo(i)); presentCounts.Add(counts[i]); }
                else absent.Add(IndexToCargo(i));
            }
            if (present.Count == 0) { present.Add(CargoColor.Red); presentCounts.Add(1); }

            ArrowsPixelTruckSpawn[] trucks = def.trucks;
            int n = trucks.Length;
            int[] dist = new int[n];
            for (int k = 0; k < n; k++)
            {
                int best = int.MaxValue;
                foreach (var g in gates) { int dd = Mathf.Abs(trucks[k].x - g.x) + Mathf.Abs(trucks[k].z - g.z); if (dd < best) best = dd; }
                dist[k] = best == int.MaxValue ? 0 : best;
            }
            int[] sortIdx = new int[n]; for (int k = 0; k < n; k++) sortIdx[k] = k;
            System.Array.Sort(sortIdx, (a, b) => dist[a].CompareTo(dist[b]));   // kapiya YAKIN once

            // Engel sepet sayisi level ile artar (board'u DOLDURUR, %10 -> %40); en az kapi kadar (kapilari tika), matching >= present kalir.
            float curve = Mathf.Clamp01((level - 1) / 20f);
            int obstacleCount = absent.Count > 0
                ? Mathf.Clamp(Mathf.Max(gates.Length, Mathf.RoundToInt(n * Mathf.Lerp(0.10f, 0.40f, curve))), 0, n - present.Count)
                : 0;
            int matchCount = n - obstacleCount;
            int[] bpc = DistributeBaskets(presentCounts, matchCount);            // her present >=1 + orantili bolme
            System.Collections.Generic.List<CargoColor> matchColors = new System.Collections.Generic.List<CargoColor>();
            for (int i = 0; i < present.Count; i++) for (int j = 0; j < bpc[i]; j++) matchColors.Add(present[i]);
            while (matchColors.Count < matchCount) matchColors.Add(present[matchColors.Count % present.Count]);

            for (int r = 0; r < n; r++)
            {
                int k = sortIdx[r];
                if (r < obstacleCount) trucks[k].color = absent[r % absent.Count];      // kapiya yakin = ENGEL
                else trucks[k].color = matchColors[r - obstacleCount];                  // uzak = ESLESEN
            }
        }

        // Birkac levelden sonra: bir rengin tum kuplerini TEK sepet degil, COKLU ayni-renk sepet alir (kapasite bolunur).
        const int SplitStartLevel = 6;   // bu level'den itibaren coklu-sepet bolme devrede

        // Sablon slotlarina renk ata: ESLESEN sepetler (potre renkleri) + KALANLAR engel (potrede olmayan renk).
        // level >= SplitStartLevel: matching slotlar renge ORANTILI dagilir (agir renk = daha cok ayni-renk sepet).
        static void AssignColorsFromPortrait(ArrowsPixelLevelDefinition def, LevelTemplate t, int level)
        {
            int[] counts = CountPortraitColors(def.portraitRows);
            System.Collections.Generic.List<int> order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 6; i++) order.Add(i);
            order.Sort((a, b) => counts[b].CompareTo(counts[a]));   // cok kuplu renk once

            System.Collections.Generic.List<CargoColor> present = new System.Collections.Generic.List<CargoColor>();
            System.Collections.Generic.List<int> presentCounts = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<CargoColor> absent = new System.Collections.Generic.List<CargoColor>();
            foreach (int i in order)
            {
                if (counts[i] > 0) { present.Add(IndexToCargo(i)); presentCounts.Add(counts[i]); }
                else absent.Add(IndexToCargo(i));
            }
            if (present.Count == 0) { present.Add(CargoColor.Red); presentCounts.Add(1); }

            int n = t.sx.Length;

            // Her slota gidecek renk listesi (uzunluk n)
            System.Collections.Generic.List<CargoColor> slotColors = new System.Collections.Generic.List<CargoColor>(n);
            if (level < SplitStartLevel || present.Count >= n)
            {
                // ERKEN: her eslesen renk 1 sepet, kalan engel (eski davranis)
                for (int k = 0; k < n; k++)
                {
                    if (k < present.Count) slotColors.Add(present[k]);
                    else { int oi = k - present.Count; slotColors.Add(absent.Count > 0 ? absent[oi % absent.Count] : present[oi % present.Count]); }
                }
            }
            else
            {
                // ILERI: matching slotlari renge ORANTILI dagit (500'u 5 sepet gibi), birkac engel birak
                int obstacleSlots = absent.Count > 0 ? Mathf.Clamp(n / 5, 1, n - present.Count) : 0;
                int matchSlots = n - obstacleSlots;
                int[] bpc = DistributeBaskets(presentCounts, matchSlots);   // her present renge kac sepet
                for (int i = 0; i < present.Count; i++)
                    for (int j = 0; j < bpc[i]; j++) slotColors.Add(present[i]);
                for (int o = 0; o < obstacleSlots; o++) slotColors.Add(absent[o % absent.Count]);
                while (slotColors.Count < n) slotColors.Add(present[slotColors.Count % present.Count]); // guvenlik
            }

            // SLOTLARI level'e gore KARISTIR: ayni-renk sepetler dagilsin, kapida olmasin (zor + cesitli)
            int[] slotOrder = new int[n];
            for (int i = 0; i < n; i++) slotOrder[i] = i;
            System.Random rng = new System.Random(level * 7919 + 17);
            for (int i = n - 1; i > 0; i--) { int j = rng.Next(i + 1); int tmp = slotOrder[i]; slotOrder[i] = slotOrder[j]; slotOrder[j] = tmp; }

            ArrowsPixelTruckSpawn[] trucks = new ArrowsPixelTruckSpawn[n];
            for (int k = 0; k < n; k++)
            {
                int s = slotOrder[k];
                trucks[s] = new ArrowsPixelTruckSpawn { x = t.sx[s], z = t.sz[s], yaw = t.syaw[s], color = slotColors[k], capacity = 1 };
            }
            def.trucks = trucks;
        }

        // matchSlots'u present renklerine dagitir: her renk >=1, FAZLASI kup sayisina ORANTILI (largest remainder).
        static int[] DistributeBaskets(System.Collections.Generic.List<int> counts, int slots)
        {
            int p = counts.Count;
            int[] bpc = new int[p];
            for (int i = 0; i < p; i++) bpc[i] = 1;
            int extra = slots - p;
            if (extra <= 0) return bpc;
            long total = 0; for (int i = 0; i < p; i++) total += counts[i];
            if (total <= 0) { for (int i = 0; i < extra; i++) bpc[i % p]++; return bpc; }
            double[] frac = new double[p]; int used = 0;
            for (int i = 0; i < p; i++)
            {
                double ideal = (double)counts[i] / total * extra;
                int add = (int)System.Math.Floor(ideal);
                bpc[i] += add; frac[i] = ideal - add; used += add;
            }
            int rem = extra - used;
            System.Collections.Generic.List<int> idx = new System.Collections.Generic.List<int>();
            for (int i = 0; i < p; i++) idx.Add(i);
            idx.Sort((a, b) => frac[b].CompareTo(frac[a]));
            for (int r = 0; r < rem; r++) bpc[idx[r % p]]++;
            return bpc;
        }

        static ArrowsPixelLevelDefinition CreateEarlyPassableLevel(int level)
        {
            ArrowsPixelLevelDefinition def = CreateBaseLevel(level);
            def.moveLimit = 65 + level * 5;

            if (level == 1)
            {
                def.trucks = new[]
                {
                    Truck(1, 2, 180f, CargoColor.Red,    352),
                    Truck(1, 1,   0f, CargoColor.Yellow, 160),
                    Truck(1, 0,   0f, CargoColor.Blue,    36),
                    Truck(0, 0,  90f, CargoColor.Green,   40),
                    Truck(2, 0, 270f, CargoColor.Purple,  54),
                };
                return FinalizeLevel(def);
            }

            if (level == 2)
            {
                def.trucks = new[]
                {
                    Truck(1, 2, 180f, CargoColor.Red,    352),
                    Truck(1, 1,   0f, CargoColor.Yellow, 160),
                    Truck(1, 0,   0f, CargoColor.Blue,    36),
                    Truck(0, 1,  90f, CargoColor.Green,   40),
                    Truck(2, 0, 270f, CargoColor.Purple,  54),
                    Truck(2, 1, 180f, CargoColor.Orange,   1),
                };
                return FinalizeLevel(def);
            }

            if (level == 3)
            {
                def.trucks = new[]
                {
                    Truck(1, 2, 180f, CargoColor.Red,    352),
                    Truck(1, 1,   0f, CargoColor.Yellow, 160),
                    Truck(0, 1,  90f, CargoColor.Blue,    36),
                    Truck(1, 0,   0f, CargoColor.Green,   40),
                    Truck(2, 0, 270f, CargoColor.Purple,  54),
                    Truck(0, 0,  90f, CargoColor.Orange,   1),
                    Truck(2, 1, 180f, CargoColor.Orange,   1),
                };
                return FinalizeLevel(def);
            }

            if (level == 4)
            {
                def.gridWidth = 4;
                def.gridHeight = 3;
                def.truckModelScale = 0.62f;
                def.cameraOrthographicSize = 6.10f;
                def.exits = new[]
                {
                    new ArrowsPixelExitGate { x = 1, z = 2, direction = ArrowsPixelExitDirection.Up },
                    new ArrowsPixelExitGate { x = 2, z = 2, direction = ArrowsPixelExitDirection.Up }
                };
                def.trucks = new[]
                {
                    Truck(1, 2, 180f, CargoColor.Red,    352),
                    Truck(2, 2, 180f, CargoColor.Yellow, 160),
                    Truck(1, 1,   0f, CargoColor.Blue,    36),
                    Truck(2, 1,   0f, CargoColor.Green,   40),
                    Truck(0, 0,  90f, CargoColor.Purple,  54),
                    Truck(3, 1, 180f, CargoColor.Orange,   1),
                };
                return FinalizeLevel(def);
            }

            def.gridWidth = 4;
            def.gridHeight = 3;
            def.truckModelScale = 0.62f;
            def.cameraOrthographicSize = 6.15f;
            def.exits = new[]
            {
                new ArrowsPixelExitGate { x = 3, z = 1, direction = ArrowsPixelExitDirection.Right }
            };
            def.trucks = new[]
            {
                Truck(3, 1,  90f, CargoColor.Red,    352),
                Truck(2, 1,  90f, CargoColor.Yellow, 160),
                Truck(1, 1,  90f, CargoColor.Blue,    36),
                Truck(0, 1,  90f, CargoColor.Green,   40),
                Truck(0, 0,   0f, CargoColor.Purple,  54),
                Truck(2, 0, 180f, CargoColor.Orange,   1),
                Truck(3, 2, 270f, CargoColor.Orange,   1),
            };
            return FinalizeLevel(def);
        }

        static ArrowsPixelLevelDefinition CreateBaseLevel(int level)
        {
            return new ArrowsPixelLevelDefinition
            {
                levelName = "Level " + level,
                moveLimit = 30,
                gridWidth = 3,
                gridHeight = 3,
                gridStepX = 1.20f,
                gridStepZ = 1.20f,
                truckModelScale = 0.68f,
                cameraOrthographicSize = 6.5f,
                portraitStepX = 0.078f,
                portraitStepZ = 0.078f,
                portraitTileScale = new Vector3(0.074f, 0.13f, 0.074f),
                portraitRows = PortraitForLevel(level),
                exits = new[]
                {
                    new ArrowsPixelExitGate { x = 1, z = 2, direction = ArrowsPixelExitDirection.Up }
                },
                trucks = new[]
                {
                    new ArrowsPixelTruckSpawn { x = 0, z = 2, yaw =   0f, color = CargoColor.Blue,   capacity =  36 },
                    new ArrowsPixelTruckSpawn { x = 1, z = 2, yaw = 180f, color = CargoColor.Red,    capacity = 352 },
                    new ArrowsPixelTruckSpawn { x = 2, z = 2, yaw = 270f, color = CargoColor.Orange, capacity =   1 },
                    new ArrowsPixelTruckSpawn { x = 0, z = 1, yaw =  90f, color = CargoColor.Green,  capacity =  40 },
                    new ArrowsPixelTruckSpawn { x = 1, z = 1, yaw =   0f, color = CargoColor.Purple, capacity =  54 },
                    new ArrowsPixelTruckSpawn { x = 2, z = 1, yaw = 180f, color = CargoColor.Orange, capacity =   1 },
                    new ArrowsPixelTruckSpawn { x = 0, z = 0, yaw = 270f, color = CargoColor.Yellow, capacity = 160 },
                    new ArrowsPixelTruckSpawn { x = 2, z = 0, yaw =  90f, color = CargoColor.Orange, capacity =   1 },
                }
            };
        }

        static ArrowsPixelTruckSpawn Truck(int x, int z, float yaw, CargoColor color, int capacity)
        {
            return new ArrowsPixelTruckSpawn { x = x, z = z, yaw = yaw, color = color, capacity = capacity };
        }

        static void ApplyWideTopExit(ArrowsPixelLevelDefinition def)
        {
            def.gridWidth = 4;
            def.gridHeight = 3;
            def.moveLimit = 38;
            def.truckModelScale = 0.62f;
            def.cameraOrthographicSize = 6.10f;
            def.exits = new[]
            {
                new ArrowsPixelExitGate { x = 1, z = 2, direction = ArrowsPixelExitDirection.Up },
                new ArrowsPixelExitGate { x = 2, z = 2, direction = ArrowsPixelExitDirection.Up }
            };
        }

        static void ApplySideExit(ArrowsPixelLevelDefinition def)
        {
            def.gridWidth = 4;
            def.gridHeight = 3;
            def.moveLimit = 40;
            def.truckModelScale = 0.62f;
            def.cameraOrthographicSize = 6.15f;
            def.exits = new[]
            {
                new ArrowsPixelExitGate { x = 3, z = 1, direction = ArrowsPixelExitDirection.Right }
            };
        }

        static void ApplyTwoExit(ArrowsPixelLevelDefinition def)
        {
            def.gridWidth = 4;
            def.gridHeight = 4;
            def.moveLimit = 48;
            def.truckModelScale = 0.58f;
            def.cameraOrthographicSize = 6.45f;
            def.exits = new[]
            {
                new ArrowsPixelExitGate { x = 1, z = 3, direction = ArrowsPixelExitDirection.Up },
                new ArrowsPixelExitGate { x = 3, z = 1, direction = ArrowsPixelExitDirection.Right }
            };
        }

        static ArrowsPixelLevelDefinition FinalizeLevel(ArrowsPixelLevelDefinition def)
        {
            ApplyTruckCapacitiesFromPortrait(def);
            return def;
        }

        static void ApplyTruckCapacitiesFromPortrait(ArrowsPixelLevelDefinition def)
        {
            if (def == null || def.trucks == null || def.portraitRows == null) return;

            int[] counts = CountPortraitColors(def.portraitRows);
            for (int i = 0; i < counts.Length; i++)
            {
                int needed = counts[i];
                if (needed <= 0) continue;

                CargoColor color = IndexToCargo(i);
                int matchingTruckCount = 0;
                for (int t = 0; t < def.trucks.Length; t++)
                {
                    if (def.trucks[t] != null && def.trucks[t].color == color)
                    {
                        matchingTruckCount++;
                    }
                }

                if (matchingTruckCount <= 0) continue;

                int remaining = needed;
                int remainingTrucks = matchingTruckCount;
                for (int t = 0; t < def.trucks.Length; t++)
                {
                    ArrowsPixelTruckSpawn truck = def.trucks[t];
                    if (truck == null || truck.color != color) continue;

                    truck.capacity = Mathf.Max(1, Mathf.CeilToInt(remaining / (float)remainingTrucks));
                    remaining -= truck.capacity;
                    remainingTrucks--;
                }
            }
        }

        public static int[] CountPortraitColors(string[] rows) // ArrowsPixelGame.ValidateActiveLevel de kullanir
        {
            int[] counts = new int[6];
            if (rows == null) return counts;

            for (int y = 0; y < rows.Length; y++)
            {
                string row = rows[y];
                if (string.IsNullOrEmpty(row)) continue;

                for (int x = 0; x < row.Length; x++)
                {
                    int index = CharToCargoIndex(row[x]);
                    if (index >= 0) counts[index]++;
                }
            }

            return counts;
        }

        static int CharToCargoIndex(char ch)
        {
            switch (ch)
            {
                case 'P': return 0;
                case 'B': return 1;
                case 'Y': return 2;
                case 'G': return 3;
                case 'U': return 4;
                case 'O': return 5;
                default: return -1;
            }
        }

        public static CargoColor IndexToCargo(int index) // ArrowsPixelGame.ValidateActiveLevel de kullanir
        {
            switch (index)
            {
                case 1: return CargoColor.Blue;
                case 2: return CargoColor.Yellow;
                case 3: return CargoColor.Green;
                case 4: return CargoColor.Purple;
                case 5: return CargoColor.Orange;
                default: return CargoColor.Red;
            }
        }

        static string[] PortraitForLevel(int level)
        {
            // Gercek pixel-art PNG'leri (PortraitSet) atanmissa ONCELIKLI (pipeline)
            if (PortraitOverride != null && PortraitOverride.Length > 0)
            {
                string[] rows = PortraitOverride[(Mathf.Max(1, level) - 1) % PortraitOverride.Length];
                if (rows != null && rows.Length > 0) return rows;
            }
            // Yoksa: yayin icin 1000+ FARKLI tam-resim procedural uret
            return GenerateProceduralPortrait(level);
        }

        static char[][] PortraitCanvas()
        {
            char[][] rows = new char[24][];
            for (int y = 0; y < rows.Length; y++)
            {
                rows[y] = new char[48];
                for (int x = 0; x < rows[y].Length; x++)
                {
                    rows[y][x] = '.';
                }
            }

            return rows;
        }

        static string[] ToRows(char[][] canvas)
        {
            string[] rows = new string[canvas.Length];
            for (int y = 0; y < canvas.Length; y++)
            {
                rows[y] = new string(canvas[y]);
            }

            return rows;
        }

        static void DrawRect(char[][] canvas, int xMin, int yMin, int xMax, int yMax, char ch)
        {
            int height = canvas.Length;
            int width = canvas[0].Length;
            int sx = Mathf.Clamp(xMin, 0, width - 1);
            int ex = Mathf.Clamp(xMax, 0, width - 1);
            int sy = Mathf.Clamp(yMin, 0, height - 1);
            int ey = Mathf.Clamp(yMax, 0, height - 1);

            for (int y = sy; y <= ey; y++)
            {
                for (int x = sx; x <= ex; x++)
                {
                    canvas[y][x] = ch;
                }
            }
        }

        static void DrawEllipse(char[][] canvas, float cx, float cy, float rx, float ry, char ch)
        {
            if (rx <= 0f || ry <= 0f) return;

            for (int y = 0; y < canvas.Length; y++)
            {
                for (int x = 0; x < canvas[y].Length; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        canvas[y][x] = ch;
                    }
                }
            }
        }

        static void DrawDiamond(char[][] canvas, int cx, int cy, int rx, int ry, char ch)
        {
            if (rx <= 0 || ry <= 0) return;

            for (int y = 0; y < canvas.Length; y++)
            {
                for (int x = 0; x < canvas[y].Length; x++)
                {
                    int dx = Mathf.Abs(x - cx);
                    int dy = Mathf.Abs(y - cy);
                    if (dx * ry + dy * rx <= rx * ry)
                    {
                        canvas[y][x] = ch;
                    }
                }
            }
        }

        static void DrawLine(char[][] canvas, int x0, int y0, int x1, int y1, int width, char ch)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                DrawRect(canvas, x0 - width, y0 - width, x0 + width, y0 + width, ch);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        static string[] RocketPortrait()
        {
            char[][] g = PortraitCanvas();
            DrawEllipse(g, 23.5f, 11.5f, 21f, 10f, 'U');
            DrawEllipse(g, 24f, 10f, 7.5f, 9.5f, 'Y');
            DrawDiamond(g, 24, 3, 6, 4, 'P');
            DrawRect(g, 18, 7, 30, 16, 'Y');
            DrawEllipse(g, 21f, 9f, 2.4f, 2.1f, 'B');
            DrawEllipse(g, 27f, 9f, 2.4f, 2.1f, 'B');
            DrawLine(g, 17, 15, 9, 20, 1, 'P');
            DrawLine(g, 31, 15, 39, 20, 1, 'P');
            DrawRect(g, 20, 17, 22, 20, 'G');
            DrawRect(g, 26, 17, 28, 20, 'G');
            DrawEllipse(g, 24f, 21f, 7f, 2f, 'P');
            return ToRows(g);
        }

        static string[] CrownPortrait()
        {
            char[][] g = PortraitCanvas();
            DrawEllipse(g, 23.5f, 11.5f, 21f, 10f, 'P');
            DrawRect(g, 11, 11, 37, 18, 'Y');
            DrawDiamond(g, 14, 9, 6, 6, 'Y');
            DrawDiamond(g, 24, 7, 7, 8, 'Y');
            DrawDiamond(g, 34, 9, 6, 6, 'Y');
            DrawRect(g, 11, 17, 37, 20, 'Y');
            DrawEllipse(g, 14f, 9f, 2f, 2f, 'B');
            DrawEllipse(g, 24f, 7f, 2f, 2f, 'G');
            DrawEllipse(g, 34f, 9f, 2f, 2f, 'U');
            DrawRect(g, 16, 14, 19, 16, 'B');
            DrawRect(g, 22, 13, 25, 16, 'G');
            DrawRect(g, 29, 14, 32, 16, 'U');
            return ToRows(g);
        }

        static string[] FlowerPortrait()
        {
            char[][] g = PortraitCanvas();
            DrawEllipse(g, 23.5f, 11.5f, 21f, 10f, 'B');
            DrawEllipse(g, 16f, 9f, 8f, 5f, 'P');
            DrawEllipse(g, 31f, 9f, 8f, 5f, 'P');
            DrawEllipse(g, 19f, 14f, 8f, 5f, 'P');
            DrawEllipse(g, 28f, 14f, 8f, 5f, 'P');
            DrawEllipse(g, 24f, 11.5f, 6f, 4f, 'Y');
            DrawRect(g, 22, 16, 25, 22, 'G');
            DrawEllipse(g, 17f, 19f, 6f, 2.5f, 'G');
            DrawEllipse(g, 30f, 19f, 6f, 2.5f, 'G');
            DrawRect(g, 22, 11, 25, 13, 'U');
            return ToRows(g);
        }

        static string[] HousePortrait()
        {
            char[][] g = PortraitCanvas();
            DrawEllipse(g, 23.5f, 11.5f, 21f, 10f, 'G');
            DrawDiamond(g, 24, 9, 15, 8, 'P');
            DrawRect(g, 12, 10, 36, 19, 'Y');
            DrawRect(g, 16, 13, 20, 16, 'B');
            DrawRect(g, 28, 13, 32, 16, 'B');
            DrawRect(g, 22, 15, 26, 19, 'U');
            DrawRect(g, 12, 19, 36, 21, 'P');
            DrawRect(g, 9, 20, 39, 21, 'U');
            DrawRect(g, 22, 5, 26, 8, 'Y');
            return ToRows(g);
        }

        static string[] StarPortrait()
        {
            char[][] g = PortraitCanvas();
            DrawEllipse(g, 23.5f, 11.5f, 21f, 10f, 'U');
            DrawDiamond(g, 24, 11, 18, 9, 'Y');
            DrawDiamond(g, 24, 11, 12, 6, 'P');
            DrawDiamond(g, 24, 11, 7, 4, 'Y');
            DrawLine(g, 24, 2, 24, 21, 1, 'Y');
            DrawLine(g, 6, 11, 42, 11, 1, 'Y');
            DrawEllipse(g, 14f, 6f, 3f, 2f, 'B');
            DrawEllipse(g, 36f, 6f, 3f, 2f, 'G');
            DrawEllipse(g, 14f, 18f, 3f, 2f, 'G');
            DrawEllipse(g, 36f, 18f, 3f, 2f, 'B');
            return ToRows(g);
        }

        static string[] DefaultPortrait()
        {
            return new[]
            {
                "................................................",
                "................................................",
                "................................................",
                "..........PPPPPPPPPPPPPPPPPPPPPPPPPPPP..........",
                "..........PPPPPPPPPPPPPPPPPPPPPPPPPPPP..........",
                "..........PPPPPPPPPPPPPPPPPPPPPPPPPPPP..........",
                "....PPPPPPPPPPYYYYYYYYYYYYYYYYYYYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYYYYYYYYYYYYYYYYYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYBBBBBBYYYBBBBBBYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYBBBBBBYYYBBBBBBYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYBBBBBBYYYBBBBBBYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYYYYYYYYYYYYYYYYYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYYYYYYYYYYYYYYYYYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYYYYYYYYYYYYYYYYYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYGGGGYYYYYYYGGGGYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYGGGGYYYYYYYGGGGYYPPPPPPPPPP....",
                "....PPPPPPPPPPYYYGGGGYYYYYYYGGGGYYPPPPPPPPPP....",
                "........PPPPPPPPPGGGGPPPPPPPGGGGPPPPPPPP........",
                "........PPPPPPPPPGGGGPPPPPPPGGGGPPPPPPPP........",
                "...............UUUUUUUUUUUUUUUUUU...............",
                "...............UUUUUUUUUUUUUUUUUU...............",
                "...............UUUUUUUUUUUUUUUUUU...............",
                "................................................",
                "................................................",
            };
        }

        // ====== PROCEDURAL POTRE URETICI (yayin: 1000+ FARKLI tam-resim potre) ======
        // Her level seed'inden (subject + palet + serpme yildiz) deterministik bir TAM-RESIM uretir.
        // Gercek pixel-art PNG'leri PortraitSet'e atanirsa onlar oncelikli (pipeline) kalir.
        const int ProcW = 31, ProcH = 40;   // procedural tuval (tam dolu ~1240 kup, yuksek detay)
        const int SubjectCount = 12;

        // {bg, ana govde, detay, aksan} - hosa giden tam-resim renk uculeri
        static readonly char[][] ScenePalettes =
        {
            new[]{ 'U','Y','P','O' }, new[]{ 'B','P','Y','O' }, new[]{ 'G','P','Y','U' },
            new[]{ 'Y','U','P','O' }, new[]{ 'B','O','Y','P' }, new[]{ 'U','B','Y','O' },
            new[]{ 'O','U','Y','P' }, new[]{ 'P','Y','U','B' }, new[]{ 'G','U','Y','O' },
            new[]{ 'B','U','Y','O' },
        };

        static string[] GenerateProceduralPortrait(int level)
        {
            int lv = Mathf.Max(1, level);
            System.Random rng = new System.Random(lv * 92821 + 13);
            char[][] g = VCanvas(ProcW, ProcH);
            char[] pal = ScenePalettes[rng.Next(ScenePalettes.Length)];
            char bg = pal[0], body = pal[1], detail = pal[2], accent = pal[3];
            FillAll(g, bg);
            int subj = (lv - 1) % SubjectCount;             // her level farkli subject (palet de degisir -> farkli his)
            DrawSubject(g, subj, ProcW, ProcH, bg, body, detail, accent);
            ScatterPlus(g, accent, rng, 4 + rng.Next(4));   // kenarlara serpme yildizlar
            return ToRows(g);
        }

        static char[][] VCanvas(int w, int h)
        {
            char[][] g = new char[h][];
            for (int y = 0; y < h; y++) { g[y] = new char[w]; for (int x = 0; x < w; x++) g[y][x] = '.'; }
            return g;
        }

        static void FillAll(char[][] g, char c)
        {
            for (int y = 0; y < g.Length; y++) for (int x = 0; x < g[y].Length; x++) g[y][x] = c;
        }

        static void DrawTri(char[][] g, float ax, float ay, float bx, float by, float cxx, float cyy, char col)
        {
            float minX = Mathf.Min(ax, Mathf.Min(bx, cxx)), maxX = Mathf.Max(ax, Mathf.Max(bx, cxx));
            float minY = Mathf.Min(ay, Mathf.Min(by, cyy)), maxY = Mathf.Max(ay, Mathf.Max(by, cyy));
            float d = (by - cyy) * (ax - cxx) + (cxx - bx) * (ay - cyy);
            if (Mathf.Abs(d) < 1e-6f) return;
            int W = g[0].Length, H = g.Length;
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    float a = ((by - cyy) * (x - cxx) + (cxx - bx) * (y - cyy)) / d;
                    float b = ((cyy - ay) * (x - cxx) + (ax - cxx) * (y - cyy)) / d;
                    float cc = 1f - a - b;
                    if (a >= -0.02f && b >= -0.02f && cc >= -0.02f) g[y][x] = col;
                }
        }

        static void DrawPlus(char[][] g, int cx, int cy, int r, char col)
        {
            DrawRect(g, cx, cy - r, cx + 1, cy + r, col);
            DrawRect(g, cx - r, cy, cx + r, cy + 1, col);
        }

        static void ScatterPlus(char[][] g, char col, System.Random rng, int n)
        {
            int W = g[0].Length, H = g.Length;
            for (int i = 0; i < n; i++)
            {
                bool left = rng.Next(2) == 0;
                int x = left ? 1 + rng.Next(Mathf.Max(1, W / 4)) : W - 3 - rng.Next(Mathf.Max(1, W / 4));
                int y = 1 + rng.Next(Mathf.Max(1, H - 3));
                DrawPlus(g, x, y, 1, col);
            }
        }

        // 12 tam-resim subject; 36x46 referans koordinatlari W,H'ye olceklenir (PNG drawer'larimla ayni geometri)
        static void DrawSubject(char[][] g, int subj, int W, int H, char bg, char body, char detail, char accent)
        {
            float sx = W / 36f, sy = H / 46f; float cx = 17.5f;
            System.Action<float, float, float, char> C = (ox, oy, r, c) => DrawEllipse(g, ox * sx, oy * sy, r * sx, r * sy, c);
            System.Action<float, float, float, float, char> E = (ox, oy, rx, ry, c) => DrawEllipse(g, ox * sx, oy * sy, rx * sx, ry * sy, c);
            System.Action<float, float, float, float, float, float, char> T = (ax, ay, bx, by, ccx, ccy, c) => DrawTri(g, ax * sx, ay * sy, bx * sx, by * sy, ccx * sx, ccy * sy, c);
            System.Action<int, int, int, int, char> R = (x0, y0, x1, y1, c) => DrawRect(g, (int)(x0 * sx), (int)(y0 * sy), (int)(x1 * sx), (int)(y1 * sy), c);
            System.Action<float, float, int, int, char> D = (ox, oy, rx, ry, c) => DrawDiamond(g, (int)(ox * sx), (int)(oy * sy), (int)(rx * sx), (int)(ry * sy), c);
            switch (subj)
            {
                case 0: // KEDI
                    T(cx - 11, 16, cx - 3, 7, cx - 3, 17, body); T(cx + 11, 16, cx + 3, 7, cx + 3, 17, body);
                    C(cx, 25, 12, body);
                    T(cx - 8, 15, cx - 4, 10, cx - 4, 16, detail); T(cx + 8, 15, cx + 4, 10, cx + 4, 16, detail);
                    C(cx - 5, 23, 2.2f, accent); C(cx + 5, 23, 2.2f, accent);
                    T(cx - 2, 27, cx + 2, 27, cx, 30, detail);
                    break;
                case 1: // AY (hilal)
                    C(cx - 2, 23, 12, body); C(cx + 5, 20, 11, bg);
                    break;
                case 2: // KALP
                    C(cx - 6, 17, 7, body); C(cx + 6, 17, 7, body); T(cx - 12.5f, 19, cx + 12.5f, 19, cx, 37, body);
                    C(cx - 7, 14, 2.2f, detail);
                    break;
                case 3: // YILDIZ
                    D(cx, 23, 5, 18, body); D(cx, 23, 18, 5, body); D(cx, 23, 10, 10, body); C(cx, 23, 3.5f, detail);
                    break;
                case 4: // BALIK
                    E(cx - 1, 24, 13, 8, body); T(cx + 9, 24, cx + 17, 17, cx + 17, 31, body);
                    E(cx + 3, 24, 2, 7, detail); C(cx - 7, 22, 2, accent);
                    break;
                case 5: // HAYALET
                    C(cx, 17, 11, body); R(7, 17, 29, 33, body);
                    C(cx - 7, 33, 3.2f, bg); C(cx, 33, 3.2f, bg); C(cx + 7, 33, 3.2f, bg);
                    E(cx - 4, 16, 2, 3, detail); E(cx + 4, 16, 2, 3, detail);
                    break;
                case 6: // CICEK
                    C(cx, 11, 5, body); C(cx - 7, 16, 5, body); C(cx + 7, 16, 5, body); C(cx - 4, 22, 5, body); C(cx + 4, 22, 5, body);
                    C(cx, 16.5f, 4.5f, detail); R(17, 23, 19, 41, accent);
                    break;
                case 7: // MANTAR
                    E(cx, 18, 13, 8, body); R(5, 18, 31, 21, body); R(13, 21, 23, 37, detail);
                    C(cx - 5, 14, 2.5f, accent); C(cx + 4, 17, 2, accent);
                    break;
                case 8: // ROKET
                    E(cx, 20, 5, 12, body); T(cx - 5, 9, cx + 5, 9, cx, 1, detail);
                    T(cx - 5, 28, cx - 11, 34, cx - 5, 34, accent); T(cx + 5, 28, cx + 11, 34, cx + 5, 34, accent);
                    C(cx, 17, 3, detail); T(cx - 3, 33, cx + 3, 33, cx, 41, accent);
                    break;
                case 9: // BALON
                    C(cx, 16, 9, body); T(cx - 2, 24, cx + 2, 24, cx, 27, detail); R(17, 27, 18, 42, detail);
                    break;
                case 10: // HEDIYE
                    R(8, 22, 28, 40, body); R(6, 18, 30, 23, detail);
                    R(17, 18, 19, 40, accent); C(cx - 3, 16, 2.5f, accent); C(cx + 3, 16, 2.5f, accent);
                    break;
                default: // 11: ELMAS / GEM
                    D(cx, 23, 12, 15, body); R(6, 16, 30, 19, detail);
                    DrawLine(g, (int)((cx - 6) * sx), (int)(19 * sy), (int)(cx * sx), (int)(34 * sy), 0, accent);
                    DrawLine(g, (int)((cx + 6) * sx), (int)(19 * sy), (int)(cx * sx), (int)(34 * sy), 0, accent);
                    break;
            }
        }
    }

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
        [SerializeField] private bool useBlockStyle = true; // Color Block Jam tarzi studlu blok (tir yerine); kapatirsan tir gorseli geri gelir
        [SerializeField] private bool useBasketStyle = true; // toon SEPET gorseli (blok/tir yerine); en yuksek oncelik, kapatirsan bloga/tira doner
        [SerializeField] private Texture2D arrowTexture;   // Assets/Art/Ok.png (default saga = +X = kafa yonu)
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool buildOnStart = true;

        [Header("Sahne Atamalari (Editor menusu doldurur; hiyerarsiden tasiyabilirsin)")]
        [SerializeField] private Transform pictureArea;    // UST: kup-resim bu noktaya kurulur (pozisyon+rotasyon ondan)
        [SerializeField] private Transform[] slotPoints;   // ORTA: tirin park edecegi slot noktalari (satir merkezi/Z buradan alinir)
        [SerializeField] private int slotCount = 4;        // park slot SAYISI (slotPoints satiri ortalanir, bu kadar esit slot dizilir)
        [SerializeField] private Transform parkingArea;    // ALT: tir puzzle grid merkezi
        [SerializeField] private bool autoSetupCamera = true; // kapatirsan kamerayi sen ayarlarsin

        [Header("Gorsel Ayarlar (kolay erisim)")]
        [SerializeField, Range(0.4f, 1.5f)] private float basketHeightScale = 0.68f; // sepet duvar BOYU carpani (1 = eski boy; 0.68 = daha kompakt referans blok)
        [SerializeField, Range(0.70f, 1.0f)] private float basketFootprint = 0.76f;  // sepet TABAN genisligi (1 = hucreyi tam doldur/dip dibe; <1 = kucult + bosluk)
        [SerializeField, Range(0.10f, 0.48f)] private float basketCornerRound = 0.32f; // sepet kose YUVARLAGI (ovallik): 0.14=koseli, 0.32=referans gibi yumusak
        [SerializeField] private bool showBasketBaseBand = false; // sepet altindaki acik ten ALT BANT (Beads Out tabani). false = sepet TEK RENK/temiz
        [SerializeField, Range(0.10f, 0.9f)] private float dragLiftHeight = 0.42f;   // sepeti TUTUNCA zeminden kaldirma (eski ~0.27; buyuk = daha cok pop)
        [SerializeField] private float slotZOffset = -0.55f;                          // 3 slotu portre cercevesinden UZAKLASTIR (negatif = oyuncuya dogru)
        [SerializeField] private bool basketSpinOnDrag = true;                        // drag'de sepet kendi etrafinda doner
        [SerializeField, Range(0f, 2f)] private float basketSpinTurns = 0.5f;         // kac tur (0.5 = yarim tur/180, daha sakin; simetrik sepet ayni gorunur)
        [SerializeField] private float portraitCubeSize = 0.155f; // potre kup boyutu; referansa yakin daha iri/net pixel
        [SerializeField] private bool showPortraitFrame = false; // potre cercevesi/zemini. false = kupler direkt resmi olusturur (cerceve yok)

        // --- Potre Yolu (konveyor: v1'deki hareketli yol, potre etrafini sarar) ---
        [Header("Potre Yolu (konveyor)")]
        [SerializeField] private bool showPortraitRoad = true;
        [SerializeField] private float roadScale = 1.0f;         // v1 yol kesiti olcegi (1.0 = birebir v1)
        [SerializeField] private float roadMargin = 0.20f;       // potre kenarindan yola bosluk
        [SerializeField] private float roadFlowSpeed = 1.8f;     // chevron akis hizi (v1 = 1.8)
        [SerializeField] private float roadChevronSpacing = 0.55f; // chevron'lar arasi mesafe (cevreye gore adet)
        [SerializeField] private Color roadColor = new Color(0.36f, 0.27f, 0.50f);     // koyu LANE (v1 laneColor)
        [SerializeField] private Color roadWallColor = new Color(0.93f, 0.94f, 1.00f); // DUVAR (tek renk, v1 trackColor)
        [SerializeField] private Color roadChevronColor = new Color(0.98f, 0.78f, 0.74f); // akan ok (v1 krem-pembe)

        [Header("Match Juice (sepet cikinca havai fisek)")]
        [SerializeField] private bool enableExtractFireworks = true;     // sepet dolup yok olunca o noktada havai fisek
        [SerializeField] private GameObject extractFireworksPrefab;      // atarsan: GERCEK havai fisek particle prefab'i (yoksa kod-tabanli renkli patlama)
        [SerializeField] private int extractFireworksCount = 18;         // kod-tabanli patlamada parca sayisi

        [Header("Ses")]
        [Tooltip("Oyun sahnesinde loop calacak background music. Inspector'dan klibi buraya ata.")]
        [SerializeField] private AudioClip gameBackgroundMusic;

        [Header("Booster Butonlari (gecici gorsel; ikon/animasyon sonra giydirilecek)")]
        [SerializeField] private bool showBoosterButtons = true; // kapatirsan kod GECICI bar kurmaz (sahnedeki butonlar varsa onlar kullanilir)
        [SerializeField] private int destroyFillerCost = 100;    // dolgu sepeti yok etme bedeli (coin)
        [SerializeField] private int extraExitCost = 150;        // ekstra cikis kapisi bedeli (coin)
        [SerializeField] private int shuffleCost = 200;          // sepetleri karistirma bedeli (coin)
        [Space(4)]
        [Tooltip("Sahnedeki kalici butonlar (menu 'Build Booster UI' doldurur). Atanmissa kod runtime buton KURMAZ; sadece onClick'i baglar. Gorselleri sen verirsin.")]
        [SerializeField] private UnityEngine.UI.Button destroyFillerButton; // bos = kod gecici buton kurar
        [SerializeField] private UnityEngine.UI.Button extraExitButton;
        [SerializeField] private UnityEngine.UI.Button shuffleButton;
        [Space(2)]
        [Tooltip("Adet yazilari (xN). Kendi TMP'ni butona koy, KONUM/STIL sende; buraya surukle. Bos birakirsan adet yazisi gosterilmez (buton yine adet 0'da kilitlenir). Kod konuma DOKUNMAZ.")]
        [SerializeField] private TMPro.TMP_Text destroyFillerCountText;
        [SerializeField] private TMPro.TMP_Text extraExitCountText;
        [SerializeField] private TMPro.TMP_Text shuffleCountText;
        [Space(4)]
        [Tooltip("Booster ADEDI (envanter). Kullandikca duser, bitince buton kilitlenir. Paketlerle (para ile) doldurulur -> AddBoosterCount().")]
        [SerializeField] private int boosterMaxCount = 3;                     // ust sinir (paket ile bile asilamaz)
        [SerializeField, Range(0, 3)] private int destroyFillerCount = 3;     // baslangic adedi
        [SerializeField, Range(0, 3)] private int extraExitCount = 3;
        [SerializeField, Range(0, 3)] private int shuffleCount = 3;
        [Header("Power-up Satin Alma Paneli")]
        [Tooltip("Power-up BITINCE (x0) butona tiklayinca acilan panel. Kapali baslar.")]
        [SerializeField] private GameObject boosterShopPanel;
        [Tooltip("Panel: SATIN AL butonu -> ilgili power-up'a +3 (coin ile).")]
        [SerializeField] private UnityEngine.UI.Button boosterShopBuyButton;
        [Tooltip("Panel: KAPAT butonu.")]
        [SerializeField] private UnityEngine.UI.Button boosterShopCloseButton;
        [Tooltip("Panel: FIYAT yazisi (SADECE rakam, orn 200). Kod ilgili power-up'in coin bedelini yazar.")]
        [SerializeField] private TMPro.TMP_Text boosterShopText;
        [Tooltip("Panel: alinacak power-up'in IKONU. Kod, tiklanan booster butonunun sprite'ini OTOMATIK buraya koyar.")]
        [SerializeField] private UnityEngine.UI.Image boosterShopIcon;
        int pendingBoosterIndex = -1;

        [Header("UI (Canvas'ta sen kur, sonra bagla)")]
        [SerializeField] private TMPro.TMP_Text moveText;  // kalan hamle
        [SerializeField] private TMPro.TMP_Text coinText;
        const int StartingCoinAmount = 1000;
        [SerializeField] private int coinAmount = StartingCoinAmount;
        [Tooltip("Her level tamamlaninca (win) kazanilan coin.")]
        [SerializeField] private int coinPerLevel = 50;
        [Tooltip("BOLUM yazisi. Canvas'ta TMP olustur, buraya surukle (konum/stil sende).")]
        [SerializeField] private TMPro.TMP_Text levelText;
        [Tooltip("Her bolumun yazisi (index 0 = Level 1). SEN DOLDURURSUN. Bos/eksik bolumde asagidaki format kullanilir.")]
        [SerializeField] private string[] levelTexts;
        [Tooltip("levelTexts'te karsiligi yoksa kullanilir. {0} = bolum numarasi.")]
        [SerializeField] private string levelTextFormat = "Level {0}";
        [SerializeField] private GameObject winPanel;      // KAZANDIN paneli
        [SerializeField] private UnityEngine.UI.Button nextButton; // win panelindeki Next butonu
        [SerializeField] private GameObject losePanel;     // KAYBETTIN paneli
        [Tooltip("Lose panel: Canvas'ta kur+bagla -> BASTAN OYNA (leveli restart).")]
        [SerializeField] private UnityEngine.UI.Button loseRestartButton;
        [Tooltip("Lose panel: REKLAM IZLE LEVELI GEC (odullu reklam -> sonraki level).")]
        [SerializeField] private UnityEngine.UI.Button loseSkipAdButton;
        [Header("Magaza (Shop)")]
        [Tooltip("HUD'daki MAGAZA butonu (paneli acar). Canvas'ta kur+bagla.")]
        [SerializeField] private UnityEngine.UI.Button shopButton;
        [Tooltip("Magaza paneli (coin paketleri icinde). Kapali baslar.")]
        [SerializeField] private GameObject shopPanel;
        [Tooltip("Magaza panelindeki KAPAT (X) butonu.")]
        [SerializeField] private UnityEngine.UI.Button shopCloseButton;

        [Header("Reklamlar")]
        [SerializeField] private AdsManager adsManager;
        [SerializeField, Range(2, 3)] private int interstitialEveryLevels = 3;
        [SerializeField] private int rewardedAdCoinAmount = 100;

        [Header("Level Sistemi")]
        [Min(1)]
        [SerializeField] private int currentLevel = 1;
        [Tooltip("Bolum ilerlemesi cihazda kaydedilsin (kapanip acinca kaldigi bolumden devam). Kapatirsan hep Inspector currentLevel'dan baslar (test icin).")]
        [SerializeField] private bool saveProgress = true;
        const string SaveKeyLevel = "ccl_level";
        const string SaveKeyCoin = "ccl_coin";
        const string SaveKeyBoost0 = "ccl_b0"; // Yok Et adedi (kalici)
        const string SaveKeyBoost1 = "ccl_b1"; // +Kapi adedi (kalici)
        const string SaveKeyBoost2 = "ccl_b2"; // Karistir adedi (kalici)
        [SerializeField] private bool useGeneratedLevels = true;
        [SerializeField] private ArrowsPixelLevelDefinition[] levels;
        [SerializeField] private ArrowsPixelPortraitSet portraitSet; // importer'in urettigi potreler (PNG'lerden). Atanirsa geometrik yerine BUNLAR kullanilir.

        Sprite arrowSprite;

        // ===== FAZ 2: cikarma mekanigi (Arrows) =====
        sealed class TruckInfo { public Transform root; public Vector3 headDir; public Vector3 exitDir; public CargoColor cargo; public int capacity; public int filled; public int flyingCubes; public bool extracted; public bool moving; public int gx; public int gz; }
        sealed class SlotInfo { public Vector3 pos; public TruckInfo occupant; }
        enum GameState { Playing, Won, Lost }
        readonly List<TruckInfo> trucks = new List<TruckInfo>();
        readonly List<SlotInfo> slotList = new List<SlotInfo>();
        readonly Dictionary<CargoColor, List<GameObject>> cubesByColor = new Dictionary<CargoColor, List<GameObject>>();
        int activeCubeTransfers;
        int moveCount = 0;
        [Header("Level Ayarlari")]
        [Min(1)]
        [SerializeField] private int moveLimit = 30;
        GameState gstate = GameState.Playing;
        bool inputLocked = false;
        TruckInfo dragTruck;
        Vector3 dragStartScreen;
        const float TruckGroundY = 0.075f; // tir root zemini: pad yuzeyine yakin, ucma/hover yok
        const float TruckLiftY = 0.34f; // drag baslayinca hafif kaldirma
        const float DragMinPixels = 35f;
        int gridWidth = 3;
        int gridHeight = 3;
        float gridStepX = 1.64f;
        float gridStepZ = 1.20f;
        float truckModelScale = 0.68f;
        float cameraOrthographicSize = 6.5f;
        Vector3 parkingOrigin;   // grid (0,0,0) hucresinin referans dunya merkezi (parkingArea atanmissa ondan)
        Transform parkTransform; // runtime Parking koku (booster yeni kapi acarken duvari bulup degistirir)
        ArrowsPixelLevelDefinition activeLevel;
        ArrowsPixelExitGate[] activeExits = new ArrowsPixelExitGate[0];

        // Candy pastel palet (CargoColorPalette ile ayni aile)
        static readonly Color C_PINK   = new Color(0.99f, 0.48f, 0.54f);
        static readonly Color C_BLUE   = new Color(0.56f, 0.81f, 1.00f);
        static readonly Color C_YELLOW = new Color(1.00f, 0.83f, 0.43f);
        static readonly Color C_GREEN  = new Color(0.57f, 0.90f, 0.68f);
        static readonly Color C_PURPLE = new Color(0.77f, 0.63f, 1.00f);
        static readonly Color C_ORANGE = new Color(1.00f, 0.62f, 0.30f);
        static readonly Color C_CREAM  = new Color(0.99f, 0.92f, 0.80f);
        static readonly Color C_PAD    = new Color(0.99f, 0.86f, 0.74f);

        [Header("Palet / Cevre Renkleri (Inspector'dan ayarla; Play'de uygulanir)")]
        [SerializeField] private Color backgroundColor = new Color(0.90f, 0.76f, 0.86f); // arka plan (kamera + zemin plani)
        [SerializeField] private Color areaColor       = new Color(0.71f, 0.63f, 0.85f); // tepsi: board / slot / potre
        [SerializeField] private Color areaDarkColor   = new Color(0.74f, 0.67f, 0.90f); // grid soket
        [Tooltip("TEK-MESH board tepsisi (yumusak oval, yukseltilmis kenar + cukur taban). Kapatirsan eski cok-parca duvar/zemin.")]
        [SerializeField] private bool useSingleMeshBoard = false;
        [SerializeField] private Color wallColor       = new Color(0.93f, 0.94f, 1.00f); // moduler duvar + kapi postu

        [Header("Toon Gorunum (cartoon outline + golge; canli)")]
        [SerializeField] private bool useToonShader = true; // true = ESKI TOON (outline'li, kesin); false = parlak/Lit/bloom deneme (kullanici begenmedi -> toon'a donuldu)
        [SerializeField] private Color toonOutlineColor = new Color(0.11f, 0.08f, 0.17f, 1f); // cizgi rengi (koyu)
        [SerializeField, Range(0f, 0.08f)] private float cubeOutline   = 0f; // potre kupleri: siyah serit olmamasi icin outline kapali
        [SerializeField, Range(0f, 0.08f)] private float basketOutline = 0.006f; // sepet outline: referanstaki gibi temiz/ince
        [SerializeField, Range(0f, 1f)] private float toonShade = 0.28f;         // golge bandi sertligi (kontrast)
        [SerializeField, Range(0f, 1f)] private float toonRamp  = 0.34f;         // lit/golge esigi
        [SerializeField, Range(0f, 1f)] private float colorVividness = 0.32f;    // renk canliligi (doygunluk artisi -> cartoon); pastelden CANLI'ya
        // --- Feedback (titresim + tutma outline). SESLER artik AudioManager'da (oyun kodu sadece AudioManager.Play cagirir). ---
        [Header("Feedback (titresim + outline)")]
        [SerializeField] private bool enableHaptics = true;                      // telefon titresimi (kup dolarken) - SADECE cihazda
        [SerializeField] private Color grabOutlineColor = new Color(1f, 0.95f, 0.45f, 1f); // tutunca PARLAK outline rengi
        [SerializeField, Range(0f, 0.2f)] private float grabOutlineWidth = 0.075f;          // tutunca outline kalinligi (normal sepet ~0.036)

        Transform root;
        ColorCargoLoopGame oldGame;   // birebir uÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â§an kup mesh/material kaynagi
        readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        void Start()
        {
            // Eski oyun ayni sahnedeyse: REFERANSINI AL (kup uretici icin), sonra devre disi birak (loop kurmasin)
            oldGame = FindObjectOfType<ColorCargoLoopGame>();
            if (oldGame != null) oldGame.gameObject.SetActive(false);

            // Importer potreleri (PNG'lerden) varsa geometrik potreler yerine onlar kullanilir
            ArrowsPixelLevelLibrary.PortraitOverride = (portraitSet != null && portraitSet.HasPortraits)
                ? portraitSet.ToRowsArray() : null;

            if (saveProgress) currentLevel = Mathf.Max(1, PlayerPrefs.GetInt(SaveKeyLevel, currentLevel)); // kayitli bolumden devam et
            if (saveProgress) coinAmount = PlayerPrefs.GetInt(SaveKeyCoin, StartingCoinAmount);           // ilk kurulumda 1000, sonra kayitli coin
            if (saveProgress) // kayitli power-up adetlerini yukle
            {
                destroyFillerCount = PlayerPrefs.GetInt(SaveKeyBoost0, destroyFillerCount);
                extraExitCount     = PlayerPrefs.GetInt(SaveKeyBoost1, extraExitCount);
                shuffleCount       = PlayerPrefs.GetInt(SaveKeyBoost2, shuffleCount);
            }
            if (gameBackgroundMusic != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(gameBackgroundMusic, true);
            if (buildOnStart) BuildLayout();
        }

        // Pixel-art tile = BIREBIR uÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â§an kargo kupu (GetRoundedCargoMesh + candy toon), basik+sik tile olarak
        void PicCube(Transform parent, string name, Vector3 pos, Vector3 scale, char ch)
        {
            if (oldGame != null)
            {
                GameObject cube = oldGame.CreateCargoBlockObject(CharToCargo(ch), name);
                cube.transform.SetParent(parent, false);
                cube.transform.localPosition = pos;
                cube.transform.localScale = scale;
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                if (cubeRenderer == null) cubeRenderer = cube.GetComponentInChildren<Renderer>();
                if (cubeRenderer != null)
                {
                    cubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    cubeRenderer.receiveShadows = false;
                }
                CargoColor _cc = CharToCargo(ch);
                if (!cubesByColor.TryGetValue(_cc, out var _lst)) { _lst = new List<GameObject>(); cubesByColor[_cc] = _lst; }
                _lst.Add(cube); // teslim icin takip
                // Kup gorunumu (kullanici tarifi): toon outline + ON/KENAR isik yansimasi (rim+highlight) + ust hafif golge
                var pm = cubeRenderer != null && cubeRenderer.sharedMaterial != null ? new Material(cubeRenderer.sharedMaterial) : null;
                if (pm != null)
                {
                    if (pm.HasProperty("_OutlineColor")) pm.SetColor("_OutlineColor", toonOutlineColor);
                    if (pm.HasProperty("_OutlineWidth")) pm.SetFloat("_OutlineWidth", cubeOutline);
                    if (pm.HasProperty("_ShadowColor")) pm.SetColor("_ShadowColor", Color.Lerp(CharColor(ch), new Color(0.22f, 0.18f, 0.38f), 0.20f));
                    if (pm.HasProperty("_RampThreshold")) pm.SetFloat("_RampThreshold", toonRamp);
                    if (pm.HasProperty("_RimStrength")) pm.SetFloat("_RimStrength", 0.18f);
                    if (pm.HasProperty("_RimColor")) pm.SetColor("_RimColor", new Color(0.96f, 0.94f, 1.00f));
                    if (pm.HasProperty("_HighlightStrength")) pm.SetFloat("_HighlightStrength", 0.95f);
                    if (pm.HasProperty("_HighlightColor")) pm.SetColor("_HighlightColor", new Color(0.96f, 0.94f, 1.00f));
                    if (pm.HasProperty("_ShadeStrength")) pm.SetFloat("_ShadeStrength", toonShade);
                    if (pm.HasProperty("_EmissionColor")) pm.SetColor("_EmissionColor", CharColor(ch) * 0.025f);
                    if (pm.HasProperty("_EmissionStrength")) pm.SetFloat("_EmissionStrength", 0.20f);
                    if (pm.HasProperty("_Color")) pm.SetColor("_Color", Vivid(CharColor(ch))); // canli renk
                    cubeRenderer.sharedMaterial = pm;
                }
            }
            else
            {
                var g = RoundedCube(name, parent, pos, 1f, CharColor(ch), 0f);
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
                case 'O': return CargoColor.Orange;
                default:  return CargoColor.Red; // P = candy pembe-kirmizi
            }
        }

        static Color CharColor(char ch)
        {
            return ch == 'B' ? C_BLUE : ch == 'Y' ? C_YELLOW : ch == 'G' ? C_GREEN : ch == 'U' ? C_PURPLE : ch == 'O' ? C_ORANGE : C_PINK;
        }

        public void BuildLayout()
        {
            if (root != null) DestroyImmediate(root.gameObject);
            foreach (var t in FindObjectsOfType<Transform>(true))
            {
                if (t != null && t.name == "ArrowsPixelRoot") DestroyImmediate(t.gameObject);
            }
            root = new GameObject("ArrowsPixelRoot").transform;
            trucks.Clear(); slotList.Clear(); cubesByColor.Clear(); activeCubeTransfers = 0; moveCount = 0; inputLocked = false; gstate = GameState.Playing; levelTransitionPending = false;
            ApplyMobilePerformanceProfile();
            ResolveActiveLevel();
            ValidateActiveLevel();
            ApplyReferenceVisualProfile();
            useSingleMeshBoard = false; // eski moduler board tasarimi zorunlu; shader/material tarafina dokunma
            backgroundColor = new Color(0.90f, 0.76f, 0.86f); // mevcut lavanta/pembe asset zemini
            areaColor = new Color(0.71f, 0.63f, 0.85f);      // mevcut board tray lavanta
            areaDarkColor = new Color(0.74f, 0.67f, 0.90f);  // mevcut hucre/slot/potre soketi
            wallColor = Color.Lerp(backgroundColor, Color.white, 0.80f);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false); // magaza kapali baslar
            if (boosterShopPanel != null) boosterShopPanel.SetActive(false); // power-up satin alma kapali baslar
            WireNextButton();
            WireLoseButtons();
            WireShop();
            WireBoosterShop();
            DisableLayoutPadOutlines();

            // Alanlar atanmissa (Editor menusu kurdu): STATIK gorseller sahnede zaten -> sadece DINAMIK icerik dolar.
            // Hicbiri atanmamissa: eski runtime davranisi (geriye donuk uyum, hicbir sey bozulmaz).
            if (pictureArea == null && slotPoints == null && parkingArea == null)
                BuildBackground();

            BuildSlots(1.0f);           // slotPoints atanmissa onlari kullanir
            BuildParking(-3.0f);        // parkingArea atanmissa onu merkez alir
            try
            {
                BuildPictureGrid(3.7f); // pictureArea atanmissa oraya kurar
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            SetupBrightLighting();
            // Clean project: keep default Unity materials; only convert legacy/toon references to default shaders.
            ConvertSceneToBright();
            if (!useToonShader) SetupPostFX();
            // Runtime texture sari cast uretebildigi icin kapali; mevcut asset/material renkleri korunur.
            if (autoSetupCamera) SetupCamera();
            UpdateMoveUI();
            UpdateCoinUI();
            UpdateLevelUI();
            BuildBoosterButtons();
            EnsureAdsManager();
            NotificationManager.Instance.CancelReminders(); // oyuncu oyunda -> bekleyen hatirlaticilari iptal et + sistemi olustur (arka plana gidince planlar)
            Analytics.LevelStart(currentLevel);
        }

        void ApplyReferenceVisualProfile()
        {
            basketHeightScale = 0.68f;
            basketFootprint = 0.76f;
            basketCornerRound = 0.32f;
            portraitCubeSize = 0.155f;
            cubeOutline = 0f;
            basketOutline = 0.006f;
            toonShade = 0.28f;
            toonRamp = 0.34f;
            colorVividness = 0.32f;
            backgroundColor = new Color(0.90f, 0.76f, 0.86f);
            areaColor = new Color(0.71f, 0.63f, 0.85f);
            areaDarkColor = new Color(0.74f, 0.67f, 0.90f);
            wallColor = Color.Lerp(backgroundColor, Color.white, 0.80f);
        }
        void ApplyMobilePerformanceProfile()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadowDistance = 14f;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            var cam = gameCamera != null ? gameCamera : Camera.main;
            if (cam == null) return;
            var ud = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (ud != null)
            {
                ud.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
                ud.renderPostProcessing = false;
            }
        }
        // PARLAK MOD ISIK: ambient'i parlak-notr yapar -> renkler golgede bile CANLI kalir (loÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¸ mavimsi ambient donuklastiriyordu).
        void SetupBrightLighting()
        {
            UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            UnityEngine.RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f); // notr; COK flat degil -> kontrast + specular shine kalsin
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var ud = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (ud != null) ud.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; // SMAA pixel-art'i BULANIKLASTIRIYORDU -> kapat (MSAA4 net yeter)
            }
        }

        // BLOOM + renk post-fx: doygun renkler ISILDAR (canli/parlak glow). Camera post-processing acilir + global Volume kurulur.
        void SetupPostFX()
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var ud = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (ud != null) ud.renderPostProcessing = true;
            }
            var existing = GameObject.Find("PostFX_Volume");
            UnityEngine.Rendering.Volume vol;
            if (existing != null) vol = existing.GetComponent<UnityEngine.Rendering.Volume>();
            else { var go = new GameObject("PostFX_Volume"); vol = go.AddComponent<UnityEngine.Rendering.Volume>(); vol.isGlobal = true; vol.priority = 10f; }
            if (vol == null) return;
            if (vol.profile == null) vol.profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            var profile = vol.profile;
            UnityEngine.Rendering.Universal.Bloom bloom;
            if (!profile.TryGet(out bloom)) bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(0.95f);
            bloom.scatter.Override(0.72f);
            UnityEngine.Rendering.Universal.ColorAdjustments ca;
            if (!profile.TryGet(out ca)) ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            ca.active = true;
            ca.saturation.Override(20f);  // ekstra doygunluk
            ca.contrast.Override(8f);
            ca.postExposure.Override(0.10f);
        }

        // PARLAK/PURUZSUZ MOD: sahnedeki TUM toon malzemeleri URP Lit'e cevirir (yumusak golge + specular, outline YOK).
        // useToonShader=false iken BuildLayout sonunda cagrilir. Toon'a donmek istersen Inspector'dan useToonShader=true.
        void ConvertSceneToBright()
        {
            var lit = Shader.Find("Standard");
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;

            var cache = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mats = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null || !NeedsDefaultShaderReset(src)) continue;
                    if (!cache.TryGetValue(src, out var lm))
                    {
                        Color col = ReadLegacyMaterialColor(src);
                        lm = new Material(lit) { name = src.name + "_Default" };
                        if (lm.HasProperty("_Color")) lm.SetColor("_Color", col);
                        if (lm.HasProperty("_BaseColor")) lm.SetColor("_BaseColor", col);
                        if (lm.HasProperty("_Glossiness")) lm.SetFloat("_Glossiness", 0.12f);
                        if (lm.HasProperty("_Smoothness")) lm.SetFloat("_Smoothness", 0.12f);
                        if (lm.HasProperty("_Metallic")) lm.SetFloat("_Metallic", 0f);
                        cache[src] = lm;
                    }
                    mats[i] = lm;
                    changed = true;
                }
                if (changed) mr.sharedMaterials = mats;
            }
        }

        bool NeedsDefaultShaderReset(Material src)
        {
            if (src == null) return false;
            if (src.shader == null) return true;
            string shaderName = src.shader.name;
            return shaderName == "Color Cargo Loop/Toon Plastic"
                || shaderName == "Hidden/InternalErrorShader"
                || shaderName.Contains("InternalErrorShader")
                || shaderName.Contains("Error");
        }

        Color ReadLegacyMaterialColor(Material src)
        {
            if (src != null)
            {
                if (src.HasProperty("_Color")) return src.GetColor("_Color");
                if (src.HasProperty("_BaseColor")) return src.GetColor("_BaseColor");
                string name = src.name ?? string.Empty;
                int us = name.LastIndexOf('_');
                if (us >= 0 && name.Length >= us + 7)
                {
                    string hex = name.Substring(us + 1, 6);
                    if (int.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) &&
                        int.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) &&
                        int.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
                    {
                        return new Color(r / 255f, g / 255f, b / 255f, 1f);
                    }
                }
                if (name.Contains("Background")) return backgroundColor;
            }
            return areaColor;
        }
        // Procedural ARKA PLAN dokulari (LACIVERT bazli, renk dokuya BAKILI): 0=felt grain, 1=dikey cizgi, 2=grid.
        // currentLevel%3 ile level basina DEGISIR ("degisir durur"). Hafif acik lacivert desen -> koyu lacivert uzerinde gorunur.
        Texture2D[] _bgTex = new Texture2D[3];
        Texture2D GetBgTexture(int variant)
        {
            variant = ((variant % 3) + 3) % 3;
            if (_bgTex[variant] != null) UnityEngine.Object.Destroy(_bgTex[variant]);
            int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "BgGrad" + variant, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color center = new Color(0.96f, 0.80f, 0.86f);   // mevcut lavanta-pembe merkez
            Color edge = new Color(0.90f, 0.76f, 0.86f);     // mevcut lavanta kenar
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float nx = x / (float)(N - 1), ny = y / (float)(N - 1);
                    float dx = nx - 0.5f, dy = ny - 0.42f;
                    float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 0.72f);  // 0 merkez .. 1 kose
                    float g = Mathf.SmoothStep(0f, 1f, dist);
                    float cloud = Mathf.PerlinNoise(nx * 3.2f + variant * 7f, ny * 4.4f) - 0.5f; // yumusak bulut
                    g = Mathf.Clamp01(g + cloud * 0.10f);
                    var c = Color.Lerp(center, edge, g); c.a = 1f;        // mevcut pembe/lavanta zemin
                    px[y * N + x] = c;
                }
            tex.SetPixels(px); tex.Apply(true);
            _bgTex[variant] = tex; return tex;
        }

        // Arka plan ("Background"/"BG") -> level'e gore DEGISEN lacivert doku. Doku lacivert bakili -> tint beyaz (renk dokudan).
        void ApplyBackgroundTexture()
        {
            var tex = GetBgTexture(currentLevel);
            if (tex == null) return;
            Vector2 tile = new Vector2(1f, 1f); // gradient tek sefer yansisin (tekrar yok)
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string nm = mr.gameObject.name;
                if (nm != "Background" && nm != "BG") continue;
                var src = mr.sharedMaterial;
                if (src == null) continue;
                var m = new Material(src); mr.sharedMaterial = m; // paylasilani bozma
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tile); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tile); }
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", backgroundColor);
                if (m.HasProperty("_Color")) m.SetColor("_Color", backgroundColor);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);        // MAT (parlama yok)
            }
        }

        public void LoadLevel(int oneBasedLevel)
        {
            currentLevel = Mathf.Max(1, oneBasedLevel);
            if (saveProgress) { PlayerPrefs.SetInt(SaveKeyLevel, currentLevel); PlayerPrefs.Save(); } // ilerlemeyi cihazda kaydet
            BuildLayout();
        }

        public void LoadNextLevel()
        {
            if (levelTransitionPending) return;

            int completedLevel = currentLevel;
            levelTransitionPending = true;

            if (ShouldShowInterstitial(completedLevel))
            {
                EnsureAdsManager().ShowInterstitial(() => LoadNextLevelNow(completedLevel));
                return;
            }

            LoadNextLevelNow(completedLevel);
        }

        bool levelTransitionPending;

        void LoadNextLevelNow(int completedLevel)
        {
            levelTransitionPending = false;
            if (completedLevel == 5 || completedLevel == 10) ReviewManager.Instance.RequestReviewOnce(); // 5/10'u gecince Google yildiz/yorum paneli (cihaz basina 1 kez)
            LoadLevel(completedLevel + 1);
        }

        bool ShouldShowInterstitial(int completedLevel)
        {
            int interval = Mathf.Clamp(interstitialEveryLevels, 2, 3);
            return completedLevel > 0 && completedLevel % interval == 0;
        }

        AdsManager EnsureAdsManager()
        {
            if (adsManager == null) adsManager = AdsManager.Instance;
            return adsManager;
        }

        public void ShowRewardedAdForCoins()
        {
            EnsureAdsManager().ShowRewarded(granted =>
            {
                if (!granted) return;
                coinAmount += Mathf.Max(0, rewardedAdCoinAmount);
                UpdateCoinUI();
                Analytics.Event("rewarded_ad_coin");
            });
        }

        void WireNextButton()
        {
            if (nextButton == null) return;
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        void OnNextButtonClicked()
        {
            LoadNextLevel();
        }

        // LOSE panel butonlari: BASTAN OYNA + REKLAM IZLE LEVELI GEC (atanmazsa sessiz gecer)
        void WireLoseButtons()
        {
            if (loseRestartButton != null) { loseRestartButton.onClick.RemoveListener(OnLoseRestart); loseRestartButton.onClick.AddListener(OnLoseRestart); }
            if (loseSkipAdButton != null) { loseSkipAdButton.onClick.RemoveListener(OnLoseSkipAd); loseSkipAdButton.onClick.AddListener(OnLoseSkipAd); loseSkipAdButton.gameObject.SetActive(false); } // Next/skip butonu lose'da gizli (kullanici istegi)
        }

        void OnLoseRestart() { EnsureAdsManager().ShowInterstitial(() => RestartLevel()); } // Again -> reklam izle, sonra leveli bastan oyna

        // MAGAZA: HUD butonu paneli acar, panel ici KAPAT butonu kapatir (atanmazsa sessiz gecer)
        void WireShop()
        {
            if (shopButton != null) { shopButton.onClick.RemoveListener(OpenShop); shopButton.onClick.AddListener(OpenShop); }
            if (shopCloseButton != null) { shopCloseButton.onClick.RemoveListener(CloseShop); shopCloseButton.onClick.AddListener(CloseShop); }
        }
        public void OpenShop() { if (shopPanel != null) shopPanel.SetActive(true); }
        public void CloseShop() { if (shopPanel != null) shopPanel.SetActive(false); }

        void OnLoseSkipAd()
        {
            EnsureAdsManager().ShowRewarded(granted =>
            {
                if (!granted) return;                 // reklam izlenmediyse panel acik kalir
                Analytics.Event("rewarded_ad_skip_level");
                LoadNextLevel();                      // reklam izlendi -> leveli GEC
            });
        }

        public void RestartLevel()
        {
            BuildLayout();
        }

        // BillingManager (IAP) coin paketi satin alinca buradan coin ekler (+ kalici kaydeder)
        public void AddCoins(int n)
        {
            coinAmount += Mathf.Max(0, n);
            UpdateCoinUI();
        }

        void ResolveActiveLevel()
        {
            activeLevel = null;
            int index = Mathf.Max(0, currentLevel - 1);
            if (levels != null && index < levels.Length) activeLevel = levels[index];

            if (activeLevel == null && useGeneratedLevels)
            {
                activeLevel = ArrowsPixelLevelLibrary.CreateGeneratedLevel(currentLevel);
            }

            if (activeLevel == null)
            {
                activeLevel = ArrowsPixelLevelLibrary.CreateGeneratedLevel(1);
                activeLevel.moveLimit = Mathf.Max(1, moveLimit);
            }

            moveLimit = Mathf.Max(1, activeLevel.moveLimit);
            gridWidth = Mathf.Max(2, activeLevel.gridWidth);
            gridHeight = Mathf.Max(2, activeLevel.gridHeight);
            gridStepX = Mathf.Max(0.2f, activeLevel.gridStepX);
            gridStepZ = Mathf.Max(0.2f, activeLevel.gridStepZ);
            truckModelScale = Mathf.Max(0.2f, activeLevel.truckModelScale);
            cameraOrthographicSize = Mathf.Max(1f, activeLevel.cameraOrthographicSize);
            activeExits = activeLevel.exits != null && activeLevel.exits.Length > 0
                ? activeLevel.exits
                : new[] { new ArrowsPixelExitGate { x = gridWidth / 2, z = gridHeight - 1, direction = ArrowsPixelExitDirection.Up } };
        }

        // Level saglik kontrolu (Faz A): renk dengesi + bos hucre + ilk hamle.
        // Sorunlari Console'a uyari olarak yazar, oyunu durdurmaz. Uretilen leveller
        // ApplyTruckCapacitiesFromPortrait ile zaten dengelenir; bu kontrol ozellikle elle yazilan leveller icin.
        void ValidateActiveLevel()
        {
            if (activeLevel == null) return;
            string lv = "[Level " + currentLevel + " dogrulama] ";

            // 1) Renk dengesi: potre ihtiyaci vs ayni renk sepet kapasitesi toplami (fazla sepetler dolgu/hamle sepetidir, sorun degil)
            int[] need = ArrowsPixelLevelLibrary.CountPortraitColors(activeLevel.portraitRows);
            int[] have = new int[6];
            int truckCount = 0;
            if (activeLevel.trucks != null)
            {
                foreach (var tr in activeLevel.trucks)
                {
                    if (tr == null) continue;
                    if (!CellExists(tr.x, tr.z))
                    {
                        Debug.LogWarning(lv + "sepet grid disinda/maskeli hucrede: (" + tr.x + "," + tr.z + ") - spawn atlanir");
                        continue;
                    }
                    truckCount++;
                    for (int i = 0; i < 6; i++)
                        if (ArrowsPixelLevelLibrary.IndexToCargo(i) == tr.color) { have[i] += Mathf.Max(1, tr.capacity); break; }
                }
            }
            for (int i = 0; i < 6; i++)
                if (need[i] > 0 && have[i] < need[i])
                    Debug.LogWarning(lv + ArrowsPixelLevelLibrary.IndexToCargo(i) + " kapasitesi yetersiz: potre " + need[i] + " kup istiyor, sepetlerde " + have[i] + " var");

            // 2) En az 1 bos hucre olmali ki kaydirma hamlesi yapilabilsin
            int cellCount = 0;
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                    if (CellExists(gx, gz)) cellCount++;
            if (truckCount >= cellCount)
                Debug.LogWarning(lv + "BOS HUCRE YOK (" + truckCount + " sepet / " + cellCount + " hucre) - oyuncu kaydirma yapamaz");

            // 3) Ilk hamle mumkun mu: bir sepet ya bos hucreye kayabilmeli ya da cikis kenarindan cikabilmeli
            bool anyMove = false;
            if (activeLevel.trucks != null)
            {
                int[] dxs = { 1, -1, 0, 0 };
                int[] dzs = { 0, 0, 1, -1 };
                foreach (var tr in activeLevel.trucks)
                {
                    if (tr == null || !CellExists(tr.x, tr.z)) continue;
                    for (int d = 0; d < 4 && !anyMove; d++)
                    {
                        int nx = tr.x + dxs[d], nz = tr.z + dzs[d];
                        bool occupied = false;
                        foreach (var o in activeLevel.trucks)
                            if (o != null && o != tr && o.x == nx && o.z == nz) { occupied = true; break; }
                        if (CellExists(nx, nz) && !occupied) anyMove = true;
                        else if (IsExitEdge(tr.x, tr.z, dxs[d], dzs[d])) anyMove = true;
                    }
                    if (anyMove) break;
                }
            }
            if (!anyMove && truckCount > 0)
                Debug.LogWarning(lv + "DEADLOCK: hicbir sepet ilk hamlede hareket edemiyor");
        }

        // Renk canliligi: doygunlugu artirir (cartoon "canli" his). colorVividness=0 ise dokunmaz.
        Color Vivid(Color c)
        {
            if (colorVividness <= 0.001f) return c;
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            s = Mathf.Clamp01(s * (1f + colorVividness * 1.65f) + colorVividness * 0.18f); // kontrollu doygunluk
            v = Mathf.Clamp01(v * (1f + colorVividness * 0.04f) + colorVividness * 0.025f); // parlak/cilali, soluk degil
            Color o = Color.HSVToRGB(h, s, v);
            o.a = c.a;
            return o;
        }

        // ---------- Materyal ----------
        Material Mat(Color c)
        {
            string key = c.r.ToString("0.00") + c.g.ToString("0.00") + c.b.ToString("0.00");
            Material m;
            if (matCache.TryGetValue(key, out m)) return m;
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(sh) { name = "APX_" + key };
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            // Ortam objelerinde OUTLINE YOK: buyuk tepsi/panellerde outline obje-uzayinda
            // sisip kalin siyah cizgi artifaktina donusuyordu. Sadece toon golge bandi kalir.
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0f);
            if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", toonShade);
            if (m.HasProperty("_RampThreshold")) m.SetFloat("_RampThreshold", toonRamp);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", Color.Lerp(c, new Color(0.22f, 0.18f, 0.38f), 0.24f));
            if (m.HasProperty("_HighlightColor")) m.SetColor("_HighlightColor", new Color(0.96f, 0.94f, 1.00f));
            if (m.HasProperty("_HighlightStrength")) m.SetFloat("_HighlightStrength", 0.86f);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.08f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            m.color = c;
            matCache[key] = m;
            return m;
        }

        void DisableLayoutPadOutlines()
        {
            GameObject layout = GameObject.Find("ArrowsPixelLayout");
            if (layout == null) return;
            foreach (Renderer rend in layout.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null || rend.sharedMaterial == null) continue;
                Material m = new Material(rend.sharedMaterial);
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.10f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.10f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                // Arka plan paneli -> backgroundColor; diger krem alanlar (slot+potre bg) -> areaColor
                bool isBackground = rend.gameObject.name == "Background";
                Color cc = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.clear;
                if (isBackground || Mathf.Abs(cc.r - 0.99f) + Mathf.Abs(cc.g - 0.86f) + Mathf.Abs(cc.b - 0.74f) < 0.22f)
                {
                    Color targetColor = isBackground ? backgroundColor : areaColor;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", targetColor);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", targetColor);
                    if (isBackground)
                    {
                        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", Texture2D.whiteTexture);
                        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", Texture2D.whiteTexture);
                    }
                }
                rend.sharedMaterial = m;
            }
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
                if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.055f, 0.045f, 0.09f, 1f));
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
                if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", Color.Lerp(color, new Color(0.07f, 0.05f, 0.12f), 0.58f));
                if (m.HasProperty("_RampThreshold")) m.SetFloat("_RampThreshold", 0.56f);
                if (m.HasProperty("_HighlightStrength")) m.SetFloat("_HighlightStrength", 0.72f);
                if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.30f);
                if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", 0.50f);
            }
            mr.sharedMaterial = m;
            return g;
        }

        // Yuvarlak kose zemin pad (arti + 4 disk)
        void Pad(string name, Transform parent, Vector3 center, float sx, float sz, Color color, float h = 0.08f)
        {
            RoundedPad(name, parent, center, sx, sz, color, h, Mathf.Min(sx, sz) * 0.28f);
        }

        // Pad ile ayni sekil ama kose yaricapi disaridan verilir (buyuk zemin panellerinde %28 fazla yuvarlak kaliyor)
        void RoundedPad(string name, Transform parent, Vector3 center, float sx, float sz, Color color, float h, float r)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = center;
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
        // DINAMIK arka plan paleti: her level kuratorlu yumusak/orta-ton renk -> parlak kargo renkleri & potre one cikar.
        static readonly Color[] DynamicBgPalette =
        {
            new Color(0.36f, 0.40f, 0.60f), // indigo (Pixel Flow hissi)
            new Color(0.30f, 0.52f, 0.55f), // teal
            new Color(0.50f, 0.40f, 0.58f), // erik/plum
            new Color(0.38f, 0.52f, 0.46f), // yumusak orman yesili
            new Color(0.56f, 0.40f, 0.50f), // berry
            new Color(0.42f, 0.48f, 0.68f), // periwinkle
            new Color(0.40f, 0.46f, 0.58f), // arduvaz mavisi
            new Color(0.54f, 0.46f, 0.52f), // sicak mor-gri
        };

        Color DynamicBackgroundForLevel(int level)
        {
            int n = DynamicBgPalette.Length;
            return DynamicBgPalette[(Mathf.Max(1, level) - 1) % n];
        }

        void BuildBackground()
        {
            Box("BG", root, new Vector3(0f, -0.2f, 0.5f), new Vector3(14f, 0.2f, 22f), backgroundColor);
        }

        // ---------- UST: pixel-art resim ----------
        void BuildPictureGrid(float topZ)
        {
            string[] pic = GetActivePortraitRows();
            float stepX = activeLevel != null ? Mathf.Max(0.01f, activeLevel.portraitStepX) : 0.060f;
            float stepZ = activeLevel != null ? Mathf.Max(0.01f, activeLevel.portraitStepZ) : 0.060f;
            Vector3 tileScale = activeLevel != null ? activeLevel.portraitTileScale : new Vector3(0.057f, 0.10f, 0.057f);
            if (tileScale.x <= 0f || tileScale.y <= 0f || tileScale.z <= 0f) tileScale = new Vector3(0.057f, 0.10f, 0.057f);
            // Inspector'dan kolay ayar: >0 ise level degerini ezer (0.14 = sepete dolan kup ile ayni boyut)
            if (portraitCubeSize > 0.001f)
            {
                tileScale = new Vector3(portraitCubeSize, portraitCubeSize * 0.72f, portraitCubeSize);
                stepX = stepZ = portraitCubeSize * 1.00f;
            }
            GameObject grid = new GameObject("PictureGrid");
            int rows = pic.Length;
            int cols = 0;
            for (int i = 0; i < rows; i++) if (!string.IsNullOrEmpty(pic[i])) cols = Mathf.Max(cols, pic[i].Length);

            // Atanmis pictureArea varsa kup-resim oraya kurulur (pozisyon+rotasyon ondan);
            // yoksa eski sabit konum (Pixel Flow gibi zemine oturur).
            if (pictureArea != null)
            {
                grid.transform.SetParent(pictureArea, false);
                grid.transform.localPosition = new Vector3(0f, 0.018f, 0f);
                grid.transform.localRotation = Quaternion.identity;

                // Statik krem pano runtime'da area rengine boyanip kayboluyordu; gizle,
                // parking board ile AYNI dilde zemin kur: area tepsi + koyu soket (root altinda -> rebuild'de temizlenir).
                Transform bb = pictureArea.Find("PictureBackboard");
                if (bb != null) bb.gameObject.SetActive(false);
                GameObject pgnd = new GameObject("PictureGround");
                pgnd.transform.SetParent(root, false);
                pgnd.transform.SetPositionAndRotation(pictureArea.position, pictureArea.rotation);
                // Zemin, resim icerigine gore otomatik boyutlanir (kup boyutu degisince tasma olmaz)
                int minC = int.MaxValue, maxC = -1, minR = int.MaxValue, maxR = -1;
                for (int r = 0; r < rows; r++)
                {
                    string row = pic[r]; if (string.IsNullOrEmpty(row)) continue;
                    for (int c = 0; c < row.Length; c++)
                        if (row[c] != '.') { if (c < minC) minC = c; if (c > maxC) maxC = c; if (r < minR) minR = r; if (r > maxR) maxR = r; }
                }
                if (maxC < 0) { minC = 0; maxC = cols - 1; minR = 0; maxR = rows - 1; }
                float pw = (maxC - minC) * stepX + tileScale.x + 0.34f;
                float ph = (maxR - minR) * stepZ + tileScale.z + 0.34f;
                float pcx = ((minC + maxC) * 0.5f - (cols - 1) * 0.5f) * stepX;
                float pcz = ((rows - 1) * 0.5f - (minR + maxR) * 0.5f) * stepZ;
                // Potre cercevesi/zemini (Inspector'dan kapatilabilir). KAPALI = kupler direkt resmi olusturur, cerceve yok.
                if (showPortraitFrame)
                {
                    // INCE cerceve (kalin DEGIL) -> potre acik pembe bg'den belli olur + ic acik soket
                    Color pframe = new Color(0.58f, 0.47f, 0.82f); // medium lavanta cerceve (bg'den belirgin)
                    RoundedPad("PictureFrame", pgnd.transform, new Vector3(pcx, -0.115f, pcz), pw + 0.20f, ph + 0.20f, pframe, 0.055f, 0.30f);
                    RoundedPad("PictureCell", pgnd.transform, new Vector3(pcx, -0.085f, pcz), pw, ph, areaDarkColor, 0.045f, 0.26f);
                }
                BuildPortraitConveyor(pgnd.transform, pcx, pcz, pw, ph); // potre etrafinda hareketli yol
            }
            else
            {
                grid.transform.SetParent(root, false);
                grid.transform.localPosition = new Vector3(0f, 0.10f, topZ);
                grid.transform.localRotation = Quaternion.identity;
            }

            for (int r = 0; r < rows; r++)
            {
                string row = pic[r];
                if (string.IsNullOrEmpty(row)) continue;
                for (int c = 0; c < row.Length; c++)
                {
                    char ch = row[c];
                    if (ch == '.') continue;
                    Vector3 lp = new Vector3((c - (cols - 1) * 0.5f) * stepX, 0f, ((rows - 1) * 0.5f - r) * stepZ);
                    PicCube(grid.transform, "Px_" + r + "_" + c, lp, tileScale, ch);
                }
            }
        }

        string[] GetActivePortraitRows()
        {
            if (activeLevel != null && activeLevel.portraitRows != null && activeLevel.portraitRows.Length > 0)
                return activeLevel.portraitRows;
            return ArrowsPixelLevelLibrary.CreateGeneratedLevel(1).portraitRows;
        }

        // ---------- ORTA: 3 slot ----------
        void BuildSlots(float z)
        {
            int n = Mathf.Max(1, slotCount);

            // Atanmis slot noktalari varsa SATIR GEOMETRISINI ondan al (merkez X + Z), slotCount kadar esit slot diz.
            if (slotPoints != null && slotPoints.Length > 0)
            {
                // Statik krem padler runtime'da area rengine boyandigi icin gorunmez kaliyordu;
                // onlari gizle, parking grid hucresiyle AYNI gorsel soketi kur (root altinda -> rebuild'de temizlenir).
                GameObject slotVis = new GameObject("SlotPads");
                slotVis.transform.SetParent(root, false);
                float cx = 0f, cz = 0f; int cnt = 0;
                for (int i = 0; i < slotPoints.Length; i++)
                {
                    if (slotPoints[i] == null) continue;
                    Transform spad = slotPoints[i].Find("Pad"); if (spad != null) spad.gameObject.SetActive(false);
                    Transform srim = slotPoints[i].Find("Rim"); if (srim != null) srim.gameObject.SetActive(false);
                    cx += slotPoints[i].position.x; cz += slotPoints[i].position.z; cnt++;
                }
                if (cnt > 0) { cx /= cnt; cz /= cnt; }
                cz += slotZOffset; // portre cercevesinden uzaklastir (Inspector'dan ayarlanir)
                Color slotC = new Color(0.58f, 0.47f, 0.82f); // KOYU lavanta soket -> acik bg'den NET belli (kullanici istegi)
                for (int i = 0; i < n; i++)
                {
                    float x = cx + (i - (n - 1) * 0.5f) * gridStepX; // satiri ortala, grid sutun araligi ile diz
                    Vector3 sp = new Vector3(x, TruckGroundY, cz);
                    Pad("SlotCell_" + i, slotVis.transform, new Vector3(sp.x, 0.045f, sp.z), gridStepX * 0.86f, gridStepZ * 0.86f, slotC, 0.05f);
                    slotList.Add(new SlotInfo { pos = sp, occupant = null });
                }
                return;
            }

            GameObject slots = new GameObject("Slots");
            slots.transform.SetParent(root, false);
            float gap = 1.65f;
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * gap;
                Pad("Slot_" + i, slots.transform, new Vector3(x, 0.04f, z), 1.45f, 1.25f, C_CREAM, 0.10f);
                // ince cerceve hissi icin koyu pad alt
                Pad("SlotRim_" + i, slots.transform, new Vector3(x, 0.02f, z), 1.62f, 1.42f, C_PAD, 0.06f);
                slotList.Add(new SlotInfo { pos = new Vector3(x, TruckGroundY, z), occupant = null }); // zemin seviyesi (tir park eder)
            }
        }

        // ---------- ALT: park alani + karisik oklu tirlar ----------
        void BuildParking(float centerZ)
        {
            // Atanmis parkingArea varsa grid merkezi ondan (gorsel zemin sahnede); yoksa eski runtime zemin.
            bool useAnchor = parkingArea != null;
            parkingOrigin = useAnchor
                ? new Vector3(parkingArea.position.x, TruckGroundY, parkingArea.position.z)
                : new Vector3(0f, TruckGroundY, centerZ);

            // NOTCH (gorunmez) cikislari ELE: kapi+kesik olmayan yerden sepet cikmasin (solid duvardan cikiyor gibiydi).
            // activeLevel.exits'e DOKUNMUYORUZ (level verisi kalici); sadece runtime activeExits'ten cikar.
            if (activeExits != null && activeExits.Length > 0)
            {
                var visExits = new List<ArrowsPixelExitGate>();
                foreach (var g in activeExits)
                    if (g != null)
                    {
                        Vector3 d = ExitDirectionVector(g.direction);
                        if (!IsNotchExit(g.x, g.z, Mathf.RoundToInt(d.x), Mathf.RoundToInt(d.z))) visExits.Add(g);
                    }
                if (visExits.Count == 0) // hepsi notch idi -> level kilitlenmesin diye en az 1 GECERLI (notch olmayan) cikis ekle
                {
                    int egx, egz, edx, edz;
                    if (FindNewExitEdge(out egx, out egz, out edx, out edz))
                        visExits.Add(new ArrowsPixelExitGate { x = egx, z = egz, direction = VectorToExitDirection(edx, edz) });
                }
                activeExits = visExits.ToArray();
            }

            GameObject park = new GameObject("Parking");
            park.transform.SetParent(root, false);
            parkTransform = park.transform;
            float parkSizeX = ParkingSizeX();
            float parkSizeZ = ParkingSizeZ();
            // Statik krem zemini gizle (cirkin tan margin gitsin); temiz board kuruyoruz
            if (useAnchor && parkingArea != null)
            {
                Transform pg = parkingArea.Find("ParkGround");
                if (pg != null) pg.gameObject.SetActive(false);
                // Eski duz pembe duvarlar + statik kapi pad'i gizlenir; yerine modÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¼ler yumusak duvar kurulur
                foreach (Transform pc in parkingArea)
                    if (pc.name.StartsWith("ParkWall") || pc.name == "ExitGate") pc.gameObject.SetActive(false);
            }
            else
            {
                BuildExitGateVisuals(park.transform);
            }

            // BOARD: yuvarlak koseli cartoon tepsi (zemin; Beads Out referansi)
            // cellMask varsa taban HUCRE HUCRE kurulur -> T/L formunu takip eder (duvarlar zaten takip ediyor)
            if (!HasCellMask())
            {
                float boardX = (gridWidth - 1) * gridStepX + gridStepX + 0.28f;
                float boardZ = (gridHeight - 1) * gridStepZ + gridStepZ + 0.28f;
                RoundedPad("BoardBase", park.transform, new Vector3(parkingOrigin.x, -0.05f, parkingOrigin.z), boardX, boardZ, areaColor, 0.32f, 0.32f);
            }

            // GRID: her (var olan) hucrede yuvarlak koseli koyu socket (cartoon recess)
            for (int sgx = 0; sgx < gridWidth; sgx++)
                for (int sgz = 0; sgz < gridHeight; sgz++)
                {
                    if (!CellExists(sgx, sgz)) continue;
                    Vector3 cp = CellToWorld(sgx, sgz);
                    if (HasCellMask())
                    {
                        Vector3 bp = cp; bp.y = -0.05f; // asagi uzar -> kalin yukseltilmis platform (havuz dibi degil)
                        RoundedPad("BoardBase_" + sgx + "_" + sgz, park.transform, bp, gridStepX + 0.02f, gridStepZ + 0.02f, areaColor, 0.32f, 0.10f);
                    }
                    cp.y = 0.095f;
                    Pad("Cell_" + sgx + "_" + sgz, park.transform, cp, gridStepX * 0.86f, gridStepZ * 0.86f, areaDarkColor, 0.05f);
                }

            // Duvar/cerceve: eski moduler cok-parca board tasarimi sabit tutulur.
            if (useAnchor)
            {
                BuildModularWalls(park.transform);
            }

            ArrowsPixelTruckSpawn[] defs = activeLevel != null ? activeLevel.trucks : null;
            if (defs == null || defs.Length == 0) defs = ArrowsPixelLevelLibrary.CreateGeneratedLevel(1).trucks;
            foreach (var d in defs)
            {
                if (d == null || !CellExists(d.x, d.z)) continue; // sinir + cellMask kontrolu
                BuildTruck(park.transform, CellToWorld(d.x, d.z), d.yaw, d.color, d.capacity, d.x, d.z);
            }
        }

        float ParkingSizeX()
        {
            return Mathf.Max(3.80f, (gridWidth - 1) * gridStepX + 1.50f);
        }

        float ParkingSizeZ()
        {
            return Mathf.Max(4.25f, (gridHeight - 1) * gridStepZ + 1.35f);
        }

        void BuildExitGateVisuals(Transform parent)
        {
            if (activeExits == null) return;
            for (int i = 0; i < activeExits.Length; i++)
            {
                ArrowsPixelExitGate gate = activeExits[i];
                if (gate == null) continue;
                Vector3 dir = ExitDirectionVector(gate.direction);
                float offset = Mathf.Abs(dir.x) > 0f ? gridStepX * 0.62f : gridStepZ * 0.62f;
                Vector3 pos = CellToWorld(gate.x, gate.z) + dir * offset + new Vector3(0f, -0.01f, 0f);
                bool horizontal = Mathf.Abs(dir.z) > 0f;
                Pad("ExitGate_" + i, parent, pos, horizontal ? 1.20f : 0.42f, horizontal ? 0.42f : 1.20f, C_CREAM, 0.08f);
            }
        }

        void BuildParkingWalls(Transform parent, float sx, float sz)
        {
            Color wall = new Color(0.72f, 0.74f, 0.84f); // mavi-gri, acik mavi bg ile uyumlu
            const float wallH = 0.34f;
            const float wallT = 0.14f;
            float halfX = sx * 0.5f;
            float halfZ = sz * 0.5f;

            var topGaps = new List<float>();
            var bottomGaps = new List<float>();
            var leftGaps = new List<float>();
            var rightGaps = new List<float>();
            if (activeExits != null)
            {
                for (int i = 0; i < activeExits.Length; i++)
                {
                    ArrowsPixelExitGate gate = activeExits[i];
                    if (gate == null) continue;
                    Vector3 p = CellToWorld(gate.x, gate.z);
                    if (gate.direction == ArrowsPixelExitDirection.Up) topGaps.Add(p.x);
                    else if (gate.direction == ArrowsPixelExitDirection.Down) bottomGaps.Add(p.x);
                    else if (gate.direction == ArrowsPixelExitDirection.Left) leftGaps.Add(p.z);
                    else if (gate.direction == ArrowsPixelExitDirection.Right) rightGaps.Add(p.z);
                }
            }

            BuildHorizontalWallSegments(parent, "ParkWall_Top", parkingOrigin.z + halfZ - wallT * 0.5f, sx, wallT, wallH, topGaps, 1.35f, wall);
            BuildHorizontalWallSegments(parent, "ParkWall_Bottom", parkingOrigin.z - halfZ + wallT * 0.5f, sx, wallT, wallH, bottomGaps, 1.35f, wall);
            BuildVerticalWallSegments(parent, "ParkWall_Left", parkingOrigin.x - halfX + wallT * 0.5f, sz, wallT, wallH, leftGaps, 1.35f, wall);
            BuildVerticalWallSegments(parent, "ParkWall_Right", parkingOrigin.x + halfX - wallT * 0.5f, sz, wallT, wallH, rightGaps, 1.35f, wall);
        }

        void BuildHorizontalWallSegments(Transform parent, string name, float z, float totalX, float wallT, float wallH, List<float> gapCenters, float gapSize, Color wall)
        {
            float min = parkingOrigin.x - totalX * 0.5f;
            float max = parkingOrigin.x + totalX * 0.5f;
            var segments = BuildSegments(min, max, gapCenters, gapSize);
            for (int i = 0; i < segments.Count; i++)
            {
                Vector2 s = segments[i];
                float len = s.y - s.x;
                if (len <= 0.05f) continue;
                Box(name + "_" + i, parent, new Vector3((s.x + s.y) * 0.5f, 0.20f, z), new Vector3(len, wallH, wallT), wall);
            }
        }

        // ===== MODULER YUMUSAK DUVAR (Beads Out tarzi) =====
        // Her sinir hucre kenarina yuvarlak uclu pill segment koyar; grid hangi formda olursa olsun
        // (dikdortgen / T / L) duvar kenari otomatik takip eder. Cikis kenarina duvar yerine kapi gelir.
        // Duvar pastel candy renkleri (soft -> sepetler one cikar). Segment basina dondurulur.
        int wallSegIdx = 0;
        Color WallPastelColor(int i)
        {
            // TEK RENK (coklu renk kaldirildi): bg pembe-lavanta ile uyumlu ama zeminden AYRISAN koyu/doygun mor cerceve.
            // (Acik lavanta zemin + acik sepetler ile karismasin diye belirgin koyu mor.)
            return new Color(0.58f, 0.47f, 0.82f);
        }

        void BuildModularWalls(Transform parent)
        {
            Color wallC = wallColor; // Inspector (moduler duvar; gate post icin kullanilir)
            wallSegIdx = 0;          // canli candy parca renk sayacini sifirla
            float t = 0.26f, wh = 0.32f, wy = 0.02f; // wh: duvar yuksekligi; enine kalinlik ayni, sadece daha basik/yatik durur
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                {
                    if (!CellExists(gx, gz)) continue;
                    TryWallEdge(parent, gx, gz, 1, 0, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, -1, 0, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, 0, 1, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, 0, -1, wallC, t, wh, wy);
                    // CONVEX koselere kapak (perpendicular duvar bindirmesini TEK renkle ortbas -> ic ice gorunmez)
                    TryWallCorner(parent, gx, gz, 1, 1, t, wh, wy);
                    TryWallCorner(parent, gx, gz, 1, -1, t, wh, wy);
                    TryWallCorner(parent, gx, gz, -1, 1, t, wh, wy);
                    TryWallCorner(parent, gx, gz, -1, -1, t, wh, wy);
                }
        }

        // Hucre var mi? Sinir + opsiyonel cellMask (T/L formlar). Maske satirlari USTTEN alta yazilir.
        bool CellExists(int gx, int gz)
        {
            if (gx < 0 || gx >= gridWidth || gz < 0 || gz >= gridHeight) return false;
            string[] mask = activeLevel != null ? activeLevel.cellMask : null;
            if (mask == null || mask.Length == 0) return true;
            int row = gridHeight - 1 - gz; // mask[0] = en ust satir (gz = gridHeight-1)
            if (row < 0 || row >= mask.Length) return true;
            string line = mask[row];
            if (string.IsNullOrEmpty(line) || gx >= line.Length) return true;
            return line[gx] != '.';
        }

        bool HasCellMask()
        {
            return activeLevel != null && activeLevel.cellMask != null && activeLevel.cellMask.Length > 0;
        }

        void TryWallEdge(Transform parent, int gx, int gz, int dx, int dz, Color c, float t, float wh, float wy)
        {
            if (CellExists(gx + dx, gz + dz)) return; // ic kenar -> duvar yok
            Vector3 dir = new Vector3(dx, 0f, dz);
            Vector3 center = CellToWorld(gx, gz) + dir * ((dx != 0 ? gridStepX : gridStepZ) * 0.5f + t * 0.5f);
            center.y = wy;
            if (IsExitEdge(gx, gz, dx, dz)) { BuildGateMarker(parent, center, dir, t); return; }
            float len = (dx != 0 ? gridStepZ : gridStepX); // duvar TAM hucre sinirinda biter, OTESINE GECMEZ (uzama YOK); kose bosluklarini KAPAK doldurur
            float sx = dx != 0 ? t : len;
            float sz = dx != 0 ? len : t;
            float radius = Mathf.Min(sx, sz) * 0.49f; // pill/oval uc: sert blok hissini kaldirir
            int wkey = (dx != 0) ? (100 + gx * 2 + (dx > 0 ? 1 : 0)) : (gz * 2 + (dz > 0 ? 1 : 0)); // collinear AYNI renk
            RoundedPad("Wall_" + gx + "_" + gz + "_" + dx + "_" + dz, parent, center,
                       sx, sz, WallPastelColor(wkey), wh, radius);
        }

        // CONVEX kose KAPAGI: hem (cdx,0) hem (0,cdz) kenar DIS ise, kosedeki perpendicular duvar bindirmesini
        // TEK renkle (yatay kenar rengi) USTTEN orter -> "ic ice / renk tasmasi" gorunmez, uc uca temiz durur.
        void TryWallCorner(Transform parent, int gx, int gz, int cdx, int cdz, float t, float wh, float wy)
        {
            if (CellExists(gx + cdx, gz) || CellExists(gx, gz + cdz)) return; // convex kose degil
            if (IsExitEdge(gx, gz, cdx, 0) || IsExitEdge(gx, gz, 0, cdz)) return; // kapi kenarinda kose KOYMA
            Vector3 cc = CellToWorld(gx, gz) + new Vector3(cdx * (gridStepX * 0.5f + t * 0.5f), 0f, cdz * (gridStepZ * 0.5f + t * 0.5f));
            cc.y = wy + 0.012f; // duvarlarin USTUNDE -> kose noktasini garanti orter
            int wkey = gz * 2 + (cdz > 0 ? 1 : 0); // yatay kenar rengi -> kose o renkte (tutarli)
            float cornerSize = t * 1.08f;
            RoundedPad("WallCorner_" + gx + "_" + gz + "_" + cdx + "_" + cdz, parent, cc, cornerSize, cornerSize, WallPastelColor(wkey), wh, cornerSize * 0.49f);
        }

        static bool ApproxXZ(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 1e-4f; }

        // ===================== TEK-MESH BOARD TEPSISI =====================
        // Cok-parca duvar/zemin yerine: hucre dis-hattini takip eden TEK pÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¼rÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¼zsÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¼z mesh.
        // - Yuvarlatilmis (oval/organik) koseler -> tek silÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¼et (dikis/kose sorunu YOK)
        // - Yukseltilmis kenar + cukur taban -> derinlik (havuz dibi degil)
        // - Cikis kenarlarinda kenar ALCALIR (esik) -> sepet temiz kayar, delik olusmaz
        void BuildBoardTray(Transform parent, bool withGates = true)
        {
            // --- ayar (yukseklikler dunya-Y; XZ parkingOrigin'e gore lokal) ---
            float rimTopY = 0.18f;     // yukseltilmis kenarin ust seviyesi
            float floorY = 0.05f;      // cukur taban (sepetler burada oturur)
            float bottomY = -0.13f;    // tepsi dibi (asagi -> kalin, yukseltilmis platform)
            float rimW = 0.17f;        // kenar (rim) genisligi
            float floorMargin = 0.12f; // cell sinirindan DISA bosluk -> kenar cell'leri YARIM kalmaz, rim cell'lerin DISINDA
            float corner = 0.24f;      // kose yuvarlatma yaricapi (oval/organik)
            float sillY = floorY + 0.03f; // cikis esigi (sepet bunun uzerinden kayar)
            int arcN = 5;              // kose yay alt-bolum
            Color trayCol = new Color(0.58f, 0.47f, 0.83f); // cerceve/rim MOR (bg ile uyumlu)
            Color floorCol = new Color(0.42f, 0.34f, 0.62f); // TABAN ayri (koyu) -> ic ile rim ayrisir, acik hucreler one cikar

            // 1) Sinir kenarlari (CCW: ic SOLDA), exit bayragiyla
            float hx = gridStepX * 0.5f, hz = gridStepZ * 0.5f;
            var sA = new System.Collections.Generic.List<Vector2>();
            var sB = new System.Collections.Generic.List<Vector2>();
            var sE = new System.Collections.Generic.List<bool>();
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                {
                    if (!CellExists(gx, gz)) continue;
                    Vector3 c = CellToWorld(gx, gz) - parkingOrigin;
                    float cx = c.x, cz = c.z;
                    // sE = GORUNUR cikis (exit AND notch DEGIL) -> mesh boslugu sadece kapinin oldugu yerde acilir (notch'ta kapi gizli, bosluk da olmasin)
                    if (!CellExists(gx + 1, gz)) { sA.Add(new Vector2(cx + hx, cz - hz)); sB.Add(new Vector2(cx + hx, cz + hz)); sE.Add(IsExitEdge(gx, gz, 1, 0) && !IsNotchExit(gx, gz, 1, 0)); }
                    if (!CellExists(gx - 1, gz)) { sA.Add(new Vector2(cx - hx, cz + hz)); sB.Add(new Vector2(cx - hx, cz - hz)); sE.Add(IsExitEdge(gx, gz, -1, 0) && !IsNotchExit(gx, gz, -1, 0)); }
                    if (!CellExists(gx, gz + 1)) { sA.Add(new Vector2(cx + hx, cz + hz)); sB.Add(new Vector2(cx - hx, cz + hz)); sE.Add(IsExitEdge(gx, gz, 0, 1) && !IsNotchExit(gx, gz, 0, 1)); }
                    if (!CellExists(gx, gz - 1)) { sA.Add(new Vector2(cx - hx, cz - hz)); sB.Add(new Vector2(cx + hx, cz - hz)); sE.Add(IsExitEdge(gx, gz, 0, -1) && !IsNotchExit(gx, gz, 0, -1)); }
                }
            int sc = sA.Count;
            if (sc < 3) { Debug.Log("[BoardTray] FAIL sc<3 = " + sc); return; }

            // 2) Zincirle -> sirali kose dongusu + her kenar(edge) exit mi
            var pts = new System.Collections.Generic.List<Vector2>();
            var ptE = new System.Collections.Generic.List<bool>();
            var used = new bool[sc];
            pts.Add(sA[0]); ptE.Add(sE[0]); used[0] = true;
            Vector2 end = sB[0];
            while (!ApproxXZ(end, pts[0]) && pts.Count <= sc)
            {
                int nxt = -1;
                for (int k = 0; k < sc; k++) if (!used[k] && ApproxXZ(sA[k], end)) { nxt = k; break; }
                if (nxt < 0) break;
                used[nxt] = true;
                pts.Add(sA[nxt]); ptE.Add(sE[nxt]);
                end = sB[nxt];
            }
            int M0 = pts.Count;
            if (M0 < 3) { Debug.Log("[BoardTray] FAIL M0<3 = " + M0); return; }

            // 3) Koseleri yumusat (quadratic bezier) -> Po + her edge exit bayragi
            var Po = new System.Collections.Generic.List<Vector2>();
            var PoE = new System.Collections.Generic.List<bool>();
            for (int i = 0; i < M0; i++)
            {
                Vector2 prev = pts[(i - 1 + M0) % M0], cc = pts[i], nx = pts[(i + 1) % M0];
                Vector2 dIn = cc - prev; float lIn = dIn.magnitude; if (lIn > 1e-5f) dIn /= lIn;
                Vector2 dOut = nx - cc; float lOut = dOut.magnitude; if (lOut > 1e-5f) dOut /= lOut;
                float rr = Mathf.Min(corner, lIn * 0.49f, lOut * 0.49f);
                Vector2 pIn = cc - dIn * rr, pOut = cc + dOut * rr;
                for (int s = 0; s <= arcN; s++)
                {
                    float t = s / (float)arcN;
                    Vector2 p = (1 - t) * (1 - t) * pIn + 2 * (1 - t) * t * cc + t * t * pOut;
                    Po.Add(p);
                    PoE.Add(s < arcN ? false : ptE[i]); // yay-ici kenar duvar; son nokta -> duz segment (exit bayragi)
                }
            }
            int M = Po.Count;

            // Kapi acikligini KAPI BOYU kadar GENISLET: cikis duz-kenarinin yanindaki yay kenarlarini da bosluga kat (yoksa kesik cok kisa kalir)
            int gateSpread = 2;
            var PoEw = new bool[M];
            for (int k = 0; k < M; k++)
                if (PoE[k])
                    for (int s = -gateSpread; s <= gateSpread; s++)
                        PoEw[((k + s) % M + M) % M] = true;

            // 4) Ic cevre (rim inseti) + dis kenar
            Vector2 ctr = Vector2.zero; for (int k = 0; k < M; k++) ctr += Po[k]; ctr /= M;
            var Pout = new Vector2[M]; // DIS kenar (cell sinirinin disinda + rim)
            var Pi = new Vector2[M];   // ic rim kenari (cell'lerin hemen disinda -> cell'ler tam tabanda)
            for (int k = 0; k < M; k++)
            {
                Vector2 a = Po[(k - 1 + M) % M], b = Po[k], d = Po[(k + 1) % M];
                Vector2 e1 = (b - a).normalized, e2 = (d - b).normalized;
                Vector2 outN = new Vector2(e1.y, -e1.x) + new Vector2(e2.y, -e2.x); // DISA normal (CCW) -> tepsiyi cell'lerin DISINA buyut
                if (outN.sqrMagnitude > 1e-6f) outN.Normalize();
                Pout[k] = b + outN * (floorMargin + rimW); // dis kenar
                Pi[k] = b + outN * floorMargin;            // ic rim kenari = cell sinirinin biraz disi (uniform rim)
            }

            // 5) Mesh: rim + GERCEK KAPI BOSLUGU (cikis kenarinda rim YOK -> taban disa uzar = doorway, kart oraya oturur)
            var v = new System.Collections.Generic.List<Vector3>();
            int OB  = 0;     for (int k = 0; k < M; k++) v.Add(new Vector3(Pout[k].x, bottomY, Pout[k].y));
            int OT  = M;     for (int k = 0; k < M; k++) v.Add(new Vector3(Pout[k].x, rimTopY, Pout[k].y));
            int IT  = 2 * M; for (int k = 0; k < M; k++) v.Add(new Vector3(Pi[k].x,   rimTopY, Pi[k].y));
            int IFl = 3 * M; for (int k = 0; k < M; k++) v.Add(new Vector3(Pi[k].x,   floorY,  Pi[k].y));
            int OFl = 4 * M; for (int k = 0; k < M; k++) v.Add(new Vector3(Pout[k].x, floorY,  Pout[k].y)); // doorway tabani (dis kenar floor seviyesinde)
            var tris = new System.Collections.Generic.List<int>();       // submesh 0 = CERCEVE
            var floorTris = new System.Collections.Generic.List<int>();  // submesh 1 = TABAN (ayri renk)
            for (int k = 0; k < M; k++)
            {
                int j = (k + 1) % M;
                if (PoEw[k]) // CIKIS kenari (genisletilmis) -> KAPI BOSLUGU: rim YOK, alcak dis duvar + taban disa uzar
                {
                    tris.Add(OB + k);  tris.Add(OFl + k); tris.Add(OFl + j);  // alcak dis duvar (bottom->floor)
                    tris.Add(OB + k);  tris.Add(OFl + j); tris.Add(OB + j);
                    floorTris.Add(OFl + k); floorTris.Add(OFl + j); floorTris.Add(IFl + j);  // taban uzantisi (TABAN submesh) = acikligin tabani
                    floorTris.Add(OFl + k); floorTris.Add(IFl + j); floorTris.Add(IFl + k);
                }
                else
                {
                    tris.Add(OB + k); tris.Add(OT + k); tris.Add(OT + j);   // dis duvar
                    tris.Add(OB + k); tris.Add(OT + j); tris.Add(OB + j);
                    tris.Add(OT + k); tris.Add(OT + j); tris.Add(IT + j);   // ust kenar bandi
                    tris.Add(OT + k); tris.Add(IT + j); tris.Add(IT + k);
                    tris.Add(IT + k); tris.Add(IFl + k); tris.Add(IFl + j); // ic duvar (cukura)
                    tris.Add(IT + k); tris.Add(IFl + j); tris.Add(IT + j);
                }
                if (PoEw[k] != PoEw[(k - 1 + M) % M]) // rim<->bosluk gecisi -> rim kesitini KAPAT (delik olmasin), cift yonlu
                {
                    tris.Add(OT + k); tris.Add(IT + k);  tris.Add(IFl + k);
                    tris.Add(OT + k); tris.Add(IFl + k); tris.Add(OFl + k);
                    tris.Add(OT + k); tris.Add(IFl + k); tris.Add(IT + k);
                    tris.Add(OT + k); tris.Add(OFl + k); tris.Add(IFl + k);
                }
            }
            // Ana taban artik polygon fan degil, hucre hucre quad: icbukey boardlarda arkaya uzayan ucgen artefakti olusmaz.
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                {
                    if (!CellExists(gx, gz)) continue;
                    Vector3 c = CellToWorld(gx, gz) - parkingOrigin;
                    float x0 = c.x - hx, x1 = c.x + hx;
                    float z0 = c.z - hz, z1 = c.z + hz;
                    if (!CellExists(gx - 1, gz)) x0 -= floorMargin;
                    if (!CellExists(gx + 1, gz)) x1 += floorMargin;
                    if (!CellExists(gx, gz - 1)) z0 -= floorMargin;
                    if (!CellExists(gx, gz + 1)) z1 += floorMargin;
                    int f = v.Count;
                    v.Add(new Vector3(x0, floorY, z0));
                    v.Add(new Vector3(x0, floorY, z1));
                    v.Add(new Vector3(x1, floorY, z1));
                    v.Add(new Vector3(x1, floorY, z0));
                    floorTris.Add(f); floorTris.Add(f + 2); floorTris.Add(f + 1);
                    floorTris.Add(f); floorTris.Add(f + 3); floorTris.Add(f + 2);
                }
            int bc = v.Count; v.Add(new Vector3(ctr.x, bottomY, ctr.y));
            for (int k = 0; k < M; k++) { int j = (k + 1) % M; tris.Add(bc); tris.Add(OB + j); tris.Add(OB + k); } // dip
            for (int fi = 0; fi < tris.Count; fi += 3) { int tmp = tris[fi + 1]; tris[fi + 1] = tris[fi + 2]; tris[fi + 2] = tmp; } // sarimi ters cevir -> normaller dogru
            for (int fi = 0; fi < floorTris.Count; fi += 3) { int tmp = floorTris[fi + 1]; floorTris[fi + 1] = floorTris[fi + 2]; floorTris[fi + 2] = tmp; }
            Mesh mesh = new Mesh { name = "BoardTray" };
            mesh.SetVertices(v); mesh.subMeshCount = 2; mesh.SetTriangles(tris, 0); mesh.SetTriangles(floorTris, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            GameObject go = new GameObject("BoardTray");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(parkingOrigin.x, 0f, parkingOrigin.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Material tm = new Material(Mat(trayCol));
            if (tm.HasProperty("_OutlineWidth")) tm.SetFloat("_OutlineWidth", 0f); // KALIN siyah outline ince cerceveyi kapliyordu -> kaldir (temiz parlak)
            if (tm.HasProperty("_RimStrength")) tm.SetFloat("_RimStrength", 0f);             // kenar Fresnel BEYAZ CIZGILERINI kaldir (kullanici istegi)
            if (tm.HasProperty("_HighlightStrength")) tm.SetFloat("_HighlightStrength", 0f); // spekuler parlama lekelerini de kaldir
            if (tm.HasProperty("_ShadeStrength")) tm.SetFloat("_ShadeStrength", 0f);         // DUZ golgeleme -> ust yuzey ile yanlar AYNI renk (ust kenardaki acik mor bant gider)
            Material fm = new Material(Mat(floorCol)); // TABAN materyali (ayri KOYU renk -> ic ile rim ayrisir)
            if (fm.HasProperty("_OutlineWidth")) fm.SetFloat("_OutlineWidth", 0f);
            if (fm.HasProperty("_RimStrength")) fm.SetFloat("_RimStrength", 0f);
            if (fm.HasProperty("_HighlightStrength")) fm.SetFloat("_HighlightStrength", 0f);
            if (fm.HasProperty("_ShadeStrength")) fm.SetFloat("_ShadeStrength", 0f);
            if (tm.HasProperty("_Glossiness")) tm.SetFloat("_Glossiness", 0.08f);
            if (tm.HasProperty("_Smoothness")) tm.SetFloat("_Smoothness", 0.08f);
            if (tm.HasProperty("_Metallic")) tm.SetFloat("_Metallic", 0f);
            if (fm.HasProperty("_Glossiness")) fm.SetFloat("_Glossiness", 0.08f);
            if (fm.HasProperty("_Smoothness")) fm.SetFloat("_Smoothness", 0.08f);
            if (fm.HasProperty("_Metallic")) fm.SetFloat("_Metallic", 0f);
            var boardRenderer = go.AddComponent<MeshRenderer>();
            boardRenderer.sharedMaterials = new Material[] { tm, fm }; // 0=cerceve, 1=taban

            // 6) Cikis ok isaretleri (duvar yok ama yon belli olsun). withGates=false -> sadece mesh tazele (booster rebuild)
            if (withGates)
            {
                float gt = 0.26f;
                float gateOut = floorMargin + rimW; // tepsi cell DISINA buyudu -> kapi da o kadar disa (rim ustunde/icinde kalmasin)
                for (int gx = 0; gx < gridWidth; gx++)
                    for (int gz = 0; gz < gridHeight; gz++)
                    {
                        if (!CellExists(gx, gz)) continue;
                        TryGateOnly(parent, gx, gz, 1, 0, gt, sillY, gateOut);
                        TryGateOnly(parent, gx, gz, -1, 0, gt, sillY, gateOut);
                        TryGateOnly(parent, gx, gz, 0, 1, gt, sillY, gateOut);
                        TryGateOnly(parent, gx, gz, 0, -1, gt, sillY, gateOut);
                    }
            }
        }

        // Booster yeni cikis ekleyince: tepsi MESH'ini tazele (rim yeni cikista da alcalir), kapilara dokunma
        void RebuildBoardTrayMesh()
        {
            if (parkTransform == null || !useSingleMeshBoard) return;
            Transform old = parkTransform.Find("BoardTray");
            if (old != null) DestroyImmediate(old.gameObject);
            Transform oldShadow = parkTransform.Find("BoardTrayShadow");
            if (oldShadow != null) DestroyImmediate(oldShadow.gameObject);
            BuildBoardTray(parkTransform, false);
        }

        // Booster yeni kapisi: level-basi mantigiyla (dogru offset + notch-skip) + dogus animasyonu
        void AddExitGateAnimated(int gx, int gz, int dx, int dz)
        {
            if (parkTransform == null) return;
            bool notch = (dx != 0)
                ? (CellExists(gx + dx, gz + 1) || CellExists(gx + dx, gz - 1))
                : (CellExists(gx + 1, gz + dz) || CellExists(gx - 1, gz + dz));
            if (notch) return; // notch'sa isaret koyma (cikis yine calisir)
            float gt = 0.26f, gateOut = 0.12f + 0.17f; // = floorMargin+rimW (BuildBoardTray ile senkron)
            Vector3 dir = new Vector3(dx, 0f, dz);
            Vector3 center = CellToWorld(gx, gz) + dir * ((dx != 0 ? gridStepX : gridStepZ) * 0.5f + gateOut + gt * 0.5f);
            int before = parkTransform.childCount;
            BuildGateMarker(parkTransform, center, dir, gt);
            GameObject cont = new GameObject("GateSpawn");
            cont.transform.SetParent(parkTransform, false);
            cont.transform.position = new Vector3(center.x, 0f, center.z);
            for (int i = parkTransform.childCount - 1; i >= before; i--)
            {
                Transform child = parkTransform.GetChild(i);
                if (child == cont.transform) continue;
                child.SetParent(cont.transform, true);
            }
            StartCoroutine(GateSpawnAnim(cont.transform, dir));
        }

        void TryGateOnly(Transform parent, int gx, int gz, int dx, int dz, float t, float gateY, float outOff)
        {
            if (CellExists(gx + dx, gz + dz)) return;
            if (!IsExitEdge(gx, gz, dx, dz)) return;
            // IC-CEP (notch): cikis yonundeki YAN komsuda cell varsa, kapi kollar-arasi bosluga (board ICI gibi) duser -> isaret KOYMA (cikis yine calisir)
            bool notch = (dx != 0)
                ? (CellExists(gx + dx, gz + 1) || CellExists(gx + dx, gz - 1))
                : (CellExists(gx + 1, gz + dz) || CellExists(gx - 1, gz + dz));
            if (notch) return;
            Vector3 dir = new Vector3(dx, 0f, dz);
            Vector3 center = CellToWorld(gx, gz) + dir * ((dx != 0 ? gridStepX : gridStepZ) * 0.5f + outOff + t * 0.5f); // outOff: tepsi genislemesi -> kapi rim'in DISINDA durur
            center.y = gateY;
            BuildGateMarker(parent, center, dir, t);
        }

        // NOTCH (ic-cep) cikis: cikis yonundeki yan komsuda cell varsa -> kollar-arasi bosluga bakar.
        // Bu cikislarda kapi isareti KOYULMAZ (board ici gibi durur) -> mesh boslugu da olmasin.
        bool IsNotchExit(int gx, int gz, int dx, int dz)
        {
            return (dx != 0)
                ? (CellExists(gx + dx, gz + 1) || CellExists(gx + dx, gz - 1))
                : (CellExists(gx + 1, gz + dz) || CellExists(gx - 1, gz + dz));
        }

        bool IsExitEdge(int gx, int gz, int dx, int dz)
        {
            if (activeExits == null) return false;
            foreach (var g in activeExits)
            {
                if (g == null || g.x != gx || g.z != gz) continue;
                Vector3 d = ExitDirectionVector(g.direction);
                if (Mathf.RoundToInt(d.x) == dx && Mathf.RoundToInt(d.z) == dz) return true;
            }
            return false;
        }

        // Cikis kapisi: iki yan post + krem paspas + disari bakan sari chevron ok (yonu net gosterir)
        void BuildGateMarker(Transform parent, Vector3 center, Vector3 dir, float t)
        {
            Color matC   = new Color(0.99f, 0.93f, 0.78f);
            Color arrowC = new Color(1.00f, 0.78f, 0.25f);
            bool horiz = Mathf.Abs(dir.x) > 0.5f; // kapi sag/sol kenarda mi
            // Gri postlar KALDIRILDI (kullanici istegi) -> sadece ten rengi kart + sari ok.
            // Kart + ok board'a YAKIN (rim'e degsin): eski dir*0.40 -> 0.08
            Vector3 mp = center + dir * 0.08f; mp.y = 0.095f;
            RoundedPad("GateMat", parent, mp, horiz ? 0.46f : 0.95f, horiz ? 0.95f : 0.46f, matC, 0.05f, 0.14f);
            GameObject ar = new GameObject("GateArrow");
            ar.transform.SetParent(parent, false);
            Vector3 ap = center + dir * 0.10f; ap.y = 0.145f;
            ar.transform.localPosition = ap;
            ar.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            GameObject a1 = Box("ArmL", ar.transform, new Vector3(-0.07f, 0f, 0f), new Vector3(0.075f, 0.045f, 0.26f), arrowC);
            a1.transform.localRotation = Quaternion.Euler(0f, 41f, 0f);
            GameObject a2 = Box("ArmR", ar.transform, new Vector3(0.07f, 0f, 0f), new Vector3(0.075f, 0.045f, 0.26f), arrowC);
            a2.transform.localRotation = Quaternion.Euler(0f, -41f, 0f);
        }

        void BuildVerticalWallSegments(Transform parent, string name, float x, float totalZ, float wallT, float wallH, List<float> gapCenters, float gapSize, Color wall)
        {
            float min = parkingOrigin.z - totalZ * 0.5f;
            float max = parkingOrigin.z + totalZ * 0.5f;
            var segments = BuildSegments(min, max, gapCenters, gapSize);
            for (int i = 0; i < segments.Count; i++)
            {
                Vector2 s = segments[i];
                float len = s.y - s.x;
                if (len <= 0.05f) continue;
                Box(name + "_" + i, parent, new Vector3(x, 0.20f, (s.x + s.y) * 0.5f), new Vector3(wallT, wallH, len), wall);
            }
        }

        List<Vector2> BuildSegments(float min, float max, List<float> gapCenters, float gapSize)
        {
            var segments = new List<Vector2> { new Vector2(min, max) };
            if (gapCenters == null) return segments;

            for (int g = 0; g < gapCenters.Count; g++)
            {
                float gapMin = gapCenters[g] - gapSize * 0.5f;
                float gapMax = gapCenters[g] + gapSize * 0.5f;
                var next = new List<Vector2>();
                for (int i = 0; i < segments.Count; i++)
                {
                    Vector2 s = segments[i];
                    if (gapMax <= s.x || gapMin >= s.y)
                    {
                        next.Add(s);
                        continue;
                    }
                    if (gapMin > s.x) next.Add(new Vector2(s.x, Mathf.Clamp(gapMin, s.x, s.y)));
                    if (gapMax < s.y) next.Add(new Vector2(Mathf.Clamp(gapMax, s.x, s.y), s.y));
                }
                segments = next;
            }
            return segments;
        }

        Vector3 CellToWorld(int gx, int gz)
        {
            return parkingOrigin + new Vector3((gx - (gridWidth - 1) * 0.5f) * gridStepX, 0f, (gz - (gridHeight - 1) * 0.5f) * gridStepZ);
        }

        void BuildTruck(Transform parent, Vector3 pos, float yawDeg, CargoColor cargo, int capacity, int gx, int gz)
        {
            Color color = CargoColorPalette.ToColor(cargo);
            GameObject t = new GameObject("Truck");
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos;
            t.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            if (useBasketStyle)
            {
                BuildBasket(t.transform, color);
            }
            else if (useBlockStyle)
            {
                BuildBlock(t.transform, color);
            }
            else
            {
            if (truckPrefab != null)
            {
                GameObject body = Instantiate(truckPrefab, t.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // kabin -> +X (ok yonu ile ayni)
                body.transform.localScale = Vector3.one * truckModelScale;
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
            BuildCargoBed(t.transform, color);
            }

            // Faz 2: tiklama icin collider + kayit (headDir = local +X = ok/kafa yonu)
            var col = t.AddComponent<BoxCollider>();
            float colliderScale = Mathf.Clamp(truckModelScale / 0.68f, 0.70f, 1.15f);
            col.center = new Vector3(0f, 0.32f * colliderScale, 0f);
            col.size = new Vector3(1.55f, 0.75f, 1.05f) * colliderScale;
            trucks.Add(new TruckInfo { root = t.transform, headDir = t.transform.right, cargo = cargo, capacity = capacity, filled = 0, extracted = false, gx = gx, gz = gz });
        }

        void BuildCargoBed(Transform parent, Color color)
        {
            Color wall = Color.Lerp(color, C_CREAM, 0.48f);
            Color floor = Color.Lerp(color, new Color(0.12f, 0.08f, 0.14f), 0.18f);
            Box("CargoBedFloor", parent, new Vector3(-0.26f, 0.48f, 0f), new Vector3(0.92f, 0.055f, 0.70f), floor);
            Box("CargoBedLeftWall", parent, new Vector3(-0.26f, 0.61f, 0.37f), new Vector3(0.96f, 0.25f, 0.055f), wall);
            Box("CargoBedRightWall", parent, new Vector3(-0.26f, 0.61f, -0.37f), new Vector3(0.96f, 0.25f, 0.055f), wall);
            Box("CargoBedBackWall", parent, new Vector3(-0.74f, 0.61f, 0f), new Vector3(0.055f, 0.25f, 0.70f), wall);
            Box("CargoBedFrontLip", parent, new Vector3(0.22f, 0.57f, 0f), new Vector3(0.055f, 0.17f, 0.70f), wall);
        }

        // Color Block Jam tarzi studlu tek-hucre blok (tir gorseli yerine; useBlockStyle ile acilir)
        void BuildBlock(Transform parent, Color color)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material body = new Material(sh) { name = "BlockBody" };
            if (body.HasProperty("_Color")) body.SetColor("_Color", color);
            if (body.HasProperty("_BaseColor")) body.SetColor("_BaseColor", color);
            if (body.HasProperty("_OutlineColor")) body.SetColor("_OutlineColor", new Color(0.06f, 0.05f, 0.10f, 1f));
            if (body.HasProperty("_OutlineWidth")) body.SetFloat("_OutlineWidth", 0.022f);
            if (body.HasProperty("_ShadowColor")) body.SetColor("_ShadowColor", Color.Lerp(color, new Color(0.07f, 0.05f, 0.12f), 0.55f));
            if (body.HasProperty("_RampThreshold")) body.SetFloat("_RampThreshold", 0.55f);
            if (body.HasProperty("_ShadeStrength")) body.SetFloat("_ShadeStrength", 0.50f);
            if (body.HasProperty("_RimColor")) body.SetColor("_RimColor", new Color(1f, 0.96f, 0.86f));
            if (body.HasProperty("_RimStrength")) body.SetFloat("_RimStrength", 0.28f);
            if (body.HasProperty("_HighlightColor")) body.SetColor("_HighlightColor", new Color(1f, 0.99f, 0.92f));
            if (body.HasProperty("_HighlightStrength")) body.SetFloat("_HighlightStrength", 0.78f);

            Material stud = new Material(body) { name = "BlockStud" };
            if (stud.HasProperty("_Color")) stud.SetColor("_Color", Color.Lerp(color, Color.white, 0.18f));
            if (stud.HasProperty("_OutlineWidth")) stud.SetFloat("_OutlineWidth", 0.012f);

            float w = Mathf.Min(gridStepX, gridStepZ) * 0.82f;
            float h = 0.52f;

            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "BlockCell";
            DestroyImmediate(b.GetComponent<Collider>());
            b.transform.SetParent(parent, false);
            b.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            b.transform.localScale = new Vector3(w, h, w);
            b.GetComponent<Renderer>().sharedMaterial = body;

            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            s.name = "BlockStudTop";
            DestroyImmediate(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false);
            s.transform.localPosition = new Vector3(0f, h + 0.04f, 0f);
            s.transform.localScale = new Vector3(w * 0.34f, 0.07f, w * 0.34f);
            s.GetComponent<Renderer>().sharedMaterial = stud;
        }

        // Color Block Jam / Marble Sort tarzi toon SEPET (kupler icine dolar; useBasketStyle ile acilir)
        void BuildBasket(Transform parent, Color color)
        {
            color = Vivid(color); // canli renk (cartoon)
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh) { name = "BasketBody" };
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", toonOutlineColor);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", basketOutline); // govde TEK-MESH -> outline TEMIZ/butun cikar (kesik yok)
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", Color.Lerp(color, new Color(0.07f, 0.05f, 0.12f), 0.30f)); // golge HAFIF tint (yuvarlak kose KOYU olmasin)
            if (m.HasProperty("_RampThreshold")) m.SetFloat("_RampThreshold", toonRamp);
            if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", 0.50f); // sepet golgesi yumusak -> kose koyulugu gider
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(1f, 0.96f, 0.86f));
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.28f);
            if (m.HasProperty("_HighlightColor")) m.SetColor("_HighlightColor", new Color(1f, 0.99f, 0.92f));
            if (m.HasProperty("_HighlightStrength")) m.SetFloat("_HighlightStrength", 0.88f); // hafif daha parlak (premium cila), renk degismez

            // Olculer (t/fh/h: alt bant icin korunur)
            float Ex = gridStepX * 0.49f * basketFootprint;
            float Ez = gridStepZ * 0.49f * basketFootprint;
            float cm = Mathf.Min(gridStepX, gridStepZ);
            float h = cm * 0.46f * basketHeightScale, t = cm * 0.065f, fh = cm * 0.09f;
            float bodyH = fh + h;                                    // toplam sepet yuksekligi
            float r = Mathf.Min(Ex, Ez) * basketCornerRound;        // kose yuvarlagi (Inspector: ovallik)

            Material inner = new Material(m) { name = "BasketInner" };
            Color innerC = Color.Lerp(color, new Color(0.05f, 0.04f, 0.09f), 0.38f);
            if (inner.HasProperty("_Color")) inner.SetColor("_Color", innerC);
            if (inner.HasProperty("_BaseColor")) inner.SetColor("_BaseColor", innerC);
            if (inner.HasProperty("_ShadowColor")) inner.SetColor("_ShadowColor", Color.Lerp(innerC, Color.black, 0.58f));
            if (inner.HasProperty("_ShadeStrength")) inner.SetFloat("_ShadeStrength", 0.72f);
            if (inner.HasProperty("_RampThreshold")) inner.SetFloat("_RampThreshold", 0.42f);
            if (inner.HasProperty("_HighlightStrength")) inner.SetFloat("_HighlightStrength", 0.18f);
            if (inner.HasProperty("_RimStrength")) inner.SetFloat("_RimStrength", 0.04f);
            if (inner.HasProperty("_Glossiness")) inner.SetFloat("_Glossiness", 0.08f);
            if (inner.HasProperty("_Smoothness")) inner.SetFloat("_Smoothness", 0.08f);
            if (inner.HasProperty("_OutlineWidth")) inner.SetFloat("_OutlineWidth", 0f);

            RoundedBoxMat("BasketBody", parent, new Vector3(0f, bodyH * 0.5f, 0f), 2f * Ex, 2f * Ez, bodyH, r, m);
            float inset = cm * 0.105f, innerH = cm * 0.12f;
            float innerR = Mathf.Max(0.01f, r - inset * 0.55f);
            RoundedBoxMat("BasketInner", parent, new Vector3(0f, bodyH - innerH * 0.5f + 0.004f, 0f), 2f * (Ex - inset), 2f * (Ez - inset), innerH, innerR, inner);

            // ALT TEN BANT (Beads Out referansi: sepetin oturdugu acik taban bandi).
            // Inspector'dan showBasketBaseBand ile kapatilabilir; KAPALI (false) = sepet TEK RENK/temiz gorunur.
            if (showBasketBaseBand)
            {
                Material baseM = new Material(m) { name = "BasketBase" };
                Color baseC = new Color(0.99f, 0.97f, 0.92f);
                if (baseM.HasProperty("_Color")) baseM.SetColor("_Color", baseC);
                if (baseM.HasProperty("_ShadowColor")) baseM.SetColor("_ShadowColor", new Color(0.74f, 0.72f, 0.78f));
                if (baseM.HasProperty("_OutlineWidth")) baseM.SetFloat("_OutlineWidth", 0.012f);
                float off = t * 0.35f;            // duvardan disari tasma
                float bh = (fh + h) * 0.34f;      // bant yuksekligi (sepetin alt ~1/3'u)
                float by = bh * 0.5f;
                float bt = t * 0.9f + off;        // ic yuzu duvarin icinde kalsin (z-fight olmasin)
                BasketCube(parent, baseM, new Vector3(Ex + off - bt * 0.5f, by, 0f), new Vector3(bt, bh, 2f * Ez - 2f * r));
                BasketCube(parent, baseM, new Vector3(-(Ex + off - bt * 0.5f), by, 0f), new Vector3(bt, bh, 2f * Ez - 2f * r));
                BasketCube(parent, baseM, new Vector3(0f, by, Ez + off - bt * 0.5f), new Vector3(2f * Ex - 2f * r, bh, bt));
                BasketCube(parent, baseM, new Vector3(0f, by, -(Ez + off - bt * 0.5f)), new Vector3(2f * Ex - 2f * r, bh, bt));
                BasketCorner(parent, baseM, new Vector3(Ex - r, by, Ez - r), r + off, bh);
                BasketCorner(parent, baseM, new Vector3(-(Ex - r), by, Ez - r), r + off, bh);
                BasketCorner(parent, baseM, new Vector3(Ex - r, by, -(Ez - r)), r + off, bh);
                BasketCorner(parent, baseM, new Vector3(-(Ex - r), by, -(Ez - r)), r + off, bh);
            }
        }

        // Yuvarlatilmis-dikdortgen cevre noktalari (CCW), 4*(steps+1) nokta
        System.Collections.Generic.List<Vector2> RoundedRectPerim(float hx, float hz, float r, int steps)
        {
            var P = new System.Collections.Generic.List<Vector2>();
            float[,] cs = { { hx - r, hz - r, 0f, 90f }, { -(hx - r), hz - r, 90f, 180f }, { -(hx - r), -(hz - r), 180f, 270f }, { hx - r, -(hz - r), 270f, 360f } };
            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= steps; s++)
                {
                    float a = Mathf.Lerp(cs[c, 2], cs[c, 3], s / (float)steps) * Mathf.Deg2Rad;
                    P.Add(new Vector2(cs[c, 0] + Mathf.Cos(a) * r, cs[c, 1] + Mathf.Sin(a) * r));
                }
            return P;
        }

        // SHELL: dis duvarlar + ust kenar bandi (delikli) + alt kapak. TEK mesh -> outline = dis silhouette + ince delik halkasi (kesiksiz).
        Mesh BuildBasketShellMesh(float sx, float sz, float h, float r, float rimW, int steps)
        {
            float hx = sx * 0.5f, hz = sz * 0.5f, hy = h * 0.5f;
            float ix = hx - rimW, iz = hz - rimW;
            r = Mathf.Clamp(r, 0.01f, Mathf.Min(hx, hz) - 0.01f);
            float ir = Mathf.Max(0.02f, r - rimW);
            var Po = RoundedRectPerim(hx, hz, r, steps);
            var Pi = RoundedRectPerim(ix, iz, ir, steps);
            int n = Po.Count;
            var v = new System.Collections.Generic.List<Vector3>();
            int OB = 0; for (int i = 0; i < n; i++) v.Add(new Vector3(Po[i].x, -hy, Po[i].y));
            int OT = n; for (int i = 0; i < n; i++) v.Add(new Vector3(Po[i].x, hy, Po[i].y));
            int IT = 2 * n; for (int i = 0; i < n; i++) v.Add(new Vector3(Pi[i].x, hy, Pi[i].y));
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                t.Add(OB + i); t.Add(OT + i); t.Add(OT + j);   // dis duvar
                t.Add(OB + i); t.Add(OT + j); t.Add(OB + j);
                t.Add(OT + i); t.Add(OT + j); t.Add(IT + j);   // ust kenar bandi (delikli)
                t.Add(OT + i); t.Add(IT + j); t.Add(IT + i);
            }
            int bc = v.Count; v.Add(new Vector3(0f, -hy, 0f));
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; t.Add(bc); t.Add(OB + j); t.Add(OB + i); }
            Mesh mesh = new Mesh { name = "BasketShell" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // WELL: ic cukur (ic duvarlar + taban), CIFT-YUZLU (culling'den bagimsiz gorunur), AYRI obje -> outline KAPALI (siyah dolmaz). Derinlik buradan.
        Mesh BuildBasketWellMesh(float sx, float sz, float r, float topY, float floorY, int steps)
        {
            float ix = sx * 0.5f, iz = sz * 0.5f;
            r = Mathf.Clamp(r, 0.01f, Mathf.Min(ix, iz) - 0.01f);
            var Pi = RoundedRectPerim(ix, iz, r, steps);
            int n = Pi.Count;
            var v = new System.Collections.Generic.List<Vector3>();
            int T = 0; for (int i = 0; i < n; i++) v.Add(new Vector3(Pi[i].x, topY, Pi[i].y));
            int F = n; for (int i = 0; i < n; i++) v.Add(new Vector3(Pi[i].x, floorY, Pi[i].y));
            int fc = v.Count; v.Add(new Vector3(0f, floorY, 0f));
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                t.Add(T + i); t.Add(F + i); t.Add(F + j); t.Add(T + i); t.Add(F + j); t.Add(T + j);   // ic duvar
                t.Add(T + i); t.Add(F + j); t.Add(F + i); t.Add(T + i); t.Add(T + j); t.Add(F + j);   // ters (cift-yuzlu)
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                t.Add(fc); t.Add(F + i); t.Add(F + j); t.Add(fc); t.Add(F + j); t.Add(F + i);          // taban (cift-yuzlu)
            }
            Mesh mesh = new Mesh { name = "BasketWell" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // TEK-MESH TRAY (recessed ust = derinlik). 2 submesh: 0=govde (outline), 1=ic cukur (outline'siz). Silhouette tek -> outline kesiksiz/ince.
        Mesh BuildBasketTrayMesh(float sx, float sz, float h, float r, float rimW, float recessD, int steps)
        {
            float hx = sx * 0.5f, hz = sz * 0.5f, hy = h * 0.5f;
            float ix = hx - rimW, iz = hz - rimW;
            r = Mathf.Clamp(r, 0.01f, Mathf.Min(hx, hz) - 0.01f);
            float ir = Mathf.Max(0.02f, r - rimW);
            recessD = Mathf.Clamp(recessD, 0.02f, h - 0.02f);
            float floorY = hy - recessD;
            var Po = RoundedRectPerim(hx, hz, r, steps);
            var Pi = RoundedRectPerim(ix, iz, ir, steps);
            int n = Po.Count;
            var v = new System.Collections.Generic.List<Vector3>();
            int OB = 0; for (int i = 0; i < n; i++) v.Add(new Vector3(Po[i].x, -hy, Po[i].y));     // dis alt
            int OT = n; for (int i = 0; i < n; i++) v.Add(new Vector3(Po[i].x, hy, Po[i].y));       // dis ust
            int IT = 2 * n; for (int i = 0; i < n; i++) v.Add(new Vector3(Pi[i].x, hy, Pi[i].y));   // ic ust (kenar)
            int IFl = 3 * n; for (int i = 0; i < n; i++) v.Add(new Vector3(Pi[i].x, floorY, Pi[i].y)); // ic taban
            var body = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                body.Add(OB + i); body.Add(OT + i); body.Add(OT + j);   // dis duvar (disa)
                body.Add(OB + i); body.Add(OT + j); body.Add(OB + j);
                body.Add(OT + i); body.Add(OT + j); body.Add(IT + j);   // ust kenar bandi (yukari)
                body.Add(OT + i); body.Add(IT + j); body.Add(IT + i);
                body.Add(IT + i); body.Add(IFl + i); body.Add(IFl + j); // ic duvar (ice bakar -> cukur)
                body.Add(IT + i); body.Add(IFl + j); body.Add(IT + j);
            }
            int bc = v.Count; v.Add(new Vector3(0f, -hy, 0f));
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; body.Add(bc); body.Add(OB + j); body.Add(OB + i); } // alt kapak
            int fc = v.Count; v.Add(new Vector3(0f, floorY, 0f));
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; body.Add(fc); body.Add(IFl + i); body.Add(IFl + j); } // ic taban (yukari)
            Mesh mesh = new Mesh { name = "BasketTray" };
            mesh.SetVertices(v);
            mesh.SetTriangles(body, 0);   // TEK submesh: kapali mesh -> outline sadece dis silhouette (ic-kenar artefakti yok), cukur gÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚ÂÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¶lgeyle derinlik
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // TEK MESH yuvarlak-kutu (yuvarlatilmis dikey koseler) -> tek temiz silhouette -> toon outline KESIKSIZ cikar
        Mesh BuildRoundedBoxMesh(float sx, float sz, float h, float r, int steps)
        {
            r = Mathf.Clamp(r, 0.001f, Mathf.Min(sx, sz) * 0.5f - 0.001f);
            float hx = sx * 0.5f, hz = sz * 0.5f, hy = h * 0.5f;
            System.Collections.Generic.List<Vector2> P = new System.Collections.Generic.List<Vector2>();
            float[,] cs = { { hx - r, hz - r, 0f, 90f }, { -(hx - r), hz - r, 90f, 180f }, { -(hx - r), -(hz - r), 180f, 270f }, { hx - r, -(hz - r), 270f, 360f } };
            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= steps; s++)
                {
                    float a = Mathf.Lerp(cs[c, 2], cs[c, 3], s / (float)steps) * Mathf.Deg2Rad;
                    P.Add(new Vector2(cs[c, 0] + Mathf.Cos(a) * r, cs[c, 1] + Mathf.Sin(a) * r));
                }
            int n = P.Count;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++) verts.Add(new Vector3(P[i].x, -hy, P[i].y)); // alt halka 0..n-1
            for (int i = 0; i < n; i++) verts.Add(new Vector3(P[i].x, hy, P[i].y));   // ust halka n..2n-1
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                tris.Add(i); tris.Add(i + n); tris.Add(j + n);   // yan duvar (disa bakar)
                tris.Add(i); tris.Add(j + n); tris.Add(j);
            }
            int topC = verts.Count; verts.Add(new Vector3(0f, hy, 0f));
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; tris.Add(topC); tris.Add(i + n); tris.Add(j + n); } // ust kapak
            int botC = verts.Count; verts.Add(new Vector3(0f, -hy, 0f));
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; tris.Add(botC); tris.Add(j); tris.Add(i); }         // alt kapak
            Mesh mesh = new Mesh { name = "RoundedBoxMesh" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // RoundedPad gibi ama malzeme disaridan + tam yukseklik: SOLID yuvarlak kutu (koseler flush, cikinti yok)
        void RoundedBoxMat(string name, Transform parent, Vector3 center, float sx, float sz, float h, float r, Material mat)
        {
            r = Mathf.Clamp(r, 0.001f, Mathf.Min(sx, sz) * 0.5f - 0.001f);
            GameObject p = new GameObject(name);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = center;
            AddBoxMat(p.transform, new Vector3(sx, h, sz - 2f * r), mat);
            AddBoxMat(p.transform, new Vector3(sx - 2f * r, h, sz), mat);
            for (int i = 0; i < 4; i++)
            {
                float ox = (i % 2 == 0 ? 1f : -1f) * (sx * 0.5f - r);
                float oz = (i < 2 ? 1f : -1f) * (sz * 0.5f - r);
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = "corner"; c.transform.SetParent(p.transform, false);
                c.transform.localPosition = new Vector3(ox, 0f, oz);
                c.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
                DestroyImmediate(c.GetComponent<Collider>());
                c.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        void AddBoxMat(Transform parent, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "box"; DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = Vector3.zero;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
        }

        void BasketCube(Transform parent, Material m, Vector3 pos, Vector3 scale)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "BasketPart";
            DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
        }

        void BasketCorner(Transform parent, Material m, Vector3 pos, float r, float h)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "BasketCorner";
            DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
            g.GetComponent<Renderer>().sharedMaterial = m;
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
            a.transform.localScale = Vector3.one * (0.62f * Mathf.Clamp(truckModelScale / 0.68f, 0.75f, 1.10f));
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
            Vector3 lookAt = new Vector3(0f, 0.3f, 0.15f);            // potre > slot > tir kompozisyon merkezi
            cam.transform.position = lookAt - fwd * 13f;
            cam.orthographicSize = cameraOrthographicSize;
            cam.backgroundColor = backgroundColor;

            // Isik TIRLARIN tarafindan (on-ust, yaw 0) -> kup onleri parlar, golge yukari/arkaya (blob yok)
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type != LightType.Directional || !l.gameObject.activeInHierarchy) continue;
                l.transform.eulerAngles = new Vector3(50f, 0f, 0f);
                l.color = Color.white;
                l.intensity = 1.05f;
            }
        }

        // ===================== SES & FEEDBACK (mekanik) =====================
        static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
        struct OutlineSave { public Color col; public float w; }
        readonly Dictionary<Material, OutlineSave> grabOrig = new Dictionary<Material, OutlineSave>();

        // Sepeti TUTUNCA tum parcalarinin toon outline'ini parlak/kalin yap; birakinca eski haline don.
        // Not: SRP Batcher MaterialPropertyBlock'u yok saydigi icin materyal instance'i DOGRUDAN degistiriyoruz.
        // Her sepetin materyalleri kendine ozel (BuildBasket new Material) -> sadece tutulan sepet etkilenir.
        void SetGrabHighlight(TruckInfo t, bool on)
        {
            if (t == null || t.root == null) return;
            if (on)
            {
                grabOrig.Clear();
                var rends = t.root.GetComponentsInChildren<Renderer>();
                // Tutma efekti: outline DEGIL (cok parcali sepette kesik cikar) -> govde rengini PARLAT (temiz, butun)
                for (int i = 0; i < rends.Length; i++)
                {
                    Renderer r = rends[i];
                    if (r == null) continue;
                    Material mat = r.sharedMaterial;
                    if (mat == null || !mat.HasProperty("_Color") || grabOrig.ContainsKey(mat)) continue;
                    OutlineSave s;
                    s.col = mat.GetColor("_Color");
                    s.w = 0f;
                    grabOrig[mat] = s;
                }
                foreach (var kv in grabOrig)
                    kv.Key.SetColor("_Color", Color.Lerp(kv.Value.col, Color.white, 0.30f)); // tutunca acilir/parlar
            }
            else
            {
                foreach (var kv in grabOrig)
                {
                    if (kv.Key == null) continue;
                    kv.Key.SetColor("_Color", kv.Value.col);
                }
                grabOrig.Clear();
            }
        }

        // Kup sepete inerken feedback: titresim (cihazda) + ses (AudioManager kendi throttle'lar).
        void CubeFillFeedback()
        {
            if (enableHaptics) Haptic.Light(0.06f, 24, 135);
            AudioManager.Play(AudioManager.Sfx.CubeFill);
        }

        // ===================== FAZ 2: cikarma mekanigi =====================
        // ===================== POTRE YOLU (v1 loop BIREBIR: LoopPath + curved track + chevron) =====================
        readonly LoopPath roadPath = new LoopPath();
        sealed class RoadFlow { public Transform tr; public float dist; }
        readonly List<RoadFlow> roadFlows = new List<RoadFlow>();
        bool roadActive;

        // v1 loop'u (yuvarlatilmis-dikdortgen pist: lane + ic/dis duvar + akan chevron) potre etrafina kurar.
        void BuildPortraitConveyor(Transform parent, float cx, float cz, float pw, float ph)
        {
            roadFlows.Clear();
            roadActive = false;
            if (!showPortraitRoad || parent == null) return;

            // v1 yol kesiti (birebir; roadScale ile olceklenir)
            float sc = Mathf.Max(0.2f, roadScale);
            float wallOffset = 0.60f * sc, wallThickness = 0.16f * sc, wallHeight = 0.46f * sc;
            float laneWidth = wallOffset * 2f - wallThickness;
            float reach = wallOffset + wallThickness * 0.5f;   // merkez cizgiden yolun ic kenarina

            // loop boyutu: potre + yol kesiti ortada sigsin
            float halfW = pw * 0.5f + reach + roadMargin;
            float halfD = ph * 0.5f + reach + roadMargin;
            float radius = Mathf.Min(halfW, halfD) * 0.34f;
            roadPath.SetRoundedRectangle(halfW * 2f, halfD * 2f, radius, 10, 0f);

            Transform roadRoot = new GameObject("PortraitRoad").transform;
            roadRoot.SetParent(parent, false);
            roadRoot.localPosition = new Vector3(cx, -0.04f, cz);

            // LANE (koyu zemin) + DIS/IC DUVAR (tek renk) - v1 birebir
            RoadCurvedBar("RoadLane", roadRoot, 0f, laneWidth, 0.06f * sc, -0.02f * sc, roadColor);
            RoadCurvedBar("RoadOuterWall", roadRoot, +wallOffset, wallThickness, wallHeight, 0f, roadWallColor);
            RoadCurvedBar("RoadInnerWall", roadRoot, -wallOffset, wallThickness, wallHeight, 0f, roadWallColor);

            // AKAN CHEVRON'LAR (v1 birebir)
            int count = Mathf.Max(6, Mathf.RoundToInt(roadPath.TotalLength / Mathf.Max(0.1f, roadChevronSpacing)));
            for (int i = 0; i < count; i++)
            {
                Transform ch = CreateRoadChevron(roadRoot, roadChevronColor, sc);
                roadFlows.Add(new RoadFlow { tr = ch, dist = roadPath.TotalLength * i / count });
            }
            PlaceRoadFlows();
            roadActive = true;
        }

        // v1 BuildCurvedBar BIREBIR: roadPath'i takip eden egri 4-yuzlu bar mesh
        void RoadCurvedBar(string barName, Transform parent, float sideOffset, float width, float height, float baseY, Color color)
        {
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(roadPath.TotalLength * 8f), 80, 320);
            Vector3[] verts = new Vector3[sampleCount * 4];
            Vector2[] uvs = new Vector2[sampleCount * 4];
            for (int s = 0; s < sampleCount; s++)
            {
                float t = (float)s / sampleCount;
                float d = roadPath.TotalLength * t;
                Vector3 pathPos = roadPath.GetPosition(d);
                Vector3 fwd = roadPath.GetForward(d);
                Vector3 perp = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 bc = pathPos + perp * sideOffset;
                Vector3 bi = bc - perp * width * 0.5f + Vector3.up * baseY;
                Vector3 bo = bc + perp * width * 0.5f + Vector3.up * baseY;
                Vector3 tiv = bi + Vector3.up * height;
                Vector3 tov = bo + Vector3.up * height;
                int b = s * 4;
                verts[b + 0] = bi; verts[b + 1] = bo; verts[b + 2] = tov; verts[b + 3] = tiv;
                uvs[b + 0] = new Vector2(0f, t); uvs[b + 1] = new Vector2(1f, t); uvs[b + 2] = new Vector2(1f, t); uvs[b + 3] = new Vector2(0f, t);
            }
            int[] tris = new int[4 * sampleCount * 2 * 3];
            int ti = 0;
            for (int s = 0; s < sampleCount; s++)
            {
                int v0 = s * 4;
                int v1 = ((s + 1) % sampleCount) * 4;
                tris[ti++] = v0 + 0; tris[ti++] = v0 + 1; tris[ti++] = v1 + 1;
                tris[ti++] = v0 + 0; tris[ti++] = v1 + 1; tris[ti++] = v1 + 0;
                tris[ti++] = v0 + 1; tris[ti++] = v0 + 2; tris[ti++] = v1 + 2;
                tris[ti++] = v0 + 1; tris[ti++] = v1 + 2; tris[ti++] = v1 + 1;
                tris[ti++] = v0 + 2; tris[ti++] = v0 + 3; tris[ti++] = v1 + 3;
                tris[ti++] = v0 + 2; tris[ti++] = v1 + 3; tris[ti++] = v1 + 2;
                tris[ti++] = v0 + 3; tris[ti++] = v0 + 0; tris[ti++] = v1 + 0;
                tris[ti++] = v0 + 3; tris[ti++] = v1 + 0; tris[ti++] = v1 + 3;
            }
            Mesh mesh = new Mesh { name = "RoadBar_" + barName };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject go = new GameObject(barName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Material m = Mat(color);
            if (m.HasProperty("_OutlineWidth")) { m = new Material(m); m.SetFloat("_OutlineWidth", 0f); } // v1: yolda outline yok
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // v1 chevron BIREBIR: 2 kup ">>" (45 donuk), forward'a bakar
        Transform CreateRoadChevron(Transform parent, Color color, float sc)
        {
            GameObject root = new GameObject("RoadFlow");
            root.transform.SetParent(parent, false);
            for (int j = 0; j < 2; j++)
            {
                GameObject chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = "Chev_" + j;
                DestroyImmediate(chev.GetComponent<Collider>());
                chev.transform.SetParent(root.transform, false);
                chev.transform.localPosition = new Vector3(0f, 0f, j * 0.16f * sc - 0.08f * sc);
                chev.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                chev.transform.localScale = new Vector3(0.10f, 0.10f, 0.04f) * sc;
                Material m = Mat(color);
                if (m.HasProperty("_OutlineWidth")) { m = new Material(m); m.SetFloat("_OutlineWidth", 0f); }
                chev.GetComponent<Renderer>().sharedMaterial = m;
            }
            return root.transform;
        }

        void PlaceRoadFlows()
        {
            for (int i = 0; i < roadFlows.Count; i++)
            {
                RoadFlow f = roadFlows[i];
                if (f.tr == null) continue;
                Vector3 pos = roadPath.GetPosition(f.dist);
                Vector3 fwd = roadPath.GetForward(f.dist);
                f.tr.localPosition = pos + Vector3.up * 0.06f;
                f.tr.localRotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        // Her frame chevron'lari path boyunca ilerlet -> yol akiyor hissi (win/lose'da da aksin)
        void AnimatePortraitRoad()
        {
            if (!roadActive || roadFlows.Count == 0 || roadPath.TotalLength <= 0f) return;
            float delta = roadFlowSpeed * Time.deltaTime;
            for (int i = 0; i < roadFlows.Count; i++)
            {
                if (roadFlows[i].tr == null) continue;
                roadFlows[i].dist = Mathf.Repeat(roadFlows[i].dist + delta, roadPath.TotalLength);
            }
            PlaceRoadFlows();
        }

        void Update()
        {
            AnimatePortraitRoad();   // konveyor surekli aksin
            if (gstate != GameState.Playing || trucks.Count == 0) return;
            Vector3 sp;
            if (!inputLocked && dragTruck == null && PointerDown(out sp))
            {
                Camera cam = gameCamera != null ? gameCamera : Camera.main;
                if (cam == null) return;
                RaycastHit hit;
                if (!Physics.Raycast(cam.ScreenPointToRay(sp), out hit, 300f)) return;
                TruckInfo ti = FindTruck(hit.collider != null ? hit.collider.transform : null);
                if (ti == null || ti.extracted || ti.moving) return;
                dragTruck = ti;
                dragStartScreen = sp;
                ti.moving = true;
                LiftTruck(ti);
                AudioManager.Play(AudioManager.Sfx.BasketGrab); // sepeti tutunca ses
                SetGrabHighlight(ti, true);                     // sepeti tutunca parlak outline
                return;
            }

            if (dragTruck != null && PointerPosition(out sp))
            {
                Vector3 delta = sp - dragStartScreen;
                if (delta.magnitude >= DragMinPixels)
                {
                    TruckInfo ti = dragTruck;
                    dragTruck = null;
                    SetGrabHighlight(ti, false); // hamle basladi -> outline kapat
                    int dx = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x > 0f ? 1 : -1) : 0;
                    int dz = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x) ? (delta.y > 0f ? 1 : -1) : 0;
                    Vector3 dir = new Vector3(dx, 0f, dz);
                    ti.headDir = dir.normalized;
                    ti.root.rotation = DirectionToTruckRotation(dir);
                    TryPuzzleMove(ti, dx, dz);
                    return;
                }
            }

            if (dragTruck != null && PointerUp(out sp))
            {
                SetGrabHighlight(dragTruck, false); // birakildi -> outline kapat
                DropTruck(dragTruck);
                dragTruck.moving = false;
                dragTruck = null;
            }
        }

        bool PointerDown(out Vector3 screenPos)
        {
            screenPos = Vector3.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) { screenPos = Input.mousePosition; return true; }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) { screenPos = Input.GetTouch(0).position; return true; }
#endif
            return false;
        }

        bool PointerUp(out Vector3 screenPos)
        {
            screenPos = Vector3.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonUp(0)) { screenPos = Input.mousePosition; return true; }
            if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)) { screenPos = Input.GetTouch(0).position; return true; }
#endif
            return false;
        }

        bool PointerPosition(out Vector3 screenPos)
        {
            screenPos = Vector3.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0)) { screenPos = Input.mousePosition; return true; }
            if (Input.touchCount > 0) { screenPos = Input.GetTouch(0).position; return true; }
#endif
            return false;
        }

        TruckInfo FindTruck(Transform t)
        {
            while (t != null)
            {
                for (int i = 0; i < trucks.Count; i++) if (trucks[i].root == t) return trucks[i];
                t = t.parent;
            }
            return null;
        }

        void TryPuzzleMove(TruckInfo t, int dx, int dz)
        {
            ArrowsPixelExitGate exitGate = FindExitMove(t.gx, t.gz, dx, dz);
            if (exitGate != null)
            {
                if (!CanTruckEnterSlot(t)) { StartCoroutine(ReturnToCellRoutine(t, true)); return; }
                SlotInfo free = null;
                for (int i = 0; i < slotList.Count; i++) if (slotList[i].occupant == null) { free = slotList[i]; break; }
                if (free == null) { StartCoroutine(ReturnToCellRoutine(t, true)); return; }
                free.occupant = t;
                t.extracted = true;
                t.moving = true;
                t.exitDir = ExitDirectionVector(exitGate.direction);
                moveCount++;
                UpdateMoveUI();
                StartCoroutine(ExtractRoutine(t, free));
                return;
            }

            int targetX = t.gx + dx;
            int targetZ = t.gz + dz;
            if (!CellExists(targetX, targetZ) || CellOccupied(targetX, targetZ))
            {
                StartCoroutine(ReturnToCellRoutine(t, true));
                return;
            }

            t.gx = targetX;
            t.gz = targetZ;
            moveCount++;
            UpdateMoveUI();

            Vector3 dir = new Vector3(dx, 0f, dz);
            t.headDir = dir.normalized;
            t.moving = true;
            StartCoroutine(SlideRoutine(t, CellToWorld(t.gx, t.gz), dir));
        }

        void LiftTruck(TruckInfo t)
        {
            Vector3 p = t.root.position;
            p.y = TruckGroundY; // tutunca sepet havaya kalkmasin
            t.root.position = p;
        }

        void DropTruck(TruckInfo t)
        {
            Vector3 p = t.root.position;
            p.y = TruckGroundY;
            t.root.position = p;
        }

        bool CellOccupied(int gx, int gz)
        {
            for (int i = 0; i < trucks.Count; i++)
            {
                TruckInfo o = trucks[i];
                if (o.extracted) continue;
                if (o.gx == gx && o.gz == gz) return true;
            }
            return false;
        }

        ArrowsPixelExitGate FindExitMove(int gx, int gz, int dx, int dz)
        {
            if (activeExits == null) return null;
            for (int i = 0; i < activeExits.Length; i++)
            {
                ArrowsPixelExitGate gate = activeExits[i];
                if (gate == null || gate.x != gx || gate.z != gz) continue;
                Vector3 dir = ExitDirectionVector(gate.direction);
                if (Mathf.RoundToInt(dir.x) == dx && Mathf.RoundToInt(dir.z) == dz) return gate;
            }
            return null;
        }

        ArrowsPixelExitGate FindExitAtCell(int gx, int gz)
        {
            if (activeExits == null) return null;
            for (int i = 0; i < activeExits.Length; i++)
            {
                ArrowsPixelExitGate gate = activeExits[i];
                if (gate != null && gate.x == gx && gate.z == gz) return gate;
            }
            return null;
        }

        Vector3 ExitDirectionVector(ArrowsPixelExitDirection direction)
        {
            switch (direction)
            {
                case ArrowsPixelExitDirection.Down: return Vector3.back;
                case ArrowsPixelExitDirection.Left: return Vector3.left;
                case ArrowsPixelExitDirection.Right: return Vector3.right;
                default: return Vector3.forward;
            }
        }

        System.Collections.IEnumerator SlideRoutine(TruckInfo t, Vector3 target, Vector3 dir)
        {
            t.moving = true;
            Transform tr = t.root;
            Vector3 a = tr.position;
            a.y = TruckGroundY;
            tr.position = a;
            target.y = TruckGroundY;
            if (basketSpinOnDrag && useBasketStyle)
            {
                yield return MoveSpin(tr, a, target, 0.22f, basketSpinTurns); // sepet kendi etrafinda doner
            }
            else
            {
                Quaternion rb = DirectionToTruckRotation(dir);
                yield return MoveRot(tr, a, target, tr.rotation, rb, 0.22f, false);
                tr.rotation = rb;
            }
            tr.position = target;

            ArrowsPixelExitGate exitGate = FindExitAtCell(t.gx, t.gz);
            if (exitGate != null)
            {
                if (!CanTruckEnterSlot(t))
                {
                    t.moving = false;
                    CheckEnd();
                    yield break;
                }
                SlotInfo free = null;
                for (int i = 0; i < slotList.Count; i++) if (slotList[i].occupant == null) { free = slotList[i]; break; }
                if (free == null) { t.moving = false; CheckEnd(); yield break; }
                free.occupant = t;
                t.extracted = true;
                t.exitDir = ExitDirectionVector(exitGate.direction);
                StartCoroutine(ExtractRoutine(t, free));
                yield break;
            }
            t.moving = false;
            CheckEnd();
        }

        Quaternion DirectionToTruckRotation(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
            return Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, -90f, 0f);
        }

        System.Collections.IEnumerator ExtractRoutine(TruckInfo t, SlotInfo slot)
        {
            AudioManager.Play(AudioManager.Sfx.BasketExtract); // sepet kapidan cikti
            t.moving = true;
            Transform tr = t.root;
            Vector3 a = tr.position;
            a.y = TruckGroundY;
            tr.position = a;
            Vector3 parkTarget = new Vector3(slot.pos.x, TruckGroundY, slot.pos.z);

            Vector3 exitDir = t.exitDir.sqrMagnitude > 0.001f ? t.exitDir.normalized : Vector3.forward;
            float pullStep = Mathf.Abs(exitDir.x) > Mathf.Abs(exitDir.z) ? gridStepX : gridStepZ;
            Vector3 pullOut = a + exitDir * pullStep * 0.95f;
            pullOut.y = TruckGroundY;
            Quaternion pullRot = DirectionToTruckRotation(exitDir);
            yield return MoveRot(tr, a, pullOut, tr.rotation, pullRot, 0.22f, false);

            Vector3 toSlot = parkTarget - pullOut; toSlot.y = 0f;
            Vector3 slotDir = toSlot;
            if (Mathf.Abs(slotDir.x) > Mathf.Abs(slotDir.z)) slotDir = new Vector3(Mathf.Sign(slotDir.x), 0f, 0f);
            else slotDir = new Vector3(0f, 0f, Mathf.Sign(slotDir.z));
            Quaternion driveRot = DirectionToTruckRotation(slotDir);
            yield return MoveRot(tr, pullOut, parkTarget, pullRot, driveRot, 0.48f, false); // ZEMINDE sur (arc YOK)
            Quaternion parked = driveRot;
            yield return MoveRot(tr, parkTarget, parkTarget, driveRot, parked, 0.16f, false);
            tr.position = parkTarget; tr.rotation = parked;
            t.moving = false;
            StartCoroutine(DeliverCubes(t, slot));   // Faz 3: kupler ona akar
        }

        System.Collections.IEnumerator MoveRot(Transform tr, Vector3 a, Vector3 b, Quaternion ra, Quaternion rb, float dur, bool arc)
        {
            float groundY = a.y;
            b.y = groundY;
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                Vector3 p = Vector3.Lerp(a, b, u);
                if (arc) p += Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.12f;
                if (!arc) p.y = groundY;
                tr.position = p;
                tr.rotation = Quaternion.Slerp(ra, rb, u);
                yield return null;
            }
            tr.position = b; tr.rotation = rb;
        }

        // Sepeti hedefe kaydirirken kendi Y ekseninde dondurur (turns tam tur). Simetrik sepet -> ayni aciya oturur.
        System.Collections.IEnumerator MoveSpin(Transform tr, Vector3 a, Vector3 b, float dur, float turns)
        {
            float groundY = a.y;
            b.y = groundY;
            Quaternion startRot = tr.rotation;
            float totalDeg = turns * 360f;
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                Vector3 p = Vector3.Lerp(a, b, u);
                p.y = groundY;
                tr.position = p;
                tr.rotation = startRot * Quaternion.Euler(0f, totalDeg * u, 0f);
                yield return null;
            }
            tr.position = b;
            tr.rotation = startRot * Quaternion.Euler(0f, totalDeg, 0f);
        }

        System.Collections.IEnumerator MoveArc(Transform tr, Vector3 a, Vector3 b, float dur, bool arc)
        {
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                Vector3 p = Vector3.Lerp(a, b, u);
                if (arc) p += Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.35f;
                tr.position = p;
                yield return null;
            }
            tr.position = b;
        }

        System.Collections.IEnumerator ReturnToCellRoutine(TruckInfo t, bool shake)
        {
            t.moving = true;
            Transform tr = t.root;
            Vector3 a = tr.position;
            Vector3 b = CellToWorld(t.gx, t.gz);
            b.y = TruckGroundY;
            yield return MoveRot(tr, a, b, tr.rotation, DirectionToTruckRotation(t.headDir), 0.14f, false);
            tr.position = b;
            if (shake) yield return Shake(t);
            else t.moving = false;
        }

        System.Collections.IEnumerator Shake(TruckInfo t)
        {
            t.moving = true;
            Transform tr = t.root;
            Vector3 o = tr.position;
            Vector3 ax = tr.right;
            for (int i = 0; i < 6; i++)
            {
                tr.position = o + ax * (i % 2 == 0 ? 0.07f : -0.07f);
                yield return new WaitForSeconds(0.03f);
            }
            tr.position = o;
            t.moving = false;
        }

        // ===================== FAZ 3: kup teslim + tir gider =====================
        System.Collections.IEnumerator DeliverCubes(TruckInfo t, SlotInfo slot)
        {
            List<GameObject> list;
            cubesByColor.TryGetValue(t.cargo, out list);
            int take = list == null ? 0 : Mathf.Min(t.capacity, list.Count);
            for (int i = 0; i < take; i++)
            {
                GameObject cube = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                if (cube != null)
                {
                    t.flyingCubes++;
                    activeCubeTransfers++;
                    StartCoroutine(FlyCubeToTruck(cube, t, t.filled));
                }
                t.filled++;
                yield return new WaitForSeconds(0.004f); // daha YOGUN/hizli akis
            }
            yield return new WaitForSeconds(0.26f);   // son kupler insin (kup ucusu ~0.23)
            while (t != null && t.flyingCubes > 0) yield return null; // son kup potreden ayrilmadan win paneli acilmasin
            yield return DepartTruck(t, slot);
        }

        System.Collections.IEnumerator FlyCubeToTruck(GameObject cube, TruckInfo t, int cargoIndex)
        {
            cube.transform.SetParent(root, true);     // dunya pozisyonunu koru
            Vector3 a = cube.transform.position;
            Vector3 s0 = cube.transform.localScale;
            Vector3 localTarget = useBasketStyle ? BasketFillPosition(cargoIndex) : CargoBedCubePosition(cargoIndex);
            Vector3 tgt = t.root.TransformPoint(localTarget);
            Vector3 finalScale = Vector3.one * 0.14f; // iri kup -> sepeti dolu gostersin
            float dur = 0.23f, e = 0f; // kup ucus suresi (0.38->0.23: daha HIZLI ucma+dolma)
            while (e < dur && cube != null)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                cube.transform.position = Vector3.Lerp(a, tgt, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.60f;
                cube.transform.localScale = Vector3.Lerp(s0, finalScale, u);
                yield return null;
            }
            if (cube != null)
            {
                cube.transform.SetParent(t.root, true);
                cube.transform.localPosition = localTarget;
                cube.transform.localRotation = Quaternion.identity;
                cube.transform.localScale = finalScale;
                CubeFillFeedback();   // kup indi -> titresim (cihaz) + opsiyonel ses
            }
            if (t != null) t.flyingCubes = Mathf.Max(0, t.flyingCubes - 1);
            activeCubeTransfers = Mathf.Max(0, activeCubeTransfers - 1);
        }

        Vector3 CargoBedCubePosition(int cargoIndex)
        {
            const int cols = 10;
            const int rows = 6;
            const float stepX = 0.078f;
            const float stepZ = 0.082f;
            const float layerY = 0.073f;
            int perLayer = cols * rows;
            int layer = cargoIndex / perLayer;
            int inLayer = cargoIndex % perLayer;
            int row = inLayer / cols;
            int col = inLayer % cols;
            if ((row & 1) == 1) col = cols - 1 - col;
            float x = -0.26f + (col - (cols - 1) * 0.5f) * stepX;
            float z = (row - (rows - 1) * 0.5f) * stepZ;
            return new Vector3(x, 0.545f + layer * layerY, z);
        }

        // Sepet ICINE kup dizilimi (tir kasasi degil): merkezli, oyuk tabanindan yukari kat kat
        Vector3 BasketFillPosition(int index)
        {
            float cell = Mathf.Min(gridStepX, gridStepZ);
            float innerHalf = cell * 0.36f;          // sepet ic yari-genislik (buyuk sepet)
            float spacing = 0.155f;                  // iri kup araligi (kup ~0.14) -> sepet dolu gorunsun
            int cols = Mathf.Max(2, Mathf.FloorToInt((innerHalf * 2f) / spacing));
            int perLayer = cols * cols;
            int layer = index / perLayer;
            int inLayer = index % perLayer;
            int row = inLayer / cols;
            int col = inLayer % cols;
            float x = (col - (cols - 1) * 0.5f) * spacing;
            float z = (row - (cols - 1) * 0.5f) * spacing;
            float y = cell * 0.115f + 0.075f + layer * 0.145f;
            return new Vector3(x, y, z);
        }

        System.Collections.IEnumerator DepartTruck(TruckInfo t, SlotInfo slot)
        {
            Transform tr = t.root;
            Vector3 s0 = tr.localScale;
            Vector3 p0 = tr.position;
            Vector3 popPos = p0 + Vector3.up * 0.18f;
            Vector3 endPos = p0 + Vector3.up * 1.05f;

            float popDur = 0.07f, e = 0f;   // %50 hizli dogus (eski 0.14)
            while (e < popDur && tr != null)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / popDur));
                tr.position = Vector3.Lerp(p0, popPos, u);
                tr.localScale = Vector3.Lerp(s0, s0 * 1.08f, u);
                yield return null;
            }

            Vector3 liftStart = tr != null ? tr.position : popPos;
            Vector3 scaleStart = tr != null ? tr.localScale : s0;
            float dur = 0.19f;              // %50 hizli havalanma/kaybolma (eski 0.38)
            e = 0f;
            while (e < dur && tr != null)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                tr.position = Vector3.Lerp(liftStart, endPos, u);
                tr.localScale = Vector3.Lerp(scaleStart, s0 * 0.02f, u);
                yield return null;
            }
            slot.occupant = null;
            if (tr != null) Destroy(tr.gameObject);
            SpawnExtractFireworks(p0 + Vector3.up * 0.4f);   // sepet yok oldu -> o noktada havai fisek
            CheckEnd();
        }

        // Sepet cikinca/yok olunca havai fisek: prefab atanmissa onu, yoksa kod-tabanli renkli kup patlamasi
        static readonly Color[] FwColors =
        {
            new Color(1f, 0.32f, 0.36f), new Color(1f, 0.84f, 0.30f), new Color(0.40f, 0.72f, 1f),
            new Color(0.42f, 0.95f, 0.55f), new Color(1f, 0.60f, 0.25f), new Color(0.80f, 0.45f, 1f)
        };

        void SpawnExtractFireworks(Vector3 pos)
        {
            if (!enableExtractFireworks) return;
            AudioManager.PlayFireworks(); // ayri kanal -> partikul bitince kesilebilir
            if (extractFireworksPrefab != null)
            {
                StartCoroutine(StopFireworksSoundAfter(2.5f)); // prefab 2.5s -> ses partikulle birlikte biter
                GameObject go = Instantiate(extractFireworksPrefab, pos, Quaternion.identity);
                Destroy(go, 2.5f);
                return;
            }
            StartCoroutine(StopFireworksSoundAfter(1.0f)); // kod partikulu ~0.95s -> ses bitince kesilir
            int count = Mathf.Clamp(extractFireworksCount, 4, 60);
            for (int i = 0; i < count; i++)
                StartCoroutine(FireworkParticle(pos, FwColors[Random.Range(0, FwColors.Length)]));
        }

        System.Collections.IEnumerator StopFireworksSoundAfter(float t)
        {
            yield return new WaitForSeconds(t);
            AudioManager.StopFireworks();
        }

        System.Collections.IEnumerator FireworkParticle(Vector3 origin, Color color)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(q.GetComponent<Collider>());
            q.transform.position = origin;
            float s = Random.Range(0.05f, 0.10f);
            q.transform.localScale = Vector3.one * s;
            Material m = Mat(color);
            if (m.HasProperty("_OutlineWidth")) { m = new Material(m); m.SetFloat("_OutlineWidth", 0f); }
            q.GetComponent<Renderer>().sharedMaterial = m;
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.4f), Random.Range(-1f, 1f)).normalized;
            Vector3 vel = dir * Random.Range(2.2f, 4.2f);
            Vector3 spin = new Vector3(Random.Range(-300f, 300f), Random.Range(-300f, 300f), Random.Range(-300f, 300f));
            float life = Random.Range(0.55f, 0.95f), e = 0f;
            while (e < life && q != null)
            {
                e += Time.deltaTime;
                vel += Vector3.up * -7f * Time.deltaTime;   // yercekimi
                q.transform.position += vel * Time.deltaTime;
                q.transform.Rotate(spin * Time.deltaTime, Space.Self);
                q.transform.localScale = Vector3.one * s * Mathf.Clamp01(1f - e / life);
                yield return null;
            }
            if (q != null) Destroy(q);
        }

        bool PictureEmpty()
        {
            if (activeCubeTransfers > 0) return false; // gorsel kup ucusu bitmeden win verme
            foreach (var kv in cubesByColor) if (kv.Value != null && kv.Value.Count > 0) return false;
            return true;
        }

        bool HasRemainingCubes(CargoColor color)
        {
            List<GameObject> list;
            return cubesByColor.TryGetValue(color, out list) && list != null && list.Count > 0;
        }

        bool CanTruckEnterSlot(TruckInfo t)
        {
            return t != null && HasRemainingCubes(t.cargo);
        }

        void CheckEnd()
        {
            if (gstate != GameState.Playing) return;
            if (PictureEmpty())
            {
                gstate = GameState.Won;
                inputLocked = true;
                coinAmount += Mathf.Max(0, coinPerLevel); // her level tamamlaninca +50 coin (win odulu)
                UpdateCoinUI();
                Analytics.LevelWin(currentLevel, moveCount);
                ShowEndPanel(winPanel, true);
                return;
            }
            if (moveCount >= moveLimit)
            {
                gstate = GameState.Lost;
                inputLocked = true;
                Analytics.LevelLose(currentLevel, moveCount);
                ShowEndPanel(losePanel, false);
            }
        }

        // TEST aracÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬ÂÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â±: Play modunda component'e sag tikla -> win panelini goster (logo pop + havai fisek denemesi)
        [ContextMenu("TEST: Win panelini goster")]
        void DebugShowWin()
        {
            gstate = GameState.Won;
            inputLocked = true;
            ShowEndPanel(winPanel, true);
        }

        // Panel atanmissa goster (Berkant tasarlar; butonlari LoadNextLevel/RestartLevel'e baglar).
        // Atanmamissa eski davranis: win -> sonraki level, lose -> restart (geriye donuk uyum).
        void ShowEndPanel(GameObject panel, bool win)
        {
            AudioManager.Play(win ? AudioManager.Sfx.Win : AudioManager.Sfx.Lose);
            if (panel != null) { panel.SetActive(true); return; }
            if (win) StartCoroutine(LoadNextLevelAfterDelay());
            else StartCoroutine(RestartAfterDelay());
        }

        System.Collections.IEnumerator LoadNextLevelAfterDelay()
        {
            yield return new WaitForSeconds(0.55f);
            LoadNextLevel();
        }

        System.Collections.IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(0.85f);
            RestartLevel();
        }

        // ===================== FAZ C: BOOSTER'LAR =====================
        // UI butonlari bu public metodlari cagirir. Buton gorselleri GECICI; Berkant giydirecek.

        bool TrySpendCoins(int cost)
        {
            if (coinAmount < cost)
            {
                Debug.Log("[Booster] Yetersiz coin: " + coinAmount + "/" + cost);
                return false;
            }
            coinAmount -= cost;
            UpdateCoinUI();
            return true;
        }

        int CountRemainingCubes(CargoColor c)
        {
            List<GameObject> l;
            return cubesByColor.TryGetValue(c, out l) && l != null ? l.Count : 0;
        }

        // ----- BOOSTER ENVANTERI (adet) -----
        // Adet 0'sa booster kullanilamaz (buton kilitli). Eylem GERCEKTEN yapilacaksa cagrilir -> 1 duser.
        bool ConsumeBooster(ref int count, string label)
        {
            if (count <= 0)
            {
                Debug.Log("[Booster] " + label + " bitti (adet 0) - paket al");
                return false;
            }
            count--;
            RefreshBoosterButtons();
            Analytics.BoosterUsed(label, currentLevel);
            return true;
        }

        // Paket sistemi (IAP/para) buradan adet ekler: which 0=YokEt 1=Kapi 2=Karistir
        public void AddBoosterCount(int which, int amount)
        {
            switch (which)
            {
                case 0: destroyFillerCount = Mathf.Clamp(destroyFillerCount + amount, 0, boosterMaxCount); break;
                case 1: extraExitCount     = Mathf.Clamp(extraExitCount + amount, 0, boosterMaxCount); break;
                case 2: shuffleCount       = Mathf.Clamp(shuffleCount + amount, 0, boosterMaxCount); break;
            }
            RefreshBoosterButtons();
        }

        public int GetBoosterCount(int which)
        {
            switch (which) { case 0: return destroyFillerCount; case 1: return extraExitCount; case 2: return shuffleCount; }
            return 0;
        }

        // Butonlarin KILIT (interactable) durumunu + ATANMIS adet yazisini gunceller.
        // Adet yazilarinin KONUMU/STILI tamamen Inspector'dan (Berkant); kod sadece "xN" yazar.
        void RefreshBoosterButtons()
        {
            // Adet 0 olsa da buton TIKLANABILIR kalir -> tiklayinca satin alma paneli acilir (kullanici istegi)
            if (destroyFillerButton != null) destroyFillerButton.interactable = true;
            if (extraExitButton != null)     extraExitButton.interactable     = true;
            if (shuffleButton != null)       shuffleButton.interactable       = true;

            if (destroyFillerCountText != null) destroyFillerCountText.text = "x" + destroyFillerCount;
            if (extraExitCountText != null)     extraExitCountText.text     = "x" + extraExitCount;
            if (shuffleCountText != null)       shuffleCountText.text       = "x" + shuffleCount;

            if (saveProgress) // power-up adetlerini kalici kaydet (coin gibi; diske yazim level gecisinde)
            {
                PlayerPrefs.SetInt(SaveKeyBoost0, destroyFillerCount);
                PlayerPrefs.SetInt(SaveKeyBoost1, extraExitCount);
                PlayerPrefs.SetInt(SaveKeyBoost2, shuffleCount);
            }
        }

        // ----- POWER-UP SATIN ALMA PANELI (x0'da butona tiklayinca) -----
        int BoosterCost(int which) { switch (which) { case 0: return destroyFillerCost; case 1: return extraExitCost; case 2: return shuffleCost; } return 0; }
        string BoosterName(int which) { switch (which) { case 0: return "Yok Et"; case 1: return "+Kapi"; case 2: return "Karistir"; } return "Power-up"; }

        public void OpenBoosterShop(int which)
        {
            pendingBoosterIndex = which;
            if (boosterShopIcon != null)
            {
                UnityEngine.UI.Button b = which == 0 ? destroyFillerButton : (which == 1 ? extraExitButton : shuffleButton);
                UnityEngine.UI.Image src = b != null ? (b.image != null ? b.image : b.GetComponentInChildren<UnityEngine.UI.Image>()) : null;
                if (src != null && src.sprite != null) boosterShopIcon.sprite = src.sprite; // tiklanan gucun ikonu OTOMATIK gelir
            }
            if (boosterShopText != null) boosterShopText.text = BoosterCost(which).ToString(); // SADECE fiyat rakami (orn 200)
            if (boosterShopPanel != null) boosterShopPanel.SetActive(true);
        }
        public void CloseBoosterShop() { if (boosterShopPanel != null) boosterShopPanel.SetActive(false); }

        // Panel SATIN AL: coin ile ilgili power-up'a +3 ekler (yetersiz coin -> iptal).
        public void BuyPendingBooster()
        {
            if (pendingBoosterIndex < 0) return;
            if (!TrySpendCoins(BoosterCost(pendingBoosterIndex))) return;
            AddBoosterCount(pendingBoosterIndex, 3);
            Analytics.Event("buy_booster_" + pendingBoosterIndex);
            CloseBoosterShop();
        }

        void WireBoosterShop()
        {
            if (boosterShopBuyButton != null) { boosterShopBuyButton.onClick.RemoveListener(BuyPendingBooster); boosterShopBuyButton.onClick.AddListener(BuyPendingBooster); }
            if (boosterShopCloseButton != null) { boosterShopCloseButton.onClick.RemoveListener(CloseBoosterShop); boosterShopCloseButton.onClick.AddListener(CloseBoosterShop); }
        }

        // --- 1) DOLGU SEPETI YOK ET: resmin ihtiyaci olmayan bir sepeti kaldirir -> alan acilir ---
        public void BoosterDestroyFiller()
        {
            if (gstate != GameState.Playing || inputLocked) return;
            if (destroyFillerCount <= 0) { OpenBoosterShop(0); return; } // bitti -> satin alma paneli ac
            TruckInfo victim = FindFillerTruck();
            if (victim == null) { Debug.Log("[Booster] Yok edilecek dolgu sepeti yok"); return; }
            if (!ConsumeBooster(ref destroyFillerCount, "YOK ET")) return;
            victim.extracted = true; // hucresi aninda bosalir, tiklanamaz olur
            StartCoroutine(DestroyTruckRoutine(victim));
        }

        // Guvenli aday: silinince kalan ayni renk kapasite, kalan kup ihtiyacini HALA karsilamali (level kilitlenmesin)
        TruckInfo FindFillerTruck()
        {
            TruckInfo best = null;
            int bestScore = int.MinValue;
            foreach (var t in trucks)
            {
                if (t == null || t.extracted || t.moving || t.root == null) continue;
                int remainingCubes = CountRemainingCubes(t.cargo);
                int otherCapacity = 0;
                foreach (var o in trucks)
                    if (o != null && o != t && !o.extracted && o.cargo == t.cargo)
                        otherCapacity += Mathf.Max(0, o.capacity - o.filled);
                if (otherCapacity < remainingCubes) continue; // gerekli sepet, dokunma
                int score = remainingCubes == 0 ? 100 : 10;   // resimde hic olmayan renk en iyi aday
                if (score > bestScore) { bestScore = score; best = t; }
            }
            return best;
        }

        System.Collections.IEnumerator DestroyTruckRoutine(TruckInfo t)
        {
            Transform tr = t.root;
            if (tr == null) yield break;
            Vector3 s0 = tr.localScale;
            float dur = 0.14f, e = 0f;
            while (e < dur && tr != null) // once hafif pop
            {
                e += Time.deltaTime;
                tr.localScale = s0 * (1f + 0.16f * Mathf.Sin(Mathf.Clamp01(e / dur) * Mathf.PI));
                yield return null;
            }
            dur = 0.18f; e = 0f;
            while (e < dur && tr != null) // sonra kuculerek yok ol
            {
                e += Time.deltaTime;
                tr.localScale = s0 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur)));
                yield return null;
            }
            if (tr != null) Destroy(tr.gameObject);
        }

        // --- 2) EKSTRA CIKIS: mevcut kapilardan en uzak sinir kenarina yeni kapi acar ---
        public void BoosterExtraExit()
        {
            if (gstate != GameState.Playing || inputLocked) return;
            if (extraExitCount <= 0) { OpenBoosterShop(1); return; } // bitti -> satin alma paneli ac
            int bgx, bgz, bdx, bdz;
            if (!FindNewExitEdge(out bgx, out bgz, out bdx, out bdz)) { Debug.Log("[Booster] Yeni kapi icin uygun kenar yok"); return; }
            if (!ConsumeBooster(ref extraExitCount, "+KAPI")) return;

            var list = new List<ArrowsPixelExitGate>(activeExits ?? new ArrowsPixelExitGate[0]);
            list.Add(new ArrowsPixelExitGate { x = bgx, z = bgz, direction = VectorToExitDirection(bdx, bdz) });
            activeExits = list.ToArray(); // activeLevel.exits'e DOKUNMUYORUZ -> level verisi kalici bozulmaz

            if (useSingleMeshBoard) // TEK-MESH: tepsi mesh'ini tazele (rim YENI cikista alcalir) + yeni kapi level-basi mantigiyla (offset+notch-skip+animasyon)
            {
                RebuildBoardTrayMesh();
                AddExitGateAnimated(bgx, bgz, bdx, bdz);
            }
            else if (parkTransform != null)
            {
                Transform wall = parkTransform.Find("Wall_" + bgx + "_" + bgz + "_" + bdx + "_" + bdz);
                if (wall != null) Destroy(wall.gameObject);
                Vector3 dir = new Vector3(bdx, 0f, bdz);
                const float wt = 0.17f; // BuildModularWalls ile ayni duvar kalinligi
                Vector3 center = CellToWorld(bgx, bgz) + dir * ((bdx != 0 ? gridStepX : gridStepZ) * 0.5f + wt * 0.5f);
                center.y = 0.16f;

                // Kapi parcalarini olustur, sonra hepsini bir konteynere toplayip "minik->buyu + zipla + sallan" animasyonu oynat
                int before = parkTransform.childCount;
                BuildGateMarker(parkTransform, center, dir, wt);
                GameObject cont = new GameObject("GateSpawn");
                cont.transform.SetParent(parkTransform, false);
                Vector3 pivot = center; pivot.y = 0f;          // taban pivotu -> yerden buyur gibi
                cont.transform.localPosition = pivot;
                for (int i = parkTransform.childCount - 1; i >= before; i--)
                {
                    Transform child = parkTransform.GetChild(i);
                    if (child == cont.transform) continue;
                    child.SetParent(cont.transform, true);      // dunya pozisyonu korunur
                }
                StartCoroutine(GateSpawnAnim(cont.transform, dir));
            }
        }

        // +KAPI dogus animasyonu: minik->tam boy (overshoot), kucuk zipla, sonra demir-para gibi sonumlu sallan
        System.Collections.IEnumerator GateSpawnAnim(Transform t, Vector3 dir)
        {
            if (t == null) yield break;
            Quaternion baseRot = t.localRotation;
            Vector3 basePos = t.localPosition;
            Vector3 axis = Vector3.Cross(Vector3.up, dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward);
            if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;
            axis.Normalize();

            // Faz 1: minikten buyu (overshoot) + zipla
            float d1 = 0.28f, e = 0f, hop = 0.30f;
            while (e < d1 && t != null)
            {
                e += Time.deltaTime;
                float u = Mathf.Clamp01(e / d1);
                t.localScale = Vector3.one * EaseOutBack(u);
                t.localPosition = basePos + Vector3.up * (Mathf.Sin(u * Mathf.PI) * hop);
                yield return null;
            }
            if (t == null) yield break;
            t.localScale = Vector3.one;
            t.localPosition = basePos;

            // Faz 2: sonumlu sallanim (yere konan demir para hissi)
            float d2 = 0.45f; e = 0f;
            const float amp = 11f, freq = 24f;
            while (e < d2 && t != null)
            {
                e += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(e / d2);      // sonum (0'a iner)
                float ang = Mathf.Sin(e * freq) * amp * k * k;
                t.localRotation = baseRot * Quaternion.AngleAxis(ang, axis);
                yield return null;
            }
            if (t != null) t.localRotation = baseRot;
        }

        // EaseOutBack: 1'i hafifce gecip geri oturan yumusak overshoot egrisi
        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }

        bool FindNewExitEdge(out int bgx, out int bgz, out int bdx, out int bdz)
        {
            bgx = bgz = bdx = bdz = 0;
            int[] dxs = { 1, -1, 0, 0 };
            int[] dzs = { 0, 0, 1, -1 };
            int bestScore = int.MinValue;
            bool found = false;
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                {
                    if (!CellExists(gx, gz)) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        if (CellExists(gx + dxs[d], gz + dzs[d])) continue; // sinir kenari degil
                        if (IsExitEdge(gx, gz, dxs[d], dzs[d])) continue;   // zaten kapi var
                        // NOTCH (kollar-arasi) kenari ATLA: orada kapi isareti gizleniyor -> booster bosa gitmesin, sadece GORUNUR dis kenar sec
                        bool notch = (dxs[d] != 0)
                            ? (CellExists(gx + dxs[d], gz + 1) || CellExists(gx + dxs[d], gz - 1))
                            : (CellExists(gx + 1, gz + dzs[d]) || CellExists(gx - 1, gz + dzs[d]));
                        if (notch) continue;
                        int score = int.MaxValue;
                        if (activeExits != null)
                            foreach (var g in activeExits)
                                if (g != null) score = Mathf.Min(score, Mathf.Abs(g.x - gx) + Mathf.Abs(g.z - gz));
                        if (score > bestScore) { bestScore = score; bgx = gx; bgz = gz; bdx = dxs[d]; bdz = dzs[d]; found = true; }
                    }
                }
            return found;
        }

        ArrowsPixelExitDirection VectorToExitDirection(int dx, int dz)
        {
            if (dx > 0) return ArrowsPixelExitDirection.Right;
            if (dx < 0) return ArrowsPixelExitDirection.Left;
            if (dz > 0) return ArrowsPixelExitDirection.Up;
            return ArrowsPixelExitDirection.Down;
        }

        // --- 3) KARISTIR: sepetlerin yerlerini permute eder; sonrasinda en az 1 hamle garantili ---
        public void BoosterShuffle()
        {
            if (gstate != GameState.Playing || inputLocked) return;
            List<TruckInfo> act = new List<TruckInfo>();
            if (shuffleCount <= 0) { OpenBoosterShop(2); return; } // bitti -> satin alma paneli ac
            foreach (var t in trucks) if (t != null && !t.extracted && !t.moving && t.root != null) act.Add(t);
            if (act.Count < 2) { Debug.Log("[Booster] Karistirilacak yeterli sepet yok"); return; }
            if (!ConsumeBooster(ref shuffleCount, "KARISTIR")) return;

            var cells = new List<Vector2Int>();
            foreach (var t in act) cells.Add(new Vector2Int(t.gx, t.gz));
            var order = new List<int>();
            for (int i = 0; i < cells.Count; i++) order.Add(i);

            for (int attempt = 0; attempt < 16; attempt++)
            {
                for (int i = order.Count - 1; i > 0; i--) // Fisher-Yates
                {
                    int j = Random.Range(0, i + 1);
                    int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
                }
                if (ShuffleHasMove(act, cells, order)) break;
            }

            for (int i = 0; i < act.Count; i++)
            {
                act[i].gx = cells[order[i]].x;
                act[i].gz = cells[order[i]].y;
            }
            StartCoroutine(ShuffleAnimRoutine(act));
        }

        bool ShuffleHasMove(List<TruckInfo> act, List<Vector2Int> cells, List<int> order)
        {
            int[] dxs = { 1, -1, 0, 0 };
            int[] dzs = { 0, 0, 1, -1 };
            for (int i = 0; i < act.Count; i++)
            {
                int gx = cells[order[i]].x, gz = cells[order[i]].y;
                for (int d = 0; d < 4; d++)
                {
                    int nx = gx + dxs[d], nz = gz + dzs[d];
                    bool occupied = false;
                    for (int k = 0; k < act.Count; k++)
                        if (k != i && cells[order[k]].x == nx && cells[order[k]].y == nz) { occupied = true; break; }
                    if (CellExists(nx, nz) && !occupied) return true;
                    if (IsExitEdge(gx, gz, dxs[d], dzs[d])) return true;
                }
            }
            return false;
        }

        System.Collections.IEnumerator ShuffleAnimRoutine(List<TruckInfo> act)
        {
            inputLocked = true;
            var from = new List<Vector3>();
            var to = new List<Vector3>();
            foreach (var t in act)
            {
                from.Add(t.root.position);
                Vector3 target = CellToWorld(t.gx, t.gz);
                target.y = t.root.position.y;
                to.Add(target);
            }
            float dur = 0.38f, e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur));
                float hop = Mathf.Sin(u * Mathf.PI) * 0.55f;
                for (int i = 0; i < act.Count; i++)
                    if (act[i].root != null) act[i].root.position = Vector3.Lerp(from[i], to[i], u) + Vector3.up * hop;
                yield return null;
            }
            for (int i = 0; i < act.Count; i++)
                if (act[i].root != null) act[i].root.position = to[i];
            inputLocked = false;
        }

        bool boosterWired;

        // Sahnede atanmis kalici butonlari (menu 'Build Booster UI' kurar) onClick'e baglar.
        // Berkant gorselleri verir; davranis koddan gelir. Bir kere baglanir (level reload'da cogalmaz).
        bool TryWireSceneBoosterButtons()
        {
            if (destroyFillerButton == null && extraExitButton == null && shuffleButton == null) return false;
            if (!boosterWired)
            {
                if (destroyFillerButton != null) destroyFillerButton.onClick.AddListener(BoosterDestroyFiller);
                if (extraExitButton != null) extraExitButton.onClick.AddListener(BoosterExtraExit);
                if (shuffleButton != null) shuffleButton.onClick.AddListener(BoosterShuffle);
                boosterWired = true;
            }
            RefreshBoosterButtons(); // adet rozetleri + kilit durumu (her level basinda da tazele)
            return true;
        }

        // --- GECICI UI: sahnede atanmis buton yoksa Canvas altina 3 placeholder buton kurar ---
        void BuildBoosterButtons()
        {
            if (TryWireSceneBoosterButtons()) return; // sahnedeki gercek butonlar varsa onlari kullan
            if (!showBoosterButtons) return;
            Canvas cv = FindObjectOfType<Canvas>();
            if (cv == null) return;
            Transform old = cv.transform.Find("BoosterBar");
            if (old != null) Destroy(old.gameObject);

            GameObject bar = new GameObject("BoosterBar", typeof(RectTransform));
            bar.transform.SetParent(cv.transform, false);
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 26f);
            rt.sizeDelta = new Vector2(580f, 104f);

            destroyFillerButton = CreateBoosterButton(bar.transform, 0, "YOK ET", BoosterDestroyFiller);
            extraExitButton     = CreateBoosterButton(bar.transform, 1, "+KAPI", BoosterExtraExit);
            shuffleButton       = CreateBoosterButton(bar.transform, 2, "KARISTIR", BoosterShuffle);
            boosterWired = true;        // bu butonlari kod kurdu -> onClick zaten bagli (TryWire tekrar baglamasin)
            RefreshBoosterButtons();    // adet rozetleri + kilit durumu
        }

        UnityEngine.UI.Button CreateBoosterButton(Transform parent, int index, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("Booster_" + label, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(180f, 96f);
            rt.anchoredPosition = new Vector2(8f + index * 196f, 0f);
            UnityEngine.UI.Image img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.99f, 0.90f, 0.70f, 0.96f); // cerceve altin tonu (gecici gorsel)
            UnityEngine.UI.Button btn = go.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(onClick);

            GameObject txt = new GameObject("Label", typeof(RectTransform));
            txt.transform.SetParent(go.transform, false);
            TMPro.TextMeshProUGUI tm = txt.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = label;
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.fontSize = 30f;
            tm.color = new Color(0.45f, 0.30f, 0.15f);
            tm.raycastTarget = false;
            RectTransform trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            return btn;
        }

        // UI: yazilar artik Canvas'tan (TextMeshPro). OnGUI kaldirildi; referanslar atanmazsa sessizce gecer.
        void UpdateMoveUI()
        {
            if (moveText != null) moveText.text = Mathf.Max(0, moveLimit - moveCount).ToString();
        }

        // BOLUM yazisi: levelTexts'te varsa onu, yoksa formatli ("Level N") goster. levelText atanmazsa sessiz gecer.
        void UpdateLevelUI()
        {
            if (levelText == null) return;
            int idx = currentLevel - 1;
            levelText.text = (levelTexts != null && idx >= 0 && idx < levelTexts.Length && !string.IsNullOrEmpty(levelTexts[idx]))
                ? levelTexts[idx]
                : string.Format(levelTextFormat, currentLevel);
        }

        void UpdateCoinUI()
        {
            if (coinText != null) coinText.text = coinAmount.ToString();
            if (saveProgress) PlayerPrefs.SetInt(SaveKeyCoin, coinAmount); // coin kalici kaydet
        }
    }
}







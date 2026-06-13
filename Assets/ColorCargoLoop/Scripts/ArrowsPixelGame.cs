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
        [Min(1f)] public float cameraOrthographicSize = 5.85f;

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
        public static ArrowsPixelLevelDefinition CreateGeneratedLevel(int oneBasedLevel)
        {
            int level = Mathf.Max(1, oneBasedLevel);
            if (level <= 5) return CreateEarlyPassableLevel(level);

            ArrowsPixelLevelDefinition def = CreateBaseLevel(level);

            if (level > 1)
            {
                int variant = (level - 1) % 4;
                if (variant == 1) ApplyWideTopExit(def);
                else if (variant == 2) ApplySideExit(def);
                else if (variant == 3) ApplyTwoExit(def);
            }

            return FinalizeLevel(def);
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
                cameraOrthographicSize = 5.85f,
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
            switch ((Mathf.Max(1, level) - 1) % 6)
            {
                case 1: return RocketPortrait();
                case 2: return CrownPortrait();
                case 3: return FlowerPortrait();
                case 4: return HousePortrait();
                case 5: return StarPortrait();
                default: return DefaultPortrait();
            }
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
        [SerializeField] private Transform[] slotPoints;   // ORTA: tirin park edecegi slot noktalari (3)
        [SerializeField] private Transform parkingArea;    // ALT: tir puzzle grid merkezi
        [SerializeField] private bool autoSetupCamera = true; // kapatirsan kamerayi sen ayarlarsin

        [Header("Gorsel Ayarlar (kolay erisim)")]
        [SerializeField, Range(0.4f, 1.5f)] private float basketHeightScale = 0.80f; // sepet duvar BOYU carpani (1 = eski boy; 0.8 = %20 kisa)
        [SerializeField] private float portraitCubeSize = 0.14f; // potre kup boyutu; 0.14 = sepete dolan kup ile AYNI (0 yaparsan level degeri kullanilir)

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

        [Header("UI (Canvas'ta sen kur, sonra bagla)")]
        [SerializeField] private TMPro.TMP_Text moveText;  // kalan hamle
        [SerializeField] private TMPro.TMP_Text coinText;
        [SerializeField] private int coinAmount = 625;
        [SerializeField] private GameObject winPanel;      // KAZANDIN paneli
        [SerializeField] private GameObject losePanel;     // KAYBETTIN paneli

        [Header("Level Sistemi")]
        [Min(1)]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private bool useGeneratedLevels = true;
        [SerializeField] private ArrowsPixelLevelDefinition[] levels;

        Sprite arrowSprite;

        // ===== FAZ 2: cikarma mekanigi (Arrows) =====
        sealed class TruckInfo { public Transform root; public Vector3 headDir; public Vector3 exitDir; public CargoColor cargo; public int capacity; public int filled; public bool extracted; public bool moving; public int gx; public int gz; }
        sealed class SlotInfo { public Vector3 pos; public TruckInfo occupant; }
        enum GameState { Playing, Won, Lost }
        readonly List<TruckInfo> trucks = new List<TruckInfo>();
        readonly List<SlotInfo> slotList = new List<SlotInfo>();
        readonly Dictionary<CargoColor, List<GameObject>> cubesByColor = new Dictionary<CargoColor, List<GameObject>>();
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
        float cameraOrthographicSize = 5.85f;
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
        static readonly Color C_FLOOR  = new Color(0.66f, 0.84f, 0.97f); // arka plan: acik mavi (eski pembe: 0.95,0.71,0.74)
        static readonly Color C_PAD    = new Color(0.99f, 0.86f, 0.74f);
        static readonly Color C_AREA      = new Color(0.55f, 0.59f, 0.72f); // birlesik alan rengi (board/slot/potre bg)
        static readonly Color C_AREA_DARK = new Color(0.45f, 0.49f, 0.62f); // grid hucre recess (koyu, acik renk yok)

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
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                if (cubeRenderer == null) cubeRenderer = cube.GetComponentInChildren<Renderer>();
                if (cubeRenderer != null) cubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // resim golge atmasin (blob olmasin)
                CargoColor _cc = CharToCargo(ch);
                if (!cubesByColor.TryGetValue(_cc, out var _lst)) { _lst = new List<GameObject>(); cubesByColor[_cc] = _lst; }
                _lst.Add(cube); // teslim icin takip
                // Kup gorunumu (kullanici tarifi): toon outline + ON/KENAR isik yansimasi (rim+highlight) + ust hafif golge
                var pm = cubeRenderer != null && cubeRenderer.sharedMaterial != null ? new Material(cubeRenderer.sharedMaterial) : null;
                if (pm != null)
                {
                    if (pm.HasProperty("_OutlineColor")) pm.SetColor("_OutlineColor", new Color(0.055f, 0.045f, 0.09f, 1f));
                    if (pm.HasProperty("_OutlineWidth")) pm.SetFloat("_OutlineWidth", 0.034f);
                    if (pm.HasProperty("_ShadowColor")) pm.SetColor("_ShadowColor", Color.Lerp(CharColor(ch), new Color(0.07f, 0.05f, 0.12f), 0.58f));
                    if (pm.HasProperty("_RampThreshold")) pm.SetFloat("_RampThreshold", 0.56f);
                    if (pm.HasProperty("_RimStrength")) pm.SetFloat("_RimStrength", 0.30f);
                    if (pm.HasProperty("_RimColor")) pm.SetColor("_RimColor", new Color(1f, 0.94f, 0.82f));
                    if (pm.HasProperty("_HighlightStrength")) pm.SetFloat("_HighlightStrength", 0.72f);
                    if (pm.HasProperty("_HighlightColor")) pm.SetColor("_HighlightColor", new Color(1f, 0.98f, 0.88f));
                    if (pm.HasProperty("_ShadeStrength")) pm.SetFloat("_ShadeStrength", 0.50f);
                    cubeRenderer.sharedMaterial = pm;
                }
            }
            else
            {
                var g = RoundedCube(name, parent, pos, 1f, CharColor(ch), 0.020f);
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
            trucks.Clear(); slotList.Clear(); cubesByColor.Clear(); moveCount = 0; inputLocked = false; gstate = GameState.Playing;
            ResolveActiveLevel();
            ValidateActiveLevel();
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
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
            if (autoSetupCamera) SetupCamera();
            UpdateMoveUI();
            UpdateCoinUI();
            BuildBoosterButtons();
        }

        public void LoadLevel(int oneBasedLevel)
        {
            currentLevel = Mathf.Max(1, oneBasedLevel);
            BuildLayout();
        }

        public void LoadNextLevel()
        {
            LoadLevel(currentLevel + 1);
        }

        public void RestartLevel()
        {
            BuildLayout();
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
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0f);
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
                // krem alanlari (slot + potre bg) birlesik area rengine cevir (acik renk istenmiyor)
                Color cc = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.clear;
                if (Mathf.Abs(cc.r - 0.99f) + Mathf.Abs(cc.g - 0.86f) + Mathf.Abs(cc.b - 0.74f) < 0.22f)
                {
                    if (m.HasProperty("_Color")) m.SetColor("_Color", C_AREA);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
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
        void BuildBackground()
        {
            Box("BG", root, new Vector3(0f, -0.2f, 0.5f), new Vector3(14f, 0.2f, 22f), C_FLOOR);
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
                tileScale = Vector3.one * portraitCubeSize;
                stepX = stepZ = portraitCubeSize * 1.06f;
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
                grid.transform.localPosition = Vector3.zero;
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
                // Renkli MODULER cerceve: resim boyutu degisince cerceve de otomatik uyar
                Color frameC = new Color(0.99f, 0.90f, 0.70f); // sicak krem-altin (UI plaketleriyle ayni aile)
                RoundedPad("PictureFrame", pgnd.transform, new Vector3(pcx, -0.115f, pcz), pw + 0.48f, ph + 0.48f, frameC, 0.07f, 0.36f);
                RoundedPad("PictureBase", pgnd.transform, new Vector3(pcx, -0.10f, pcz), pw + 0.30f, ph + 0.30f, C_AREA, 0.08f, 0.32f);
                RoundedPad("PictureCell", pgnd.transform, new Vector3(pcx, -0.075f, pcz), pw, ph, C_AREA_DARK, 0.05f, 0.26f);
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
            // Atanmis slot noktalari varsa onlardan oku (gorsel padler sahnede); yoksa eski runtime padleri kur.
            if (slotPoints != null && slotPoints.Length > 0)
            {
                // Statik krem padler runtime'da area rengine boyandigi icin gorunmez kaliyordu;
                // onlari gizle, parking grid hucresiyle AYNI gorsel soketi kur (root altinda -> rebuild'de temizlenir).
                GameObject slotVis = new GameObject("SlotPads");
                slotVis.transform.SetParent(root, false);
                for (int i = 0; i < slotPoints.Length; i++)
                {
                    if (slotPoints[i] == null) continue;
                    Transform spad = slotPoints[i].Find("Pad"); if (spad != null) spad.gameObject.SetActive(false);
                    Transform srim = slotPoints[i].Find("Rim"); if (srim != null) srim.gameObject.SetActive(false);
                    Vector3 sp = slotPoints[i].position; sp.y = TruckGroundY;
                    // Renkli cerceve (potre cercevesiyle ayni sicak ton)
                    RoundedPad("SlotFrame_" + i, slotVis.transform, new Vector3(sp.x, 0.045f, sp.z), gridStepX * 1.10f + 0.16f, gridStepZ * 1.10f + 0.16f, new Color(0.99f, 0.90f, 0.70f), 0.09f, 0.26f);
                    RoundedPad("SlotBase_" + i, slotVis.transform, new Vector3(sp.x, 0.05f, sp.z), gridStepX * 1.10f, gridStepZ * 1.10f, C_AREA, 0.12f, 0.22f);
                    Pad("SlotCell_" + i, slotVis.transform, new Vector3(sp.x, 0.095f, sp.z), gridStepX * 0.86f, gridStepZ * 0.86f, C_AREA_DARK, 0.05f);
                    slotList.Add(new SlotInfo { pos = sp, occupant = null });
                }
                return;
            }

            GameObject slots = new GameObject("Slots");
            slots.transform.SetParent(root, false);
            float gap = 1.65f;
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * gap;
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
                // Eski duz pembe duvarlar + statik kapi pad'i gizlenir; yerine modüler yumusak duvar kurulur
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
                RoundedPad("BoardBase", park.transform, new Vector3(parkingOrigin.x, 0.05f, parkingOrigin.z), boardX, boardZ, C_AREA, 0.12f, 0.32f);
            }

            // GRID: her (var olan) hucrede yuvarlak koseli koyu socket (cartoon recess)
            for (int sgx = 0; sgx < gridWidth; sgx++)
                for (int sgz = 0; sgz < gridHeight; sgz++)
                {
                    if (!CellExists(sgx, sgz)) continue;
                    Vector3 cp = CellToWorld(sgx, sgz);
                    if (HasCellMask())
                    {
                        Vector3 bp = cp; bp.y = 0.05f;
                        RoundedPad("BoardBase_" + sgx + "_" + sgz, park.transform, bp, gridStepX + 0.02f, gridStepZ + 0.02f, C_AREA, 0.12f, 0.10f);
                    }
                    cp.y = 0.095f;
                    Pad("Cell_" + sgx + "_" + sgz, park.transform, cp, gridStepX * 0.86f, gridStepZ * 0.86f, C_AREA_DARK, 0.05f);
                }

            // Modüler yumusak duvar: sinir kenarlarini hucre hucre takip eder (T/L grid formlarinda da calisir)
            if (useAnchor) BuildModularWalls(park.transform);

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
        void BuildModularWalls(Transform parent)
        {
            Color wallC = new Color(0.93f, 0.94f, 1.00f); // acik, yumusak (pembe dandik duvarin yerine)
            float t = 0.17f, wh = 0.30f, wy = 0.16f;
            for (int gx = 0; gx < gridWidth; gx++)
                for (int gz = 0; gz < gridHeight; gz++)
                {
                    if (!CellExists(gx, gz)) continue;
                    TryWallEdge(parent, gx, gz, 1, 0, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, -1, 0, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, 0, 1, wallC, t, wh, wy);
                    TryWallEdge(parent, gx, gz, 0, -1, wallC, t, wh, wy);
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
            float len = (dx != 0 ? gridStepZ : gridStepX) + t; // koseler kapanacak kadar uzat
            RoundedPad("Wall_" + gx + "_" + gz + "_" + dx + "_" + dz, parent, center,
                       dx != 0 ? t : len, dx != 0 ? len : t, c, wh, t * 0.42f);
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
            Color postC  = new Color(0.93f, 0.94f, 1.00f);
            Color matC   = new Color(0.99f, 0.93f, 0.78f);
            Color arrowC = new Color(1.00f, 0.78f, 0.25f);
            bool horiz = Mathf.Abs(dir.x) > 0.5f; // kapi sag/sol kenarda mi
            Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
            float half = (horiz ? gridStepZ : gridStepX) * 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "GatePost";
                DestroyImmediate(post.GetComponent<Collider>());
                post.transform.SetParent(parent, false);
                Vector3 pp = center + perp * (half * s); pp.y = 0.20f;
                post.transform.localPosition = pp;
                post.transform.localScale = new Vector3(0.26f, 0.20f, 0.26f);
                post.GetComponent<Renderer>().sharedMaterial = Mat(postC);
            }
            // Paspas + ok duvarin DISINDA durur -> hucrede sepet varken de yon gorunur
            Vector3 mp = center + dir * 0.40f; mp.y = 0.095f;
            RoundedPad("GateMat", parent, mp, horiz ? 0.46f : 0.95f, horiz ? 0.95f : 0.46f, matC, 0.05f, 0.14f);
            GameObject ar = new GameObject("GateArrow");
            ar.transform.SetParent(parent, false);
            Vector3 ap = center + dir * 0.42f; ap.y = 0.145f;
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
            Shader sh = Shader.Find("Color Cargo Loop/Toon Plastic");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material body = new Material(sh) { name = "BlockBody" };
            if (body.HasProperty("_Color")) body.SetColor("_Color", color);
            if (body.HasProperty("_BaseColor")) body.SetColor("_BaseColor", Color.white);
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
            Shader sh = Shader.Find("Color Cargo Loop/Toon Plastic");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh) { name = "BasketBody" };
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.06f, 0.05f, 0.10f, 1f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.020f);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", Color.Lerp(color, new Color(0.07f, 0.05f, 0.12f), 0.55f));
            if (m.HasProperty("_RampThreshold")) m.SetFloat("_RampThreshold", 0.55f);
            if (m.HasProperty("_ShadeStrength")) m.SetFloat("_ShadeStrength", 0.50f);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(1f, 0.96f, 0.86f));
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.28f);
            if (m.HasProperty("_HighlightColor")) m.SetColor("_HighlightColor", new Color(1f, 0.99f, 0.92f));
            if (m.HasProperty("_HighlightStrength")) m.SetFloat("_HighlightStrength", 0.78f);

            // HUCREYI TAM DOLDUR -> komsu sepetlerle DIP DIBE (bosluk yok)
            float Ex = gridStepX * 0.49f;
            float Ez = gridStepZ * 0.49f;
            float cm = Mathf.Min(gridStepX, gridStepZ);
            float h = cm * 0.46f * basketHeightScale, t = cm * 0.065f, fh = cm * 0.09f; // ince duvar (Beads Out referansi); boy Inspector'dan
            float r = Mathf.Min(Ex, Ez) * 0.14f; // kucuk kose yaricapi -> ince duvarla orantili (Beads Out referansi)
            float wy = fh + h * 0.5f;

            // PARLAK RENKLI IC TABAN (Beads Out referansi: ic, sepet renginin hafif koyusu + parlak)
            Material inner = new Material(m) { name = "BasketInner" };
            Color innerC = Color.Lerp(color, Color.black, 0.16f);
            if (inner.HasProperty("_Color")) inner.SetColor("_Color", innerC);
            if (inner.HasProperty("_ShadowColor")) inner.SetColor("_ShadowColor", Color.Lerp(innerC, Color.black, 0.35f));
            if (inner.HasProperty("_HighlightStrength")) inner.SetFloat("_HighlightStrength", 0.55f);
            if (inner.HasProperty("_RimStrength")) inner.SetFloat("_RimStrength", 0.15f);
            if (inner.HasProperty("_OutlineWidth")) inner.SetFloat("_OutlineWidth", 0.010f);
            BasketCube(parent, inner, new Vector3(0f, fh * 0.6f, 0f), new Vector3(2f * (Ex - t * 0.55f), fh * 1.2f, 2f * (Ez - t * 0.55f)));

            // DIS DUVARLAR + koseler (govde rengi, yuksek)
            BasketCube(parent, m, new Vector3(Ex - t * 0.5f, wy, 0f), new Vector3(t, h, 2f * Ez - 2f * r));       // sag duvar
            BasketCube(parent, m, new Vector3(-(Ex - t * 0.5f), wy, 0f), new Vector3(t, h, 2f * Ez - 2f * r));    // sol duvar
            BasketCube(parent, m, new Vector3(0f, wy, Ez - t * 0.5f), new Vector3(2f * Ex - 2f * r, h, t));       // on duvar
            BasketCube(parent, m, new Vector3(0f, wy, -(Ez - t * 0.5f)), new Vector3(2f * Ex - 2f * r, h, t));    // arka duvar
            BasketCorner(parent, m, new Vector3(Ex - r, wy, Ez - r), r, h);
            BasketCorner(parent, m, new Vector3(-(Ex - r), wy, Ez - r), r, h);
            BasketCorner(parent, m, new Vector3(Ex - r, wy, -(Ez - r)), r, h);
            BasketCorner(parent, m, new Vector3(-(Ex - r), wy, -(Ez - r)), r, h);

            // UST DUDAK (acik renk ince kapak) -> Block Jam pop + agiz vurgusu
            Material rim = new Material(m) { name = "BasketRim" };
            Color rimC = Color.Lerp(color, Color.white, 0.22f);
            if (rim.HasProperty("_Color")) rim.SetColor("_Color", rimC);
            if (rim.HasProperty("_OutlineWidth")) rim.SetFloat("_OutlineWidth", 0.012f);
            float ry = fh + h;
            float rh = cm * 0.075f;
            float ryc = ry + rh * 0.5f - 0.01f;
            BasketCube(parent, rim, new Vector3(Ex - t * 0.5f, ryc, 0f), new Vector3(t * 1.18f, rh, 2f * Ez - 2f * r));
            BasketCube(parent, rim, new Vector3(-(Ex - t * 0.5f), ryc, 0f), new Vector3(t * 1.18f, rh, 2f * Ez - 2f * r));
            BasketCube(parent, rim, new Vector3(0f, ryc, Ez - t * 0.5f), new Vector3(2f * Ex - 2f * r, rh, t * 1.18f));
            BasketCube(parent, rim, new Vector3(0f, ryc, -(Ez - t * 0.5f)), new Vector3(2f * Ex - 2f * r, rh, t * 1.18f));
            BasketCorner(parent, rim, new Vector3(Ex - r, ryc, Ez - r), r * 1.06f, rh);
            BasketCorner(parent, rim, new Vector3(-(Ex - r), ryc, Ez - r), r * 1.06f, rh);
            BasketCorner(parent, rim, new Vector3(Ex - r, ryc, -(Ez - r)), r * 1.06f, rh);
            BasketCorner(parent, rim, new Vector3(-(Ex - r), ryc, -(Ez - r)), r * 1.06f, rh);

            // ALT BEYAZ BANT (Beads Out referansi: sepetin oturdugu acik taban bandi)
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
            cam.backgroundColor = C_FLOOR;

            // Isik TIRLARIN tarafindan (on-ust, yaw 0) -> kup onleri parlar, golge yukari/arkaya (blob yok)
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type != LightType.Directional || !l.gameObject.activeInHierarchy) continue;
                l.transform.eulerAngles = new Vector3(50f, 0f, 0f);
                l.intensity = 1.05f;
            }
        }

        // ===================== FAZ 2: cikarma mekanigi =====================
        void Update()
        {
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
                return;
            }

            if (dragTruck != null && PointerPosition(out sp))
            {
                Vector3 delta = sp - dragStartScreen;
                if (delta.magnitude >= DragMinPixels)
                {
                    TruckInfo ti = dragTruck;
                    dragTruck = null;
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
            p.y = TruckLiftY;
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
            Quaternion rb = DirectionToTruckRotation(dir);
            yield return MoveRot(tr, a, target, tr.rotation, rb, 0.22f, false);
            tr.position = target;
            tr.rotation = rb;

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
                if (cube != null) StartCoroutine(FlyCubeToTruck(cube, t, t.filled));
                t.filled++;
                yield return new WaitForSeconds(0.008f); // yogun akis (eski sirali: 0.035)
            }
            yield return new WaitForSeconds(0.45f);   // son kupler insin
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
            float dur = 0.38f, e = 0f;
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
            }
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

            float popDur = 0.14f, e = 0f;
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
            float dur = 0.38f;
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
            CheckEnd();
        }

        bool PictureEmpty()
        {
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
                StartCoroutine(LoadNextLevelAfterDelay());
                return;
            }
            if (moveCount >= moveLimit) gstate = GameState.Lost;
        }

        System.Collections.IEnumerator LoadNextLevelAfterDelay()
        {
            yield return new WaitForSeconds(0.55f);
            LoadNextLevel();
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

        // --- 1) DOLGU SEPETI YOK ET: resmin ihtiyaci olmayan bir sepeti kaldirir -> alan acilir ---
        public void BoosterDestroyFiller()
        {
            if (gstate != GameState.Playing || inputLocked) return;
            TruckInfo victim = FindFillerTruck();
            if (victim == null) { Debug.Log("[Booster] Yok edilecek dolgu sepeti yok"); return; }
            if (!TrySpendCoins(destroyFillerCost)) return;
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
            int bgx, bgz, bdx, bdz;
            if (!FindNewExitEdge(out bgx, out bgz, out bdx, out bdz)) { Debug.Log("[Booster] Yeni kapi icin uygun kenar yok"); return; }
            if (!TrySpendCoins(extraExitCost)) return;

            var list = new List<ArrowsPixelExitGate>(activeExits ?? new ArrowsPixelExitGate[0]);
            list.Add(new ArrowsPixelExitGate { x = bgx, z = bgz, direction = VectorToExitDirection(bdx, bdz) });
            activeExits = list.ToArray(); // activeLevel.exits'e DOKUNMUYORUZ -> level verisi kalici bozulmaz

            if (parkTransform != null)
            {
                Transform wall = parkTransform.Find("Wall_" + bgx + "_" + bgz + "_" + bdx + "_" + bdz);
                if (wall != null) Destroy(wall.gameObject);
                Vector3 dir = new Vector3(bdx, 0f, bdz);
                const float wt = 0.17f; // BuildModularWalls ile ayni duvar kalinligi
                Vector3 center = CellToWorld(bgx, bgz) + dir * ((bdx != 0 ? gridStepX : gridStepZ) * 0.5f + wt * 0.5f);
                center.y = 0.16f;
                BuildGateMarker(parkTransform, center, dir, wt);
            }
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
            foreach (var t in trucks) if (t != null && !t.extracted && !t.moving && t.root != null) act.Add(t);
            if (act.Count < 2) { Debug.Log("[Booster] Karistirilacak yeterli sepet yok"); return; }
            if (!TrySpendCoins(shuffleCost)) return;

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

            CreateBoosterButton(bar.transform, 0, "YOK ET", destroyFillerCost, BoosterDestroyFiller);
            CreateBoosterButton(bar.transform, 1, "+KAPI", extraExitCost, BoosterExtraExit);
            CreateBoosterButton(bar.transform, 2, "KARISTIR", shuffleCost, BoosterShuffle);
        }

        void CreateBoosterButton(Transform parent, int index, string label, int cost, UnityEngine.Events.UnityAction onClick)
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
            tm.text = label + "\n<size=64%>" + cost + " coin</size>";
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.fontSize = 30f;
            tm.color = new Color(0.45f, 0.30f, 0.15f);
            tm.raycastTarget = false;
            RectTransform trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        // UI: yazilar artik Canvas'tan (TextMeshPro). OnGUI kaldirildi; referanslar atanmazsa sessizce gecer.
        void UpdateMoveUI()
        {
            if (moveText != null) moveText.text = Mathf.Max(0, moveLimit - moveCount).ToString();
        }

        void UpdateCoinUI()
        {
            if (coinText != null) coinText.text = coinAmount.ToString();
        }
    }
}

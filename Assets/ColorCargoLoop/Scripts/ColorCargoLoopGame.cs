using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColorCargoLoop
{
    public enum GameState
    {
        Loading,
        Playing,
        Won,
        Lost,
        Paused
    }

    public enum PathDesign
    {
        RoundedLoop,
        WideLoop,
        PinchedLoop,
        OffsetLoop,
        SoftSquare
    }

    /// <summary>
    /// Color Cargo Loop - prototype root.
    /// Cartlar loop'un MERKEZINDE dikey istif halinde durur.
    /// Cargo loop boyunca akar ve renk eÅŸleÅŸen ColorZone'a vardÄ±ÄŸÄ±nda toplanÄ±r.
    /// </summary>
    public sealed class ColorCargoLoopGame : MonoBehaviour
    {
        // ----- Eski serileÅŸtirilmiÅŸ alanlar korunuyor (sahne uyumluluÄŸu iÃ§in) -----
        [Header("Prototype")]
        [SerializeField] private int startLevelIndex = 1;
        [SerializeField] private UnityEngine.Object cartModelPrefab;
        [SerializeField] private string cartModelAssetPath = "Assets/Meshy_AI_Purple_Toy_Wagon_with_0515222443_texture.fbx";
        [SerializeField] private Vector3 cartModelScale = Vector3.one;
        [SerializeField] private Vector3 cartModelLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 cartModelLocalOffset = Vector3.zero;
        [SerializeField] private float cartModelTargetSize = 2.95f;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool useImportedCartModel = false;

        [Header("Camera & Background")]
        [SerializeField] private Color cameraBackground = new Color(0.085f, 0.06f, 0.18f);
        [SerializeField] private Color floorColor = new Color(0.135f, 0.10f, 0.30f);
        [SerializeField] private float orthographicSize = 7.4f;
        [SerializeField] private float orthographicSizePerCart = 1.85f; // Her tır için ek kamera genişliği

        [Header("Track")]
        [SerializeField] private Color trackColor = new Color(0.40f, 0.27f, 0.78f);
        [SerializeField] private Color laneColor = new Color(0.18f, 0.12f, 0.42f);
        [SerializeField] private Color railColor = new Color(0.72f, 0.61f, 1.0f);
        [SerializeField] private float trackWidthX = 5.4f;
        [SerializeField] private float trackDepthZ = 8.6f;
        [SerializeField] private float trackCornerRadius = 1.85f;

        [Header("Cargo")]
        [SerializeField] private Vector3 roadCargoScale = new Vector3(0.37f, 0.30f, 0.37f);
        [SerializeField] private float cargoEnterFlightSpeed = 1.55f;
        [SerializeField] private float cargoRoadSpinSpeed = 0f;
        [SerializeField] private float cargoPathSpacing = 0.58f;
        [SerializeField] private float cargoLaneScatter = 0f;
        [SerializeField] private float cargoCollectSpeed = 5.0f;
        [SerializeField] private float cargoCollectTriggerDistance = 0.28f;

        [Header("Carts")]
        [SerializeField] private bool simpleCenterTruckMode = true;
        [SerializeField] private float cartVerticalSpacing = 2.95f;
        [SerializeField] private float cartCenterOffsetX = 0f;
        [SerializeField] private float cartHeightY = 0.03f; // Kasa yüksekliği %40 azaltıldı (eski: 0.05f)

        [Header("Cargo Slot Layout (in cart)")]
        [SerializeField] private Vector3 inSlotCargoScale = new Vector3(0.18f, 0.18f, 0.18f);
        [SerializeField] private float inSlotColumnStep = 0.38f;
        [SerializeField] private float inSlotRowStep = 0.36f;
        [SerializeField] private float cubeStackStep = 0.17f;
        [SerializeField] private Vector3 cargoSlotLocalOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private float importedCargoSlotHeightRatio = 0.34f;
        [SerializeField] private bool addAccentFlagOnImportedModel = true;
        [SerializeField] private int columnCapacityPerCart = 6;

        [Header("Cart Grid (2x8 per cart = 16 slot)")]
        [SerializeField] private float cartGridWidth = 0.88f;     // X yÃ¶nÃ¼ (2 sÃ¼tun toplam geniÅŸlik)
        [SerializeField] private float cartGridDepth = 1.55f;     // Z yÃ¶nÃ¼ (8 satÄ±r toplam derinlik) - wagon iÃ§ine sÄ±ÄŸar
        [SerializeField] private Vector3 slotBlockSize = new Vector3(0.29f, 0.22f, 0.19f);
        [SerializeField] private int particlesPerTap = 8;         // Her slot release = N kÃ¼Ã§Ã¼k partikÃ¼l
        [SerializeField] private int slotFillThreshold = 8;       // N partikÃ¼l birikince slot full olur
        [SerializeField] private float particleSize = 0.16f;      // Yol partikÃ¼lleri gÃ¶rÃ¼nÃ¼r boyut (yumuÅŸak kÃ¼re)
        [SerializeField] private float particleBurstStagger = 0.035f; // YoÄŸun stream iÃ§in kÄ±sa stagger
        [SerializeField] private float particleSpacing = 0.26f;   // Ä°niÅŸ sonrasÄ± birbirine yapÄ±ÅŸmasÄ±n

        [Header("Boosters")]
        [SerializeField] private int undoStartCount = 3;
        [SerializeField] private int shuffleStartCount = 3;
        [SerializeField] private int extraSlotStartCount = 3;

        // Public getters for CargoCartView
        public Vector3 CartModelLocalOffset { get { return cartModelLocalOffset; } }
        public Vector3 CartModelLocalEuler { get { return cartModelLocalEuler; } }
        public Vector3 CartModelScale { get { return cartModelScale; } }
        public float CartModelTargetSize { get { return cartModelTargetSize; } }
        public Vector3 InSlotCargoScale { get { return inSlotCargoScale; } }
        public Vector3 CubeStackScale { get { return inSlotCargoScale; } }
        public float CubeStackStep { get { return cubeStackStep; } }
        public float InSlotColumnStep { get { return inSlotColumnStep; } }
        public float InSlotRowStep { get { return inSlotRowStep; } }
        public Vector3 CargoSlotLocalOffset { get { return cargoSlotLocalOffset; } }
        public float ImportedCargoSlotHeightRatio { get { return importedCargoSlotHeightRatio; } }
        public bool AddAccentFlagOnImportedModel { get { return addAccentFlagOnImportedModel; } }
        public int ColumnCapacityPerCart { get { return columnCapacityPerCart; } }
        public float CartGridWidth { get { return cartGridWidth; } }
        public float CartGridDepth { get { return cartGridDepth; } }
        public float CartHeightY { get { return cartHeightY; } } // Tır kasa yüksekliği
        public Vector3 SlotBlockSize { get { return slotBlockSize; } }
        public int ParticlesPerTap { get { return particlesPerTap; } }
        public int SlotFillThreshold { get { return slotFillThreshold; } }
        public float ParticleSize { get { return particleSize; } }

        // ----- Runtime -----
        private readonly LoopPath path = new LoopPath();
        private readonly List<RuntimeLevel> levels = new List<RuntimeLevel>();
        private readonly List<CargoCartView> carts = new List<CargoCartView>();
        private readonly List<ActiveCargo> activeCargo = new List<ActiveCargo>();
        private readonly List<ReleaseRecord> releaseHistory = new List<ReleaseRecord>();
        private readonly List<ColorZone> colorZones = new List<ColorZone>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly Dictionary<CargoColor, Material> cargoMaterials = new Dictionary<CargoColor, Material>();
        private readonly HashSet<CargoCartView> completedCarts = new HashSet<CargoCartView>();
        private readonly Dictionary<CargoCartView, TruckExitRoute> truckExitRoutes = new Dictionary<CargoCartView, TruckExitRoute>();
        private readonly List<float> cartPickupDistances = new List<float>(); // Her cart iÃ§in path Ã¼stÃ¼ndeki en yakÄ±n nokta
        private readonly List<AnimatedFlowMarker> flowMarkers = new List<AnimatedFlowMarker>(); // Yolda akan oklar

        [Header("Road Flow Animation")]
        [SerializeField] private int flowMarkerCount = 16;
        [SerializeField] private float flowMarkerSpeed = 1.8f;

        private sealed class AnimatedFlowMarker
        {
            public Transform Root;
            public float PathDistance;
        }

        private Transform runtimeRoot;
        private Transform trackRoot;
        private Transform cartRoot;
        private Transform cargoRoot;
        private Transform targetRoot;
        private Transform fxRoot;
        private Camera mainCamera;
        private RuntimeLevel currentLevel;

        // UI
        private Text levelLabel;
        private Text progressLabel;
        private Text capacityLabel;
        private Text stateLabel;
        private Text undoBadge;
        private Text shuffleBadge;
        private Text extraBadge;
        private GameObject winPanel;
        private GameObject losePanel;

        private int clearedCount;
        private int currentLevelIndex;
        private int maxLoopCapacity;
        private int lastCartReleaseFrame = -1;
        private int undoCount;
        private int shuffleCount;
        private int extraSlotCount;
        private GameState state = GameState.Loading;
        private Mesh roundedCargoMesh;
        private Coroutine autoNextLevelCoroutine;

        private sealed class TruckExitRoute
        {
            public Vector3 Start;
            public Vector3 PortalMouth;
            public Vector3 PortalInside;
            public GameObject Portal;
        }

        public GameState State { get { return state; } }

        // ============================================================
        // Lifecycle
        // ============================================================
        private void Start()
        {
            if (buildOnStart)
            {
                BuildPrototype();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!useImportedCartModel || cartModelPrefab != null)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("Purple_Toy_Wagon t:GameObject");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("Meshy_AI_Purple_Toy_Wagon t:GameObject");
            }

            if (guids.Length > 0)
            {
                cartModelAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Update()
        {
            // Akan yol animasyonu (Lost/Won state'inde de aksÄ±n - gÃ¶rsel)
            UpdateFlowMarkers();

            if (state != GameState.Playing)
            {
                return;
            }

            HandlePrototypeTap();
            TickCargo();
            CheckSolvedCartsForDeparture();
            CheckLoseByNoMoves();
        }

        // ============================================================
        // Public API (sahne / dÄ±ÅŸ buton referanslarÄ± iÃ§in)
        // ============================================================
        public void BuildPrototype()
        {
            state = GameState.Loading;
            levels.Clear();
            levels.AddRange(RuntimeLevel.CreateDefaultLevels());

            CreateRuntimeRoot();
            SetupCameraAndLighting();
            // UI geÃ§ici olarak kapalÄ± (kullanÄ±cÄ± tercihi - temiz gÃ¶rÃ¼nÃ¼m)
            // BuildUi();
            LoadLevel(Mathf.Clamp(startLevelIndex, 1, levels.Count));
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        public void NextLevel()
        {
            int nextLevel = currentLevelIndex + 1;
            if (nextLevel > levels.Count)
            {
                nextLevel = 1;
            }

            LoadLevel(nextLevel);
        }

        public void TryReleaseFromCart(CargoCartView cart)
        {
            if (state != GameState.Playing || cart == null) return;

            if (lastCartReleaseFrame == Time.frameCount) return;
            lastCartReleaseFrame = Time.frameCount;

            var released = cart.ReleaseAllFront();
            if (released.Count == 0) return;

            StartCoroutine(SpawnFrontSequential(cart, released));
        }

        private IEnumerator SpawnFrontSequential(CargoCartView cart, List<CargoCartView.ReleasedCube> released)
        {
            // Artık partikül yağdırmıyoruz, tırda duran BÜYÜK KÜPLERİ tek tek fırlatacağız
            for (int i = 0; i < released.Count; i++)
            {
                if (state != GameState.Playing) yield break;
                var r = released[i];

                // Tırın içindeki slotun dünya pozisyonunu al
                Vector3 slotWorldPos = cart.GetSlotWorldPosition(r.SlotIndex);
                
                // Tırın içindeki "FullVisual" (büyük küp) objesini bulmaya çalış
                // CargoCartView içindeki Slot yapısında FullVisual tutuluyor ama public değil
                // Bu yüzden GetSlotWorldPosition'dan gelen yerden bir küp spawn edip oradan fırlatacağız
                // YA DA daha iyisi: CargoCartView'a bir metod ekleyip o görseli alacağız.
                // Şimdilik mevcut yapıda en temiz çözüm: 
                // Tırın slot pozisyonunda yeni bir "Büyük Küp" oluşturup onu fırlatmak.
                // Ama sen "tırda zaten duranlar uçsun" dedin. 
                // O halde CargoCartView içindeki FullVisual'ı erişilebilir yapmalıyız veya oradan hareket ettirmeliyiz.
                
                // En garanti yöntem: Tırın slotundaki görseli (varsa) al, yoksa spawn et.
                // CargoCartView.Slots[r.SlotIndex].FullVisual -> bu private olduğu için erişemiyoruz.
                // Bu yüzden CargoCartView'a bir getter eklememiz lazım.
                
                // HIZLI ÇÖZÜM: Mevcut sistemi "Büyük Küp" mantığına çeviriyoruz.
                // SpawnParticleFromSlot yerine direkt Büyük Küp fırlatan bir korutin yazıyoruz.
                
                Vector3 exitOrigin = slotWorldPos + Vector3.up * 0.2f + Vector3.back * 0.1f;
                float dockDist = FindNearestPathDistance(exitOrigin);

                // Büyük küpü yarat ve fırlat
                SpawnBigCargoFromCart(r.Color, cart, r.SlotIndex, exitOrigin, dockDist);

                // Sıradaki küp için kısa bekleme (akıcı döküm efekti)
                yield return new WaitForSeconds(0.15f);
            }
        }

        private void SpawnBigCargoFromCart(CargoColor color, CargoCartView sourceCart, int sourceSlotIndex, Vector3 startPosition, float entryDistance)
        {
            // Yoldaki normal küp boyutunda (roadCargoScale) bir küp oluştur
            GameObject cargoObj = CreateCargoVisual(color, roadCargoScale);
            cargoObj.transform.SetParent(cargoRoot, false);
            cargoObj.transform.position = startPosition;
            cargoObj.transform.rotation = Quaternion.identity;

            var active = new ActiveCargo
            {
                Color = color,
                Visual = cargoObj,
                Distance = entryDistance,
                PreviousDistance = entryDistance,
                IsEnteringRoad = true,
                EntryDistance = entryDistance,
                FlyStart = startPosition,
                FlyTarget = GetCargoRoadPosition(entryDistance),
                FlyProgress = 0f,
                BaseScale = roadCargoScale, // Normal küp boyutu
                SourceCart = sourceCart,
                SourceColumn = sourceSlotIndex,
                TumbleAxis = Random.onUnitSphere,
                TumbleSpeed = Random.Range(180f, 360f),
                LaneOffset = Random.Range(-0.1f, 0.1f), // Daha az saçılma
                VerticalOffset = 0f
            };
            activeCargo.Add(active);
            releaseHistory.Add(new ReleaseRecord(sourceCart, sourceSlotIndex, active, color));

            if (activeCargo.Count > maxLoopCapacity)
            {
                Lose();
            }
            UpdateHud();
        }

        private GameObject CreateCargoVisual(CargoColor color, Vector3 scale)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(obj.GetComponent<Collider>());
            obj.name = "Cargo_" + color.ToString();
            
            Material mat;
            if (!cargoMaterials.TryGetValue(color, out mat))
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = GetColorValue(color);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.4f);
                cargoMaterials[color] = mat;
            }
            
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            obj.transform.localScale = scale;
            return obj;
        }

        private void SpawnParticleFromSlot(CargoColor color, CargoCartView sourceCart, int sourceSlotIndex, Vector3 startPosition, float entryDistance)
        {
            GameObject p = BuildParticleVisual(color);
            p.transform.SetParent(cargoRoot, false);
            p.transform.position = startPosition + new Vector3(Random.Range(-0.025f, 0.025f), Random.Range(0f, 0.045f), Random.Range(-0.025f, 0.025f));
            p.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            var active = new ActiveCargo
            {
                Color = color,
                Visual = p,
                Distance = entryDistance,
                PreviousDistance = entryDistance,
                IsEnteringRoad = true,
                EntryDistance = entryDistance,
                FlyStart = p.transform.position,
                FlyTarget = GetCargoRoadPosition(entryDistance),
                FlyProgress = 0f,
                BaseScale = p.transform.localScale,
                SourceCart = sourceCart,
                SourceColumn = sourceSlotIndex,
                TumbleAxis = Random.onUnitSphere,
                TumbleSpeed = Random.Range(220f, 380f),
                LaneOffset = Random.Range(-0.32f, 0.32f),
                VerticalOffset = Random.Range(0f, 0.18f)
            };
            activeCargo.Add(active);
            releaseHistory.Add(new ReleaseRecord(sourceCart, sourceSlotIndex, active, color));

            if (activeCargo.Count > maxLoopCapacity)
            {
                Lose();
            }
            UpdateHud();
        }

        private void SpawnParticle(CargoColor color, CargoCartView sourceCart, int sourceSlotIndex, Vector3 startPosition, float entryDistance)
        {
            GameObject p = BuildParticleVisual(color);
            p.transform.SetParent(cargoRoot, false);
            p.transform.position = startPosition + new Vector3(Random.Range(-0.025f, 0.025f), Random.Range(0f, 0.045f), Random.Range(-0.025f, 0.025f));
            p.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            var active = new ActiveCargo
            {
                Color = color,
                Visual = p,
                Distance = entryDistance,
                PreviousDistance = entryDistance,
                IsEnteringRoad = true,
                EntryDistance = entryDistance,
                FlyStart = p.transform.position,
                FlyTarget = GetCargoRoadPosition(entryDistance),
                FlyProgress = 0f,
                BaseScale = p.transform.localScale,
                SourceCart = sourceCart,
                SourceColumn = sourceSlotIndex,
                // Her kÃ¼p kendine has yumuÅŸak tumble eksen ve hÄ±zÄ±
                TumbleAxis = Random.onUnitSphere,
                TumbleSpeed = Random.Range(220f, 380f),
                // YÄ±ÄŸÄ±n iÃ§in rastgele yan ve yÃ¼kseklik offset
                LaneOffset = Random.Range(-0.32f, 0.32f),
                VerticalOffset = Random.Range(0f, 0.18f)
            };
            activeCargo.Add(active);
            releaseHistory.Add(new ReleaseRecord(sourceCart, sourceSlotIndex, active, color));

            if (activeCargo.Count > maxLoopCapacity)
            {
                Lose();
            }
            UpdateHud();
        }

        private GameObject BuildParticleVisual(CargoColor color)
        {
            // Cube primitive - referansa benzer kare gÃ¶rÃ¼nÃ¼m
            // Hafif rastgele scale â†’ organik / yumuÅŸak hissi
            GameObject p = CreateCargoBlockObject(color, "Particle_" + color);
            float s = particleSize * Random.Range(0.88f, 1.12f);
            Vector3 baseScale = roadCargoScale;
            if (baseScale.sqrMagnitude < 0.001f)
            {
                baseScale = new Vector3(s, s * 0.85f, s);
            }
            p.transform.localScale = Vector3.Scale(baseScale, Vector3.one * Random.Range(0.92f, 1.12f));
            return p;
        }

        public void UndoLast()
        {
            if (state != GameState.Playing || undoCount <= 0) return;

            // Undo: en son boÅŸaltÄ±lan slot'u tekrar full yap + o partikÃ¼lleri loop'tan kaldÄ±r
            // Geri Ã§ekme tÃ¼m burst iÃ§in: aynÄ± SourceCart+SourceColumn'a sahip aktif partikÃ¼lleri kaldÄ±r.
            ReleaseRecord lastRecord = null;
            for (int i = releaseHistory.Count - 1; i >= 0; i--)
            {
                if (releaseHistory[i].Cargo != null && activeCargo.Contains(releaseHistory[i].Cargo))
                {
                    lastRecord = releaseHistory[i];
                    break;
                }
            }
            if (lastRecord == null) return;

            // AynÄ± kaynak slot'tan Ã§Ä±kan tÃ¼m aktif partikÃ¼lleri kaldÄ±r
            for (int i = activeCargo.Count - 1; i >= 0; i--)
            {
                ActiveCargo c = activeCargo[i];
                if (c.SourceCart == lastRecord.SourceCart && c.SourceColumn == lastRecord.SourceColumn)
                {
                    if (c.Visual != null) Destroy(c.Visual);
                    activeCargo.RemoveAt(i);
                }
            }
            // Release history'den kaynaklÄ± kayÄ±tlarÄ± temizle
            for (int i = releaseHistory.Count - 1; i >= 0; i--)
            {
                ReleaseRecord r = releaseHistory[i];
                if (r.SourceCart == lastRecord.SourceCart && r.SourceColumn == lastRecord.SourceColumn)
                {
                    releaseHistory.RemoveAt(i);
                }
            }
            // Slot'u eski rengiyle full yap
            lastRecord.SourceCart.PushColorIntoSlot(lastRecord.SourceColumn, lastRecord.Color);
            undoCount--;
            UpdateHud();
        }

        public void ShuffleCarts()
        {
            if (state != GameState.Playing || shuffleCount <= 0)
            {
                return;
            }

            for (int i = 0; i < carts.Count; i++)
            {
                carts[i].Shuffle();
            }

            shuffleCount--;
            UpdateHud();
        }

        public void AddCapacity()
        {
            if (state != GameState.Playing || extraSlotCount <= 0)
            {
                return;
            }

            maxLoopCapacity += 1;
            extraSlotCount--;
            UpdateHud();
        }

        public Material GetCargoMaterial(CargoColor color)
        {
            Material material;
            if (!cargoMaterials.TryGetValue(color, out material))
            {
                material = GetRuntimeMaterial("Cargo_" + color, CargoColorPalette.ToColor(color));
                cargoMaterials[color] = material;
            }

            return material;
        }

        public GameObject CreateCargoBlockObject(CargoColor color, string objectName)
        {
            GameObject block = new GameObject(objectName);
            MeshFilter meshFilter = block.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetRoundedCargoMesh();

            MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetCargoMaterial(color);
            return block;
        }

        private Mesh GetRoundedCargoMesh()
        {
            if (roundedCargoMesh != null)
            {
                return roundedCargoMesh;
            }

            const int steps = 5;
            const float half = 0.5f;
            const float radius = 0.16f;
            const float inner = half - radius;

            var verts = new List<Vector3>();
            var tris = new List<int>();

            AddRoundedFace(verts, tris, Vector3.forward, Vector3.right, Vector3.up, steps, half, inner, radius);
            AddRoundedFace(verts, tris, Vector3.back, Vector3.left, Vector3.up, steps, half, inner, radius);
            AddRoundedFace(verts, tris, Vector3.right, Vector3.back, Vector3.up, steps, half, inner, radius);
            AddRoundedFace(verts, tris, Vector3.left, Vector3.forward, Vector3.up, steps, half, inner, radius);
            AddRoundedFace(verts, tris, Vector3.up, Vector3.right, Vector3.back, steps, half, inner, radius);
            AddRoundedFace(verts, tris, Vector3.down, Vector3.right, Vector3.forward, steps, half, inner, radius);

            roundedCargoMesh = new Mesh();
            roundedCargoMesh.name = "RoundedCargoBlock";
            roundedCargoMesh.SetVertices(verts);
            roundedCargoMesh.SetTriangles(tris, 0);
            roundedCargoMesh.RecalculateNormals();
            roundedCargoMesh.RecalculateBounds();
            return roundedCargoMesh;
        }

        private static void AddRoundedFace(
            List<Vector3> verts,
            List<int> tris,
            Vector3 normal,
            Vector3 axisA,
            Vector3 axisB,
            int steps,
            float half,
            float inner,
            float radius)
        {
            int start = verts.Count;
            for (int y = 0; y <= steps; y++)
            {
                float v = Mathf.Lerp(-half, half, y / (float)steps);
                for (int x = 0; x <= steps; x++)
                {
                    float u = Mathf.Lerp(-half, half, x / (float)steps);
                    Vector3 p = normal * half + axisA * u + axisB * v;
                    Vector3 clamped = new Vector3(
                        Mathf.Clamp(p.x, -inner, inner),
                        Mathf.Clamp(p.y, -inner, inner),
                        Mathf.Clamp(p.z, -inner, inner));
                    Vector3 fromCore = p - clamped;
                    if (fromCore.sqrMagnitude > 0.000001f)
                    {
                        p = clamped + fromCore.normalized * radius;
                    }
                    verts.Add(p);
                }
            }

            int row = steps + 1;
            for (int y = 0; y < steps; y++)
            {
                for (int x = 0; x < steps; x++)
                {
                    int a = start + y * row + x;
                    int b = a + 1;
                    int c = a + row;
                    int d = c + 1;
                    Vector3 triNormal = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                    if (Vector3.Dot(triNormal, normal) >= 0f)
                    {
                        tris.Add(a);
                        tris.Add(b);
                        tris.Add(c);
                        tris.Add(b);
                        tris.Add(d);
                        tris.Add(c);
                    }
                    else
                    {
                        tris.Add(a);
                        tris.Add(c);
                        tris.Add(b);
                        tris.Add(b);
                        tris.Add(c);
                        tris.Add(d);
                    }
                }
            }
        }

        public Material GetRuntimeMaterial(string key, Color color)
        {
            Material material;
            if (materials.TryGetValue(key, out material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            material.name = "MAT_Runtime_" + key;
            material.color = color;
            material.SetFloat("_Smoothness", 0.7f);
            material.SetFloat("_Metallic", 0.05f);
            materials.Add(key, material);
            return material;
        }

        public Material GetEmissiveMaterial(string key, Color baseColor, Color emissionColor, float intensity = 1.5f)
        {
            Material material;
            if (materials.TryGetValue(key, out material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            material.name = "MAT_Runtime_" + key;
            material.color = baseColor;
            material.SetFloat("_Smoothness", 0.9f);
            material.SetFloat("_Metallic", 0.0f);

            // Emission - neon glow effect
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            materials.Add(key, material);
            return material;
        }

        // ============================================================
        // Input
        // ============================================================
        private void HandlePrototypeTap()
        {
            if (!WasPrimaryTapPressed())
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            CargoCartView tappedCart = RaycastCart();
            if (tappedCart != null)
            {
                TryReleaseFromCart(tappedCart);
            }
        }

        private bool WasPrimaryTapPressed()
        {
            bool pressed = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetMouseButtonDown(0);
#endif

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pressed |= Mouse.current.leftButton.wasPressedThisFrame;
            }

            if (Touchscreen.current != null)
            {
                pressed |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
#endif

            return pressed;
        }

        private CargoCartView RaycastCart()
        {
            Camera camera = mainCamera != null ? mainCamera : Camera.main;
            if (camera == null)
            {
                return null;
            }

            Vector2 screenPosition = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = Input.mousePosition;
#endif
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif

            Ray ray = camera.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                return hit.collider.GetComponentInParent<CargoCartView>();
            }

            return null;
        }

        // ============================================================
        // Cargo lifecycle (yeni: TryReleaseFromCart -> SpawnParticleBurst -> SpawnParticle)
        // ============================================================
        private float ReserveEntryDistance(float requestedDistance)
        {
            // Mevcut cargolarla Ã§akÄ±ÅŸÄ±yorsa biraz geri Ã§ek
            for (int safety = 0; safety < 12; safety++)
            {
                bool clash = false;
                for (int i = 0; i < activeCargo.Count; i++)
                {
                    ActiveCargo other = activeCargo[i];
                    if (other.IsEnteringRoad)
                    {
                        continue;
                    }

                    if (Mathf.Abs(SignedPathDelta(other.Distance, requestedDistance)) < cargoPathSpacing)
                    {
                        clash = true;
                        break;
                    }
                }

                if (!clash)
                {
                    return requestedDistance;
                }

                requestedDistance -= cargoPathSpacing;
            }

            return requestedDistance;
        }

        private float SignedPathDelta(float a, float b)
        {
            float diff = b - a;
            float total = path.TotalLength;
            if (total <= 0f)
            {
                return diff;
            }

            diff = Mathf.Repeat(diff + total * 0.5f, total) - total * 0.5f;
            return diff;
        }

        private void TickCargo()
        {
            for (int i = activeCargo.Count - 1; i >= 0; i--)
            {
                ActiveCargo cargo = activeCargo[i];

                // Her state'te yumuÅŸak tumble rotation (sÃ¼rekli akÄ±ÅŸ hissi)
                if (cargo.Visual != null)
                {
                    cargo.Visual.transform.Rotate(cargo.TumbleAxis, cargo.TumbleSpeed * Time.deltaTime, Space.World);
                }

                if (cargo.IsEnteringRoad)
                {
                    cargo.FlyProgress += Time.deltaTime * cargoEnterFlightSpeed;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cargo.FlyProgress));
                    Vector3 arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.65f;
                    cargo.Visual.transform.position = Vector3.Lerp(cargo.FlyStart, cargo.FlyTarget, t) + arc;

                    if (t >= 1f)
                    {
                        cargo.IsEnteringRoad = false;
                        // YÄ±ÄŸÄ±n efekti: tÃ¼m cubes aynÄ± dock distance'a iner, lateral offset ile yayÄ±lÄ±rlar
                        cargo.Distance = cargo.EntryDistance;
                        cargo.PreviousDistance = cargo.Distance;
                        cargo.Age = 0f;
                    }

                    continue;
                }

                if (cargo.IsCollecting)
                {
                    cargo.FlyProgress += Time.deltaTime * cargoCollectSpeed;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cargo.FlyProgress));
                    Vector3 arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.24f;
                    cargo.Visual.transform.position = Vector3.Lerp(cargo.FlyStart, cargo.FlyTarget, t) + arc;
                    cargo.Visual.transform.localScale = cargo.BaseScale;
                    cargo.Visual.transform.Rotate(cargo.TumbleAxis, cargo.TumbleSpeed * 0.5f * Time.deltaTime, Space.World);

                    if (t >= 1f)
                    {
                        CollectCargo(cargo);
                        activeCargo.RemoveAt(i);
                    }

                    continue;
                }

                // Yol Ã¼stÃ¼ hareket - SABÄ°T hÄ±z, hiÃ§ yavaÅŸlama yok (yol akÄ±yor hissi)
                cargo.Age += Time.deltaTime;
                cargo.PreviousDistance = cargo.Distance;
                cargo.Distance += currentLevel.CargoMoveSpeed * Time.deltaTime;

                // YÄ±ÄŸÄ±n efekti: lateral + vertical offset uygulanÄ±r
                Vector3 position = GetCargoRoadPosition(cargo.Distance, cargo.LaneOffset, cargo.VerticalOffset);
                cargo.Visual.transform.position = position;
                // Rotation override yok - tumble dÃ¶nmeye devam eder, doÄŸal akÄ±ÅŸ hissi

                // Pickup noktasÄ±ndan geÃ§ti mi? GeÃ§tiyse o tÄ±ra uygunluk kontrolÃ¼.
                if (cargo.Age > 0.05f)
                {
                    CargoCartView destCart;
                    int destSlot;
                    Vector3 destWorld;
                    if (TryFindOpenSlot(cargo, out destCart, out destSlot, out destWorld))
                    {
                        cargo.IsCollecting = true;
                        cargo.FlyProgress = 0f;
                        cargo.FlyStart = cargo.Visual.transform.position;
                        cargo.FlyTarget = destWorld + Vector3.up * 0.10f;
                        cargo.DestinationCart = destCart;
                        cargo.DestinationColumn = destSlot;
                    }
                }
            }
        }

        // Iterative push-back: place behind any conflicting cube until no overlap
        private float ResolveLandingDistance(float requestedDistance, int selfIndex)
        {
            float minGap = particleSpacing;
            float candidate = requestedDistance;

            for (int iter = 0; iter < 40; iter++)
            {
                bool conflict = false;
                for (int j = 0; j < activeCargo.Count; j++)
                {
                    if (j == selfIndex) continue;
                    ActiveCargo other = activeCargo[j];
                    if (other.IsEnteringRoad || other.IsCollecting) continue;

                    float diff = SignedPathDelta(candidate, other.Distance);
                    // Other is AHEAD of candidate within minGap â†’ push candidate behind
                    if (diff > 0f && diff < minGap)
                    {
                        candidate = other.Distance - minGap;
                        conflict = true;
                        break;
                    }
                    // Other is AT or just behind candidate (very close) â†’ push back
                    if (diff <= 0f && diff > -0.001f)
                    {
                        candidate = other.Distance - minGap;
                        conflict = true;
                        break;
                    }
                }
                if (!conflict) break;
            }
            return candidate;
        }

        private bool TryFindOpenSlot(ActiveCargo cargo, out CargoCartView destCart, out int destSlot, out Vector3 destWorld)
        {
            destCart = null;
            destSlot = -1;
            destWorld = Vector3.zero;

            // KURAL: KÃ¼p ancak bir tÄ±rÄ±n PICKUP noktasÄ±ndan geÃ§tiÄŸi frame'de o tÄ±ra inebilir.
            // Yani kÃ¼p loop'ta dolaÅŸÄ±r, tÄ±rÄ±n yanÄ±na geldiÄŸinde "uygun mu" kontrolÃ¼ olur,
            // uygun deÄŸilse devam eder, sonraki tÄ±rÄ±n yanÄ±na gelince yeniden kontrol.
            for (int i = 0; i < carts.Count && i < cartPickupDistances.Count; i++)
            {
                float pickup = cartPickupDistances[i];
                // Bu frame'de pickup noktasÄ±nÄ± geÃ§ti mi?
                if (!path.DidCross(cargo.PreviousDistance, cargo.Distance, pickup)) continue;

                CargoCartView candidate = carts[i];
                if (completedCarts.Contains(candidate)) continue;
                int slotIndex;
                Vector3 worldPos;
                if (candidate.TryFindOpenSlot(cargo.Color, out slotIndex, out worldPos))
                {
                    destCart = candidate;
                    destSlot = slotIndex;
                    destWorld = worldPos;
                    return true;
                }
            }
            return false;
        }

        private bool TryFindMatchingZone(ActiveCargo cargo, out ColorZone zone)
        {
            for (int i = 0; i < colorZones.Count; i++)
            {
                ColorZone candidate = colorZones[i];
                if (candidate.Color != cargo.Color)
                {
                    continue;
                }

                if (path.DidCross(cargo.PreviousDistance, cargo.Distance, candidate.PathDistance))
                {
                    zone = candidate;
                    return true;
                }
            }

            zone = null;
            return false;
        }

        private Vector3 GetCargoRoadPosition(float distance)
        {
            // No offset version - used for FlyTarget calc (dock center)
            Vector3 center = path.GetPosition(distance);
            return center + Vector3.up * 0.28f;
        }

        private Vector3 GetCargoRoadPosition(float distance, float laneOffset, float verticalOffset)
        {
            // Lane-spread version - cubes scatter as pile not single line
            Vector3 center = path.GetPosition(distance);
            Vector3 forward = path.GetForward(distance);
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            return center + side * laneOffset + Vector3.up * (0.18f + verticalOffset);
        }

        private float FindNearestPathDistance(Vector3 worldPosition)
        {
            float best = 0f;
            float bestSqr = float.PositiveInfinity;
            int samples = 64;
            for (int i = 0; i < samples; i++)
            {
                float d = path.TotalLength * i / samples;
                Vector3 p = path.GetPosition(d);
                float sqr = (p - worldPosition).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = d;
                }
            }

            return best;
        }

        private void CollectCargo(ActiveCargo cargo)
        {
            clearedCount++;
            bool slotJustBecameFull = false;
            if (cargo.DestinationCart != null && cargo.DestinationColumn >= 0)
            {
                slotJustBecameFull = cargo.DestinationCart.AddParticleToSlot(cargo.DestinationColumn, cargo.Color);
            }
            if (slotJustBecameFull)
            {
                if (cargo.DestinationCart != null && !completedCarts.Contains(cargo.DestinationCart))
                {
                    CargoColor solvedColor;
                    if (cargo.DestinationCart.IsCartFullSingleColor(out solvedColor))
                    {
                        StartCoroutine(DepartSolvedCart(cargo.DestinationCart));
                    }
                }
            }
            Destroy(cargo.Visual);
            UpdateHud();
        }

        private bool AreAllCartsSolved()
        {
            if (carts.Count == 0) return false;
            for (int i = 0; i < carts.Count; i++)
            {
                CargoColor c;
                if (!carts[i].IsCartFullSingleColor(out c)) return false;
            }
            return true;
        }

        private void CheckSolvedCartsForDeparture()
        {
            if (state != GameState.Playing) return;

            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                if (cart == null || completedCarts.Contains(cart)) continue;

                CargoColor solvedColor;
                if (cart.IsCartFullSingleColor(out solvedColor))
                {
                    StartCoroutine(DepartSolvedCart(cart));
                }
            }
        }

        private IEnumerator DepartSolvedCart(CargoCartView cart)
        {
            if (cart == null || completedCarts.Contains(cart)) yield break;
            completedCarts.Add(cart);

            Collider collider = cart.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            Vector3 start = cart.transform.position;
            TruckExitRoute route;
            if (!truckExitRoutes.TryGetValue(cart, out route) || route == null)
            {
                route = new TruckExitRoute
                {
                    Start = start,
                    PortalMouth = start + Vector3.right * 1.35f,
                    PortalInside = start + Vector3.right * 2.1f
                };
            }

            // Tırı portal ağzına kadar akıcı hareket ettir (tek seferde, kesintisiz)
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.8f; // Hızlı ve akıcı hareket
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                cart.transform.position = Vector3.Lerp(start, route.PortalMouth, eased);
                cart.transform.rotation = Quaternion.identity;
                
                // Tır portal ağzına yaklaşırken fade-out başlat (son %30'da)
                float distToPortal = Vector3.Distance(cart.transform.position, route.PortalMouth);
                float totalDist = Vector3.Distance(start, route.PortalMouth);
                float fadeStartRatio = 0.35f; // Portal'a uzaklığın %35'inde başla
                float tunnelAlpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((fadeStartRatio - (distToPortal / totalDist)) / fadeStartRatio));
                cart.SetTunnelFade(tunnelAlpha);
                
                yield return null;
            }

            // Tır portal ağzında tamamen görünmez olana kadar bekle
            cart.SetTunnelFade(0f);
            
            // Hemen popup yap ve yok et (tünel içine girmeden)
            cart.gameObject.SetActive(false);
            UpdateHud();

            if (completedCarts.Count >= carts.Count)
            {
                Win();
            }
        }

        private void CheckLoseByNoMoves()
        {
            if (state != GameState.Playing || activeCargo.Count > 0 || carts.Count == 0)
            {
                return;
            }

            if (completedCarts.Count >= carts.Count || AreAllCartsSolved())
            {
                return;
            }

            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                if (cart == null || completedCarts.Contains(cart)) continue;
                if (cart.HasReleasableFrontGroup())
                {
                    return;
                }
            }

            Lose();
        }

        // ============================================================
        // Level / world build
        // ============================================================
        private void LoadLevel(int levelIndex)
        {
            if (autoNextLevelCoroutine != null)
            {
                StopCoroutine(autoNextLevelCoroutine);
                autoNextLevelCoroutine = null;
            }

            currentLevelIndex = Mathf.Clamp(levelIndex, 1, levels.Count);
            currentLevel = levels[currentLevelIndex - 1];
            maxLoopCapacity = currentLevel.MaxLoopCapacity;
            clearedCount = 0;
            releaseHistory.Clear();
            undoCount = undoStartCount;
            shuffleCount = shuffleStartCount;
            extraSlotCount = extraSlotStartCount;

            ClearChildren(cartRoot);
            ClearChildren(cargoRoot);
            ClearChildren(targetRoot);
            ClearChildren(fxRoot);
            carts.Clear();
            activeCargo.Clear();
            colorZones.Clear();
            completedCarts.Clear();
            truckExitRoutes.Clear();
            cartPickupDistances.Clear();

            BuildPathAndTrack();
            BuildCarts(currentLevel);

            // Her cart iÃ§in path Ã¼stÃ¼ndeki "pickup noktasÄ±" hesapla
            // KÃ¼p ancak bu noktadan geÃ§ince landing kontrolÃ¼ yapÄ±lÄ±r
            for (int i = 0; i < carts.Count; i++)
            {
                float d = FindNearestPathDistance(carts[i].transform.position);
                cartPickupDistances.Add(d);
            }

            state = GameState.Playing;
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (stateLabel != null) stateLabel.gameObject.SetActive(false);
            UpdateHud();
        }

        private void Win()
        {
            state = GameState.Won;
            if (stateLabel != null)
            {
                stateLabel.text = "Seviye Tamam!";
                stateLabel.gameObject.SetActive(true);
            }
            if (winPanel != null) winPanel.SetActive(true);

            if (autoNextLevelCoroutine == null)
            {
                autoNextLevelCoroutine = StartCoroutine(AutoNextLevelAfterWin());
            }
        }

        private IEnumerator AutoNextLevelAfterWin()
        {
            yield return new WaitForSeconds(0.85f);
            autoNextLevelCoroutine = null;
            NextLevel();
        }

        private void Lose()
        {
            state = GameState.Lost;
            Debug.LogWarning("[CCL] LOSE triggered! activeCargo=" + activeCargo.Count + " maxLoopCapacity=" + maxLoopCapacity);
            if (stateLabel != null)
            {
                stateLabel.text = "Loop Doldu!";
                stateLabel.gameObject.SetActive(true);
            }
            if (losePanel != null) losePanel.SetActive(true);
        }

        private void BuildCarts(RuntimeLevel level)
        {
            GameObject resolvedCartModel = ResolveCartModelPrefab();
            int cartCount = level.Carts.Count;
            if (cartCount == 0) return;

            // Coklu cart - dikey istif (cart uzun ekseni Z yonunde)
            float totalSpan = (cartCount - 1) * cartVerticalSpacing;
            float startZ = totalSpan * 0.5f;

            for (int i = 0; i < cartCount; i++)
            {
                RuntimeCart cartData = level.Carts[i];
                Vector3 position = new Vector3(cartCenterOffsetX, cartHeightY, startZ - i * cartVerticalSpacing);

                GameObject cartObject = new GameObject("Cart_" + (i + 1));
                cartObject.transform.SetParent(cartRoot, false);
                cartObject.transform.position = position;
                cartObject.transform.rotation = Quaternion.identity;

                BoxCollider collider = cartObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.30f, 0f);
                collider.size = new Vector3(cartGridWidth + 0.5f, 1.0f, cartGridDepth + 0.5f);

                CargoCartView view = cartObject.AddComponent<CargoCartView>();
                Color accentColor = CargoColorPalette.ToColor(cartData.TargetColor);
                view.Initialize(this, i + 1, cartData.TargetColor, cartData.InitialSlotColors, slotFillThreshold, accentColor, resolvedCartModel);
                carts.Add(view);
                BuildTruckExitRoute(view);
            }
        }

        private void BuildTruckExitRoute(CargoCartView cart)
        {
            if (cart == null) return;

            Vector3 basePos = cart.transform.position;
            Bounds cartBounds;
            cart.TryGetVisualBounds(out cartBounds);

            float innerRightLimit = (trackWidthX * 0.5f) - 1.15f;
            float portalX = Mathf.Min(cartBounds.max.x + 0.42f, innerRightLimit);
            if (portalX < cartBounds.max.x + 0.16f)
            {
                portalX = cartBounds.max.x + 0.16f;
            }

            float laneStartX = cartBounds.min.x - 0.08f;
            float laneEndX = portalX + 0.16f;
            float laneLength = Mathf.Max(0.9f, laneEndX - laneStartX);
            float laneWidth = Mathf.Clamp(cartBounds.size.z * 0.78f, 0.62f, 1.06f);
            Vector3 start = new Vector3(laneStartX, cartHeightY, basePos.z);
            Vector3 portalMouth = new Vector3(portalX, cartHeightY + 0.04f, basePos.z);
            Vector3 portalInside = portalMouth + Vector3.right * 1.15f;

            Material laneMat = GetRuntimeMaterial("TruckExitLane", new Color(0.18f, 0.17f, 0.22f));
            Material laneEdgeMat = GetEmissiveMaterial("TruckExitEdge", new Color(0.66f, 0.63f, 0.82f), new Color(0.34f, 0.27f, 0.95f), 0.7f);
            Material portalOuterMat = GetRuntimeMaterial("TruckPortalOuter", new Color(0.58f, 0.46f, 0.92f));
            Material portalInnerMat = GetRuntimeMaterial("TruckPortalInner", new Color(0.055f, 0.045f, 0.085f));

            GameObject routeRoot = new GameObject("TruckExitRoute_" + cart.CartIndex);
            routeRoot.transform.SetParent(trackRoot, false);

            CreateRoundedGroundStrip(
                routeRoot.transform,
                "ExitLane",
                new Vector3(laneStartX + laneLength * 0.5f, basePos.y, basePos.z),
                laneLength,
                laneWidth,
                0.055f,
                laneMat);

            CreateRoundedGroundStrip(
                routeRoot.transform,
                "ExitLaneOuterGlow",
                new Vector3(laneStartX + laneLength * 0.5f, basePos.y, basePos.z),
                laneLength + 0.12f,
                laneWidth + 0.16f,
                0.045f,
                laneEdgeMat);

            routeRoot.transform.Find("ExitLaneOuterGlow").SetAsFirstSibling();

            GameObject portal = new GameObject("TruckPortal_" + cart.CartIndex);
            portal.transform.SetParent(routeRoot.transform, false);
            portal.transform.position = portalMouth + Vector3.up * 0.18f;
            portal.transform.rotation = Quaternion.identity;

            GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outer.name = "PortalOuter";
            outer.transform.SetParent(portal.transform, false);
            outer.transform.localScale = new Vector3(0.24f, 0.42f, laneWidth * 1.26f);
            Destroy(outer.GetComponent<Collider>());
            outer.GetComponent<Renderer>().sharedMaterial = portalOuterMat;

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "PortalOvalTop";
            top.transform.SetParent(portal.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            top.transform.localScale = new Vector3(0.26f, 0.20f, laneWidth * 1.28f);
            Destroy(top.GetComponent<Collider>());
            top.GetComponent<Renderer>().sharedMaterial = portalOuterMat;

            GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inner.name = "PortalDarkMouth";
            inner.transform.SetParent(portal.transform, false);
            inner.transform.localPosition = new Vector3(-0.025f, 0.01f, 0f);
            inner.transform.localScale = new Vector3(0.26f, 0.30f, laneWidth * 0.92f);
            Destroy(inner.GetComponent<Collider>());
            inner.GetComponent<Renderer>().sharedMaterial = portalInnerMat;

            // TunnelRoof kaldırıldı - artık tırın kendi gövdesi tünel gibi davranacak
            // Tır tünele girince giren kısım görünmez olacak (fade-out efekti ile)

            truckExitRoutes[cart] = new TruckExitRoute
            {
                Start = start,
                PortalMouth = portalMouth,
                PortalInside = portalInside,
                Portal = portal
            };
        }

        private void CreateRoundedGroundStrip(Transform parent, string name, Vector3 center, float length, float width, float height, Material material)
        {
            GameObject strip = new GameObject(name);
            strip.transform.SetParent(parent, false);
            strip.transform.position = center + Vector3.up * height;

            GameObject middle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            middle.name = "Middle";
            middle.transform.SetParent(strip.transform, false);
            middle.transform.localScale = new Vector3(Mathf.Max(0.1f, length - width), 0.05f, width);
            Destroy(middle.GetComponent<Collider>());
            middle.GetComponent<Renderer>().sharedMaterial = material;

            GameObject leftCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftCap.name = "RearRoundCap";
            leftCap.transform.SetParent(strip.transform, false);
            leftCap.transform.localPosition = new Vector3(-(length - width) * 0.5f, 0f, 0f);
            leftCap.transform.localScale = new Vector3(width, 0.05f, width);
            Destroy(leftCap.GetComponent<Collider>());
            leftCap.GetComponent<Renderer>().sharedMaterial = material;

            GameObject rightCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightCap.name = "TunnelRoundCap";
            rightCap.transform.SetParent(strip.transform, false);
            rightCap.transform.localPosition = new Vector3((length - width) * 0.5f, 0f, 0f);
            rightCap.transform.localScale = new Vector3(width, 0.05f, width);
            Destroy(rightCap.GetComponent<Collider>());
            rightCap.GetComponent<Renderer>().sharedMaterial = material;
        }

        private GameObject ResolveCartModelPrefab()
        {
            // Inspector'dan direkt atanmÄ±ÅŸ prefab her zaman Ã¶nceliklidir
            GameObject directPrefab = cartModelPrefab as GameObject;
            if (directPrefab != null)
            {
                return directPrefab;
            }

            // Sadece flag aÃ§Ä±ksa path'ten yÃ¼klemeyi dene
            if (!useImportedCartModel)
            {
                return null;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(cartModelAssetPath))
            {
                GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>(cartModelAssetPath);
                if (loaded != null)
                {
                    return loaded;
                }
            }
#endif
            return null;
        }

        private void BuildColorZones()
        {
            // Bu level'da kullanÄ±lan renkleri al, ilk 4'Ã¼nÃ¼ 4 ana yÃ¶n ile eÅŸle
            var colors = new List<CargoColor>(currentLevel.UsedColors);
            if (colors.Count == 0)
            {
                return;
            }

            // Sabit yÃ¶n sÄ±rasÄ±: top -> right -> bottom -> left
            float[] zoneT = new float[] { 0.0f, 0.25f, 0.5f, 0.75f };
            int zoneCount = Mathf.Min(colors.Count, zoneT.Length);
            // Daha az renk varsa pozisyonlarÄ± yay
            if (zoneCount < zoneT.Length)
            {
                for (int i = 0; i < zoneCount; i++)
                {
                    zoneT[i] = i / (float)zoneCount;
                }
            }

            for (int i = 0; i < zoneCount; i++)
            {
                CargoColor color = colors[i];
                float distance = path.TotalLength * zoneT[i];
                Vector3 zonePos = path.GetPosition(distance);
                Vector3 zoneFwd = path.GetForward(distance);

                // GÃ¶rsel: parlak renk barÄ±
                GameObject zoneObj = new GameObject("ColorZone_" + color);
                zoneObj.transform.SetParent(targetRoot, false);
                zoneObj.transform.position = zonePos + Vector3.up * 0.14f;
                zoneObj.transform.rotation = Quaternion.LookRotation(zoneFwd, Vector3.up);

                // Ana renkli bar
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Bar";
                bar.transform.SetParent(zoneObj.transform, false);
                bar.transform.localScale = new Vector3(1.15f, 0.18f, 0.55f);
                Destroy(bar.GetComponent<Collider>());
                bar.GetComponent<Renderer>().sharedMaterial = GetCargoMaterial(color);

                // Ãœst parlak Ã§ubuk
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glow.name = "Glow";
                glow.transform.SetParent(zoneObj.transform, false);
                glow.transform.localScale = new Vector3(1.25f, 0.04f, 0.18f);
                glow.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                Destroy(glow.GetComponent<Collider>());
                glow.GetComponent<Renderer>().sharedMaterial = GetRuntimeMaterial(
                    "ZoneGlow_" + color,
                    Color.Lerp(CargoColorPalette.ToColor(color), Color.white, 0.55f));

                colorZones.Add(new ColorZone
                {
                    Color = color,
                    PathDistance = distance,
                    WorldPosition = zonePos
                });
            }
        }

        private void BuildPathAndTrack()
        {
            BuildCurrentPath();
            ClearChildren(trackRoot);

            // Yol geometrisi:
            //   - DÄ±ÅŸ duvar: tek renk + tek kapalÄ± eÄŸri mesh (yolun dÄ±ÅŸ sÄ±nÄ±rÄ±)
            //   - Ä°Ã§ duvar: tek renk + tek kapalÄ± eÄŸri mesh (yolun iÃ§ sÄ±nÄ±rÄ±)
            //   - Lane: dÄ±ÅŸ ve iÃ§ duvar arasÄ±nÄ± dolduran tek koyu zemin mesh
            //   - Flow markers: animated chevron oklar (yol akÄ±yor hissi)
            float wallOffset = 0.60f;        // duvarlarÄ±n path merkezinden uzaklÄ±ÄŸÄ±
            float wallThickness = 0.16f;     // duvar kalÄ±nlÄ±ÄŸÄ± (incelttim 0.30 â†’ 0.16)
            float wallHeight = 0.30f;
            float laneWidth = (wallOffset * 2f) - wallThickness; // tam arasÄ±nÄ± doldursun

            // ===== LANE (koyu zemin, iki duvar arasÄ±) =====
            Material laneMat = GetRuntimeMaterial("TrackLane", laneColor);
            BuildCurvedBar("Lane", 0f, 0f, 0f, laneWidth, 0.06f, -0.04f, laneMat, closedLoop: true);

            // ===== DUVARLAR (tek renk, tek mesh, kapalÄ± halka) =====
            Material wallMat = GetRuntimeMaterial("WallSingle", trackColor);
            BuildCurvedBar("OuterWall", 0f, 0f, +wallOffset, wallThickness, wallHeight, 0.0f, wallMat, closedLoop: true);
            BuildCurvedBar("InnerWall", 0f, 0f, -wallOffset, wallThickness, wallHeight, 0.0f, wallMat, closedLoop: true);

            // ===== AKIYOR-YOL CHEVRON OKLARI (animated) =====
            Material arrowMat = GetRuntimeMaterial("TrackArrow", new Color(0.85f, 0.82f, 1f));
            flowMarkers.Clear();
            for (int i = 0; i < flowMarkerCount; i++)
            {
                float t = (float)i / flowMarkerCount;
                float distance = path.TotalLength * t;
                GameObject root = CreateChevronArrowRoot(distance, arrowMat);
                flowMarkers.Add(new AnimatedFlowMarker { Root = root.transform, PathDistance = distance });
            }
        }

        /// <summary>
        /// Path'te akan ok gruplarÄ± iÃ§in bir parent transform dÃ¶ndÃ¼rÃ¼r (animasyonda hareket ettirilir).
        /// </summary>
        private GameObject CreateChevronArrowRoot(float distance, Material material)
        {
            Vector3 center = path.GetPosition(distance);
            Vector3 fwd = path.GetForward(distance);

            GameObject root = new GameObject("FlowMarker");
            root.transform.SetParent(trackRoot, false);
            root.transform.position = center + Vector3.up * 0.06f;
            root.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

            // 2 cube â†’ Ã§ift chevron ">>"
            for (int j = 0; j < 2; j++)
            {
                GameObject chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = "Chev_" + j;
                chev.transform.SetParent(root.transform, false);
                chev.transform.localPosition = new Vector3(0f, 0f, j * 0.16f - 0.08f);
                chev.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                chev.transform.localScale = new Vector3(0.10f, 0.10f, 0.04f);
                Destroy(chev.GetComponent<Collider>());
                Renderer rr = chev.GetComponent<Renderer>();
                if (rr != null) rr.sharedMaterial = material;
            }
            return root;
        }

        /// <summary>
        /// Her frame chevron'larÄ± path boyunca ilerlet â†’ yol akÄ±yormuÅŸ gibi hissedilir.
        /// </summary>
        private void UpdateFlowMarkers()
        {
            if (flowMarkers.Count == 0 || path.TotalLength <= 0f) return;
            float totalLen = path.TotalLength;
            float delta = flowMarkerSpeed * Time.deltaTime;
            for (int i = 0; i < flowMarkers.Count; i++)
            {
                var m = flowMarkers[i];
                if (m.Root == null) continue;
                m.PathDistance = Mathf.Repeat(m.PathDistance + delta, totalLen);
                Vector3 pos = path.GetPosition(m.PathDistance);
                Vector3 fwd = path.GetForward(m.PathDistance);
                m.Root.position = pos + Vector3.up * 0.06f;
                m.Root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        private void CreateWallCube(string cubeName, Vector3 position, Vector3 scale, Material material, Vector3 forward)
        {
            // Eski dÃ¼z primitive cube - artÄ±k kullanÄ±lmÄ±yor (curved mesh kullanÄ±yoruz)
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = cubeName;
            cube.transform.SetParent(trackRoot, false);
            cube.transform.position = position;
            cube.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            cube.transform.localScale = scale;
            Destroy(cube.GetComponent<Collider>());
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private void CreateRimStrip(string stripName, Vector3 position, Vector3 forward, Vector3 scale, Material material)
        {
            // Eski dÃ¼z primitive strip - artÄ±k kullanÄ±lmÄ±yor
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = stripName;
            strip.transform.SetParent(trackRoot, false);
            strip.transform.position = position;
            strip.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            strip.transform.localScale = scale;
            Destroy(strip.GetComponent<Collider>());
            Renderer renderer = strip.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Path arc'Ä±nÄ± TAKÄ°P EDEN curved (eÄŸri) bar Ã¼retir. Vertex-by-vertex mesh inÅŸa eder.
        /// closedLoop=true ise tÃ¼m loop boyunca sÃ¼rekli halka mesh (end cap yok, last sample â†’ first sample).
        /// </summary>
        /// <param name="startDist">Path mesafesi baÅŸlangÄ±Ã§ (closedLoop'ta Ã¶nemsiz)</param>
        /// <param name="endDist">Path mesafesi bitiÅŸ (closedLoop'ta Ã¶nemsiz)</param>
        /// <param name="sideOffset">Path merkezinden yan offset (+ = dÄ±ÅŸ, - = iÃ§)</param>
        /// <param name="width">Bar geniÅŸliÄŸi (path'e dik)</param>
        /// <param name="height">Bar yÃ¼ksekliÄŸi (Y)</param>
        /// <param name="baseY">Bar tabanÄ± Y</param>
        /// <param name="closedLoop">true: kapalÄ± halka (tÃ¼m loop), false: aÃ§Ä±k segment</param>
        private GameObject BuildCurvedBar(string barName, float startDist, float endDist, float sideOffset, float width, float height, float baseY, Material material, bool closedLoop = false)
        {
            // Sample sayÄ±sÄ±: kapalÄ± halka iÃ§in path'in Ã§evresine gÃ¶re yÃ¼ksek (smooth corners iÃ§in)
            int sampleCount;
            if (closedLoop)
            {
                sampleCount = Mathf.Clamp(Mathf.CeilToInt(path.TotalLength * 8f), 80, 320);
            }
            else
            {
                float arcLen = endDist - startDist;
                sampleCount = Mathf.Clamp(Mathf.CeilToInt(arcLen * 6f), 2, 24);
            }

            // KapalÄ± halkada vertCount = sampleCount*4 (last â†’ first wrap)
            // AÃ§Ä±kta vertCount = (sampleCount+1)*4 (extra vertex set sonda)
            int vertRingCount = closedLoop ? sampleCount : (sampleCount + 1);
            int vertCount = vertRingCount * 4;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            for (int s = 0; s < vertRingCount; s++)
            {
                float t;
                float d;
                if (closedLoop)
                {
                    t = (float)s / sampleCount;
                    d = path.TotalLength * t;
                }
                else
                {
                    t = (float)s / sampleCount;
                    d = Mathf.Lerp(startDist, endDist, t);
                }

                Vector3 pathPos = path.GetPosition(d);
                Vector3 fwd = path.GetForward(d);
                Vector3 perp = Vector3.Cross(Vector3.up, fwd).normalized;

                Vector3 barCenter = pathPos + perp * sideOffset;
                Vector3 botInner = barCenter - perp * width * 0.5f + Vector3.up * baseY;
                Vector3 botOuter = barCenter + perp * width * 0.5f + Vector3.up * baseY;
                Vector3 topInner = botInner + Vector3.up * height;
                Vector3 topOuter = botOuter + Vector3.up * height;

                int baseIdx = s * 4;
                verts[baseIdx + 0] = botInner;
                verts[baseIdx + 1] = botOuter;
                verts[baseIdx + 2] = topOuter;
                verts[baseIdx + 3] = topInner;

                uvs[baseIdx + 0] = new Vector2(0f, t);
                uvs[baseIdx + 1] = new Vector2(1f, t);
                uvs[baseIdx + 2] = new Vector2(1f, t);
                uvs[baseIdx + 3] = new Vector2(0f, t);
            }

            // Triangles: 4 face (bottom, outer, top, inner) Ã— sampleCount segment Ã— 2 tri/quad
            // + 2 end cap (sadece aÃ§Ä±k bar)
            int capTris = closedLoop ? 0 : 4;
            int triCount = (4 * sampleCount * 2 + capTris) * 3;
            var tris = new int[triCount];
            int ti = 0;

            for (int s = 0; s < sampleCount; s++)
            {
                int v0 = s * 4;
                int v1 = closedLoop ? ((s + 1) % sampleCount) * 4 : (s + 1) * 4;

                // Bottom face
                tris[ti++] = v0 + 0; tris[ti++] = v0 + 1; tris[ti++] = v1 + 1;
                tris[ti++] = v0 + 0; tris[ti++] = v1 + 1; tris[ti++] = v1 + 0;
                // Outer face
                tris[ti++] = v0 + 1; tris[ti++] = v0 + 2; tris[ti++] = v1 + 2;
                tris[ti++] = v0 + 1; tris[ti++] = v1 + 2; tris[ti++] = v1 + 1;
                // Top face
                tris[ti++] = v0 + 2; tris[ti++] = v0 + 3; tris[ti++] = v1 + 3;
                tris[ti++] = v0 + 2; tris[ti++] = v1 + 3; tris[ti++] = v1 + 2;
                // Inner face
                tris[ti++] = v0 + 3; tris[ti++] = v0 + 0; tris[ti++] = v1 + 0;
                tris[ti++] = v0 + 3; tris[ti++] = v1 + 0; tris[ti++] = v1 + 3;
            }

            if (!closedLoop)
            {
                // End cap - baÅŸlangÄ±Ã§
                tris[ti++] = 0; tris[ti++] = 3; tris[ti++] = 2;
                tris[ti++] = 0; tris[ti++] = 2; tris[ti++] = 1;
                // End cap - bitiÅŸ
                int last = sampleCount * 4;
                tris[ti++] = last + 0; tris[ti++] = last + 1; tris[ti++] = last + 2;
                tris[ti++] = last + 0; tris[ti++] = last + 2; tris[ti++] = last + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = "CurvedBar_" + barName;
            if (verts.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(barName);
            go.transform.SetParent(trackRoot, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        private void CreateChevronArrow(float distance, Material material)
        {
            // ">>" gÃ¶rÃ¼nÃ¼mlÃ¼ iki ince Ã¼Ã§gen prizma (rotate edilmiÅŸ cube)
            Vector3 center = path.GetPosition(distance);
            Vector3 fwd = path.GetForward(distance);

            for (int j = 0; j < 2; j++)
            {
                Vector3 chevPos = center + fwd * (j * 0.20f - 0.10f) + Vector3.up * 0.06f;
                GameObject chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = "Chevron_" + j;
                chev.transform.SetParent(trackRoot, false);
                chev.transform.position = chevPos;
                chev.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 0f, 45f);
                // Ä°nce, ileriye doÄŸru sivrilen ÅŸekil iÃ§in scale
                chev.transform.localScale = new Vector3(0.13f, 0.13f, 0.04f);
                Destroy(chev.GetComponent<Collider>());
                Renderer renderer = chev.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }
        }

        private void BuildCurrentPath()
        {
            switch (currentLevel.PathDesign)
            {
                case PathDesign.WideLoop:
                    AddOvalPath(trackWidthX + 0.4f, trackDepthZ + 0.2f, 0.06f, 0.12f);
                    break;
                case PathDesign.PinchedLoop:
                    AddOvalPath(trackWidthX, trackDepthZ + 0.5f, 0.12f, 0.22f);
                    break;
                case PathDesign.OffsetLoop:
                    AddOvalPath(trackWidthX - 0.2f, trackDepthZ + 0.6f, 0.18f, 0.16f);
                    break;
                case PathDesign.SoftSquare:
                    path.SetRoundedRectangle(trackWidthX, trackDepthZ, trackCornerRadius * 0.8f, 12, 0f);
                    break;
                default:
                    path.SetRoundedRectangle(trackWidthX, trackDepthZ, trackCornerRadius, 14, 0f);
                    break;
            }
        }

        private void AddOvalPath(float width, float depth, float wobble, float pinch)
        {
            var points = new List<Vector3>();
            int count = 110;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float angle = t * Mathf.PI * 2f;
                float radiusPulse = 1f + Mathf.Sin(angle * 3f + currentLevelIndex) * wobble;
                float x = Mathf.Cos(angle) * width * 0.5f * radiusPulse;
                float z = Mathf.Sin(angle) * depth * 0.5f * (1f - Mathf.Cos(angle * 2f) * pinch * 0.18f);
                points.Add(new Vector3(x, 0f, z));
            }

            path.SetPoints(points);
        }

        private void CreateTrackPad(string padName, float distance, Vector3 scale, Material material, float yOffset)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = padName;
            pad.transform.SetParent(trackRoot, false);
            pad.transform.position = path.GetPosition(distance) + Vector3.up * yOffset;
            pad.transform.localScale = scale;
            pad.transform.rotation = Quaternion.LookRotation(path.GetForward(distance), Vector3.up);
            Destroy(pad.GetComponent<Collider>());
            Renderer renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private void CreateRimDot(Vector3 position, Material material)
        {
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rim.name = "Rim";
            rim.transform.SetParent(trackRoot, false);
            rim.transform.position = position;
            rim.transform.localScale = new Vector3(0.1f, 0.07f, 0.1f);
            Destroy(rim.GetComponent<Collider>());
            Renderer renderer = rim.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private GameObject BuildCargoVisual(CargoColor color)
        {
            // Bilye boyutunda yumuÅŸak kenarlÄ± kÃ¼p (ribbon yok, sade)
            GameObject root = new GameObject("Cargo_" + color);
            root.transform.localScale = Vector3.one;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = inSlotCargoScale;
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = GetCargoMaterial(color);

            return root;
        }

        private void SpawnClearFx(Vector3 position, CargoColor color)
        {
            // Disabled: the expanding sphere read as a "bubble" when cargo landed in trucks.
        }

        private IEnumerator ScaleAndDestroy(GameObject visual)
        {
            float t = 0f;
            while (t < 1f && visual != null)
            {
                t += Time.deltaTime * 4f;
                visual.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.95f, t);
                yield return null;
            }

            if (visual != null)
            {
                Destroy(visual);
            }
        }

        // ============================================================
        // Scene setup
        // ============================================================
        private void CreateRuntimeRoot()
        {
            Transform oldRoot = transform.Find("Runtime");
            if (oldRoot != null)
            {
                DestroySmart(oldRoot.gameObject);
            }

            runtimeRoot = new GameObject("Runtime").transform;
            runtimeRoot.SetParent(transform, false);
            trackRoot = CreateChild(runtimeRoot, "LoopTrack");
            cartRoot = CreateChild(runtimeRoot, "CartContainer");
            cargoRoot = CreateChild(runtimeRoot, "CargoContainer");
            targetRoot = CreateChild(runtimeRoot, "TargetContainer");
            fxRoot = CreateChild(runtimeRoot, "FXContainer");
        }

        private Transform CreateChild(Transform parent, string childName)
        {
            Transform child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        private void SetupCameraAndLighting()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
            }

            mainCamera.orthographic = true;
            // Kamera boyutunu tır sayısına göre dinamik ayarla
            int cartCount = currentLevel != null ? currentLevel.Carts.Count : 1;
            float dynamicOrthoSize = orthographicSize + (cartCount - 1) * orthographicSizePerCart;
            mainCamera.orthographicSize = dynamicOrthoSize;
            
            // Kamerayı tırların merkezine hizala
            float totalSpan = (cartCount - 1) * cartVerticalSpacing;
            mainCamera.transform.position = new Vector3(0f, 12.0f + cartCount * 0.3f, -5.0f - cartCount * 0.2f);
            mainCamera.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            // Background ile floor aynÄ± renk â†’ tek tip zemin gÃ¶rÃ¼nÃ¼mÃ¼
            mainCamera.backgroundColor = floorColor;

            Light light = FindFirstObjectByType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.intensity = 1.0f;                       // hafiflet (renkler daha pÃ¼r)
            light.color = new Color(1f, 1f, 1f);          // nÃ¶tr beyaz (warm tint kalksÄ±n)
            light.transform.rotation = Quaternion.Euler(55f, -20f, 12f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DarkPurpleFloor";
            floor.transform.SetParent(runtimeRoot, false);
            floor.transform.position = new Vector3(0f, -0.18f, 0f);
            // Tek bÃ¼yÃ¼k plaka - tÃ¼m gÃ¶rÃ¼nÃ¼r alanÄ± kapla, kamera background ile renk eÅŸit
            int cartCountForFloor = currentLevel != null ? currentLevel.Carts.Count : 1;
            float totalSpanForFloor = (cartCountForFloor - 1) * cartVerticalSpacing;
            float floorWidth = Mathf.Max(60f, trackWidthX * 3f + cartCountForFloor * cartVerticalSpacing * 2f);
            float floorLength = Mathf.Max(90f, totalSpanForFloor * 3f);
            floor.transform.localScale = new Vector3(floorWidth, 0.1f, floorLength);
            Destroy(floor.GetComponent<Collider>());
            Renderer floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial = GetRuntimeMaterial("Floor", floorColor);
            }
        }

        // ============================================================
        // UI
        // ============================================================
        private void BuildUi()
        {
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                DestroySmart(existingCanvas.gameObject);
            }

            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.transform.SetParent(transform, false);
                eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            }

            // TOP BAR
            Color pillColor = new Color(0.31f, 0.21f, 0.65f, 0.95f);
            Color pillOutline = new Color(0.10f, 0.06f, 0.22f, 1f);

            GameObject levelPill = CreatePill(canvasObject.transform, "LevelPill",
                new Vector2(0.04f, 0.92f), new Vector2(0.32f, 0.97f), pillColor, pillOutline);
            levelLabel = CreateText(levelPill.transform, "Label", "Seviye 1",
                Vector2.zero, Vector2.one, 46, TextAnchor.MiddleCenter, Color.white);

            GameObject progressPill = CreatePill(canvasObject.transform, "ProgressPill",
                new Vector2(0.36f, 0.92f), new Vector2(0.64f, 0.97f), pillColor, pillOutline);
            CreateIcon(progressPill.transform, "Icon", new Color(1f, 0.79f, 0.12f),
                new Vector2(0.06f, 0.12f), new Vector2(0.30f, 0.88f));
            progressLabel = CreateText(progressPill.transform, "Label", "0/0",
                new Vector2(0.30f, 0f), new Vector2(0.98f, 1f), 46, TextAnchor.MiddleLeft, Color.white);

            GameObject settingsPill = CreatePill(canvasObject.transform, "SettingsPill",
                new Vector2(0.86f, 0.92f), new Vector2(0.96f, 0.97f), pillColor, pillOutline);
            CreateText(settingsPill.transform, "Gear", "âš™",
                Vector2.zero, Vector2.one, 44, TextAnchor.MiddleCenter, Color.white);

            capacityLabel = CreateText(canvasObject.transform, "CapacityLabel", "Loop 0/0",
                new Vector2(0.30f, 0.87f), new Vector2(0.70f, 0.91f), 32, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.82f, 1f, 0.9f));

            stateLabel = CreateText(canvasObject.transform, "StateLabel", "",
                new Vector2(0.10f, 0.52f), new Vector2(0.90f, 0.64f), 88, TextAnchor.MiddleCenter, Color.white);
            stateLabel.gameObject.SetActive(false);

            // BOTTOM BOOSTERS
            undoBadge = CreateBoosterButton(canvasObject.transform, "UndoButton", "â†¶",
                new Vector2(0.18f, 0.04f), new Vector2(0.34f, 0.12f), UndoLast);
            shuffleBadge = CreateBoosterButton(canvasObject.transform, "ShuffleButton", "â‡„",
                new Vector2(0.42f, 0.04f), new Vector2(0.58f, 0.12f), ShuffleCarts);
            extraBadge = CreateBoosterButton(canvasObject.transform, "CapacityButton", "+1",
                new Vector2(0.66f, 0.04f), new Vector2(0.82f, 0.12f), AddCapacity);

            winPanel = CreatePanel(canvasObject.transform, "WinPanel", "SÄ±radaki",
                "Seviye Tamam!", NextLevel, new Color(0.16f, 0.10f, 0.30f, 0.95f));
            losePanel = CreatePanel(canvasObject.transform, "LosePanel", "Tekrar Dene",
                "Loop Doldu!", RestartLevel, new Color(0.30f, 0.10f, 0.16f, 0.95f));
        }

        private GameObject CreatePill(Transform parent, string pillName, Vector2 anchorMin, Vector2 anchorMax, Color fill, Color outlineColor)
        {
            GameObject pill = new GameObject(pillName);
            pill.transform.SetParent(parent, false);
            RectTransform rect = pill.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = pill.AddComponent<Image>();
            bg.color = fill;
            bg.sprite = GetUiRoundedSprite();
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.5f;

            Outline outline = pill.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            return pill;
        }

        private Text CreateText(Transform parent, string textName, string value, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(textName);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Image CreateIcon(Transform parent, string iconName, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject icon = new GameObject(iconName);
            icon.transform.SetParent(parent, false);
            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = icon.AddComponent<Image>();
            image.color = color;
            image.sprite = GetUiRoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            return image;
        }

        private Text CreateBoosterButton(Transform parent, string buttonName, string icon, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            // DÄ±ÅŸ altÄ±n Ã§erÃ§eve
            GameObject outer = new GameObject(buttonName);
            outer.transform.SetParent(parent, false);
            RectTransform outerRect = outer.AddComponent<RectTransform>();
            outerRect.anchorMin = anchorMin;
            outerRect.anchorMax = anchorMax;
            outerRect.offsetMin = Vector2.zero;
            outerRect.offsetMax = Vector2.zero;

            Image outerImage = outer.AddComponent<Image>();
            outerImage.color = new Color(1f, 0.78f, 0.22f, 1f);
            outerImage.sprite = GetUiRoundedSprite();
            outerImage.type = Image.Type.Sliced;
            outerImage.pixelsPerUnitMultiplier = 1.5f;
            outerImage.raycastTarget = true;

            // Ä°Ã§ mor zemin
            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(outer.transform, false);
            RectTransform innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.08f, 0.10f);
            innerRect.anchorMax = new Vector2(0.92f, 0.92f);
            innerRect.offsetMin = Vector2.zero;
            innerRect.offsetMax = Vector2.zero;

            Image innerImage = inner.AddComponent<Image>();
            innerImage.color = new Color(0.35f, 0.22f, 0.74f, 1f);
            innerImage.sprite = GetUiRoundedSprite();
            innerImage.type = Image.Type.Sliced;
            innerImage.pixelsPerUnitMultiplier = 1.8f;
            innerImage.raycastTarget = false;

            CreateText(inner.transform, "Icon", icon,
                Vector2.zero, Vector2.one, 60, TextAnchor.MiddleCenter, Color.white);

            // Buton komponenti
            Button button = outer.AddComponent<Button>();
            button.targetGraphic = outerImage;
            button.onClick.AddListener(action);
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = cb;

            // SayaÃ§ badge (yeÅŸil yuvarlak)
            GameObject badge = new GameObject("Badge");
            badge.transform.SetParent(outer.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.62f, -0.18f);
            badgeRect.anchorMax = new Vector2(1.08f, 0.28f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;

            Image badgeImage = badge.AddComponent<Image>();
            badgeImage.color = new Color(0.22f, 0.78f, 0.36f, 1f);
            badgeImage.sprite = GetUiRoundedSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.pixelsPerUnitMultiplier = 4f;
            badgeImage.raycastTarget = false;

            Outline badgeOutline = badge.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.05f, 0.20f, 0.08f, 1f);
            badgeOutline.effectDistance = new Vector2(2f, -2f);

            Text badgeText = CreateText(badge.transform, "Count", "3",
                Vector2.zero, Vector2.one, 36, TextAnchor.MiddleCenter, Color.white);
            return badgeText;
        }

        private GameObject CreatePanel(Transform parent, string panelName, string buttonLabel, string title, UnityEngine.Events.UnityAction action, Color bg)
        {
            GameObject panel = new GameObject(panelName);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.18f, 0.34f);
            rect.anchorMax = new Vector2(0.82f, 0.62f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = bg;
            image.sprite = GetUiRoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.5f;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.22f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);

            CreateText(panel.transform, "Title", title,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.92f), 68, TextAnchor.MiddleCenter, Color.white);

            // Ä°Ã§ buton
            GameObject buttonGo = new GameObject("ActionButton");
            buttonGo.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = buttonGo.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.20f, 0.10f);
            buttonRect.anchorMax = new Vector2(0.80f, 0.45f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(1f, 0.78f, 0.22f, 1f);
            buttonImage.sprite = GetUiRoundedSprite();
            buttonImage.type = Image.Type.Sliced;
            buttonImage.pixelsPerUnitMultiplier = 1.5f;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(action);

            CreateText(buttonGo.transform, "Label", buttonLabel,
                Vector2.zero, Vector2.one, 52, TextAnchor.MiddleCenter, new Color(0.18f, 0.10f, 0.08f, 1f));

            panel.SetActive(false);
            return panel;
        }

        private Sprite uiRoundedSprite;
        private Sprite GetUiRoundedSprite()
        {
            if (uiRoundedSprite != null)
            {
                return uiRoundedSprite;
            }

            // Unity 6'da UI/Skin/UISprite.psd builtin'i kaldÄ±rÄ±ldÄ± -> direkt procedural Ã¼ret.
            uiRoundedSprite = BuildRoundedRectSprite(96, 96, 22);
            return uiRoundedSprite;
        }

        private static Sprite BuildRoundedRectSprite(int width, int height, int radius)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "UI_Rounded";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = InsideRoundedRect(x, y, width, height, radius)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var rect = new Rect(0, 0, width, height);
            var pivot = new Vector2(0.5f, 0.5f);
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            int cornerX = -1;
            int cornerY = -1;
            if (x < radius && y < radius) { cornerX = radius; cornerY = radius; }
            else if (x >= width - radius && y < radius) { cornerX = width - radius - 1; cornerY = radius; }
            else if (x < radius && y >= height - radius) { cornerX = radius; cornerY = height - radius - 1; }
            else if (x >= width - radius && y >= height - radius) { cornerX = width - radius - 1; cornerY = height - radius - 1; }

            if (cornerX < 0)
            {
                return true;
            }

            int dx = x - cornerX;
            int dy = y - cornerY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private void UpdateHud()
        {
            if (currentLevel == null)
            {
                return;
            }

            if (levelLabel != null) levelLabel.text = "Seviye " + currentLevelIndex;

            int targetColorSlots = 0;
            int totalSlots = 0;
            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                targetColorSlots += cart.FullSlotsOfColor(cart.TargetColor);
                totalSlots += CargoCartView.SlotCount;
            }

            if (progressLabel != null)
            {
                progressLabel.text = targetColorSlots + "/" + totalSlots;
            }
            if (capacityLabel != null) capacityLabel.text = "Loop " + activeCargo.Count + "/" + maxLoopCapacity;
            if (undoBadge != null) undoBadge.text = undoCount.ToString();
            if (shuffleBadge != null) shuffleBadge.text = shuffleCount.ToString();
            if (extraBadge != null) extraBadge.text = extraSlotCount.ToString();
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroySmart(parent.GetChild(i).gameObject);
            }
        }

        private void DestroySmart(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        // ============================================================
        // Inner types
        // ============================================================
        private sealed class ActiveCargo
        {
            public CargoColor Color;
            public GameObject Visual;
            public float Distance;
            public float PreviousDistance;
            public float Age;
            public bool IsEnteringRoad;
            public float EntryDistance;
            public bool IsCollecting;
            public float FlyProgress;
            public Vector3 FlyStart;
            public Vector3 FlyTarget;
            public Vector3 BaseScale;
            public CargoCartView SourceCart;
            public int SourceColumn;
            public CargoCartView DestinationCart;
            public int DestinationColumn;
            // Rotasyon (tumble) â€” yumuÅŸak, akÄ±ÅŸkan dÃ¶nÃ¼ÅŸ iÃ§in
            public Vector3 TumbleAxis;
            public float TumbleSpeed;
            // YÄ±ÄŸÄ±n efekti iÃ§in her kÃ¼pÃ¼n path-yan offset'i ve yÃ¼kseklik offset'i
            public float LaneOffset;
            public float VerticalOffset;
        }

        private sealed class ColorZone
        {
            public CargoColor Color;
            public float PathDistance;
            public Vector3 WorldPosition;
        }

        private sealed class ReleaseRecord
        {
            public readonly CargoCartView SourceCart;
            public readonly int SourceColumn;
            public readonly ActiveCargo Cargo;
            public readonly CargoColor Color;

            public ReleaseRecord(CargoCartView sourceCart, int sourceColumn, ActiveCargo cargo, CargoColor color)
            {
                SourceCart = sourceCart;
                SourceColumn = sourceColumn;
                Cargo = cargo;
                Color = color;
            }
        }

        private sealed class RuntimeCart
        {
            public CargoColor TargetColor;
            public CargoColor?[] InitialSlotColors; // 8 slot (2x4), null = boÅŸ baÅŸlar

            public RuntimeCart(CargoColor target, CargoColor?[] initialSlots)
            {
                TargetColor = target;
                InitialSlotColors = initialSlots ?? new CargoColor?[CargoCartView.SlotCount];
            }
        }

        private sealed class RuntimeLevel
        {
            public int RequiredCargoCount;
            public int MaxLoopCapacity;
            public float CargoMoveSpeed;
            public PathDesign PathDesign;
            public List<RuntimeCart> Carts = new List<RuntimeCart>();

            public IEnumerable<CargoColor> UsedColors
            {
                get
                {
                    var colors = new List<CargoColor>();
                    for (int i = 0; i < Carts.Count; i++)
                    {
                        if (!colors.Contains(Carts[i].TargetColor)) colors.Add(Carts[i].TargetColor);
                        var s = Carts[i].InitialSlotColors;
                        for (int j = 0; j < s.Length; j++)
                        {
                            if (s[j].HasValue && !colors.Contains(s[j].Value)) colors.Add(s[j].Value);
                        }
                    }
                    return colors;
                }
            }

            public static List<RuntimeLevel> CreateDefaultLevels()
            {
                // Ã‡oklu cart + stripe yapÄ±sÄ±.
                // Her cart: Ã¼st 4 slot = Ã–N renk (atÄ±lacak), alt 4 slot = ARKA = TARGET renk (kalacak).
                // Tap: en Ã¼stteki Ã–N renk slot'u patlar -> kÃ¼p yola.
                // Yoldaki kÃ¼p: sadece TARGET rengi eÅŸleÅŸen tÄ±ra iner (boÅŸ yer varsa).
                // Win: her tÄ±r 8 slotu tek renk (kendi target rengi) ile dolu.

                // NOT: maxLoopCapacity = ortalama 50 partikÃ¼l + buffer (her tap 8 slot Ã— 6 partikÃ¼l = 48)
                return new List<RuntimeLevel>
                {
                    // Level 1 - 2 tÄ±r, 2 renk
                    Level(PathDesign.RoundedLoop, 200, 1.20f,
                        Cart(R, Stripe(B, R)),
                        Cart(B, Stripe(R, B))),

                    // Level 2 - 3 tÄ±r, 3 renk, dairesel akÄ±ÅŸ
                    Level(PathDesign.RoundedLoop, 250, 1.25f,
                        Cart(R, Stripe(B, R)),
                        Cart(B, Stripe(Y, B)),
                        Cart(Y, Stripe(R, Y))),

                    // Level 3 - 4 tÄ±r, 4 renk
                    Level(PathDesign.WideLoop, 300, 1.30f,
                        Cart(R, Stripe(B, R)),
                        Cart(B, Stripe(Y, B)),
                        Cart(Y, Stripe(G, Y)),
                        Cart(G, Stripe(R, G))),

                    // Level 4 - 4 tÄ±r, karÄ±ÅŸÄ±k Ã¶n stripe (16 slot - Ã¼st 8 mixed, alt 8 target)
                    Level(PathDesign.PinchedLoop, 400, 1.35f,
                        Cart(R, Slots(B, B, Y, B, G, B, Y, B, R, R, R, R, R, R, R, R)),
                        Cart(B, Slots(Y, Y, R, Y, G, Y, R, Y, B, B, B, B, B, B, B, B)),
                        Cart(Y, Slots(G, G, B, G, R, G, B, G, Y, Y, Y, Y, Y, Y, Y, Y)),
                        Cart(G, Slots(R, R, Y, R, B, R, Y, R, G, G, G, G, G, G, G, G))),
                };
            }

            /// <summary>
            /// 2x8 (16 slot) stripe: Ã¼st 8 slot frontColor, alt 8 slot backColor.
            /// Slot indeksi 0..15: 0,1 = en Ã¶n sÄ±ra; 14,15 = en arka sÄ±ra.
            /// </summary>
            private static CargoColor?[] Stripe(CargoColor frontColor, CargoColor backColor)
            {
                return new CargoColor?[]
                {
                    frontColor, frontColor, // row 0 (en Ã¶n)
                    frontColor, frontColor, // row 1
                    frontColor, frontColor, // row 2
                    frontColor, frontColor, // row 3
                    backColor,  backColor,  // row 4
                    backColor,  backColor,  // row 5
                    backColor,  backColor,  // row 6
                    backColor,  backColor   // row 7 (en arka)
                };
            }

            private static RuntimeLevel Level(PathDesign pathDesign, int capacity, float speed, params RuntimeCart[] carts)
            {
                return new RuntimeLevel
                {
                    PathDesign = pathDesign,
                    RequiredCargoCount = CargoCartView.SlotCount,
                    MaxLoopCapacity = capacity,
                    CargoMoveSpeed = speed,
                    Carts = new List<RuntimeCart>(carts)
                };
            }

            private static RuntimeCart Cart(CargoColor target, CargoColor?[] initialSlots)
            {
                return new RuntimeCart(target, initialSlots);
            }

            private static CargoColor?[] Slots(params CargoColor[] colors)
            {
                var arr = new CargoColor?[CargoCartView.SlotCount];
                for (int i = 0; i < arr.Length && i < colors.Length; i++) arr[i] = colors[i];
                return arr;
            }

            private static readonly CargoColor R = CargoColor.Red;
            private static readonly CargoColor B = CargoColor.Blue;
            private static readonly CargoColor Y = CargoColor.Yellow;
            private static readonly CargoColor G = CargoColor.Green;
            private static readonly CargoColor P = CargoColor.Purple;
        }
    }
}

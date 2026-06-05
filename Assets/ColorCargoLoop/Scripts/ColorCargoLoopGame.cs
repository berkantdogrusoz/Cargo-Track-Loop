using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;
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
        SoftSquare,
        Serpentine // kivrimli/dalgali yol - yan kenarlar serpantin gibi kivrilir
    }

    public enum CartLayout
    {
        CenterStack,
        Staggered,
        Diagonal,
        LeftBias,
        RightBias,
        Grid // 2 kolon / 2x2 grid - tirlar yana dagilir (kucuk tir gerekir)
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
        [SerializeField] private float cartHeightY = 0.05f;

        [Header("Cargo Slot Layout (in cart)")]
        [SerializeField] private Vector3 inSlotCargoScale = new Vector3(0.18f, 0.18f, 0.18f);
        [SerializeField] private float inSlotColumnStep = 0.38f;
        [SerializeField] private float inSlotRowStep = 0.36f;
        [SerializeField] private float cubeStackStep = 0.17f;
        [SerializeField] private Vector3 cargoSlotLocalOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private float importedCargoSlotHeightRatio = 0.34f;
        [SerializeField] private bool addAccentFlagOnImportedModel = true;
        [SerializeField] private int columnCapacityPerCart = 6;

        [Header("Phase 2 (box mechanic) - PREVIEW")]
        [SerializeField] private bool phase2BoxPreview = false;   // Inspector'dan ac -> kutu onizlemesi + demo dolma
        [SerializeField] private GameObject boxModelPrefab;       // ColorBox.glb (bos = path'ten yuklenir)
        private readonly List<CargoBoxView> previewBoxes = new List<CargoBoxView>();
        private Coroutine phase2PreviewCoroutine;

        public GameObject ResolveBoxModel()
        {
            if (boxModelPrefab != null) return boxModelPrefab;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/ColorBox.glb");
#else
            return null;
#endif
        }

        [Header("Cart cargo bed fit (imported model)")]
        [SerializeField] private float bedLengthFillRatio = 0.45f; // kasanin uzunluga oranli dolum (kup gridi)
        [SerializeField] private float bedWidthFillRatio = 0.62f;  // kasanin genislige oranli dolum
        [SerializeField] private float bedOffsetRatio = -0.10f;    // gridi kasaya kaydir (+/- yon)
        public float BedLengthFillRatio { get { return bedLengthFillRatio; } }
        public float BedWidthFillRatio { get { return bedWidthFillRatio; } }
        public float BedOffsetRatio { get { return bedOffsetRatio; } }

        [Header("Haptics (titresim - sadece cihazda)")]
        [SerializeField] private bool enableHaptics = true;
        private float fillHapticTimer;
        private bool fillHapticActive;

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
        [SerializeField] private float stuckGraceSeconds = 5f; // takilinca ekstra tir eklemek icin taninan sure (sn)

        [Header("Meta")]
        [SerializeField] private int winCoinReward = 25;
        [SerializeField] private bool showMainMenuOnStart = false; // kullanici istegi: otomatik menu YOK, oyun direkt baslar
        [SerializeField] private int rewardedAdCoinAmount = 50;

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
        public Vector3 SlotBlockSize { get { return slotBlockSize; } }
        public int ParticlesPerTap { get { return particlesPerTap; } }
        public int SlotFillThreshold { get { return slotFillThreshold; } }
        public float ParticleSize { get { return particleSize; } }
        public Vector3 RoadCargoScale { get { return roadCargoScale; } }

        // ----- Runtime -----
        private readonly LoopPath path = new LoopPath();
        private readonly List<RuntimeLevel> levels = new List<RuntimeLevel>();
        private readonly List<CargoCartView> carts = new List<CargoCartView>();
        private readonly List<Vector3> reservedCartSlots = new List<Vector3>(); // ekstra tir icin park alaninda onceden hesaplanmis gizli yerler
        private readonly List<ActiveCargo> activeCargo = new List<ActiveCargo>();
        private readonly List<ReleaseRecord> releaseHistory = new List<ReleaseRecord>();
        private readonly List<ColorZone> colorZones = new List<ColorZone>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly Dictionary<CargoColor, Material> cargoMaterials = new Dictionary<CargoColor, Material>();
        private readonly HashSet<CargoCartView> completedCarts = new HashSet<CargoCartView>();
        private readonly HashSet<CargoCartView> bonusCarts = new HashSet<CargoCartView>();
        private readonly Dictionary<CargoCartView, TruckExitRoute> truckExitRoutes = new Dictionary<CargoCartView, TruckExitRoute>();
        private readonly List<float> cartPickupDistances = new List<float>(); // Her cart iÃ§in path Ã¼stÃ¼ndeki en yakÄ±n nokta (REAR/back tarafi)
        private readonly List<float> cartHeadPickupDistances = new List<float>(); // CALISAN SISTEM: ek pickup noktasi (HEAD/sag tarafi) - 2-yon dolma
        private readonly List<AnimatedFlowMarker> flowMarkers = new List<AnimatedFlowMarker>(); // Yolda akan oklar
        private readonly List<GameObject> shuffleSelectionHighlights = new List<GameObject>();
        private const string ToonPlasticShaderName = "Color Cargo Loop/Toon Plastic";
        private static readonly bool RuntimeToonMaterialsEnabled = true;

        [Header("Road Flow Animation")]
        [SerializeField] private int flowMarkerCount = 16;
        [SerializeField] private float flowMarkerSpeed = 1.8f;
        [Tooltip("GLOBAL yol hizi carpani - tum levellerdeki kup hizini olcekler (Inspector'dan ayarla)")]
        [SerializeField] private float roadSpeedMultiplier = 1.35f;

        [Header("Tunnel (kesik-yol gorunumu)")]
        [SerializeField] private bool enableTunnels = true;
        [Tooltip("Tunelin koseden ne kadar asagi indigi (cornerRadius'un orani)")]
        [SerializeField] private float tunnelReachRatio = 0.7f;
        [SerializeField] private Color tunnelCoverColor = new Color(0.52f, 0.47f, 0.66f);
        private float tunnelTopZ = 99999f;   // bu z'den yukarisi UST tunel (kup gizlenir)
        private float tunnelBotZ = -99999f;  // bu z'den asagisi ALT tunel
        private bool tunnelTopActive;
        private bool tunnelBotActive;
        private float tunnelTopStart;
        private float tunnelTopEnd;
        private float tunnelBotStart;
        private float tunnelBotEnd;

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
        private Text coinLabel;
        private Text winRewardLabel;
        private Text removeAdsLabel;
        private GameObject mainMenuPanel;
        [SerializeField] private TMP_Text stuckCountdownText; // sahnedeki "5 saniye sayac text" (Inspector'dan ata; bos ise isimle bulunur)
        private RectTransform stuckCountdownRect;
        private bool stuckCountdownActive;
        private float stuckCountdownRemaining;
        private float stuckShowTime;         // pop-in animasyon zamani
        private float stuckPulseTime;        // her saniye pulse zamani
        private int stuckShownSecond;
        private bool playerActedThisLevel;   // ilk hamleden once sayac baslamasin (cold-start lose engeli)
        private readonly HashSet<CargoColor> colorScratch = new HashSet<CargoColor>();
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
        private int coins;
        private bool coinRewardGrantedThisLevel;
        private bool removeAdsPurchased;
        private bool mainMenuShownThisSession;
        private bool shuffleSelectionMode;
        private GameObject shuffleSelectionOverlay;
        private TMP_Text sceneShuffleBadge;
        private TMP_Text sceneExtraBadge;
        private TMP_Text sceneUndoBadge;

        private sealed class TruckExitRoute
        {
            public Vector3 Start;
            public Vector3 PortalMouth;
            public Vector3 PortalInside;
            public GameObject Portal;
        }

        private sealed class ButtonPressFeedback : MonoBehaviour
        {
            private Button button;
            private Vector3 baseScale;
            private Coroutine routine;
            private bool initialized;

            public void Initialize()
            {
                button = GetComponent<Button>();
                baseScale = transform.localScale;
                if (!initialized && button != null)
                {
                    button.onClick.AddListener(Play);
                    initialized = true;
                }
            }

            private void Play()
            {
                Haptics.Vibrate(24, 120);
                if (routine != null) StopCoroutine(routine);
                routine = StartCoroutine(PlayRoutine());
            }

            private IEnumerator PlayRoutine()
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 14f;
                    transform.localScale = Vector3.Lerp(baseScale, baseScale * 0.86f, Mathf.Clamp01(t));
                    yield return null;
                }

                t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime * 12f;
                    float eased = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 0.5f);
                    transform.localScale = Vector3.Lerp(baseScale * 0.86f, baseScale, eased);
                    yield return null;
                }

                transform.localScale = baseScale;
                routine = null;
            }
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
            UpdateFillHaptics();
            CheckSolvedCartsForDeparture();
            CheckLoseByNoMoves();
            TickStuckCountdown();
        }

        /// <summary>
        /// TIRA KUP DOLARKEN surekli titresim (dolma boyunca). Hicbir kup inmiyorsa durdurur.
        /// </summary>
        private void UpdateFillHaptics()
        {
            if (!enableHaptics) return;

            bool filling = false;
            for (int i = 0; i < activeCargo.Count; i++)
            {
                if (activeCargo[i].IsCollecting) { filling = true; break; }
            }

            if (filling)
            {
                fillHapticActive = true;
                fillHapticTimer -= Time.deltaTime;
                if (fillHapticTimer <= 0f)
                {
                    // 160ms titresim her 0.12s yenilenir -> SUREKLI hissi (dolma boyunca)
                    Haptics.Vibrate(160, 95);
                    fillHapticTimer = 0.12f;
                }
            }
            else if (fillHapticActive)
            {
                fillHapticActive = false;
                fillHapticTimer = 0f;
                Haptics.Cancel();
            }
        }

        // ============================================================
        // Public API (sahne / dÄ±ÅŸ buton referanslarÄ± iÃ§in)
        // ============================================================
        public void BuildPrototype()
        {
            state = GameState.Loading;
            Haptics.Enabled = enableHaptics;
            LoadCoins();
            levels.Clear();
            levels.AddRange(RuntimeLevel.CreateDefaultLevels());

            CreateRuntimeRoot();
            SetupCameraAndLighting();
            // Alttan beyaz fill light KAPALI (kullanici istegi)
            GameObject fillBottom = GameObject.Find("Fill Light Bottom");
            if (fillBottom != null) fillBottom.SetActive(false);
            // UI geÃ§ici olarak kapalÄ± (kullanÄ±cÄ± tercihi - temiz gÃ¶rÃ¼nÃ¼m)
            // BuildUi();
            WireSceneBoosterButtons();
            EnsureMetaUi();
            LoadLevel(Mathf.Clamp(startLevelIndex, 1, levels.Count));
            if (showMainMenuOnStart && !mainMenuShownThisSession)
            {
                ShowMainMenu();
            }
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        // HIZLI AYAR: Play sirasinda bed-fit degerlerini degistir, sonra component sag-tik ->
        // "Rebuild Level (bed fit uygula)" -> aninda yeniden kurar (stop/play gerekmez).
        [ContextMenu("Rebuild Level (bed fit uygula)")]
        public void RebuildLevelNow()
        {
            if (Application.isPlaying && levels.Count > 0) LoadLevel(currentLevelIndex);
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

        private readonly HashSet<CargoCartView> burstingCarts = new HashSet<CargoCartView>();

        /// <summary>Bu tira su an kup INIYOR mu? (collecting hedefi bu tir)</summary>
        private bool IsCartReceiving(CargoCartView cart)
        {
            for (int i = 0; i < activeCargo.Count; i++)
            {
                if (activeCargo[i].IsCollecting && activeCargo[i].DestinationCart == cart) return true;
            }
            return false;
        }

        public void TryReleaseFromCart(CargoCartView cart)
        {
            if (state != GameState.Playing || cart == null) return;

            if (shuffleSelectionMode)
            {
                ApplyShuffleToSelectedCart(cart);
                return;
            }

            // PER-TIR tap-lock: SADECE bu tir mesgulse kapali (kendi burst'u devam ediyor VEYA kup iniyor).
            // Diger tirlar serbest -> sirayla bosaltabilirsin.
            if (burstingCarts.Contains(cart) || IsCartReceiving(cart)) return;

            if (lastCartReleaseFrame == Time.frameCount) return;
            lastCartReleaseFrame = Time.frameCount;

            // BUG FIX: tir dolarken boSaltilirsa karisikligi onle.
            // Bu tira gelen in-flight kupleri yola dondur + yarim slotlardaki kupleri yola geri dok.
            // Boylece sayim KAYBOLMAZ, slotlar temiz (hep dolu/bos) kalir.
            CancelIncomingAndDrainPartials(cart);

            // Tek tap = TÃœM non-target Ã¶n stripe kÃ¼pleri yola Ã§Ä±kar (burst)
            var released = cart.ReleaseAllFront();
            if (released.Count == 0) return;

            playerActedThisLevel = true; // ilk hamle yapildi -> stuck sayaci artik tetiklenebilir
            Haptics.Vibrate(28, 160); // TIRA DOKUNUNCA kisa net titresim
            StartCoroutine(SpawnFrontBurst(cart, released));
        }

        /// <summary>
        /// Bir tir boSaltilirken: ona gelen in-flight kupleri yola dondur ve yarim (kismi dolu)
        /// slotlardaki kupleri yola geri dok. Kup sayisi korunur -> tir asla eksik kalmaz.
        /// </summary>
        private void CancelIncomingAndDrainPartials(CargoCartView cart)
        {
            for (int i = 0; i < activeCargo.Count; i++)
            {
                ActiveCargo c = activeCargo[i];
                if (c.IsCollecting && c.DestinationCart == cart)
                {
                    c.IsCollecting = false;
                    c.DestinationCart = null;
                    c.DestinationColumn = -1;
                    c.Age = 0f;
                    if (c.Visual != null)
                    {
                        c.Distance = FindNearestPathDistance(c.Visual.transform.position);
                        c.PreviousDistance = c.Distance;
                    }
                }
            }

            List<CargoCartView.PartialInfo> partials = cart.DrainPartialSlots();
            if (partials.Count > 0)
            {
                Vector3 origin = cart.GetRearExitPoint();
                float dock = FindNearestPathDistance(origin);
                for (int p = 0; p < partials.Count; p++)
                {
                    for (int k = 0; k < partials[p].Count; k++)
                    {
                        SpawnParticle(partials[p].Color, cart, -1, origin, dock);
                    }
                }
            }
        }

        private IEnumerator SpawnFrontBurst(CargoCartView cart, List<CargoCartView.ReleasedCube> released)
        {
            // CALISAN SISTEM: kupler kendi DIZILI SLOT konumlarindan ucar (tek nokta degil).
            // Yola giris noktasi (dock) yine en yakin path noktasi - oraya akarlar.
            Vector3 cartRear = GetSharedExitOrigin(cart);
            float dockDist = FindNearestPathDistance(cartRear);

            int particlesPerSlot = Mathf.Max(1, particlesPerTap);

            burstingCarts.Add(cart); // SADECE bu tir burst boyunca kilitli (diger tirlar serbest)
            for (int i = 0; i < released.Count; i++)
            {
                if (state != GameState.Playing) { burstingCarts.Remove(cart); yield break; }
                var r = released[i];

                // Bu slotun gercek dunya konumu = kupun ucus baslangici
                Vector3 slotOrigin = cart.GetSlotWorldPosition(r.SlotIndex) + Vector3.up * 0.15f;

                // CALISAN SISTEM: bu slotu simdi bosalt (sirayla gorsel)
                cart.EmptySlot(r.SlotIndex);

                // CALISAN SISTEM: 2'ser 2'ser ucma - her dalga 2 kup birden, slot konumundan
                for (int p = 0; p < particlesPerSlot; p += 2)
                {
                    if (state != GameState.Playing) { burstingCarts.Remove(cart); yield break; }
                    SpawnParticle(r.Color, cart, r.SlotIndex, slotOrigin, dockDist);
                    if (p + 1 < particlesPerSlot)
                    {
                        SpawnParticle(r.Color, cart, r.SlotIndex, slotOrigin, dockDist);
                    }
                    if (particleBurstStagger > 0f)
                    {
                        yield return new WaitForSeconds(particleBurstStagger);
                    }
                }
            }
            burstingCarts.Remove(cart); // bu tirin burst'u bitti
        }

        /// <summary>
        /// Cart'Ä±n TEK Ã§Ä±kÄ±ÅŸ portu - tÃ¼m kÃ¼pler buradan fÄ±rlar ve buraya geri dÃ¶ner.
        /// Cart merkezinin Ã¼stÃ¼, dock yÃ¶nÃ¼ne hafif yatÄ±k.
        /// </summary>
        private Vector3 GetSharedExitOrigin(CargoCartView cart)
        {
            if (cart == null)
            {
                return Vector3.up * 0.55f;
            }

            return cart.GetRearExitPoint();
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

            // CALISAN SISTEM: overflow Lose KAPATILDI - kupler yolda donmasin, dolasmaya devam etsin
            // (yeni mantikta kupler ancak uyan tira iner, o yuzden yolda birikip dolasabilirler)
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

            BeginShuffleSelection();
        }

        private void BeginShuffleSelection()
        {
            shuffleSelectionMode = true;
            ShowShuffleSelectionOverlay();
        }

        private void ApplyShuffleToSelectedCart(CargoCartView cart)
        {
            if (cart == null || shuffleCount <= 0)
            {
                HideShuffleSelectionOverlay();
                return;
            }

            cart.Shuffle();
            shuffleCount--;
            HideShuffleSelectionOverlay();
            UpdateHud();
        }

        public void AddCapacity()
        {
            if (state != GameState.Playing || extraSlotCount <= 0)
            {
                return;
            }

            SpawnExtraEmptyCart();
            extraSlotCount--;
            CancelStuckCountdown(); // ekstra tir eklendi -> home sayisi artti, takilma cozulebilir
            UpdateHud();
        }

        private void WireSceneBoosterButtons()
        {
            // Canvas'i Screen Space - Overlay yap: ortografik kamerada Screen Space-Camera gorunmuyordu
            GameObject canvasGO = GameObject.Find("Canvas");
            if (canvasGO != null)
            {
                Canvas cv = canvasGO.GetComponent<Canvas>();
                if (cv != null) cv.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            Button extraButton = FindSceneButton("Extra slot button");
            if (extraButton != null)
            {
                EnsureSceneButtonAction(extraButton, "AddCapacity", AddCapacity);
                AttachButtonFeedback(extraButton);
                sceneExtraBadge = FindBadgeText(extraButton);
            }

            Button swapButton = FindSceneButton("swap button");
            if (swapButton != null)
            {
                EnsureSceneButtonAction(swapButton, "ShuffleCarts", ShuffleCarts);
                AttachButtonFeedback(swapButton);
                sceneShuffleBadge = FindBadgeText(swapButton);
            }

            Button reloadButton = FindSceneButton("reload button");
            if (reloadButton != null)
            {
                EnsureSceneButtonAction(reloadButton, "UndoLast", UndoLast);
                AttachButtonFeedback(reloadButton);
                sceneUndoBadge = FindBadgeText(reloadButton);
            }

            EnsureStuckCountdownText(); // "5 saniye sayac text"i bul + baslangicta gizle
        }

        private void EnsureMetaUi()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

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

            Transform root = canvas.transform;
            if (coinLabel == null)
            {
                GameObject coinPill = CreatePill(root, "CoinPill",
                    new Vector2(0.66f, 0.92f), new Vector2(0.84f, 0.97f),
                    new Color(0.31f, 0.21f, 0.65f, 0.95f),
                    new Color(0.10f, 0.06f, 0.22f, 1f));
                CreateIcon(coinPill.transform, "CoinIcon", new Color(1f, 0.78f, 0.22f, 1f),
                    new Vector2(0.07f, 0.18f), new Vector2(0.28f, 0.82f));
                coinLabel = CreateText(coinPill.transform, "CoinAmount", coins.ToString(),
                    new Vector2(0.30f, 0f), new Vector2(0.96f, 1f), 40, TextAnchor.MiddleLeft, Color.white);
            }

            if (winPanel == null)
            {
                winPanel = CreatePanel(root, "WinPanel", "Devam", "Seviye Tamam!", NextLevel,
                    new Color(0.16f, 0.10f, 0.30f, 0.95f));
                winRewardLabel = CreateText(winPanel.transform, "Reward", "+" + winCoinReward + " Coin",
                    new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.62f), 42, TextAnchor.MiddleCenter,
                    new Color(1f, 0.82f, 0.26f, 1f));
            }

            if (losePanel == null)
            {
                losePanel = CreatePanel(root, "LosePanel", "Tekrar", "Kaybettin", RestartLevel,
                    new Color(0.30f, 0.10f, 0.16f, 0.95f));
            }

            if (mainMenuPanel == null)
            {
                mainMenuPanel = new GameObject("MainMenuPanel");
                mainMenuPanel.transform.SetParent(root, false);
                RectTransform rect = mainMenuPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.12f, 0.24f);
                rect.anchorMax = new Vector2(0.88f, 0.76f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = mainMenuPanel.AddComponent<Image>();
                image.color = new Color(0.12f, 0.08f, 0.26f, 0.96f);
                image.sprite = GetUiRoundedSprite();
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.5f;

                Outline outline = mainMenuPanel.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.78f, 0.22f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);

                CreateText(mainMenuPanel.transform, "Title", "Color Cargo Loop",
                    new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.94f), 58, TextAnchor.MiddleCenter, Color.white);
                CreateMenuButton(mainMenuPanel.transform, "PlayButton", "Oyna",
                    new Vector2(0.20f, 0.55f), new Vector2(0.80f, 0.70f), StartGameFromMenu);
                CreateMenuButton(mainMenuPanel.transform, "RewardCoinsButton", "+" + rewardedAdCoinAmount + " Coin",
                    new Vector2(0.20f, 0.34f), new Vector2(0.80f, 0.49f), ShowRewardedCoinsStub);
                CreateMenuButton(mainMenuPanel.transform, "RemoveAdsButton", "Reklamsiz",
                    new Vector2(0.20f, 0.13f), new Vector2(0.80f, 0.28f), PurchaseRemoveAdsStub);
                removeAdsLabel = CreateText(mainMenuPanel.transform, "RemoveAdsState", "",
                    new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.10f), 26, TextAnchor.MiddleCenter,
                    new Color(0.80f, 0.77f, 1f, 0.92f));
                mainMenuPanel.SetActive(false);
            }

            UpdateRemoveAdsLabel();
        }

        private void LoadCoins()
        {
            coins = PlayerPrefs.GetInt("CCL_Coins", 0);
            removeAdsPurchased = PlayerPrefs.GetInt("CCL_RemoveAds", 0) == 1;
        }

        private void AddCoins(int amount)
        {
            coins = Mathf.Max(0, coins + amount);
            PlayerPrefs.SetInt("CCL_Coins", coins);
            PlayerPrefs.Save();
            UpdateHud();
        }

        public void ShowMainMenu()
        {
            mainMenuShownThisSession = true;
            state = GameState.Paused;
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }

        public void StartGameFromMenu()
        {
            state = GameState.Playing;
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        public void ShowRewardedCoinsStub()
        {
            AddCoins(rewardedAdCoinAmount);
            Debug.Log("[CCL] Rewarded ad stub granted +" + rewardedAdCoinAmount + " coins.");
        }

        public void PurchaseRemoveAdsStub()
        {
            removeAdsPurchased = true;
            PlayerPrefs.SetInt("CCL_RemoveAds", 1);
            PlayerPrefs.Save();
            UpdateRemoveAdsLabel();
            Debug.Log("[CCL] Remove Ads purchase stub completed.");
        }

        private void UpdateRemoveAdsLabel()
        {
            if (removeAdsLabel != null)
            {
                removeAdsLabel.text = removeAdsPurchased ? "Reklamlar kapali" : "Test stub: satin alma baglanacak";
            }
        }

        private Button CreateMenuButton(Transform parent, string buttonName, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonGo = new GameObject(buttonName);
            buttonGo.transform.SetParent(parent, false);
            RectTransform rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(1f, 0.78f, 0.22f, 1f);
            image.sprite = GetUiRoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.5f;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            CreateText(buttonGo.transform, "Label", label,
                Vector2.zero, Vector2.one, 42, TextAnchor.MiddleCenter, new Color(0.18f, 0.10f, 0.08f, 1f));
            AttachButtonFeedback(button);
            return button;
        }

        /// <summary>Butonun "adet text" cocugunu (TMP) kesin bulur (isimle), yoksa ilk TMP'yi alir.</summary>
        private TMP_Text FindBadgeText(Button button)
        {
            if (button == null) return null;
            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                string n = texts[i].gameObject.name.ToLowerInvariant();
                if (n.Contains("adet") || n.Contains("count") || n.Contains("badge")) return texts[i];
            }
            return texts.Length > 0 ? texts[0] : null;
        }

        private void EnsureSceneButtonAction(Button button, string methodName, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return;
                }
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void AttachButtonFeedback(Button button)
        {
            if (button == null) return;
            ButtonPressFeedback feedback = button.GetComponent<ButtonPressFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<ButtonPressFeedback>();
            }
            feedback.Initialize();
        }

        private Button FindSceneButton(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private void SpawnExtraEmptyCart()
        {
            // Onceden hesaplanmis REZERVE park yerine spawn (yola binmez, mevcut tirlarin ustune binmez)
            int addedExtras = bonusCarts.Count; // simdiye kadar eklenen ekstra tir sayisi -> rezerve index
            if (addedExtras >= reservedCartSlots.Count) return; // guvenlik: rezerve yer kalmadi

            GameObject resolvedCartModel = ResolveCartModelPrefab();
            int cartIndex = carts.Count + 1;
            Vector3 position = reservedCartSlots[addedExtras];

            GameObject cartObject = new GameObject("Extra_Cart_" + cartIndex);
            cartObject.transform.SetParent(cartRoot, false);
            cartObject.transform.position = position;
            cartObject.transform.rotation = Quaternion.identity;

            BoxCollider collider = cartObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.30f, 0f);
            collider.size = new Vector3(cartGridWidth + 0.5f, 1.0f, cartGridDepth + 0.5f);

            CargoCartView view = cartObject.AddComponent<CargoCartView>();
            view.Initialize(this, cartIndex, CargoColor.Red, new CargoColor?[CargoCartView.SlotCount], slotFillThreshold, new Color(0.82f, 0.74f, 0.96f), resolvedCartModel);
            view.SetAllowLastColorRelease(true);
            view.SetAcceptAnyColorWhenEmpty(true);

            carts.Add(view);
            bonusCarts.Add(view);
            BuildTruckExitRoute(view);

            // Ekstra tir eklenince: TUM tirlarda son-renk tiklanabilir olsun (oyuncu artik hep cozebilsin)
            for (int ci = 0; ci < carts.Count; ci++)
                if (carts[ci] != null) carts[ci].SetAllowLastColorRelease(true);

            cartPickupDistances.Add(FindNearestPathDistance(view.GetRearExitPoint()));
            cartHeadPickupDistances.Add(FindNearestPathDistance(view.GetHeadEntryPoint()));
            // Kamera + park zemini zaten level basinda TOTAL (base+ekstra) tira gore kurulu -> ekstra shift gerekmez
        }

        private void AdjustCameraForExtraCart(CargoCartView extraCart)
        {
            if (mainCamera == null || extraCart == null) return;

            float targetSize = Mathf.Max(mainCamera.orthographicSize, orthographicSize + 1.35f);
            mainCamera.orthographicSize = targetSize;

            Vector3 pos = mainCamera.transform.position;
            pos.x = Mathf.Lerp(pos.x, extraCart.transform.position.x * 0.28f, 0.75f);
            mainCamera.transform.position = pos;
        }

        private void ShowShuffleSelectionOverlay()
        {
            if (shuffleSelectionOverlay == null)
            {
                Material overlayMat = GetRuntimeMaterial("ShuffleSelectionOverlay", new Color(0f, 0f, 0f, 0.68f));
                SetMaterialTransparent(overlayMat, 0.68f);

                shuffleSelectionOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shuffleSelectionOverlay.name = "ShuffleSelectionOverlay";
                shuffleSelectionOverlay.transform.SetParent(fxRoot, false);
                shuffleSelectionOverlay.transform.position = new Vector3(0f, cartHeightY - 0.02f, 0f);
                shuffleSelectionOverlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                shuffleSelectionOverlay.transform.localScale = new Vector3(trackWidthX + 5f, trackDepthZ + 5f, 1f);
                Collider overlayCollider = shuffleSelectionOverlay.GetComponent<Collider>();
                if (overlayCollider != null) Destroy(overlayCollider);
                Renderer renderer = shuffleSelectionOverlay.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = overlayMat;
            }
            shuffleSelectionOverlay.SetActive(true);
            RebuildShuffleSelectionHighlights();
        }

        private void HideShuffleSelectionOverlay()
        {
            shuffleSelectionMode = false;
            if (shuffleSelectionOverlay != null)
            {
                shuffleSelectionOverlay.SetActive(false);
            }
            for (int i = 0; i < shuffleSelectionHighlights.Count; i++)
            {
                DestroySmart(shuffleSelectionHighlights[i]);
            }
            shuffleSelectionHighlights.Clear();
        }

        private void RebuildShuffleSelectionHighlights()
        {
            for (int i = 0; i < shuffleSelectionHighlights.Count; i++)
            {
                DestroySmart(shuffleSelectionHighlights[i]);
            }
            shuffleSelectionHighlights.Clear();

            Material highlightMat = GetRuntimeMaterial("ShuffleCartHighlight", new Color(1f, 0.90f, 0.22f, 0.38f));
            SetMaterialTransparent(highlightMat, 0.38f);

            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                if (cart == null || completedCarts.Contains(cart)) continue;

                GameObject h = GameObject.CreatePrimitive(PrimitiveType.Quad);
                h.name = "ShuffleHighlight_" + cart.CartIndex;
                h.transform.SetParent(fxRoot, false);
                h.transform.position = cart.transform.position + Vector3.up * 0.015f;
                h.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                h.transform.localScale = new Vector3(cartModelTargetSize * 1.18f, cartModelTargetSize * 0.54f, 1f);
                Collider highlightCollider = h.GetComponent<Collider>();
                if (highlightCollider != null) Destroy(highlightCollider);
                Renderer renderer = h.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = highlightMat;
                shuffleSelectionHighlights.Add(h);
            }
        }

        private void SetMaterialTransparent(Material material, float alpha)
        {
            if (material == null) return;
            Color c = material.color;
            c.a = alpha;
            material.color = c;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
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

            bool useToon = RuntimeToonMaterialsEnabled && color.a >= 0.999f;
            Shader shader = FindRuntimeShader(useToon);

            material = new Material(shader);
            material.name = "MAT_Runtime_" + key;
            ConfigureRuntimeMaterial(material, color, useToon);
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

            bool useToon = RuntimeToonMaterialsEnabled && baseColor.a >= 0.999f;
            Shader shader = FindRuntimeShader(useToon);

            material = new Material(shader);
            material.name = "MAT_Runtime_" + key;
            ConfigureRuntimeMaterial(material, baseColor, useToon);

            // Emission - neon glow effect
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor * intensity);
            }
            if (material.HasProperty("_EmissionStrength")) material.SetFloat("_EmissionStrength", intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            materials.Add(key, material);
            return material;
        }

        public void ApplyCartoonStyleToRenderers(GameObject root, string keyPrefix)
        {
            if (!RuntimeToonMaterialsEnabled) return;
            if (root == null) return;
            Shader toonShader = Shader.Find(ToonPlasticShaderName);
            if (toonShader == null) return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null) continue;

                Material source = renderer.sharedMaterial;
                string key = keyPrefix + "_" + i + "_" + source.name;
                Material material;
                if (!materials.TryGetValue(key, out material))
                {
                    Color color = GetMaterialColor(source);
                    material = new Material(toonShader);
                    material.name = "MAT_Runtime_" + key;
                    ConfigureRuntimeMaterial(material, color, true);

                    Texture texture = GetMaterialTexture(source);
                    if (texture != null)
                    {
                        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    }

                    materials.Add(key, material);
                }

                renderer.sharedMaterial = material;
            }
        }

        private Shader FindRuntimeShader(bool preferToon)
        {
            Shader shader = preferToon ? Shader.Find(ToonPlasticShaderName) : null;
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            return shader;
        }

        private void ConfigureRuntimeMaterial(Material material, Color color, bool toon)
        {
            material.color = color;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", toon ? new Color(1f, 1f, 1f, color.a) : color);
            }

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.7f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);

            if (!toon) return;

            Color shadow = Color.Lerp(color, new Color(0.16f, 0.12f, 0.32f), 0.52f);
            Color rim = Color.Lerp(Color.white, color, 0.18f);
            if (material.HasProperty("_ShadowColor")) material.SetColor("_ShadowColor", shadow);
            if (material.HasProperty("_ShadeStrength")) material.SetFloat("_ShadeStrength", 0.44f);
            if (material.HasProperty("_RampThreshold")) material.SetFloat("_RampThreshold", 0.45f);
            if (material.HasProperty("_HighlightColor")) material.SetColor("_HighlightColor", Color.white);
            if (material.HasProperty("_HighlightStrength")) material.SetFloat("_HighlightStrength", 0.28f);
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", rim);
            if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength", 0.16f);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", new Color(0.045f, 0.035f, 0.10f, 1f));
            if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", 0.010f);
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("baseColorFactor")) return material.GetColor("baseColorFactor");
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return material.color;
        }

        private static Texture GetMaterialTexture(Material material)
        {
            if (material == null) return null;
            if (material.HasProperty("_BaseMap"))
            {
                Texture texture = material.GetTexture("_BaseMap");
                if (texture != null) return texture;
            }
            if (material.HasProperty("_MainTex"))
            {
                Texture texture = material.GetTexture("_MainTex");
                if (texture != null) return texture;
            }
            return null;
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
                    if (cargo.Visual != null && !cargo.Visual.activeSelf) cargo.Visual.SetActive(true);
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
                    if (cargo.Visual != null && !cargo.Visual.activeSelf) cargo.Visual.SetActive(true);
                    cargo.FlyProgress += Time.deltaTime * cargoCollectSpeed;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cargo.FlyProgress));
                    Vector3 arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.24f;
                    cargo.Visual.transform.position = Vector3.Lerp(cargo.FlyStart, cargo.FlyTarget, t) + arc;
                    cargo.Visual.transform.localScale = cargo.BaseScale;
                    cargo.Visual.transform.Rotate(cargo.TumbleAxis, cargo.TumbleSpeed * 0.5f * Time.deltaTime, Space.World);

                    if (t >= 1f)
                    {
                        // Kup inemezse CollectCargo false doner -> listede kalir, yola geri doner
                        if (CollectCargo(cargo)) activeCargo.RemoveAt(i);
                    }

                    continue;
                }

                // Yol Ã¼stÃ¼ hareket - SABÄ°T hÄ±z, hiÃ§ yavaÅŸlama yok (yol akÄ±yor hissi)
                cargo.Age += Time.deltaTime;
                cargo.PreviousDistance = cargo.Distance;
                cargo.Distance += currentLevel.CargoMoveSpeed * roadSpeedMultiplier * Time.deltaTime;
                if (TryTeleportThroughTunnel(cargo.PreviousDistance, ref cargo.Distance))
                {
                    cargo.PreviousDistance = cargo.Distance - 0.001f;
                }

                // YÄ±ÄŸÄ±n efekti: lateral + vertical offset uygulanÄ±r
                Vector3 position = GetCargoRoadPosition(cargo.Distance, cargo.LaneOffset, cargo.VerticalOffset);
                bool hideInTunnel = IsCargoInsideTunnel(cargo.Distance);
                if (cargo.Visual != null && cargo.Visual.activeSelf != !hideInTunnel)
                {
                    cargo.Visual.SetActive(!hideInTunnel);
                }
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

        private bool IsCargoInsideTunnel(float distance)
        {
            if (!enableTunnels || path.TotalLength <= 0f) return false;
            distance = Mathf.Repeat(distance, path.TotalLength);
            return (tunnelTopActive && distance >= tunnelTopStart && distance <= tunnelTopEnd)
                || (tunnelBotActive && distance >= tunnelBotStart && distance <= tunnelBotEnd);
        }

        private bool TryTeleportThroughTunnel(float previousDistance, ref float currentDistance)
        {
            if (!enableTunnels || path.TotalLength <= 0f) return false;
            if (tunnelTopActive && path.DidCross(previousDistance, currentDistance, tunnelTopStart))
            {
                currentDistance += tunnelTopEnd - tunnelTopStart;
                return true;
            }
            if (tunnelBotActive && path.DidCross(previousDistance, currentDistance, tunnelBotStart))
            {
                currentDistance += tunnelBotEnd - tunnelBotStart;
                return true;
            }
            return false;
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
                // CALISAN SISTEM: 2-yon dolma - hem rear hem head pickup noktasini kontrol et
                float rearPickup = cartPickupDistances[i];
                float headPickup = (i < cartHeadPickupDistances.Count) ? cartHeadPickupDistances[i] : rearPickup;
                bool crossedRear = path.DidCross(cargo.PreviousDistance, cargo.Distance, rearPickup);
                bool crossedHead = path.DidCross(cargo.PreviousDistance, cargo.Distance, headPickup);
                if (!crossedRear && !crossedHead) continue;

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

        /// <summary>
        /// Kup hedef slota varinca cagrilir.
        /// return true: kup tuketildi (listeden cikar). false: slot uygun degildi, kup YOLA GERI BIRAKILDI (kaybetme).
        /// </summary>
        private bool CollectCargo(ActiveCargo cargo)
        {
            if (cargo.DestinationCart != null && cargo.DestinationColumn >= 0)
            {
                bool deposited;
                bool slotJustBecameFull = cargo.DestinationCart.AddParticleToSlot(cargo.DestinationColumn, cargo.Color, out deposited);

                if (!deposited)
                {
                    // Slot artik uygun degil (dolmus/renk degismis) -> kupu KAYBETME, yola geri birak
                    cargo.IsCollecting = false;
                    cargo.DestinationCart = null;
                    cargo.DestinationColumn = -1;
                    cargo.Age = 0f;
                    if (cargo.Visual != null)
                    {
                        cargo.Distance = FindNearestPathDistance(cargo.Visual.transform.position);
                        cargo.PreviousDistance = cargo.Distance;
                    }
                    return false; // listede kalsin, dolasmaya devam etsin
                }

                clearedCount++;
                if (slotJustBecameFull && !completedCarts.Contains(cargo.DestinationCart))
                {
                    if (IsCartSolved(cargo.DestinationCart))
                    {
                        StartCoroutine(DepartSolvedCart(cargo.DestinationCart));
                    }
                }
            }
            else
            {
                clearedCount++;
            }

            Destroy(cargo.Visual);
            UpdateHud();
            return true;
        }

        private bool AreAllCartsSolved()
        {
            if (carts.Count == 0) return false;
            for (int i = 0; i < carts.Count; i++)
            {
                CargoColor c;
                if (!carts[i].IsSingleColor(out c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Bir tir "BITTI" sayilir: TEK RENK (tam dolu sart DEGIL) + o renk baska tirda/yolda KALMAMIS
        /// (yani o renk tamamen bu tirda toplanmis). Boylece yarim tir erken gidip rengi ortada birakmaz.
        /// </summary>
        private bool IsCartSolved(CargoCartView cart)
        {
            if (cart == null) return false;
            CargoColor c;
            if (!cart.IsSingleColor(out c)) return false;

            // Ayni renk baska (tamamlanmamis) tirda var mi?
            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView other = carts[i];
                if (other == null || other == cart || completedCarts.Contains(other)) continue;
                colorScratch.Clear();
                other.CollectPresentColors(colorScratch);
                if (colorScratch.Contains(c)) return false;
            }
            // Ayni renk yolda (ucan) var mi? -> gelecekse bekle
            for (int i = 0; i < activeCargo.Count; i++)
            {
                if (activeCargo[i].Color == c) return false;
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

                if (IsCartSolved(cart))
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
            Vector3 baseScale = cart.transform.localScale;
            Vector3 exitTarget = start + Vector3.right * 1.15f; // kisa mesafe, saga dogru

            // CALISAN SISTEM: TEK PARCA akici surus (gidip-durup-gitme YOK)
            float t = 0f;
            float driveDur = 0.34f;
            while (t < 1f)
            {
                t += Time.deltaTime / driveDur;
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                cart.transform.position = Vector3.Lerp(start, exitTarget, eased);
                cart.transform.rotation = Quaternion.identity;
                yield return null;
            }

            // POP: hizlica buyu sonra kuculup yok ol
            t = 0f;
            float popUpDur = 0.10f;
            while (t < 1f)
            {
                t += Time.deltaTime / popUpDur;
                cart.transform.localScale = baseScale * Mathf.Lerp(1f, 1.25f, Mathf.Clamp01(t));
                yield return null;
            }
            t = 0f;
            float popDownDur = 0.12f;
            while (t < 1f)
            {
                t += Time.deltaTime / popDownDur;
                cart.transform.localScale = baseScale * Mathf.Lerp(1.25f, 0f, Mathf.Clamp01(t));
                yield return null;
            }

            cart.transform.localScale = baseScale;
            cart.gameObject.SetActive(false);
            UpdateHud();

            if (AreRequiredCartsCompleted())
            {
                Win();
            }
        }

        private bool AreRequiredCartsCompleted()
        {
            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                if (cart == null || bonusCarts.Contains(cart)) continue;
                if (!completedCarts.Contains(cart)) return false;
            }
            return carts.Count > bonusCarts.Count;
        }

        private void CheckLoseByNoMoves()
        {
            // LOSE KALDIRILDI (kullanici istegi: rahat/casual akis - oyuncu hic kaybetmez).
            // Geri sayim/lose tetiklenmez; varsa sayac gizli kalir. (Cozum icin Undo/Shuffle/+1 hep var.)
            CancelStuckCountdown();
        }

        /// <summary>
        /// Oyuncunun yapabilecegi URETKEN bir hamle var mi?
        /// Uretken = bir tirin ON salacagi rengin EVI var (hedef-renk tiri ya da bos buffer),
        /// VEYA yolda evi olan (inecek) kup var. Sadece evsiz renk kaldiysa -> uretken hamle YOK.
        /// </summary>
        private bool HasProductiveMove()
        {
            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView cart = carts[i];
                if (cart == null || completedCarts.Contains(cart)) continue;
                CargoColor front;
                if (cart.TryGetReleasableFrontColor(out front) && ColorHasHome(front, cart)) return true;
            }
            for (int i = 0; i < activeCargo.Count; i++)
            {
                if (ColorHasHome(activeCargo[i].Color, null)) return true; // yolda inecek kup -> hala uretken
            }
            return false;
        }

        // c rengi icin bir "ev" var mi? (exclude haric tamamlanmamis tir; hedef-renk == c VEYA bos buffer)
        private bool ColorHasHome(CargoColor c, CargoCartView exclude)
        {
            for (int i = 0; i < carts.Count; i++)
            {
                CargoCartView h = carts[i];
                if (h == null || h == exclude || completedCarts.Contains(h)) continue;
                if (h.TargetColor == c) return true;        // hedef-renk tiri
                if (h.IsCompletelyEmpty()) return true;     // bos buffer -> her rengi alir
            }
            return false;
        }

        // Sahnedeki "5 saniye sayac text" (TMP) referansini bulur (Inspector'dan atanmadiysa isimle).
        // Sayac aktif DEGILSE her zaman gizli tutar (Inspector'dan atanmis/sahnede acik birakilmis olsa bile).
        private void EnsureStuckCountdownText()
        {
            if (stuckCountdownText == null)
            {
                GameObject canvasGO = GameObject.Find("Canvas");
                if (canvasGO != null)
                {
                    foreach (Transform ch in canvasGO.transform)
                    {
                        if (ch.name.IndexOf("saniye", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            stuckCountdownText = ch.GetComponent<TMP_Text>();
                            break;
                        }
                    }
                }
            }
            if (stuckCountdownText != null)
            {
                if (stuckCountdownRect == null) stuckCountdownRect = stuckCountdownText.rectTransform;
                if (!stuckCountdownActive) stuckCountdownText.gameObject.SetActive(false); // sayac yokken gizli
            }
        }

        private void StartStuckCountdown()
        {
            EnsureStuckCountdownText();
            stuckCountdownActive = true;
            stuckCountdownRemaining = Mathf.Max(1f, stuckGraceSeconds);
            stuckShowTime = 0f;
            stuckPulseTime = 999f;
            stuckShownSecond = -1;
            if (stuckCountdownText != null) stuckCountdownText.gameObject.SetActive(true);
        }

        private void CancelStuckCountdown()
        {
            stuckCountdownActive = false;
            if (stuckCountdownText != null)
            {
                if (stuckCountdownRect != null) stuckCountdownRect.localScale = Vector3.one;
                stuckCountdownText.gameObject.SetActive(false);
            }
        }

        private void TickStuckCountdown()
        {
            if (!stuckCountdownActive) return;
            stuckCountdownRemaining -= Time.deltaTime;
            stuckShowTime += Time.deltaTime;
            stuckPulseTime += Time.deltaTime;

            int sec = Mathf.CeilToInt(Mathf.Max(0f, stuckCountdownRemaining));
            if (sec != stuckShownSecond)
            {
                stuckShownSecond = sec;
                stuckPulseTime = 0f; // her yeni saniyede pulse
                if (stuckCountdownText != null) stuckCountdownText.text = sec.ToString();
            }

            // ANIMASYON: ortada pop-in (ilk 0.3s overshoot) + her saniye nabiz (pulse)
            if (stuckCountdownRect != null)
            {
                float popIn = EaseOutBack(Mathf.Clamp01(stuckShowTime / 0.3f));
                float pulse = 0.28f * Mathf.Max(0f, 1f - stuckPulseTime / 0.30f);
                stuckCountdownRect.localScale = Vector3.one * (popIn * (1f + pulse));
            }

            if (stuckCountdownRemaining <= 0f)
            {
                CancelStuckCountdown();
                Lose();
            }
        }

        // Pop-up overshoot egrisi (0 -> ~1, ortada hafif buyuyup oturur)
        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
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
            coinRewardGrantedThisLevel = false;

            currentLevelIndex = Mathf.Clamp(levelIndex, 1, levels.Count);
            currentLevel = levels[currentLevelIndex - 1];
            maxLoopCapacity = currentLevel.MaxLoopCapacity;
            clearedCount = 0;
            releaseHistory.Clear();
            undoCount = undoStartCount;
            shuffleCount = shuffleStartCount;
            // +1 booster sayisi = bu level'da eklenebilecek ekstra tir sayisi (rezerve slot adedi)
            extraSlotCount = currentLevel != null ? Mathf.Max(0, currentLevel.ExtraCartCount) : 0;
            playerActedThisLevel = false;
            CancelStuckCountdown();

            ClearChildren(cartRoot);
            ClearChildren(cargoRoot);
            ClearChildren(targetRoot);
            ClearChildren(fxRoot);
            carts.Clear();
            activeCargo.Clear();
            colorZones.Clear();
            completedCarts.Clear();
            bonusCarts.Clear();
            truckExitRoutes.Clear();
            cartPickupDistances.Clear();
            cartHeadPickupDistances.Clear();
            burstingCarts.Clear();
            HideShuffleSelectionOverlay();

            BuildPathAndTrack();
            BuildCarts(currentLevel);

            // Her cart iÃ§in path Ã¼stÃ¼ndeki "pickup noktasÄ±" hesapla
            // KÃ¼p ancak bu noktadan geÃ§ince landing kontrolÃ¼ yapÄ±lÄ±r
            for (int i = 0; i < carts.Count; i++)
            {
                // CALISAN SISTEM: 2 pickup nokta - REAR (back) ve HEAD (sag)
                float dRear = FindNearestPathDistance(carts[i].GetRearExitPoint());
                float dHead = FindNearestPathDistance(carts[i].GetHeadEntryPoint());
                cartPickupDistances.Add(dRear);
                cartHeadPickupDistances.Add(dHead);
            }

            state = GameState.Playing;
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (stateLabel != null) stateLabel.gameObject.SetActive(false);
            UpdateHud();

            // FAZ 2 onizleme (Inspector toggle) - Faz 1'e dokunmaz, sadece kutulari gosterir + demo doldurur
            if (phase2BoxPreview)
            {
                if (phase2PreviewCoroutine != null) StopCoroutine(phase2PreviewCoroutine);
                phase2PreviewCoroutine = StartCoroutine(BuildPhase2Preview());
            }
        }

        /// <summary>
        /// FAZ 2 ONIZLEME (Pixel Flow tarzi): loop altina 5 SIYAH SLOT (kutu yerlestirme alani) +
        /// altina renkli kutu KUYRUGU (kapasite sayili). Sadece gorunum onayi - gercek mekanik sonra.
        /// </summary>
        private System.Collections.IEnumerator BuildPhase2Preview()
        {
            previewBoxes.Clear();
            var colors = new List<CargoColor>(currentLevel.UsedColors);
            if (colors.Count == 0) colors.Add(CargoColor.Blue);

            GameObject model = ResolveBoxModel();
            int n = 5;
            float w = 0.92f, gap = 0.22f;
            float totalW = n * w + (n - 1) * gap;
            float startX = -totalW * 0.5f + w * 0.5f;
            float zSlots = -((trackDepthZ * 0.5f) + 1.0f);   // loop altinda siyah slotlar
            float zQueue = zSlots - 1.55f;                    // altinda kutu kuyrugu

            Material darkSlot = GetRuntimeMaterial("Phase2Slot", new Color(0.10f, 0.10f, 0.14f));

            // 5 siyah aktif slot (kutu buraya konacak)
            for (int i = 0; i < n; i++)
            {
                float x = startX + i * (w + gap);
                CreateRoundedPad("Phase2Slot_" + i, cartRoot, new Vector3(x, 0.03f, zSlots), w, w, 0.14f, 0.05f, darkSlot);
            }

            // Kutu kuyrugu: renkli container + kapasite sayisi
            for (int i = 0; i < n; i++)
            {
                float x = startX + i * (w + gap);
                CargoColor c = colors[i % colors.Count];
                int cap = Random.Range(10, 25);

                GameObject go = new GameObject("Phase2Container_" + i);
                go.transform.SetParent(cartRoot, false);
                go.transform.position = new Vector3(x, 0.05f, zQueue);
                CargoBoxView box = go.AddComponent<CargoBoxView>();
                box.Initialize(this, c, cap, w * 0.82f, model);
                previewBoxes.Add(box);

                CreatePhase2Label(go.transform, cap.ToString(), new Vector3(0f, 0.6f, -w * 0.5f), 0.9f);
            }
            yield break;
        }

        private void CreatePhase2Label(Transform parent, string text, Vector3 localPos, float charSize)
        {
            GameObject t = new GameObject("CapLabel");
            t.transform.SetParent(parent, false);
            t.transform.localPosition = localPos;
            t.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // top-down kameraya yatik
            TextMesh tm = t.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 72;
            tm.characterSize = charSize * 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
            {
                tm.font = f;
                MeshRenderer mr = t.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = f.material;
            }
        }

        private void Win()
        {
            state = GameState.Won;
            if (!coinRewardGrantedThisLevel)
            {
                AddCoins(winCoinReward);
                coinRewardGrantedThisLevel = true;
            }
            // Kullanici istegi: WIN PANEL YOK (oyunda sadece LOSE panel olacak) -> kazaninca otomatik sonraki level.
            if (autoNextLevelCoroutine == null) autoNextLevelCoroutine = StartCoroutine(AutoNextLevelAfterWin());
        }

        private IEnumerator AutoNextLevelAfterWin()
        {
            yield return new WaitForSeconds(0.85f);
            autoNextLevelCoroutine = null;
            NextLevel();
        }

        private void Lose()
        {
            CancelStuckCountdown();
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

            reservedCartSlots.Clear();
            int extraCount = Mathf.Max(0, level.ExtraCartCount);
            int totalCount = cartCount + extraCount; // park alani BASE + EKSTRA(rezerve) icin hesaplanir

            // CALISAN SISTEM: auto-spacing - loop derinligi TOTAL cart sayisina bolunur (ekstra yerler de sigsin)
            // Inner loop alani = trackDepthZ - 2 * (trackCornerRadius + buffer)
            float innerDepth = Mathf.Max(2f, trackDepthZ - (trackCornerRadius * 2f) - 1.6f);
            float autoSpacing = (totalCount > 1) ? (innerDepth / totalCount) : innerDepth;
            float useSpacing = Mathf.Min(cartVerticalSpacing, Mathf.Max(0.5f, autoSpacing));

            // ===== TIR KONUMLARI: kolon (dikey istif) veya GRID (2 kolon yan yana - kucuk tir) =====
            // Per-level dizilis cesitliligi: butun istifi Z'de kaydir (her level merkezde AYNI durmasin)
            int arrSeed = Mathf.Abs(currentLevelIndex);
            float stackZShift = ((arrSeed % 5) - 2) * 0.34f;   // -0.68..+0.68 istif yukari/asagi
            var cartPositions = new Vector3[totalCount];
            bool useGrid = (level.CartLayout == CartLayout.Grid) && (totalCount >= 3);
            if (useGrid)
            {
                // 2 kolon yan yana, satirlar Z'de; tek kalan tir ortalanir (ucgen dizilim)
                int rows = (totalCount + 1) / 2;
                float colOff = 0.74f;                                  // X kolon offseti (kucuk tir icin guvenli)
                float rowSpacing = Mathf.Clamp(useSpacing * 1.7f, 1.25f, 2.4f);
                float row0Z = (rows - 1) * rowSpacing * 0.5f + stackZShift;
                for (int i = 0; i < totalCount; i++)
                {
                    int row = i / 2, col = i % 2;
                    bool lastAlone = (totalCount % 2 == 1) && (i == totalCount - 1);
                    float x = cartCenterOffsetX + (lastAlone ? 0f : (col == 0 ? -colOff : colOff));
                    float zj = (((i * 37 + arrSeed * 19) % 5) - 2) * 0.04f;
                    cartPositions[i] = new Vector3(x, cartHeightY, row0Z - row * rowSpacing + zj);
                }
            }
            else
            {
                float totalSpan = (totalCount - 1) * useSpacing;
                float startZ = totalSpan * 0.5f + stackZShift;
                for (int i = 0; i < totalCount; i++)
                {
                    float zJit = (((i * 53 + arrSeed * 29) % 7) - 3) * 0.05f;   // kucuk organik Z sapmasi
                    cartPositions[i] = new Vector3(GetCartLayoutX(level.CartLayout, i, totalCount), cartHeightY, startZ - i * useSpacing + zJit);
                }
            }

            // Per-level camera ayari - loop boyutuna gore ortho size (total tira gore)
            AdjustCameraForLevel();

            // PARK ALANI zemini: gercek tir konum bounds'undan boyutlanir (grid'de genis, kolonda uzun)
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < totalCount; i++)
            {
                minX = Mathf.Min(minX, cartPositions[i].x); maxX = Mathf.Max(maxX, cartPositions[i].x);
                minZ = Mathf.Min(minZ, cartPositions[i].z); maxZ = Mathf.Max(maxZ, cartPositions[i].z);
            }
            float groundSizeX = (maxX - minX) + cartModelTargetSize + 0.8f;
            float groundSizeZ = (maxZ - minZ) + cartModelTargetSize * 0.55f + 0.8f;
            Vector3 groundCenter = new Vector3((minX + maxX) * 0.5f, 0.02f, (minZ + maxZ) * 0.5f);
            CreateParkingGround(groundCenter, groundSizeX, groundSizeZ, 0.55f);

            for (int i = 0; i < totalCount; i++)
            {
                Vector3 position = cartPositions[i];

                // Ekstra (rezerve) slotlar: BASE tirlardan sonra gelen yerler -> +1 ile buraya spawn olur (gizli)
                if (i >= cartCount)
                {
                    reservedCartSlots.Add(position);
                    continue;
                }

                RuntimeCart cartData = level.Carts[i];

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
                view.SetAllowLastColorRelease(level.AllowLastColorRelease);
                carts.Add(view);
                BuildTruckExitRoute(view);
            }
        }

        private float GetCartLayoutX(CartLayout layout, int index, int totalCount)
        {
            if (totalCount <= 1) return cartCenterOffsetX;
            switch (layout)
            {
                case CartLayout.Staggered:
                    return cartCenterOffsetX + ((index % 2 == 0) ? -0.55f : 0.55f);
                case CartLayout.Diagonal:
                    return cartCenterOffsetX + Mathf.Lerp(-0.62f, 0.62f, index / (float)(totalCount - 1));
                case CartLayout.LeftBias:
                    return cartCenterOffsetX - 0.58f + (index % 2) * 0.26f;
                case CartLayout.RightBias:
                    return cartCenterOffsetX + 0.58f - (index % 2) * 0.26f;
                default:
                    // CenterStack bile her level hafif farkli yana yatsin (dump-merkez olmasin)
                    return cartCenterOffsetX + ((Mathf.Abs(currentLevelIndex) % 3) - 1) * 0.22f;
            }
        }

        /// <summary>
        /// CALISAN SISTEM: cart sayisina gore ortho size, levellar arasi smooth zoom
        /// </summary>
        private void AdjustCameraForLevel()
        {
            if (mainCamera == null) return;
            // BASE + EKSTRA(rezerve) tir sayisi -> park alani yuksekligi buna gore, kamera da buna gore sigar
            int cartCount = currentLevel != null ? currentLevel.Carts.Count + Mathf.Max(0, currentLevel.ExtraCartCount) : 1;
            // Loop SABIT boyut - cart sayisina gore cok az zoom-out (cok cart = hafif kucuk)
            // +1.6: kamera biraz acik kalsin ki etraftaki candy dunya gorunsun (kullanici istegi)
            float targetSize = orthographicSize + 1.6f + Mathf.Max(0, cartCount - 1) * 0.2f;
            mainCamera.orthographicSize = targetSize;
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

            // CALISAN SISTEM: siyah/mor lane ve portal GORSELLERI KALDIRILDI
            // Sadece route logic kaydedilir (cart kazaninca portal'a dogru hareket icin)
            truckExitRoutes[cart] = new TruckExitRoute
            {
                Start = start,
                PortalMouth = portalMouth,
                PortalInside = portalInside,
                Portal = null
            };
        }

        /// <summary>
        /// Yuvarlak koseli zemin paneli (plus govde + 4 disc kose). Park alani ve dekor panelleri icin ORTAK.
        /// </summary>
        private GameObject CreateRoundedPad(string name, Transform parent, Vector3 center, float sizeX, float sizeZ, float cornerRadius, float height, Material mat)
        {
            float r = Mathf.Min(cornerRadius, Mathf.Min(sizeX, sizeZ) * 0.5f);

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = center;

            GameObject barX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barX.name = "PadBarX";
            barX.transform.SetParent(root.transform, false);
            barX.transform.localScale = new Vector3(sizeX, height, Mathf.Max(0.1f, sizeZ - 2f * r));
            Destroy(barX.GetComponent<Collider>());
            barX.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject barZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barZ.name = "PadBarZ";
            barZ.transform.SetParent(root.transform, false);
            barZ.transform.localScale = new Vector3(Mathf.Max(0.1f, sizeX - 2f * r), height, sizeZ);
            Destroy(barZ.GetComponent<Collider>());
            barZ.GetComponent<Renderer>().sharedMaterial = mat;

            float cx = sizeX * 0.5f - r;
            float cz = sizeZ * 0.5f - r;
            Vector3[] corners =
            {
                new Vector3( cx, -0.002f,  cz),
                new Vector3(-cx, -0.002f,  cz),
                new Vector3( cx, -0.002f, -cz),
                new Vector3(-cx, -0.002f, -cz)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "PadCorner" + i;
                disc.transform.SetParent(root.transform, false);
                disc.transform.localPosition = corners[i];
                disc.transform.localScale = new Vector3(2f * r, height * 0.5f, 2f * r);
                Destroy(disc.GetComponent<Collider>());
                disc.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return root;
        }

        /// <summary>
        /// Tirlarin altina park alani zemini (yuvarlak koseli acik panel).
        /// </summary>
        private void CreateParkingGround(Vector3 center, float sizeX, float sizeZ, float cornerRadius)
        {
            Material groundMat = NoOutline(GetRuntimeMaterial("ParkingGround", new Color(0.99f, 0.83f, 0.70f)));
            CreateRoundedPad("ParkingGround", cartRoot, center, sizeX, sizeZ, cornerRadius, 0.04f, groundMat);
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
            PrepareTunnelRanges();

            // Yol geometrisi:
            //   - DÄ±ÅŸ duvar: tek renk + tek kapalÄ± eÄŸri mesh (yolun dÄ±ÅŸ sÄ±nÄ±rÄ±)
            //   - Ä°Ã§ duvar: tek renk + tek kapalÄ± eÄŸri mesh (yolun iÃ§ sÄ±nÄ±rÄ±)
            //   - Lane: dÄ±ÅŸ ve iÃ§ duvar arasÄ±nÄ± dolduran tek koyu zemin mesh
            //   - Flow markers: animated chevron oklar (yol akÄ±yor hissi)
            float wallOffset = 0.60f;        // duvarlarÄ±n path merkezinden uzaklÄ±ÄŸÄ±
            float wallThickness = 0.16f;     // duvar kalÄ±nlÄ±ÄŸÄ±
            float wallHeight = 0.46f;        // duvar yuksekligi (uzattik 0.30 -> 0.46, daha belirgin tup)
            float laneWidth = (wallOffset * 2f) - wallThickness; // tam arasÄ±nÄ± doldursun

            // ===== LANE (koyu zemin, iki duvar arasÄ±) =====
            Material laneMat = NoOutline(GetRuntimeMaterial("TrackLane", laneColor));
            BuildTrackBarWithTunnelCuts("Lane", 0f, laneWidth, 0.06f, -0.04f, laneMat);

            // ===== DUVARLAR (tek renk, tek mesh, kapalÄ± halka) =====
            Material wallMat = NoOutline(GetRuntimeMaterial("WallSingle", trackColor));
            BuildTrackBarWithTunnelCuts("OuterWall", +wallOffset, wallThickness, wallHeight, 0.0f, wallMat);
            BuildTrackBarWithTunnelCuts("InnerWall", -wallOffset, wallThickness, wallHeight, 0.0f, wallMat);

            // ===== AKIYOR-YOL CHEVRON OKLARI (animated) =====
            // NoOutline ZORUNLU: cevronlar kucuk -> toon outline kabugu onlari SIYAH gosteriyordu (kullanici sikayeti)
            // Renk de yumusak candy (krem-pembe) -> yola gomulur, gentle akis ipucu
            Material arrowMat = NoOutline(GetRuntimeMaterial("TrackArrow", new Color(0.98f, 0.78f, 0.74f)));
            flowMarkers.Clear();
            for (int i = 0; i < flowMarkerCount; i++)
            {
                float t = (float)i / flowMarkerCount;
                float distance = path.TotalLength * t;
                if (IsCargoInsideTunnel(distance)) continue;
                GameObject root = CreateChevronArrowRoot(distance, arrowMat);
                flowMarkers.Add(new AnimatedFlowMarker { Root = root.transform, PathDistance = distance });
            }

            // Dekoratif yollar KALDIRILDI (kullanici istegi)
            // BuildDecorRoads();

            // ===== TUNEL: ust + alt bolume "tavan kapagi" (path boyunca deforme) -> tup kapanir, tunel olur =====
            if (enableTunnels) BuildTunnels();

            // ===== CEVRE DEKOR: oyun alani disina candy objeleri (dolu/zengin) =====
            BuildEnvironmentDecor();
        }

        private static readonly Color[] CandyPalette =
        {
            new Color(1.00f, 0.55f, 0.70f), // pembe
            new Color(0.58f, 0.92f, 0.78f), // mint
            new Color(1.00f, 0.84f, 0.40f), // sari
            new Color(0.78f, 0.64f, 1.00f), // mor
            new Color(1.00f, 0.70f, 0.45f), // turuncu
            new Color(0.60f, 0.82f, 1.00f), // mavi
            new Color(0.99f, 0.97f, 0.95f), // krem/beyaz
        };

        /// <summary>Oyun alani (loop) DISINA candy dekor objeleri serpistirir - dolu/zengin candy dunya.</summary>
        private void BuildEnvironmentDecor()
        {
            GameObject root = new GameObject("EnvDecor");
            root.transform.SetParent(trackRoot, false);

            float loopHalfX = trackWidthX * 0.5f + trackCornerRadius;
            float loopHalfZ = trackDepthZ * 0.5f + trackCornerRadius;
            float bandX = loopHalfX + 3.2f; // loop kenarina yakin gorunur bant
            float bandZ = loopHalfZ + 5.0f;

            System.Random rng = new System.Random(20260603);
            int target = 120; // dolu/zengin
            int placed = 0, attempts = 0;
            while (placed < target && attempts < target * 8)
            {
                attempts++;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * bandX;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * bandZ;
                // Oyun alani (loop + park) icindeyse atla
                if (Mathf.Abs(x) < loopHalfX + 0.6f && Mathf.Abs(z) < loopHalfZ + 0.6f) continue;
                if (Mathf.Abs(x) > loopHalfX && Mathf.Abs(z) < loopHalfZ * 0.6f) continue; // ekran disi sol/sag orta serit -> atla
                Color col = CandyPalette[rng.Next(CandyPalette.Length)];
                float s = 0.55f + (float)rng.NextDouble() * 0.75f;
                int type = rng.Next(4);
                float yaw = (float)(rng.NextDouble() * 360.0);
                BuildCandyProp(root.transform, type, new Vector3(x, 0f, z), s, col, yaw);
                placed++;
            }
        }

        private void BuildCandyProp(Transform parent, int type, Vector3 pos, float scale, Color col, float yaw)
        {
            string key = "Candy_" + Mathf.RoundToInt(col.r * 255f) + "_" + Mathf.RoundToInt(col.g * 255f) + "_" + Mathf.RoundToInt(col.b * 255f);
            Material mat = GetRuntimeMaterial(key, col);

            GameObject g = new GameObject("CandyProp");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (type == 0) // gumball: kure
            {
                AddDecorPrimitive(g.transform, PrimitiveType.Sphere, new Vector3(0f, scale * 0.5f, 0f), Vector3.one * scale, mat);
            }
            else if (type == 1) // gummy: bastirilmis kure
            {
                AddDecorPrimitive(g.transform, PrimitiveType.Sphere, new Vector3(0f, scale * 0.34f, 0f), new Vector3(scale, scale * 0.62f, scale), mat);
            }
            else if (type == 2) // lolipop: cubuk + kure kafa
            {
                Material stickMat = GetRuntimeMaterial("CandyStick", new Color(0.98f, 0.97f, 0.94f));
                AddDecorPrimitive(g.transform, PrimitiveType.Cylinder, new Vector3(0f, scale * 0.55f, 0f), new Vector3(scale * 0.10f, scale * 0.55f, scale * 0.10f), stickMat);
                AddDecorPrimitive(g.transform, PrimitiveType.Sphere, new Vector3(0f, scale * 1.20f, 0f), Vector3.one * (scale * 0.72f), mat);
            }
            else // naneli disk: yassi kure
            {
                AddDecorPrimitive(g.transform, PrimitiveType.Sphere, new Vector3(0f, scale * 0.18f, 0f), new Vector3(scale * 1.05f, scale * 0.36f, scale * 1.05f), mat);
            }
        }

        private void AddDecorPrimitive(Transform parent, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(prim);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = localScale;
            Collider col = g.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = g.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // Zemin/yol/tunel materyallerinde toon SIYAH outline'i kapatir (kesik/ek yerlerinde siyah cizgi olusmasin).
        // Tirlar ve kupler outline'li kalir (cartoon karakterler).
        private Material NoOutline(Material m)
        {
            if (m != null && m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0f);
            return m;
        }

        /// <summary>
        /// Loop'un UST ve ALT bolgesine, yol tupunu kapatan bir "tavan" mesh'i koyar (BuildCurvedBar ile
        /// path boyunca deforme). O bolgeye giren kup gizlenir -> diger agizdan cikiyormus gibi (teleport hissi).
        /// </summary>
        private void BuildTunnels()
        {
            Material tunnelMat = NoOutline(GetRuntimeMaterial("TunnelCover", tunnelCoverColor));
            if (tunnelTopActive)
            {
                BuildTunnelMouths("TunnelTop", tunnelTopStart, tunnelTopEnd, tunnelMat);
            }
            if (tunnelBotActive)
            {
                BuildTunnelMouths("TunnelBot", tunnelBotStart, tunnelBotEnd, tunnelMat);
            }
        }

        private void PrepareTunnelRanges()
        {
            tunnelTopActive = false;
            tunnelBotActive = false;
            tunnelTopZ = 99999f;
            tunnelBotZ = -99999f;
            float total = path.TotalLength;
            if (!enableTunnels || currentLevel == null || !currentLevel.Tunnels || total <= 0f) return;

            // Path'in z araligini bul
            const int N = 256;
            float maxZ = -99999f, minZ = 99999f;
            for (int i = 0; i <= N; i++)
            {
                float z = path.GetPosition(total * i / N).z;
                if (z > maxZ) maxZ = z;
                if (z < minZ) minZ = z;
            }

            float band = Mathf.Max(0.4f, trackCornerRadius * tunnelReachRatio);
            tunnelTopZ = maxZ - band;
            tunnelBotZ = minZ + band;

            tunnelTopActive = FindZArc(tunnelTopZ, 99999f, out tunnelTopStart, out tunnelTopEnd);
            tunnelBotActive = FindZArc(-99999f, tunnelBotZ, out tunnelBotStart, out tunnelBotEnd);
        }

        private void BuildTrackBarWithTunnelCuts(string barName, float sideOffset, float width, float height, float baseY, Material material)
        {
            if (!enableTunnels || (!tunnelTopActive && !tunnelBotActive))
            {
                BuildCurvedBar(barName, 0f, 0f, sideOffset, width, height, baseY, material, closedLoop: true);
                return;
            }

            var hidden = new List<Vector2>();
            if (tunnelTopActive) hidden.Add(new Vector2(tunnelTopStart, tunnelTopEnd));
            if (tunnelBotActive) hidden.Add(new Vector2(tunnelBotStart, tunnelBotEnd));
            hidden.Sort((a, b) => a.x.CompareTo(b.x));

            float cursor = 0f;
            int part = 0;
            for (int i = 0; i < hidden.Count; i++)
            {
                if (hidden[i].x > cursor + 0.05f)
                {
                    BuildCurvedBar(barName + "_Part" + part, cursor, hidden[i].x, sideOffset, width, height, baseY, material, false);
                    part++;
                }
                cursor = Mathf.Max(cursor, hidden[i].y);
            }

            if (path.TotalLength > cursor + 0.05f)
            {
                BuildCurvedBar(barName + "_Part" + part, cursor, path.TotalLength, sideOffset, width, height, baseY, material, false);
            }
        }

        // Path uzerinde z'si [zMin,zMax] araliginda olan SUREKLI yay'in mesafe araligini bulur (wrap yok varsayimi: ust/alt bolge tek yay).
        private bool FindZArc(float zMin, float zMax, out float dStart, out float dEnd)
        {
            dStart = 0f; dEnd = 0f;
            float total = path.TotalLength;
            const int N = 256;
            int firstIn = -1, lastIn = -1;
            for (int i = 0; i <= N; i++)
            {
                float z = path.GetPosition(total * i / N).z;
                if (z >= zMin && z <= zMax)
                {
                    if (firstIn < 0) firstIn = i;
                    lastIn = i;
                }
            }
            if (firstIn < 0) return false;
            // Bir adim disari tasir ki tunel agzi tam gorunur yola otursun
            dStart = total * Mathf.Max(0f, firstIn - 0.5f) / N;
            dEnd = total * Mathf.Min(N, lastIn + 0.5f) / N;
            return true;
        }

        private void BuildTunnelCover(string coverName, float dStart, float dEnd, Material mat)
        {
            const float wallOffset = 0.60f;                  // BuildPathAndTrack ile ayni
            float coverWidth = (wallOffset * 2f) + 0.45f;    // duvarlari ve kesik agiz boslugunu ortsun
            const float coverBaseY = 0.42f;                  // duvar ustu (wallHeight 0.46) -> tup tavanini kapat
            const float coverHeight = 0.16f;
            BuildCurvedBar(coverName, dStart, dEnd, 0f, coverWidth, coverHeight, coverBaseY, mat, false);
        }

        private void BuildTunnelMouths(string baseName, float dStart, float dEnd, Material mat)
        {
            float mouthLength = Mathf.Min(0.85f, Mathf.Max(0.05f, (dEnd - dStart) * 0.32f));
            float overlap = 0.32f;
            BuildTunnelCover(baseName + "_In", Mathf.Max(0f, dStart - overlap), dStart + mouthLength, mat);
            BuildTunnelCover(baseName + "_Out", dEnd - mouthLength, Mathf.Min(path.TotalLength, dEnd + overlap), mat);
        }

        /// <summary>
        /// Loop disindaki bos ekrani dolduran DEKORATIF paneller - park alani tarzi (yuvarlak koseli).
        /// Collider yok, gameplaye etkisiz. Ust/alt bantta park yeri cizgili paneller.
        /// </summary>
        private void BuildDecorRoads()
        {
            Material padMat = GetRuntimeMaterial("DecorPad", new Color(0.80f, 0.81f, 0.86f));
            Material slotMat = GetRuntimeMaterial("DecorPadSlot", new Color(0.69f, 0.70f, 0.76f));

            float y = 0.02f;
            float zBand = (trackDepthZ * 0.5f) + 2.6f;                  // loop'a DEGMESIN
            float padX = trackWidthX + (trackCornerRadius * 2f) + 2.0f; // ekran genisligini doldur
            float padZ = 2.3f;
            float radius = 0.7f;

            CreateDecorPad("DecorPadTop", new Vector3(0f, y, zBand), padX, padZ, radius, padMat, slotMat);
            CreateDecorPad("DecorPadBot", new Vector3(0f, y, -zBand), padX, padZ, radius, padMat, slotMat);
        }

        private void CreateDecorPad(string name, Vector3 center, float sizeX, float sizeZ, float radius, Material padMat, Material slotMat)
        {
            GameObject pad = CreateRoundedPad(name, trackRoot, center, sizeX, sizeZ, radius, 0.04f, padMat);

            // Park yeri bolme cizgileri (ince koyu cubuklar) - parking lot hissi
            int slots = Mathf.Max(3, Mathf.RoundToInt(sizeX / 1.6f));
            float inner = sizeX - 2f * radius;
            for (int i = 1; i < slots; i++)
            {
                float t = (float)i / slots;
                float px = Mathf.Lerp(-inner * 0.5f, inner * 0.5f, t);
                GameObject divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                divider.name = "Slot" + i;
                divider.transform.SetParent(pad.transform, false);
                divider.transform.localPosition = new Vector3(px, 0.03f, 0f);
                divider.transform.localScale = new Vector3(0.08f, 0.02f, sizeZ * 0.62f);
                Destroy(divider.GetComponent<Collider>());
                divider.GetComponent<Renderer>().sharedMaterial = slotMat;
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
                bool hidden = IsCargoInsideTunnel(m.PathDistance);
                if (m.Root.gameObject.activeSelf != !hidden)
                {
                    m.Root.gameObject.SetActive(!hidden);
                }
                if (hidden) continue;
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
                case PathDesign.Serpentine:
                    AddSerpentinePath();
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

        /// <summary>Kivrimli/serpantin kapali yol: yan kenarlar dalgali (sinus), ust/alt duz, koseler yuvarlak.</summary>
        private void AddSerpentinePath()
        {
            var pts = new System.Collections.Generic.List<Vector3>();
            float r = Mathf.Min(trackCornerRadius, 1.3f);
            float hw = trackWidthX * 0.5f - r;
            float hd = trackDepthZ * 0.5f - r;
            // Level'e gore hafif degisken ama YUMUSAK kivrim (cok keskin peak -> yol katlanip dark artifact yapiyordu)
            const int waves = 2;                                                      // 2 dalga (3 cok keskindi)
            float amp = 0.50f + (Mathf.Abs(currentLevelIndex) % 3) * 0.08f;           // 0.50 / 0.58 / 0.66 (moderate, smooth)
            const int side = 120;     // yogun ornekleme -> yol katlanmaz, smooth kivrim
            const int cs = 10;        // kose ornek
            int i; float a, t, z, w;

            // TR corner 0->90
            for (i = 0; i <= cs; i++) { a = Mathf.Deg2Rad * Mathf.Lerp(0f, 90f, i / (float)cs); pts.Add(new Vector3(hw + Mathf.Cos(a) * r, 0f, hd + Mathf.Sin(a) * r)); }
            // TOP (duz): x hw -> -hw
            for (i = 1; i < side; i++) { t = i / (float)side; pts.Add(new Vector3(Mathf.Lerp(hw, -hw, t), 0f, hd + r)); }
            // TL corner 90->180
            for (i = 0; i <= cs; i++) { a = Mathf.Deg2Rad * Mathf.Lerp(90f, 180f, i / (float)cs); pts.Add(new Vector3(-hw + Mathf.Cos(a) * r, 0f, hd + Mathf.Sin(a) * r)); }
            // LEFT (DALGALI): z hd -> -hd
            for (i = 1; i < side; i++) { t = i / (float)side; z = Mathf.Lerp(hd, -hd, t); w = amp * Mathf.Sin(t * waves * Mathf.PI * 2f); pts.Add(new Vector3(-(hw + r) - w, 0f, z)); }
            // BL corner 180->270
            for (i = 0; i <= cs; i++) { a = Mathf.Deg2Rad * Mathf.Lerp(180f, 270f, i / (float)cs); pts.Add(new Vector3(-hw + Mathf.Cos(a) * r, 0f, -hd + Mathf.Sin(a) * r)); }
            // BOTTOM (duz): x -hw -> hw
            for (i = 1; i < side; i++) { t = i / (float)side; pts.Add(new Vector3(Mathf.Lerp(-hw, hw, t), 0f, -(hd + r))); }
            // BR corner 270->360
            for (i = 0; i <= cs; i++) { a = Mathf.Deg2Rad * Mathf.Lerp(270f, 360f, i / (float)cs); pts.Add(new Vector3(hw + Mathf.Cos(a) * r, 0f, -hd + Mathf.Sin(a) * r)); }
            // RIGHT (DALGALI): z -hd -> hd
            for (i = 1; i < side; i++) { t = i / (float)side; z = Mathf.Lerp(-hd, hd, t); w = amp * Mathf.Sin(t * waves * Mathf.PI * 2f); pts.Add(new Vector3((hw + r) + w, 0f, z)); }

            // YUMUSATMA: kink'leri / kenar bukulmesini gider (kapali loop Laplacian)
            // 5 iter + yogun ornek -> peak'ler yumusak, yol katlanmaz (dark artifact gider)
            for (int it = 0; it < 5; it++)
            {
                var sm = new System.Collections.Generic.List<Vector3>(pts.Count);
                for (int k = 0; k < pts.Count; k++)
                {
                    Vector3 prev = pts[(k - 1 + pts.Count) % pts.Count];
                    Vector3 next = pts[(k + 1) % pts.Count];
                    sm.Add(Vector3.Lerp(pts[k], (prev + next) * 0.5f, 0.5f));
                }
                pts = sm;
            }

            path.SetPoints(pts);
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
            mainCamera.orthographicSize = orthographicSize;
            // Kamera bi tik geri (-Z) -> loop YUKARI kayar, altta buton icin yer acilir
            mainCamera.transform.position = new Vector3(0f, 12.0f, -7.0f);
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
            floor.transform.localScale = new Vector3(60f, 0.1f, 90f);
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
                if (bonusCarts.Contains(cart)) continue;
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
            if (sceneShuffleBadge != null) sceneShuffleBadge.text = shuffleCount.ToString();
            if (sceneExtraBadge != null) sceneExtraBadge.text = extraSlotCount.ToString();
            if (sceneUndoBadge != null) sceneUndoBadge.text = undoCount.ToString();
            if (coinLabel != null) coinLabel.text = coins.ToString();
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
            public CartLayout CartLayout;
            public bool AllowLastColorRelease; // ileri levellerde true: son renk de tiklanabilir
            public int ExtraCartCount;         // bu level'da eklenebilecek BOS tir sayisi (renk>tir cozumu / lose mantigi)
            public bool Tunnels = false;       // bu level'da tunel/kesik var mi - DEFAULT KAPALI (temiz yol); sadece ileri proc levellerde acilir
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
                // Renkler: B=Mavi, G=Yesil, Y=Sari, P=Mor (4 renk). Yapi: her cart FRONT=non-target (atilir), BACK=target.
                // Cesitlilik: farkli yol tasarimlari + 2-4 tir + 2-4 renk + ekstra-tir (renk>tir) levelleri.
                // Tum levellar tasarim geregi KAZANILABILIR (her rengin toplami <= 16, renk sayisi = tir + ekstra).
                var list = new List<RuntimeLevel>
                {
                    // L1 - GIRIS: 2 tir, 2 renk (B/G) - KIVRIMLI yol (serpantin), tunel YOK
                    NoTun(Level(PathDesign.Serpentine, 200, 1.38f,
                        Cart(G, Slots(B, B, B, B, B, B, G, G, G, G, G, G, G, G, G, G)),
                        Cart(B, Slots(G, G, G, G, G, G, B, B, B, B, B, B, B, B, B, B)))),

                    // L2 - 3 tir, 3 renk (B/G/Y)
                    Level(PathDesign.RoundedLoop, 250, 1.44f,
                        Cart(G, Slots(B, B, B, B, B, Y, Y, Y, Y, Y, Y, G, G, G, G, G)),
                        Cart(Y, Slots(G, G, G, G, G, G, B, B, B, B, B, B, B, Y, Y, Y)),
                        Cart(B, Slots(Y, Y, Y, Y, Y, Y, Y, G, G, G, G, G, B, B, B, B))),

                    // L3 - 3 tir, 3 renk (B/G/Y) - COZULEBILIR (renk=tir): her renk toplam 16, tiklayarak siralanir
                    Adv(Level(PathDesign.WideLoop, 320, 1.45f,
                        Cart(G, Slots(B, B, B, B, Y, Y, Y, Y, G, G, G, G, G, G, G, G)),
                        Cart(B, Slots(G, G, G, G, Y, Y, Y, Y, B, B, B, B, B, B, B, B)),
                        Cart(Y, Slots(G, G, G, G, B, B, B, B, Y, Y, Y, Y, Y, Y, Y, Y)))),

                    // L4 - 4 tir, 4 renk (ileri)
                    Adv(Level(PathDesign.WideLoop, 300, 1.50f,
                        Cart(B, Slots(Y, Y, Y, Y, G, G, G, G, B, B, B, B, B, B, B, B)),
                        Cart(Y, Slots(G, G, G, G, P, P, P, P, Y, Y, Y, Y, Y, Y, Y, Y)),
                        Cart(G, Slots(P, P, P, P, B, B, B, B, G, G, G, G, G, G, G, G)),
                        Cart(P, Slots(B, B, B, B, Y, Y, Y, Y, P, P, P, P, P, P, P, P)))),

                    // L5 - 4 tir, 4 renk (G/B/Y/P) - COZULEBILIR: her renk toplam 16, tiklayarak siralanir
                    Adv(Level(PathDesign.OffsetLoop, 420, 1.52f,
                        Cart(G, Slots(B, B, B, Y, Y, Y, P, P, G, G, G, G, G, G, G, G)),
                        Cart(B, Slots(G, G, G, Y, Y, P, P, P, B, B, B, B, B, B, B, B)),
                        Cart(Y, Slots(G, G, B, B, B, P, P, P, Y, Y, Y, Y, Y, Y, Y, Y)),
                        Cart(P, Slots(G, G, G, B, B, Y, Y, Y, P, P, P, P, P, P, P, P)))),

                    // L6 - 4 tir, 4 renk (ileri, farkli yol)
                    Adv(Level(PathDesign.PinchedLoop, 400, 1.55f,
                        Cart(Y, Slots(B, B, B, B, B, G, G, G, Y, Y, Y, Y, Y, Y, Y, Y)),
                        Cart(G, Slots(P, P, P, P, P, Y, Y, Y, G, G, G, G, G, G, G, G)),
                        Cart(P, Slots(Y, Y, Y, Y, Y, B, B, B, P, P, P, P, P, P, P, P)),
                        Cart(B, Slots(G, G, G, G, G, P, P, P, B, B, B, B, B, B, B, B)))),

                    // L7 - 4 tir, 4 renk (farkli dizilim) - COZULEBILIR: her renk toplam 16, tiklayarak siralanir
                    Adv(Level(PathDesign.SoftSquare, 420, 1.50f,
                        Cart(P, Slots(G, G, G, B, B, Y, Y, Y, P, P, P, P, P, P, P, P)),
                        Cart(Y, Slots(G, G, B, B, B, P, P, P, Y, Y, Y, Y, Y, Y, Y, Y)),
                        Cart(B, Slots(G, G, G, Y, Y, P, P, P, B, B, B, B, B, B, B, B)),
                        Cart(G, Slots(B, B, B, Y, Y, Y, P, P, G, G, G, G, G, G, G, G)))),
                };

                for (int levelIndex = list.Count + 1; levelIndex <= 1000; levelIndex++)
                {
                    list.Add(GenerateProceduralLevel(levelIndex));
                }

                return list;
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
                    CartLayout = CartLayout.CenterStack,
                    RequiredCargoCount = CargoCartView.SlotCount,
                    MaxLoopCapacity = capacity,
                    CargoMoveSpeed = speed,
                    Carts = new List<RuntimeCart>(carts)
                };
            }

            private static RuntimeLevel GenerateProceduralLevel(int levelIndex)
            {
                var rng = new System.Random(7301 + levelIndex * 97);
                // Zorluk RAMP: 2 tir -> 3 (~L18) -> 4 (~L28), sonra 4 sabit (kademeli zorlasir)
                int cartCount = Mathf.Clamp(2 + levelIndex / 10, 2, 4);
                if (levelIndex > 28 && rng.NextDouble() < 0.30) cartCount = Mathf.Min(4, cartCount + 1);

                int extraCount = 0;
                if (levelIndex >= 14 && rng.NextDouble() < 0.22) extraCount = 1;
                if (levelIndex >= 45 && rng.NextDouble() < 0.10) extraCount = 2;

                var colors = new List<CargoColor> { B, G, Y, P, R };
                RotateColors(colors, levelIndex % colors.Count);
                var targets = colors.GetRange(0, cartCount);

                int[] frontCounts = new int[cartCount];
                for (int i = 0; i < cartCount; i++)
                {
                    frontCounts[i] = rng.Next(4, Mathf.Min(10, CargoCartView.SlotCount - 4) + 1);
                }

                List<RuntimeCart> carts = null;
                for (int attempt = 0; attempt < 32 && carts == null; attempt++)
                {
                    var frontPool = new List<CargoColor>();
                    for (int i = 0; i < cartCount; i++)
                    {
                        for (int k = 0; k < frontCounts[i]; k++) frontPool.Add(targets[i]);
                    }
                    Shuffle(frontPool, rng);

                    var candidate = new List<RuntimeCart>();
                    bool failed = false;
                    for (int i = 0; i < cartCount; i++)
                    {
                        CargoColor target = targets[i];
                        var slots = new CargoColor?[CargoCartView.SlotCount];
                        for (int s = frontCounts[i]; s < CargoCartView.SlotCount; s++)
                        {
                            slots[s] = target;
                        }

                        for (int s = 0; s < frontCounts[i]; s++)
                        {
                            int poolIndex = FindNonTargetPoolIndex(frontPool, target);
                            if (poolIndex < 0)
                            {
                                failed = true;
                                break;
                            }
                            slots[s] = frontPool[poolIndex];
                            frontPool.RemoveAt(poolIndex);
                        }

                        if (failed) break;
                        candidate.Add(Cart(target, slots));
                    }

                    if (!failed) carts = candidate;
                }

                if (carts == null)
                {
                    carts = new List<RuntimeCart>();
                    for (int i = 0; i < cartCount; i++)
                    {
                        CargoColor target = targets[i];
                        CargoColor front = targets[(i + 1) % targets.Count];
                        carts.Add(Cart(target, Stripe(front, target)));
                    }
                }

                PathDesign pathDesign = PickPathDesign(levelIndex, rng);
                float speed = 1.42f + Mathf.Min(0.28f, levelIndex * 0.0035f) + (float)rng.NextDouble() * 0.08f;
                int capacity = 230 + cartCount * 42 + extraCount * 35 + Mathf.Min(90, levelIndex / 8);
                RuntimeLevel level = Level(pathDesign, capacity, speed, carts.ToArray());
                // 3+ tirli levellerde bazen GRID (2 kolon - yana dagil), yoksa kolon cesitleri
                level.CartLayout = (cartCount >= 3 && rng.NextDouble() < 0.55)
                    ? CartLayout.Grid
                    : PickCartLayout(levelIndex, rng);
                level.AllowLastColorRelease = levelIndex >= 8;
                level.ExtraCartCount = extraCount;
                // Tunel: serpantinde KAPALI (yoksa yolu kesip bozar); looplarda ~%40 (her levelde degil)
                // Tunel: sadece ILERI levellerde (L16+) + DUSUK oran + serpantinde asla (ilk leveller HEP temiz yol)
                level.Tunnels = pathDesign != PathDesign.Serpentine && levelIndex >= 16 && rng.NextDouble() < 0.22;
                return level;
            }

            private static PathDesign PickPathDesign(int levelIndex, System.Random rng)
            {
                PathDesign[] designs =
                {
                    PathDesign.Serpentine,   // kivrimli (agirlikli - kullanici sevdi)
                    PathDesign.RoundedLoop,
                    PathDesign.WideLoop,
                    PathDesign.Serpentine,   // 2x -> daha sik kivrimli yol
                    PathDesign.PinchedLoop,
                    PathDesign.OffsetLoop,
                    PathDesign.SoftSquare
                };
                // levelIndex'e gore DONGUSEL -> ardisik leveller FARKLI yol; hafif rng karistirir
                return designs[(levelIndex + rng.Next(0, 2)) % designs.Length];
            }

            private static CartLayout PickCartLayout(int levelIndex, System.Random rng)
            {
                CartLayout[] layouts =
                {
                    CartLayout.CenterStack,
                    CartLayout.Staggered,
                    CartLayout.Diagonal,
                    CartLayout.LeftBias,
                    CartLayout.RightBias
                };
                return layouts[Mathf.Abs(levelIndex * 3 + rng.Next(0, layouts.Length)) % layouts.Length];
            }

            private static int FindNonTargetPoolIndex(List<CargoColor> pool, CargoColor target)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i] != target) return i;
                }
                return -1;
            }

            private static void RotateColors(List<CargoColor> colors, int amount)
            {
                for (int i = 0; i < amount; i++)
                {
                    CargoColor c = colors[0];
                    colors.RemoveAt(0);
                    colors.Add(c);
                }
            }

            private static void Shuffle<T>(List<T> list, System.Random rng)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    T tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
            }

            // Ileri level: son renk de tiklanabilir olsun
            private static RuntimeLevel Adv(RuntimeLevel lvl)
            {
                lvl.AllowLastColorRelease = true;
                return lvl;
            }

            // Bu level'da tunel/kesik OLMASIN (orn. serpantin/kivrimli yol -> temiz tek mesh)
            private static RuntimeLevel NoTun(RuntimeLevel lvl)
            {
                lvl.Tunnels = false;
                return lvl;
            }

            // Bu level'da X adet ekstra (bos) tir eklenebilsin -> park alaninda rezerve yer + +1 booster sayisi
            private static RuntimeLevel Extra(RuntimeLevel lvl, int extraCarts)
            {
                lvl.ExtraCartCount = Mathf.Clamp(extraCarts, 0, 3); // oyunda MAX 3 ekstra tir
                return lvl;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Tek bÃ¼yÃ¼k cart. 2 sÃ¼tun Ã— 4 satÄ±r = 8 slot grid.
    /// Her slot ya FULL (tek renkli bÃ¼yÃ¼k blok) ya da EMPTY (partikÃ¼l bekliyor).
    /// Tap edilen slot patlar, partikÃ¼lleri loop'a saÃ§ar.
    /// Loop'tan dÃ¶nen partikÃ¼ller uygun slotlara birikip slotlarÄ± yeniden full yapar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CargoCartView : MonoBehaviour
    {
        public const int GridCols = 1;
        public const int GridRows = 16;
        public const int SlotCount = GridCols * GridRows;

        public sealed class Slot
        {
            public int Index;
            public CargoColor? ClaimedColor;     // null = boÅŸ ve unclaimed
            public int FillCount;                // 0..threshold
            public int ReservedCount;            // yoldan bu slota gelmekte olan kutu sayısı
            public bool IsFull;                  // true â†’ bÃ¼yÃ¼k blok gÃ¶rÃ¼nÃ¼r
            public GameObject FullVisual;        // bÃ¼yÃ¼k blok gÃ¶rsel
            public Vector3 LocalPosition;
        }

        private readonly Slot[] slots = new Slot[SlotCount];
        private ColorCargoLoopGame game;
        private int cartIndex;
        private Color accentColor;
        private CargoColor targetColor;
        private Transform slotRoot;
        private int fillThreshold = 30;
        private Coroutine punchCoroutine;

        // CALISAN SISTEM: ilk levellerde son/tek renk tiklanmasin (false), ileri levellerde tiklanabilir (true)
        private bool allowLastColorRelease = false;
        private bool acceptAnyColorWhenEmpty = false;
        public void SetAllowLastColorRelease(bool value) { allowLastColorRelease = value; }
        public void SetAcceptAnyColorWhenEmpty(bool value) { acceptAnyColorWhenEmpty = value; }

        // Grid boyutu - import edilen wagon'a gÃ¶re runtime'da hesaplanÄ±r
        private float effectiveGridWidth = 0.78f;
        private float effectiveGridDepth = 1.45f;

        public int CartIndex { get { return cartIndex; } }
        public CargoColor TargetColor { get { return targetColor; } }
        public int FillThreshold { get { return fillThreshold; } }
        public Slot[] Slots { get { return slots; } }

        public int FullSlotCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < slots.Length; i++) if (slots[i] != null && slots[i].IsFull) n++;
                return n;
            }
        }

        public int FullSlotsOfColor(CargoColor color)
        {
            int n = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].IsFull && slots[i].ClaimedColor == color) n++;
            }
            return n;
        }

        /// <summary>
        /// Win: TUM slotlar dolu VE hepsi AYNI renk (hangi renk olursa olsun).
        /// CALISAN SISTEM: artik atanmis targetColor yok - oyuncu tek renge indirir.
        /// </summary>
        public bool IsCartFullSingleColor(out CargoColor color)
        {
            color = targetColor;
            CargoColor? first = null;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) return false;
                if (!slots[i].IsFull) return false;
                if (!slots[i].ClaimedColor.HasValue) return false;
                if (first == null) first = slots[i].ClaimedColor.Value;
                else if (slots[i].ClaimedColor.Value != first.Value) return false;
            }
            if (first.HasValue) color = first.Value;
            return true;
        }

        public void Initialize(
            ColorCargoLoopGame owner,
            int index,
            CargoColor target,
            CargoColor?[] initialSlotColors,
            int threshold,
            Color accent,
            GameObject optionalImportedModel)
        {
            game = owner;
            cartIndex = index;
            targetColor = target;
            accentColor = accent;
            fillThreshold = Mathf.Max(1, threshold);

            // Effective grid size - default to inspector values, BuildImportedShell override eder
            effectiveGridWidth = game.CartGridWidth;
            effectiveGridDepth = game.CartGridDepth;

            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = new Slot
                {
                    Index = i,
                    ClaimedColor = (initialSlotColors != null && i < initialSlotColors.Length) ? initialSlotColors[i] : null,
                    FillCount = (initialSlotColors != null && i < initialSlotColors.Length && initialSlotColors[i].HasValue) ? fillThreshold : 0,
                    IsFull = (initialSlotColors != null && i < initialSlotColors.Length && initialSlotColors[i].HasValue)
                };
            }

            BuildCartShell(optionalImportedModel);
            ComputeSlotPositions();
            RebuildAllSlotVisuals();
        }

        /// <summary>
        /// En Ã¶ndeki NON-TARGET dolu slot'u patlat. (Tek kÃ¼p release - geri uyumluluk iÃ§in)
        /// </summary>
        public bool TryReleaseTop(out CargoColor color, out int slotIndex)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s != null && s.IsFull && s.ClaimedColor.HasValue && s.ClaimedColor.Value != targetColor)
                {
                    color = s.ClaimedColor.Value;
                    slotIndex = i;
                    EmptySlot(i);
                    Punch(1.06f);
                    return true;
                }
            }
            color = targetColor;
            slotIndex = -1;
            Punch(0.96f);
            return false;
        }

        public struct ReleasedCube
        {
            public CargoColor Color;
            public int SlotIndex;
        }

        /// <summary>
        /// Tek tÄ±klama ile TÃœM non-target (Ã¶n stripe) kÃ¼plerini patlat.
        /// Returns: tÃ¼m patlayan kÃ¼plerin renk ve slot listesi.
        /// </summary>
        public bool HasReleasableFrontGroup()
        {
            return FindFrontReleaseGroup().Count > 0;
        }

        /// <summary>Bu tap'te ON'den salinacak grubun rengini verir (yoksa false). Uretken-hamle kontrolu icin.</summary>
        public bool TryGetReleasableFrontColor(out CargoColor color)
        {
            var g = FindFrontReleaseGroup();
            if (g.Count > 0) { color = g[0].Color; return true; }
            color = default(CargoColor);
            return false;
        }

        public List<ReleasedCube> ReleaseAllFront()
        {
            // CALISAN SISTEM: slot'lar hemen bosalmaz - SpawnFrontBurst sirayla bosaltir
            var released = FindFrontReleaseGroup();
            if (released.Count > 0) Punch(1.12f);
            else Punch(0.96f);
            return released;
        }

        private List<ReleasedCube> FindFrontReleaseGroup()
        {
            var released = new List<ReleasedCube>();
            int startIndex = -1;
            CargoColor groupColor = targetColor;

            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null || !s.IsFull || !s.ClaimedColor.HasValue) continue;

                // CALISAN SISTEM: HER renk tiklanabilir (son/arka renk dahil).
                // targetColor koruma blogu kaldirildi.
                startIndex = i;
                groupColor = s.ClaimedColor.Value;
                break;
            }

            if (startIndex < 0)
            {
                return released;
            }

            // CALISAN SISTEM: SON RENK korumasi.
            // Tirda baska renk yoksa (on grup = TEK kalan renk) ve allowLastColorRelease=false ise
            // bu son renk TIKLANAMAZ (ilk levellerde kafa karismasin, klasik sort mantigi).
            if (!allowLastColorRelease)
            {
                bool hasDifferentColor = false;
                for (int i = 0; i < SlotCount; i++)
                {
                    Slot s = slots[i];
                    if (s != null && s.IsFull && s.ClaimedColor.HasValue && s.ClaimedColor.Value != groupColor)
                    {
                        hasDifferentColor = true;
                        break;
                    }
                }
                if (!hasDifferentColor)
                {
                    return released; // sadece tek renk var -> son renk, tiklanamaz
                }
            }

            for (int i = startIndex; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null || !s.IsFull || !s.ClaimedColor.HasValue) break;
                if (s.ClaimedColor.Value != groupColor) break;
                released.Add(new ReleasedCube { Color = groupColor, SlotIndex = i });
            }

            return released;
        }

        /// <summary>
        /// TÄ±rÄ±n Ã¶nÃ¼ (non-target slotlar) komple boÅŸ mu?
        /// Sadece bu durumda gelen kÃ¼pler iner.
        /// </summary>
        public bool IsFrontFullyEmptied()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null) continue;
                // Bir slot dolu ve target deÄŸil ise: Ã¶n hala dolu sayÄ±lÄ±r
                if (s.IsFull && s.ClaimedColor.HasValue && s.ClaimedColor.Value != targetColor)
                {
                    return false;
                }
            }
            return true;
        }

        public void EmptySlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            Slot s = slots[slotIndex];
            if (s == null) return;
            s.ClaimedColor = null;
            s.FillCount = 0;
            s.ReservedCount = 0;
            s.IsFull = false;
            RebuildSlotVisual(slotIndex);
        }

        public struct PartialInfo { public CargoColor Color; public int Count; }

        /// <summary>
        /// Yarim (kismi dolu, IsFull degil) slotlardaki kupleri DISARI cikar (yola geri donsun diye).
        /// Tum rezervasyonlari da temizler. Boylece slotlar hep dolu/bos kalir, sayim korunur.
        /// </summary>
        public List<PartialInfo> DrainPartialSlots()
        {
            var list = new List<PartialInfo>();
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null) continue;
                s.ReservedCount = 0;
                if (!s.IsFull && s.FillCount > 0 && s.ClaimedColor.HasValue)
                {
                    list.Add(new PartialInfo { Color = s.ClaimedColor.Value, Count = s.FillCount });
                    s.ClaimedColor = null;
                    s.FillCount = 0;
                    RebuildSlotVisual(i);
                }
            }
            return list;
        }

        public void PushColorIntoSlot(int slotIndex, CargoColor color)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            Slot s = slots[slotIndex];
            if (s == null) return;
            s.ClaimedColor = color;
            s.FillCount = fillThreshold;
            s.ReservedCount = 0;
            s.IsFull = true;
            RebuildSlotVisual(slotIndex);
        }

        /// <summary>
        /// HEDEF-TABANLI landing (garanti ev): kup SADECE kendi HEDEF tirina iner (mor kup -> mor tir).
        /// Tirda hala NON-TARGET renk varsa inmez (once oyuncu onlari eject etmeli -> karismaz).
        /// Boylece her renk kesin yerini bulur, tir tek renge dolup gider.
        /// </summary>
        public bool TryFindOpenSlot(CargoColor color, out int slotIndex, out Vector3 worldPosition)
        {
            slotIndex = -1;
            worldPosition = transform.position;

            // 1) Kup, bu tirin HEDEF rengiyle eslesmeli
            if (color != targetColor)
            {
                if (!acceptAnyColorWhenEmpty || !IsCompletelyEmpty())
                {
                    return false;
                }

                targetColor = color;
                RetintBody(CargoColorPalette.ToColor(color)); // ekstra/buffer tir, benimsedigi RENGE boyansin
            }

            // 2) Tirda non-target dolu slot varsa target kup inmesin (mixlenmesin)
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s != null && s.IsFull && s.ClaimedColor.HasValue && s.ClaimedColor.Value != targetColor)
                    return false;
            }

            // 3) Bos slota target koy (arkadan one dogru doldur -> tek renk olur)
            for (int i = SlotCount - 1; i >= 0; i--)
            {
                if (TryReserveSlot(i, color, out slotIndex, out worldPosition)) return true;
            }
            return false;
        }

        public bool IsCompletelyEmpty()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null) continue;
                if (s.IsFull || s.FillCount > 0 || s.ReservedCount > 0 || s.ClaimedColor.HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryReserveSlot(int i, CargoColor color, out int slotIndex, out Vector3 worldPosition)
        {
            slotIndex = -1;
            worldPosition = transform.position;
            Slot s = slots[i];
            if (s == null || s.IsFull) return false;
            if (s.FillCount + s.ReservedCount >= fillThreshold) return false;
            if (s.ClaimedColor.HasValue && s.ClaimedColor.Value != color) return false;
            if (!s.ClaimedColor.HasValue) s.ClaimedColor = color;

            int fillOrder = Mathf.Clamp(s.FillCount + s.ReservedCount, 0, fillThreshold - 1);
            s.ReservedCount++;
            slotIndex = i;
            worldPosition = slotRoot.TransformPoint(GetFillLocalPosition(s, fillOrder));
            return true;
        }

        /// <summary>
        /// Bir partikÃ¼lÃ¼ slot'a ekle.
        /// deposited=true: kup gercekten slota kondu. deposited=false: slot uygun degil (dolu/yanlis renk),
        ///   cagiran kupu KAYBETMEMELI, yola geri birakmali.
        /// return=true: slot bu kupla FULL oldu.
        /// </summary>
        public bool AddParticleToSlot(int slotIndex, CargoColor color, out bool deposited)
        {
            deposited = false;
            if (slotIndex < 0 || slotIndex >= SlotCount) return false;
            Slot s = slots[slotIndex];
            if (s == null) return false;

            // Slot artik kabul edemiyor (dolu ya da baska renge claim) -> fantom rezervasyonu birak, deposit YOK
            if (s.IsFull || (s.ClaimedColor.HasValue && s.ClaimedColor.Value != color))
            {
                if (s.ReservedCount > 0) s.ReservedCount--;
                return false;
            }

            if (s.ReservedCount > 0) s.ReservedCount--;
            if (!s.ClaimedColor.HasValue) s.ClaimedColor = color;

            s.FillCount++;
            deposited = true;
            RebuildSlotVisual(slotIndex);
            if (s.FillCount >= fillThreshold)
            {
                s.FillCount = fillThreshold;
                s.IsFull = true;
                RebuildSlotVisual(slotIndex);
                Punch(1.04f);
                return true;
            }
            // YarÄ± dolu gÃ¶rsel ipucu (opsiyonel)
            return false;
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || slotRoot == null) return transform.position;
            return slotRoot.TransformPoint(slots[slotIndex].LocalPosition);
        }

        public Vector3 GetTopReleasePoint(int slotIndex)
        {
            return GetSlotWorldPosition(slotIndex) + Vector3.up * 0.25f;
        }

        public Vector3 GetRearExitPoint()
        {
            // Prototype trucks face right in screen space; cargo always exits from the rear/bed tail.
            return transform.position + Vector3.left * (effectiveGridDepth * 0.52f) + Vector3.up * 0.52f;
        }

        public Vector3 GetHeadEntryPoint()
        {
            // CALISAN SISTEM: tirin kafa tarafi (sag yon) - kupler bu noktadan da iner
            return transform.position + Vector3.right * (effectiveGridDepth * 0.52f) + Vector3.up * 0.52f;
        }

        public bool TryGetVisualBounds(out Bounds bounds)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = new Bounds(transform.position, new Vector3(1.4f, 0.5f, 0.8f));
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return true;
        }

        public void Shuffle()
        {
            // Sadece full slotlarÄ±n renklerini yer deÄŸiÅŸtir
            var fullColors = new List<CargoColor>();
            var fullIndices = new List<int>();
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].IsFull && slots[i].ClaimedColor.HasValue)
                {
                    fullColors.Add(slots[i].ClaimedColor.Value);
                    fullIndices.Add(i);
                }
            }
            for (int i = 0; i < fullColors.Count; i++)
            {
                int swap = Random.Range(i, fullColors.Count);
                CargoColor tmp = fullColors[i];
                fullColors[i] = fullColors[swap];
                fullColors[swap] = tmp;
            }
            for (int i = 0; i < fullIndices.Count; i++)
            {
                int idx = fullIndices[i];
                slots[idx].ClaimedColor = fullColors[i];
                RebuildSlotVisual(idx);
            }
            Punch(1.04f);
        }

        private void OnMouseDown()
        {
            if (game == null) return;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return;
            game.TryReleaseFromCart(this);
        }

        // ============================================================
        // Geometri / shell
        // ============================================================
        private void BuildCartShell(GameObject optionalImportedModel)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroySafe(transform.GetChild(i).gameObject);
            }

            slotRoot = new GameObject("SlotGrid").transform;
            slotRoot.SetParent(transform, false);
            slotRoot.localPosition = new Vector3(0f, 0.36f, 0f);
            slotRoot.localRotation = Quaternion.identity;

            if (optionalImportedModel != null)
            {
                BuildImportedShell(optionalImportedModel);
                return;
            }

            BuildPrimitiveShell();
        }

        // Tirin govdesini RUNTIME'da yeniden boyar (ekstra/buffer tir bir renk benimseyince).
        private void RetintBody(Color tint)
        {
            Transform iw = transform.Find("ImportedWagon");
            if (iw != null) TintImportedBody(iw.gameObject, tint);
            accentColor = tint;
        }

        // Imported tirin GOVDE materyalini hedef rengine boyar (tekerlek/cam dokunulmaz).
        // Govde = materyal adinda "body" gecen (wheel/glass haric). Toon shader _Color ile renk uygular.
        private void TintImportedBody(GameObject root, Color tint)
        {
            if (root == null) return;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                Material[] mats = rs[i].materials; // INSTANCED kopya (per-renderer) -> diger tirlari etkilemez
                bool changed = false;
                for (int j = 0; j < mats.Length; j++)
                {
                    if (mats[j] == null) continue;
                    string n = mats[j].name.ToLowerInvariant();
                    if (n.Contains("wheel") || n.Contains("glass") || n.Contains("tire") || n.Contains("window")) continue;
                    if (!n.Contains("body")) continue;
                    if (mats[j].HasProperty("baseColorFactor")) mats[j].SetColor("baseColorFactor", tint); // glTFast (asil bu)
                    if (mats[j].HasProperty("_BaseColor")) mats[j].SetColor("_BaseColor", tint);           // URP Lit
                    if (mats[j].HasProperty("_Color")) mats[j].SetColor("_Color", tint);                   // Toon
                    changed = true;
                }
                if (changed) rs[i].materials = mats;
            }
        }

        private void BuildImportedShell(GameObject sourcePrefab)
        {
            GameObject imported = Instantiate(sourcePrefab, transform);
            imported.name = "ImportedWagon";

            Quaternion naturalRotation = imported.transform.localRotation;
            Vector3 naturalScale = imported.transform.localScale;
            Vector3 userEuler = game.CartModelLocalEuler;
            Vector3 userScale = game.CartModelScale;
            if (userScale.sqrMagnitude < 0.0001f) userScale = Vector3.one;

            imported.transform.localPosition = Vector3.zero;
            imported.transform.localRotation = naturalRotation * Quaternion.Euler(userEuler);
            imported.transform.localScale = Vector3.Scale(naturalScale, userScale);

            Collider[] importedColliders = imported.GetComponentsInChildren<Collider>();
            for (int i = 0; i < importedColliders.Length; i++) DestroySafe(importedColliders[i]);

            // Normalize: target boyutuna scale + yere otur + xz merkezde
            Bounds bounds;
            if (TryComputeWorldBounds(imported, out bounds))
            {
                float longestPlanarAxis = Mathf.Max(bounds.size.x, bounds.size.z);
                float targetSize = Mathf.Max(0.1f, game.CartModelTargetSize);
                if (longestPlanarAxis > 0.001f)
                {
                    float scaleFactor = targetSize / longestPlanarAxis;
                    imported.transform.localScale *= scaleFactor;
                }
                if (TryComputeWorldBounds(imported, out bounds))
                {
                    Vector3 parentOrigin = transform.position;
                    Vector3 centerOffset = bounds.center - parentOrigin;
                    imported.transform.localPosition -= new Vector3(centerOffset.x, bounds.min.y - parentOrigin.y, centerOffset.z);
                }
            }

            imported.transform.localPosition += game.CartModelLocalOffset;
            game.ApplyCartoonStyleToRenderers(imported, "CartImported_" + cartIndex);
            TintImportedBody(imported, accentColor); // tiri HEDEF rengine boya (sadece govde; tekerlek/cam haric)

            // Wagon yÃ¼ksekliÄŸi
            float wagonHeight = 0.45f;
            if (TryComputeWorldBounds(imported, out bounds))
            {
                wagonHeight = bounds.size.y;
            }

            // --- Slot Grid yerleÅŸimi: CargoArea marker varsa onu kullan, yoksa otomatik ---
            Transform cargoArea = FindChildByNameRecursive(imported.transform, "CargoArea");
            if (cargoArea != null)
            {
                // CargoArea world position -> cart local position
                slotRoot.position = cargoArea.position;
                slotRoot.rotation = cargoArea.rotation;

                // CargoArea'nÄ±n world-space boyutu (lossy scale) bizim grid boyutu olur
                Vector3 areaWorldScale = cargoArea.lossyScale;
                effectiveGridWidth = Mathf.Abs(areaWorldScale.x);
                effectiveGridDepth = Mathf.Abs(areaWorldScale.z);
            }
            else
            {
                // Otomatik: wagon'un uzun ekseni grid'in derinlik (4 satÄ±r) ekseni olur
                slotRoot.localPosition = Vector3.zero;
                if (TryComputeWorldBounds(imported, out bounds))
                {
                    float wagonX = bounds.size.x;
                    float wagonZ = bounds.size.z;
                    // CALISAN SISTEM: kasa-fit oranlari Inspector/MCP'den ayarlanabilir (BedLengthFill/Width/Offset)
                    float lengthFillRatio = game.BedLengthFillRatio;
                    float widthFillRatio = game.BedWidthFillRatio;
                    float offsetRatio = game.BedOffsetRatio;
                    if (wagonX >= wagonZ)
                    {
                        // Wagon uzun ekseni X â†’ grid'i 90Â° dÃ¶ndÃ¼r (4 satÄ±r X yÃ¶nÃ¼nde)
                        slotRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        effectiveGridDepth = wagonX * lengthFillRatio;
                        effectiveGridWidth = wagonZ * widthFillRatio;
                        slotRoot.localPosition += new Vector3(wagonX * offsetRatio, 0f, 0f);
                    }
                    else
                    {
                        // Wagon uzun ekseni Z â†’ grid Z yÃ¶nÃ¼nde (default)
                        slotRoot.localRotation = Quaternion.identity;
                        effectiveGridDepth = wagonZ * lengthFillRatio;
                        effectiveGridWidth = wagonX * widthFillRatio;
                        slotRoot.localPosition += new Vector3(0f, 0f, wagonZ * offsetRatio);
                    }
                }
                else
                {
                    effectiveGridWidth = game.CartGridWidth;
                    effectiveGridDepth = game.CartGridDepth;
                }

                float slotY = wagonHeight * Mathf.Clamp01(game.ImportedCargoSlotHeightRatio);
                slotRoot.localPosition += new Vector3(0f, slotY, 0f) + game.CargoSlotLocalOffset;
            }

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null && TryComputeWorldBounds(imported, out bounds))
            {
                boxCollider.center = new Vector3(0f, wagonHeight * 0.5f, 0f);
                boxCollider.size = new Vector3(Mathf.Max(0.6f, bounds.size.x), Mathf.Max(0.4f, bounds.size.y), Mathf.Max(0.6f, bounds.size.z));
            }

            if (game.AddAccentFlagOnImportedModel) AddAccentFlag(wagonHeight);
        }

        private Transform FindChildByNameRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
                Transform deep = FindChildByNameRecursive(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private void BuildPrimitiveShell()
        {
            Material bodyMat = game.GetRuntimeMaterial("CartBody_" + ColorToHex(accentColor), accentColor);
            Material bodyDarkMat = game.GetRuntimeMaterial("CartBodyDark_" + ColorToHex(accentColor), Color.Lerp(accentColor, Color.black, 0.35f));
            Material wheelMat = game.GetRuntimeMaterial("CartWheel", new Color(0.10f, 0.09f, 0.14f));
            Material trayMat = game.GetRuntimeMaterial("CartTray", new Color(0.22f, 0.16f, 0.42f));

            float cw = game.CartGridWidth;
            float cd = game.CartGridDepth;

            CreatePart("BodyBase", PrimitiveType.Cube, new Vector3(0f, 0.10f, 0f), new Vector3(cw + 0.30f, 0.36f, cd + 0.30f), bodyDarkMat);
            CreatePart("BodyTop", PrimitiveType.Cube, new Vector3(0f, 0.30f, 0f), new Vector3(cw + 0.22f, 0.18f, cd + 0.22f), bodyMat);
            CreatePart("Tray", PrimitiveType.Cube, new Vector3(0f, 0.40f, 0f), new Vector3(cw + 0.08f, 0.04f, cd + 0.08f), trayMat);

            float wheelOffsetX = (cw + 0.30f) * 0.45f;
            float wheelOffsetZ = (cd + 0.30f) * 0.45f;
            CreateWheel("WheelFL", new Vector3(wheelOffsetX, -0.12f, wheelOffsetZ), wheelMat);
            CreateWheel("WheelFR", new Vector3(wheelOffsetX, -0.12f, -wheelOffsetZ), wheelMat);
            CreateWheel("WheelBL", new Vector3(-wheelOffsetX, -0.12f, wheelOffsetZ), wheelMat);
            CreateWheel("WheelBR", new Vector3(-wheelOffsetX, -0.12f, -wheelOffsetZ), wheelMat);

            AddAccentFlag(0.6f);
        }

        private void AddAccentFlag(float wagonHeight)
        {
            // CALISAN SISTEM: targetColor kalktigi icin tir bayraklari (direk + bayrak) KALDIRILDI
        }

        private void CreateWheel(string wheelName, Vector3 localPosition, Material material)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = wheelName;
            wheel.transform.SetParent(transform, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.24f, 0.06f, 0.24f);
            DestroySafe(wheel.GetComponent<Collider>());
            wheel.GetComponent<Renderer>().sharedMaterial = material;
        }

        private GameObject CreatePart(string partName, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            DestroySafe(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private bool TryComputeWorldBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { bounds = new Bounds(root.transform.position, Vector3.one * 0.5f); return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        // ============================================================
        // Slot grid layout
        // ============================================================
        private void ComputeSlotPositions()
        {
            // 2 sÃ¼tun Ã— 4 satÄ±r - slotRoot'un kendi local space'inde X=col, Z=row
            // slotRoot zaten rotate edilmiÅŸ olabilir (auto-detected wagon orientation)
            float gridW = effectiveGridWidth;
            float gridD = effectiveGridDepth;
            float colStep = gridW / GridCols;
            float rowStep = gridD / GridRows;

            for (int r = 0; r < GridRows; r++)
            {
                for (int c = 0; c < GridCols; c++)
                {
                    int idx = r * GridCols + c;
                    float x = (c - (GridCols - 1) * 0.5f) * colStep;
                    float z = (r - (GridRows - 1) * 0.5f) * rowStep;
                    slots[idx].LocalPosition = new Vector3(x, 0f, z);
                }
            }
        }

        private readonly List<GameObject> groupVisuals = new List<GameObject>();
        private readonly List<CargoColor> cargoColorBuffer = new List<CargoColor>();

        // Kasa dolgu yogunlugu: dolu birim basina kac kup render edilsin (gorsel doluluk).
        private const int CargoCubesPerUnit = 3;

        /// <summary>
        /// KARGO PILE render: kasa, yoldaki ucan kuplerle AYNI boyutta kuplerle DOLU gorunur.
        /// Dolu birim basina birkac kup, kasanin 2D alanina + hafif yigin halinde DAGINIK yerlesir.
        /// Pozisyonlar kasa sinirlari icine CLAMP'li -> yatay tasma yok; yukseklik sinirli -> dikey tasma kontrollu.
        /// Renkler karisik ama oran korunur (oyuncu hangi renkten ne kadar var gorur).
        /// </summary>
        private void RebuildAllSlotVisuals()
        {
            for (int i = 0; i < groupVisuals.Count; i++) DestroySafe(groupVisuals[i]);
            groupVisuals.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].FullVisual != null) { DestroySafe(slots[i].FullVisual); slots[i].FullVisual = null; }
            }

            // Dolu slot renklerini topla (oran korunur -> oyuncu sayilari gorur)
            cargoColorBuffer.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s != null && s.IsFull && s.ClaimedColor.HasValue) cargoColorBuffer.Add(s.ClaimedColor.Value);
            }
            int filled = cargoColorBuffer.Count;
            if (filled == 0) return;

            // Kasadaki kup = YOLDAKI ucan kup ile BIREBIR ayni dunya boyutu (slotRoot olcegi telafi edilir)
            Vector3 rc = game.RoadCargoScale;
            float roadWorld = (rc.x + rc.z) * 0.5f;
            Vector3 ls = slotRoot != null ? slotRoot.lossyScale : Vector3.one;
            float parentScale = (Mathf.Abs(ls.x) + Mathf.Abs(ls.z)) * 0.5f;
            float cubeSize = roadWorld / Mathf.Max(0.0001f, parentScale);

            // Kasa bolgesi (slot local pozisyonlarindan -> kesin ortali). TASMA YOK: kup yarisi kadar iceri.
            Vector3 centerLocal = (slots[0].LocalPosition + slots[SlotCount - 1].LocalPosition) * 0.5f;
            float rowStep = effectiveGridDepth / GridRows;
            float zHalf = Mathf.Abs(slots[SlotCount - 1].LocalPosition.z - slots[0].LocalPosition.z) * 0.5f + rowStep * 0.5f;
            float inset = cubeSize * 0.6f; // rotasyon payi dahil guvenli kenar
            float xHalfSafe = Mathf.Max(0f, effectiveGridWidth * 0.5f - inset);
            float zHalfSafe = Mathf.Max(0f, zHalf - inset);
            float baseY = game.SlotBlockSize.y * 0.4f;
            float stackH = cubeSize * 1.0f; // yigin yuksekligi (duvar ustune cok tasmaz)

            int n = filled * CargoCubesPerUnit;
            for (int k = 0; k < n; k++)
            {
                float x = (Hash01(k, 1) * 2f - 1f) * xHalfSafe;
                float z = (Hash01(k, 3) * 2f - 1f) * zHalfSafe;
                float y = baseY + Hash01(k, 2) * stackH;
                Vector3 pos = new Vector3(centerLocal.x + x, y, centerLocal.z + z);
                Quaternion rot = Quaternion.Euler(
                    (Hash01(k, 4) - 0.5f) * 34f,
                    Hash01(k, 5) * 360f,
                    (Hash01(k, 6) - 0.5f) * 34f);

                CargoColor col = cargoColorBuffer[k % filled];
                GameObject cube = game.CreateCargoBlockObject(col, "Cargo_" + k);
                cube.transform.SetParent(slotRoot, false);
                cube.transform.localPosition = pos;
                cube.transform.localRotation = rot;
                cube.transform.localScale = Vector3.one * cubeSize;
                MeshRenderer mr = cube.GetComponent<MeshRenderer>();
                if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                groupVisuals.Add(cube);
            }
        }

        private static float Hash01(int a, int b)
        {
            uint h = (uint)((a * 73856093) ^ (b * 19349663));
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h >> 8) & 0xFFFF) / 65535f;
        }

        /// <summary>Bu tirin DOLU slotlarindaki distinct renkleri sete ekler (yapisal cozulebilirlik icin).</summary>
        public void CollectPresentColors(HashSet<CargoColor> set)
        {
            if (set == null) return;
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s != null && s.IsFull && s.ClaimedColor.HasValue) set.Add(s.ClaimedColor.Value);
            }
        }

        /// <summary>En az 1 dolu slot var ve hepsi AYNI renk mi? (TAM DOLU sart DEGIL)</summary>
        public bool IsSingleColor(out CargoColor color)
        {
            color = default(CargoColor);
            bool found = false;
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null || !s.IsFull || !s.ClaimedColor.HasValue) continue;
                if (!found) { color = s.ClaimedColor.Value; found = true; }
                else if (s.ClaimedColor.Value != color) return false;
            }
            return found;
        }

        private void RebuildSlotVisual(int slotIndex)
        {
            // Grup bazli render -> tek slot degisse de tum gruplari yeniden kur (komsuluk degisir)
            RebuildAllSlotVisuals();
            return;
#pragma warning disable 0162
            Slot s = slots[slotIndex];
            if (s == null) return;

            if (s.FullVisual != null) DestroySafe(s.FullVisual);
            s.FullVisual = null;

            // Slot tabanÄ± (her zaman gÃ¶rÃ¼nÃ¼r koyu kare)
            // Bunu sadece bir kez yapmak iÃ§in, slot base'leri ayrÄ± tutmak gerek
            // Åimdilik full visual ile birlikte yapÄ±yoruz

            // Cell-fit boyut: slotlar arasÄ± BOÅLUK YOK, blocklar full
            float colStep = effectiveGridWidth / GridCols;
            float rowStep = effectiveGridDepth / GridRows;
            // 0.99 ile Ã§ok kÃ¼Ã§Ã¼k gap (z-fighting Ã¶nlemek iÃ§in ama gÃ¶rsel olarak bitiÅŸik)
            Vector3 fitScale = new Vector3(colStep * 0.99f, game.SlotBlockSize.y, rowStep * 0.99f);

            if (s.IsFull && s.ClaimedColor.HasValue)
            {
                // CARTOON: YUVARLAK (oval kenarli) kup - sivri degil, yumusak gorunum
                GameObject cube = new GameObject("Slot_" + slotIndex + "_Full_" + s.ClaimedColor.Value);
                cube.transform.SetParent(slotRoot, false);
                cube.transform.localPosition = s.LocalPosition + new Vector3(0f, game.SlotBlockSize.y * 0.5f, 0f);
                // SOLID BLOK - ayni renk komsular bitisik (Z'de hafif overlap 1.04)
                cube.transform.localScale = new Vector3(colStep * 0.92f, fitScale.y, rowStep * 1.04f);
                MeshFilter mf = cube.AddComponent<MeshFilter>();
                mf.sharedMesh = MeshUtils.RoundedCube();
                MeshRenderer mr = cube.AddComponent<MeshRenderer>();
                mr.sharedMaterial = game.GetCargoMaterial(s.ClaimedColor.Value);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                s.FullVisual = cube;
            }
            else
            {
                // SLOT BOS: GORUNMEZ (siyah/mor paca kaldirildi)
                s.FullVisual = null;
            }
        }

        private Vector3 GetFillLocalPosition(Slot slot, int order)
        {
            float colStep = effectiveGridWidth / GridCols;
            float rowStep = effectiveGridDepth / GridRows;
            Vector3 fitScale = new Vector3(colStep * 0.99f, game.SlotBlockSize.y, rowStep * 0.99f);

            int cols = 2;
            int rows = Mathf.CeilToInt(fillThreshold / (float)cols);
            float cellX = fitScale.x / cols;
            float cellZ = fitScale.z / Mathf.Max(1, rows);
            int clampedOrder = Mathf.Clamp(order, 0, fillThreshold - 1);
            int c = clampedOrder % cols;
            int r = clampedOrder / cols;
            float x = (c - 0.5f) * cellX;
            float z = (-fitScale.z * 0.5f) + cellZ * (r + 0.5f);
            return slot.LocalPosition + new Vector3(x, 0f, z);
        }

        private void Punch(float scale)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(PunchRoutine(scale));
        }

        private IEnumerator PunchRoutine(float targetScale)
        {
            Vector3 baseScale = Vector3.one;
            Vector3 peak = Vector3.one * targetScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 9f;
                transform.localScale = Vector3.Lerp(baseScale, peak, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
                yield return null;
            }
            transform.localScale = baseScale;
            punchCoroutine = null;
        }

        private static string ColorToHex(Color c)
        {
            return ((int)(c.r * 255)).ToString("X2") + ((int)(c.g * 255)).ToString("X2") + ((int)(c.b * 255)).ToString("X2");
        }

        private void DestroySafe(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        public override string ToString()
        {
            return "Cart " + cartIndex + "(target:" + targetColor + ")";
        }
    }
}

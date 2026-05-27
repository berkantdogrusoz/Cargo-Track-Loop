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

        private Renderer[] cachedRenderers; // Tır fade-out için cache'lenmiş renderer'lar

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
        /// Win: 8 slot da full VE hepsi target rengiyle dolu.
        /// </summary>
        public bool IsCartFullSingleColor(out CargoColor color)
        {
            color = targetColor;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) return false;
                if (!slots[i].IsFull) return false;
                if (slots[i].ClaimedColor != targetColor) return false;
            }
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

        public List<ReleasedCube> ReleaseAllFront()
        {
            var released = FindFrontReleaseGroup();
            for (int i = 0; i < released.Count; i++)
            {
                EmptySlot(released[i].SlotIndex);
            }
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

                // If the first visible cargo is already target color, keep it in the truck.
                if (s.ClaimedColor.Value == targetColor)
                {
                    return released;
                }

                startIndex = i;
                groupColor = s.ClaimedColor.Value;
                break;
            }

            if (startIndex < 0)
            {
                return released;
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
        /// Bu renk iÃ§in uygun boÅŸ slot var mÄ±?
        /// KURALLAR:
        /// 1) Renk bu tÄ±rÄ±n HEDEF rengiyle eÅŸleÅŸmeli
        /// 2) TÄ±rÄ±n Ã–NÃœ tamamen boÅŸalmÄ±ÅŸ olmalÄ± (hiÃ§ non-target dolu slot yok)
        /// </summary>
        public bool TryFindOpenSlot(CargoColor color, out int slotIndex, out Vector3 worldPosition)
        {
            slotIndex = -1;
            worldPosition = transform.position;

            // Kural 1: sadece kendi target rengini kabul et
            if (color != targetColor) return false;

            // Kural 2: Ã¶n (non-target slotlar) tamamen boÅŸ olmalÄ±
            if (!IsFrontFullyEmptied()) return false;

            // Ä°lk boÅŸ slot'u bul - en Ã¶ne yakÄ±n olanÄ± tercih et
            for (int i = 0; i < SlotCount; i++)
            {
                Slot s = slots[i];
                if (s == null || s.IsFull) continue;
                if (s.FillCount + s.ReservedCount >= fillThreshold) continue;
                if (s.ClaimedColor.HasValue && s.ClaimedColor.Value != color) continue;
                if (!s.ClaimedColor.HasValue) s.ClaimedColor = color;

                int fillOrder = Mathf.Clamp(s.FillCount + s.ReservedCount, 0, fillThreshold - 1);
                s.ReservedCount++;
                slotIndex = i;
                worldPosition = slotRoot.TransformPoint(GetFillLocalPosition(s, fillOrder));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Bir partikÃ¼lÃ¼ slot'a ekle. True dÃ¶nerse slot full olmuÅŸ demektir.
        /// </summary>
        public bool AddParticleToSlot(int slotIndex, CargoColor color)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return false;
            Slot s = slots[slotIndex];
            if (s == null || s.IsFull) return false;
            if (s.ReservedCount > 0) s.ReservedCount--;

            if (!s.ClaimedColor.HasValue)
            {
                s.ClaimedColor = color;
            }
            else if (s.ClaimedColor.Value != color)
            {
                return false; // baÅŸka renge claim edilmiÅŸ
            }

            s.FillCount++;
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
                    // %85 wagon'a sÄ±ÄŸsÄ±n
                    float lengthFillRatio = 0.62f;
                    float widthFillRatio = 0.72f;
                    if (wagonX >= wagonZ)
                    {
                        // Wagon uzun ekseni X â†’ grid'i 90Â° dÃ¶ndÃ¼r (4 satÄ±r X yÃ¶nÃ¼nde)
                        slotRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        effectiveGridDepth = wagonX * lengthFillRatio;
                        effectiveGridWidth = wagonZ * widthFillRatio;
                        slotRoot.localPosition += new Vector3(-wagonX * 0.16f, 0f, 0f);
                    }
                    else
                    {
                        // Wagon uzun ekseni Z â†’ grid Z yÃ¶nÃ¼nde (default)
                        slotRoot.localRotation = Quaternion.identity;
                        effectiveGridDepth = wagonZ * lengthFillRatio;
                        effectiveGridWidth = wagonX * widthFillRatio;
                        slotRoot.localPosition += new Vector3(0f, 0f, -wagonZ * 0.16f);
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
            Color flagColor = CargoColorPalette.ToColor(targetColor);
            Material flagMat = game.GetRuntimeMaterial("CartTargetFlag_" + targetColor, flagColor);
            Material poleMat = game.GetRuntimeMaterial("CartAccentPole", new Color(0.10f, 0.08f, 0.16f));

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "AccentPole";
            pole.transform.SetParent(transform, false);
            float poleHeight = 0.46f;
            pole.transform.localScale = new Vector3(0.05f, poleHeight * 0.5f, 0.05f);
            pole.transform.localPosition = new Vector3(-game.CartGridWidth * 0.5f - 0.18f, wagonHeight + poleHeight * 0.5f, 0f);
            DestroySafe(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "AccentFlag";
            flag.transform.SetParent(transform, false);
            flag.transform.localScale = new Vector3(0.05f, 0.30f, 0.46f);
            flag.transform.localPosition = new Vector3(-game.CartGridWidth * 0.5f - 0.18f, wagonHeight + poleHeight + 0.10f, 0.28f);
            DestroySafe(flag.GetComponent<Collider>());
            flag.GetComponent<Renderer>().sharedMaterial = flagMat;
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

        /// <summary>
        /// Tırın tüm renderer'larını cache'le. Tunnel fade-out efekti için kullanılır.
        /// </summary>
        public void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>
        /// Tır tünele girerken alpha fade-out efekti uygula.
        /// tunnelAlpha: 1 = tam görünür, 0 = tamamen görünmez
        /// </summary>
        public void SetTunnelFade(float tunnelAlpha)
        {
            if (cachedRenderers == null) CacheRenderers();
            
            float actualAlpha = Mathf.Clamp01(tunnelAlpha);
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer r = cachedRenderers[i];
                if (r == null) continue;
                
                // Slot grid'deki cargo bloklarını fade-out'a dahil etme
                if (r.transform.IsChildOf(slotRoot)) continue;
                
                Material mat = r.sharedMaterial;
                if (mat != null)
                {
                    Color color = mat.color;
                    color.a = actualAlpha;
                    mat.color = color;
                }
            }
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

        private void RebuildAllSlotVisuals()
        {
            for (int i = 0; i < SlotCount; i++) RebuildSlotVisual(i);
        }

        private void RebuildSlotVisual(int slotIndex)
        {
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

            if (s.ClaimedColor.HasValue && (s.FillCount > 0 || s.IsFull))
            {
                GameObject group = new GameObject("Slot_" + slotIndex + "_Cargo_" + s.ClaimedColor.Value);
                group.transform.SetParent(slotRoot, false);
                group.transform.localPosition = s.LocalPosition;

                int count = s.IsFull ? fillThreshold : Mathf.Clamp(s.FillCount, 1, fillThreshold);
                
                // FULL slot: single large block that fills the entire slot height
                if (s.IsFull)
                {
                    // Dinamik yukseklik hesaplama - kasa tam dolsun ama tasmasin
                    // Slot block height * stack count = toplam dolu yukseklik
                    float fullBlockHeight = game.SlotBlockSize.y * fillThreshold;
                    
                    // Grid derinligine gore maksimum yukseklik siniri (kasa disina tasmasin)
                    float maxAllowedHeight = effectiveGridDepth * 0.85f;
                    fullBlockHeight = Mathf.Min(fullBlockHeight, maxAllowedHeight);
                    
                    // Minimum yükseklik - gorunur olsun
                    fullBlockHeight = Mathf.Max(fullBlockHeight, game.SlotBlockSize.y * 1.5f);
                    
                    Vector3 fullBlockSize = new Vector3(colStep * 0.98f, fullBlockHeight, rowStep * 0.98f);
                    
                    GameObject fullBlock = game.CreateCargoBlockObject(
                        s.ClaimedColor.Value,
                        "FullBlock");
                    fullBlock.transform.SetParent(group.transform, false);
                    fullBlock.transform.localPosition = Vector3.up * (fullBlockSize.y * 0.5f);
                    fullBlock.transform.localScale = fullBlockSize;
                    s.FullVisual = group;
                }
                else
                {
                    // Partially filled: show mini particles
                    int cols = 2;
                    int rows = Mathf.CeilToInt(fillThreshold / (float)cols);
                    float cellX = fitScale.x / cols;
                    float cellZ = fitScale.z / Mathf.Max(1, rows);
                    Vector3 miniScale = new Vector3(cellX * 0.96f, fitScale.y * 1.45f, cellZ * 0.96f);

                    for (int i = 0; i < count; i++)
                    {
                        GameObject mini = game.CreateCargoBlockObject(
                            s.ClaimedColor.Value,
                            "Fill_" + i);
                        mini.transform.SetParent(group.transform, false);
                        mini.transform.localPosition = GetFillLocalPosition(s, i) - s.LocalPosition + Vector3.up * (miniScale.y * 0.5f);
                        mini.transform.localScale = miniScale;
                    }

                    s.FullVisual = group;
                }
            }
            else
            {
                // BoÅŸ slot - koyu mini taban (boÅŸluk hissi)
                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.name = "Slot_" + slotIndex + "_Empty";
                pad.transform.SetParent(slotRoot, false);
                pad.transform.localPosition = s.LocalPosition + new Vector3(0f, 0.02f, 0f);
                pad.transform.localScale = new Vector3(fitScale.x * 0.96f, 0.04f, fitScale.z * 0.96f);
                DestroySafe(pad.GetComponent<Collider>());
                pad.GetComponent<Renderer>().sharedMaterial = game.GetRuntimeMaterial("EmptySlot", new Color(0.20f, 0.14f, 0.42f));
                s.FullVisual = pad;
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

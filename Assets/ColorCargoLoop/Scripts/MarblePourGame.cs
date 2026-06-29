using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorCargoLoop
{
    /// <summary>
    /// PROTOTIP v3 — SEPET + SLOT + KAPAK akisi, GERCEK PortraitSet ile. Board YOK.
    /// Sepetler KAYBOLMAZ; her sepette renk + KUP SAYISI var. Sepet sec -> SLOTa gider ->
    /// KAPAK acilir -> kupler potrenin ilgili (ayni renk) hucrelerine UCAR/dolar.
    /// Potre verisi: ArrowsPixelPortraitSet (gercek asset). Atanmazsa generated cember kullanir.
    /// Self-contained UI (koddan). Canli oyuna sifir dokunus.
    /// </summary>
    public class MarblePourGame : MonoBehaviour
    {
        [Header("GERCEK potre kaynagi")]
        [Tooltip("Asil PortraitSet asset'ini ata (Assets/Art/Portraits/PortraitSet). Bos -> generated cember.")]
        [SerializeField] private ArrowsPixelPortraitSet portraitSet;
        [Tooltip("Hangi potre (index).")]
        [SerializeField] private int levelIndex = 0;
        [Tooltip("Potre ters gorunuyorsa isaretle.")]
        [SerializeField] private bool flipVertical = false;

        [Header("Gorunum")]
        [SerializeField] private int basketChunk = 999;    // 1 sepet = 1 renk (buyukse boler)
        [SerializeField] private Color screenBg = new Color(0.937f, 0.913f, 0.984f);

        int pcols, prows;
        int[] target;     // hucre -> renk index (0..11) veya -1 (bos)
        int[] filled;
        int totalNeeded;
        Color[] colors = new Color[12];

        Image[] cellImg;
        RectTransform canvasRT;
        RectTransform slotFrame;
        Text statusText;
        GameObject winPanel;
        bool won, busy;

        class Basket
        {
            public int color, count;
            public bool emptied;
            public RectTransform root;
            public RectTransform lid;
            public Text countText;
        }
        readonly List<Basket> baskets = new List<Basket>();
        float slotX = 0f, slotY = 740f;

        void Start()
        {
            EnsureEventSystem();
            BuildPortraitData();
            BuildUI();
            BuildBaskets();
            Render();
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        static int CharIndex(char ch)
        {
            switch (ch)
            {
                case '.': return -1;
                case 'B': return 1; case 'Y': return 2; case 'G': return 3; case 'U': return 4; case 'O': return 5;
                case 'K': return 6; case 'C': return 7; case 'T': return 8; case 'L': return 9; case 'W': return 10; case 'N': return 11;
                default: return 0; // P + bilinmeyen -> 0
            }
        }

        void BuildPortraitData()
        {
            // palet: portrenin adaptive paleti varsa onu, yoksa candy
            ArrowsPixelPortraitSet.Entry entry = null;
            if (portraitSet != null && portraitSet.HasPortraits)
            {
                int li = Mathf.Clamp(levelIndex, 0, portraitSet.portraits.Count - 1);
                entry = portraitSet.portraits[li];
            }
            for (int i = 0; i < 12; i++)
            {
                if (entry != null && entry.palette != null && i < entry.palette.Length && entry.palette[i].a > 0.001f)
                    colors[i] = entry.palette[i];
                else
                    colors[i] = CargoColorPalette.ToColor((CargoColor)i);
            }

            if (entry != null && entry.rows != null && entry.rows.Length > 0)
            {
                var rows = entry.rows;
                prows = rows.Length; pcols = 1;
                for (int y = 0; y < rows.Length; y++) if (rows[y] != null) pcols = Mathf.Max(pcols, rows[y].Length);
                target = new int[pcols * prows];
                filled = new int[pcols * prows];
                for (int y = 0; y < prows; y++)
                {
                    string row = rows[flipVertical ? (prows - 1 - y) : y] ?? "";
                    for (int x = 0; x < pcols; x++)
                    {
                        char ch = x < row.Length ? row[x] : '.';
                        int idx = y * pcols + x;
                        target[idx] = CharIndex(ch);
                        filled[idx] = -1;
                    }
                }
            }
            else
            {
                BuildGeneratedFallback();
            }
            totalNeeded = 0;
            for (int i = 0; i < target.Length; i++) if (target[i] >= 0) totalNeeded++;
        }

        void BuildGeneratedFallback()
        {
            pcols = prows = 9;
            target = new int[pcols * prows]; filled = new int[pcols * prows];
            float c = (pcols - 1) / 2f, rMax = c + 0.2f;
            for (int y = 0; y < prows; y++)
                for (int x = 0; x < pcols; x++)
                {
                    int idx = y * pcols + x; filled[idx] = -1;
                    float dx = x - c, dy = y - c, r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > rMax) { target[idx] = -1; continue; }
                    float ang = Mathf.Atan2(dy, dx);
                    int q = Mathf.FloorToInt(((ang + Mathf.PI) / (2f * Mathf.PI)) * 4f);
                    target[idx] = Mathf.Clamp(q, 0, 3);
                }
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("[MarblePourCanvas]");
            var canvas = canvasGO.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasRT = canvasGO.GetComponent<RectTransform>();

            var bg = NewImage("BG", canvasGO.transform, screenBg, false); Stretch(bg.rectTransform);

            statusText = NewText("Status", canvasGO.transform, "", 40, new Color(0.29f, 0.23f, 0.45f));
            var sRT = statusText.rectTransform; sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 1f); sRT.pivot = new Vector2(0.5f, 1f);
            sRT.anchoredPosition = new Vector2(0, -60); sRT.sizeDelta = new Vector2(900, 70); statusText.alignment = TextAnchor.MiddleCenter;

            var portrait = new GameObject("Portrait", typeof(RectTransform));
            portrait.transform.SetParent(canvasGO.transform, false);
            var prt = portrait.GetComponent<RectTransform>(); prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f); prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0, -150);
            float gridPx = 820f; prt.sizeDelta = new Vector2(gridPx, gridPx);
            portrait.AddComponent<Image>().color = Color.white;
            var grid = portrait.AddComponent<GridLayoutGroup>();
            int maxDim = Mathf.Max(pcols, prows);
            float cs = (gridPx - 16f) / maxDim;
            grid.cellSize = new Vector2(cs, cs); grid.spacing = new Vector2(1, 1);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = pcols;
            grid.childAlignment = TextAnchor.MiddleCenter;
            cellImg = new Image[pcols * prows];
            for (int i = 0; i < cellImg.Length; i++) cellImg[i] = NewImage("c" + i, portrait.transform, new Color(0.92f, 0.90f, 0.95f), false);

            slotFrame = NewImage("Slot", canvasGO.transform, new Color(1f, 1f, 1f, 0.6f), false).rectTransform;
            slotFrame.anchorMin = slotFrame.anchorMax = new Vector2(0.5f, 0f); slotFrame.pivot = new Vector2(0.5f, 0.5f);
            slotFrame.anchoredPosition = new Vector2(slotX, slotY); slotFrame.sizeDelta = new Vector2(190, 230);
            var slotOutline = slotFrame.gameObject.AddComponent<Outline>(); slotOutline.effectColor = new Color(0.79f, 0.74f, 0.90f); slotOutline.effectDistance = new Vector2(3, 3);
            var slotLbl = NewText("SlotLbl", slotFrame, "SLOT", 24, new Color(0.66f, 0.60f, 0.78f)); slotLbl.alignment = TextAnchor.MiddleCenter; Stretch(slotLbl.rectTransform);

            var hint = NewText("Hint", canvasGO.transform, "sepet sec -> slota gider, kapagi acilir, kupler potreye dolar", 28, new Color(0.52f, 0.46f, 0.67f));
            var hRT = hint.rectTransform; hRT.anchorMin = hRT.anchorMax = new Vector2(0.5f, 0f); hRT.pivot = new Vector2(0.5f, 0f);
            hRT.anchoredPosition = new Vector2(0, 470); hRT.sizeDelta = new Vector2(1000, 40); hint.alignment = TextAnchor.MiddleCenter;

            var resetGO = NewButton("Reset", canvasGO.transform, "Yeniden", Restart);
            var rRT = resetGO.GetComponent<RectTransform>(); rRT.anchorMin = rRT.anchorMax = new Vector2(0.5f, 0f); rRT.pivot = new Vector2(0.5f, 0f);
            rRT.anchoredPosition = new Vector2(0, 40); rRT.sizeDelta = new Vector2(280, 80);

            winPanel = NewImage("Win", canvasGO.transform, new Color(0, 0, 0, 0.55f), false).gameObject;
            Stretch(winPanel.GetComponent<RectTransform>());
            var wTxt = NewText("WinTxt", winPanel.transform, "POTRE TAMAM!", 64, Color.white); wTxt.alignment = TextAnchor.MiddleCenter; Stretch(wTxt.rectTransform);
            winPanel.SetActive(false);
        }

        void BuildBaskets()
        {
            foreach (var b in baskets) if (b.root != null) Destroy(b.root.gameObject);
            baskets.Clear();

            int[] need = new int[12];
            for (int i = 0; i < target.Length; i++) if (target[i] >= 0) need[target[i]]++;
            var defs = new List<int[]>();
            for (int col = 0; col < 12; col++)
            {
                int left = need[col];
                while (left > 0) { int cnt = Mathf.Min(basketChunk, left); defs.Add(new[] { col, cnt }); left -= cnt; }
            }
            for (int i = defs.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var t = defs[i]; defs[i] = defs[j]; defs[j] = t; }

            int M = defs.Count;
            int perRow = Mathf.Clamp(M, 1, 6);
            int rowCount = Mathf.CeilToInt(M / (float)perRow);
            float stepX = Mathf.Min(185f, 1040f / perRow);
            float stepY = 215f;
            float baseY = 220f;
            for (int i = 0; i < M; i++)
            {
                int r = i / perRow, c = i % perRow;
                int inRow = (r < rowCount - 1) ? perRow : (M - perRow * (rowCount - 1));
                float startX = -(inRow - 1) * stepX / 2f;
                var b = new Basket { color = defs[i][0], count = defs[i][1] };
                MakeBasketWidget(b, startX + c * stepX, baseY + (rowCount - 1 - r) * stepY, stepX);
                baskets.Add(b);
            }
        }

        void MakeBasketWidget(Basket b, float homeX, float homeY, float stepX)
        {
            float w = Mathf.Min(160f, stepX - 22f);
            var go = new GameObject("Basket", typeof(RectTransform));
            go.transform.SetParent(canvasRT, false);
            b.root = go.GetComponent<RectTransform>();
            b.root.anchorMin = b.root.anchorMax = new Vector2(0.5f, 0f); b.root.pivot = new Vector2(0.5f, 0.5f);
            b.root.anchoredPosition = new Vector2(homeX, homeY); b.root.sizeDelta = new Vector2(w, 190);

            go.AddComponent<Image>().color = new Color(0.96f, 0.94f, 1f);
            var btn = go.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            var capt = b; btn.onClick.AddListener(() => OnBasketTapped(capt));

            var fill = NewImage("fill", go.transform, colors[b.color], false);
            var fRT = fill.rectTransform; fRT.anchorMin = new Vector2(0.12f, 0.12f); fRT.anchorMax = new Vector2(0.88f, 0.74f); fRT.offsetMin = fRT.offsetMax = Vector2.zero;

            var lid = NewImage("lid", go.transform, new Color(0.80f, 0.72f, 0.95f), false);
            b.lid = lid.rectTransform; b.lid.anchorMin = new Vector2(0.06f, 0.74f); b.lid.anchorMax = new Vector2(0.94f, 0.92f); b.lid.offsetMin = b.lid.offsetMax = Vector2.zero; b.lid.pivot = new Vector2(0f, 0.5f);

            b.countText = NewText("cnt", go.transform, "x" + b.count, 36, new Color(0.25f, 0.2f, 0.4f));
            var cRT = b.countText.rectTransform; cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0f); cRT.pivot = new Vector2(0.5f, 1f);
            cRT.anchoredPosition = new Vector2(0, -4); cRT.sizeDelta = new Vector2(160, 46); b.countText.alignment = TextAnchor.MiddleCenter;
        }

        void OnBasketTapped(Basket b)
        {
            if (won || busy || b.emptied || b.count <= 0) return;
            StartCoroutine(OpenAndPour(b));
        }

        IEnumerator OpenAndPour(Basket b)
        {
            busy = true;
            Vector2 home = b.root.anchoredPosition;
            Vector2 slotPos = new Vector2(slotX, slotY);
            b.root.SetAsLastSibling();
            yield return MoveRT(b.root, home, slotPos, 0.26f);
            yield return RotateLid(b.lid, 0f, -110f, 0.16f);
            Vector3 src = b.root.position + new Vector3(0, 30, 0);
            while (b.count > 0)
            {
                int cell = NextEmptyCellOfColor(b.color);
                if (cell < 0) break;
                StartCoroutine(FlyCube(src, cell, b.color));
                b.count--; b.countText.text = "x" + b.count;
                yield return new WaitForSeconds(0.04f);
            }
            yield return new WaitForSeconds(0.25f);
            yield return RotateLid(b.lid, -110f, 0f, 0.14f);
            yield return MoveRT(b.root, slotPos, home, 0.22f);
            b.emptied = b.count <= 0;
            if (b.emptied) { var img = b.root.GetComponent<Image>(); if (img) img.color = new Color(0.90f, 0.89f, 0.93f); }
            busy = false; UpdateStatus(); CheckWin();
        }

        IEnumerator MoveRT(RectTransform rt, Vector2 from, Vector2 to, float dur)
        {
            float t = 0f; while (t < dur) { t += Time.deltaTime; float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / dur)); rt.anchoredPosition = Vector2.LerpUnclamped(from, to, u); yield return null; }
            rt.anchoredPosition = to;
        }

        IEnumerator RotateLid(RectTransform lid, float a, float b, float dur)
        {
            float t = 0f; while (t < dur) { t += Time.deltaTime; float u = Mathf.Clamp01(t / dur); lid.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(a, b, u)); yield return null; }
            lid.localRotation = Quaternion.Euler(0, 0, b);
        }

        IEnumerator FlyCube(Vector3 fromWorld, int cell, int color)
        {
            var flyer = NewImage("fly", canvasRT, colors[color], false);
            var frt = flyer.rectTransform; frt.sizeDelta = cellImg[cell].rectTransform.sizeDelta * 1.1f;
            flyer.transform.position = fromWorld;
            Vector3 a = flyer.transform.position, to = cellImg[cell].rectTransform.position;
            float dur = 0.3f, t = 0f;
            while (t < dur) { t += Time.deltaTime; float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / dur)); flyer.transform.position = Vector3.LerpUnclamped(a, to, u); yield return null; }
            filled[cell] = color; cellImg[cell].color = colors[color];
            StartCoroutine(PopCell(cellImg[cell].rectTransform));
            Destroy(flyer.gameObject);
        }

        IEnumerator PopCell(RectTransform rt)
        {
            float t = 0f; while (t < 0.14f) { t += Time.deltaTime; float u = t / 0.14f; float s = 1f + 0.4f * Mathf.Sin(u * Mathf.PI); rt.localScale = new Vector3(s, s, 1); yield return null; }
            rt.localScale = Vector3.one;
        }

        int NextEmptyCellOfColor(int color)
        {
            for (int i = 0; i < target.Length; i++) if (target[i] == color && filled[i] < 0) return i;
            return -1;
        }

        void Render()
        {
            for (int i = 0; i < cellImg.Length; i++)
            {
                if (target[i] < 0) cellImg[i].color = new Color(1, 1, 1, 0);
                else if (filled[i] >= 0) cellImg[i].color = colors[filled[i]];
                else cellImg[i].color = new Color(0.92f, 0.90f, 0.95f);
            }
            UpdateStatus();
        }

        void UpdateStatus()
        {
            int done = 0; for (int i = 0; i < filled.Length; i++) if (filled[i] >= 0) done++;
            if (statusText != null) statusText.text = "Pixel Pour — pour  (" + done + " / " + totalNeeded + ")";
        }

        void CheckWin()
        {
            for (int i = 0; i < target.Length; i++) if (target[i] >= 0 && filled[i] < 0) return;
            won = true; if (winPanel != null) winPanel.SetActive(true);
        }

        void Restart()
        {
            if (busy) return;
            won = false; if (winPanel != null) winPanel.SetActive(false);
            for (int i = 0; i < filled.Length; i++) filled[i] = -1;
            for (int i = 0; i < cellImg.Length; i++) cellImg[i].rectTransform.localScale = Vector3.one;
            BuildBaskets(); Render();
        }

        Image NewImage(string name, Transform parent, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = color; img.raycastTarget = raycast; return img;
        }

        Text NewText(string name, Transform parent, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>(); txt.text = text; txt.fontSize = size; txt.color = color; txt.raycastTarget = false;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return txt;
        }

        GameObject NewButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = Color.white; go.AddComponent<Button>().onClick.AddListener(onClick);
            var txt = NewText("L", go.transform, label, 34, new Color(0.36f, 0.30f, 0.55f)); txt.alignment = TextAnchor.MiddleCenter; Stretch(txt.rectTransform);
            return go;
        }

        void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    }
}

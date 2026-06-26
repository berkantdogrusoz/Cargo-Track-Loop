using System.Collections.Generic;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Importer'in urettigi potreleri tutan asset. Her potre, satir-satir char grid (P/B/Y/G/U/O renk, '.' = bos).
    /// PortraitImporter (Editor) PNG'leri buraya yazar; ArrowsPixelGame bu asset'i kullanir.
    /// Icerik dikeyse cerceve otomatik dikey olur (potre build bounding-box'a gore boyutlanir).
    /// </summary>
    [CreateAssetMenu(fileName = "PortraitSet", menuName = "Color Cargo Loop/Portrait Set")]
    public sealed class ArrowsPixelPortraitSet : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            public string name;
            public string[] rows;                         // potre satirlari (her char bir renk; '.' = bos)
            [TextArea(4, 40)] public string preview;      // okunabilir onizleme (importer doldurur, salt-bilgi)
        }

        public List<Entry> portraits = new List<Entry>();

        public bool HasPortraits { get { return portraits != null && portraits.Count > 0; } }

        public string[][] ToRowsArray()
        {
            if (!HasPortraits) return null;
            string[][] arr = new string[portraits.Count][];
            for (int i = 0; i < portraits.Count; i++) arr[i] = portraits[i].rows;
            return arr;
        }
    }
}

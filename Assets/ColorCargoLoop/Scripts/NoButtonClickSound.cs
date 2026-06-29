using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Bir Button'a takilirsa ButtonClickSound o butona genel "pick" click sesini EKLEMEZ.
    /// Orn. win "devam et" butonu -> sadece coin sesi calsin diye bu componenti butona ekle.
    /// </summary>
    public sealed class NoButtonClickSound : MonoBehaviour { }
}

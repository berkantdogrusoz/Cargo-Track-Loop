using UnityEngine;
using UnityEngine.UI;

namespace ColorCargoLoop
{
    /// <summary>
    /// Canvas'a (veya buton kokune) takilir: altindaki TUM Button'lara tiklayinca
    /// AudioManager.Sfx.Button calar. AudioManager'da "Button" klibi atanmis olmali.
    /// </summary>
    public sealed class ButtonClickSound : MonoBehaviour
    {
        void Start()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b == null) continue;
                if (b.GetComponent<NoButtonClickSound>() != null) continue; // bu buton haric (orn. coin sesli buton)
                b.onClick.RemoveListener(PlayClick); // tekrar takilmaya karsi
                b.onClick.AddListener(PlayClick);
            }
        }

        static void PlayClick()
        {
            AudioManager.Play(AudioManager.Sfx.Button);
        }
    }
}

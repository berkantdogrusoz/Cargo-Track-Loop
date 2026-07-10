using UnityEditor;
using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// Google Play Games Services kurulumunu tek tikla uygular.
    /// Play Console > Play Oyun Hizmetleri > Yapilandirma > "Kaynaklari al" XML'i gomulu.
    /// (app_id: 696453330530, paket: com.Altare.CandyCargo, cloud projesi: pixel-pour-77f13)
    /// Calisinca Assets/GooglePlayGames altina GameInfo/ayar dosyalarini uretir ve EDM resolve tetikler.
    /// </summary>
    public static class GPGSSetupTool
    {
        const string ResourceXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
  <string name=""app_id"" translatable=""false"">696453330530</string>
  <string name=""package_name"" translatable=""false"">com.Altare.CandyCargo</string>
</resources>";

        [MenuItem("Color Cargo Loop/Google Play/GPGS Kurulumunu Uygula (Pixel Pour)")]
        static void ApplySetup()
        {
            bool ok = GooglePlayGames.Editor.GPGSAndroidSetupUI.PerformSetup(
                "",                        // web client id (ID token gerekmedigi icin bos)
                "Assets/GooglePlayGames",  // sabit dosyalarinin yazilacagi klasor
                "GPGSIds",                 // uretilecek sinif adi
                ResourceXml,
                null);                     // nearby service yok
            if (ok)
                Debug.Log("[GPGS] KURULUM TAMAM - app_id 696453330530 / com.Altare.CandyCargo. Cihaz build'inde otomatik giris hazir (GooglePlayLogin acilista calisir).");
            else
                Debug.LogError("[GPGS] Kurulum BASARISIZ - Console'daki hata mesajlarina bak.");
        }
    }
}

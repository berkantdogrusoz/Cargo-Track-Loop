# Pixel Pour - Google HTML5 Playable

Google App Campaigns icin, oyunun guncel Unity sahnesini temel alan responsive oynanabilir reklam paketi.

## Cikti

- `PixelPour_Google_Playable.zip`: Google Ads'e yuklenecek paket.
- `index.html`, `styles.css`, `game.js`: Sahne, responsive yerlesim ve mini oyun akisi.
- `assets/`: Tamami yerel ve web icin optimize edilmis oyun varliklari.
- `tools/render_assets.py`: Unity projesindeki FBX ve texture dosyalarindan seffaf reklam gorselleri uretir.

## Unity ile Eslesen Varliklar

- Kum zemini: `Assets/Art/Dekor/kum_zemin.png`
- Anubis, piramit, kaktus, kaya ve kemikler: Oyunda kullanilan FBX + base-color texture dosyalari
- Booster bari ve dort buton: Sahnedeki gercek PNG sprite'lari
- HUD: Oyundaki kalp, coin ve tutorial el sprite'lari
- Ses: Unity sahnesindeki jump, land, tekil kup atisi ve tamamlanma SFX'leri
- Portre: `Assets/Art/Portraits/PortraitSet.asset` icindeki Level 1, 56x56 satir ve 12 renkli adaptif palet

## Oynanis

Oyuncu kuyruktaki Anubis'e dokunur. Portreye bakan Anubis kendi sutunundan slota ziplayip yerlesir; sayaci azalirken kupleri sesli ve tek tek portreye firlatir. Her kup varisinda gercek Level 1 portresinin ilgili hucreleri parca parca acilir. Dev kup ve ekstra slot booster'lari da etkilesimlidir. Yedi hamle sonunda tamamlanan gercek portre ve Google `ExitApi.exit()` kullanan CTA gorunur.

## Google Kontrolleri

- Paket kokunde `index.html` bulunur.
- Tek harici referans Google Ads resmi `ExitApi` betigidir; oyun gorsellerinin tamami yereldir.
- Ses yalnizca ilk oyuncu etkilesiminden sonra calar; otomatik video yoktur.
- Portrait ve landscape yerlesimler ayni 9:16 oyun sahnesini korur.
- Paket 5 MB ve 512 dosya sinirlarinin altindadir.
- JavaScript syntax, asset yukleme, sutun kaymasi, portre doldurma ve final karti tarayicida test edilmistir.

3D reklam varliklarini tekrar uretmek icin:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'PlayableAds/Google/tools/render_assets.py'
```

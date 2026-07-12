# Pixel Pour - Google HTML5 Playable

Google App Campaigns icin responsive oynanabilir reklam paketi.

## Cikti

- `PixelPour_Google_Playable.zip`: Google Ads'e yuklenecek dosya.
- `index.html`: Tek sayfalik oynanis, CSS ve JavaScript.
- `assets/`: Pakete gomulu, optimize edilmis yerel gorseller.

## Oynanis

Oyuncu parlayan pandadaki dogru renkli kubu secer. Kup portreye ucar,
sekiz dogru hamlede portre tamamlanir ve `UCRETSIZ OYNA` butonu gorunur.
Buton Google `ExitApi.exit()` entegrasyonunu kullanir.

## Google Kontrolleri

- Responsive portrait ve landscape meta etiketi mevcut.
- Harici varlik yoktur; yalnizca Google'in izin verdigi `exitapi.js` kullanilir.
- Ses veya otomatik video oynatma yoktur.
- Local storage kullanilmaz.
- ZIP kokunde `index.html` bulunur.
- Paket 5 MB ve 512 dosya sinirlarinin altinda tutulur.

Test edilen gorunumler: 320x480, 360x640 ve 640x360.

Not: Google App Campaigns video ve playable varliklarini kampanya icinde kendisi
eslestirir. Playable ZIP'i video varliklariyla ayni app campaign/ad group'a ekleyin;
her gosterimde videodan hemen sonra playable acilmasi Google tarafindan garanti edilmez.

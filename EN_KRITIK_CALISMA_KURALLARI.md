# EN KRITIK CALISMA KURALLARI

Bu dosya Color Cargo Loop projesinde calisan her ajan/asistan icin baglayici kural dosyasidir.
Kullanici baska bir sey istemedigi surece bu dosyadaki kurallar her gorevden once okunmus ve kabul edilmis sayilir.

## 1. Ana Kural

Sadece kullanicinin acikca istedigi duzenleme yapilacak.

Kullanicinin istemedigi hicbir sey degistirilmeyecek, eklenmeyecek, silinmeyecek, yeniden adlandirilmayacak, tasinmayacak, buyutulmeyecek, kucultulmeyecek veya "iyilestirme" bahanesiyle oynanmayacak.

Bu kural; kod, sahne, prefab, material, UI, text, animasyon, kamera, isik, obje, hareket, hiz, renk, boyut, efekt, particle, level data, build ayari ve proje dosyalarinin tamamini kapsar.

## 2. Degisiklik Siniri

Her istekte once su sinir cizilecek:

1. Kullanici tam olarak ne istedi?
2. Bu istek hangi dosya, method, field, prefab, scene objesi veya ayarla dogrudan ilgili?
3. Bu istegi yapmak icin en az hangi satirlar degismeli?

Bu sorularin disinda kalan hicbir seye dokunulmayacak.

Bir satir, obje veya ayar kullanicinin istedigi sonuc icin dogrudan gerekli degilse degistirilmeyecek.

## 3. Yasaklar

Kullanici acikca istemedikce sunlar kesinlikle yasaktir:

- Yeni ozellik eklemek.
- Mevcut ozelligi kaldirmak.
- Sahnedeki objeleri silmek.
- Sahneye yeni obje eklemek.
- Obje pozisyonu, rotasyonu veya scale degerini degistirmek.
- Kamera acisi, kamera konumu, orthographic size veya background rengini degistirmek.
- Light intensity, light color veya light rotation degistirmek.
- Material, shader, renk, texture, smoothness, metallic veya outline ayari degistirmek.
- Animasyon, hareket, tween, hiz, gecikme, timing veya easing degistirmek.
- UI text, font, buton, panel, ikon veya layout degistirmek.
- Level data, capacity, move limit, grid, slot, path, cart sayisi veya cargo dizilimi degistirmek.
- Prefab, scene, meta veya ProjectSettings dosyalarina dolayli sebeple dokunmak.
- Refactor yapmak.
- Kod stilini topluca duzeltmek.
- Dosya tasimak, yeniden adlandirmak veya temizlemek.
- "Daha guzel olur", "daha temiz olur", "zaten gerekliydi" diyerek ekstra is yapmak.

## 4. Kod Kurallari

Kodda sadece istenen davranisi saglayan minimum degisiklik yapilir.

- Ilgili olmayan methodlara dokunulmaz.
- Ilgili olmayan field default degerleri degistirilmez.
- Public API, serialized field ve inspector baglantilari korunur.
- Mevcut scene serialize degerlerini bozabilecek alanlar silinmez veya yeniden adlandirilmaz.
- Helper method ancak istenen degisiklik onsuz temiz ve guvenli yapilamiyorsa eklenir.
- Yorumlar, bosluklar ve format sadece dokunulan kucuk bolgede gerekiyorsa degisir.
- Gereksiz kod sisirmesi yapilmaz.

## 5. Sahne ve Gorsel Kurallari

Sahne nasil teslim alindiysa o sekilde korunur.

- Kullanici obje ekle demediyse obje eklenmez.
- Kullanici obje sil demediyse obje silinmez.
- Kullanici boyut degistir demediyse scale degismez.
- Kullanici hareket degistir demediyse hareket/hiz/animasyon degismez.
- Kullanici renk degistir demediyse renk/material degismez.
- Kullanici UI yazisi degistir demediyse text degismez.
- Kullanici kamera degistir demediyse kamera degismez.

Gorunumde kaybolan obje, farkli obje, beklenmeyen renk, beklenmeyen boyut, beklenmeyen hareket veya yeni efekt cikmasi kabul edilemez.

## 6. Teslim Sekli

Her gorevde teslimat su sekilde yapilir:

1. Sadece istenen duzenleme uygulanir.
2. Mumkunse compile/build/test kontrolu yapilir.
3. Kullaniciya sadece yapilan degisiklik net sekilde soylenir.
4. Ekstra oneriler, yeni fikirler veya baska ozellik teklifleri zorla eklenmez.

## 7. Test Hedefi

Bu oyun kapali test / oynanabilir test hedefine gidecek.
Bu nedenle hizli, net, minimum riskli ve kullanici istegine birebir bagli duzenleme yapilacak.

Testi bitirmeyi geciktirecek gereksiz mimari, polish, refactor, gorsel oynama veya kapsam genisletme yapilmayacak.

## 8. Durma Kriteri

Istenen degisiklik tamamlandiginda dur.

Kodda veya sahnede baska bir sorun fark edilirse:

- Kullanici onu istemediyse degistirme.
- Sadece raporda kisa ve net belirt.
- Kullanici onay verirse ayri gorev olarak ele al.

## 9. Kirmizi Cizgi

Bu projede "ben bunu da duzelttim" yaklasimi yasaktir.

Dogru yaklasim:

> "Sadece istedigini yaptim. Diger her seyi aynen biraktim."

Yanlis yaklasim:

> "Bunu yaparken sahneyi, UI'yi, objeleri, animasyonu veya kod mimarisini de toparladim."

Bu dosyadaki kurallar, kullanici acikca aksini soyleyene kadar gecerli ana calisma sozlesmesidir.

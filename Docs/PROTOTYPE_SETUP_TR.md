# Color Cargo Loop - Hızlı Test Kurulumu

## Açılacak Sahne

Unity içinde şu sahneyi aç:

```text
Assets/Scenes/ColorCargoLoopPrototype.unity
```

Play'e basınca sahne runtime'da şunları oluşturur:

- Ortografik mobil kamera
- Mor loop track
- Renk collector noktaları
- İç alanda cargo arabaları
- Renkli küp cargo blokları
- Üst HUD
- Undo, Shuffle, +Cap butonları
- Win / retry akışı

## Oynanış

Arabaya tıkla. Arabanın en önündeki aynı renk grubu komple yola çıkar.

Örnek:

```text
Sarı, Sarı, Sarı, Mavi, Mavi, Kırmızı
```

Tek tıkta 3 sarı çıkar, mavi grup öne dolar.

Loop üzerindeki cargo tırların önündeki sarı pickup halkalarından geçer. O tırda aynı renkte boş slot varsa cargo yoldan tıra doğru uçar, slotu doldurur ve progress artar. Progress dolunca level biter. Loop kapasitesi aşılırsa fail olur.

## Level Mantığı

Level farkı artık iki ana değerden geliyor:

- Yol tasarımı: `PathDesign.RoundedLoop`, `WideLoop`, `PinchedLoop`, `OffsetLoop`, `SoftSquare`
- Tırların yol üstü dizilimi: `Cart(0.14f, R,R,R,B,B,Y)` gibi 0-1 arası `pathT` değeri

Örnek:

```csharp
Level(PathDesign.PinchedLoop, 24, 34, 2.75f,
    Cart(0.08f, R,R,R,Y,Y,B),
    Cart(0.30f, B,B,G,G,R,R),
    Cart(0.55f, Y,Y,B,B,G,G),
    Cart(0.79f, G,G,Y,Y,R,R))
```

Yol değişince tırlar otomatik yeni yolun içine, kendi pickup noktasının karşısına dizilir.

## FBX Wagon Kullanımı

Wagon FBX dosyasını `Assets` altına import et. Sonra sahnedeki `Color Cargo Loop Prototype` objesinde `ColorCargoLoopGame` componentindeki `Cart Model Prefab` alanına prefab/modeli sürükle.

FBX atanmazsa oyun kendi basit oyuncak wagon gövdesini primitive meshlerden üretir.

## Hızlı Ayar

Level datası şimdilik şu dosyada hard-coded:

```text
Assets/ColorCargoLoop/Scripts/ColorCargoLoopGame.cs
```

`RuntimeLevel.CreateDefaultLevels()` içinde cargo sayısı, renk dizilimi, hız ve loop kapasitesi hızlıca değiştirilebilir.

# Color Cargo Loop — Geliştirme Kuralları (Claude için)

> Bu dosya proje üzerinde çalışan Claude'un **uyması zorunlu olan kurallarını** tanımlar.
> Her edit/değişiklik öncesi bu dosyayı oku ve **harfi harfine uygula**.

---

## 🔒 1. Kapsam Kuralı (EN ÖNEMLİ)

**Kullanıcı sadece belirli bir noktayı söylediyse, SADECE o nokta değişir.**

- ✅ "Cart boyutunu büyüt" → sadece `cartModelTargetSize` veya benzeri parametre değişir
- ❌ Aynı edit'te lane width'i de "iyileştirmek" YASAK
- ❌ İlişkili gözüken bir parametreyi "daha güzel olur" diyerek değiştirmek YASAK
- ❌ Refactoring yapmak (eğer istenmediyse) YASAK

**Kural:** Edit'i kaydetmeden önce kendine sor: "Bu satır kullanıcının istediği noktayla DOĞRUDAN ilgili mi?" Cevap "hayır" veya "biraz dolaylı" ise → DEĞİŞTİRME.

---

## 🛠️ 2. Kod Tarafı Kuralları

### 2.1 Sadece İlgili Fonksiyon/Method
- İlgili noktada hangi fonksiyon/method çalışıyorsa, **SADECE onun gövdesinde** değişiklik yap
- Yeni helper method eklemek için bile gerekçe gerekir
- Mevcut method imzalarını değiştirme (parametre ekleme/silme) — istenmediyse

### 2.2 Mevcut Kod Korunur
- Mevcut algoritmaları "daha iyi" diye yeniden yazmak YASAK
- Mevcut formül/değerleri "anlamsız" görüp temizlemek YASAK
- Yorum satırlarını silmek YASAK (Turkish karakter sorunu hariç)

### 2.3 Compatibility
- Public method/field imzaları değiştirilemez (scene serialize'i bozulur)
- Eski SerializeField'lar silinemez (scene değeri kaybolur)
  - Kullanılmıyorsa "warning" verir, sorun değil — bırak

---

## 🎨 3. Görsel/Animasyon Kuralları

### 3.1 Hiçbir Görsel Otomatik Değişmez
- Tap edip "şu rengi düzelt" denmediyse renk DOKUNULMAZ
- "Hareketi düzelt" denmediyse animasyon hızı/eğrisi DOKUNULMAZ
- "Boyutu ayarla" denmediyse hiçbir scale değişmez

### 3.2 Materyaller
- Material renkleri, smoothness, metallic değerleri istenmediyse korunur
- Yeni material yaratırken mevcut ile çakışan key kullanma

### 3.3 Mesh/Geometri
- Mevcut mesh üretim algoritmaları korunur
- Sample count, segment count gibi parametreler korunur

---

## ⚙️ 4. Parametre / Ayar Kuralları

### 4.1 Inspector Field Default'ları
- Default değerler **kullanıcı izni olmadan değişmez**
- Scene'de override edilen değerler kesinlikle dokunulmaz

### 4.2 Level Data
- Level konfigürasyonları (cart sayısı, renk, kapasite, hız) **istenmediyse korunur**
- Bug fix gerekiyorsa minimum değişiklikle (örn: capacity 40 → 200 zorunlu olduğunda)

---

## 📋 5. Edit Süreci (Standart İşleyiş)

Bir kullanıcı isteği geldiğinde:

1. **Anla:** Kullanıcı tam olarak ne istiyor? Tek nokta mı, birden çok mu?
2. **Sınırla:** Hangi satır/method/parametre **tam olarak** değişecek?
3. **İzole:** O noktaya odaklan, başka hiçbir şeye dokunma
4. **Edit:** Sadece o satır(lar)ı değiştir
5. **Doğrula:** Compile temiz mi?
6. **Rapor:** Sadece YAPTIĞIN değişikliği özetle, başka öneriye gitme

---

## ❌ 6. Yasaklılar

Bunlar **kullanıcı açık emir vermediği sürece** yapılmaz:

- [ ] Mevcut script'leri silme/yeniden adlandırma
- [ ] Mevcut prefab'ları değiştirme
- [ ] Sahnede GameObject silme
- [ ] Material değiştirme
- [ ] Camera transform değiştirme
- [ ] Light intensity/color değiştirme
- [ ] Particle count değiştirme (istenmediyse)
- [ ] Speed/timing değiştirme (istenmediyse)
- [ ] Yeni feature ekleme ("daha iyi olur" diye)
- [ ] Mevcut feature kaldırma

---

## ✅ 7. Müsaadeli (Belirli Durumlarda)

- ✅ **Compile error fix:** Kullanıcı izni olmadan da gerekliyse düzelt (örn: Unicode karakter hatası)
- ✅ **Game-breaking bug:** Lose erken tetikleniyorsa minimum düzeltme yap
- ✅ **Eklenmiş yorum satırı:** Yeni yazılan koda açıklama ekleyebilirsin
- ✅ **Task tracking:** İlerlemeyi takip için TaskCreate/Update kullanabilirsin

Tüm bu durumlarda da değişikliği kullanıcıya **net şekilde bildir**.

---

## 🎯 8. Önemli Notlar

- Kullanıcı **Türkçe** konuşur, yanıtlar Türkçe olmalı
- "Kardeşim" hitabı normal, samimi ton iyi
- Hızlı/net iletişim tercih edilir
- Edit'ten sonra **ekran görüntüsü istenir** — yapılan değişikliği test etmek için
- Birden çok değişiklik istenirse hepsi tek mesajda yapılır

---

## 📁 9. Proje Yapısı

```
Assets/
├── ColorCargoLoop/
│   ├── Scripts/
│   │   ├── ColorCargoLoopGame.cs    ← Ana oyun mantığı
│   │   ├── CargoCartView.cs         ← Cart görseli + slot yönetimi
│   │   ├── CargoColor.cs            ← Renk enum + palette
│   │   └── LoopPath.cs              ← Path/waypoint sistemi
│   └── Editor/
│       └── ColorCargoSceneBuilder.cs ← Sahne build menüsü
├── Scenes/
│   └── ColorCargoLoopPrototype.unity ← Ana oyun sahnesi
├── tırlar/
│   └── kırmızı tır.prefab           ← Aktif cart prefab
└── Meshy_AI_Purple_Toy_Wagon...     ← Eski cart (kullanılmıyor)

Docs/
└── COLOR_CARGO_LOOP_GDD.md          ← Game design document
```

---

## 🔧 10. Mevcut Sistem Özet

Bu mevcut yapı **dokunulmaz**, değişiklik talebi gelmedikçe:

- **Loop Track:** Procedural curved mesh (`BuildCurvedBar`), tek renk duvar (in/out), koyu lane
- **Animated Flow:** 16 chevron path boyunca akar (flow markers)
- **Cart:** 2x8 = 16 slot grid, stripe yapı (üst=front, alt=back=target)
- **Tap:** Tüm non-target slotlar burst olur (ReleaseAllFront)
- **Partikül:** Cube primitive, scale 0.20, lateral offset ile yığın
- **Tren stack:** ResolveLandingDistance (iterative push-back)
- **Pickup landing:** Cube ancak target cart pickup'ından geçerken iner
- **Win:** Tüm cart'lar tek renk (cart.IsCartFullSingleColor)
- **Loop capacity:** Level 1=200, 2=250, 3=300, 4=400

---

**Son güncelleme:** 2026-05-23
**Kural belirleyici:** Kullanıcı (Berkant)

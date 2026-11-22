# CAM — Game view kayması + editör 9×9 kadrajı (ajan-CAM)

Durum: bitti (22.11 16:45).

## Adımlar
- [x] 0. Okuma (AGENTS, MIMARI, ORTAK-BRIEF, T2, LE1, B1, P2; CameraFitter, ScreenLayout, LayoutModeController, EditorBoardViewport, ToolkitGameUI(.Layout), GamePanelSettings)
- [x] 1. Yeniden üretme (`once_fixed1080_z065_L10.jpg`)
- [x] 2. Düzeltme (LayoutModeController) — `8e33236`
- [x] 3. Editör 9×9 kadrajı (CameraFitter + EditorBoardViewport) — `8e33236`
- [x] 4. Doğrulama görüntüleri, ölçüm tablosu, Console temiz

## Kaymanın nedeni
Device Simulator sekmesi Game view ile aynı dock grubunda açıkken (Asen'in düzeni), Simulator'ın shim'i Game view odaktayken de `Screen.safeArea`'yı **simüle edilen cihazın pikselleriyle** veriyor; `Screen.width/height` ise Game view'un boyutu. Ölçüm (Game view sabit 1080×1920, ölçek 0.65, Simulator cihazı Galaxy S7):
`Screen` 1080×1920, `Screen.safeArea` (0,0,1440,2560), `UnityEngine.Device.Screen` 1440×2560 → `SafeViewport` (0,0,1.333,1.333).
- Kamera: `m02 = 1-(xMin+xMax) = -0.333` → dünya ekran genişliğinin 1/6'sı sağa kayar, sağdan kesilir.
- HUD: kök padding negatif (UI Toolkit 0'a kırpar) ama `hudTop.top = HudTopFraction·h − safeTop` (safeTop negatif) → HUD aşağı iner, otobüslerin üstüne biner.
- Tam ekranda (maximize) Simulator sekmesi görünmez → shim yok, safeArea = Screen → doğru. Simulator'ın kendisinde ise B1'in gördüğü durum: bazen `Screen` Game view boyutunu (ör. 707×1257) döndürüyor, safeArea cihaz pikselinde.

## Düzeltme
`LayoutModeController.Refresh`: safeArea `UnityEngine.Device.Screen` ölçüsünden `Screen`'e oranla ölçeklenir (cihazda iki ölçü aynı → etkisiz). `ApplyViewport`: güvenli alan ekrana kırpılır, boş/geçersizse tam ekran. Kamera/HUD/editör bu normalize değeri kullandığı için başka dosyaya gerek kalmadı.

## Editör kamerası
`CameraFitter.SetMinimumGrid(c, r)`: kadraj derinlik/genişlikte en az bu grid boyutuna göre kurulur. `EditorBoardViewport` OnEnable'da `GameRules.MaxWidth × MaxHeight` (9×9) verir, OnDisable'da 0×0. Oyun sahnesinde çağıran yok → oyun kadrajı aynı. Board yatayda zaten ortalı (CellToLocal), dikeyde slot şeridine bağlı (oyundaki gibi).

## Ölçüm tablosu (düzeltme sonrası)
| Görünüm | Screen | safeArea (ham) | Device | SafeViewport | m02 | kamera y | HUD üst |
|---|---|---|---|---|---|---|---|
| Önce: Game 1080×1920 sabit, ölçek 0.65 | 1080×1920 | 0,0,1440,2560 | 1440×2560 | 0,0,1.333,1.333 | -0.333 | 30.32 | ~0.38 |
| Game 1080×1920 sabit, ölçek 0.65 / 0.40 | 1080×1920 | 0,0,1440,2560 | 1440×2560 | 0,0,1,1 | 0 | 30.32 | 0.045 |
| Game 9:16 aspect (707×1257) | 707×1257 | 0,0,1440,2560 | 1440×2560 | 0,0,1,1 | 0 | 30.32 | 0.045 |
| Game Free Aspect (1322×1257) | 1322×1257 | 0,0,1440,2560 | 1440×2560 | 0,0,1,1 | 0 | 30.32 | 0.045 |
| Game tam ekran (708×1259) | 708×1259 | 0,0,708,1259 | 708×1259 | 0,0,1,1 | 0 | 30.32 | 0.045 |
| Sim iPhone 12 (çentik) | 1170×2532 | 0,102,1170,2289 | 1170×2532 | 0,.040,1,.904 | 0 | 32.18 | 0.045 |
| Sim Galaxy S10+ (uzun) | 1440×3040 | 0,0,1440,2898 | 1440×3040 | 0,0,1,.953 | 0 | 30.32 (L1) / 30.97 (L10, L18; genişlikten) | 0.045 |
| Sim iPad Pro 11 | 1668×2388 | 0,40,1668,2348 | 1668×2388 | 0,.017,1,.983 | 0 | 31.07 | 0.045 |
| Editör 1080×1920 / iPhone 12 / iPad Pro 11, 4×4·7×8·9×9 | — | — | — | — | (bölge kaydırması) | 33.14 her boyutta | — |
Simulator ve tam ekran değerleri LE1 tablosuyla aynı (iPhone 12 = p1170 32.18, iPad = t1668 31.07) → gerçek cihaz/çentik davranışı değişmedi. Board her çekimde yatayda ortada (boardX 0.500).

## Görüntüler (CAM-shots/)
- Önce: `asen_gameview_kayik.png` (Asen), `once_fixed1080_z065_L10.jpg` (yeniden üretim).
- Sonra Game view: `gv1080z065_L1/L10/L18.jpg`, `gv1080z040_L10.jpg`, `gv9x16_L10.jpg`, `gvfree_L10.jpg`, tam ekran `tam_L1/L10/L18.jpg`; yan yana `yanyana_gameview.jpg`.
- Simulator: `sim_iphone12_*`, `sim_s10plus_*`, `sim_ipadpro11_*` (L1/L10/L18; Simulator çerçevesi olmadan render dokusu); `yanyana_simulator.jpg`.
- Editör: `editor_1080x1920_4x4/7x8/9x9.jpg`, `editor_iphone12_4x4/9x9.jpg`, `editor_ipadpro11_4x4/9x9.jpg`; `yanyana_editor.jpg`. Önceki hal: `LE1-shots/editor_*`, `LE2-shots/sonra_main_1080x1920.jpg`.

## Notlar
- Yakalama aracı geçici editör script'iydi (`Assets/CAMTemp`, silindi; kopya scratchpad `cam/CamCapture.cs`). Game view boyutu (indeks 32, 1080×1920), Simulator cihazı (Galaxy S7), sahne (LevelEditor) başlangıç değerine döndü. Kayıt: LevelIndex 11, Coins 275 değişmedi; Lives 4 (X1 16:23'te 3 bırakmıştı → doğal yenilenme; can harcayan yol çalışmadı).
- Console: derleme sonrası 0 hata/uyarı.
- Editörde board alanının yüksekliği UI'ya bağlı: otobüs listesi 2 satıra çıkınca (çok otobüs) alan kısalır, görüntü biraz küçülür; kamera aynı.

## Asen'in bakması gerekenler
1. Kendi düzeninde (Game + Simulator sekmesi yan yana) Game view'u küçük pencere/sabit 1080×1920 ile Play: board ortada, HUD üstte mi.
2. Simulator'da çentikli telefon: HUD çentiğin altında mı (önceki gibi).
3. Level Editör: küçük levelde yol + otobüs kuyruğu + slotlar tam görünüyor mu; kuyruğun sol ucu sol yolcu panelinin altında kalıyor (UI paneli, kamera değil).
4. Not: Simulator sekmesi Game view'la aynı gruptayken Game view, Simulator cihazının çentik oranlarını uygular (ör. iPhone seçiliyse Game view'da da üst boşluk). Kayma yok, yalnız boşluk; istenmezse Game view'u ayrı gruba al.

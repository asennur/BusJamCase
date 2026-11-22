# LE2 — Level Editör UI kullanılabilirlik + tünel paneli (ajan-LE2)

Durum: bitti (22.11 ~13:25). Kod commit'i `8db366c`.

## Kapsam
1. Tünel ekleyince panel/arka plan bozuk: yeniden üret, bul, düzelt.
2. Tüm araç/butonlar tek tek dene; taşan/çakışan/küçük dokunma alanlarını düzelt, yanlış girişte anlaşılır mesaj.
3. Oyunun görsel dili (Fredoka, kontur/gölge, vektör buton/panel), İngilizce metin, yeni doku yok (T3).
4. Görüntüler: telefon 9:16, 9:19.5, tablet 3:4; tünel paneli önce/sonra → LE2-shots/.

## Çakışma
- LevelEditor.unity LE1'de (sahne eşitleme, GameRules 9×9, LevelEditorPanel.cs boyut kontrolü, LevelData.Validate). LE2 sahneye dokunmaz.

## Adımlar
- [x] 0. Okuma (AGENTS, MIMARI §6, ORTAK-BRIEF, T2, R4, R8, T3, UXML/USS/cs)
- [x] 1. Önce görüntüleri (12:57-13:00, LE2 kilidi): LE2-shots/once_*.
- [x] 2. Tünel hatasının nedeni: popup bir ScrollView'a `.modal` verilmişti; VectorArt `vector-modal` katmanı ekliyor ama yalnız settings/pause/confirm/no-lives için USS'te doku var → editör popup'ı tamamen saydam, karartma da yok (overlay picking Ignore, arka plan rengi yok). Yazı ve butonlar tahtanın üstünde yüzüyor; renk ekleme satırı popup'tan taşıyor (ScrollView + `.modal` align-items:center), kapatma butonu uzakta.
- [x] 3. Kod/UXML/USS (Roslyn dış derleme 0 hata): oyun popup ailesi (surface_result_modal + başlık şeridi + close), soket (surface_result_inner), karartma + dışına dokununca kapanma, Esc kapatır; tünelde Up/Down/Left/Right (seçili yeşil), kuyruk çipini seç → < > Remove, Remove tunnel; yolcu: Hidden (oyun toggle'ı), Ropes -/+ (− glifi fontta yoktu), Cloak, Remove; otobüs sırası sıralanabilir; dokunmatik: yolcuya/tünele dokun = düzenle; kaydedilmemiş değişiklikte New/Load iki basış; yeni levelde seçici "New level N (not saved)", Load index -1 çökmesi; boş otobüs sırasında İngilizce hata.
- [x] 4. Editörde test (13:09-13:15, LE2 kilidi; Play'de UI butonlarına NavigationSubmit ile basıldı):
  - Boyut: `abc`/`12` → "Size must be 1-9 × 1-9."; 5×6 → "Grid is 5 × 6." Süre `0` → "Time must be at least 1 second."
  - Save: otobüssüz → "Cannot save: add at least one bus."; boş grid → Validate mesajı (Türkçe, aşağıda).
  - Otobüs: + ile ekle (yeni çip seçili gelir), < ile sırala, Remove; seçim yokken Remove/</> pasif.
  - Tünel: Tile→Hole→Obstacle→Tunnel dokunuşları, Tunnel'da panel açılır; kuyruğa ekle, < ile sırala, Up/Down/Left/Right; otobüsü olmayan renk → "There is no Pink bus. Add a Pink bus first."; panel açıkken board'a tık geçmez.
  - Yolcu: Ropes +/- (0'da ve 4'te mesaj), Hidden açınca/Cloak seçince diğeri kapanır ve söylenir.
  - Auto Grid 6×7 (1 tünel) → Auto Fill (12 yolcu, ≥2 çözüm) → Solve "10+ solutions".
  - Yeni level Save → Level_28 oluştu, seçicide göründü; Load (yeni levelde) → "Pick a level from the list first."; kirliyken başka level seçimi uyarır, ikinci seçim yükler; Test Play → Game "Level 28", "Editor" butonu → editörde Level 28 açık.
  - Temizlik: Level_28 silindi, LevelDatabase.asset kopyadan geri (Level_26/27 satırları duruyor, Level_27 LE1'in), PlayerPrefs LevelIndex=9 geri, aktif sahne Game.
- [x] 5. Görüntüler LE2-shots/: `tunel_once_sonra.jpg`, `once_tunnel_1080x1920.jpg`, `once_yolcu_telefon_tablet.jpg`, `sonra_{main,tunnel,passenger}.jpg` (9:16 | 9:19.5 | 3:4), tek kareler `sonra_*_WxH.jpg`, `testplay_return.jpg`.
- [ ] 6. Console temiz doğrulaması (kilit bekleniyor)

## Bilinen / Asen'e
- `LevelData.Validate` mesajları Türkçe ("Red: 0 yolcu, 3 koltuk" vb.) → editörde İngilizce arayüzün içinde Türkçe görünüyor. Validate LE1/ortak dosya; çevrilmesi önerilir.
- `LevelAutoGrid` mesajı otobüs varken de "Add buses, then Auto Fill." diyor (küçük).
- 9:19.5'te üstteki boşluk LayoutModeController safe area'sı (çentik bölgesi), bilerek.
- Solve 6 sn'ye kadar editörü dondurabilir (senkron çözücü), değişmedi.

## LE1'den not (13:10)
- LevelEditor.unity'de yeni bileşen: `Level Editor UI` üstünde `EditorBoardViewport` (Scripts/UI/LevelEditor/EditorBoardViewport.cs, LE1'in). UXML'deki `.editor-board-space` sınıflı boş alanın ekran dikdörtgenini `CameraFitter.SetScreenArea`'ya verir; oyun kadrajı (telefon ekranı gibi) o bölgeye çizilir. Bu sınıfı kaldırır/yeniden adlandırırsan haber ver (alan inspector'dan `boardSpaceClass` ile de değişir).
- Console'da LevelEditorPanel.Toolkit.cs:44 (OnEnable) ve :66 (OnDisable) NullReferenceException görüldü (13:03, script reload sırasında) — senin yarım değişikliğinden olabilir, bakar mısın.
- Board/BusStation/WaitingArea/Lighting/Background artık Prefabs/Scene/*.prefab örnekleri; LevelEditorPanel referansları yeni örneklere bağlı.

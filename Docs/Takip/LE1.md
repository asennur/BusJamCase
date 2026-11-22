# LE1 — Level Editör sahnesi oyunla birebir + 9×9 grid + kayıt akışı (ajan-LE1)

Durum: bitti (22.11 14:06).

## Adımlar
- [x] 0. Okuma (AGENTS, MIMARI, ORTAK-BRIEF, T2, W1, R6, G1, F1; CameraFitter, ScreenLayout, LevelEditorPanel, LevelData, AutoGrid/AutoFill/Solver)
- [x] 1. Fark çıkarma (Game ↔ LevelEditor YAML): editörde eksik/eski: Global Volume yok, kamera post-process kapalı, ışık 1.1 ve farklı açı, ortam Skybox (Game: Gradient), eski `World Background` görüntü quad'ı (Game'de kapalı), procedural zemin/çimen/bordür yok, Board'da `sidewalkMaterial`/`sidewalkJoinDepth` yok, BusStation egzoz/exitOffset farklı, WaitingArea standHeight yok, CameraFitter eski "bounds" kadrajında.
- [x] 2. Ortak prefab'lar (`Assets/_BusJam/Prefabs/Scene/`): Game'den `SaveAsPrefabAssetAndConnect` ile — `Lighting` (yeni kök: Directional Light + Global Volume), `Background` (Floor, Environment/GrassLayout, Curb Extensions + `World Background` altına taşındı), `Board`, `BusStation`, `WaitingArea`. Game'de sahne referansları korundu (GameManager, CameraFitter, UI); Background.layout sahne override'ı.
- [x] 3. LevelEditor.unity: eski Directional Light/Board/BusStation/WaitingArea/World Background silindi, prefab örnekleri kondu; kamera Game'den kopya (Camera, URP verisi post-process açık, render tipi Base, CameraFitter alanları; gameManager boş); RenderSettings Game'le aynı (Gradient ambient, skybox yok); LevelEditorPanel referansları yeni örneklere; `EditorBoardViewport` eklendi.
- [x] 4. Kod: `GameRules.MaxWidth/MaxHeight = 9`; LevelData/Panel/AutoGrid/AutoFill/Solver/Board/BoardView zaten boyuttan bağımsız (dinamik diziler, BFS level.width ile) → değişiklik gerekmedi. `CameraFitter`: eski bounds kadrajı silindi, hep referans kadrajı (board bağlıysa); `SetScreenArea(Rect)` ile görüntü ekranın bir bölgesine (projeksiyon NDC ölçek+kaydırma) sıkıştırılır. Tam ekranda matematik öncekiyle aynı → mevcut leveller aynı.
- [x] 5. Kayıt testi (editör Play, API çağrıları): New → 9×9 Apply (10 reddedildi: "Size must be 1-9 × 1-9") → Auto Grid (59 boş karo) → 10 otobüs → Auto Fill (2.4 sn, 30 yolcu, ≥2 çözüm) → Validate ✓ → Save: `Level_27.asset` + LevelDatabase 26→27 ✓. Başka levele geçip geri Load: hücreler/otobüsler asset'le aynı ✓. Düzenle (süre 77, (0,1) engel) → Save üzerine yazdı (diskte timeLimit 77, hücre Obstacle) ✓. Sil/yeniden adlandır editörde yok.
- [x] 6. Test Play (Level 27 seçili) → Game sahnesi açıldı, `currentLevelIndex 26`, `currentLevel Level_27`, Level 27, kamera 9×9 kadrajında (34.32) ✓ (`LE1-shots/testplay_editorden_L27.jpg`).
- [x] 7. Görüntüler (aşağıda)
- [x] 8. Temizlik (Unity MCP düştüğü için dosya düzeyinde): Level_27.asset(+meta) silindi, LevelDatabase.asset başlangıç yedeğine döndü (Asen'in Level_26 satırı duruyor, Level_26.asset dokunulmadı), Assets/LE1Temp silindi, PlayerPrefs LevelIndex registry'de 26 → 9. Unity bir sonraki odak/refresh'te bunları içeri alır.
- [x] 9. LE2 bulguları (Luma): Validate/Solver mesajları İngilizce, AutoGrid otobüs varken "Now Auto Fill." (`194056f`). Test Play koşusunda görülen `IsMatrixValid(matrix)` assert + `Screen position out of view frustum`: Game view boyutu değişirken/UI yerleşimi öncesi NaN/sıfır en-boy → CameraFitter ve EditorBoardViewport korumaları (`194056f`, Unity'nin Roslyn'iyle dış derleme 0 hata).
- [x] 10. Son temiz Console koşusu (13:52-14:05): derleme temiz; Console temizlendi → LevelEditor Play (Level 21 yükle, Auto Grid → "... Now Auto Fill."), aynı Play'de Game sahnesine geçiş, Level 21 Playing → 0 hata/uyarı (yalnız MCP köprüsünün kendi WebSocket uyarıları). Not: her Play giriş/çıkışında MCP köprüsü 10-30 dk kopuyor (13:20-13:45, 13:52-14:03).
- Kayıt verisi: LevelIndex 9, Coins 225, Tutorial bayrakları aynı; Lives 4 → 5 doğal yenilenme (20 dk).

## Asen'in commit'siz Game.unity satırları
Prefab'a çevirirken korundu ve editöre aynen taşındı: Directional Light intensity 0.85 (→ Lighting.prefab), Main Camera konumu (0, 30.32, -16.14) (sahne; editör kamerasına kopyalandı), MenuCamera dönüş/konum, Menu Background quad ölçeği, `Menu Lens Volume` (yalnız MenuCamera'nın gördüğü katman 10 → editöre gerekmedi), WaitingArea `standHeight 0.034` (→ prefab), BoardView `sidewalkEdgeExtraCells 12` (→ prefab), ToolkitGameUI resultTimeline/fade alanları (sahne). Prefab dönüşümü Game.unity'yi baştan yazdığı için bu satırlar `e9a171f` commit'ine girdi.

## Başlangıç kayıt verileri (geri yüklenecek)
LevelIndex 9, Coins 225, Lives 4, LastRegenUtc 639309379161724922, LivesUtcHighWater 639309379179594404, TutorialSeen 1/3/6/8 = 1. LevelDatabase.asset ve Level_26 yedeği scratchpad le1/backup.

## 9×9 kamera sonucu (oyun, kamera konumu; küçük grid = 7×8 ile aynı)
| Ekran | 4×4 | 7×8 | 9×9 |
|---|---|---|---|
| 1080×1920 (9:16) | 30.32 | 30.32 | 34.32 (geri) |
| 1170×2532 (9:19.5, çentik 141/102) | 32.18 | 32.28 | 41.19 (genişlikten geri) |
| 1080×2520 (9:21, 120/90) | 32.35 | 34.64 | 44.20 (genişlikten geri) |
| 1668×2388 (iPad 11, 48/40) | 31.07 | 31.07 | 33.94 |
| 1536×2048 (3:4) | 30.32 | 30.32 | 33.14 |
Her ekranda 9 sütun ve 9 satır tam görünüyor, otobüs tavanı referans yerinde (205), grid booster çubuğunun üstünde. Dar ekranda 9×9 genişlikten sınırlı → grid altı ile bar arasında boşluk kalır (kaçınılmaz).

## Görüntüler (LE1-shots/)
- `<ekran>_oyun_editor.jpg`: oyun | editör yan yana, 4×4 (Level 3), 7×8 (Level 21), 9×9 (geçici Level 27); ekranlar p1080, p1170, p2520, t1668, t1536.
- `oyun_<ekran>_<boyut>.jpg`, `editor_<ekran>_<boyut>.jpg`: tek kareler.

## Notlar
- `GrassLayoutBuilder` (Scripts/Editor) `GameObject.Find("Background")` ile sahnedeki Environment'ı yeniden kurar; artık Background bir prefab örneği → yeniden kurulum prefab modunda (Background.prefab açıkken) yapılmalı.
- Editörün board alanı UI'ya bağlı (LE2 UXML'i `.editor-board-space`); alan küçükse görüntü küçülür, kadraj aynı kalır.
- Editörde Void hücrelerinde yarı saydam işaret (EditorVoidMarker) kalıyor — editör yardımcısı, oyunda yok.

## Commit'ler
- `028cfda` kod: GameRules 9×9, CameraFitter, EditorBoardViewport, LevelData mesajı
- `e9a171f` prefab'lar + Game.unity + LevelEditor.unity + MIMARI
- `194056f` İngilizce doğrulama mesajları, AutoGrid mesajı, NaN/sıfır boyut korumaları

## Bulunan hatalar
- Editör sahnesi oyundan kopmuştu: Global Volume yok (tonemapping/grading yok), post-process kapalı, ışık 1.1/farklı açı, Skybox ambient, eski görüntü quad'ı, kaldırım birleşimi/egzoz/standHeight yok, kamera eski bounds kadrajında → prefab'larla giderildi.
- Kayıt akışında hata çıkmadı (yeni/yükle/üzerine yaz/Test Play). Sil/yeniden adlandır editörde yok.
- Validate mesajları Türkçeydi; AutoGrid mesajı yanıltıcıydı; kamera NaN koruması yoktu (yukarıda).

## Asen'in bakması gerekenler
1. Editörde bir levelı kaydet → Test Play → oyunda doğru level açılıyor mu (LE1 API ile denedi, butonla değil).
2. Editörde board alanı UI'nın kalanına göre küçük (özellikle tablet): kadraj oyunla aynı, yalnız ölçek küçük. Kabul mü.
3. 9×9 dar ekranda (9:19.5, 9:21) genişlikten sınırlı → grid altı ile booster barı arasında boşluk (LE1-shots/p2520_oyun_editor.jpg).
4. Background prefab'ı: GrassLayoutBuilder artık prefab modunda çalıştırılmalı.

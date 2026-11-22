# LE4 — Editörde doğru hücre seçimi + tünel çıkışı boş kalır (ajan-LE4)

Durum: bitti (22.11 ~17:30). Kod commit'i `0e110ae`.

## Adımlar
- [x] 0. Okuma (AGENTS, MIMARI §6, ORTAK-BRIEF, LE1-3, CAM, B1, P2; EditorBoardInput, LevelEditorPanel*, LevelData, TunnelData, LevelAutoFill, BoardView, GameplayController tünel mantığı)
- [x] 1. Yanlış algılamanın nedeni (ölçüm)
- [x] 2. Kod: seçim + tünel çıkışı kuralı — `0e110ae`
- [x] 3. Derleme: Console temiz (yalnız MCP köprüsünün kendi WebSocket uyarısı)
- [x] 4. Play testi (Simulator Galaxy S7 + Game view telefon/uzun telefon/tablet; 4×4, 7×8, 9×9)
- [x] 5. Temizlik: kayıt/level/DB başlangıçla aynı, Game view boyutu geri (32), kilit silindi

## Yanlış algılamanın nedeni
Karolarda collider yok; editör zaten collider kullanmıyordu. `BoardView.TryPickCell` dokunulan ışını **tahtanın y=0 düzlemiyle** kesiyordu. Kamera 65° eğik; yolcu ~2.2 hücre boyunda. Yolcunun gövdesine/başına dokununca ışın düzleme yolcunun **arkasındaki** hücrede değiyordu (gövde ≈0.6, baş ≈0.9 hücre geride) → arkadaki yolcu silindi/değişti, boş karoya dokunulsa öndeki yolcunun başı örtüyordu. Ölçüm (eski yöntem, yolcu gövde+baş noktaları): 4×4'te 18/18, 7×8'de 45/48, 9×9'da 75/78 yanlış.
Elenenler: CAM projeksiyon kaydırması (m02/m12) ve 9×9 kadraj tutarlı (ScreenPointToRay ile WorldToScreenPoint aynı matrisi kullanıyor; ekran görüntüsündeki noktalar nesnelerin üstüne düşüyor). UI Toolkit panel ↔ ekran dönüşümü (BlocksBoard, y ters) doğru: board üstündeki hiçbir test noktası UI tarafından engellenmedi. Simulator'da Screen = cihaz pikseli (1440×2560), tutarlı. Karo üst yüzeyi (y 0.05) ile y=0 farkı yalnız 0.02 hücre — sorun değil.

## Düzeltme
`BoardView.TryPickCell(ray, level, out cell)` (yalnız editör kullanır; oyunun B1 dokunuşu değişmedi): dokunulan noktada **görünen** nesne seçilir. Her yolcu iki kutu (geniş gövde 0–1.45, dar baş 1.45–2.15 hücre), tünel 1.0, engel 0.3 boyunda kutu; kameraya en yakın kutu kazanır (öndeki uzun yolcu arkadakini gerçekten örttüğü için). Kutuya değmezse karo üst yüzeyi düzleminden hücre (Void dahil). Collider yok, analitik ışın-kutu; yalnız tıklamada çalışır. Boylar BoardView Inspector'ında ("Level editörü seçimi").

## Tünel çıkışı kuralı
- Karar (sade): **yolcu asla kendiliğinden silinmez; çıkışı yolcuya bakacak işlem reddedilir.**
  - Çıkış karosuna yolcu boyama/bırakma: reddedilir, "Tunnel exit must stay empty."
  - Yeni tünel (karo döngüsü): çıkışı boş karoya bakan ilk yöne döner (Up → Right → Down → Left). Hiçbiri yoksa Up + uyarı.
  - Popup yön butonu: çıkışta yolcu varsa reddedilir ("… Move the passenger at (x, y) first.").
  - R ile döndürme: çıkışında yolcu olan yönler atlanır, mesajda söylenir.
- Görsel: çıkış karosunda void işaretinin quad'ı, düz yarı saydam yeşil, 0.8 ölçek (`tunnelExitTint`, `tunnelExitScale` LevelEditorPanel Inspector'ında; çok parlak gelirse alfa düşürülebilir).
- `LevelData.ValidateTunnels` (Save/Solve/Test Play engeller): dışarı bakıyor / delik / engel / başka tünel / çıkışta yolcu için ayrı, okunur hata. `IsTunnelExit(cell)` eklendi.
- `LevelAutoFill`: çıkışlar zaten boş bırakılıyordu; şimdi başta ValidateTunnels — eski levelde çıkış doluysa "No solvable layout" yerine nedeni söylenir.

## Test sonuçları
Hücre seçimi (her hücre: boş karoda merkez + 4 köşe, yolcuda gövde + baş, tünel/engel üstü). Turuncu = boş karonun o noktasını öndeki yolcu/tünel örtüyor, görünen nesne seçildi (doğru sayıldı).
| Görünüm | 4×4 (L03) | 7×8 (L21) | 9×9 (L27) |
|---|---|---|---|
| Sim Galaxy S7 1440×2560 | 47+2 / 0 yanlış | 168+40 / 0 | 224+52 / 0 |
| Telefon 1080×1920 | 47+2 / 0 | 168+40 / 0 | 224+52 / 0 |
| Uzun telefon 1080×2340 | 47+2 / 0 | 166+42 / 0 | 222+54 / 0 |
| Tablet 1536×2048 | 47+2 / 0 | 168+40 / 0 | 224+52 / 0 |
İlk denemede tek kutulu yolcu, 9×9'da tünel üstünü öndeki yolcunun kutusuyla seçti → baş dar kutuya ayrıldı, 0 yanlış.

Tünel (yeni 5×6, 3 dokunuşla tünel, `EditorBoardInput.ProcessClick` ile): tünel Up'a döndü; Red seçiliyken çıkışa dokunuş → reddedildi, mesaj; (3,3)'e yolcu → popup Right reddedildi; R → Right atlandı, Down; çıkışa doğrudan yolcu yazılınca Validate/Save "Cannot save: Tunnel exit must stay empty: …", asset yazılmadı; delik/engel/dışarı mesajları doğru. Auto Fill (8 yolcu) çıkışı boş bıraktı, Validate geçti. Level_03'te Auto Fill nedeni söyledi.

## Kurala uymayan mevcut leveller (değiştirilmedi — Asen'e)
Oyunda çalışıyorlar (tünel, çıkış boşalınca yolcu çıkarır) ama editörde açılınca Save/Solve/Test Play artık hata verir:
- Level_03 (4×4): tünel (3,3) Left → çıkış (2,3) yolcu
- Level_05 (5×5): tünel (2,4) Up → (2,3) yolcu
- Level_07 (7×6): tünel (3,5) Left → (2,5) yolcu
- Level_08 (7×6): tünel (1,4) Up → (1,3) yolcu
- Level_09 (7×7): tünel (5,5) Left → (4,5) yolcu
- Level_10 (7×7): tüneller (1,5) ve (5,5) Up → (1,4), (5,4) yolcu
Diğer 21 level (Level_26/27/28 dahil) uyuyor; dışarı/deliğe bakan tünel yok.

## Görüntüler (LE4-shots/)
- `pick_<görünüm>_<level>.jpg`: yeşil = doğru hücre, turuncu = öndeki nesne, kırmızı = yanlış (yok); büyük noktada seçilen hücre yazılı; alt şeritte eski yöntemin yanlış sayısı.
- `tunnel_exit_reddedildi.jpg`: yeşil çıkış işareti + "Tunnel exit must stay empty."
- `level03_autofill_mesaji.jpg`: eski levelde Auto Fill'in mesajı.

## Notlar
- Test aracı MCP `execute_code` (scratchpad `le4/`), geçici dosya projede yok. Kayıt: LevelIndex 15, Lives 2, Coins 400 ve LivesUtc anahtarları değişmedi; LevelDatabase.asset başlangıç kopyasıyla aynı (Level_28 satırı Asen'in, dokunulmadı); yeni level asset'i oluşmadı.
- Play sonrası MCP köprüsü donuyor; ALT + öne alma (P2) ile canlandı.

## Asen'in deneyeceği akış
1. LevelEditor → 7×8 ya da 9×9 bir level yükle; yolcuların gövdesine/başına dokun → popup doğru yolcuyu açıyor mu; renk seçip öndeki yolcunun hemen arkasındaki boş karoya dokun.
2. New → bir karoya 3 kez dokun (tünel) → çıkıştaki yeşil kare; renk seçip yeşil kareye dokun → reddedilmeli. Yanına yolcu koyup popup'tan o yöne çevirmeyi dene → reddedilmeli; R ile döndür → o yön atlanmalı.
3. Auto Fill → yeşil kare boş kalmalı; Save.
4. Level 3/5/7/8/9/10'u editörde aç → Save hatası; bu levellerin çıkışındaki yolcuyu taşımak isteyip istemediğine karar ver.

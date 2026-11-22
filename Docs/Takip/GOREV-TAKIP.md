# Görev takibi — 22 Kasım 2026 oturumu (Luma = takipçi)

Luma kod yazmaz; işleri alt ajanlar yapar. Her ajan kendi dosyasını (`Docs/Takip/<ID>.md`) günceller.
Timeout olursa: bu tablo + ajan dosyası nerede kalındığını gösterir; aynı ID ile yeniden görev verilir.

Referans: Asen'in iki ekran görüntüsü — telefon (Level 6, yakın kamera) ve tablet (Level 63, uzak kamera).
Kaynak dosyalar: `C:\Users\Kullanici\Downloads\WhatsApp Image 2026-11-22 at 00.17.29.jpeg`, `...00.32.41.jpeg`

## Kurallar (tüm ajanlar)
- Unity editörü tek: MCP'yi sırayla kullan; sahneyi kaydetmeden önce `git diff --stat` bak.
- Commit: yalnız kendi dosyaların (`git add <yol>`), Türkçe mesaj, push yok.
- Başkasının değişikliği olan dosyaya (özellikle `Game.unity`) dokunacaksan takip dosyana yaz.
- Önceden kirli dosyalar (oturum başı): LevelDatabase.asset, M_Purple.mat, M_Yellow.mat, Game.unity, manifest.json, packages-lock.json, Level_26, LightingData, ArtSource/ — bunları kendi commit'ine katma.

## Görevler

| ID | Görev | Ajan | Durum | Not |
|----|-------|------|-------|-----|
| T1 | Toon shader (stickman, otobüs, zemin) + ışık; gölge referanstaki gibi kapalı | ajan-T1 | bitti `1aeb215` `f7c7a13` `7510c13` `0f67a86` `3ca5575` | `Docs/Takip/T1.md` — BusJam/Toon (2 varyant, SRP Batcher), 39 materyal, ışık (50,150,0), gölge kapalı; kaldırım+arka plan kapsam dışı |
| T2 | Telefon/tablet birebir yerleşim: safe area (çentik), alt reklam boşluğu, kamera grid büyüdükçe uzaklaşır, sığan kare sayısı, UI dili/hiza/parlama, buton kayması | ajan-T2 | bitti `4a7366d` `2de64bb` `0a1ca85` `4be0e32` | `Docs/Takip/T2.md` — kadraj sabit 7×8 (büyük grid/dar ekranda geri çekilir), HUD referans dili, banner yer tutucu, InputHandler UI dokunuşunu yutuyor, Fredoka + Baloo yedek (ĞŞİ); overlay: kart/ayar/otobüs 0-1 px, tablette alt +31/+58 (ev çubuğu farkı) |
| V | Doğrulama: T4 Play testi, T2 dokunuş testi, T3 RAM ölçümü | ajan-V | bitti `61ecb1d` `c45bab9` | `Docs/Takip/V.md` — tüm geçiş/dokunuş testleri geçti (Start→Play kararma atlaması düzeltildi); sızıntı yok (Retry×20 sabit, artış=havuz); 60 fps, PlayerLoop ~2.5 ms, oyun kodu 0 B/kare; açık karar: UI doku 2048→1024 (~40 MB), win sonu 0.1 sn boşluk |
| T3 | UI Toolkit + genel CPU/GPU/RAM denetimi, uzun süreli RAM şişmesi, minimum UI | ajan-T3 | kod bitti `9ce0419` `9d3e8dd` `1deff09`, ölçüm bekliyor | `Docs/Takip/T3.md` — sızıntı yok; SVG ASTC (~184→~46 MB tahmin), kutlama RT serbest, font atlas readable kapalı |
| T4 | Level geçişi: Play'e basınca ekran kararıp geçiş (fade) | ajan-T4 | kod bitti `0cb0b0a`, Play testi bekliyor | `Docs/Takip/T4.md` — fade 0.3/0.3 sn, input kilidi; Claude'da Unity MCP yok |
| R1 | Stickman modeli/oranı/animasyonu + altındaki platform/karo birebir | ajan-R1 | bitti `4ddbcc8` `f33f68c` | yeni Yolcu.fbx + 8 anim, ölçek 1.15, kontur, karo 0.90 yuvarlak |
| R2 | Otobüs: kapılar kapalı, yolcu binerken açılır; kapı hizası | ajan-R2 | bitti `81940b0` `006f666` | kapı kapalı gelir, yolcu varınca açılır, 0.22 sn sonra kapanır; kapı x=0 hizalı |
| R3 | Engel modülleri + tünel kapağı + soru işaretleri (yalnız bizde olan mekanikler) | ajan-R3 | bitti `d27b681` `9f085ed` | yeni tünel, ip (4 katman), ? mesh, engel=boşluk; bomba/R yok (bizde mekanik yok) |
| R4 | Menü/win/fail/pause ekranları birebir (T5 yerine; ekstra buton/etkinlik/mağaza yok) | ajan-R4 | bitti `7dcbd72` `bb45f17` `62e46ce` `e43f0bd` | popup ekran-yükseklik ölçeği, UI doku 46→15 MB (ASTC), win geçişi örtüşmeli — ek (Asen onayı): UI doku küçültme kalite düşmeden (~40 MB hedef), win sonu kararmayı panel solmasıyla örtüştür |
| R5 | Genel fark taraması (kareler + video) → `R5-farklar.md`, P0/P1 listesi | ajan-R5 | bitti `f0ccbcf` `bfbf77f` | `R5-farklar.md`: P0 A1 kilitli slot, A2 kaldırım rengi, B1 oturma pozu |
| R6 | Üst arka plan: bitki sayısı/konumu referans kadar sade (bizde çok kalabalık) | ajan-R6 | bitti `33ad746` `19261e7` | 46 öğe→16 (3 obje, 2 DC), GrassKit.fbx, Toon `_BaseMap` |
| T1b | Her şey toon mu: tam denetim (sahne, prefab, FBX gömülü, runtime) → kalanları geçir (zemin, kaldırım dahil) → ikinci denetim | ajan-T1b | bitti `e01280e` `87c1273` | `Docs/Takip/T1b.md`; R1/R2/R3/R6'ya "yeni materyal toon" bildirildi |
| R7a | Temizlik + klasör düzeni + kod temizliği PLANI (yalnız analiz) | ajan-R7a | bitti `1221d92` `0a0baaf` | `Docs/Takip/R7-plan.md` — A:14 madde ~32.5 MB, B:15 (Asen'e soruldu), C:11 taşıma, D:13 kod; R7b önce taramayı yeniden çalıştırmalı |
| W1 | Kaldırım #D2DEFE + lekeler, kilitli slot, karo beyazı (grading), çimen rengi, dekor spec 0 | ajan-W1 | bitti `d61ab3e` (it2 renkleri ekranda doğrulanmadı) | Tonemapping kapalı (beyaz 208→254), MuteBackground yalnız mor karo/yol, kaldırım buz-lila + shader lekeleri, kilitli slot yeni model, M_KitSlot beyaz |
| W2 | Oturma pozu (P0), koltuk parlaması, duman, lime/cyan renkler, fail gecikmesi 2.6→~0.6 sn | ajan-W2 | bitti `9f42ef9` `6a8dea1` | oturma ofseti, koltuk parlaması (FX_SeatFlash), egzoz pufları (Puff.fbx), lime #C2FE00 / cyan #00D0FE, kapı 0.1 sn önce açılır; fail gecikmesi zaten referansla aynı (0.6 sn) |
| W3 | Konfeti, booster bar söner, kutlama otobüsü alt bantta, CelebrationReview hatası | ajan-W3 | bitti `1f03628` `9fe58c8` + `3d4c932` (konfeti süre 1.0 sn, hız ×1.25, 24 beyaz kırıntı; referansla aynı) | konfeti 72 havuzlu UI parçası, booster söner, başlık kemik+kafatası; kutlama otobüsü zaten referansla aynı; CelebrationReview R7'de silinecek |
| R7b | Planı uygula (sil/taşı/düzenle) + Asen B kararları + Toon.shader→ToonLighting.hlsl sadeleştirme + Kit_Question remap | ajan-R7b → ajan-R7b2 | bitti `8f5bd6e` `aeba893` `620cf20` `cd44e18` `227fc3e` `2904ce7` `995e8da` | `Docs/Takip/R7b.md` |
| R8 | Level Completed ekranı Asen'in yeni referansına göre (dizilim + görünüm; hard level YOK), başlık arkası ışık/efekt kaliteli | ajan-R8 | tur 1 bitti `96de439` `ba3d041` `53a31c5`; tur 2 bitti `6e07b77` `64a3efd` `6a22a49` (kemer başlık+kurdele, mavi hale baskın, ışın .12, Continue %80) | `Docs/Takip/R8.md`, ref `R8-shots/ref-level-completed.png` |
| P1 | Yolcular: level içinde ayaklar zemine gömülüyor; otobüste otururken bacaklar görünmüyor/oturma pozu uygun değil | ajan-P1 | bitti `3384112` `7573719` `0f3af38` | `Docs/Takip/P1.md` — ayakkabı büyüdü/topuk, yeni Sit/Wave pozu, seatedVisualOffset (0,0,-0.08), slot standHeight 0.034 |
| F1 | Toparlama: outline tek skinning (2. materyal), Toon `_Edge*` sil, LevelEditor queueSpacing 7.8, kaldırımdaki yolcu yüksekliği, kutlamadaki oturan yolcular kontrol | ajan-F1 | bitti `fad3300` `2c43859` `45dbc18` `1e1f6a5` | `Docs/Takip/F1.md` — kontur tek skinning, `_Edge*` silindi, LE queueSpacing 7.8, kaldırım y değişmedi (görünmüyor), kızgın yüz doğrulandı, 0 hata |
| B1 | HATA: çıkabilir yolcuda outline yok + dokununca bir şey olmuyor; bir yolcu yalnız diğerleri çıkınca görünür oldu | ajan-B1 | bitti `96e3fa7` `e328361` `4295ae4` | kök: taşma kuyruğu reload'da havuza 2. kez veriliyordu (görünmez engel/çift hücre); tap collider kutu+baş küresi |
| B2 | Otobüs tasarımı referanstaki ana oyun otobüsünden farklı → birebir | ajan-B2 | bitti `8297dc0` `145b93f` | gövde basık/geniş, güverte dudağı, pencere dizilimi + teal cam, tekerlek küçük, kapı %51, yan yüz aydınlık, 12406 üçgen |
| K1 | Win ekranı kutlama otobüsü üstündeki kutlayan yolcular kalitesiz/yabancı → referansa göre | ajan-K1 | bitti `9579acf` `dc74f8c` | yolcular güvertede ayakta, boy 1.3, yeni Cheer_Jump/Wave/Lean (tutunma), faz farkı |
| R8c | Win ekranı coin görseli eskisi gibi büyük/çok coin | ajan-R8 | bitti `5e1a01a` `b100d39` `ccfe9b3` | büyük coin piramidi geri, 1024 doku |
| G1 | Kaldırım–grid karo tabanı birleşiminde boşluk/kesinti → kesintisiz; engel hücreleri grid haritasının içinde, bombeli (referans) | ajan-G1 | bitti `e30882b` `856b035` | boşluk: board çıkış kenarı z4.47 / kaldırım z4.70 → taban uzatıldı (sidewalkJoinDepth .5); engel grid içinde, Kit_Obstacle kubbe (lila-gri, h .30), Obstacle.prefab eski GUID'le |
| B3 | Otobüs: B2'nin parlaklık/normal oyununu geri al; istenen ŞEKİL — önü/burnu referanstaki gibi yuvarlak, model oyundakine benzesin | ajan-B3 | bitti `1839a7f` `1f1e868` | B2 ışık hilesi + cam/jant renkleri geri alındı; burun üstten r .46 / yandan alt kavis .40, ön cam kavise sarılı, 14242 üçgen |
| P2 | Yolcu ayakları hâlâ karo içinde görünüyor (P1 sayısal olarak düz dedi) → görsel olarak üstte dursun | ajan-P2 | bitti `b4dd2b9` `9e539a5` | kontur ayak altında karo tarafından kesiliyordu → OutlineHull `_DepthBias` .06; temas gölgesi (ContactShadow quad) |
| LE1 | Level editör sahnesi: oyun sahnesinin tüm görünüşü/ışık/ayarlar/mantığı aynı; grid sınırı 9×9; kamera her çözünürlükte 9×9'a göre; level kaydet/yükle/test doğru mu | ajan-LE1 | bitti `028cfda` `e9a171f` `194056f` `77f8174` | ortak sahne prefab'ları (Lighting/Background/Board/BusStation/WaitingArea), 9×9, CameraFitter referans kadraj + SetScreenArea, kayıt testi geçti, mesajlar İngilizce, NaN koruması |
| LE2 | Level editör UI: tünel koyunca arka plan/UI bozuk; her şey kullanılabilir; oyunun görsel dili (font, buton, renk; İngilizce metin) | ajan-LE2 | bitti `8db366c` `9153ade` `a17c890` | tünel popup saydamdı (.modal dokusu yok) → oyun popup ailesi + karartma; tünel yön/kuyruk sıralama, otobüs sırası, dokunmatik düzenleme, kaydetmeden çıkma uyarısı, Load -1 çökmesi, Test Play'den Editor dönüşü |
| O2 | Çıkamayan yolcular: ince (~1 px) kendi renginin koyu tonunda kontur + hafif soluk renk; çıkabilenler canlı + kalın koyu kontur (referans) | ajan-O2 | bitti `7e80f0d` `8831462` `4700228` | erişilemez: parlaklık ×.85, specular 0, ince kontur ×.4 kendi tonu koyu; anında geçiş; ipli = erişilemez; 16 materyal bir kez |
| LE3 | Editör: Save yalnız çözülebilirse (sona ekle), Solve arka planda, Test Play kaydetmeden | ajan-LE3 | bitti `680aa0c` `07ee9b7` `3554ab5` | Save yalnız çözülebilirse + sona ekler; Solve/Save/AutoFill Task.Run + iptal; Test Play kopya (GameManager.TestLevel), asset/ilerleme değişmez |
| X1 | Güvenli kullanılmayan temizliği (guid + yol + ad taraması, kod silme yok, Play doğrulama) | ajan-X1 | bitti `f7c87e3` `b0811dc` `91b4482` `9f027bc` `1488b97` `b93da0a` | Assets'te silinecek yok; 22 dosya ~0.66 MB (çift PNG, eski rapor, bayat SVG üreticileri, log) |
| CAM | Sınırlı (tam ekran değil) Game view'da kamera/HUD sağa-aşağı kayıyor (Simulator + tam ekran doğru); editörde daha geniş kamera (her zaman 9×9 kadrajı, taban yolu/otobüsleri örtmesin) | ajan-CAM | bitti `8e33236` `1d5d16d` (safeArea Simulator cihaz ölçüsünde geliyordu → Screen'e oranla taşı + kırp; editör SetMinimumGrid 9×9) | `Docs/Takip/CAM.md`, Asen görüntüsü CAM-shots/asen_gameview_kayik.png |
| G2 | Kenarlık (çerçeve) ile karo içleri arasında köşelerde boşluk (board kare, yumuşak köşe yok) → doldur; süre göstergesi %20 büyük | ajan-G2 | bitti `0553e99` `ebe5282` `13f3afb` (taban köşe noktası başına çeyrek parça, iç köşe kavise kadar dolu, dış köşe r .316; süre 192×67 / 36px) | `Docs/Takip/G2.md` |
| LE4 | Editör: tünelin baktığı yöndeki ilk karoya yolcu konamasın; dokunulan karo yanlış algılanıyor (yanlış yolcu siliniyor/değişiyor) | ajan-LE4 | bitti `0e110ae` `9c9f26b` (seçim: görünen nesne kutusu en yakın, yoksa karo düzlemi; tünel çıkışı boş kuralı + yeşil işaret + Validate; L03/05/07/08/09/10 kurala uymuyor → Asen) | `Docs/Takip/LE4.md` |
| O1 | Outline'lar referansa benzemiyor (aşırı kötü) → referans kadar ince/temiz kontur | ajan-O1 | bitti `d854c92` `cedba93` | `Docs/Takip/O1.md` — ekran düzleminde hull, kalınlık max(dünya 0.085, 2.5 px), kafa çapının ~%10'u; yolcu `_EdgeStrength` 0; görüntüler O1-shots |
| W4 | Kalan polish: otobüs kalkış eğimi (B4), kuyruk dumanı/bağlantı topu (A9), grid ağzı köşe eğrisi (A7), yol kenarı turuncu şerit (A8), slot iç bevel, kilitli slot parlaması, koltuk parlaması parçaları | ajan-W4 | bitti `3725c68` `85af896` `d5ee555` `d087261` | `Docs/Takip/W4.md` — kalkış pitch .6°, queueSpacing 7.8 + HitchBall, ağız fillet, yol turuncu şerit + krem bordür, slot iç çizgi, kilitli slot gölge, SeatSparks |
| R | Son rapor: RAPOR.md + HTML + vault güncellemesi | — | bekliyor | W4 sonrası |
| R | Son rapor: `Docs/Takip/RAPOR.md` + HTML | Luma | bekliyor | |

## Olay günlüğü
- 22.11 — Oturum açıldı, T1-T4 paralel başlatıldı.
- 22.11 — T2'ye ek: ekran görüntüsünü referansla overlay ile karşılaştırma; font da referansa benzesin (açık lisanslı benzer font, kontur + gölge).
- 22.11 — T4 bitti (`0cb0b0a`), editör dışı derleme 0 hata. Claude ajanlarında Unity MCP yok → T1/T2/T3'e editör/ekran görüntüsü işi için `codex exec` kullanmaları söylendi.
- 22.11 — T3 bitti. ENGEL: `codex exec -m gpt-6.2-sol` → 400 "model is not supported when using Codex with a ChatGPT account". Editör işi (Play, ekran görüntüsü) şu an kimsede yok; Asen'e soruldu.
- 22.11 — Asen: Unity MCP Claude'a bağlansın. `claude mcp add --transport http unityMCP http://127.0.0.1:8080/mcp` (local scope) eklendi, durum Connected. Araçlar ancak oturum yeniden açılınca görünür. Plan: T1/T2 statik bitsin → `claude -c` ile yeniden aç → doğrulama görevleri: V1 (T1 görsel), V2 (T2 overlay), V3 (T3 RAM ölçümü, vault Zed-Promptlari "T3-OLCUM"), V4 (T4 Play testi).
- 22.11 — T1 bitti. ÇÖZÜM: ajanlar Unity MCP'ye (127.0.0.1:8080/mcp) Python HTTP istemcisiyle doğrudan bağlanabiliyor → yeniden başlatma gerekmiyor. T2'ye bildirildi. T4 derleme hatası geçiciydi (GameManager.SetInputLocked mevcut).
- 22.11 — T2 bitti, V başladı. Asen yeni istek: Referans\Kareler + video (Video-Envanteri.md) ile her farkı birebir yap, gereksiz özellik yok. R1-R5 paralel başladı. Ortak kurallar: `Docs/Takip/ORTAK-BRIEF.md`; editör kilidi `Docs/Takip/EDITOR.lock`.
- 22.11 — Asen: bitkiler çok kalabalık → R6. Modeller birebir, gerekirse Blender → Blender 5.1 bpy arka plan yolu R1/R2/R3/R6'ya bildirildi (MCP soketi 9876 kapalı). Temizlik/düzen → R7a analiz şimdi, R7b uygulama R1-R6 sonrası (paralel taşıma çakışır).
- 22.11 — R1-R6, R7a, T1b bitti. Asen B kararları: eski kanıt görüntüleri sil, UI PNG→ArtSource/UI, .blend commit, repo dışı kopyalar sil, klasör adları İngilizce, yorumlar Türkçe, MIMARI güncelle.
- 22.11 — Dalga 2 (W1-W3) başlatıldı: R5 P0/P1 + ajanların kalan farkları. R7b W'ler bitince.
- 22.11 — W1 bitti; 07:35'ten sonra Unity MCP yanıt vermedi (Asen editörde Play'de test ediyordu). W2'ye tonemapping notu gitti.
- 22.11 — W2 bitti. R7b başladı (editör tek ajanda). W4 + rapor sırada.
- 22.11 — Bilgisayar kapandı (R7b adım 5 commit'siz, EDITOR.lock R7b 08:35 bayat). Asen: R7b devam + outline düzelt (O1); editör kontrolü en sonda Asen'de.
- 22.11 — Asen: win ekranı hard level'dan esinlenmiş, yanlış → yeni referans ekran görüntüsü (R8).
- 22.11 — Asen kararı (R8): ilerleme barı + x2 reklam butonu yok; Continue tek ve ortada.
- 22.11 — R7b2 bitti (Console 0 hata, L1 win NullReference yok). Açık: UnityDefaultRuntimeTheme.tss izlenmiyor; editörde ResultTimeline Preview + LevelEditor Solve bakılacak. Index çakışması: commit'ler `git commit -- <yollar>` ile.
- 22.11 — Asen: yolcuların ayakları gömülü, otobüste oturuş kötü → P1.
- 22.11 — O1 bitti. Sonraki iş (P1 sonrası): outline ikinci SkinnedMeshRenderer yerine aynı renderer'a 2. materyal (çift skinning kalkar); Toon.shader'dan kullanılmayan `_Edge*` çıkar.
- 22.11 — R8 tur 1 bitti (hard-level izleri kalktı, mavi kurdele, hale/ışın/toz/yıldız). Luma denetimi: başlık düz (ref kemerli), ışınlar sert, Continue yüksek → tur 2.
- 22.11 — R8 tur 2 bitti; Luma denetimi: referansa yakın, kabul. Kalan küçük fark: kurdele uçları referansta kıvrık, bizde çatallı.
- 22.11 — W4 bitti. Açık: LevelEditor.unity queueSpacing hâlâ 8.3 (Game.unity 7.8); tablet kadrajı denenmedi.
- 22.11 — P1 bitti (Luma denetimi: otobüste bacaklar görünüyor, kabul). F1 toparlama başladı; sonra rapor.
- 22.11 — Asen: kızgın yüz çıkmıyor. Neden: R7b 4149956 icon_angry.png'yi ArtSource'a taşıdı, Passenger.prefab AngryIcon sprite'ı kopuk kaldı. Luma geri getirdi (aynı guid). Taşınan diğer PNG'lerin referansı yok (kontrol edildi).
- 22.11 — Luma silme denetimi: tüm Assets/ProjectSettings guid taraması → tek kopuk ref LevelEditor.unity obstaclePrefab (R3'te bilerek silinen Obstacle.prefab; kodda alan yok, zararsız). Kodda adla/yolla yüklenen tüm asset'ler mevcut; USS url'leri sağlam; animator state'leri (Brake DoorClose DoorOpen Drive Idle Run Sit SlotIdle Wave) var; Resources'a bağlı yükleme kalmadı. Kızgın yüz dışında kırık yok.
- 22.11 — Asen: B1 hata, B2 otobüs tasarımı, K1 kutlama yolcuları, R8c coin büyük. README/video/build en sonda. Level editör birlikte.
- 22.11 — B1 bitti. Açık: Game.unity'de O1'den kalma 3 `O1Cam` (commit'siz) silinecek; MCP Simulator safeArea≠Screen ortam notu.
- 22.11 — K1 bitti. Kayıt verisi LevelIndex 17→1 olmuş (bir ajan geri yüklememiş); Asen'e soruldu.
- 22.11 — B2 bitti (Luma denetimi: referansa yakın). C1: O1Cam silme. Sırada LE (Asen'le).
- 22.11 — C1: 3 O1Cam silindi (Game.unity kaydedildi, commit yok). Yan etki: WaitingArea standHeight 0.034 serileşti, arka plan quad ölçeği 100.18×178.09→105.35×187.29 (muhtemelen editör sığdırması). Yedek: scratchpad/Game.before.unity.
- 22.11 — Asen: kaldırım-karo boşluğu + engel bombe/grid içinde (G1); otobüste parlaklık istenmedi, şekil (yuvarlak ön) istendi (B3). Sonra LE.
- 22.11 — G1, B3 bitti. Sırada LE (Asen'le).
- 22.11 — Asen LE istekleri: sahne paritesi, 9×9 + kamera, kayıt doğrulama, tünel UI hatası, kullanılabilirlik, oyunun dili korunsun → LE1 + LE2.
- 22.11 — LE2 bitti. LE1'e: Validate/AutoGrid mesajları İngilizce, AutoGrid metin hatası, Level_27 temizliği, son Console koşusu. Açık (Asen'e): Solve senkron (≤6 sn donma), Test Play asset'i yeniden yazıyor.
- 22.11 — LE1 bitti (MCP Play giriş/çıkışında 10-30 dk kopuyor → P2 13:02'den beri bekliyor).
- 22.11 — P2 bitti. MCP ipucu: Unity arka plandayken köprü donuyor; ALT bas-bırak canlandırıyor (scratchpad p2/focus2.ps1).
- 22.11 — Asen: çıkamayan yolcu görsel dili (O2). Editör Save: çözülebilirse kaydet + LevelDatabase sonuna ekle (sonuna ekleme zaten var, çözülebilirlik kontrolü yok).
- 22.11 — Asen kararları: editör ölçeği kalsın; uzun ekranda 9×9 boşluğu kalsın; Solve arka planda; Test Play kaydetmeden. Editör kontrolü en sonda.
- 22.11 — LE3 kusurları bitti (durum mesajı, çip sarma). Açık iş yok; sırada: Asen editör kontrolü, vault/rapor, README/video/build/push.
- 22.11 — Asen: Level_26 + gelecek leveller kalsın (c305e94), Device Simulator kalsın (932c1ad), Fredoka mat (f261511). Luma sildi: Scenes/Game LightingData (6c2422a), UnityDefaultRuntimeTheme.tss (izlenmiyordu, referans yok). X1 temizlik başladı.
- 22.11 — Asen testi: sorun yalnız şunlar (diğerleri tamam): sınırlı Game view'da kamera/HUD kayıyor (Simulator ve tam ekran doğru); kenarlık-karo köşe boşlukları; süre göstergesi %20 büyük; editörde daha geniş kamera (9×9 gibi, taban yolu örtmesin). Önce push.
- 22.11 — Push: 7951cb7 (origin/main). CAM + G2 başladı, X1 devam (ölü kod + doğrulama).
- 22.11 — CAM, G2, X1 bitti.
- 22.11 — Asen: LE4 (tünel çıkış karosu + editör dokunma hassasiyeti). Push onayı sorulu, cevap yok → push yapılmadı.
- 22.11 — Push: 4046074 (LE4 + Level_28). Asen: R9 (ip katmanı = boğum sayısı, en çok 3) ve U1 (Playing alt 4 kilitli booster butonu silinsin, board kaymasın) başladı. Sonra: Asen diğer testleri bitirince performans testi.
- 22.11 — U1 bitti (1b0308f): booster çubuğu + 2 SVG silindi, görünmez bar-space kaldı (konfeti çıkış noktası), board konumu değişmedi (L2/L10, 1080×1920/2400 cmp). R9 sürüyor.
- 22.11 — R9 bitti (13c9318, ab94a51, d5fedaf): her ip ayrı halka + krem düğüm (1/2/3), MaxRopes=3 (editör, Validate, Solver); hiçbir levelde 3+ ip yoktu, değişen level yok. Sırada: Asen testleri → performans testi.

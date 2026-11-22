# R7b — Temizlik planını uygula (22 Kasım 2026)

Plan: `Docs/Takip/R7-plan.md`. Asen onayları: A (doğrulananlar silinir), B (görüntüler sil, Art/UI→ArtSource/UI, .blend commit, repo dışı kopyalar), C (İngilizce adlar), D (kod), yorumlar Türkçe, MIMARI güncel.

## Durum
- [x] 0. Brief/plan okundu, editör boşta (Play kapalı), tarama yeniden çalıştırıldı
- [x] 1. Önce görüntüleri (Docs/Takip/R7b-shots/before: menu, L1, L6, L18 Playing + L1 bitirilip result)
- [x] 2. A: inceleme/yakalama araçları + ZScn üreticileri (tek parti) — e66acdd
- [x] 3. A: kullanılmayan asset'ler — efa4a8f (afterA: fark gürültü seviyesinde)
- [x] 4. C: taşıma/yeniden adlandırma — 9bd7f27 (afterC: aynı)
- [x] 5. D: kod temizliği (R7b2) — Toon.shader→ToonLighting 8f5bd6e (**Toon.shader serbest**, O1 dokunabilir); D1+C7 CelebrationSettings Inspector/Settings aeba893; D2/D3 namespace + GrassLayoutBuilder 620cf20; D4 kullanılmayan üyeler cd44e18; D6 Update temizliği 227fc3e. Atlanan: D3 `AntialiasedSvgImporter` (SVG .meta'larda importer override tip hash'i, ad değişirse override kopar), D8 (`Kill()` DOTween uzantısı, using gerekli — R7a yanılmış), D11 (düşük öncelik).
- [x] 6. Yorumlar Türkçe — 995e8da (29 dosya, 147 satır, yalnız yorum; kod satırları HEAD ile birebir doğrulandı). Hariç (Passenger.cs, LevelAutoGrid.cs hariç: O1; ToolkitGameUI*, ResultTimeline*, CelebrationSettings hariç: R8)

**UI dosyaları serbest** (R7b2, 10:33): ToolkitGameUI.cs, ToolkitGameUI.Results.cs ve diğer partial'lar, ResultTimeline.cs, ResultTimelineEditor/Preview.cs, CelebrationSettings(.cs/.asset) — R7b2 işi commit'li (aeba893, 620cf20, 227fc3e), bundan sonra R7b2 dokunmaz.
- [x] 7. B: görüntüler (5b257bf), Art/UI→ArtSource/UI (4149956), ArtSource commit + .gitignore (68abfe2), repo dışı kopyalar silindi (kilitli 5 MB kaldı)
- [x] 8. MIMARI.md güncel — 2904ce7
- [x] 9. Son doğrulama (R7b2 10:20, kod commit'leri sonrası; yorum commit'i yalnız yorum) — R7b-shots/final/ + sheet_before_final.png: Console 0 hata, L1 6 dokunuşla Complete, sonuç ekranı NullReference yok. Fark afterC→final: menü %26.7 (kayan dekor + kayıt farkı: can 2, Level 17, 475 coin), L1/L6/L18 %1.0-1.2 (sayaç/idle + O1 outline işi), result %0.08. Kayıt geri yüklendi (LevelIndex=16, Coins=475; can dokunulmadı).

## Yeniden tarama (refscan/deadcode, çalışma kopyası)
Plana göre değişenler:
- `M_Obstacle.mat` artık ulaşılabilir (R3) → silinmez. `M_Outline.mat` hâlâ kullanılmıyor.
- Yeni kullanılmayan: `Art/Models/Environment/Generated/ApprovedEnvironment.asset` (3,8 MB; R6 Grass Layout'a geçti, yalnız ZScnBackground üretiyordu).
- `ScrollingWater.shader` yalnız `ZScnImport.cs` içinde adla geçiyor (M_Water artık Toon) → ZScnImport ile birlikte gider. `BackgroundSimpleLit.shader` hiçbir yerde yok.
- `GrassKit.fbx` yalnız `R6GrassLayout.cs` (editör üreticisi, repo içi yollar) kullanıyor → kaynak olarak kalır.
- Yeni editör scriptleri: `R4UiCapture.cs` (yakalama → silinir), `R6GrassLayout.cs` (üretici, kendi kendine yeterli → kalır, adı `GrassLayoutBuilder`).
- ZScnImport/ZScnBackground/ZScnMenu girdilerini `BusJamCase_ZVEC_Review/scn/...` klasöründen okuyor; o klasör silineceği için araçlar çalışamaz → silinir (Asen: "araç gereksizse sil"). `Tools/zscn_*` de gider.

## Oturum başından kirli dosyalar (dokunulmadı, Asen'e sorulacak)
- `Packages/manifest.json` + `packages-lock.json`: `com.unity.device-simulator.devices 1.0.1` eklenmiş (Device Simulator cihaz paketi; biri simülatörde cihaz listesi için kurmuş).
- `Data/LevelDatabase.asset`: listeye `Level_26` (guid dcd755cc…) eklenmiş. `Data/Levels/Level_26.asset` (+meta) izlenmiyor: 6×5, süre 60 sn — editörde kaydedilmiş yeni bir level.
- `Scenes/Game/LightingData.asset` (+meta): bake çıktısı, sahne bağlamıyor (`m_LightingDataAsset: 0`).
- `Art/Fonts/Fredoka-Bold SDF Plain.mat`: `_ScaleRatioB/C` 0.758→0.618 (TMP'nin otomatik yeniden hesabı; dış çizgi/gölge oranı).
- `Scenes/Game.unity` (başkasına ait satırlar): `obstaclePrefab` satırı düştü (kodda alan yok), Directional Light yoğunluğu 0.5→0.85, oyun kamerası ve menü kamera konumu, arka plan quad ölçeği, ToolkitGameUI `resultTimeline` referansı + T4 fade alanları, SceneRoots'a `Menu Lens Volume`.

## Commit'ler
- e66acdd İnceleme/yakalama editör araçları + ZScn üreticileri + Tools kanıt scriptleri silindi; ResultTimeline eski alanlar, SVG importer ölü dallar
- efa4a8f Kullanılmayan asset'ler (76 dosya silme satırı)
- 9bd7f27 İngilizce adlar / shader'lar tek klasör / LevelEditor klasörü / GameVolumeProfile
- 4149956 Art/UI 31 PNG → ArtSource/UI (Tools yolları)
- 68abfe2 ArtSource .blend + paletler commit, yedekler silindi, .gitignore
- 5b257bf Eski kanıt görüntüleri (Docs/*-Captures, Images, kök PNG)

## Notlar / kararlar
- Doğrulama yöntemi: scratchpad `r7b/shots.sh` (Play, GameView 1080×1920, ApplyViewport, L1/L6/L18 Playing ScreenCapture) + `playlevel.sh` (L1 dokunuşla bitir, sonuç ekranı), `cmp.py` (piksel farkı >24). Gürültü tabanı: menü ~%30 (kayan dekor), level %0.1-0.8 (idle animasyon/sayaç). Kayıt (LevelIndex=17, Coins=425) her testten sonra geri yüklenir.
- Derleme: `r7b/build.py` (Bee rsp + Unity Roslyn; Editor derlemesi yeni Assembly-CSharp ref'ine bağlanır).
- Passenger.fbx: Yolcu→Passenger/M_Yolcu→M_Passenger adlandırmasında legacy materialLocation(0) yanına boş `Materials/M_Yolcu.mat` üretti → materialLocation=InPrefab (remap M_Passenger), üretilen materyal silindi.
- Kit_Question/Kit_Rope_1..4 (ve Puff): materialImportMode=None → gömülü materyal yok, prefab toon materyali (M_Rope, M_QuestionFill/Line) kullanıyor; remap gerekmez (doğrulandı).
- Repo dışı kopyalar: "BusJamCase - Kopya" eski bir anlık görüntü (HEAD fefcd04, main'in atası; kirli olanlar eski M_Purple/M_Yellow Simple Lit ayarı, Level_26 (main'deki ile aynı), eski .blend'ler) — önemli iş yok. ZVEC/UITK_Review yalnız kanıt/inceleme (png/json/inceleme .cs/py). Silindi (~8,8 GB). Kalan: "BusJamCase - Kopya" boş klasör ve `BusJamCase_ZVEC_Review/celeb4/editor-*.log` (5 MB) bir süreç tarafından kilitli (muhtemelen açık Unity editörünün -logFile'ı) → editör kapanınca elle silinmeli.
- B4 (Z* raporlarını Docs/Arsiv'e taşı) Asen onay listesinde yok → yapılmadı.

## R7b2 notları (22 Kasım, 10:12-10:45)
- Bayat `EDITOR.lock` (R7b 08:35) silindi. Derleme MCP `refresh_unity` ile (scratchpad `mcp.py`), Console 0 hata; Toon shader hatasız.
- İki kez `git commit` index'teki başka işleri süpürdü (önce eski R7b staged parçaları, sonra R8'in staged `ToolkitGameUI.Results.cs` + `ribbon_purple→ribbon_blue`); ikisi de push'suz `reset --soft` ile geri alındı, R8'in index'i aynen duruyor. Ders: commit hep `git commit -- <yollar>` ile.
- Dokunulmayan kirli dosyalar: listede olanlar + `Assets/UnityDefaultRuntimeTheme.tss` (izlenmiyor; Unity'nin PanelSettings tema yokken ürettiği dosya, sahibi belirsiz), O1'in materyal/OutlineHull değişiklikleri, `AntialiasedSvgImporter.cs` (başka ajanın `ribbon_blue`/`coin_pile` değişikliği; İngilizce 8 yorum kaldı).
- Yorum commit'inden sonra ayrıca derlenmedi (yalnız yorum; editör kilidi O1'deydi).

# R7 — Temizlik ve düzen planı (R7a analiz, 22 Kasım 2026)

R7a yalnız analiz yaptı: bu dosya dışında hiçbir dosya silinmedi, taşınmadı, düzenlenmedi. Uygulamayı R7b yapacak, R1-R6 bittikten sonra. **R7b ilk iş olarak taramayı yeniden çalıştırmalı** çünkü diğer ajanlar bu sırada dosya ekliyor (ör. `GrassKit.fbx`, `R4UiCapture.cs`, `Level_26.asset`).

## Yöntem

- `refscan.py` (scratchpad, `py -3 -I`): `Assets/` altındaki her `.meta` dosyasından GUID okundu. Referans grafiği şu kaynaklardan kuruldu: `.unity/.prefab/.mat/.asset/.controller/.anim/.spriteatlas`, `.meta` dosyalarının kendisi (FBX materyal remap'leri), UXML/USS/TSS içindeki `url()`/`src` göreli yolları, shader `#include`'ları, kodda geçen `Assets/...` yolları ve `Resources.Load` adları, `ProjectSettings/` ve `Packages/`.
- Kökler: Build Settings'teki iki sahne (`Game.unity`, `LevelEditor.unity`), ProjectSettings, `Resources/` klasörleri ve kodda yolla geçen asset'ler. Ulaşılabilirlik dolaylı da hesaplandı: bir asset yalnız ulaşılamayan bir asset'ten referans alıyorsa kullanılmıyor sayıldı.
- `deadcode.py`: hiç çağrılmayan metotlar, alanlar ve tipler aranırken sahne, prefab ve asset'lerdeki `m_MethodName` ile script GUID'leri de hesaba katıldı. Ardından her bulgu elle `grep` ile doğrulandı.
- Tarama `Game.unity`'nin çalışma kopyasıyla yapıldı (diğer ajanların henüz commit'lenmemiş değişiklikleri dahil).

---

## (A) Güvenle silinebilir — referans yok

Unity'de silerken her zaman **asset ile `.meta` dosyası birlikte** silinir. Tercihen editörde `AssetDatabase.DeleteAsset` ile (MCP üzerinden, EDITOR.lock alınarak) yapılır.

| # | Yol | Boyut | Kanıt / gerekçe |
|---|---|---|---|
| A1 | `Assets/_BusJam/Art/Atlases/UI_Icons.spriteatlas`, `UI_Panels.spriteatlas` (klasörle birlikte) | 12 KB | Hiçbir sahne, prefab, kod ya da ayar bu GUID'lere referans vermiyor. Eski uGUI dönemi atlasları; UI artık UI Toolkit ve SVG ile çiziliyor. |
| A2 | `Assets/_BusJam/Art/Textures/bg_game.png`, `bg_menu.png` | 4,36 MB | GUID referansı yok, adları ne kodda ne Tools'ta geçiyor. Kullanılanlar `GameBG.png` ve `MenuSky.png`. |
| A3 | `Assets/_BusJam/Art/Fonts/LilitaOne SDF.asset` | 8,49 MB | Hiçbir yerden referans almıyor. Kendisi Fredoka SDF'yi fallback olarak gösteriyor ama tersi yok. TMP'nin varsayılan fontu `Fredoka-Bold SDF`. |
| A4 | `Assets/_BusJam/UI/Fonts/LilitaOne.asset` | 2,15 MB | `Theme.uss` yalnız `Fonts/Fredoka-Bold.asset` dosyasını kullanıyor; Fredoka'nın fallback'i `Baloo2-TR`. LilitaOne UITK font asset'ine hiçbir yerden referans yok. |
| A5 | `Assets/_BusJam/Materials/M_Angry.mat`, `M_Obstacle.mat`, `M_Outline.mat`, `M_SeatEmpty.mat`, `M_Slot.mat`, `M_SlotLocked.mat`, `M_Tile.mat`, `M_Tunnel.mat` | 25 KB | GUID referansı yok, adları kodda geçmiyor. Primitive placeholder döneminden kaldılar; board kit artık `M_Kit*` materyallerini, outline `M_OutlineHull`'u kullanıyor. |
| A6 | `Assets/_BusJam/Prefabs/LockIcon.prefab` | 3 KB | GUID referansı yok. `WaitingArea` kilit görselini başka bir prefab'dan alıyor. |
| A7 | `Assets/_BusJam/UI/Vector/` altındaki 11 SVG: `glow_rays`, `lives_halo`, `logo`, `logo_constructed`, `surface_badge`, `surface_blue`, `surface_header`, `surface_lives_inner`, `surface_modal`, `surface_white`, `title_failed` | 6,58 MB | `Theme.uss`/UXML'de `url()` yok, sahnede GUID yok. Yalnız `AntialiasedSvgImporter.RasterSize` içinde adla geçiyorlar (D7'de temizlenecek) ve inceleme scriptlerinde/Tools'ta kullanılıyorlar. Hepsi ZVEC/ZUIR iterasyonlarından kalan eski sürümler. **Not:** `logo*` için R görevlerinden biri menüye logo ekliyorsa önce sor; o durumda madde B'ye kayar. |
| A8 | `Assets/Plugins/Demigiant/DOTween/DOTween.dll.mdb(.meta)`, `Editor/DOTweenEditor.dll.mdb(.meta)` | 80 KB | `.gitignore` `*.mdb` dosyalarını dışlıyor ama `.meta` dosyaları git'te. Bu yüzden temiz bir klonda **sahipsiz .meta** kalıyor ve Unity uyarı veriyor. Bunlar debug sembolü, oyuna gerekmez. Dördü birlikte silinir. |
| A9 | `Assets/TextMesh Pro/Documentation/` | 0,69 MB | TMP Essentials'ın PDF belgesi. Hiçbir referans yok. |
| A10 | `.gitkeep` dosyaları: `Data/Levels`, `Materials`, `Prefabs`, `Scenes`, `Scripts/{Core,Editor,Gameplay,Level,Save,UI}` | ~0 | Bu klasörlerin hepsi artık dolu, `.gitkeep` işe yaramıyor. |
| A11 | İnceleme/yakalama editör scriptleri (13 dosya, `.meta` ile): `Scripts/Editor/CelebrationArcReview.cs`, `CelebrationChoreographyReview.cs`, `CelebrationReview.cs`, `ResultScreenReview.cs`, `ResultTimelineReview.cs`, `VectorUiReview.cs`, `ZScnBackgroundReview.cs`, `ZScnMenuReview.cs`, `ZScnMenuValidation.cs`, `ZScnReview.cs`, `ZScnValidation.cs`, `T2LayoutCapture.cs`, `R4UiCapture.cs` (R4 bitince) | 157 KB | Hepsi tek seferlik kanıt aracı. Çıktılarını repo dışındaki `C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/...` klasörlerine yazıyorlar. `Docs/MIMARI.md`'de hiçbiri geçmiyor. Yalnız birbirlerine referans veriyorlar: `VectorUiReview`/`ResultScreenReview`/`CelebrationReview`/`ZScnImport` içeriden çağrılıyor ama hepsi bu küme içinde ya da B2'de. Tutulan kodun hiçbiri bunlara bağlı değil (`ResultTimelineEditor` yalnız `ResultTimelinePreview`'a bağlı, o da korunuyor). **Silme sırası:** hepsi aynı commit'te silinmeli; tek tek silinirse derleme kırılır. |
| A12 | `Docs/Takip/R1-shots/`, `R5-shots/`, `R6-shots/before/` (boş klasörler) | 0 | Boş klasörler. Sahibi olan R görevi bittiyse silinir. |
| A13 | `Tools/BuildLogoEvidence.py`, `BuildResultEvidence.py`, `MeasureResultReference.py`, `zscn_evidence.py`, `zscn_background_evidence.py`, `GenerateUIEffects.py`, `GenerateLogoArt.py` | ~45 KB | Kanıt ve ölçüm scriptleri; repo dışındaki inceleme klasörlerine bağımlılar. `GenerateUIEffects.py` çıktısını kullanılmayan `Art/UI` PNG'lerine yazıyor. `GenerateLogoArt.py` yalnız kullanılmayan `logo*.svg` dosyalarını üretiyor (A7 ile birlikte gider). |
| A14 | `ArtSource/*.blend1` (7 dosya) ve `ArtSource/*_pre*.bak` (4 dosya) | 4,79 MB | Blender'ın otomatik yedekleri ve K1/K2 öncesi elle alınmış yedekler. `.blend` dosyaları güncel kaynak olarak kalıyor (bkz. B6). |

**A toplamı:** 50+ dosya. Assets altında yaklaşık **32,5 MB** (A1-A11), ArtSource altında 4,8 MB.

## (B) Şüpheli — Asen'e sor

| # | Yol | Boyut | Soru / öneri |
|---|---|---|---|
| B1 | `Assets/_BusJam/Art/UI/*.png` (30 PNG; `editor_void_marker.png` hariç) | 28,3 MB | Oyunda kullanılmıyorlar: yalnız silinecek atlaslar (A1) ve `ResultScreenReview` (A11) referans veriyor. Ama `Tools/RedrawSvgArt.py` SVG'leri bu PNG'lerden yeniden çiziyor (`SOURCE=Assets/_BusJam/Art/UI`). Kaynakları Downloads'taki AI üretimi görseller (`Docs/UI-Assets.md`). **Öneri:** silme, C5'teki gibi `ArtSource/UI/` altına taşı (Assets dışına çıkınca import ve Library maliyeti kalkar) ve Tools yolunu güncelle. Kaynak tutulmayacaksa sil. |
| B2 | `Scripts/Editor/ZScnImport.cs`, `ZScnBackground.cs`, `ZScnMenu.cs` ve `Tools/zscn_extract.py`, `zscn_background_extract.py` | ~30 KB | Tek seferlik üreticiler: Blender'dan çıkarılan layout JSON'larını okuyup `Generated/*.asset` mesh'lerini, sahne objelerini ve materyalleri kuruyorlar. Girdi JSON'ları repo dışında (`BusJamCase_ZVEC_Review/scn/...`), yani başka bir makinede çalışmazlar. Çıktıları zaten commit'li. **Öneri:** sil. Yeniden üretim gerekecekse JSON'ları `ArtSource/Layouts/` altına al ve `Folder` sabitlerini repo içine çevir. |
| B3 | `Scripts/Editor/LevelBatchGenerator.cs` | 8 KB | 12-25 arası level'ları üreten araç; menü öğesi var, iş bitti. Level tasarım sürecini gösterdiği için case'te faydalı olabilir. **Öneri:** tut. |
| B4 | `Docs/` altındaki Z* raporları (`Z*-Report.md`, `Z*-Rapor.md`, `UITK-*.md`, `ZUI-Validation.md`; ~35 dosya) ve kökteki `ZSEAM-Report.md` | ~0,4 MB | Süreç kayıtları. MIMARI yalnız `ZLIGHT-Report.md`, `ZRES-World-Report.md`, `ZSCN-Report.md` ve `ZJUICE-A` raporlarına atıf yapıyor. **Öneri:** hepsini `Docs/Arsiv/` altına taşı (C9), MIMARI'deki atıf yollarını güncelle. Kökte yalnız `MIMARI.md`, `CREDITS.md` ve `ResultTimeline.md` (editör kullanım kılavuzu) kalsın. |
| B5 | `Docs/*-Captures/` (11 klasör), `Docs/ZLE-LevelEditor-1080x1920.png`, `ZUI-ContactSheet.png`, `ZLIGHT-NoSpecular.png`, `Docs/Takip/T1-shots`, `T2-overlay` (içinde `tools/` scriptleri de var), `V-shots`, `Docs/Images/` | ~75 MB (git'te izleniyor) | Eski kanıt görüntüleri. Çoğu artık değişmiş uGUI dönemi ekranlarını gösteriyor. Klonun büyük kısmı bunlar. **Öneri:** teslimden önce sil, yalnız güncel 3-4 ekran görüntüsünü `Docs/Images/` altında tut (README/Teslim için). Git geçmişi küçülmez (pack 383 MB). Geçmiş de temizlenecekse ayrı bir karar gerekir (`git filter-repo`, push edilmiş repo için riskli). R görevlerinin kendi `*-shots` klasörleri görevler kapanınca aynı kurala tabi. |
| B6 | `ArtSource/` (git'e girmemiş; `.blend` dosyaları 2,36 MB, PNG kopyaları 0,4 MB) | 2,8 MB | **Öneri:** `.blend` ve PNG'leri commit et; modellerin kaynağı repoda olsun, AGENTS.md de böyle tarif ediyor. PNG'ler Assets'teki kopyalarla bayt bayt aynı (md5 kontrol edildi) ama `.blend` dosyaları bunlara göreli yolla bağlanıyor olabilir, o yüzden tut. Önce A14 uygulanmalı, `.gitignore`'a `*.blend1` ve `*.bak` eklenmeli. |
| B7 | `Assets/_BusJam/Art/Fonts/LilitaOne-Regular.ttf` (+ `LilitaOne-OFL.txt`) | 32 KB | Yalnız silinecek SDF ve UITK asset'leri (A3, A4) ile `GenerateLogoArt.py` (A13) kullanıyor. Logo yoksa bu font tamamen boşa duruyor. **Öneri:** sil, `CREDITS.md`'den Lilita satırını da çıkar. Logo planı varsa tut. OFL dosyaları lisans gereği font durdukça kalır. |
| B8 | `Assets/_BusJam/Scenes/Game/LightingData.asset` (untracked) | 18 KB | Sahne buna bağlı değil (`m_LightingDataAsset: {fileID: 0}`). Bir ajan bake etmiş ama sahneye bağlanmamış. Bake kullanılmıyor (gerçek zamanlı ışık, gölge yok). **Öneri:** sil. R görevleri bitince sahne hâlâ bağlamıyorsa silinir. |
| B9 | `Assets/_BusJam/Art/Textures/GameBG.png` + `Materials/M_GameBG.mat` | — | Sahnedeki pasif arka plan quad'ı kullanıyor. ZSCN-2 kararı "inactive image retain" diyor. **Soru:** hâlâ gerekli mi? Değilse quad ile birlikte silinir. |
| B10 | `Materials/M_Bus*.mat` (10 dosya) ve `Art/Models/Otobus/Materials/M_Presentation_M_Bus*.mat` (10 dosya) | — | İkisi de ulaşılabilir (FBX remap'i ve prefab'lar). Aynı renk setinin iki kopyası gibi duruyorlar. R7b, hangi FBX'in hangi seti kullandığını Inspector'da doğrulasın. Tek set yetiyorsa remap'i birleştirsin. |
| B11 | `Packages/manifest.json`: `com.coplaydev.unity-mcp` | — | Geliştirme aracı. Teslim repoda kalsın mı? Kalırsa klonlayan kişi GitHub'dan paket indirmek zorunda kalır. **Öneri:** teslim commit'inden önce çıkar ya da README'de belirt. Kullanılmayan built-in modüller (ai, cloth, terrain, terrainphysics, vehicles, wind, vr, xr, video, umbra, tilemap, physics2d) kapatılabilir; kodda hiçbiri kullanılmıyor. Build boyutu açısından küçük kazanç, düşük öncelik. |
| B12 | `thumbnail.png` (kök, `.git/info/exclude` ile dışlanmış), `.claude/` | 92 KB | Proje dışı yerel dosyalar, repoya girmiyorlar. Dokunma. |
| B13 | Repo dışı: `C:\Users\Kullanici\Documents\BusJamCase - Kopya`, `BusJamCase_ZVEC_Review`, `BusJamCase_UITK_Review` | 5,0 GB + 3,4 GB + 351 MB | Repo değil, Asen'in diski. A11 ve B2 silinince inceleme klasörlerine yazan araç kalmaz; elle silinebilirler. |
| B14 | `Assets/_BusJam/Scripts/UI/LevelEditorPanel.cs`: `MoveBus(int,int)` hiç çağrılmıyor | — | MIMARI §6 "Otobüs sırası: renk ekle/sil/**sırala**" diyor ama UI'da sıralama bağlı değil. Ya UI'ya bağlanmalı (eksik özellik) ya da kural güncellenip metot silinmeli. `MovePassenger` da çağrılmıyor (sürükleyerek taşıma bağlı değil). |
| B15 | Yorum dili | — | MIMARI §0 "Yorumlar … Türkçe" diyor. Core ve Gameplay büyük ölçüde Türkçe; UI, Core/AudioManager, GameManager, LivesSystem, Bus, Passenger, CameraFitter ve LevelAutoFill/Grid İngilizce. **Soru:** kural Türkçe kalacaksa İngilizce yorumlar çevrilecek mi (~150 satır), yoksa kural "İngilizce" olarak mı güncellenecek? |

## (C) Taşınacak / yeniden adlandırılacak (eski → yeni)

Kural: taşımayı **Unity içinden** yap (`AssetDatabase.MoveAsset` / MCP), ya da editör kapalıyken dosyayı `.meta` ile birlikte taşı. GUID korunur, sahne ve prefab referansları kırılmaz. Yalnız **yolla** referans veren yerler güncellenmeli; bunlar her maddede yazılı. `git mv` kullan ki geçmiş izlenebilsin.

| # | Eski | Yeni | Güncellenecek yol referansı |
|---|---|---|---|
| C1 | `Scripts/UI/LevelEditorPanel.cs`, `Scripts/UI/LevelEditorPanel.Toolkit.cs` | `Scripts/UI/LevelEditor/` | Yok. Script GUID'le bağlı. `EditorBoardInput.cs` zaten orada; editörün üç dosyası bir arada olur. |
| C2 | `Assets/_BusJam/Art/UI/editor_void_marker.png` | `Assets/_BusJam/Art/Textures/EditorVoidMarker.png` | Yok (`EditorVoidMarker.mat` GUID'le bağlı). |
| C3 | `Assets/_BusJam/Materials/EditorVoidMarker.mat` | `Assets/_BusJam/Materials/M_EditorVoidMarker.mat` | Yok. `M_` önek kuralına uyar. |
| C4 | `Art/Models/Environment/ModularGround.shader`, `ScrollingWater.shader`, `Art/Models/Menu/MenuGround.shader`, `MenuMovingRoad.shader`, `MenuMovingScenery.shader` | `Assets/_BusJam/Shaders/` | Yok. Kod ve materyaller shader'ı adla (`BusJam/...`) ya da GUID'le buluyor. Shader `#include` yolu varsa kontrol et. Shader'ların hepsi tek klasörde olur. |
| C5 | `Art/UI/*.png` (B1'de taşıma seçilirse) ve `Art/UI/Renkli Bus Jam Oyun Logosu.png` | `ArtSource/UI/` (logo: `ArtSource/UI/logo_source.png`) | `Tools/RedrawSvgArt.py` → `SOURCE=ROOT/'ArtSource/UI'`. Assets dışına çıktığı için `.meta` dosyaları silinir. |
| C6 | `Assets/_BusJam/Scenes/Game/Global Volume Profile.asset` | `Assets/_BusJam/Settings/GameVolumeProfile.asset` | Yok (GUID). `MenuVolumeProfile` ile aynı yerde ve aynı adlandırmada olur. MIMARI §1 bu profile `PP_Game` diyor; ad ya `GameVolumeProfile` olarak MIMARI'ye işlenmeli ya da `PP_Game` olarak verilmeli. Boşalan `Scenes/Game/` klasörü B8 ile birlikte silinir. |
| C7 | `Assets/_BusJam/Resources/CelebrationSettings.asset` | `Assets/_BusJam/Settings/CelebrationSettings.asset` | **Önce D1 uygulanmalı** (Resources.Load yerine Inspector referansı). Yoksa runtime kırılır. Sonra `_BusJam/Resources/` klasörü kalkar. `Assets/Resources/DOTweenSettings.asset` yerinde kalır (DOTween orada arıyor). |
| C8 | `Art/Models/Otobus/` → `Art/Models/Bus/`; `Otobus.fbx` → `Bus.fbx`, `OtobusKutlama.fbx` → `BusCelebration.fbx`, `OtobusMenu.fbx` → `BusMenu.fbx`, `*_Animator.controller` aynı kalıpla; `Art/Models/Yolcu/` → `Art/Models/Passenger/`, `Yolcu.fbx` → `Passenger.fbx`; `ArtSource/*.blend` aynı adlarla | — | **Asen onayıyla.** Kod tamamen İngilizce (`Bus.cs`, `Passenger.cs`); Türkçe/İngilizce karışıklığı yalnız burada. Güncellenecekler: `ZScnImport.cs` (B2'de silinmezse), `ORTAK-BRIEF.md` ve vault notlarındaki yollar, Blender export scriptleri. FBX içindeki `YolcuRig` gibi düğüm adları değişmez; animasyon yolları bozulmaz. Riskli değil ama çok dosyaya dokunuyor, o yüzden ayrı bir commit olmalı. |
| C9 | `Docs/Z*-Report.md`, `Z*-Rapor.md`, `UITK-*.md`, `ZUI-Validation.md`, `UI-Assets.md`, kökteki `ZSEAM-Report.md` | `Docs/Arsiv/` | MIMARI'deki `ZLIGHT-Report.md`, `ZRES-World-Report.md`, `ZSCN-Report.md` atıfları `Arsiv/...` yapılır. Raporlardaki göreli görsel linkleri B5 silinirse zaten ölür. |
| C10 | `Art/Models/Otobus/Materials/M_Presentation_M_Bus*.mat` | `M_BusPresentation_*.mat` (B10'da birleştirilmezse) | Yok (FBX remap GUID'le). Çift `M_` öneki temizlenir. |
| C11 | (opsiyonel, önerilmez) `UI/Fonts/*.asset` → `Art/Fonts/UITK/` | — | `Theme.uss`'teki `url("Fonts/Fredoka-Bold.asset")` yolu değişir. Şu anki ayrım (TMP fontları `Art/Fonts`, UITK font asset'leri `UI/Fonts`) tutarlı; dokunmamak daha az risk. |

Mevcut yapıda **doğru** olanlar: `Scripts/{Core,Gameplay,Level,Save,UI,Editor}`, `Data/Levels/Level_NN`, `Prefabs/` (PascalCase), `Settings/` (URP), `UI/` (UXML/USS/TSS/Vector), `Audio/sfx_*` ve `Materials/M_*` (C3 dışında).

## (D) Kod temizliği

| # | Yer | Madde |
|---|---|---|
| D1 | `Scripts/UI/ToolkitGameUI.Results.cs:48,55` | `Resources.Load<CelebrationSettings>("CelebrationSettings")` çağrısı MIMARI §0'daki "Referanslar Inspector'dan" kuralına aykırı. `[SerializeField] CelebrationSettings celebrationSettings` alanı eklenmeli ve Game.unity'de bağlanmalı (Codex/editör işi). Sonra C7 uygulanır. |
| D2 | `Scripts/Level/LevelSolver.cs` | Dosya `Level/` klasöründe ama namespace'i `BusJam.LevelEditor`. Ya namespace `BusJam.Level` yapılmalı (`using` satırları `LevelEditorPanel`, `LevelBatchGenerator`, `LevelEditorPanel.Toolkit` içinde güncellenir) ya da dosya `Scripts/UI/LevelEditor/` altına taşınmalı. Öneri: namespace `BusJam.Level` olsun, çünkü MIMARI §4 veri ve çözücüyü Level'da anlatıyor. |
| D3 | `Scripts/Editor/*.cs` (kalanlar) | `AntialiasedSvgImporter`, `ResultTimelineEditor`/`ResultStepDrawer`, `ResultTimelinePreview` global namespace'te. `AndroidBuilder` ve `LevelBatchGenerator` ise `BusJam.EditorTools` kullanıyor. Hepsi `BusJam.EditorTools` olmalı. Dikkat: `ScriptedImporter` ve `CustomEditor` attribute'ları namespace değişikliğinden etkilenmez, importer GUID'i script GUID'ine bağlı. |
| D4 | Kullanılmayan üyeler (hepsi yalnız tanımında geçiyor) | `Core/ScreenLayout.cs:11` `ColumnWidth`; `Gameplay/CameraFitter.cs:36` `ReferencePosition`; `Gameplay/Bus.cs:57` `PresentationSeat(int)`; `Gameplay/Passenger.cs:71` `PresentationSeatRotation`; `UI/LevelEditorPanel.cs:51` `BoardCamera`. Silinebilir. (`LevelSolver.SolveAll`/`ReplaySettled` MIMARI §6'da geçtiği için korunur; `MoveBus`/`MovePassenger` için bkz. B14.) |
| D5 | `Scripts/UI/ResultTimeline.cs:23-25,38-40` | Tooltip'i "Obsolete" yazan 6 alan: `waveArmRaise`, `waveSpeed`, `waveAmplitude`, `riderHopHeight`, `riderHopFrequency`, `riderHopStagger`. Yalnız A11'deki inceleme scriptleri okuyor. A11 silinince bu alanlar da silinir. `ResultTimeline.asset`'te yetim YAML satırları kalır, zararsızdır; asset bir kez yeniden kaydedilince temizlenir. |
| D6 | `Scripts/UI/ToolkitGameUI.cs:73` `Update` → `RefreshResultTimeline()` | Timeline revizyonu her karede kontrol ediliyor (allocation yok ama canlı ayar yalnız editörde lazım). Çağrı `#if UNITY_EDITOR` içine alınmalı. `celebrationBus?.Resize(Screen.width, Screen.height)` de her kare çalışıyor; `LayoutModeController.Changed` event'ine bağlanmalı ("Boştaki sistem Update kullanmaz", MIMARI §0). |
| D7 | `Scripts/Editor/AntialiasedSvgImporter.cs:46-53` `RasterSize` | A7'de silinen SVG adları (`surface_modal`, `surface_white`, `surface_header`, `logo*`, `title_*` için `title_failed`) burada özel durum olarak duruyor. A7 sonrası gereksiz dallar temizlenir. Kalan importer önemli: **bütün SVG'ler bu importer'la içe aktarılıyor, silinmemeli.** |
| D8 | `Scripts/UI/TapTutorial.cs:5` | `using DG.Tweening;` kullanılmıyor (DOTween çağrıları partial `TapTutorial.Toolkit.cs`'de). Kaldırılmalı. Diğer runtime dosyalarda kullanılmayan `using` bulunmadı. |
| D9 | `Scripts/UI/ToolkitGameUI.Preview.cs` | Yalnız `ResultTimelinePreview` (korunuyor) kullanıyor ve zaten `#if UNITY_EDITOR` içinde. **Tut.** Bilgi için listede. |
| D10 | Partial dağılımı | `ToolkitGameUI` 5 dosyaya bölünmüş (toplam ~1000 satır), `TapTutorial` ve `LevelEditorPanel` ikişer dosyada. Bu kabul edilebilir. Yine de `ToolkitGameUI.Results.cs` (457 satır) tek başına büyük; temizlik kapsamında bölme önerilmez, yalnız not olarak burada. |
| D11 | `Gameplay/Passenger.cs:376` `path.ToArray()` | Her dokunuşta (her karede değil) dizi ayırıyor. Kural ihlali sayılmaz. İstenirse `DOPath`'e verilmek üzere yeniden kullanılan bir `Vector3[]` tamponu tutulabilir. Düşük öncelik. |
| D12 | Belge kayması: `Docs/MIMARI.md` §1, §5, §6 | Yazanlar: "Canvas Scaler / uGUI / TextMeshPro" (UI artık UI Toolkit; TMP yalnız dünya metinlerinde), `UIManager`, `StartPanel/ReadyPanel/HudPanel/CompletePanel/FailPanel` (bu sınıflar yok, yerlerini `ToolkitGameUI` partial'ları aldı), "uGUI paneli" (Level Editor UI Toolkit), "portrait only" (ZRES kararı bunu geçersiz kılmış). MIMARI tasarım kaynağı olduğu için R7b bu satırları güncellemeli. Karar değişikliği değil, mevcut duruma hizalama. |
| D13 | `.gitignore` eksikleri | Eklenecekler: `*.blend1`, `*.bak`, `__pycache__/`, `*.pyc`, `Docs/Takip/EDITOR.lock`, `/[Mm]emoryCaptures/` (zaten var), `*.tmp`. Unity için gerekli olanlar mevcut (Library, Temp, Obj, Build(s), Logs, UserSettings, `*.csproj`, `*.sln`, `.vs`, `.idea`, `*.apk`, `*.aab`). `*.mdb` kuralı kalabilir, A8 sahipsiz `.meta` sorununu çözüyor. |

---

## R7b uygulama sırası

1. R1-R6'nın commit'leri bitti mi, `GOREV-TAKIP.md`'den doğrula. `git status` temiz olmalı (ya da yalnız başkasına ait dosyalar kalmalı).
2. `refscan.py` ve `deadcode.py`'yi yeniden çalıştır (bu dosyanın "Yöntem" bölümü). Yeni kullanılmayan dosya çıkarsa listeye ekle, A'daki bir dosya artık kullanılıyorsa çıkar.
3. **EDITOR.lock al.** Silme ve taşımaları MCP üzerinden `AssetDatabase.DeleteAsset/MoveAsset` ile yap (`.meta` güvenli kalır). Sıra: A11 + D5 + D7 tek parti (derleme kırılmasın) → A1-A10 → C1-C4, C6 → D1 (+ Inspector bağlama) → C7 → D2-D4, D6, D8.
4. Her partiden sonra: derle, Console temiz olsun; Game ve LevelEditor sahnelerinde Play testi yap (Start → Ready → Playing → Complete/Fail, Result Timeline Preview penceresi açılıyor mu, SVG'ler doğru görünüyor mu).
5. Repo dışı dosyalar: A13, A14, `.gitignore` (D13), B6 (ArtSource commit), C9, B4, B5 (Asen onayıyla).
6. Commit'ler küçük ve Türkçe: "Kullanılmayan asset'leri sil", "İnceleme editör araçlarını kaldır", "Shader'ları tek klasöre topla", "CelebrationSettings'i Inspector'dan bağla" vb.
7. Taşıma sonrası `ORTAK-BRIEF.md`, AGENTS.md ve vault'taki (Mimari.md, Kod-Notlari.md) eski yolları güncelle.

## Özet sayılar

- **A:** 14 madde, ~70 dosya (`.meta` hariç). Assets altında **~32,5 MB** (bunun 10,6 MB'ı LilitaOne font asset'leri, 6,6 MB'ı eski SVG'ler, 4,4 MB'ı eski arka plan PNG'leri), ArtSource'ta 4,8 MB yedek.
- **B:** 15 madde. Onaylanırsa Assets'ten 28,3 MB daha çıkar (Art/UI PNG'leri ArtSource'a taşınır), git çalışma ağacından ~75 MB kanıt görüntüsü silinir.
- **C:** 11 taşıma/yeniden adlandırma (C8 ve C11 opsiyonel).
- **D:** 13 kod/belge temizliği maddesi.
- Toplam olası kazanç: Assets'te **~61 MB** (A + B1), git çalışma ağacında ek **~75 MB** (B5), ArtSource'ta 4,8 MB. Library de buna göre küçülür (Art/UI PNG'leri ve büyük SVG raster'ları en pahalı import'lar).

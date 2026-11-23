# Bus Jam Case — Mimari ve tasarım kuralları

Bu dosya kodun tek tasarım kaynağıdır; kod (AI destekli) buna uyar. Kararlar Asen'in (18 Kasım 2026).

## 0. Genel ilkeler

- Temiz, okunur, optimize. Gereksiz soyutlama, interface yığını, DI framework, "manager of managers" yok.
- Sistemler birbirini aramaz: `FindObjectOfType`, `GameObject.Find`, singleton yok. Referanslar Inspector'dan, haberleşme C# `event` ile.
- Her karede çalışan kodda allocation yok: LINQ, `new List`, string birleştirme, `GetComponent` yok. Boştaki sistem `Update` kullanmaz.
- Sık yaratılan nesneler (yolcu, otobüs) `UnityEngine.Pool.ObjectPool` ile havuzdan gelir.
- Yorumlar kısa: sınıf ve gerekli metot başına bir iki cümle, Türkçe. Satır satır yorum yok.
- Namespace: `BusJam.Core`, `BusJam.Level` (veri, `LevelSolver`, otomatik doldurma), `BusJam.Gameplay`, `BusJam.UI`, `BusJam.Save`, `BusJam.LevelEditor` (editör sahnesi UI'ı), `BusJam.EditorTools` (`Scripts/Editor`). İstisna: `AntialiasedSvgImporter` global kalır; SVG `.meta` dosyalarındaki importer override'ı tip adının hash'ini tutar, ad değişirse override kopar.
- Asset'ler `Resources.Load` ile değil Inspector referansıyla gelir; `_BusJam/Resources` klasörü yok (DOTween'in kendi `Assets/Resources/DOTweenSettings.asset`'i hariç). Karar: R7 temizliği, `CelebrationSettings` `Settings/` altında ve `ToolkitGameUI`'a bağlı (22 Kasım 2026).
- İsimlendirme: `PascalCase` tip/metot/property, `camelCase` alan, `[SerializeField] private` tercih edilir.
- Kod `Assets/_BusJam/Scripts/{Core,Level,Gameplay,UI,Save,Editor}` altında. Editor-only kod `Editor` klasöründe ya da `#if UNITY_EDITOR` içinde.

## 1. Platform: dikey mobil

- Render: **URP 14.x**, Unity 2022.3.19f1. One mobile pipeline asset: HDR off, MSAA 2x, all shadows off, one warm directional light, Gradient ambient fill, SRP Batcher on. Gameplay materials (passengers, buses, board kit) use the keyword-free `BusJam/Toon` shader (stepped diffuse, hard specular, top rim; SRP Batcher + instancing). Everything else that is lit uses the same light model from `Shaders/ToonLighting.hlsl` (half-Lambert single step + SH ambient): procedural ground, board sidewalk and menu ground light flat surfaces in the vertex stage, the moving menu scenery/road share `MenuMoving.hlsl`; menu, celebration and decor materials are `BusJam/Toon`. Only the outline hull, TMP/UI text, sprites, the menu sky and editor helpers stay unlit. Decision: one toon light model for the whole game (T1b, 22 November 2026). Decision: toon look, no realtime shadows, game key light (50,150,0) (T1, 22 November 2026). A single global `Settings/GameVolumeProfile` (menu: `MenuVolumeProfile`) provides light LDR grading, quarter-resolution low-quality Bloom and a subtle vignette. UI stays in Screen Space Overlay; the background camera canvas may be graded. Decision: no realtime or fake shadow geometry (ZLIGHT, 20 November 2026). Exact values and cost are in `ZLIGHT-Report.md`.

- Player Settings: tasarım dikey (Portrait); ZRES kararıyla otomatik döndürme ve yeniden boyutlandırılabilir pencere açık, geniş ekranda kadraj ZRES kararına göre uyarlanır (bkz. ZRES Decision).
- Game view referansı **1080×1920**. `Application.targetFrameRate = 60` (GameManager Awake).
- UI: **UI Toolkit** (`UI/GameUI.uxml`, `Theme.uss`, `GamePanelSettings`: Scale With Screen Size, referans 1080×1920, Expand). Güvenli alan `LayoutModeController.SafeViewport` ile kök elemana padding olarak verilir. UI çizimleri SVG, `AntialiasedSvgImporter` ile rasterlanır.
- Input: eski Input Manager, `Input.GetMouseButtonDown(0)` (dokunmayı da karşılar).
- Text: UI metinleri UI Toolkit font asset'i (Fredoka); dünyadaki metinler (tünel sayacı vb.) TextMeshPro.
- Kamera: perspektif, yukarıdan eğik (~65°, §2 ile aynı), **sabit** (Asen kararı, Z5.1). Grid boyutu `GameRules.MaxWidth`/`MaxHeight` (9×9) ile sınırlı; referanstan (7×8) büyük gridde `CameraFitter` kamerayı bakış ekseninde geri çeker (Asen, 22 Kasım 2026, LE1). Otobüs ekranın üstünde, bekleme alanı ortada, grid altta.

## 2. Görseller: placeholder

Model yok. Unity primitive + düz renkli materyal. Referans: Asen'in oyun videosu (level 29-30), aşağıdaki yerleşim oradan.

**Ekran düzeni (yukarıdan aşağı):** sol üstte "Level N" rozeti → yatay yol ve otobüsler → bekleme slotu şeridi → grid → (alt boşluk; orijinaldeki booster çubuğu kapsam dışı).
- Karar (U1, Asen, 22 Kasım 2026): Kilitli booster butonları (Level 8/15/25/35) ve SVG'leri silindi; yerine görünmez `bar-space` şeridi (`ScreenLayout.BarGap/BarHeight`) kalır, kamera grid'i yine onun üstüne koyar (board konumu değişmedi), konfeti çıkış noktası bu şerit.
- Kamera: neredeyse tepeden (~65-70°), hafif perspektif; grid ekranın alt yarısında ortalı.
- **Yol:** ekran genişliğinde yatay şerit. Otobüsler soldan gelir, ortada (slot şeridinin hizasında) durur, dolunca sağa çıkar. **Sıradaki otobüs aktifin hemen solunda, kuyrukta görünür.**
- **Otobüs:** üstü açık, renkli gövde (uzun küp), üstünde 3 koltuk; binen yolcu koltukta görünür. Placeholder: renkli küp + 3 koltuk küpü, binen yolcu koltuğa yerleşir.
- **Slot şeridi:** yolun hemen altında açık renk şerit, 7 beyaz kare slot (sabit sayı, Asen kuralı). Uçtaki iki slot level numarasına göre kilitli başlayabilir: koyu/kilitli materyal + üstünde kilit işareti. Slottaki yolcu slotun üstünde ayakta.
- **Grid:** açık renk zemin paneli, beyaz kare hücreler, kalın açık kenarlık; arka plan koyu. Şekil düzensiz (merdiven/piramit), **çıkış üst kenardaki dar ağızdan** (y = 0 satırında genelde 1-2 yürünebilir hücre, ağzın yanları `Void`). Hücre boyutu ≈ yolcu boyutu, yolcular hücreleri sık doldurur.
- **Yolcu:** capsule (+ küçük küre kafa isteğe bağlı). Renk başına **tek paylaşılan materyal**. Koyu outline: erişilebilir olan belirgin.
- Kızgın yüz: yolcunun üstünde kısa süre görünen kırmızı ikon/quad (DOTween ile zıplar, kaybolur).
- Gizli yolcu: gri materyal + başın üstünde dünya uzayında "?" mesh'i (beyaz dolgu, koyu kontur; renk açılınca gerçek materyal, "?" kalkar).
- Tünel: hücreyi kaplayan lila-gri yuvarlak blok, ağız tarafında koyu lacivert kapak; üstünde beyaz dolgu + siyah konturlu kalan yolcu sayısı. Yolcu çıkınca blok esner, yolcu içeriden büyüyerek ağıza kayar.
- Engel: `Void` karo yok, delik (grid dışı ya da ortada boşluk, hiçbir şey çizilmez); `Obstacle` referanstaki tahta boşluğu gibi çizilir: karo yok, zemin görünür, krem çerçeve etrafından dolaşır. Karar: engel modülü referansla aynı, ayrı kasa/çukur modeli yok (Asen, 22 Kasım 2026). Karar (G1, Asen, 22 Kasım 2026): `Obstacle` tahtanın içinde kalır (taban ve çerçeve onu da kapsar, delik kesilmez); karo yerine yuvarlak kenarlı lila-gri kubbe (`Kit_Obstacle`, `M_KitObstacle`). Tahta tabanı çıkış kenarından kaldırıma `sidewalkJoinDepth` kadar bindirilir (kaldırımla arada zemin şeridi kalmaz).
- **Complete:** grid üstünde konfeti patlaması → "Level Completed" paneli (Continue). **Fail:** "Level Failed" paneli (Try Again). Coin and ad offers remain outside scope; lives follow the rule in §3.

## 3. Oyun kuralları

- Grid düzensiz şekilli olabilir: boşluklar, çıkıntılar, üç yanı kapalı hücreler. Şekil `Void` hücrelerle verilir.
- Hareket 4 yön, çapraz yok. Yolcular birbirini engeller; duvar tipi yok.
- **Çıkış satırı:** `y = 0` (otobüse en yakın satır). Bu satırdaki yürünebilir hücreye ulaşan yolcu gridden çıkar.
- **Erişilebilir yolcu:** çıkış satırında ya da çıkışa boş hücrelerle bağlı. Erişilebilir olan outline'lı.
- **Dokunma:**
  1. Oyun `Playing` değilse yok say.
  2. Pelerin kapalıysa veya erişilemezse → kızgın yüz; pelerinler değişmez.
  3. Hedef seç: otobüs kapıdaysa, rengi aynıysa ve boş (rezerve edilmemiş) koltuk varsa → koltuk. Yoksa boş slot. Slotların hepsi rezerve/doluysa: devam eden koşular ve otobüs gelişleri sonunda bir slot açılacaksa (slottaki/slota koşan bir yolcunun rengi kapıdaki/kapıya gelen otobüsle, o dolacaksa sıradaki otobüsle eşleşiyorsa) yolcu slot sırasının arkasına (taşma kuyruğu, FIFO) koşar, orada bekler; slot açılınca sırayla yerleşir. Hiç açılmayacaksa dokunma hiçbir şey yapmaz, kızgın yüz de yok (Asen kararı, 19 Kasım; ZD2, ZD'deki "varışta hemen fail" iptal).
  4. Hedefi **hemen rezerve et**, yolcunun hücresini **hemen boşalt**, erişilebilirliği yeniden hesapla (outline güncellenir).
  5. Yolcu yol boyunca akıcı koşar (DOTween path). Otobüse varınca koltuğa ışınlanır.
- Input kilidi yok: art arda dokunulan yolcular aynı anda koşar.
- **Otobüs:** sıra level verisinden. Kapasite 3. Dolunca gider, sıradaki gelir. Gelince slottaki aynı renk yolcular (en fazla boş koltuk kadar) kendiliğinden biner.
- Gridde yolcu kendiliğinden hareket etmez; yalnız oyuncu dokununca koşar. Kendiliğinden hareket eden tek şey slottakilerin otobüse binişidir.
- **Gizli yolcu:** erişilemezken gri ve "?" görünür, rengi bilinmez. Erişilebilir olduğu an rengi açılır ve bir daha gizlenmez.
- **İpli yolcu (Asen kuralı, 19 Kasım; üst sınır 22 Kasım):** 0–3 kat ip taşır (`GameRules.MaxRopes`). İp varken erişilemez, outline göstermez, dokununca kızar ve hücresi dolu kalır. Dört yöndeki her komşu hücre dokunulan yolcu ayrılınca bir kat çözer; aynı tünel ağzının her yeniden boşalması sayılır, ağza yolcu gelişi sayılmaz. Son ip çözülünce erişilebilirlik yeniden hesaplanır. Gizli yolcuyla birlikte kullanılabilir; renk erişilebilir olunca açılır.
- **Gizemli/pelerinli yolcu (Asen kuralı, 19 Kasım):** Her yolcu ayrı açık ya da kapalı başlar. Kapalıyken altın kenarlı koyu gece laciverti kukuleta/pelerin gerçek rengi gizler; dokununca kızar. Açıkken normal yol/ipuçları kuralları geçerlidir; outline yalnız açık ve erişilebilir olanda görünür. Kabul edilen her dokunuşta, yolcu koşmaya başladıktan sonra gridde kalan bütün pelerinliler (yolu kapalı veya ipli olanlar dahil) durumunu tersine çevirir. Otobüs, slot ve dolu slotların arkasına koşma sayılır; reddedilen/yok sayılan dokunuş ve otomatik biniş sayılmaz. Ayrılan yolcu etkilenmez. İple birlikte kullanılabilir, `hidden` ile kullanılamaz. Liste level kurulumunda bir kez toplanır; pelerin geçiş tween'i yolcu başına bir kez kurulur ve iki yönde tekrar kullanılır.
- **Tünel:** bir hücre kaplar, yürünmez (engel gibi). Bir yöne bakar; baktığı komşu hücre ağzıdır. İçinde levelde tanımlı sıralı yolcu kuyruğu vardır. Ağız hücresi boşaldığı an (oradaki yolcu dokunulup kalktığında) kuyruktaki sıradaki yolcu ağız hücresine çıkar, erişilebilirlik yeniden hesaplanır. Kuyruk bitince tünel engel olarak kalır.
- **Kazan:** son otobüs dolup gitti.
- **Kaybet (yer):** Devam eden koşular ve otobüsün kapıya gelişi sonuçlandıktan sonra hâlâ boş slot yoksa (taşma kuyruğunda bekleyen varsa dahil) kaybedilir; griddeki renkler sonucu değiştirmez (Asen kararı, 19 Kasım; ZD2).
- **Kaybet (süre):** timer 0.
- **Kaybet (hamle, Asen kuralı, 19 Kasım; ZM2b):** koşu, otobüs geçişi, bekleyen biniş ve taşma yokken gridde yolcu kalır ama erişilebilir, ipsiz ve açık pelerinli/pelerinsiz hiçbir dokunuş yoksa `NoMoves`; metin "No moves left!".
- Karar: Hiçbir level çözülemez olamaz; ip veya pelerin içeren level en az iki farklı kazanan dokunuş dizisi taşır. İlk ip Level_06, ilk pelerin Level_08 (Asen, 19 Kasım 2026).
- Karar (R9, 22 Kasım 2026): İp en çok 3 kat (`GameRules.MaxRopes`; SetRopes, editör, Validate, BatchGenerator). Her kat gövdede ayrı kalın halka (Kit_Rope_1 bel, 2 kalça, 3 göğüs altı) + kameraya bakan krem düğüm; düğüm sayısı = kalan ip sayısı. Halka `M_Rope` (RopePalette dokusu: halat/düğüm rengi) + ince `M_RopeOutline` 2. materyal; pivot halka merkezinde, patlama yerinde şişer. Üretim betiği `ArtSource/BoardKit.blend` içinde `build_rope.py`.
- Karar (R9b, 22 Kasım 2026): Krem düğümler kaldırıldı (Asen beğenmedi); katman sayısı yalnız halka sayısıyla okunur. `M_Rope` düz `_BaseColor` (0.44,0.30,0.34), RopePalette.png silindi. Halka geometrisi/pivotları, kontur ve patlama aynı.

### Lives (Asen rule, 19 November)

- Five lives maximum, one life every 20 minutes (serialized settings). UTC regeneration persists in PlayerPrefs (`Lives`, `LastRegenUtc` ticks as a string); startup and resume recover offline time. A one-second InvokeRepeating publishes changed counts/seconds without Update polling.
- Fail spends one life once. The HUD Restart button spends one life and retries; Fail Retry does not spend again. Starting with zero lives is blocked.
- The Start screen remains visible at zero lives, including the existing life count/countdown. A blocked Play raises `GameManager.PlayBlockedNoLives`; `ToolkitGameUI` shows the no-lives overlay for a serialized 3.5 unscaled seconds after the latest press. One reusable timer tween checks a shared deadline; repeated presses allocate nothing. Regeneration, leaving Start or disabling UI hides the warning. The translucent warning is above StartPanel and does not intercept repeated Play presses.
- Clock rollback rebases LastRegenUtc once and persists a UTC high-water guard (`LivesUtcHighWater`); returning the clock forward grants no rebound lives. Normal elapsed intervals continue below the guard, and startup preserves valid partial progress even with a future saved guard. Production uses server time.
- Decision: Preserve the full 1.4x standing passenger world size when seated, including inverse seat-parent scale; loop imported exhaust on a smoke-only Animator layer and shake only the bus model child (ZPAX2, 19 November 2026).
- Decision: Implement the authorized five-life / 20-minute rule and event-driven start gating (Asen rule, 19 November 2026).
- Karar (W2, 22 Kasım 2026): Yolcu koltukta `seatedVisualOffset` ile minder üstünde oturur; koltuğa oturunca tek parçacıklık parlama (havuzlu Emit); egzoz sürüş mesafesine bağlı puf, BusStation'daki ortak dünya-uzayı ParticleSystem; kapı yolcu varmadan 0.1 sn önce açılır. Fail zamanlaması değişmedi: slot dolunca anında, panel 0.6 sn sonra (referansla aynı).
- Karar (W4, 22 Kasım 2026): Kalkışta gövde yola paralel (`departurePitch` 0.6°, ezilme %1.5); kuyruk aralığı 7.8 (tampon arası ≈ otobüs boyunun %10'u) ve arka tamponda beyaz bağlantı topu (`HitchBall`, Puff mesh). Çıkış satırının üstü kaldırım sayılır: kaldırım kenarı boyunca düz çerçeve, ağız iki yanında iç köşe (fillet). Yol kenarlarında koyu oluk içinde ince turuncu şerit, üst bordür krem (ModularGround). Slotta ince iç çizgi, kilitli slotta sağ-alt koyu yarım; koltuk parlamasına dışa uçan beyaz parçalar (`SeatSparks`, Emit).
- Karar (B2, 22 Kasım 2026): Oynanış otobüsü (`ArtSource/Bus.blend`, üretim scripti Docs/Takip/B2.md'de) referans oranlarında: güverte 1.72 (model), yuvarlak güverte dudağı, 2+kapı(2)+1 yatay gömme pencere ve eğik şoför camı, r 0.30 tekerlek, bijonsuz açık jant; gövde kapının 0.2 gerisine kaydı (kapı boyun %51'inde). Kameraya bakan yan yüz ışığın arkasında kaldığı için gövde normalleri yukarı eğik (custom normal, yan aydınlık tonda). Kapı kanat x aralıkları, koltuk kemiği x'leri ve P1 oturuş ilişkisi, 11 materyal yuvası değişmedi.

## 4. Veri (`BusJam.Level`)

- `PassengerColor : byte` enum: `Red, Pink, Purple, Blue, Green, Yellow, White` (videodaki 7 renk).
- `CellType : byte` → `Empty`, `Passenger`, `Void` (karo yok, delik, çizilmez), `Tunnel`, `Obstacle` (yürünmez engel; tahta içinde kubbe). Yeni değerler sona eklenir ki var olan level asset'lerindeki sayılar kaymasın.
- `TunnelData` (serializable class) → `Vector2Int cell`, `Direction facing` (`Up, Down, Left, Right`), `List<PassengerColor> queue`. `LevelData.tunnels` listesinde; hücre tipi `Tunnel` olur.
- `CellData` struct → `type`, `color`, `hidden`, `byte ropes`, en sonda `Cloak cloak`. `Cloak : byte { None, StartsOpen, StartsClosed }`; eski assetlerde 0 = pelerinsiz/ipsiz (Asen kuralı, 19 Kasım).
- `LevelData : ScriptableObject` → `width`, `height`, `CellData[] cells` (`index = y * width + x`), `timeLimit`, `List<PassengerColor> busOrder`. `Resize` örtüşen hücreleri korur. Bekleme slotu sayısı level verisinde **tutulmaz**: ekranda her zaman 7 sabit slot vardır, uçtaki ikisi level numarasına göre açılır (`GameRules.VisibleSlotCount/FirstSlotUnlockLevel/LastSlotUnlockLevel`, Asen kuralı 19 Kasım).
- `Validate(out string error)`:
  - pelerin yalnız Passenger hücresinde; `hidden` ile birleşimi ve tanımsız pelerin değeri koordinatlı hata verir; ip+pelerin geçerlidir (Asen kuralı, 19 Kasım),
  - ip yalnız Passenger hücresinde; ip sayısı başlangıçta dolu, sonradan boşalabilecek dört yön komşularının (Passenger ya da kuyruğu dolu tünel ağzı) sayısını aşamaz (Asen kuralı, 19 Kasım),
  - her renkte yolcu sayısı (gridde + tünel kuyruklarında) = o renkteki otobüs × 3,
  - her tünelin ağız hücresi grid içinde ve `Void`/`Tunnel` değil,
  - çıkış satırında en az bir yürünebilir hücre,
  - tüm yürünebilir hücreler çıkışa bağlı (boş gridde BFS).
- `LevelDatabase : ScriptableObject` → sıralı liste; `Get(index)` liste bitince baştan döner, ekrandaki level numarası artmaya devam eder.
- Assetler: `Assets/_BusJam/Data/Levels/Level_01.asset` …, `Assets/_BusJam/Data/LevelDatabase.asset`.

## 5. Sistemler

| Sınıf | Tür | Sorumluluk |
| --- | --- | --- |
| `GameManager` (Core) | MonoBehaviour | Controls `GameState { Start, Ready, Playing, Complete, Fail }`, level loading, progress, next and retry. Events: `StateChanged`, `PlayingStarted`, `LevelLoaded(int levelNumber, LevelData)`. |
| `LivesSystem` (Core) | MonoBehaviour | Persistent life count, offline UTC regeneration, rollback guard and changed-count/countdown events (Asen rule, 19 November). |
| `GameRules` (Core) | static | Sabitler (`BusCapacity = 3`). |
| `Board` (Gameplay) | düz C# sınıfı | Hücre doluluğu (hücredeki `Passenger` referansı), yürünebilirlik, `TryFindPath` (BFS), `ComputeReachable` (çıkıştan tek flood-fill). MonoBehaviour değil, sahneye bağlı değil. BFS kuyruk/parent dizileri bir kez ayrılır, tekrar kullanılır. |
| `BoardView` (Gameplay) | MonoBehaviour | Levelden karoları ve yolcuları kurar (havuz), `CellToWorld`. Kamera sahnede sabit. |
| `Passenger` (Gameplay) | MonoBehaviour | Renk, hücre, durum (`Idle, Running, InSlot, Seated`). `SetReachable`, `ShowAngry`, `RunTo(path, onArrived)`, `Reveal`. Mantık kararı vermez. |
| `BusStation` (Gameplay) | MonoBehaviour | Otobüs sırası, aktif otobüs, koltuk rezervasyonu, dolunca gönder / sıradakini getir. Event: `BusArrived`, `AllBusesDone`. |
| `WaitingArea` (Gameplay) | MonoBehaviour | N slot, rezervasyon, slot pozisyonları, renge göre yolcu verme. |
| `GameplayController` (Gameplay) | MonoBehaviour | Kuralların tek yeri: dokunma kararı, hedef rezervasyonu, otomatik biniş, kazan/kaybet. Event: `LevelCompleted`, `LevelFailed`. |
| `InputHandler` (Gameplay) | MonoBehaviour | Tap → kamera raycast (yolcu layer'ı) → event `PassengerTapped`, `ScreenTapped`. Kamera bir kez cache'lenir. |
| `LevelTimer` (Gameplay) | MonoBehaviour | Yalnız çalışırken `enabled`. Görünen saniye değişince `TimeChanged`, 0'da `Expired`. |
| `SaveSystem` (Save) | static | PlayerPrefs: son level indeksi. |
| `ToolkitGameUI` (UI) | MonoBehaviour, partial | `StateChanged`'i dinler, UI Toolkit ekranlarını açar/kapar. Dosyalar: ana (HUD, Start/Ready, ayarlar, pause), `.Layout` (yerleşim), `.Results` (Complete/Fail, `ResultTimeline` ile örneklenen sonuç animasyonu, kutlama otobüsü), `.Transition` (geçiş karartması), `.Preview` (yalnız editör önizlemesi). `CelebrationSettings` ve `ResultTimeline` Inspector'dan. |
| Ekranlar (`GameUI.uxml`) | UXML | Start: Level button. Ready: centered pulsing Fredoka Bold "Tap to start" over a light dim. HUD: level and timer. Complete: immediate +25 pop; flying coins credit once at the final arrival, with one counter punch and one save. Fail: reason and Retry. |

Flow: `GameManager` loads the saved level into `Start` (menu). The Level button checks lives and enters `Ready`: board, buses and HUD are visible with the full stopped timer. The next tap anywhere enters `Playing`, removes the overlay and starts the timer. Passenger input runs before screen input, so this tap cannot move a passenger. The menu activation frame cannot also start play. Retry, Restart and Next load into `Ready`; zero lives returns to the normal Start menu. `PlayingStarted` shows unseen Level 1/2/6/8 tutorials after the ready tap and arms Level 3 to wait for the green bus.

### Feedback tempo (ZFEEL baseline, 20 November 2026)

This table records the ZFEEL baseline. ZJUICE-A overrides the bus presentation timings below.

| Setting | Previous | Current |
| --- | --- | --- |
| Bus arrival / departure movement | 0.6 s each | 0.3 s each |
| Brake / settle wait and clip | 0.6 s | 0.3 s |
| Door open / close clip | 0.333333 s each | 0.166667 s each |
| Departure delay (door close) | 0.333333 s | 0.166667 s |
| Bus animation crossfade | 0.1 s | 0.05 s |
| Passenger run speed (grid, slots, overflow) | 4.8 units/s | 6.72 units/s |
| Passenger Run state speed | 1 | 1.4 |
| Placeholder half-stride | 0.105 s | 0.075 s |
| Reachable passenger hull thickness | 0.015 | 0.027 (1.8x) |

The nonfinal bus change takes 1.066667 s from physical fill to the next boarding-ready event (previously 2.133333 s). Door opening still starts at boarding readiness; its clip runs at twice the previous speed, along with Brake and DoorClose. Drive, idle shake, boarding pops and the immediate silent final departure keep their existing behavior. Serialized values in Game and LevelEditor, the bus/passenger prefabs and code defaults agree. Run cadence increases with translation speed; idle and seated animation speeds stay unchanged.

Bus movement never locks normal passenger input. Seat/slot reservations happen on acceptance. `IsTransitioning`, `runningCount`, `SlotWillOpen` and the physical arrival/fill callbacks still govern automatic boarding and failure checks; timing changes add no timer-based failure decisions. Outline visibility still depends on reachability, ropes and cloak state; only the shared hull material thickness changes.

### Continuous bus flow (ZJUICE-A, 20 November 2026)

- Nonfinal fill starts a reusable 0.08-second model squash/recovery; the existing door-close clip is compressed into that interval. Departure then accelerates with InQuad over the existing 0.3 seconds. The next bus starts 0.12 seconds into departure, giving 60% movement overlap; followers start 0.04 seconds apart. Arrival remains 0.3 seconds with OutCubic, zero positional overshoot, a 3-degree model-child pitch and one 0.025-unit suspension bob. Four Inspector-bound wheel bones rotate from actual travel distance, with a 0.24-unit radius.
- `BusApproaching` fires once at 85% of arrival distance and stops with closed doors (see R2 decision below). Matching passengers already in slots may reserve seats and run to the stationary final door position then. `Bus.BoardWhenReady` buffers early runners until `IsAtDoor` becomes true; physical seat callbacks still drive fill. Direct grid taps retain the existing door-only rule. No runner spacing, traffic scheduling or extra target reservations are added.
- Fill to next boarding-ready event is now 0.500 seconds nominal, versus the 1.066667-second ZFEEL baseline. Final-bus waving, immediate `AllBusesDone`/completion and silent departure retain their existing event timing. Failure guards, timer rules and passenger travel speed are unchanged. Reset/pooling cancels motion and clears buffered passengers/callbacks.

Decision: Overlap bus presentation and allow slot approach at 85%, while gating boarding at the door and retaining immediate final completion (Asen's ZJUICE-A scope, 20 November 2026). Edit-mode timing evidence and the five-frame handoff strip are in `ZJUICE-A-Report.md`.

Decision: Bus doors stay closed on arrival; `Bus.Enter` opens them (no crossfade, 30% head start, 0.11 s states) for each passenger at the door and a reusable 0.22 s timer closes them after the last one walks in. A dark door well sits behind the leaves; the model child is offset so the door centre is on `doorPoint` / the middle slot (R2, 22 November 2026).

### Tutorials (ZTUT2 + ZTUT3, 20 November 2026)

- Level 1 reads the first bus colour from Level_01: Red. The hand targets reachable red passengers at (0,0), (0,1), (0,2), advancing only after the indicated passenger leaves. Other taps are silently filtered before angry feedback. After the third accepted tap the hand hides; the input filter releases and `TutorialSeen_1` saves when `BusStation.BusFilled` confirms the first bus is physically full.
- Level 2 teaches hidden passengers with "Hidden passengers show their colour once a path opens." The hand first targets a tappable, non-hidden neighbour of a hidden passenger (active bus colour preferred; Level_02: red (0,0) next to hidden red (0,1)). Its departure reveals the hidden passenger before `TapAccepted`; if the revealed passenger is tappable the hand moves to it, otherwise the lesson completes. Tapping it completes and saves `TutorialSeen_2`. Safety release and the first-accepted-tap fallback match the other lessons.
- Level 6 retains "Free tied passengers by moving their neighbours." The pink passenger at (2,1) starts with two ropes. The former (3,0) pink passenger moves to (2,0), and (3,0) becomes Empty. Both indicated neighbours are initially reachable: pink (2,0), then green (3,1), whose hidden colour reveals normally on reachability. Only the indicated passenger can move. Each departure removes one rope; once `RopesLeft` reaches zero the rope visuals disappear, the hint hides, the filter releases and `TutorialSeen_6` saves. Other colours, bus order and the 70-second timer are unchanged.
- Level_03: the tunnel mouth (2,3) starts empty (tunnel rule), the queue is Green, Yellow, Yellow, Green, and the third bus is Green. Until the active green bus reaches boarding readiness (`BusArrived`), play stays free and the lesson stays invisible. Then the hint appears and, if tappable, only the mouth passenger can move. The hand follows non-green replacements while green remains inside; accepting green completes/saves the lesson only when no green remains inside or at the mouth, and clears the hint and filter. Filling the green bus that activated the lesson also completes it. Runtime queue queries use `GameplayController.TunnelContainsColor`, never the original asset queue. If no green remains at the mouth or inside, the hand points directly at a tappable green on the grid; if none exists, the lesson silently completes/saves. A green bus that never arrives cannot save the flag.
- A hand target must always pass `CanTapPassenger`. Input-time checks and `LateUpdate` immediately clear `TapFilter` and hide the hand if the target becomes illegal (including no destination capacity); the hint can stay. An inaccessible tunnel mouth at green arrival uses text only, without a filter, and completes on the first accepted tap. Empty-space and rejected taps do not complete it. Level 6 also releases an unsafe target, but its hint still waits until the last rope is removed.
- Level 8 keeps its existing text, target selection and first-accepted-tap dismissal. State changes and disabling the tutorial always release its filter; incomplete guided lessons are not saved.
- `GameplayController.TapFilter` is a single `Func<Passenger, bool>` property. TapTutorial assigns it only during guided lessons and clears it on completion, state changes or disabling. `OnPassengerTapped` checks it before angry feedback. Normal tappability queries and `CheckFail` never consult it, so guidance cannot create a false NoMoves failure. The shared hand sequence hops by 12.5% of hand height every 0.6 seconds, combines the tap squash, and reuses its paused, non-autokilled tween on retargeting. Hand animation stays unscaled; the level timer is unchanged and continues through guidance (Level 1: 30 seconds, Level 6: 70 seconds). A player who hesitates can still time out; runtime pacing remains a Play check.
- Flags use existing `TutorialSeen_<level>` PlayerPrefs keys, adding Level 3 to Reset Progress. Settings > Reset Progress > Yes clears all five flags (1, 2, 3, 6, 8), refills lives and returns to Level 1; it also resets progress and coins. To preserve progress, in Edit mode delete only `TutorialSeen_1`, `TutorialSeen_3`, `TutorialSeen_6`, `TutorialSeen_8`, call `PlayerPrefs.Save()`, and set `LevelIndex` to 0/2/5/7 before entering Play to test the corresponding lesson.
- Validation: Unity 2022.3.19f1 passed 77 isolated Edit-mode assertions using the saved Level_01/03/06/08 assets and runtime board fixtures: L1 sequential targets and physical bus-fill completion; L3 arrival gating, mouth/replacement guidance, inaccessible/full-slot/unreachable safety, direct grid-green targeting, silent skip and no-arrival persistence; L6 sequential rope removal, safety fallback and disable cleanup; L8 accepted-tap dismissal. Fixtures dispatched gameplay events without running movement; PlayerPrefs flags were restored. The saved Level_06 passed `LevelData.Validate`; `LevelSolver` found three solutions in 134 states without reaching its limit, and all three `ReplaySettled` checks returned `Complete`. Play was not entered; visual hand placement, animation and runtime pacing remain manual Play checks.

Decision: Keep Level_03 data unchanged, wait for the green bus and release unsafe tutorial filters; teach Level_06 with two ropes and two sequential neighbours (Asen, 20 November 2026, 12:22).

Decision: Ready is an explicit state, and coins use a single update with a pop/punch instead of numerical counting (19 November 2026, ZTS). ZTS validation runs in Edit mode only by explicit request.

## 6. Level Editor sahnesi (`Scenes/LevelEditor`)

- Play modunda çalışan ayrı sahne, UI Toolkit paneli (`LevelEditorPanel`) + grid görünümü (oyunun `BoardView`'ını yeniden kullanır).
- Karar (LE1, Asen, 22 Kasım 2026): Editör dünyası oyunla aynı prefab'lardan gelir: `Prefabs/Scene/Lighting` (Directional Light + Global Volume), `Background` (zemin, çimen, bordür), `Board`, `BusStation`, `WaitingArea`; ortam ışığı (Gradient) ve kamera ayarları Game ile aynı. Kamera oyunun `CameraFitter` kadrajını kullanır; `EditorBoardViewport` UI'daki `.editor-board-space` alanını `SetScreenArea` ile verir, kadraj o bölgeye güvenli alansız telefon ekranı gibi çizilir.
- Karar (CAM, 22 Kasım 2026): Editör kamerası level boyutundan bağımsız 9×9 kadrajını kullanır (`CameraFitter.SetMinimumGrid`, `EditorBoardViewport` verir); oyun kadrajı değişmedi. `LayoutModeController` güvenli alanı `UnityEngine.Device.Screen` ölçüsünden `Screen`'e oranla taşır ve ekrana kırpar (editörde Simulator sekmesi açıkken Game view'a cihaz pikselleri geliyordu).
- Var olan leveli seç/yükle ya da yeni oluştur.
- Genişlik/yükseklik, hücreye tıklayarak boya: araç = `Void`, `Empty`, renkli yolcu, gizli aç/kapa.
- Yolcu fırçasında ip sayısı 0–3 seçilir; diğer hücre tipleri ipsiz yazılır (Asen kuralı, 19 Kasım).
- Yolcu fırçasında pelerin: yok / açık başlar / kapalı başlar. `hidden` ile birlikte boyama hata verir; diğer hücre tiplerinde pelerin temizlenir (Asen kuralı, 19 Kasım).
- Otobüs sırası: renk ekle/sil/sırala. Süre alanı (slot sayısı sabit, editörden ayarlanmaz).
- Kaydet: `Validate` geçmezse hatayı göster, kaydetme. Geçerse `#if UNITY_EDITOR` ile `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`; yeni level `AssetDatabase.CreateAsset` + `LevelDatabase.Add`.
- Karar (LE2, 22 Kasım 2026): Editör UI oyunun görsel dilinde (Fredoka, vektör butonlar, mavi popup + soket dokuları, yeni doku yok), metinler İngilizce. Dokunmatikte tek parmakla çalışır: karoya dokun = Tile > Hole > Obstacle > Tunnel, yolcuya/tünele dokun = düzenleme popup'ı (sağ/orta tık kısayol). Otobüs sırası ve tünel kuyruğu çip seç → < > Remove. Kaydedilmemiş değişiklikte New/Load ikinci basışta uygulanır. Test Play oyunda "Editor" butonu/Esc ile aynı levelde editöre döner (`EditorReturn`).
- Karar (LE3, Asen, 22 Kasım 2026): Save yalnız çözülebilir leveli yazar: Validate → solver (en az bir çözüm); çözüm yoksa "No solution — level not saved.", sınır aşılırsa "Could not verify solvable (too complex) — not saved." Yeni level `Level_NN` adıyla LevelDatabase'in sonuna eklenir. Solve/Save/Auto Fill solver'ı taslağın kopyasıyla arka plan thread'inde (`Task` + `CancellationToken`) çalışır, editör donmaz; çalışırken bu üç buton kilitli, sonuç `Update`'te ana thread'de uygulanır, taslak değiştiyse yok sayılır. Test Play kaydetmez: taslağın kopyası `GameManager.TestLevel` olarak oynanır (asset, LevelDatabase ve LevelIndex değişmez), editöre dönüşte taslak kaydedilmemiş değişiklikleriyle geri gelir.
- Karar (LE4, Asen, 22 Kasım 2026): Tünelin baktığı yöndeki çıkış karosu boş kalır: editör oraya yolcu koymayı ve çıkışı yolcuya çevirmeyi reddeder ("Tunnel exit must stay empty."), yeni tünel boş karoya bakan ilk yöne döner, çıkış karosu yeşil yarı saydam işaretli; `Validate` (dışarı/delik/engel/tünel/yolcu) ve Auto Fill uygular. Editörde hücre seçimi dokunulan görünen nesneden: yolcu (gövde + baş), tünel, engel boyunda kutular, en yakın kazanır; yoksa karo üst yüzeyi. Collider kullanılmaz, oyun dokunuşu ayrı.

Çöz: yalnız Unity Editor derlemesindeki `LevelSolver`, taslağı level numarasından hesaplanan slotlarla tarar; DFS + tam durum anahtarlı memo, en fazla üç farklı kazanan dokunuş dizisi, 200.000 durum / 5 saniye sınırı. Sınır aşımı çözülemez sayılmaz. Statik giriş `LevelSolver.SolveAll()`, sıra doğrulama `ReplaySettled`. Oyuncu build'ine çözücü girmez (ZM2b, 19 Kasım 2026).

## 7. Doğrulama (her iş paketinde)

- Script derlendi, Console'da hata ve uyarı yok.
- Play modunda ilgili davranış denendi, Game view (1080×1920) ekran görüntüsü alındı.
- Küçük, anlamlı commit; Türkçe mesaj.

Decision: ZRES overrides the fixed-camera and portrait-only rules: preserve the exact 1080x1920 camera pose, fit larger levels and changed safe viewports along its view axis at FOV 40, offset the projection toward the free area on wider aspects, and use world-locked ground artwork with tiled extensions. Enable auto-rotation, resizable windows and native-size UI art/atlases; existing UI layout stays unchanged in this scope (20 November 2026). Evidence: `ZRES-World-Report.md`.

Decision: ZSCN-2 replaces the Game background image quad with world-locked procedural tile/road bands and the approved EnvironmentKit layout; retain the inactive image, use combined static scenery meshes and event-driven landscape-only side variants (21 November 2026). Camera, gameplay anchors, lighting and grading stay authored; evidence and Edit-mode background budgets are in `ZSCN-Report.md`.

Decision: ZSCN-3 renders Start with the extracted MenuKit world at (1000,0,1000), a cover-fit MenuSky camera quad, and Idle BusMenu. Keep the enabled gameplay camera as an unstacked URP Overlay during Start so existing Camera.main bindings survive; restore Base for every other state. Fit menu lenses by aspect band: portrait 52 degrees / 32 units, square 54 / 30, landscape 58 / 24 (21 November 2026). See `ZSCN-Report.md`.

# Bus Jam Case — Mimari ve tasarım kuralları

Bu dosya kodun tek tasarım kaynağıdır; kod (AI destekli) buna uyar. Kararlar Asen'in (18 Kasım 2026).

## 0. Genel ilkeler

- Temiz, okunur, optimize. Gereksiz soyutlama, interface yığını, DI framework, "manager of managers" yok.
- Sistemler birbirini aramaz: `FindObjectOfType`, `GameObject.Find`, singleton yok. Referanslar Inspector'dan, haberleşme C# `event` ile.
- Her karede çalışan kodda allocation yok: LINQ, `new List`, string birleştirme, `GetComponent` yok. Boştaki sistem `Update` kullanmaz.
- Sık yaratılan nesneler (yolcu, otobüs) `UnityEngine.Pool.ObjectPool` ile havuzdan gelir.
- Yorumlar kısa: sınıf ve gerekli metot başına bir iki cümle, Türkçe. Satır satır yorum yok.
- Namespace: `BusJam.Core`, `BusJam.Level`, `BusJam.Gameplay`, `BusJam.UI`, `BusJam.Save`, `BusJam.LevelEditor`.
- İsimlendirme: `PascalCase` tip/metot/property, `camelCase` alan, `[SerializeField] private` tercih edilir.
- Kod `Assets/_BusJam/Scripts/{Core,Level,Gameplay,UI,Save,Editor}` altında. Editor-only kod `Editor` klasöründe ya da `#if UNITY_EDITOR` içinde.

## 1. Platform: dikey mobil

- Render: **URP** (Universal Render Pipeline, 2022.3 için 14.x). Tek mobil ayarlı URP asset'i: HDR kapalı, MSAA 2x, gölge tek directional ve kısa mesafe, ek ışık yok, SRP Batcher açık. Materyaller `Universal Render Pipeline/Simple Lit`.

- Player Settings: Default Orientation **Portrait**, yalnız portrait izinli.
- Game view referansı **1080×1920**. `Application.targetFrameRate = 60` (GameManager Awake).
- UI: Canvas Scaler **Scale With Screen Size**, referans 1080×1920, match 0.5. HUD bir `SafeArea` paneli içinde.
- Input: eski Input Manager, `Input.GetMouseButtonDown(0)` (dokunmayı da karşılar).
- Text: TextMeshPro.
- Kamera: perspektif, yukarıdan eğik (~65°, §2 ile aynı), **sabit** (Asen kararı, Z5.1). Grid boyutu `GameRules.MaxWidth`/`MaxHeight` (7×9) ile sınırlı, bu sınırın içindeki her level sabit kadraja sığar. Otobüs ekranın üstünde, bekleme alanı ortada, grid altta.

## 2. Görseller: placeholder

Model yok. Unity primitive + düz renkli materyal. Referans: Asen'in oyun videosu (level 29-30), aşağıdaki yerleşim oradan.

**Ekran düzeni (yukarıdan aşağı):** sol üstte "Level N" rozeti → yatay yol ve otobüsler → bekleme slotu şeridi → grid → (alt boşluk; orijinaldeki booster çubuğu kapsam dışı).
- Kamera: neredeyse tepeden (~65-70°), hafif perspektif; grid ekranın alt yarısında ortalı.
- **Yol:** ekran genişliğinde yatay şerit. Otobüsler soldan gelir, ortada (slot şeridinin hizasında) durur, dolunca sağa çıkar. **Sıradaki otobüs aktifin hemen solunda, kuyrukta görünür.**
- **Otobüs:** üstü açık, renkli gövde (uzun küp), üstünde 3 koltuk; binen yolcu koltukta görünür. Placeholder: renkli küp + 3 koltuk küpü, binen yolcu koltuğa yerleşir.
- **Slot şeridi:** yolun hemen altında açık renk şerit, ortada 5 beyaz kare slot. Slottaki yolcu slotun üstünde ayakta.
- **Grid:** açık renk zemin paneli, beyaz kare hücreler, kalın açık kenarlık; arka plan koyu. Şekil düzensiz (merdiven/piramit), **çıkış üst kenardaki dar ağızdan** (y = 0 satırında genelde 1-2 yürünebilir hücre, ağzın yanları `Void`). Hücre boyutu ≈ yolcu boyutu, yolcular hücreleri sık doldurur.
- **Yolcu:** capsule (+ küçük küre kafa isteğe bağlı). Renk başına **tek paylaşılan materyal**. Koyu outline: erişilebilir olan belirgin.
- Kızgın yüz: yolcunun üstünde kısa süre görünen kırmızı ikon/quad (DOTween ile zıplar, kaybolur).
- Gizli yolcu: gri materyal + "?" (renk açılınca gerçek materyal).
- Tünel: hücreyi kaplayan koyu blok, ağzı baktığı yöne açık; üstünde içeride kalan yolcu sayısı.
- **Complete:** grid üstünde konfeti patlaması → "Level Completed" paneli (Continue). **Fail:** "Level Failed" paneli (Try Again). Orijinaldeki coin, can, reklam teklifi kapsam dışı.

## 3. Oyun kuralları

- Grid düzensiz şekilli olabilir: boşluklar, çıkıntılar, üç yanı kapalı hücreler. Şekil `Void` hücrelerle verilir.
- Hareket 4 yön, çapraz yok. Yolcular birbirini engeller; duvar tipi yok.
- **Çıkış satırı:** `y = 0` (otobüse en yakın satır). Bu satırdaki yürünebilir hücreye ulaşan yolcu gridden çıkar.
- **Erişilebilir yolcu:** çıkış satırında ya da çıkışa boş hücrelerle bağlı. Erişilebilir olan outline'lı.
- **Dokunma:**
  1. Oyun `Playing` değilse yok say.
  2. Erişilemezse → kızgın yüz.
  3. Hedef seç: otobüs kapıdaysa, rengi aynıysa ve boş (rezerve edilmemiş) koltuk varsa → koltuk. Yoksa boş slot. O da yoksa → kızgın yüz.
  4. Hedefi **hemen rezerve et**, yolcunun hücresini **hemen boşalt**, erişilebilirliği yeniden hesapla (outline güncellenir).
  5. Yolcu yol boyunca akıcı koşar (DOTween path). Otobüse varınca koltuğa ışınlanır.
- Input kilidi yok: art arda dokunulan yolcular aynı anda koşar.
- **Otobüs:** sıra level verisinden. Kapasite 3. Dolunca gider, sıradaki gelir. Gelince slottaki aynı renk yolcular (en fazla boş koltuk kadar) kendiliğinden biner.
- Gridde yolcu kendiliğinden hareket etmez; yalnız oyuncu dokununca koşar. Kendiliğinden hareket eden tek şey slottakilerin otobüse binişidir.
- **Gizli yolcu:** erişilemezken gri ve "?" görünür, rengi bilinmez. Erişilebilir olduğu an rengi açılır ve bir daha gizlenmez.
- **Tünel:** bir hücre kaplar, yürünmez (engel gibi). Bir yöne bakar; baktığı komşu hücre ağzıdır. İçinde levelde tanımlı sıralı yolcu kuyruğu vardır. Ağız hücresi boşaldığı an (oradaki yolcu dokunulup kalktığında) kuyruktaki sıradaki yolcu ağız hücresine çıkar, erişilebilirlik yeniden hesaplanır. Kuyruk bitince tünel engel olarak kalır.
- **Kazan:** son otobüs dolup gitti.
- **Kaybet (yer):** koşan yolcu yok, slotlar dolu ve slottakilerin hiçbiri kapıdaki otobüsün rengine uymuyor. (Varsayım, videoyla doğrulanacak: slotlar doluyken gridde aynı renk erişilebilir yolcu varsa dokununca otobüse gidebilir; o durumda fail değil.)
- **Kaybet (süre):** timer 0.

## 4. Veri (`BusJam.Level`)

- `PassengerColor : byte` enum: `Red, Pink, Purple, Blue, Green, Yellow, White` (videodaki 7 renk).
- `CellType : byte` → `Empty`, `Passenger`, `Void` (grid dışı, yürünmez, çizilmez), `Tunnel`.
- `TunnelData` (serializable class) → `Vector2Int cell`, `Direction facing` (`Up, Down, Left, Right`), `List<PassengerColor> queue`. `LevelData.tunnels` listesinde; hücre tipi `Tunnel` olur.
- `CellData` struct → `type`, `color`, `hidden`.
- `LevelData : ScriptableObject` → `width`, `height`, `CellData[] cells` (`index = y * width + x`), `timeLimit`, `waitingSlotCount` (varsayılan 5), `List<PassengerColor> busOrder`. `Resize` örtüşen hücreleri korur.
- `Validate(out string error)`:
  - her renkte yolcu sayısı (gridde + tünel kuyruklarında) = o renkteki otobüs × 3,
  - her tünelin ağız hücresi grid içinde ve `Void`/`Tunnel` değil,
  - çıkış satırında en az bir yürünebilir hücre,
  - tüm yürünebilir hücreler çıkışa bağlı (boş gridde BFS).
- `LevelDatabase : ScriptableObject` → sıralı liste; `Get(index)` liste bitince baştan döner, ekrandaki level numarası artmaya devam eder.
- Assetler: `Assets/_BusJam/Data/Levels/Level_01.asset` …, `Assets/_BusJam/Data/LevelDatabase.asset`.

## 5. Sistemler

| Sınıf | Tür | Sorumluluk |
| --- | --- | --- |
| `GameManager` (Core) | MonoBehaviour | Akış ve durum (`GameState { Start, Playing, Complete, Fail }`). Level yükle, kaydet, sonraki/tekrar. Event: `StateChanged`, `LevelLoaded(int levelNumber, LevelData)`. |
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
| `UIManager` (UI) | MonoBehaviour | `StateChanged`'i dinler, panelleri açar/kapar. Paneller birbirini bilmez. |
| `StartPanel`, `HudPanel`, `CompletePanel`, `FailPanel` (UI) | MonoBehaviour | Start: "Tap to start" + süre. HUD: sol üstte "Level N", timer. Complete: Next. Fail: sebep + Retry. Metin yalnız değer değişince güncellenir. |

Akış: `GameManager` açılışta kayıtlı leveli yükler → `Start` → ekrana dokunulunca `Playing` + timer başlar → `GameplayController` sonucu → `Complete` (kaydet, indeks +1) ya da `Fail` → UI butonu → `GameManager.LoadNext/Retry`.

## 6. Level Editor sahnesi (`Scenes/LevelEditor`)

- Play modunda çalışan ayrı sahne, uGUI paneli + grid görünümü (oyunun `BoardView`'ını yeniden kullanır).
- Var olan leveli seç/yükle ya da yeni oluştur.
- Genişlik/yükseklik, hücreye tıklayarak boya: araç = `Void`, `Empty`, renkli yolcu, gizli aç/kapa.
- Otobüs sırası: renk ekle/sil/sırala. Süre ve slot sayısı alanı.
- Kaydet: `Validate` geçmezse hatayı göster, kaydetme. Geçerse `#if UNITY_EDITOR` ile `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`; yeni level `AssetDatabase.CreateAsset` + `LevelDatabase.Add`.

## 7. Doğrulama (her iş paketinde)

- Script derlendi, Console'da hata ve uyarı yok.
- Play modunda ilgili davranış denendi, Game view (1080×1920) ekran görüntüsü alındı.
- Küçük, anlamlı commit; Türkçe mesaj.

# R5 — Referans ↔ oyun fark listesi (22 Kasım 2026, 03:45)

Yalnız analiz; kod yazılmadı.

**Referans:** vault `Referans\Kareler\` (10 kare), video `ScreenRecording_11-18-2026 10-20-46_1.MP4` (L29 = 18–83 sn, L30 = 92–108 sn), `Video-Envanteri.md`, WhatsApp L6 (telefon) ve L63 (tablet).
**Bizim oyun:** 03:40 çalışma kopyası. Başka ajanların commit edilmemiş değişiklikleri de içinde (R1, R2, R3, R4, R6, T1b sürüyordu). Play 1080×1920'de çalıştırıldı, `ScreenCapture` ile alındı. Görüntüler `Docs/Takip/R5-shots/` altında:

| Dosya | Ne |
|---|---|
| `01_start_menu` | Start (menü) |
| `02_L21_ready`, `03_L21_playing` | L21 (7×8, ipli + gizli) Ready / Playing |
| `04_L21_angry` | Yolu kapalı yolcuya dokunuş (kızgın yüz) |
| `05_run_*`, `06_board_*`, `07_depart_*`, `07_next_bus` | 3 yeşil dokunuş: koşu, kapıdan biniş, dolu otobüs |
| `08_slots_filling`, `09_fail_a/b/c` | Slotlar doluyor → fail (No space left) |
| `10_L2_ready`, `11_win_a…f` | L2 açgözlü çözüm → son otobüs kalkışı, konfeti, kutlama, Complete |
| `12_L01/03/05/06/08/10/15_playing` | Tünel, engel, ip, pelerin, gizli olan leveller |
| `cmp_ust-bolge_ref30-vs-L21.jpg` | Üst: ref 30 sn, alt: bizim L21 (yol + slot + grid üstü) |
| `cmp_binis_ref59.8-vs-board.jpg` | Üst: ref 59.8 sn biniş, alt: bizim biniş |
| `cmp_kalkis-konfeti-slot.jpg` | Bizim son otobüs kalkışı, konfeti, slot dolumu, koşu |
| `ref_win_81.9-82.9.jpg`, `ref_binis-kalkis_22.5-41.jpg` | Referans dizileri (karşılaştırma için) |

Öncelik: **P0** göze batıyor, **P1** belirgin fark, **P2** ince ayar. İş: **S** < 1 saat, **M** yarım gün, **L** 1 gün ve üstü.
Paralel işler yalnız işaretlendi, derine inilmedi: **R1** stickman modeli + karo/platform, **R2** otobüs kapısı, **R3** engel/tünel/"?"/ip, **R4** menü, sonuç ve popup ekranları.

## A. Oynanış ekranı (statik görünüm)

| # | Kategori | Referans | Bizde | Ne yapılmalı | Öncelik | İş |
|---|---|---|---|---|---|---|
| A1 | UI/renk | Kilitli slot (t0030, 22.5 sn): mor yuvarlak kare, beyaz kilit ikonu, **altında** "LVL 50" yazısı; normal slot boyunda. | Kilit ikonu küçük, kırık bir beyaz şekil ve "LVL 50" yazısının üstüne biniyor; karo normal slottan geniş (`03_L21_playing`, `cmp_ust-bolge…`). | Kilit ikonunu (kendi çizimimiz) yazının üstünde ortala, yazıyı alta al, karoyu slot boyuna indir, köşeleri yuvarlat, üstte açık parlama ekle. | **P0** | S |
| A2 | renk | Slot şeridinin kaldırımı çok açık buz-lila (`#D2DEFE` ölçüldü), üstünde mavi-lila yassı taş lekeleri var (UI-19). | Düz orta gri-lila (`#898091`), leke yok; ekranın geniş bir bandı koyu ve kirli görünüyor. | Kaldırım rengini açık buz-lilaya çek; tekrarlanan taş leke dekalı ekle (prosedürel ya da kendi dokumuz). | **P0** | S (renk) + M (leke) |
| A3 | renk/UI | Slot karosu saf beyaz (`#FFFFFF`), yuvarlak köşe, altta ince gölge/kalınlık. | Kirli gri-beyaz (`#DBDBD3`), düz dikdörtgen, derinlik yok. | Beyaz toon malzeme, yuvarlatılmış köşe, alt kalınlık (pah) ya da gölge şeridi. | P1 | S |
| A4 | renk | Grid hücresi kırık beyaz (`#FAF5FF`), ince lila çizgi, altında hafif gölge (UI-16). | Hücreler gri (`#C7C2CA`), aralarında koyu çizgiler; grid ref'e göre gri ve kontrastsız. | Hücre rengini açık beyaza çek, derz rengini lilaya çevir. **R1** (karo) ile birlikte yapılmalı. | P1 (R1) | S |
| A5 | renk | Yolcu renkleri doygun ve açık: yeşil = **lime** (`#BFFF00`), mavi = **cyan** (`#01CCFF`), pembe, mor (magenta-mor), kırmızı (pembe-kırmızı), sarı, beyaz; L63'te turuncu da var. | Yeşil koyu saf yeşil (`#3CB500`), mavi daha koyu (`#0696DE`). Kırmızı/mor/sarı yakın. Otobüsler de aynı malzemeyi paylaştığı için yeşil otobüs koyu yeşil görünüyor. | `M_Green` → lime, `M_Blue` → cyan; renk başına tek malzeme olduğu için yolcu ve otobüs birlikte değişir. Turuncu renk eklemek kapsam dışı. | P1 | S |
| A6 | oynanış ekranı | Yolcu hücreyi dolduruyor, başı üst hücreye taşıyor, gridde sıkı bir kalabalık görünüyor (t0030, L63). | Yolcu hücreye göre küçük ve tıknaz; hücreler arası boşluk çok (`03_L21_playing`). | Oran ve ölçek **R1**'de. | P1 (R1) | — |
| A7 | oynanış ekranı | Grid çıkış ağzı kaldırıma yumuşak, içe kıvrılan köşelerle bağlanıyor; çıkış hücreleri kaldırıma akıyor (t0030 üst ortası). | Grid üst kenarı kaldırıma düz çizgiyle değiyor; ağız kenarında kıvrım yok. | BoardView çerçeve mesh'inde ağız iki yanına ters köşe yayı (fillet) ekle. | P2 | M |
| A8 | renk | Yolun alt ve üst kenarında ince turuncu-kahve şerit ve bej bordür var (t0030, 22.5 sn). | Gri-bej bordür var, turuncu çizgi yok. | Yol kenarına ince turuncu şerit ekle (ZSCN bant rengi). | P2 | S |
| A9 | oynanış ekranı | Sıradaki otobüs aktifin hemen solunda, tamponlar neredeyse değiyor, aralarında beyaz bağlantı topu var (t0030, 22.5 sn). | Sıradaki otobüs solda ama arada belirgin boşluk var, bağlantı yok (`03`, `08`). | Kuyruk aralığını daralt; bağlantı topu küçük bir küre olabilir. | P2 | S |
| A10 | oynanış ekranı | Zemin büyük mor taş karolar, pahlı, üst-solu aydınlık, yer yer ot (UI-18). | Benzer mor karolar, ot var; pah ve ışık farkı daha az. | Kontrast küçük ayarı (T1b toon zemini ile birlikte). | P2 | S |
| A11 | UI | HUD: sol üstte Level kartı, sağ üstte ayar; **sayaç yok**. | Level kartı + sayaç + ayar. | Sayaç case gereği kalır; değişiklik yok (bilinçli fark). | — | — |
| A12 | UI | Booster çubuğu: L6'da 4 kilitli kırmızı kutu ("Level 8/15/25/35"). | Aynı (T2). | Yok. Booster işlevi **yapılmayacak** (yalnız kilitli görünüm). | — | — |
| A13 | UI | Altta gerçek reklam bandı (WhatsApp L6/L63). | Beyaz "AD" yer tutucu. | Reklam akışı **yapılmayacak**; yer tutucu kalabilir. | — | — |
| A14 | oynanış ekranı | Tünel: gri yuvarlak kutu, ön yüzünde büyük beyaz sayı (L6 WhatsApp). | L15: gri kutu, üstünde "2" (`12_L15_playing`; R3'ün yeni modeli). 02:40'taki çalıştırmada tünel mor kutuydu, sayı siyah etiketteydi. | **R3**. Not: 02:40 çalıştırmasında L15 yüklenirken `TunnelView.model` atanmamış hatası çıktı (R3 o sırada çalışıyordu); 03:40'ta hata yok. | (R3) | — |
| A15 | oynanış ekranı | Engel/ip/gizli yolcu görünümleri. | `12_L05/06/08/15`. | **R3**. | (R3) | — |

## B. Davranış ve animasyon

| # | Kategori | Referans | Bizde | Ne yapılmalı | Öncelik | İş |
|---|---|---|---|---|---|---|
| B1 | animasyon | Otobüse binen yolcu koltuğa **oturur**: yanlamasına oturma pozu, gövde koltuğun içinde, sadece baş ve üst gövde görünür (22.5–23 sn, t0060, 41 sn). | Yolcu koltuğun **üstünde ayakta / zıplama pozunda** kalıyor; kalkan otobüste iki yeşil ayakta duruyor (`06_board_b`, `07_depart_c`, `11_win_a`, `cmp_binis…`). | Oturma pozu (Seated animasyonu ya da poz) ve koltuk yüksekliğine göre yerleşim. Stickman animasyonu **R1**'de, koltuk noktası **R2**'de. İkisinden birinin üstlenmesi gerekiyor. | **P0** (R1/R2) | M |
| B2 | efekt | Yolcu koltuğa oturunca koltukta beyaz-lime parlama, cyan ince ışınlar ve birkaç beyaz parça (UI-58; t0060, 41.3 sn). | Biniş efekti görülmedi (`06_board_a/b`). | Havuzlu, kısa bir parçacık veya sprite flaş: koltuk noktasında 0.2–0.3 sn, additive. | P1 | S–M |
| B3 | efekt | Hareket eden otobüsün arkasında beyaz-buz mavisi duman pufları; kalkışta ve kuyruk ilerlerken görünür (UI-57; 40.5–41 sn, 82.0–82.6 sn). | Kalkan son otobüste duman görülmedi (`11_win_a`). MIMARI'de egzoz döngüsü var ama karede görünmüyor. | Egzoz partikülünü Drive sırasında açık, görünür boyutta tut (kendi pufumuz). Önce Play'de görünüp görünmediğini doğrula. | P1 | S |
| B4 | animasyon | Otobüs kalkarken düz çizgide sağa hızlanıyor, gövde yola paralel kalıyor. | Son otobüs kalkarken gövde belirgin açıyla dönmüş/yatmış görünüyor (`11_win_a`). | Kalkış squash/pitch değerlerini küçült, yaw dönmesi varsa kaldır. Sonra bir kare dizisiyle doğrula. | P2 | S |
| B5 | efekt | Kazanma: son otobüs çıkarken booster çubuğu sönüyor; grid üstünden büyük konfeti patlaması (yıldız, kalp, disk, kağıt; ~0.75 sn) tüm gridin üstüne dağılıp düşüyor (82.3–83.05 sn, t0083). | Konfeti kaldırım hizasında iki ince çapraz şerit hâlinde, küçük ve kısa (`11_win_b`, `cmp_kalkis…`). Booster çubuğu sahnede kalıyor. | Konfeti kaynağını grid merkezine/üstüne al, yukarı ve yanlara açılan geniş bir patlama yap: daha büyük parça, dönme ve yerçekimi. Kazanınca booster çubuğunu söndür. | P1 | M |
| B6 | animasyon | Kutlama: koyu mor katman, **küçük** mor kutlama otobüsü alt sağdan sola geçiyor, iki yolcu zıplıyor (83.05–84.25 sn). | Ekran ortasında **büyük** yeşil otobüs, iki yolcu kol kaldırıyor (`11_win_c/d`). | Boyut ve yörüngeyi refe yaklaştır: alt bantta sağdan sola geçiş, ekran genişliğinin ~%40'ı. **R4** sonuç ekranıyla birlikte. | P1 (R4) | M |
| B7 | animasyon | Level başında/retry'da slot karoları üstünde beyaz halka parlaması (UI-65; 107.0–107.3 sn). | Yok. | Level kurulurken slotlara kısa bir parlama tween'i. | P2 | S |
| B8 | davranış | Engelli yolcuya dokunuşun refteki tepkisi videoda görülmüyor. | Büyük kırmızı kızgın yüz (`04_L21_angry`). | Değişiklik yok (MIMARI kuralı). Ref doğrulanamıyor. | — | — |
| B9 | davranış | Biniş: slottaki yolcu kapıya koşuyor, kapıdan girip koltukta beliriyor; kapı açılıp kapanıyor (22.5 sn, 41 sn). | Kapı açılıyor, yolcu kapıdan giriyor (`06_board_b`, R2 `81940b0`). | **R2**. Yalnız B1 oturma pozu eksik. | (R2) | — |
| B10 | animasyon | Koşu: kollar sallanıyor, hafif öne eğik, hızlı (21–23 sn). | Koşu pozu var (`05_run_a/b`). | Ayrıntı **R1**'de. | (R1) | — |
| B11 | davranış | Fail öncesi: 5 slot dolu, ~0.6 sn sonra panel açılıyor (102.0–102.6 sn). | Fail son dokunuştan ~2.6 sn sonra (`log.txt`: 31.9 → 34.5 sn, koşu süresi dahil). | Koşu bitişinden sonra 0.5–0.6 sn bekleme yeterli; bugünkü süre koşuyla birlikte kabul edilebilir. Ölçülüp karar verilmeli. | P2 | S |
| B12 | oynanış ekranı | Ready ekranı ve "Tap to start" refte yok: menüden doğrudan oyuna giriliyor (91.7→92 sn); zor levelde "VERY HARD LEVEL" girişi var (17.5–20 sn). | Ready + "Tap to start" (MIMARI kararı). "Very hard" girişi yok. | Ready kalır (karar). "Very hard" başlığı isteğe bağlı; zorluk etiketi olmadığı için **P2 / opsiyonel**. | P2 | M |

## C. Ses

| # | Referans | Bizde | Ne yapılmalı | Öncelik | İş |
|---|---|---|---|---|---|
| C1 | Menüde müzik yok (0–17 sn sessiz). Oyun içinde kısa SFX'ler var: dokunuş/biniş tepeleri (~21–82 sn, 90+ olay), 24–31 sn boyunca sürekli düşük bir katman (koşu/motor döngüsü olabilir), kazanmada güçlü cıngıl (83–84 sn). Fail popup'ında belirgin bir tepe yok. | 6 SFX: start, button, tap, bus depart, win, lose. Müzik yok. | Eksik olabilecekler: koltuğa oturma "pop"u, slota varış, otobüs gelişi. Ses kaydı dinlenmeden tür kesinleşmez (analiz yalnız ses seviyesi tepeleriyle yapıldı). Asen dinleyip karar vermeli. | P2 | S |

## D. Menü, sonuç ve popup ekranları (yalnız işaret: R4)

| # | Konu | Not | Öncelik |
|---|---|---|---|
| D1 | Complete başlığı | Ref: kemik süslü, "O" yerine kafatası, mor kurdele. Bizde yıldızlı "LEVEL COMPLETED!" (`11_win_e`). Süsler kendi çizimimiz olmalı. | P1 (R4) |
| D2 | Coin sayacı | Ref: yığın büyüyerek gelir, sayı 10→75 sayılır, pırıltılar var. Bizde 25 hemen beliriyor (MIMARI kararı: sayma yok, pop + punch). | P2 (R4, karar Asen'in) |
| D3 | Fail paneli | Ref t0106 ile neredeyse aynı (`09_fail_b`). Ref'te HUD'dan küçük kalp yukarı uçuyor, can 5→4 sayacı başlıyor. | P2 (R4) |
| D4 | Start menü | Ref: şehir + köprü yolu, yeşil Level butonu. Bizde benzer (`01_start_menu`). | (R4) |
| D5 | Özellik açılış popup'ı ("CONGRATS! You have unlocked Rope Passenger") ve Complete'teki 9/10 ilerleme çubuğu | Yeni mekanik tanıtımı. Bizde öğretici var (L1/3/6/8). Eklenirse R4. | P2 / opsiyonel |
| D6 | L30 öğretici levhası (koyu yarı saydam kutu, beyaz 3 satır metin, gridin altında) | Bizde TapTutorial metni ve el var; stil karşılaştırması R4'te. | P2 (R4) |

## E. Yapılmayacaklar (gereksiz özellik)

| # | Referansta | Karar |
|---|---|---|
| E1 | Alt navigasyon: HOME / SKINS / SHOP / LEADERBOARD / TEAMS ve sayfaları (4–14 sn) | **Yapılmayacak** |
| E2 | Mağaza, paketler (Starter/Master/Mega Bundle), fiyatlar, No Ads | **Yapılmayacak** |
| E3 | Skin kartları, avatar/profil | **Yapılmayacak** |
| E4 | "Out Of Tiles" devam teklifi (+2 tile, 500 coin, Keep Playing / Give Up) (102.6–105.7 sn) | **Yapılmayacak**; doğrudan Fail paneli |
| E5 | Complete'te x2 reklam butonu, klaket, coin kesesi | **Yapılmayacak** |
| E6 | Can popup'ı: REFILL 500 coin, 1 FREE LIFE reklamı, Starter Bundle (15 sn) | **Yapılmayacak**; bizdeki NoLives uyarısı kalır |
| E7 | Booster işlevleri (geri al, +1 slot, el/yolcu seç, ekstra otobüs) | **Yapılmayacak**; yalnız kilitli görünüm |
| E8 | Bomba, "R" gibi L63 ek mekanikleri | **Yapılmayacak** |
| E9 | Gerçek reklam bandı | **Yapılmayacak**; yer tutucu kalır |
| E10 | Turuncu yolcu rengi (L63) | Kapsam dışı (MIMARI: 7 renk) |

## Özet: P0 ve P1

- **P0**
  - A1 Kilitli slot: ikon yazının üstüne biniyor; ikon + "LVL N" yerleşimi ve karo boyu (S).
  - A2 Kaldırım rengi gri yerine açık buz-lila, taş lekeleri (S + M).
  - B1 Binen yolcu koltukta ayakta/zıplarken kalıyor; oturma pozu gerekli (M, R1/R2 sınırında; sahibi belirlenmeli).
- **P1**
  - A3 Slot karosu beyaz, yuvarlak, kalınlıklı (S).
  - A4 Grid hücresi beyaz, derz lila (S, R1 ile).
  - A5 Yeşil → lime, mavi → cyan (S; otobüs de değişir).
  - A6 Yolcu/hücre oranı (R1).
  - B2 Koltuğa oturma parlaması (S–M).
  - B3 Kalkan otobüste duman görünmüyor (S, önce doğrula).
  - B5 Kazanma konfetisi grid üstünde geniş patlama olmalı; booster çubuğu sönmeli (M).
  - B6 Kutlama otobüsü küçük, alt bantta sağdan sola (M, R4 ile).
  - D1 Complete başlık süsleri (R4).

## Yöntem notları

- Sürücü: scratchpad `r5/drive3.cs` (MCP `execute_code`, `EditorApplication.update` zaman çizelgesi). Dokunuşlar `GameplayController.OnPassengerTapped` üzerinden gerçek kurallarla yapıldı. Win için L2 açgözlü çözüldü, fail için L21'de eşleşmeyen renkler slota yollandı. PlayerPrefs başta yedeklendi, sonda geri yazıldı (LevelIndex 17, Coins 425, Lives 5).
- Renkler 5×5 ya da 7×7 piksel ortalamasıyla ölçüldü (ref: video 30.0 sn tam çözünürlük; bizde `03_L21_playing`). Yolcu başlarındaki parlama ölçümü açığa kaydırır; değerler yön gösterir, kesin sabit değildir.
- Ref iPad (0.695), bizimkiler 1080×1920 (0.5625). Kadraj ve ölçek farkları T2'de ölçüldü, burada tekrar edilmedi.

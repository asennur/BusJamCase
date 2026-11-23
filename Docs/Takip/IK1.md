# IK1 — Uygulama ikonu örnekleri (ajan-IK1)

Durum: örnekler hazır (23.11). **Projeye atanmadı**; Asen seçince atanacak (Player Settings ikon alanı, Codex işi).

## Adımlar
- [x] 0. Okuma (AGENTS, ORTAK-BRIEF "Ek: Blender ile model", stil referansı)
- [x] 1. Model/renk envanteri (ArtSource .blend'ler, Unity materyal renkleri)
- [x] 2. Blender 5.1 arka plan betikleri (EEVEE), iki varyant 1024×1024
- [x] 3. Son işlem (hafif parlama + doygunluk), 192/48 küçültmeler, karşılaştırma görseli
- [x] 4. `ArtSource/Icon.blend` (iki sahne + betikler metin bloğu olarak)

## Çıktılar (`Docs/Takip/IK1-shots/`)
| Dosya | İçerik |
|---|---|
| `IK1-V1.png`, `_192`, `_48` | V1 "sıraya koşuş" |
| `IK1-V2.png`, `_192`, `_48` | V2 "kalabalık sevinç" |
| `IK1-karsilastirma.png` | V1/V2 yan yana, %80 daire kılavuzu, 192 kare/yuvarlak, 48 kare/yuvarlak/×2 |

- **V1:** Mavi menü otobüsü (BusMenu) 3/4 önden, alttan; dört yolcu (kırmızı, sarı, yeşil, mor) Run pozunda otobüse koşuyor, pembe yolcu kameraya yakın kollar havada zıplıyor (Cheer_Jump). Hafif sağa yatık kamera, arkada ağaç/bina/bulut, önde çimen + çiçekli çalı.
- **V2:** Solda mavi oyun otobüsü (Bus, DoorOpen pozu, açık üst katlı), sağda sarı menü otobüsü; mor yolcu açık kapıdan el sallıyor (Cheer_Wave), sarı yolcu üst katta zıplıyor, önde pembe kahraman + kırmızı/yeşil/turuncu/mavi yolcular kollar havada.

## Kullanılan kaynaklar
- Modeller: `ArtSource/BusMenu.blend` (Otobus+OtobusRig), `ArtSource/Bus.blend` (kapı kemikleri, DoorOpen aksiyonu), `ArtSource/Passenger.blend` (YolcuRig; Run, Cheer_Jump, Cheer_Wave aksiyonları), `ArtSource/MenuKit.blend` (TreeCluster A/B, Block_*), `ArtSource/EnvironmentKit.blend` (Bush/Grass). Geometri değiştirilmedi; yalnız poz (aksiyon karesi dondurma, Spine eğimi), obje ölçeği/konumu, sahneye özel materyal.
- Renkler: Unity `_BaseColor` değerleri (sRGB→lineer): M_Red, M_Yellow, M_Green, M_Purple, M_Pink, M_Blue; otobüs gövdesi M_Celebration_Blue / M_Yellow; cam/far/lastik/trim `M_Bus*` değerleri. Turuncu = M_BusTail.
- Materyal: Principled, düşük roughness + coat (plastik parlaklık). Gökyüzü: ekran-uzayı mavi degrade + küre bulutlar (MenuSky.png düz şehir silüeti olduğu için kullanılmadı).
- Işık: anahtar güneş (sıcak, kamera önü-üstü), iki arka kenar güneşi (rim), gökyüzü ortam ışığı. DOF hafif.

## Okunurluk (küçük boyut)
- 192: ikisi de net; V1'de otobüs + zıplayan yolcu, V2'de pembe kahraman + iki otobüs okunuyor.
- 48: V1 daha iyi (mavi otobüs silueti + pembe figür ayrışıyor). V2'de pembe figür baskın, otobüsler ve kalabalık renk lekesine dönüyor.
- %80 daire: V1'de zıplayanın sol eli, V2'de turuncu yolcunun sağ kenarı ve kapıdaki mor yolcu daire sınırına değiyor; otobüs yüzleri ve kahraman içeride.

## Yapılamayan / not
- BusMenu modelinde kapı yok; V1'de yolcular otobüsün ön-yan tarafına koşuyor. V2'deki açık kapı oyun otobüsünden (Bus.blend).
- Trafik konisi modeli yok; eklenmedi (referansın düzeni de tekrarlanmadı).
- Yazı yok. Projeye ikon ataması yapılmadı.

## Yeniden üretme
`ArtSource/Icon.blend` sahneleri `IK1_V1`, `IK1_V2`; betikler aynı dosyada metin bloğu (`iconlib.py`, `v1.py`, `v2.py`, `params_v*.py`, `post_pil.py`). Ham render sonrası `post_pil.py` (PIL) parlama/doygunluk + küçültme uygular.

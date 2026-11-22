# R9b — İp düğümlerini kaldır

## Durum
- [x] 1. Önce görüntüleri (Level_18, satır 3: (0,3)=1, (1,3)=2, (2,3)=3 ip, runtime `SetRopes`)
- [x] 2. `build_rope.py` (ArtSource/BoardKit.blend metin bloğu): düğüm, UV ve palet üretimi çıkarıldı; Kit_Rope_1..3 FBX yerinde yeniden yazıldı
- [x] 3. M_Rope eski düz renk düzenine döndü, RopePalette.png(+meta) silindi
- [x] 4. Sonra görüntüleri + cmp.png; Console 0 hata/uyarı
- [x] 5. Commit: 6c89b71 (asset), docs commit'i bu dosya ile

## Ne değişti
- Kit_Rope_1..3: yalnız halka (tüp r 0.045, 36×8), 576 tri (önce 744). Halka boyutu, yüksekliği (0.41/0.27/0.55) ve pivotu (halka merkezi) aynı; mesh bounds merkezi (0,0,0). FBX .meta/guid'ler ve mesh adları korundu, Passenger.prefab'ta Rope_1..3 meshleri + M_Rope/M_RopeOutline bağlı (kontrol edildi).
- `M_Rope`: `_BaseMap` boş, `_BaseColor` (0.44,0.30,0.34) — R9 öncesi (d27b681) ile birebir.
- `RopePalette.png` silindi; guid `2108a3aadf9b440c9008faeb54f1882b` için Assets + ProjectSettings tarandı, M_Rope dışında referans yoktu, şimdi hiç yok.
- Kod/mekanik değişmedi (MaxRopes=3, SetRopes, patlama). Doğrulama: 3 ipliye RemoveRope → RopesLeft 2, Rope_3 kapalı, kalan ölçekler 1.
- Docs/MIMARI.md: R9b karar satırı.

## Görüntüler (R9b-shots/)
- cmp.png: üst önce, alt sonra; sol yakın çekim (oyun kamerası açısı), sağ oyun kamerası kırpması. Soldan sağa 1/2/3 ip.
- before_game.png / after_game.png (1080×1920, yalnız 3D, UI yok), before_close.png / after_close.png.

## Notlar
- EDITOR.lock alındı (Docs/Takip/EDITOR.lock), iş sonunda silindi.
- PlayerPrefs: gerçek editör anahtarı `HKCU\Software\Unity\UnityEditor\Asen\Bus Jam Case` reg export ile yedeklendi, LevelIndex geçici 17 yapıldı, sonra reg import ile geri yüklendi; yedek ve son hal birebir aynı (LevelIndex 19).

## Asen'in test etmesi gerekenler
- İpli yolcularda katman sayısının yalnız halkalarla okunabilirliği (özellikle 2 ve 3 ip arasındaki fark oyun kamerasında).

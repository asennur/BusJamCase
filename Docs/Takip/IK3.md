# IK3 — C ikonunun yakın kadrajları

23 Kasım 2026. Yerleşik `image_gen` aracı kullanıldı; iki üretimde de doğrudan `IK2-shots/IK2-C.png` girdi olarak verildi.

- `IK3-shots/IK3-C1.png`: Yakın kadraj; otobüs ve yolcular görüntü yüksekliğinin yaklaşık %89'unu kaplıyor.
- `IK3-shots/IK3-C2.png`: Biraz daha yakın ikinci kadraj; otobüs daha geniş, alt ayaklar kenara biraz daha yakın.

Üretim istemleri: C1 için yalnız kamerayı yaklaşık %20 yaklaştır; aynı önden kırmızı otobüsü, tepede sarı/mavi/yeşil ve önde pembe/turuncu/mor/turkuaz yedi parlak yüzsüz yolcuyu, pozlarını, gökyüzü mavisi zemini ve ışığı koru. C2 için aynı girdiden yaklaşık %25 yakın kadraj iste; grubun üstünü %2, altını %95 hizasına yaklaştır, hiçbir baş/el/ayak/aynayı kesme. Her ikisinde merkezdeki %80 çaplı dairede önemli öğeleri yoğunlaştır; rehber çizme, yazı/logo/plaka yazısı, çerçeve, yuvarlatılmış köşe ve şeffaflık ekleme.

Kontrol: İki çıktı görsel olarak incelendi; yedi yolcu, renk sırası, pürüzsüz yüzler ve otobüs korundu. Kesilmiş uzuv veya belirgin el/kol bozukluğu görülmedi; yeniden üretim gerekmedi. Üretimler 1254×1254 geldi; kadraj kırpılmadan System.Drawing ile 1024×1024 PNG'ye küçültüldü. İki dosyada da bütün piksellerin alfa değeri 255 olarak doğrulandı. Kadraj yaklaşık %85 hedefinden biraz daha sıkı; merkezdeki daire için fiziksel maske uygulanmadı, dıştaki kol ve bacaklar dairenin dışında kalabilir.

Assets ve ProjectSettings değiştirilmedi, Unity açılmadı, commit atılmadı. Önceden bulunan IK2 dosyaları korundu. Vault bu oturumun yazılabilir alanı dışında olduğundan vault notları ve oturum kartı güncellenemedi; `beyin.py sync` çalıştırılmadı. Luma için kayıt: IK3 iki yakın kadraj ve bu raporla tamamlandı; kanıt 1024×1024 PNG / tam opaklık kontrolüdür.

# IK2: Uygulama ikonu (AI görsel üretimi, Codex)

Görev: image generation aracınla mobil oyun uygulama ikonu üret. Proje dosyalarına (Assets, ProjectSettings) DOKUNMA, commit ATMA. Çıktıları yalnız `Docs/Takip/IK2-shots/` altına kaydet. Türkçe kısa rapor yaz: `Docs/Takip/IK2.md` (her dosyanın adı ve tek cümle tarifi).

Oyun: Bus Jam benzeri renk eşleştirme bulmacası. Renkli yolcular aynı renkteki otobüse biner. Dikey mobil, Rollic tarzı hybrid-casual.

Rollic ikon kalıbı (araştırma): tek kahraman sahne, kadrajı dolduran büyük objeler, çok doygun renkler, parlak "oyuncak/plastik" 3D render, yumuşak stüdyo ışığı ve güçlü kenar ışığı, YAZI YOK, 48 px'te bile okunur sadelik. Zemin çoğunlukla tek doygun renk veya yumuşak gradyan.

Üç varyant üret, her biri 1024×1024 kare PNG, kenara kadar dolu (şeffaf köşe yok, yuvarlatılmış köşe yok, çerçeve yok):

- **A "renkli sıra":** düz doygun mavi→mor gradyan zemin. Ortada büyük, parlak sarı, yuvarlak hatlı oyuncak gibi bir otobüs, 3/4 önden, kapısı açık. Kapıya doğru sıraya girmiş 6–8 yuvarlak, yüzsüz, parlak plastik insancık; HER BİRİ farklı renkte (kırmızı, mavi, yeşil, pembe, turuncu, mor, turkuaz). En öndeki kapıya biniyor, biri sevinçle kollarını kaldırmış.
- **B "renk eşleştirme":** turkuaz zemin. Üç küçük otobüs (kırmızı, mavi, sarı) yan yana, her birinin önünde kendi renginde 2–3 insancık koşuyor; renk eşleştirme fikri tek bakışta anlaşılsın. Otobüsler kadrajın alt yarısını dolduruyor.
- **C "neşeli kalabalık":** gökyüzü mavisi zemin, beyaz yumuşak bulutlar. Önden büyük, parlak kırmızı bir otobüs kadrajın %60'ı; tepesinde ve önünde kolları havada zıplayan rengarenk insancıklar (her biri farklı renk). Enerjik, eğlenceli.

Kurallar: Hiçbir yazı, harf, logo, rakam, plaka yazısı olmasın. İnsancıkların yüzü yok (düz, pürüzsüz baş). Mevcut bir oyunun ikonunu birebir kopyalama; özellikle "üç otobüs üst üste yığılmış" kompozisyonunu kullanma. Önemli öğeler merkezdeki %80'lik dairede kalsın (Android yuvarlak kırpma).

Dosya adları: `IK2-A.png`, `IK2-B.png`, `IK2-C.png`. Kötü çıkan (yazı içeren, bozuk el/otobüs, bulanık) görseli yeniden üret. Son olarak üçünü yan yana koyan `IK2-karsilastirma.png` yap (varsa Python/PIL ile).

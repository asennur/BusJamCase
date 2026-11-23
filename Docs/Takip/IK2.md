# IK2 — Uygulama ikonu

23 Kasım 2026; yerleşik `image_gen` aracıyla üretildi, kadrajlar aynı araçla yuvarlak kırpmaya uygun şekilde düzeltildi.

- `IK2-shots/IK2-A.png`: Mavi–mor gradyanda sarı otobüse binen, yedi farklı renkte yüzsüz yolcunun sırası.
- `IK2-shots/IK2-B.png`: Turkuaz zeminde yan yana kırmızı, mavi ve sarı otobüsler ile her birine koşan kendi renginde iki yolcu.
- `IK2-shots/IK2-C.png`: Bulutlu gökyüzünde önden kırmızı otobüs ve üzerinde/önünde zıplayan yedi farklı renkte yüzsüz yolcu.
- `IK2-shots/IK2-karsilastirma.png`: A, B ve C ikonlarının soldan sağa yan yana karşılaştırması (3072×1024).
- `IK2-shots/IK2-prompts.txt`: Üç varyantın üretim ve kadraj düzeltme istemleri.

Üretim istemleri: `IK2-prompt.md` içindeki A/B/C sahneleri; ortak olarak parlak plastik 3D, doygun renkler, yumuşak stüdyo ve güçlü kenar ışığı, pürüzsüz yüzsüz başlar, yazısız ve çerçevesiz opak kare istendi. Son kadraj istemi: bütün otobüsleri ve yolcuların el/ayaklarını merkezdeki %80 çaplı daireye sığdır; nesneleri, renkleri ve pozları koru, yalnız kadrajı geri çek ve zemini uzat; daire/rehber çizme.

Kontrol: Üç ikon 1024×1024 PNG ve tamamen opak; yazı, logo, yüz detayı, yuvarlatılmış köşe ve çerçeve yok. Renk grupları, yolcu sayıları ve otobüs geometrisi görsel olarak incelendi; ilk kadrajlar kenarlara yakın olduğu için yenilendi. Boyutlandırma ve yan yana birleştirme System.Drawing ile yapıldı (Python `py` komutu bulunamadı).

Assets ve ProjectSettings değiştirilmedi; Unity açılmadı, commit atılmadı. Görevin yalnız belirtilen çıktı yollarına yazma sınırı nedeniyle vault dosyaları değiştirilmedi.

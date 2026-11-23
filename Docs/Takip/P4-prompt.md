# P4: Doku sıkıştırma + uzun oturum testi (Codex)

Proje: C:\Users\Kullanici\Documents\BusJamCase (Unity 2022.3.19f1, URP, dikey mobil). Önce AGENTS.md, Docs/MIMARI.md, Docs/Takip/ORTAK-BRIEF.md (editör kilidi, git kuralları) ve Docs/Takip/P3.md (önceki ölçüm, yöntem; sürücü betikleri scratchpad `p3/` altında olabilir, yoksa yeniden yaz) oku. Türkçe, kısa. Takip dosyan: Docs/Takip/P4.md (her adımda güncelle). Görüntüler Docs/Takip/P4-shots/.

Editör: MCP for Unity (http://127.0.0.1:8080/mcp). Play/asset değişikliği öncesi Docs/Takip/EDITOR.lock kuralı. Game.unity'yi KAYDETME. PlayerPrefs'i test öncesi yedekle (P3'teki gibi .reg), sonda geri yükle ve teyit et.

## A. Doku sıkıştırma
1. `Assets/_BusJam/` altındaki tüm Texture2D import ayarlarını (.meta) tara. Android ve iPhone override'ı olmayan ve Default'u sıkıştırmasız olanları listele (P3: MenuSky.png 5,9 MB, EnvWater 1,75 MB, MenuAsphalt, MenuWalk, icon_angry, EnvPalette, MenuPalette, EditorVoidMarker).
2. Palet dokuları (EnvPalette, MenuPalette ve adı/kullanımı palet/LUT olanlar) sıkıştırmasız KALSIN: düz renk örnekleme bozulur. Editöre özel doku (EditorVoidMarker) build'e girmiyorsa dokunma, giriyorsa not et.
3. Kalanlara Android ve iPhone override: ASTC 6×6. Gökyüzü/gradyan dokusunda (MenuSky) ASTC 4×4. Max Size mevcut değeri korusun, mipmap ayarını değiştirme. UI Toolkit/SVG (AntialiasedSvgImporter) dokularına dokunma.
4. Build Target'ı değiştirmeden görsel kontrol: Game view'da menü ve L10, önce/sonra ekran görüntüsü (sıkıştırma platform override'ı editörde aktif hedefte görünür; aktif hedef Android ise doğrudan görünür, değilse not et). MenuSky'da bantlanma varsa 4×4'te kal, yine kötüyse o doku için RGB24'e geri dön ve yaz.
5. Önce/sonra tablo: doku, boyut, eski format/MB, yeni format/MB (`Profiler.GetRuntimeMemorySizeLong` ve/veya `TextureUtil` mobil depolama boyutu). Toplam.
6. Yalnız .meta dosyalarını commit'le: "P4: mobil doku sıkıştırma (ASTC)".

## B. Uzun oturum testi (sızıntı)
1. Game sahnesi, Play, Device Simulator Galaxy S10e (P3 indeksi 62). Ölç: A0 = Start ekranı.
2. 30 level döngüsü: P3 yöntemiyle (LevelSolver çözümü / açgözlü strateji, InputHandler yolu) level oyna ve bitir; Continue ile sonraki levele geç. Level listesi sonuna gelirse başa sar (gerekirse SaveSystem.SetLevelIndex). Araya 3 Fail+Try Again, 2 Pause aç/kapa, 2 Main Menu dönüşü, 1 can bitip yenilenme yerine yeterli can (PlayerPrefs Lives'ı test için 5'te tut) koy.
3. Her 5 levelde ve sonda ölç: `GC.GetTotalMemory(false)`, `Profiler.GetMonoUsedSizeLong`, `GetTotalAllocatedMemoryLong`, `GetTotalReservedMemoryLong`; `Resources.FindObjectsOfTypeAll` ile GameObject/Material/Mesh/Texture2D/RenderTexture sayısı; aktif Passenger ve Bus sayısı + havuzdaki (inaktif) sayıları; `DOTween.TotalActiveTweens()`; UIDocument kökündeki VisualElement sayısı; event abone sayısı ölçülebiliyorsa (GameManager.StateChanged GetInvocationList uzunluğu, reflection); son 120 karenin PlayerLoop ms ort/p95 ve GC/kare (P3 yöntemi).
4. Ayrıca kontrol: Pause açıkken `Application.targetFrameRate` 30, kapanınca 60 mı (1b52874). Otobüs: ilk level yüklenince havuzda 5 otobüs var mı ve L27'de oyun ortasında `Bus.Awake`/Instantiate oluyor mu (P3'te 2 kez oluyordu; olmamalı).
5. Değerlendirme: lineer artan sayaç var mı (her levelde +N obje, tween, VisualElement, abone, mono bellek). Varsa kaynağını bul (hangi tip/isim artıyor) ve P4.md'ye yaz; KOD DEĞİŞTİRME, Luma düzeltir. PlayerLoop ms ve GC/kare 1. ve 30. level arasında değişiyor mu.
6. Play'den çık, PlayerPrefs ve Simulator cihazını geri yükle, EDITOR.lock sil.

## Rapor (P4.md)
En üstte teknik olmayan 4-5 satırlık özet (uzun oturumda sorun var mı). Sonra A tablosu, B ölçüm tablosu (A0, L5, L10 … L30), bulgular, görüntü yolları, commit hash'i. Ölçmediğin hiçbir şeyi ölçülmüş yazma; editör/cihaz farkını P3'teki gibi belirt. Docs/Takip/P4.md ve P4-shots'u ayrı commit'le: "Takip: P4 sonucu". Push yok.

# Ortak brief (R görevleri, 22 Kasım)

Proje: C:\Users\Kullanici\Documents\BusJamCase — Unity 2022.3.19f1, dikey mobil, URP, UI Toolkit. Önce AGENTS.md ve Docs/MIMARI.md oku. Türkçe.

Referanslar (Asen):
- Kareler: C:\Users\Kullanici\Documents\AsenOS\🏰 300-Projects\Kariyer\Rollic-Case\Referans\Kareler\ (10 jpg)
- Video: ...\Referans\ScreenRecording_11-18-2026 10-20-46_1.MP4 (110 sn, 1640×2360). ffmpeg/ffprobe PATH'te. Kare çıkar: `ffmpeg -ss <sn> -i <video> -frames:v 1 <out.jpg>`; çıktıyı scratchpad'e koy.
- Video envanteri (zaman damgalı 100 asset kalemi): ...\Referans\Video-Envanteri.md — önce bunu oku, ilgili zamanları bul.
- Ek: C:\Users\Kullanici\Downloads\WhatsApp Image 2026-11-22 at 00.17.29.jpeg ve 00.32.41.jpeg
- Önceki iş: Docs/Takip/T1.md (toon), T2.md (yerleşim/font/HUD), T3.md (perf), T4.md (geçiş).

Hedef: oynanış ekranını referansla birebir aynı göster (oran, renk, davranış, animasyon). Gereksiz özellik ekleme; menüdeki ekstra butonlar/etkinlikler/mağaza YAPILMAZ. Kod modüler, sade, mobil performanslı; her karede allocation yok; event ile haberleşme; referanslar Inspector'dan. Orijinal oyunun asset'lerini videodan kesip kullanma; kendi modelimizi/çizimimizi aynı stilde yap (case kendi işimiz olmalı).

Editör erişimi: Claude'da Unity MCP aracı yok; MCP for Unity HTTP sunucusu http://127.0.0.1:8080/mcp (streamable HTTP; Python: initialize → tools/list → tools/call). Script'leri scratchpad'de tut.
EDİTÖR KİLİDİ: editör tek, birden çok ajan paralel. Play moduna girmeden / sahne veya prefab değiştirmeden / ekran görüntüsü almadan önce `Docs/Takip/EDITOR.lock` dosyası var mı bak; yoksa içine kendi ID'ni ve saati yazıp oluştur, işin bitince (en geç 10 dk) SİL. Varsa 1 dk bekleyip tekrar dene; bu arada kod/asset işine devam et. Kod derlemesi (assembly reload) tüm editörü etkiler — derlemeyi tetikleyen kayıtları kilit alıp yap.
Blender: PATH'te yok; model işi gerekiyorsa Unity içinde (ProBuilder yoksa prosedürel mesh/primitive birleşimi) veya mevcut FBX'leri (Assets/_BusJam/Art/Models, kaynak ArtSource/*.blend) kullan. Blender bulursan (C:\Program Files\Blender Foundation\...) bpy ile arka planda çalıştırabilirsin.

Git: yalnız kendi dosyaların (`git add <yol>`; ortak dosyada `git add -p` ile kendi parçan), Türkçe mesaj, push yok. Game.unity'de başkalarına ait kirli satırlar var — kendi parçanı ayrı commit'le, takip dosyana yaz. Docs/Takip/GOREV-TAKIP.md'ye dokunma (Luma tutuyor).
Takip: kendi Docs/Takip/<ID>.md dosyanı her adımda güncelle (timeout olursa oradan devam edilecek). Önce/sonra görüntülerini Docs/Takip/<ID>-shots/ altına koy; referansla yan yana veya overlay karşılaştırma yap.
Sonuç raporu: değişen dosyalar, commit hash'leri, önce/sonra görüntü yolları, kalan farklar, Asen'in test etmesi gerekenler.

## Ek (Asen, 22 Kasım): Blender ile model
Model birebir olmalı; gerekiyorsa Blender'da yap. Blender kurulu: `C:\Program Files\Blender Foundation\Blender 5.1\blender.exe`. Arka planda bpy ile çalıştır: `blender.exe -b <dosya.blend> --python <script.py>` (script scratchpad'de; yeni model için `-b --factory-startup --python`). Kaynak .blend'ler `ArtSource/`, FBX çıktısı `Assets/_BusJam/Art/Models/` (Unity ölçeği 1 birim = 1 m, -Z ileri/Y yukarı ayarlarıyla export, Apply Transform). Blender MCP soketi (9876) şu an kapalı; bpy arka plan yolu yeterli. Kendi geometrimiz, referansla aynı stil/oran; low-poly (mobil).

## Ek: Dosya düzeni
Yeni dosyayı doğru klasöre koy, geçici/deneme dosyası bırakma (scratchpad'e koy). Temizlik ve klasör düzeni R7 görevinde, R1-R6 bitince yapılacak — kendi alanında kullanılmayan dosya üretme.

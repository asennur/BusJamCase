# T1b — Her şey toon (ajan-T1b)

Durum: **bitti** (22.11 03:25) — commit `e01280e`

## Adımlar
- [x] 0. ORTAK-BRIEF, T1.md okundu; aktif ajan dosyaları kontrol edildi
- [x] 1a. Statik denetim (Python: .mat → shader, sahne/prefab/FBX → materyal) — tablo aşağıda
- [x] 1b. Play'de `Renderer` dökümü (Game, Level 6/18/21 Playing; menü, kutlama havuzu, gizli objeler dahil)
- [x] 2. Karar (aşağıda)
- [x] 3. Uygulama + önce/sonra görüntüleri (`T1b-shots/`)
- [x] 4. İkinci tam denetim + kalan liste

## 1. İlk denetim (toon olmayanlar, 02:10)
| Materyal | Shader | Kullanan | Toon mu | Yapılan |
| --- | --- | --- | --- | --- |
| M_ModularGround | BusJam/Modular Ground (prosedürel, unlit) | Oyun zemini (karo/kaldırım/yol/çimen) | Hayır | Ortak toon ışığı |
| M_BoardSidewalk | BusJam/Board Sidewalk (unlit düz renk) | Tahta arkası, karo altları (BoardView) | Hayır | Ortak toon ışığı |
| M_ModularCurb | URP Simple Lit | Bordür (31 + 80 uzatma) | Hayır | → BusJam/Toon |
| M_MenuPalette, M_MenuWalk, M_MenuAsphalt | URP Simple Lit (doku) | Menü dünyası (MenuKit.fbx) | Hayır | → BusJam/Toon (doku `_BaseMap`) |
| M_MenuMovingPalette | Menu Moving Simple Lit (Simple Lit kopyası, onlarca multi_compile) | Menü kayan dekor (72) | Hayır | Toon tabanlı yeniden yazıldı |
| M_MenuMovingRoad | Menu Moving Road (Simple Lit kopyası) | Menü kayan yol | Hayır | Toon tabanlı yeniden yazıldı |
| M_MenuGround | Menu Ground (SH + N·L) | Menü çimen | Hayır | Ortak toon ışığı |
| M_Celebration_* (7) | URP Simple Lit | CelebrationSettings (kutlama otobüsü + yolcular) | Hayır | → BusJam/Toon |
| M_Presentation_M_Bus* (10) | URP Simple Lit | OtobusKutlama.fbx, OtobusMenu.fbx (menü otobüsü) | Hayır | → BusJam/Toon |
| M_BusSmoke | URP **Lit** | Kutlama/menü otobüsü egzozu | Hayır | → BusJam/Toon (M_ExhaustSmoke ile aynı) |
| M_Environment | Background Simple Lit | EnvironmentKit / Grass Layout | Hayır | **R6** kendi işinde Toon'a çevirdi (`33ad746`) |
| M_Water | Scrolling Water | Gölet | Hayır | **R6** Toon'a çevirdi (`33ad746`) |
| M_MenuSky | URP Unlit | Menü gökyüzü quad'ı | — | Kalır |
| M_OutlineHull | BusJam/OutlineHull | Yolcu konturu | — | Kalır |
| M_TunnelCount, LiberationSans SDF | TMP Distance Field | Tünel sayısı, HiddenMark, kilitli slot "UnlockLevel" yazısı | — | Kalır |
| Sprites-Default | Sprites/Default | Yolcu AngryIcon | — | Kalır |
| M_GameBG | URP Unlit | Game: eski quad (kapalı); LevelEditor sahnesi | — | Kalır |
| EditorVoidMarker | URP Unlit şeffaf | EditorVoidMarker.prefab | — | Kalır |
| M_Outline | URP Simple Lit | Hiçbir yerde | — | R7 temizlik |

Runtime kod: `Scripts/` altında `new Material` / `Shader.Find` yok; yalnız `sharedMaterial` atamaları (Bus, Passenger, BoardView, CompleteCelebration) ve menü otobüsü MPB `_BaseColor` (Toon'da var). Partikül sistemi yok (egzoz mesh + toon materyal).

## 2. Karar
- Toon.shader'a özellik eklemek yerine **ortak `Shaders/ToonLighting.hlsl`**: `ToonStep` (yarım-Lambert tek basamak), `ToonDiffuse` (basamaklı ana ışık + SH ambient), `ToonVertexLight` (düz yüzey: hepsi vertex'te). Zemin/menü shader'ları prosedürel desenlerini korur, ışığı bu include'dan alır; her biri kendi UnityPerMaterial'ında `_ShadeColor/_RampThreshold/_RampSmooth/_AmbientStrength` (Toon ile aynı varsayılan) taşır → SRP Batcher uyumlu, yeni keyword/varyant yok.
- Menü kayan dekor/yol: Simple Lit kopyaları (~30 multi_compile) silindi, ortak `MenuMoving.hlsl` + `#define MENU_ROAD` (keyword değil). Mesafe kırpma/alfa, yol şeridi ve `_MenuMotion` aynı.
- Doku/arka plan tonu için Toon.shader'a dokunulmadı: `_BaseMap` R6 tarafından eklendi (`33ad746`); R6'nın geçişinde dekor artık MuteBackground'dan geçmiyor (zemin hâlâ geçiyor); R6 görseli referansa göre ayarladığı için dokunulmadı.
- Zemin renk düzeltmesi: toon ışığı (yukarı bakan yüzey ≈ 0.85 ışık + SH·0.25) zemini biraz değiştiriyordu; materyal renkleri ekrandaki ortalamaya göre ölçülerek düzeltildi (alt karo bandı önce 90.7/79.1/121.1 → sonra 90.8/79.2/121.3 RGB).

## 3. Sonuç (görüntüler `T1b-shots/`)
- Oyun (Play, ana kamera UI'sız 1080×1920): `before-play-L6/L18/L21.png` ↔ `after2-play-L6/L18/L21.png`, yan yana `cmp-play-L18.png`. Zemin aynı; bordür koyu gri Simple Lit yan yüz → krem toon (referanstaki açık bordüre daha yakın). `after1-play-L18.png` ilk telafi denemesi (zemin ~%7 açık kalmıştı).
- Menü (Edit, ZScnMenuReview 1080×1920): `before-menu.png` ↔ `after-menu.png`, `cmp-menu.png`. Binalar, ağaçlar, köprü korkuluğu ve otobüs iki tonlu toon; parlaklık aynı aralıkta.
- Kutlama ekranı: `CelebrationReview` aracı güncel kodla NullReference veriyor (`CelebrationReview.cs:122`), çekim alınamadı. Materyaller Play dökümünde `BusJam/Toon`; kutlama otobüsü menü otobüsüyle aynı M_Presentation materyallerini kullanıyor.

## 4. İkinci denetim (03:20, statik + Play dökümü)
Play'de görünen (ON) renderer'lar: BusJam/Toon, Modular Ground, Board Sidewalk (toon ışıklı), OutlineHull, TMP. Menüde ek olarak Menu Moving Toon/Road, Menu Ground (toon ışıklı), MenuSky (unlit).
Kalan toon-dışı ve gerekçe:
| Materyal | Neden kalır |
| --- | --- |
| M_OutlineHull | Ters hull kontur, ışık almaz (tasarım gereği düz koyu) |
| M_TunnelCount, LiberationSans SDF (HiddenMark, UnlockLevel) | Metin (TMP SDF); sayı değişken → mesh değil |
| Sprites-Default (AngryIcon) | 2D ikon/billboard |
| M_MenuSky | Gökyüzü arka planı, ışık almaz |
| M_GameBG | Game'de kapalı eski quad; yalnız LevelEditor sahnesinde (editör aracı) |
| EditorVoidMarker | Editör yardımcısı |
| M_Outline (Simple Lit) | Hiçbir yerde kullanılmıyor → R7 silsin |

## Açık / sonraki
- **R1 bitince:** `Toon.shader` R1'in `_Edge*` değişikliğiyle kirli; Toon'un frag'ı `ToonLighting.hlsl`'deki `ToonStep/ToonDiffuse`'u çağıracak şekilde sadeleştirilmeli (aynı matematik, şimdi kopya).
- **R3 bitince:** yeni `Kit_Question.fbx`, `Kit_Rope_1..4.fbx` importer materyali gömülü (remap yok); prefab'ta toon materyal (M_Rope, M_QuestionFill/Line) atandığı Play'de doğrulanmalı.
- R6'nın M_Environment ve M_Water'ı: Toon'a geçerken Simple Lit'ten kalan `_SpecColor` gri 0.5, `_SpecStrength` .55, `_RimStrength` .35 duruyor (bilinçli seçilmemiş olabilir); dekor için spec/rim 0 önerilir — R6/Asen karar versin.
- R7 temizlik: `BackgroundSimpleLit.shader` ve `ScrollingWater.shader` artık kullanılmıyor; `M_Outline.mat` kullanılmıyor. `ZScnImport.cs` varsayılan shader'ı hâlâ "Simple Lit" (tek seferlik üretici; yeniden çalıştırılırsa menü materyallerini geri çevirir).
- Kutlama: `CelebrationReview` aracı bozuk (yukarıda); Asen Complete ekranında otobüs + yolcuların toon göründüğüne bakmalı.

## Asen'in test etmesi gerekenler
1. Menü: otobüs, binalar, ağaçlar iki tonlu; kayan dekor ufukta yumuşak beliriyor, yol şeridi kayıyor.
2. Oyun: zemin/kaldırım renkleri öncekiyle aynı; bordür krem.
3. Level bitir → Complete kutlaması: otobüs ve yolcular toon (Simple Lit parlaması yok), egzoz gri.

## Notlar
- Paylaşılan scratchpad'deki `mcp.py` yanlışlıkla üzerine yazıldı; eski arayüzle (post/session/call/read, CLI) aynı şekilde yeniden yazıldı (pyc'den çıkarıldı).
- Unity MCP 02:00–02:30 arası yanıt vermedi (ping yok); kilit 02:49–03:13 T1b'deydi.
- Play çekimi: Game view 420×710 iken `CameraFitter` kadrajı tahtayı dışarıda bırakıyor; çekimde `LayoutModeController.ApplyViewport(1080,1920)` + `Fit()` ile RT'ye çizildi, sonra geri alındı.

## Commit'ler
- `e01280e` Ortak ToonLighting + zemin/kaldırım/menü shader'ları + 24 materyal

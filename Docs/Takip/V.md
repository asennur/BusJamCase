# V — Doğrulama (V4 level geçişi, V2b T2 dokunuş, V3 bellek/performans)

Durum: **bitti** (22.11)

## Adımlar
- [x] 0. Erişim: MCP HTTP `execute_code` (CodeDom), Console başta temiz. Game view 1080×1920'ye alındı.
- [x] 1. V4 level geçişi testleri — 1 sorun bulundu, düzeltildi `61ecb1d`
- [x] 2. V2b T2 dokunuş testleri — geçti
- [x] 3. V3 bellek ölçümü (A / 5 / 10) — sızıntı yok
- [x] 4. V3 kare süresi + GC.Alloc/kare — oyun kodu 0 B/kare
- [x] 5. Sonuç (aşağıda)

## Yöntem
- Sürücü: `EditorApplication.update`'e bağlı zaman çizelgeli editör kodu (scratchpad, repo dışı). Her editör tikinde durum loglanır: `GameState`, level, `InputLocked`, `fadeAction`, overlay opaklığı, `unscaledDeltaTime`.
- Buton: `panel.Pick` + panel'e PointerDown/PointerUp (picking gerçek; overlay buton üstündeyse olay overlay'e gider).
- Ekran dokunuşu: `InputHandler.IsOverUI` (reflection, gerçek kod) → UI değilse `ScreenTapped` çağrılır (= `HandleTap` ile aynı dal). `Input.GetMouseButtonDown` editörden taklit edilemedi (GameView'e SendEvent Input'u beslemiyor).
- Ekran görüntüsü: GameView render texture'ı, bir kare gecikmeli. `V-shots/V4_*.jpg` (her karede durum ve kararma opaklığı yazılı).
- PlayerPrefs (LevelIndex 17, Coins 425, Lives 4, regen zamanları) baştan yedeklendi, sonda geri yazılacak.

## V4 sonuçları
| Test | Sonuç | Kanıt |
|---|---|---|
| Start→Play kararma/açılma | **Kaldı → düzeltildi**. Oyuna girişteki ilk buton dokunuşu karesi ~0.37–0.43 sn takılıyor (editörde ilk UI olayı); fade-out DOTween'e bu tek dt ile girip tek karede bitiyordu (kararma görünmüyor, ekran siyaha atlıyor). Sonraki geçişler pürüzsüz (0.3 sn, 18 kare). Düzeltme: fade-out dokunuştan 2 kare sonra başlar (`61ecb1d`). Sonra: dt=0.424 takılmaya rağmen opaklık 0→.01→.03…1 kademeli. | `V4_play.jpg`, log s3 |
| Açılma sırasında dokunuş oyunu başlatmıyor | Geçti. Fade-out/karanlık/fade-in boyunca 5 dokunuş: hepsi `UI:level-fade`, durum Ready'de kaldı; kilit fade bitince kalktı, sonraki boş alan dokunuşu Playing'e geçirdi. | log s1/s3 |
| Play'e çift tık tek geçiş | Geçti. 2. ve 3. tık `picked=level-fade`, `fadeAction` tek; level değişmedi. | log s1/s3 |
| Win→Continue | Geçti. Tek tık: toplama 1.8 sn → kararma → L18→L19 Ready, kilit açık. Çift tık: 2. tık toplamayı atlar (tasarım), yine tek geçiş (L19, L20 değil). | `V4_next.jpg` |
| Fail→Retry | Geçti. Retry ×2 + X aynı anda: tek geçiş, aynı level, can fail'de bir kez düştü. | `V4_retry.jpg` |
| Pause→Restart | Geçti. Onay Yes ×2: 2. tık overlay'e gitti; timeScale 0 iken unscaled fade çalıştı; aynı level Ready, can −1. | `V4_restart.jpg` |
| Pause→Main Menu | Geçti. Start ekranı, can −1, aynı level; ardından Play yine kararmalı geçti. | `V4_menu.jpg` |
| Console | Temiz (hata/uyarı yok). | |

Gözlem (düzeltilmedi, karar Asen'in): Win'de toplama sonunda complete paneli tamamen soluyor, kararma ancak sonra başlıyor → eski level ~0.1 sn çıplak görünüyor (`V4_next.jpg` 2. kare). İstenirse kararma CollectionFade ile örtüştürülebilir.

## V2b sonuçları
| Test | Sonuç |
|---|---|
| Ready'de booster barı / banner dokunuşu | Geçti: `IsOverUI` true (`hud/booster-bar`, `hud/ad-banner`), durum Ready kaldı, yolcu raycast'i yapılmadı. |
| Playing'de ayar butonu | Geçti: dokunuş butona gitti (pause açıldı), dünyaya geçmedi. |
| Ready'de boş alana (grid, 540,1000) dokunuş | Geçti: `world` → Playing. ready-overlay/ready-dim/tutorial dokunuşu yutmuyor. |
| Level kartı (picking Ignore) | Dünyaya geçiyor (tasarım: kart tıklanabilir değil). |

## V3 bellek (editör Play, Game view 1080×1920; T3-OLCUM)
Döngü: Start'ta ölçüm A → Play → 10 kez (dokun→Playing, `gm.Complete()`, Continue tıkları, kararma, sonraki level Ready). L18→L28.

| Ölçüm | A (Start) | 5. geçiş sonrası | 10. geçiş sonrası |
|---|---|---|---|
| GC.GetTotalMemory(true) MB* | 710.8 | 680.5 | 674.5 |
| Profiler TotalAllocated MB | 555.3 | 552.3 | 551.6 |
| Profiler TotalReserved MB | 1315.4 | 1315.4 | 1315.4 |
| Material | 207 | 207 | 207 |
| Texture2D | 1553 | 1553 | 1553 |
| RenderTexture (adet / MB)* | 28 / 483.8 | 28 / 483.8 | 28 / 483.8 |
| Mesh | 83 | 87 | 88 |
| GameObject (sahne) | 1664 | 1809 | 1849 |
| DOTween aktif / oynayan | 11 / 1 | 22 / 1 | 26 / 1 |
| VisualElement (UIDocument kökü) | 253 | 253 | 253 |
\* Editör sayıları: GC yığını ve RT'ler editörü de içerir (Scene/Game view RT'leri); eğilim anlamlı, mutlak değer değil.

- Artan sayaçlar (Mesh, GameObject, tween) **havuz büyümesi**: BoardView `ObjectPool`'ları ve yolcu başına bir kez yaratılan, tekrar kullanılan `cloakTween` (paused, `Transform:Cloak` ×10) daha büyük level'larda yüksek su seviyesine çıkıyor. Kanıt: aynı level'da `gm.Retry()` ×20 → tüm sayaçlar sabit (GO 1849, mesh 88, tween 26, VE 253, material 207). Sızıntı yok.
- SVG UI dokuları (`AntialiasedSvgImporter`, build target Android): 59 doku, hepsi **ASTC 4x4** + mip. Editörde `GetRuntimeMemorySizeLong` toplamı 118.9 MB (editör ASTC'yi açtığı için şişkin); cihazda tahmini 66.5 MB (1 B/px × 4/3 mip). En büyükler: result_rays 2048² (cihazda ~5.6 MB), surface_lives_modal / surface_result_modal 1229×2048, title_failed 2048×1163, surface_result_inner 1536×1475, logo_constructed 2048×1024. Not: hepsi USS'den referanslı olduğundan büyük olasılıkla birlikte yüklü; 2048'lik başlık/ışın dokuları 1024'e inerse ~40 MB kazanılır (karar Asen'in, düzeltilmedi).

## V3 kare süresi / GC (Profiler hiyerarşisi, yalnız `PlayerLoop` alt ağacı; editör döngüsü hariç)
| Faz | Kare | PlayerLoop CPU ort / maks | Kare süresi ort / p95 / maks | PlayerLoop GC.Alloc |
|---|---|---|---|---|
| Playing L18, dokunuşsuz (4 sn) | 233 | 2.37 / 4.33 ms | 17.1 / 18.0 / 125.5 ms (tek takılma) | 994 B/kare (p50 992, maks 1078) |
| Playing → süre bitti → Fail (6.2 sn) | 366 | 2.73 / 4.65 ms | 16.8 / 17.8 / 41.7 ms | 1132 B/kare (p50 992, maks 9136 = Fail ekranı karesi) |
- Kare hızı 60 fps sabit (`targetFrameRate=60`).
- Her karedeki ~992 B'ın kaynağı (10 kare, self GC): 624 B `URP UniversalRenderer.CreateCameraRenderTarget` + 368 B `GUI.Repaint / GUIUtility.BeginGUI` (projede runtime OnGUI yok; editör/URP kaynaklı). **Oyun script'lerinden kare başı allocation: 0 B.**
- Yolcu dokunuşlu ölçüm alınamadı: ölçüm sırasında editör odağı başka ajanın Device Simulator penceresine geçti, oyun karesi durdu ve oyun durumu dışarıdan değişti (bkz. Sorunlar).

## Sorunlar / notlar
1. **Düzeltildi `61ecb1d`** — Start→Play'de ilk dokunuş karesindeki takılma (editörde ~0.4 sn) kararmayı tek karede bitiriyordu. Fade-out artık dokunuştan 2 kare sonra başlıyor (fade-in'deki bekleme ile aynı mantık). Doğrulandı: takılmaya rağmen 18 karelik kademeli kararma; Continue/Retry/Restart/Main Menu yeniden test edildi, hepsi geçti.
2. Gözlem: Win sonunda complete paneli tamamen soluyor, kararma sonra başlıyor; eski level ~0.1 sn görünüyor. Öneri: kararmayı CollectionFade ile örtüştürmek.
3. SVG UI dokuları cihazda ~66.5 MB (yukarıda).
4. Ortam: ölçümün sonunda editör başka ajanlarca kullanılmaya başladı (Device Simulator odağı, Game view kapalı). Game view boyutu eski seçime (index 18) geri alınamadı — Game view penceresi yok. PlayerPrefs geri yüklendi (LevelIndex 17, Coins 425, Lives 4, regen zamanları). Sahne kaydedilmedi.

## Commit'ler
- `61ecb1d` Kararmayı dokunuştan iki kare sonra başlat (ToolkitGameUI.Transition.cs)
- Takip: V.md + V-shots (bu commit)

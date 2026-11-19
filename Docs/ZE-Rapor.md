# ZE — Model, prefab ve sahne kurulumu

19 Kasım 2026. Unity **2022.3.19f1**, BusJamCase; tüm Unity asset/prefab/sahne değişiklikleri unityMCP üzerinden yapıldı. Oyun C# kodu değiştirilmedi. Sonraki pakete geçilmedi.

## Kurulum ve dosyalar

| Dosya / grup (`Assets/_BusJam/` altında) | Yapılan ve gerekçe |
| --- | --- |
| `Art/Models/Yolcu/Yolcu.fbx`, `.meta` | Mevcut FBX teslimata eklendi. Generic rig, Root, External materials. Idle/Run/Wave loop, Sit tek sefer. |
| `Art/Models/Yolcu/Yolcu_Animator.controller` | Idle/Run/Sit/Wave; default Idle, geçiş yok. Hazır Passenger kodunun CrossFade çağrıları kullanılıyor. |
| `Prefabs/Passenger.prefab` | Primitive gövde ve kol yerine Yolcu; Animator ve SkinnedMeshRenderer bağlandı. Model child ölçeği 1.3: yaklaşık 0.60 hücre genişliği. CapsuleCollider korundu ve modele uyarlandı. Outline ikinci SkinnedMeshRenderer: aynı mesh, kemikler ve rootBone. Eski strideHeight/waveArm alanları kodda duruyor; primitive kol kaldırıldığı için waveArm boş. |
| `Materials/M_OutlineHull.mat` | BusJam/OutlineHull, Thickness 0.015. Mesh ölçeğini büyütmek yerine animasyonlu silueti normal yönünde genişleten hazır shader kullanıldı. |
| `Art/Models/Otobus/Otobus.fbx`, `.meta` | Mevcut FBX teslimata eklendi. Generic rig, Root, External materials; Idle/Drive loop. On materyal yuvası korundu, 0 = M_BusBody. |
| `Art/Models/Otobus/Otobus_Animator.controller` | Idle/Drive/Brake/DoorOpen/DoorClose; default Idle, geçiş yok. |
| `Prefabs/Bus.prefab` | Model, Animator ve body bağlandı. seatPoints doğrudan Seat_0/1/2 kemikleri; seatIndicators boş. Brake 0.6 sn, DoorClose 0.3333333 sn (import edilen klip süreleri). |
| `Materials/M_BusBody`, `M_BusGlass`, `M_BusGlassHi`, `M_BusLight`, `M_BusTail`, `M_BusTrim`, `M_BusTire`, `M_BusHub`, `M_BusSeat`, `M_BusRail` `.mat` | FBX renkleriyle dış URP/Simple Lit materyaller. Gövde rengi çalışma zamanında değişirken diğer dokuz yuva korunuyor. |
| `Materials/M_Yolcu.mat` | FBX önizlemesi için dış Simple Lit materyal. Oyun içinde mevcut renk materyalleri atanıyor. |
| `Art/Models/BoardKit/Kit_Tile`, `Kit_Hole`, `Kit_Rim_Straight`, `Kit_Rim_Outer`, `Kit_Rim_Inner`, `Kit_Tunnel`, `Kit_SlotTile`, `Kit_SlotLocked` `.fbx` ve `.meta` | Sekiz mevcut model teslimata eklendi; Scale 1, External materials. GUID'ler korundu. |
| `Materials/M_KitBase`, `M_KitTile`, `M_KitRim`, `M_KitHole`, `M_KitTunnel`, `M_KitTunnelInside`, `M_KitLocked`, `M_KitLockIcon` `.mat` | Sırasıyla açık gri-lila, beyaz, krem, koyu lila, açık lila, koyu lacivert, mor, beyaz; URP/Simple Lit. |
| `Prefabs/Tile.prefab`, `Obstacle.prefab` | Tile → Kit_Tile; Obstacle → yalnız Kit_Hole. Eski kök/GUID korundu. |
| `Prefabs/RimStraight.prefab`, `RimOuter.prefab`, `RimInner.prefab` | Model child içeren üç yeni modüler çerçeve prefab'ı. |
| `Prefabs/Tunnel.prefab` | Kit_Tunnel, kameraya dönük Badge, beyaz quad ve dünya TMP metni. TunnelView.badge/remainingText bağlı. Ağız +Z; model child yaw 0°. |
| `Prefabs/SlotTile.prefab` | Kök SlotTile; açık ve kilitli kit modelleri ile kilit altındaki TMP bağlandı. Child ölçekleri 0.9: mevcut 0.85 slot aralığında çakışmıyor. |
| `Scenes/Game.unity` | BoardView rim referansları/yaw; WaitingArea yeni SlotTile bileşenine bağlı, lockIconPrefab boş. Otobüs model uzunluğu yaklaşık 4.12 olduğundan queueSpacing 4.4 yapıldı. Background Canvas eklendi. Önceden silinmiş bağımsız AngryIcon geri getirilmedi. |
| `Scenes/LevelEditor.unity` | Aynı BoardView prefab ve yaw bağlantıları. Unity kaydederken kodda artık bulunmayan eski `slots` serialized alanı doğal olarak çıktı. |
| `Art/Textures/GameBG.png.meta` | Sprite/Single; mipmap kapalı. Görsel dosyanın kendisi değişmedi. |
| `Docs/ZE-Captures/*.png`, bu rapor | 1080×1920 gerçek Play kayıtları ve doğrulama sonuçları. |

Yeni asset'lerin ve klasörlerin `.meta` dosyaları da teslimata dahildir. `ArtSource/` hariç tutuldu. Önceden değiştirilmiş `M_Purple.mat` ve `M_Yellow.mat` içeriğine dokunulmadı ve commit'e alınmadı. Game.unity'nin önceki değişiklikleri yeni sahne kaydında korundu; yalnız ZE'nin açıkça boş istemesi nedeniyle lockIconPrefab bağlantısı kaldırıldı.

## Bulunan değerler

| Alan | Son değer |
| --- | --- |
| İki sahnede rimStraightYaw / rimOuterYaw / rimInnerYaw | **180° / 180° / 180°** |
| Bus içindeki Otobus model child yaw | **180°**; ön +X, kapı slotlara doğru −Z |
| Passenger.seatRotation | **(270.02°, 180°, 0°)**; koltukta dünya rotasyonu yaklaşık (0.02°, 90°, 0°) |
| Yolcu model child ölçeği | **1.3** |
| HiddenMark localPosition / rotation | **(0, 1.25, 0.12)** / **(65°, 0°, 0°)** |
| AngryIcon localPosition / rotation | **(0, 1.36, 0)** / **(65°, 0°, 0°)** |
| Outline Thickness | **0.015** |
| BusStation.queueSpacing | **4.4** |
| Tunnel model child yaw | **0°** |

Background: Screen Space – Camera, Main Camera, Plane Distance **99** (far clip 100), Sort Order **−10**. CanvasScaler 1080×1920, match 0.5. Image preserveAspect kapalı, raycast kapalı; envelope anchorMin **(0, −0.52)**, anchorMax **(1, 1.08)**, offset'ler 0. Yol ve kaldırım hizası bu kadrajda görsel olarak kontrol edildi.

## Play sonuçları

Testlerde gerçek oyun bileşenleri ve DOTween/Animator çalıştı. Girdi çakışmasını önlemek için InputHandler yalnız çalışma zamanında geçici kapatıldı; dokunma, GameplayController'ın mevcut handler'ına MCP ile gönderildi. Geçici gözlemciler yalnız bellekte çalıştı; projeye test scripti eklenmedi. Bekleme anlarında Time.timeScale durduruldu, sonra 1'e geri alındı.

| Kontrol | Gerçek sonuç |
| --- | --- |
| Grid Idle → dokunma Run → slot Idle | **Geçti.** Level_04 Blue (2,0) InSlot; Animator Idle. [Run](ZE-Captures/ZE-Passenger-Run.png), [slot](ZE-Captures/ZE-SlotIdle.png). |
| Otobüste Sit ve yön | **Geçti.** Red (0,2) Seated, Animator Sit; dünya yaw yaklaşık 90°. |
| Son otobüste Wave | **Geçti.** Üç yolcu Seated iken Animator Wave; animasyon en az 0.2 sn ilerledikten sonra görüntü alındı. [Wave](ZE-Captures/ZE-FinalWave.png). |
| Erişilebilir yolcu outline | **Geçti.** Siyah hull yalnız erişilebilir yolcularda; animasyonla aynı kemikleri kullanıyor. [Level_05](ZE-Captures/ZE-Level05.png). |
| Gizli yolcu gri + ? | **Geçti.** Level_05 (2,3) IsHidden, işaret aktif ve kameraya dönük. [Görsel](ZE-Captures/ZE-Level05.png). |
| Bus Drive → Brake → kapı açılışı | **Geçti.** Animator durumları canlı gözlendi. [Drive](ZE-Captures/ZE-Bus-Drive.png), [Brake](ZE-Captures/ZE-Bus-Brake.png), [DoorOpen](ZE-Captures/ZE-Bus-DoorOpen.png). |
| Dolunca kapı kapanışı → kalkış | **Geçti.** DoorClose canlı görüldü; ardından otobüs ayrıldı ve Blue geldi. [DoorClose](ZE-Captures/ZE-Bus-DoorClose.png). |
| Otobüs renkleri | **Geçti.** Yedi renk için body slot 0 doğru materyal; diğer 9 yuva ve toplam 10 materyal korundu. Red/Blue görüntülerde görüldü. |
| Level_04 çukur ve çerçeve | **Geçti.** Kit_Hole, karo olmadan; 180/180/180 ile kesintisiz çerçeve. [Level_04](ZE-Captures/ZE-Level04.png). |
| Level_05 iç köşeler, tünel ve sayı | **Geçti.** Çerçeve kesintisiz. (2,3) ağzı boşalınca sayaç **3 → 2**, yeni ağız yolcusu Purple. [Önce](ZE-Captures/ZE-Level05.png), [sonra](ZE-Captures/ZE-Level05-tunnel-2.png). |
| Slot kilitleri | **Geçti.** 1 ve 49: iki uç kilitli, LVL 50/LVL 100. 50 ve 99: yalnız sağ kilitli. 100: ikisi açık. Gerçek WaitingArea.Setup/SlotTile görünümleri kontrol edildi. |
| Arka plan | **Geçti.** Grid, slot ve otobüs arkasında; Playing ve Complete görüntülerinde görünür. |
| LevelEditor sahnesi | **Geçti.** Tüm yedi BoardView prefab referansı dolu; Play'de grid kuruldu, Console hata/uyarı 0. [Görsel](ZE-Captures/ZE-LevelEditor.png). |
| Sahne doğrulama | **Geçti.** Game ve LevelEditor: missingScripts 0, brokenPrefabs 0, toplam sorun 0. |
| Derleme / Console | **Geçti.** Oyun C# derleme hatası 0. Refresh/domain reload sırasında MCP paketinden `WebSocket is not initialised` uyarısı oluştu; bağlantı geri geldi. Uyarı kayda alınıp Console temizlendikten sonraki son kontrol: hata/uyarı **0**. Oyun kodunda düzeltme turu gerekmedi. |

## Açık kalanlar ve kapanış

- ZE kurulumu ve yukarıdaki ana Play matrisi tamamlandı. Fiziksel cihaz/build testi yapılmadı; bu paketin kapsamı Editor Play idi.
- Complete ekranındaki beyaz başlık beyaz karolar üzerinde düşük kontrastlı; mevcut UI tasarımı ZE kapsamında değiştirilmedi.
- Testteki level kaydı başlangıç değerine (**LevelIndex 49**) geri alındı. Son başarılı durum sorgusu: Game sahnesi açık, Play kapalı, timeScale 1; sahnedeki InputHandler etkin. Ardından ek Start/Fail arka plan kontrolü için Play istenirken MCP tekrar koptu; bu ek kontrol ve son Play kapanışı teyit edilemedi. Stop ve kayıt geri yükleme komutlarına da yanıt alınamadı. Asset/sahne değişikliklerinin tamamı bu kopmadan önce kaydedilmiş ve doğrulanmıştı.
- Bilgisayar kullanımıyla Unity penceresi tanılama denemesi araç tarafından onaylanmadı. Bu yol kullanılmadı; tüm kurulum ve kalan testler unityMCP ile tamamlandı.
- Push yapılmadı.

**Editörde yapılacaklar:** Ek prefab/Inspector kurulumu yok. MCP bağlantısı geri geldiğinde son Play durumunu kontrol etmek gerekiyor; bu oturumda komutlar yanıtsız kaldı.

**Test et:** Yukarıdaki Play matrisi uygulandı. Tekrar kontrol için Unity 2022.3.19f1 ile Game sahnesini 1080×1920 açıp Level_04/05 görüntülerini ve slot eşiklerini karşılaştır; ek kurulum gerekmiyor.

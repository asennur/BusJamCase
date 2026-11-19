MCP KOPTU (ZF-T) — Level_01 yükleme aşamasında, madde 1 öncesinde kalındı.

# ZF + ZF-T doğrulama raporu

19 Kasım 2026. **Commit ve push yapılmadı.** ZF kodu, prefab ve sahne değişiklikleri diskte korunuyor. ZF-T sırasında üretim kodu, prefab veya sahne dosyası değiştirilmedi; yalnız bu rapor güncellendi.

## Bağlantı ve gerçek son durum

Önceki ZF kopması: çözüldü; ZF-T başlangıcında MCP bağlı ve editör hazır olarak doğrulandı.

ZF-T sırasında yeniden kopma oluştu. Başlangıçta `mcpforunity://custom-tools` ve `mcpforunity://editor/state` başarılıydı: Unity **2022.3.19f1**, proje **BusJamCase**, açık sahne `Assets/_BusJam/Scenes/Game.unity`, Play kapalı, derleme sürmüyor. Stop gerekmiyordu.

Salt okunur sorguda **Game View 1080×1920**, **SaveSystem.GetLevelIndex() = 49** doğrulandı. İlk sorgu CodeDom'un dönüş değeri istemesi nedeniyle derleme hatası verdi; `return` eklenen tek yeniden deneme başarılı oldu. Bu, proje scriptlerinin derleme hatası değildir.

`manage_editor(play)` başarılı yanıt verdi. Ardından yalnız Level_01 hazırlığı için aşağıdaki kısa `execute_code` çağrısı yapıldı; StartPlaying bu çağrıya eklenmedi:

```csharp
var gm = UnityEngine.Object.FindObjectOfType<BusJam.Core.GameManager>();
UnityEngine.Object.FindObjectOfType<BusJam.Gameplay.InputHandler>().enabled = false;
typeof(BusJam.Core.GameManager)
    .GetField("currentLevelIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .SetValue(gm, 0);
gm.LoadNext();
return gm.CurrentLevel.name;
```

Çağrı yaklaşık 32 saniye sonunda `success:false, message:null, data:null` döndürdü. **Aynı çağrı bir kez yeniden denendi ve aynı sonucu verdi.** İkinci başarısızlıktan sonra bağlantı kuralı gereği hemen duruldu: başka MCP çağrısı, Stop, ek test, düzeltme veya commit yapılmadı. Çağrıda bekleme/döngü yoktu; level yükleme ve StartPlaying ayrılmış olmasına rağmen yükleme yanıt vermedi.

Çağrının hangi kısmının çalıştığı bilinmiyor. Son Play durumu, aktif level ve geçici InputHandler durumu teyit edilemedi. **StartPlaying çağrılmadı.** SaveSystem'e bilinçli yazma yapılmadı; başlangıç değeri 49 kesin olarak okundu, kopma sonrası değer yeniden okunamadı.

## Gerçek kabul sonuçları

| Madde | Sonuç | Kanıt / eksik |
| --- | --- | --- |
| 1 — Kaldırım bandında ortalı, büyük ve görünür 7 slot | **Kaldı — doğrulanamadı.** | Level_01 yüklemesi yanıt vermedi; yeni Play görüntüsü yok. |
| 2 — Grid/çerçeve yeşil alanda, kesintisiz taban; Level_04 çukur / Level_05 tünel | **Kaldı — doğrulanamadı.** | Üç levelin yeni Play görüntüleri alınamadı; BoardBase dış köşeleri denetlenemedi. |
| 3 — En az 3 kalkış, her kare body Renderer.bounds, 0 kesişme | **Kaldı — test yapılmadı.** | Geçici ölçüm scripti/log eklenmedi. Kalkış aralığı ölçülmedi, gecikme değiştirilmedi. |
| 4 — Brake sonrası yolcu dokunmadan kapı açık kalıyor | **Kaldı — doğrulanamadı.** | Animator son pozu ve ekran görüntüsü yok. |
| 5 — Doğrudan ve slottan kapı önüne koşu, küçülme/kaybolma, koltukta büyüme | **Kaldı — doğrulanamadı.** | İki yol denenmedi; DoorEntry kapı yüzü hizası ve geçiş anları gözlenmedi. |
| 6 — Slot ve taşma kuyruğunda ekranda yukarı bakış | **Kaldı — doğrulanamadı.** | Görsel yön kontrolü ve görüntü yok. |
| 7 — Koşu 4.8, gerekirse Run 1.2 | **Kaldı — görsel test yapılmadı.** | Diff'te varsayılan/sahne hızı 4.8; ayak kayması gözlenmedi, Run state değiştirilmedi. |

**7 maddenin hiçbiri geçti sayılmadı.** Level_01, Level_04 ve Level_05 kabul matrisi tamamlanmadı. Düzeltme turu kullanılmadı. ZF-T sırasında Console sorgulanamadı; önceki ZF turundaki temiz Console sonucu bu turun doğrulaması olarak sunulmuyor.

## Diskte korunan ZF değişiklikleri

Başlangıçta `git status`, `git diff --stat` ve `git diff` okundu. İlgili ZF değişiklikleri:

| Dosya | Değişiklik ve gerekçe |
| --- | --- |
| `Assets/_BusJam/Scripts/Gameplay/GameplayController.cs` | Hız 4.8; doğrudan koşu DoorEntry'de bitiyor ve EnterBus çağrılıyor; beklemede WaitingArea rotasyonu. |
| `Assets/_BusJam/Scripts/Gameplay/WaitingArea.cs` | Spacing 1.003, hız 4.8; slot ölçeği ve sabit bekleme yönü; otomatik binişte DoorEntry + EnterBus. |
| `Assets/_BusJam/Scripts/Gameplay/BusStation.cs` | Kuyruk hareketine kapı kapanışı + çıkış süresi gecikmesi; IsAtDoor / BusArrived fren sonunda. |
| `Assets/_BusJam/Scripts/Gameplay/Bus.cs` | DoorEntry referansı; ReserveSeat kapıyı açmıyor; Brake sonrası DoorOpen, Idle'a dönüş kaldırılmış. |
| `Assets/_BusJam/Scripts/Gameplay/Passenger.cs` | Kapıda küçülme, kısa görünmezlik, koltukta büyüme; callback belirme sonunda; slot/taşma rotasyonu, havuz görünürlüğü sıfırlama. |
| `Assets/_BusJam/Scripts/Gameplay/BoardView.cs` | Ground hücrelerinin altında KitBase materyalli tek mesh; Void/Obstacle hariç. |
| `Assets/_BusJam/Prefabs/SlotTile.prefab` | Kök ölçeği 1 → 1.18. |
| `Assets/_BusJam/Prefabs/Bus.prefab` | DoorEntry child ve Bus referansı. |
| `Assets/_BusJam/Scenes/Game.unity` | Board/WaitingArea konumları, spacing, runSpeed ve baseMaterial. |
| `Assets/_BusJam/Scenes/LevelEditor.unity` | BoardView.baseMaterial; ayrıca diff'te m_IndirectSpecularColor sıfırlanmış görünüyor, bu turda değiştirilmedi. |
| `Docs/ZF-Captures/ZF-Before.png` | Önceki ZF başlangıç görüntüsü; kabul kanıtı değildir. ZF-T görüntüsü eklenmedi. |
| `Docs/ZF-Rapor.md` | Güncel doğrulama ve bağlantı kesilme kaydı. |

**M_Purple.mat, M_Yellow.mat ve ArtSource/** başlangıçta değişmişti; korunuyor, dokunulmadı, commit'e alınmadı. Kopmadan sonra status/diff yeniden sorgulanmadı.

## Mevcut değerler (görsel kabul yerine geçmez)

| Alan | Değer |
| --- | --- |
| SlotTile kök ölçeği / model child | 1.18 / 0.9 |
| slotSpacing | 1.003 |
| WaitingArea dünya konumu | (0, 0, 1.6) |
| Board dünya konumu | (0, 0, −0.75) |
| BoardBase yerel Y | −0.09; yalnız Ground hücreleri |
| queueSpacing | 4.4 |
| Kuyruk gecikmesi | doorCloseDuration + moveDuration ≈ 0.3333333 + 0.6 = 0.9333 sn |
| DoorEntry yerel konum | (0.38, 0.08, −1.02); ileri +Z, içeri hareket 0.18 |
| Biniş | 0.15 sn küçülme 1 → 0.05; 0.02 sn görünmezlik; 0.2 sn OutBack büyüme |
| Bekleme rotasyonu | WaitingArea.transform.rotation; sahnede identity |
| runSpeed | 4.8 |

## Devam için kalan iş

MCP yeniden bağlandığında önce editör Play durumunu doğrula ve açıksa Stop yap. SaveSystem değerini kontrol et, test sırasında değişmişse **49** yap. Mevcut değişiklikleri yeniden yazmadan yedi kabul testini ve gerçek görüntüleri tamamla. En fazla iki küçük düzeltme turu; her düzeltme sonrası ilgili madde yeniden test edilmeli. Yalnız yedi madde de geçerse ZF + ZF-T tek Türkçe commit, korunacak üç yol hariç; push yok.

Vault güncellemeleri, Last-Session kartı ve `beyin.py sync`, ikinci MCP başarısızlığından sonra yeni iş yapılmaması kuralı nedeniyle uygulanmadı. Luma bu rapor üzerinden işleyebilir.

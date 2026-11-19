# ZM1 — İpli yolcu

19 Kasım 2026 · Unity **2022.3.19f1** · Yalnız ZM1.

Kod, prefab ve Level Editor bağlantıları tamamlandı. Derleme temiz; MCP `execute_code` ile **51 Edit kontrolü geçti**. Play moduna girilmedi. Geçici leveller yalnız bellekte/preview sahnelerinde kuruldu, temizlendi; test asset'i veya test scripti commit'e eklenmedi.

Vault kayıtları okunabildi; Beyin `note-read` ardından yetkili `note-edit` denendi ancak `Gorev-Listesi.md` atomik yazımı **WinError 5 / erişim engellendi** verdi. Protokol gereği doğrudan dosya yazımına geçilmedi; görev/brief, mimari/kod notları ve oturum kartı güncellenemedi. Luma bu rapordaki 51 Edit kontrolü kanıtıyla vault kayıtlarını işler. `py -3 beyin.py sync` başarılı: 174 kayıt, uyarı/çakışma yok.

## Değişen dosyalar ve gerekçe

| Dosya | Yapılan / neden |
| --- | --- |
| `Scripts/Level/CellData.cs` | `byte ropes` sona eklendi; eski assetler 0 okur. |
| `Scripts/Level/LevelData.cs` | İp yalnız Passenger hücresinde; dört yönün başlangıçta dolu komşuları sayılır. Kuyruğu dolu tünelin yürünebilir ağzı başlangıç kurulumu sırasında dolacağı için sayılır; aynı hücre iki kez sayılmaz. Fazla ip koordinatlı hata verir. Bu kontrol tam level çözücüsü değildir; istenen yerel komşuluk kuralını doğrular. |
| `Scripts/Gameplay/Passenger.cs` | `RopesLeft`, `SetRopes(int)`, `RemoveRope()`, serialized `ropeLayers`. Katlar `SetActive` ile kapanır; çözülmede 0.2 sn, 0.12 ölçek punch. Mevcut tween alanı kullanıldı; yeni Update/sistem eklenmedi. `Setup` havuzdan kalan ipleri temizler. |
| `Scripts/Gameplay/BoardView.cs` | Grid yolcusuna verideki ip sayısı uygulanır. Tünelden gelen yolcu `Setup` ile ipsiz başlar. |
| `Scripts/Gameplay/Board.cs` | Flood-fill ve başlangıç yol aramasında ipli yolcu erişilemez; occupant silinmediğinden arkasındaki yolcuya engeldir. |
| `Scripts/Gameplay/GameplayController.cs` | Kabul edilen dokunuşta hücre boşaltılır, dört komşudan birer ip çözülür, tünel ağzı yeniden doldurulur, erişim yenilenir. Ağza geliş ip çözmez; tekrar boşalma çözer. Taşma kuyruğundaki Idle yolcu eski hücresini yeniden boşaltamasın diye occupant kimliği kontrol edilir; aksi halde yanlış ip çözülmesi ve ağızdaki yeni yolcunun silinmesi mümkündü. |
| `Scripts/UI/LevelEditorPanel.cs` | Passenger fırçası için 0–4 dropdown; diğer hücre türlerine 0 yazılır. Gizlilik ayrı seçilir. |
| `Prefabs/Passenger.prefab` | Dört primitive silindir oluşturulup `ropeLayers[0..3]` alanlarına bağlandı. Collider yok; başlangıçta kapalı. Model ve gizli işaret ayrı kalır. |
| `Materials/M_Rope.mat` + `.meta` | Mevcut Simple Lit materyal şablonundan ayrı kahverengi ortak materyal. `_BaseColor=(0.58,0.33,0.13,1)`. |
| `Scenes/LevelEditor.unity` | `Controls/ropes` ve “İp sayısı (0–4)” etiketi oluşturuldu, GridInput/LevelEditorPanel.ropes bağlandı. Mevcut dropdown kopyalandığından TMP/UI stili korunur. |
| `Docs/MIMARI.md` | §3 ipli yolcu, §4 ropes ve doğrulama, §6 editör seçimi; “Asen kuralı, 19 Kasım”. |

Script/prefab/materyal yolları `Assets/_BusJam/` altındadır.

## Editörde tamamlanan bağlantılar

- `Passenger/Rope_1..4`: local konum `(0, 0.4/0.5/0.6/0.7, 0)`, rotasyon `(0,0,0)`, ölçek `(0.48,0.026,0.43)`. Primitive Cylinder yüksekliği 2 olduğundan her kat 0.052 birim kalınlıktadır. `M_Rope`, gölge kapalı, collider kaldırıldı; sıralı serialized referanslar kayıt sonrası tekrar doğrulandı.
- `LevelEditor/Controls/ropes`: 0,1,2,3,4 seçenekleri; üst-orta anchor, sol-üst pivot, anchored position `(80,-374)`. Etiket `(80,-340)`. Otobüs sırası alanının sağındaki boşluk kullanıldı.
- Prefab `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset`, sahne additive açılıp `EditorSceneManager.SaveScene` ile Edit modunda kaydedildi. Asen'e prefab/Inspector kurulumu bırakılmadı.

## Doğrulama

- 2 ip + 2 dolu komşu geçerli; 3 ip + 2 dolu komşu: **`(1,0) ipli yolcu hiç çözülemez: 3 ip, 2 dolu komşu`**. Passenger dışı ip reddedilir.
- Gerçek `OnPassengerTapped` akışı: 2→1 (tek görsel kat, outline yok), kilitliye dokunma (kızgın, Idle, hücre dolu), 1→0 (katlar kapalı, outline açık), sonra normal Running ve hücrenin boşalması.
- Gizli+ipli yolcu ip varken gri/işaretli; son ip ve açık yol ile rengi açılır. İpli yolcu çıkış satırında da erişilemez; dar koridorda arkasını kapatır.
- Tünelin ilk doluşu çözmez; aynı ağzın ardışık iki gerçek boşalması 2→1→0 çözer. Ayrılmış Idle yolcuya tekrar dokunmak ip çözmez, yeni ağız occupant'ını silmez.
- Tek boşalma dört komşunun her birinden bir ip çözer; çapraz yolcu etkilenmez. Boş kuyruk ve çapraz hücre doğrulamada sayılmaz. Sıfırın altına inilmez, havuz tekrar kurulumunda eski ip sayısı kalmaz.
- Kaydedilmiş LevelEditor sahnesinde gerçek `OnPointerClick` ile 0–4 boyama, gizlilikle birleşimi ve Empty'ye boyarken ipin temizlenmesi geçti. Dört prefab referansı/material/parent/collider durumu doğrulandı.
- Mevcut **Level_01–10**: tamamı `Validate=true`, bütün eski hücrelerde `ropes=0`. Kalıcı level verisi değişmedi.
- Derleme sonrası Console hata/uyarı kontrolü temiz. Refresh sırasında MCP'nin kendi yeniden bağlanma denemesi başarılı oldu; bağlantı geri geldi, sonraki Edit komutları çalıştı. İki denemeden sonra kalıcı kopma oluşmadı.

## Asen Play'de kontrol etsin

1. Geçici 3×1 level: üç Red yolcu, ortadaki `ropes=2`, `busOrder=[Red]`; ortadaki ayrıca gizli seçilebilir. İlk dokunuşla oyunu başlat.
2. Sol yolcuyu gönder: ortada tek ip kalsın; ortadakine dokununca kızgın ikon çıksın, yerinden ayrılmasın.
3. Sağ yolcuyu gönder: son ip kısa punch ile kalksın, ortadaki outline alsın; gizliyse rengi açılsın. Dokununca normal gitsin.
4. Tünelli geçici levelde aynı komşu ağız iki kez boşaldığında iki ip çözüldüğünü ve ağza çıkışta ip çözülmediğini görsel olarak kontrol et. Test assetlerini commit etme.

Animasyonun gerçek zamanlı görünümü, silindirlerin hareketli model üzerindeki görünümü ve tam Play akışı bu oturumda doğrulanmadı; yukarıdaki Play kontrolü Asen'de. `M_Purple`, `M_Yellow`, `ArtSource` değişikliklerine dokunulmadı ve commit kapsamına alınmadı. Push yok.

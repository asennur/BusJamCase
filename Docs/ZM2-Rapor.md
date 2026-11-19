# ZM2 — Gizemli (pelerinli) yolcu

19 Kasım 2026 · Unity **2022.3.19f1** · Yalnız ZM2.

Kod, Passenger prefab'ı ve Level Editor bağlantıları tamamlandı. Derleme temiz; MCP `execute_code` üzerinden **349 Edit kontrolü geçti**. Play'e girilmedi. Başlangıçta açık bulunan Play durduruldu; testler geçici preview sahnelerinde yapıldı ve sahneler kapatıldı.

## Önce ZM1 temizliği

- Database'deki `Test_Rope` ve `Test_RopeTunnel` kaldırıldı; kalıcı 10 level kaldı.
- `Assets/_BusJam/Levels/_Test`, meta'sı ve boş kalan `Levels` klasörü/meta'sı kaldırıldı.
- `SaveSystem.SetLevelIndex(49)` uygulandı. ZM2 koduna başlamadan `git diff --exit-code -- Assets/_BusJam/Data/LevelDatabase.asset` temizdi.

## Değişen dosyalar

Yollar aksi belirtilmedikçe `Assets/_BusJam/` altındadır.

| Dosya | Ne / neden |
| --- | --- |
| `Scripts/Level/CellData.cs` | `Cloak : byte { None, StartsOpen, StartsClosed }`, struct'ın sonuna `cloak`. Eski asset'ler None okur. |
| `Scripts/Level/LevelData.cs` | Pelerin yalnız Passenger hücresinde; hidden ile birleşimi ve tanımsız enum değeri koordinatlı hata verir. İp bağımsızdır. |
| `Scripts/Gameplay/Passenger.cs` | `IsCloaked`, `CloakOpen`, `SetCloak(bool)`; Setup sonuna isteğe bağlı Cloak parametresi. Kapalı gövde gri ortak materyal kullanır; açıkta gerçek materyal geri gelir. Pelerin child'ı kısa ölçek geçişi yapar. Tween yolcu başına bir kez oluşturulur, iki yönde oynatılır; Disable'da durur, Destroy'da temizlenir. Havuz kurulumu eski durumu sıfırlar. |
| `Scripts/Gameplay/BoardView.cs` | Hücrenin cloak verisini Passenger.Setup'a aktarır. Tünelden gelenler varsayılan None kalır. |
| `Scripts/Gameplay/GameplayController.cs` | Liste OnLevelLoaded'da bir kez dolar. Her kabul edilen dokunuşun RunTo çağrısından sonra, yalnız Idle ve board occupant kimliği eşleşen pelerinliler değişir. Böylece koşan, slottaki, taşma kuyruğunda bekleyen ve havuzda yeniden kullanılan normal yolcular etkilenmez. Kapalı dokunuş ShowAngry ile erken döner. |
| `Scripts/UI/LevelEditorPanel.cs` | Üç seçenekli pelerin fırçası. Hidden+pelerin boyaması hücreyi değiştirmeden hata verir. Passenger dışı boyamada pelerin temizlenir. |
| `Prefabs/Passenger.prefab` | Model gövdesine ve başına oturan dört collider'sız primitive child, serialized alanlar. Mevcut ip katları korundu. |
| `Materials/M_Cloak.mat`, `M_CloakShade.mat` + meta'lar | Mevcut Simple Lit materyalinden ayrı paylaşılan gri pelerin ve koyu kukuleta açıklığı materyalleri. |
| `Scenes/LevelEditor.unity` | Controls/cloak ve Pelerin etiketi; panel referansı bağlı. Eski YAML objelerinde yalnız parent çocuk listesi ve yeni serialized alan değişti; mevcut objeler silinmedi. |
| `Docs/MIMARI.md` | §3 pelerin ve dokunma, §4 veri/doğrulama, §6 fırça; “Asen kuralı, 19 Kasım”. |

`Board` değişmedi: yol erişimi mevcut geometri/ip kurallarıyla hesaplanır; pelerin dokunma ve outline kapısıdır. Bu sayede yolu kapalı pelerin de diğer kabul edilen dokunuşlarda görsel durumunu değiştirebilir.

## Editörde yapılacaklar — tamamlandı

1. `Passenger/Cloak`: local konum `(0,0.65,0)`, ölçek `(1,1,1)`, varsayılan inactive. Passenger alanları: `cloakVisual=Cloak`, `cloakMaterial=M_Cloak`, `cloakDuration=0.18`.
2. Cloak altındaki primitive'ler (konum child'a göre):

   | Child / primitive | Local konum | Local ölçek | Materyal |
   | --- | --- | --- | --- |
   | Mantle / Capsule | `(0,-0.14,0)` | `(0.45,0.31,0.33)` | M_Cloak |
   | Hood / Sphere | `(0,0.24,0)` | `(0.41,0.45,0.41)` | M_Cloak |
   | Collar / Cylinder | `(0,0.08,0)` | `(0.39,0.035,0.34)` | M_Cloak |
   | HoodOpening / Sphere | `(0,0.22,0.181)` | `(0.255,0.29,0.065)` | M_CloakShade |

   Collider'lar kaldırıldı; gölge üretimi kapalı. Gövde griye döndüğü için örtünün dışında kalan uzuvlardan gerçek renk sızmaz. Kukuleta merkezi yolcu köküne göre y=0.89, üstü yaklaşık 1.115; modelin baş yüksekliğine oturur. Statik Edit render'ında ölçü ve görünüm incelendi.
3. `M_Cloak._BaseColor=(0.42,0.44,0.47,1)`; `M_CloakShade._BaseColor=(0.13,0.15,0.18,1)`.
4. `LevelEditor/Controls/cloak`: TMP dropdown, seçenekler `Yok / Açık başlar / Kapalı başlar`, anchored position `(80,-466)`. `CloakLabel`: Pelerin, `(80,-432)`. GridInput/LevelEditorPanel.cloak bağlı. Mevcut ip seçiminin altında boş alan kullanıldı.

Prefab `LoadPrefabContents/SaveAsPrefabAsset`, sahne Edit modunda `EditorSceneManager.SaveScene` ile kaydedildi. Inspector işi bırakılmadı.

## Doğrulama

- 349 kontrol: veri ve gerçek dokunma akışı, prefab/UI kontrolleri; ayrıca 10 eski levelin Validate sonucu ve 285 eski hücrenin cloak=None olması bu sayıya dahildir.
- Biri açık/biri kapalı kurulum, normal dokunuşta tersine dönme, kapalıya dokununca kızgın ve değişmeme, açık yolcu ayrılırken kendisinin etkilenmemesi geçti.
- Açık ama yolu kapalı yolcu kızar; diğer kabul edilen dokunuşta yine pelerin değiştirir. Outline yalnız açık+erişilebilir+ipsiz olanda.
- İp+pelerin: ipliyken açık olsa da reddedilir; komşu ayrılınca ip çözülür ve pelerin ayrıca kapanır; sonraki kabul edilen dokunuşla açılır. Hidden+pelerin reddedilir.
- Otobüse, slota ve dolu slotların arkasına koşan dokunuşlar sayıldı. Slot açılmayacağı için yok sayılan dokunuş, Start durumu ve grid dışındaki Idle yolcu tekrar dokunuşu sayılmadı.
- Aynı liste ve aynı tween referansları korundu; 1.000 doğrudan pelerin tersine çevirme çağrısında **0 B managed allocation** ölçüldü. Bu ölçüm yeni pelerin akışına aittir; mevcut RunTo/diğer tween kodunun tamamı için allocation iddiası değildir.
- Havuz yeniden kurulumunda başlangıç pelerini, ip ve renk sıfırlandı. Tween ilerlemeden hızlı kapat/aç ve geçişlerin son görselleri geçti.
- Kaydedilmiş LevelEditor sahnesinde gerçek OnPointerClick ile üç pelerin seçeneği+ip, hidden çakışması, Empty'ye boyarken temizleme ve mevcut hidden fırçası geçti.
- Son Console sorgusu hata/uyarı içermedi. Script refresh sırasında MCP'nin kendi tek yeniden bağlanma denemesi başarılı oldu; kalıcı kopma olmadı. Yeniden bağlanmanın WebSocket uyarısı temizlendikten sonra proje kontrolü tekrar temizdi.

Edit testleri gerçek zamanlı Animator/Play görüntüsünü doğrulamaz. Tam Play akışı ve geçişin görsel hissi aşağıdaki hazırlıkla Asen'de kontrol edilecek.

## Test et — commit sonrası geçici Play hazırlığı

Commit'e dahil edilmeyen kurulum: `Assets/_BusJam/Levels/_Test/Test_Cloak.asset`, database'in son girdisi (indeks **10**, ekranda **Level 11**). `SaveSystem` indeksi 10; `Game.unity` açık ve kayıtlı. Süre 120 sn, altı Red, otobüs sırası `[Red, Red]`, Validate=true. Tünel/ip/hidden yok.

```text
           OTOBÜS / ÇIKIŞ
             x0  x1  x2
    y=0      A   N   B
    y=1      N   N   N

A = Red, pelerin AÇIK başlar
B = Red, pelerin KAPALI başlar (gri)
N = normal Red
```

1. Play'e Asen girer; boş alana dokunup oyunu başlatır. A kırmızı/outline'lı, B gri kukuletalı/outline'sız olmalı.
2. Üst ortadaki N'ye dokun: N koşar; A kapanır, B açılır. İkisinin geçişi yaklaşık 0.18 sn.
3. Sol üstteki kapalı A'ya dokun: kızgın ikon; A gitmez, A/B durumları değişmez.
4. Sağ üstteki açık B'ye dokun: B koşar; A açılır. Giden B açık/gerçek renginde kalır.
5. A'yı ve ardından alt sıradaki normalleri gönder: iki Red otobüs dolup gider, normal Complete akışı görülür.

## Geçici kurulumu geri alma

Play'i durdur. Database'den yalnız `Test_Cloak` girdisini kaldır (bu kurulumdan sonra başka database düzenlemesi yapılmadıysa `git restore -- Assets/_BusJam/Data/LevelDatabase.asset` aynı sonucu verir). `_Test` klasörünü/meta'sını sil; Levels tamamen boşsa Levels klasörünü/meta'sını da sil. `BusJam.Save.SaveSystem.SetLevelIndex(49)` uygula. Database tekrar 10 kayıt ve git diff temiz olmalı. Bu temizlik sonraki paket başında Codex tarafından da yapılabilir.

## Vault ve kapsam

AsenOS notları Beyin `note-read` ile okundu; `note-edit` yükseltilmiş komutla da `Gorev-Listesi.md` atomik yazımında **WinError 5 / erişim engellendi** verdi. Doğrudan dosya yazımıyla aşılmadı. Vault'taki görev/brief, mimari/kod notları ve oturum kartını Luma bu raporun 349 Edit kontrolü kanıtıyla işlemeli.

`M_Purple`, `M_Yellow`, `ArtSource` korunur ve commit'e alınmaz. Push yok. Geçici Play leveli ve database değişikliği ZM2 commit'inden sonra hazırlanır ve commit dışında kalır.

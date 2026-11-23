# SK2: Son kontrol (editör kısmı)

Durum: **bitti** (23.11, ~14:00). Kod değişmedi; yalnız test. Teslimi engelleyen hata yok.

Yöntem: MCP HTTP (`execute_code`), Simulator Galaxy S10e (62). Dokunuş P3/P4 yöntemi (`InputHandler` yolu: ekran noktası, `IsOverUI`, raycast, eventler; tüm dokunuşlarda isabet). Editör tıkları `EditorBoardInput.ProcessClick` (karonun ekran noktası, `TryScreenToCell` seçimi dahil), butonlar UI Toolkit `NavigationSubmitEvent`/`Clickable`. Görüntüler `SK2-shots/` (tam ekran jpg, el kırpımları `_crop.png`).

## Ortam yedeği
- Simulator Galaxy S7 (65), Game view boyut 5, sahne Game.unity.
- PlayerPrefs: LevelIndex 3, Coins 75, Lives 5, SoundMuted 0, TutorialSeen_1/2/3=1, 6/8 yok. `.reg` yedek: scratchpad sk2/prefs_backup.reg.

## Adımlar
- [x] 1 Level Editor — GEÇTİ. Sahne açıldı, Console temiz (0 hata/uyarı). L4 yüklendi (01b), (1,1) karo→Hole boyandı, süre 45→52, Save → "Saved as Level 4", Level_04.asset'te timeLimit 52 + type 2 (git diff) (02). Tünel ağzına yolcu: "Tunnel exit must stay empty." ve yerleşmedi (03); taslağa zorla konunca Save → "Cannot save: Tunnel exit must stay empty: remove the passenger at (1, 0)…" (04). New → 3 kırmızı + Red otobüs → Solve "6 solutions found" → Save → Level_29.asset + LevelDatabase'e yeni satır (05). Test Play: kaydedilmemiş süre 77 oyunda 77 göründü, oynanıp Complete (06, 07), Editor butonu taslağı geri getirdi ("Back from Test Play. Your changes are kept"). Asset'ler ve DB git'e döndü (count 28).
  - Not: Test Play'de level bitince Coins gerçek kayda +25 yazıldı (75→100). LevelIndex değişmedi.
  - Not: Solver işi yalnız Unity penceresi odaktayken ilerliyor (Update odak dışında yavaş; editör ayarı, oyuncuyu etkilemez).
- [x] 2 Kayıttan devam — GEÇTİ. LevelIndex 3 ile L4 oynandı, Complete (12); LevelIndex 4 yazıldı. Play'den çıkış → tekrar Play: Start butonu "Level 5", Coins 125 (100 + L4 ödülü; ödül Continue'ya basılmadan çıkışta da kaydedildi), Lives 5 (13). L5 Fail sonrası (Lives 4) çıkış/giriş: Lives 4 korundu.
- [x] 3 Öğreticiler — GEÇTİ (5/5). Anahtarlar silindi, L1→L2→L3 Continue ile, L6/L8 LevelIndex ile.
  - L1: el kırmızı (0,0) başında (20_crop); mavi (1,0) dokunuşu filtrelendi (yolcu kıpırdamadı); (0,0)→el (0,1) (21_hand2), →(0,2) (21_hand3); 3. dokunuşta el gizlendi, otobüs dolunca TutorialSeen_1=1.
  - L2: el önce kırmızı (0,0) (22), dokununca gizli (0,1) açıldı ve el ona geçti (23); dokununca ders bitti, TutorialSeen_2=1. Metin "Hidden passengers show their colour once a path opens."
  - L3: Red/Blue otobüslerde el/ipucu yok. Yeşil otobüs kapıda → el tünel ağzındaki yeşil (2,3) (24). Sonra ağıza çıkan Yellow, Yellow (el takip etti, 25), son Green → ders bitti, TutorialSeen_3=1. (Ağız veride boş; Playing başında kuyruğun ilk yeşili ağza çıkmış durumda.)
  - L6: el pembe (2,0) (26) → ip 2→1, el yeşil (3,1) (27) → ip 0, el gizlendi, TutorialSeen_6=1.
  - L8: el mor (4,0) (28), ilk kabul edilen dokunuşta bitti, TutorialSeen_8=1.
  - Parmak ucu: tüm hedeflerde başın tepesinde (başa birkaç px gömülü, kafa içine/yanına kaymıyor). Ekran ölçümü: L1 (0,0) baş tepesi 470,849 px, el kutusu x 363-541 (merkez ~452, parmak ucu baş merkezinde).
- [x] 4 Reset Progress — GEÇTİ. Start > Settings > Reset Progress > Yes (30): Level 1, Coins 0, Lives 5, TutorialSeen_1/2/3/6/8 hepsi silindi (31). Not: oyun içi (pause) Settings'te Reset yok, yalnız Start ekranındaki Settings'te (tasarım böyle).
- [x] 5 Start/Complete/Fail duman — GEÇTİ. Start (10) → Level butonu → Ready: timer 0:45 görünür, "Tap to start" görünür, 2,5 sn beklemede Ready'de kaldı (11); dokununca Playing. Complete > Continue → sonraki level Ready (L1→L2→L3). L5 yanlış renklerle Fail "No space left!" (14) > Retry → Ready, can ikinci kez düşmedi.
- [x] 6 Console: her adımda hata/uyarı 0.
- [x] Ortam geri yükleme — PlayerPrefs anahtarı silinip `.reg` yedeği içe alındı; Unity içinden teyit: LevelIndex 3, Coins 75, Lives 5, SoundMuted 0, LastRegenUtc/HighWater aynı, TutorialSeen 1/2/3=1, 6/8 yok. Simulator Galaxy S7 (65), Game view 5, odakta. Level_04 + LevelDatabase git'ten, Level_29 silindi (DB 28). Sahne Game.unity, kirli değil, kaydedilmedi. Console temiz. EDITOR.lock silindi.

## Bulgular (hiçbiri teslimi engellemiyor)
1. (Düşük) Test Play'de level bitince coin gerçek kayda eklendi. Adım: Level Editor > Test Play > leveli bitir. Beklenen (MIMARI LE3 "Test Play kaydetmez"): kayıt değişmez. Gerçek: Coins 75→100 (LevelIndex değişmedi). Görüntü 07.
2. (Düşük, editör kullanılabilirliği) Arkasında yolcu duran boş karonun merkezine dokunmak arkadaki yolcuyu seçiyor: L4'te (0,1) karo merkezi → (0,2) yolcu popup'ı açıldı (LE4 "en yakın görünen nesne kazanır" kuralı gereği; karonun görünen kenarına dokununca karo seçilir).
3. (Bilgi) Level Editor Solve/Save işi yalnız Unity penceresi odaktayken ilerliyor (odak dışında Update durur). Oyuncuyu etkilemez.

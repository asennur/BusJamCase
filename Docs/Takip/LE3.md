# LE3 — Save yalnız çözülebilirse, Solve arka planda, Test Play taslakla (ajan-LE3)

Durum: bitti (22.11 ~15:20).

## Adımlar
- [x] 0. Okuma (AGENTS, MIMARI §6, ORTAK-BRIEF, LE1, LE2, panel/solver/autofill/GameManager/EditorReturn)
- [x] 1. Kod
  - `LevelEditorPanel.cs`: Save → Validate → arka plan solver (en az 1 çözüm) → yaz. Çözüm yok: "No solution — level not saved."; sınır (400k durum / 6 sn): "Could not verify solvable (too complex) — not saved.". Yeni level `Level_NN`, `database.Add` (sona), SetDirty + SaveAssets. Solve/Save/Auto Fill: `Task.Run` + `CancellationToken`, taslağın `Instantiate` kopyası üzerinde; `revision` sayacı (her Rebuild'de artar) eski sonucu yok sayar; New/Load/Test Play/sahne kapanışı işi iptal eder. Test Play kaydetmez: taslağın kopyası `EditorReturn.Begin(scene, kopya, levelNo)` ile oyuna gider; taslak + `savedSnapshot` statikte bekler (DontUnloadUnusedAsset), dönüşte `Edit()` ile aynen açılır.
  - `LevelEditorPanel.Toolkit.cs`: `Update` → `PollJob()` (IsCompleted, allocation yok); `SetBusy` Save/Solve/Auto Fill'i kilitler; `SaveDraft` kalktı.
  - `LevelSolver.cs` / `LevelAutoFill.cs`: isteğe bağlı `CancellationToken` (iptal = Limited).
  - `GameManager.cs`: `static TestLevel/TestLevelNumber`; doluysa database yerine o oynanır, Complete LevelIndex'i kaydetmez.
  - `EditorReturn.cs`: test kopyasını sahiplenir; OnDestroy'da `TestLevel`'ı temizler ve kopyayı siler.
- [x] 2. Derleme: Roslyn dış derleme 0 hata; Unity refresh sonrası Console temiz.
- [x] 3. Play testi (14:52-15:15, LE3 kilidi, butonlara NavigationSubmit)
  - New → 1×9, otobüs +Red +Blue +Green, sütun R,R,R,B,B,B,G,G,G → Save: "Saved as Level 27 (Level_27). The solver found a solution." DB 26→27 (son satır), `Level_27.asset` ve LevelDatabase satırı diskte (guid eşleşti).
  - Otobüs sırası G,B,R (çip seç + `<`) → Save: "No solution — level not saved."; Level_27.asset ve LevelDatabase.asset sha1 önce/sonra aynı. (`save_reddedildi.png`)
  - Solve donma ölçümü (Level 25, Unity önde): eski senkron Solve ana thread'i 1146 ms donduruyordu; arka planda Solve ~1 sn sürdü, 61 kare, ort. 16.6 ms, en uzun 18.8 ms. Auto Fill 9×9 (36 yolcu, 1 tünel): ~4.8 sn, 289 kare, ort. 16.6 ms, en uzun 30.5 ms. Çalışırken Solve/Auto Fill/Save pasif (enabledSelf=False).
  - Test Play (kaydedilmemiş G,B,R taslağı): Game "Level 27", `CurrentLevel == TestLevel`, asset değil, sıra G,B,R; LevelIndex 9 değişmedi. (`testplay_taslak.png`)
  - "Editor" butonu → LevelEditor: taslak JSON önce/sonra aynı, `HasUnsavedChanges=True`, "Back from Test Play. Your changes are kept, not saved yet.", New ilk basışta uyarıyor (LE2 mantığı sağlam); TestLevel temizlendi; asset + DB sha1 aynı. (`editor_donus_taslak.png`)
  - 9×9 Solve sonucu: `solve_sonuc_9x9.png`.
- [x] 4. Temizlik: Level_27 silindi, LevelDatabase.asset başlangıç yedeğinden geri (sha1 67f83e2…, Level_26 satırı duruyor), PlayerPrefs LevelIndex=9, Lives=5, Coins=225, LastRegenUtc/LivesUtcHighWater başlangıç değerine. Play kapalı, kilit silindi.
- [x] 5. Commit (aşağıda)

## Bilinen / Asen'e
- "Too complex" yolu (sınır aşımı) kodda var ama gerçek bir levelde denenmedi (elimizdeki en ağır 9×9 da 1 sn altında çözüldü).
- Auto Fill, otobüs yokken otobüs sırası da üretir; durum satırında Auto Grid'in eski "Add buses, then Auto Fill." mesajı kalıyor (önceden de vardı, sonuç Solver satırında).
- Otobüs çip satırında 12 otobüste yatay/dikey kaydırma çubukları görünüyor (önceden de vardı).

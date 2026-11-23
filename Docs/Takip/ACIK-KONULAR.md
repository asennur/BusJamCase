# Açık konular (23 Kasım sabahı, P3 sonrası)

Kaynak: Docs/Takip/P3.md (9a0664f). Hepsi Asen'in kararını bekliyor; hiçbiri uygulanmadı.

## Sorunlar
1. **Eski 6 level tünel kuralına takılıyor (orta).** L3, 5, 7, 8, 9, 10 tünel çıkışında yolcu var; Validate/Solver "Invalid", editörde kaydedilemiyor. Oyunda sorunsuz. L3 öğreticisi ağızdaki yeşil yolcuya dayanıyor. Öneri: kuralı yalnız yeni levellere uygula (eski leveller dokunulmadan kalır).
2. **Level 27 dengesi (düşük, teyit).** 60 sn'de 42 dokunuş; koşu beklenerek 3 denemede de süre bitti. Asen elle oynasın; gerekirse süre artır.
3. **Tünel boşalınca rozet "0" (düşük).** Referansa bak; istenirse boşken gizle.
4. **TunnelView.cs:25 girinti bozuk (önemsiz).** Teslim öncesi düzelt.
- Not: PlayerPrefs Lives 4 (öncesi 5). Test öncesi yedek de 4'tü; muhtemelen Asen'in testi. Asen'e soruldu.

## Performans önerileri (opsiyonel)
1. Karoları level kurulurken tek mesh'e birleştir / tek malzeme: kalabalık levelde draw ~%38 az. Risk orta (havuz + editör aynı kod).
2. Sıkıştırmasız dokular (MenuSky 5,9 MB, EnvWater, MenuAsphalt, MenuWalk…) ASTC: ~10 MB → ~1,5 MB. Risk düşük; gökyüzü bantlanmasına bak.
3. Otobüs havuzunu level başında önceden doldur (L27'de oyun ortası 2 Instantiate). Risk düşük.
4. URP kare başı ~624 B string (bizim kod değil), IMGUI 368 B (editöre özel olası) — kabul.
5. Fizik simülasyonu script moduna (0,19 ms), Animator yavaşlatma — görsel karar, düşük öncelik.

## Luma önerisi
Teslimden önce: Sorun 1 + 4. İstenirse Öneri 2 + 3 (düşük risk). Sonra README, APK build, temiz klon testi, mail metni; video Asen'de.

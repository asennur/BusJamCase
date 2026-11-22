using UnityEngine;

namespace BusJam.Core
{
    /// <summary>Referans telefon düzeni (739×1600 ekran görüntüsü), referans piksel cinsinden. UI ve kamera bunu ekran
    /// yüksekliğine göre ölçekler; referanstan dar ekranlar bunun yerine genişliğe göre küçülür.</summary>
    public static class ScreenLayout
    {
        public const float ReferenceHeight = 1600f;
        public const float ReferenceAspect = 739f / 1600f;
        public const float HudTop = 72f;       // Level kartının üst kenarı
        public const float NotchGap = -17f;    // referans kart durum çubuğu şeridine 17 px taşar
        public const float BannerHeight = 93f; // reklam banner yer tutucusu, alttaki güvenli boşluğun üstünde durur
        public const float BarGap = 26f;       // booster çubuğunun alt kenarı, banner'ın üstünde
        public const float BarHeight = 111f;   // kilitli booster butonları, etiketler dahil

        /// <summary>Bir referans pikselin ekran yüksekliğindeki oranı.</summary>
        public static float Unit(float aspect) => Mathf.Min(1f, aspect / ReferenceAspect) / ReferenceHeight;

        /// <summary>Üst HUD kenarı, üstten ekran yüksekliği oranı olarak; daha derin bir çentik onu aşağı iter (referans kart durum çubuğu şeridine 17 px taşabilir).</summary>
        public static float HudTopFraction(Rect safe, float aspect) =>
            Mathf.Max(HudTop * Unit(aspect), 1f - safe.yMax + NotchGap * Unit(aspect));

        /// <summary>Booster çubuğunun üst kenarı, alttan ekran yüksekliği oranı olarak.</summary>
        public static float BarTopFraction(Rect safe, float aspect) =>
            safe.yMin + (BannerHeight + BarGap + BarHeight) * Unit(aspect);
    }
}

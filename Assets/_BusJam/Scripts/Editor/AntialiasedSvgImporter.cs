#if UNITY_EDITOR
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>İsteğe bağlı SVG raster çıktısı; SVG kaynağı ve paketin kenar genişletmesi korunur.</summary>
[ScriptedImporter(4, new string[0], new[] { "svg" })]
public sealed class AntialiasedSvgImporter : SVGImporter
{
    public override void OnImportAsset(AssetImportContext context)
    {
        SvgType = SVGType.Texture2D;
        FilterMode = FilterMode.Trilinear;
        WrapMode = TextureWrapMode.Clamp;
        SampleCount = 8;
        KeepTextureAspectRatio = true;
        ViewportOptions = Unity.VectorGraphics.ViewportOptions.PreserveViewport;
        GradientResolution = 256;
        AdvancedMode = true; StepDistance = 10000; SamplingStepDistance = 100;
        MaxCordDeviationEnabled = true; MaxCordDeviation = .6f;
        MaxTangentAngleEnabled = true; MaxTangentAngle = 25;
        TextureSize = RasterSize(System.IO.Path.GetFileNameWithoutExtension(context.assetPath));
        base.OnImportAsset(context);
        // Bu paket sürümü yalnız mip 0 üretir; import çıktısı yazılmadan önce mip'ler yeniden kurulur.
        var texture = (Texture2D)context.mainObject;
        var pixels = texture.GetPixels32();
        texture.Reinitialize(texture.width, texture.height, TextureFormat.RGBA32, true);
        texture.SetPixels32(pixels); texture.Apply(true, false);
        // RGBA32 + mip ile UI dokuları ≈ 184 MB; blok sıkıştırma boyutu değiştirmeden ~4 kat küçültür.
        TextureFormat format = CompressedFormat(context.selectedBuildTarget, texture);
        if (format != TextureFormat.RGBA32) EditorUtility.CompressTexture(texture, format, TextureCompressionQuality.Best);
        texture.Apply(false, true);
    }

    // Mobilde ASTC 4x4 (8 bpp, boyut sınırı yok); masaüstünde BC7 yalnız iki kenar 4'ün katıysa.
    private static TextureFormat CompressedFormat(BuildTarget target, Texture2D texture)
    {
        if (target == BuildTarget.Android || target == BuildTarget.iOS) return TextureFormat.ASTC_4x4;
        bool blockAligned = texture.width % 4 == 0 && texture.height % 4 == 0;
        if (blockAligned && (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64
            || target == BuildTarget.StandaloneOSX || target == BuildTarget.StandaloneLinux64)) return TextureFormat.BC7;
        return TextureFormat.RGBA32;
    }

    // Ekrandaki en büyük kullanım (1440x3200 telefon, 2048x2732 tablet) × ~1-1.25; yumuşak ışıklar daha düşük.
    // USS dilim payları texel cinsinden: burada boyut değişirse ilgili -unity-slice-* değerleri de ölçeklenmeli.
    private static int RasterSize(string name)
    {
        if (name.StartsWith("button_")) return 768;
        if (name == "surface_lives_modal") return 1536;
        if (name == "surface_result_modal") return 1024;
        if (name == "surface_result_inner" || name == "surface_result_header") return 512;
        if (name == "panel_tip" || name == "hud_counter") return 512; // hud_counter: USS dilimleri 85/88 texel
        if (name == "result_rays") return 1024;
        if (name == "title_complete" || name == "ribbon_blue") return 1536;
        if (name == "fx_tap_hand" || name == "glow_soft" || name == "coin_pile" || name.StartsWith("icon_heart")) return 512;
        return 256;
    }

    [MenuItem("Bus Jam/Reimport Antialiased SVGs")]
    public static void Configure()
    {
        foreach (string path in System.IO.Directory.GetFiles("Assets/_BusJam/UI/Vector", "*.svg"))
            AssetDatabase.SetImporterOverride<AntialiasedSvgImporter>(path.Replace('\\','/'));
        AssetDatabase.ImportAsset("Assets/_BusJam/UI/Theme.uss", ImportAssetOptions.ForceUpdate);
    }
}
#endif

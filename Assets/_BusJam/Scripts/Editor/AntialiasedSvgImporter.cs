#if UNITY_EDITOR
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>Opt-in SVG raster output, retaining the SVG source and the package's edge dilation.</summary>
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
        // This package version creates only mip zero. Rebuild before the import artifact is serialized.
        var texture = (Texture2D)context.mainObject;
        var pixels = texture.GetPixels32();
        texture.Reinitialize(texture.width, texture.height, TextureFormat.RGBA32, true);
        texture.SetPixels32(pixels); texture.Apply(true, false);
        // RGBA32 + mip ≈ 184 MB of resident UI art; block compression cuts it about 4x without resizing.
        TextureFormat format = CompressedFormat(context.selectedBuildTarget, texture);
        if (format != TextureFormat.RGBA32) EditorUtility.CompressTexture(texture, format, TextureCompressionQuality.Best);
        texture.Apply(false, true);
    }

    // ASTC 4x4 on mobile (8 bpp, no size limit); BC7 on desktop only when both sides are multiples of 4.
    private static TextureFormat CompressedFormat(BuildTarget target, Texture2D texture)
    {
        if (target == BuildTarget.Android || target == BuildTarget.iOS) return TextureFormat.ASTC_4x4;
        bool blockAligned = texture.width % 4 == 0 && texture.height % 4 == 0;
        if (blockAligned && (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64
            || target == BuildTarget.StandaloneOSX || target == BuildTarget.StandaloneLinux64)) return TextureFormat.BC7;
        return TextureFormat.RGBA32;
    }

    // Largest on-screen use (1440x3200 phone, 2048x2732 tablet) times ~1-1.25; soft glows go lower. USS slice
    // margins are in texels, so a size change here must rescale the matching -unity-slice-* values.
    private static int RasterSize(string name)
    {
        if (name.StartsWith("button_")) return 768;
        if (name == "surface_lives_modal") return 1536;
        if (name == "surface_result_modal") return 1024;
        if (name == "surface_result_inner" || name == "surface_result_header") return 512;
        if (name == "panel_tip" || name == "hud_counter") return 512; // hud_counter: USS slices 85/88 texels
        if (name == "result_rays") return 1024;
        if (name == "title_complete" || name == "ribbon_purple" || name == "coin_pile") return 1536;
        if (name == "fx_tap_hand" || name == "glow_soft" || name.StartsWith("icon_heart")) return 512;
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

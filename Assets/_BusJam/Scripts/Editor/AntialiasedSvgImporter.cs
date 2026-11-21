#if UNITY_EDITOR
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>Opt-in SVG raster output, retaining the SVG source and the package's edge dilation.</summary>
[ScriptedImporter(1, new string[0], new[] { "svg" })]
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
        texture.SetPixels32(pixels); texture.Apply(true, true);
    }

    private static int RasterSize(string name)
    {
        if (name.StartsWith("button_")) return 1536;
        if (name == "surface_modal" || name == "surface_result_modal" || name == "surface_lives_modal") return 2048;
        if (name == "surface_white" || name == "surface_result_inner") return 1536;
        if (name == "surface_header" || name == "surface_result_header") return 1024;
        if (name.StartsWith("title_") || name.StartsWith("logo") || name == "ribbon_purple" || name == "panel_tip" || name.EndsWith("rays")) return 2048;
        if (name == "coin_pile") return 1536;
        if (name == "fx_tap_hand" || name == "glow_soft") return 1024;
        return name.StartsWith("fx_") ? 256 : 512;
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

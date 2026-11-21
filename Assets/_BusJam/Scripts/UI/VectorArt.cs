using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    /// <summary>Builds shared, non-pickable USS layers once per visual tree.</summary>
    public static class VectorArt
    {
        private const string TexturePrefix = "BusJam.VectorGradient.v2.";
        private const int GradientHeight = 64;
        private static readonly Dictionary<string, Texture2D> Gradients = RestoreGradients();

        public static void Apply(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(element =>
            {
                if (element.ClassListContains("vector-ready")) return;
                bool button = element.ClassListContains("art-button") || element.ClassListContains("editor-button");
                bool white = element.ClassListContains("counter") || element.ClassListContains("sound-toggle") || element.ClassListContains("confirm-modal");
                bool header = element.ClassListContains("timer-counter") || element.ClassListContains("modal-heading");
                bool badge = element.ClassListContains("level-badge");
                bool modal = element.ClassListContains("modal");
                if (button || white || header || badge || modal)
                    Build(element, button ? (element.ClassListContains("red") ? "red" : element.ClassListContains("yellow") ? "yellow" : "green")
                        : white ? "white" : header ? "header" : badge ? "badge" : "modal");
                if (element is Toggle && element.ClassListContains("sound-toggle"))
                {
                    var mark = element.Q(className: "unity-toggle__checkmark");
                    Build(mark, "blue");
                    var check = Layer(mark, "vector-check");
                    check.AddToClassList("vector-check-stroke");
                }
            });
        }

        private static VisualElement Layer(VisualElement parent, string style)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.AddToClassList(style); parent.hierarchy.Add(layer);
            return layer;
        }

        private static void Build(VisualElement element, string palette)
        {
            if (element == null || element.ClassListContains("vector-ready")) return;
            element.AddToClassList("vector-ready");
            var rim = new VisualElement { pickingMode = PickingMode.Ignore };
            rim.AddToClassList("vector-art"); rim.AddToClassList("vector-" + palette);
            element.hierarchy.Insert(0, rim);
            if (palette == "green" || palette == "red" || palette == "yellow")
            {
                rim.AddToClassList("raster-button-art");
                // Unity 2022's USS validator treats this float property as a length; assign it once here.
                rim.style.unitySliceScale = element.ClassListContains("editor-button") ? .18f : .390625f;
                return;
            }
            var edge = Layer(rim, "vector-edge");
            var body = Layer(edge, "vector-body");
            var face = Layer(body, "vector-face");
            face.style.backgroundImage = new StyleBackground(Gradient(palette));
            Layer(face, "vector-highlight");
            Layer(face, "vector-glint");
        }

        // DontSave textures survive editor domain reloads; recover them instead of creating duplicates.
        private static Dictionary<string, Texture2D> RestoreGradients()
        {
            var cache = new Dictionary<string, Texture2D>(8);
            foreach (var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
                if (texture.name.StartsWith(TexturePrefix, System.StringComparison.Ordinal)
                    && texture.width == 1 && texture.height == GradientHeight
                    && texture.format == TextureFormat.RGBA32 && texture.mipmapCount == 1
                    && texture.hideFlags == HideFlags.DontSave)
                    cache[texture.name.Substring(TexturePrefix.Length)] = texture;
            return cache;
        }

        private static Texture2D Gradient(string palette)
        {
            if (Gradients.TryGetValue(palette, out var cached) && cached != null) return cached;
            Color top, middle, bottom;
            switch (palette)
            {
                case "red": top = Hex(0xF53136); middle = Hex(0xE1020A); bottom = Hex(0x800203); break;
                case "yellow": top = Hex(0xFDF158); middle = Hex(0xFDE906); bottom = Hex(0xED9702); break;
                case "green": top = Hex(0xA5EE3C); middle = Hex(0x71DB06); bottom = Hex(0x219600); break;
                case "header": top = Hex(0x0E4090); middle = Hex(0x01286E); bottom = Hex(0x011444); break;
                case "modal": top = Hex(0x0D73FA); middle = Hex(0x095DF3); bottom = Hex(0x0543E1); break;
                case "blue": top = Hex(0x219BFC); middle = Hex(0x015DF3); bottom = Hex(0x0032CD); break;
                default: top = Hex(palette == "badge" ? 0xE8EBF5 : 0xEFEFF1); middle = bottom = top; break;
            }
            var texture = new Texture2D(1, GradientHeight, TextureFormat.RGBA32, false)
            {
                name = TexturePrefix + palette,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            for (int y = 0; y < GradientHeight; y++)
            {
                float t = 1f - y / (GradientHeight - 1f);
                texture.SetPixel(0, y, t < .5f ? Color.Lerp(top, middle, t * 2) : Color.Lerp(middle, bottom, t * 2 - 1));
            }
            texture.Apply(false, true);
            Gradients[palette] = texture;
            return texture;
        }

        private static Color Hex(int value) => new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
    }

    /// <summary>Keeps Button.text and native activation while drawing text above the art layers.</summary>
    public sealed class VectorButton : Button
    {
        public new class UxmlFactory : UxmlFactory<VectorButton, UxmlTraits> { }
        private Label caption;
        public VectorButton()
        {
            caption = new Label { pickingMode = PickingMode.Ignore };
            caption.AddToClassList("vector-caption"); hierarchy.Add(caption);
        }
        public override string text
        {
            get => caption != null ? caption.text : base.text;
            set { if (caption == null) base.text = value; else { base.text = string.Empty; caption.text = value; } }
        }
    }
}

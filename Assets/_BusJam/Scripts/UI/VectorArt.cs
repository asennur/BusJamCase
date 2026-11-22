using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    /// <summary>Kaynakla eşleşen, seçilemeyen SVG dekorasyonunu her görsel ağaçta bir kez kurar.</summary>
    public static class VectorArt
    {
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
                return;
            }
            rim.AddToClassList("raster-panel-art");
        }
    }

    /// <summary>Button.text'i ve yerel aktivasyonu korur, metni görsel katmanların üstünde çizer.</summary>
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

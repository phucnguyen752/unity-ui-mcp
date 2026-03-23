using UnityEngine;
using UnityEngine.UI;

namespace UnityMCP
{
    // ══════════════════════════════════════════════════════════
    // SetAnchorTool — applies RectTransform anchor presets
    // ══════════════════════════════════════════════════════════
    public static class SetAnchorTool
    {
        public static void Apply(GameObject go, AnchorPreset preset, Vector2 resolution)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            (rt.anchorMin, rt.anchorMax, rt.pivot) = preset switch
            {
                AnchorPreset.TopLeft            => (new Vector2(0,1),     new Vector2(0,1),     new Vector2(0,1)),
                AnchorPreset.TopCenter          => (new Vector2(.5f,1),   new Vector2(.5f,1),   new Vector2(.5f,1)),
                AnchorPreset.TopRight           => (new Vector2(1,1),     new Vector2(1,1),     new Vector2(1,1)),
                AnchorPreset.MiddleLeft         => (new Vector2(0,.5f),   new Vector2(0,.5f),   new Vector2(0,.5f)),
                AnchorPreset.MiddleCenter       => (new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(.5f,.5f)),
                AnchorPreset.MiddleRight        => (new Vector2(1,.5f),   new Vector2(1,.5f),   new Vector2(1,.5f)),
                AnchorPreset.BottomLeft         => (new Vector2(0,0),     new Vector2(0,0),     new Vector2(0,0)),
                AnchorPreset.BottomCenter       => (new Vector2(.5f,0),   new Vector2(.5f,0),   new Vector2(.5f,0)),
                AnchorPreset.BottomRight        => (new Vector2(1,0),     new Vector2(1,0),     new Vector2(1,0)),
                AnchorPreset.StretchHorizontal  => (new Vector2(0,.5f),   new Vector2(1,.5f),   new Vector2(.5f,.5f)),
                AnchorPreset.StretchVertical    => (new Vector2(.5f,0),   new Vector2(.5f,1),   new Vector2(.5f,.5f)),
                AnchorPreset.StretchFull        => (Vector2.zero,         Vector2.one,          new Vector2(.5f,.5f)),
                _                               => (new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(.5f,.5f)),
            };

            // Stretch presets need zero offsets to actually fill parent
            if (preset == AnchorPreset.StretchFull ||
                preset == AnchorPreset.StretchHorizontal ||
                preset == AnchorPreset.StretchVertical)
            {
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // SetSizeTool — applies explicit width/height or leaves stretch
    // ══════════════════════════════════════════════════════════
    public static class SetSizeTool
    {
        public static void Apply(GameObject go, SizeData size, Vector2 resolution)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            if (size.width > 0 && size.height > 0)
            {
                rt.sizeDelta = new Vector2(size.width, size.height);
            }
            else if (size.width > 0)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.width);
            }
            else if (size.height > 0)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.height);
            }
            // -1 means auto/stretch — don't override sizeDelta
        }
    }

    // ══════════════════════════════════════════════════════════
    // SetStyleTool — color, font, text content
    // ══════════════════════════════════════════════════════════
    public static class SetStyleTool
    {
        public static void Apply(GameObject go, ComponentNode node)
        {
            // Image color
            var img = go.GetComponent<Image>();
            if (img != null) img.color = node.color;

            // TMP text
            var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                if (!string.IsNullOrEmpty(node.text)) tmp.text = node.text;
                tmp.fontSize = node.fontSize;
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // SetLayoutTool — HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup
    // ══════════════════════════════════════════════════════════
    public static class SetLayoutTool
    {
        public static void Apply(GameObject go, ComponentNode node)
        {
            // Remove any existing layout group first
            var existing = go.GetComponent<LayoutGroup>();
            if (existing != null) Object.DestroyImmediate(existing);

            switch (node.layout)
            {
                case LayoutType.Horizontal: ApplyHorizontal(go, node); break;
                case LayoutType.Vertical:   ApplyVertical(go, node);   break;
                case LayoutType.Grid:       ApplyGrid(go, node);       break;
            }

            // ContentSizeFitter is optional — add only when size is auto
            if (node.size.width < 0 || node.size.height < 0)
            {
                var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
                if (node.size.width  < 0) fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                if (node.size.height < 0) fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static void ApplyHorizontal(GameObject go, ComponentNode node)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing                   = node.spacing;
            h.childAlignment            = TextAnchor.MiddleCenter;
            h.childControlWidth         = true;
            h.childControlHeight        = true;
            h.childForceExpandWidth     = false;
            h.childForceExpandHeight    = false;
            h.padding = ToPadding(node.padding);
        }

        private static void ApplyVertical(GameObject go, ComponentNode node)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing                = node.spacing;
            v.childAlignment         = TextAnchor.UpperCenter;
            v.childControlWidth      = true;
            v.childControlHeight     = true;
            v.childForceExpandWidth  = true;
            v.childForceExpandHeight = false;
            v.padding = ToPadding(node.padding);
        }

        private static void ApplyGrid(GameObject go, ComponentNode node)
        {
            var g = go.AddComponent<GridLayoutGroup>();
            g.spacing     = new Vector2(node.spacing, node.spacing);
            g.cellSize    = new Vector2(node.cellWidth, node.cellHeight);
            g.constraint  = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = node.gridColumns;
            g.padding = ToPadding(node.padding);
        }

        private static RectOffset ToPadding(PaddingData p) =>
            new((int)p.left, (int)p.right, (int)p.top, (int)p.bottom);
    }
}

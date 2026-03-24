using System.Collections.Generic;
using UnityEngine;
using UnityMCP.Core;

namespace UnityMCP
{
    // ── Data model ────────────────────────────────────────────
    public enum UiComponentType { Empty, Panel, Button, Image, Text, InputField, ScrollView, Toggle, Slider }
    public enum LayoutType       { None, Horizontal, Vertical, Grid }
    public enum AnchorPreset
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
        StretchHorizontal, StretchVertical, StretchFull
    }

    [System.Serializable]
    public class PaddingData
    {
        public float left, right, top, bottom;
    }

    [System.Serializable]
    public class SizeData
    {
        public float width  = -1;   // -1 = auto / stretch
        public float height = -1;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x = 0;
        public float y = 0;
    }

    [System.Serializable]
    public class ComponentNode
    {
        public string          name      = "Element";
        public UiComponentType type      = UiComponentType.Panel;
        public AnchorPreset    anchor    = AnchorPreset.MiddleCenter;
        public PositionData    position  = new();
        public LayoutType      layout    = LayoutType.None;
        public float           spacing   = 8;
        public PaddingData     padding   = new();
        public SizeData        size      = new();
        public Color           color     = Color.white;
        public string          text      = "";
        public int             fontSize  = 16;
        public FontStyle       fontStyle = FontStyle.Normal;
        public string          spritePath = "";
        public float?          ppum      = null;
        public Color?          textColor = null;
        public string          textAlignment = "";
        public ComponentNode[] children  = System.Array.Empty<ComponentNode>();

        // Grid-layout extras
        public int   gridColumns   = 2;
        public float cellWidth     = 100;
        public float cellHeight    = 100;

        // Layout group control
        public bool? controlChildWidth;
        public bool? controlChildHeight;
        public bool? forceExpandWidth;
        public bool? forceExpandHeight;
        public string childAlignment;
    }

    // ── Parser ────────────────────────────────────────────────
    public static class LayoutParser
    {
        public static ComponentNode Parse(string raw)
        {
            var json = StripMarkdown(raw.Trim());
            var dict = MiniJson.DeserializeObject(json);
            return ParseNode(dict);
        }

        private static ComponentNode ParseNode(Dictionary<string, object> obj)
        {
            if (obj == null) return new ComponentNode();

            var node = new ComponentNode
            {
                name      = obj.GetString("name", "Element"),
                type      = ParseEnum<UiComponentType>(obj.GetString("type"), UiComponentType.Panel),
                anchor    = ParseAnchor(obj.GetString("anchor")),
                position  = ParsePosition(obj.GetObject("position")),
                layout    = ParseEnum<LayoutType>(obj.GetString("layout"), LayoutType.None),
                spacing   = obj.GetFloat("spacing", 8f),
                text      = obj.GetString("text"),
                fontSize  = obj.GetInt("fontSize", 16),
                fontStyle = ParseFontStyle(obj.GetString("fontStyle")),
                color     = ParseColor(obj.GetString("color")),
                spritePath= obj.GetString("spritePath", ""),
                ppum      = obj.ContainsKey("ppum") ? obj.GetFloat("ppum") : null,
                textColor = obj.ContainsKey("textColor") ? (Color?)ParseColor(obj.GetString("textColor")) : null,
                textAlignment = obj.GetString("textAlignment", ""),
                padding   = ParsePadding(obj.GetObject("padding")),
                size      = ParseSize(obj.GetObject("size")),
                gridColumns = obj.GetInt("gridColumns", 2),
                cellWidth   = obj.GetFloat("cellWidth", 100f),
                cellHeight  = obj.GetFloat("cellHeight", 100f),
                controlChildWidth  = obj.ContainsKey("controlChildWidth")  ? obj.GetBool("controlChildWidth")  : null,
                controlChildHeight = obj.ContainsKey("controlChildHeight") ? obj.GetBool("controlChildHeight") : null,
                forceExpandWidth   = obj.ContainsKey("forceExpandWidth")   ? obj.GetBool("forceExpandWidth")   : null,
                forceExpandHeight  = obj.ContainsKey("forceExpandHeight")  ? obj.GetBool("forceExpandHeight")  : null,
                childAlignment     = obj.GetString("childAlignment"),
            };

            var childArray = obj.GetArray("children");
            if (childArray != null)
            {
                node.children = new ComponentNode[childArray.Count];
                for (int i = 0; i < childArray.Count; i++)
                    node.children[i] = ParseNode(childArray[i] as Dictionary<string, object>);
            }

            return node;
        }

        // ── Field parsers ─────────────────────────────────────
        private static AnchorPreset ParseAnchor(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return AnchorPreset.MiddleCenter;
            return raw.ToLowerInvariant().Replace(" ", "-") switch
            {
                "top-left"             => AnchorPreset.TopLeft,
                "top-center"           => AnchorPreset.TopCenter,
                "top-right"            => AnchorPreset.TopRight,
                "middle-left"          => AnchorPreset.MiddleLeft,
                "middle-center"        => AnchorPreset.MiddleCenter,
                "middle-right"         => AnchorPreset.MiddleRight,
                "bottom-left"          => AnchorPreset.BottomLeft,
                "bottom-center"        => AnchorPreset.BottomCenter,
                "bottom-right"         => AnchorPreset.BottomRight,
                "stretch-horizontal"   => AnchorPreset.StretchHorizontal,
                "stretch-vertical"     => AnchorPreset.StretchVertical,
                "stretch-full"         => AnchorPreset.StretchFull,
                _                      => AnchorPreset.MiddleCenter,
            };
        }

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.white;
        }

        private static PaddingData ParsePadding(Dictionary<string, object> obj) =>
            obj == null ? new PaddingData { left=8, right=8, top=8, bottom=8 } : new PaddingData
            {
                left   = obj.GetFloat("left", 8),
                right  = obj.GetFloat("right", 8),
                top    = obj.GetFloat("top", 8),
                bottom = obj.GetFloat("bottom", 8),
            };

        private static SizeData ParseSize(Dictionary<string, object> obj) =>
            obj == null ? new SizeData() : new SizeData
            {
                width  = obj.GetFloat("width", -1),
                height = obj.GetFloat("height", -1),
            };

        private static PositionData ParsePosition(Dictionary<string, object> obj) =>
            obj == null ? new PositionData() : new PositionData
            {
                x = obj.GetFloat("x", 0),
                y = obj.GetFloat("y", 0),
            };

        private static FontStyle ParseFontStyle(string s) => s?.ToLower() switch
        {
            "bold"        => FontStyle.Bold,
            "italic"      => FontStyle.Italic,
            "bold-italic" => FontStyle.BoldAndItalic,
            _             => FontStyle.Normal,
        };

        private static T ParseEnum<T>(string s, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            if (System.Enum.TryParse<T>(s, true, out var v)) return v;
            return fallback;
        }

        private static string StripMarkdown(string raw)
        {
            if (raw.StartsWith("```"))
            {
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline >= 0) raw = raw[(firstNewline + 1)..];
            }
            if (raw.EndsWith("```"))
                raw = raw[..^3];
            return raw.Trim();
        }
    }
}

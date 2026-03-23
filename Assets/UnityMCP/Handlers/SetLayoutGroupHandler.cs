using UnityEngine;
using UnityEngine.UI;
using UnityMCP.Core;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public static class SetLayoutGroupHandler
    {
        public static object Execute(SetLayoutGroupParams p)
        {
            var go = ElementRegistry.GetElement(p.element_id)
                ?? throw new System.Exception($"element_id '{p.element_id}' không tồn tại.");

            var padding = new RectOffset(
                (int)p.padding_left,
                (int)p.padding_right,
                (int)p.padding_top,
                (int)p.padding_bottom
            );

            var alignment = ParseAlignment(p.child_alignment);
            string appliedType;

            switch (p.layout_type)
            {
                case "Horizontal":
                {
                    // Remove existing vertical/grid if any
                    RemoveOtherLayouts<HorizontalLayoutGroup>(go);
                    var lg = go.GetOrAddComponent<HorizontalLayoutGroup>();
                    lg.spacing          = p.spacing;
                    lg.padding          = padding;
                    lg.childAlignment   = alignment;
                    lg.childControlWidth  = p.control_child_width;
                    lg.childControlHeight = p.control_child_height;
                    lg.childForceExpandWidth  = p.force_expand_width;
                    lg.childForceExpandHeight = p.force_expand_height;
                    appliedType = "HorizontalLayoutGroup";
                    break;
                }
                case "Vertical":
                {
                    RemoveOtherLayouts<VerticalLayoutGroup>(go);
                    var lg = go.GetOrAddComponent<VerticalLayoutGroup>();
                    lg.spacing          = p.spacing;
                    lg.padding          = padding;
                    lg.childAlignment   = alignment;
                    lg.childControlWidth  = p.control_child_width;
                    lg.childControlHeight = p.control_child_height;
                    lg.childForceExpandWidth  = p.force_expand_width;
                    lg.childForceExpandHeight = p.force_expand_height;
                    appliedType = "VerticalLayoutGroup";
                    break;
                }
                case "Grid":
                {
                    RemoveOtherLayouts<GridLayoutGroup>(go);
                    var lg = go.GetOrAddComponent<GridLayoutGroup>();
                    lg.spacing        = new Vector2(p.spacing, p.spacing);
                    lg.padding        = padding;
                    lg.childAlignment = alignment;

                    if (p.cell_width.HasValue && p.cell_height.HasValue)
                        lg.cellSize = new Vector2(p.cell_width.Value, p.cell_height.Value);

                    lg.constraint = p.constraint switch
                    {
                        "FixedColumnCount" => GridLayoutGroup.Constraint.FixedColumnCount,
                        "FixedRowCount"    => GridLayoutGroup.Constraint.FixedRowCount,
                        _                  => GridLayoutGroup.Constraint.Flexible
                    };
                    if (p.constraint != "Flexible")
                        lg.constraintCount = p.constraint_count;

                    appliedType = "GridLayoutGroup";
                    break;
                }
                default:
                    throw new System.NotSupportedException($"layout_type '{p.layout_type}' không được hỗ trợ.");
            }

            // ContentSizeFitter — tự động resize container theo children
            var csf = go.GetOrAddComponent<ContentSizeFitter>();
            csf.horizontalFit = p.layout_type == "Vertical"
                ? ContentSizeFitter.FitMode.Unconstrained
                : ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = p.layout_type == "Horizontal"
                ? ContentSizeFitter.FitMode.Unconstrained
                : ContentSizeFitter.FitMode.PreferredSize;

            return new
            {
                element_id   = p.element_id,
                name         = go.name,
                layout_group = appliedType,
                spacing      = p.spacing,
                padding      = new { left=p.padding_left, right=p.padding_right, top=p.padding_top, bottom=p.padding_bottom },
                child_alignment = p.child_alignment
            };
        }

        private static TextAnchor ParseAlignment(string s) => s switch
        {
            "UpperLeft"    => TextAnchor.UpperLeft,
            "UpperCenter"  => TextAnchor.UpperCenter,
            "UpperRight"   => TextAnchor.UpperRight,
            "MiddleLeft"   => TextAnchor.MiddleLeft,
            "MiddleCenter" => TextAnchor.MiddleCenter,
            "MiddleRight"  => TextAnchor.MiddleRight,
            "LowerLeft"    => TextAnchor.LowerLeft,
            "LowerCenter"  => TextAnchor.LowerCenter,
            "LowerRight"   => TextAnchor.LowerRight,
            _              => TextAnchor.UpperLeft
        };

        private static void RemoveOtherLayouts<TKeep>(GameObject go) where TKeep : LayoutGroup
        {
            if (go.GetComponent<HorizontalLayoutGroup>() is var h && h != null && !(h is TKeep)) Object.DestroyImmediate(h);
            if (go.GetComponent<VerticalLayoutGroup>()   is var v && v != null && !(v is TKeep)) Object.DestroyImmediate(v);
            if (go.GetComponent<GridLayoutGroup>()        is var g && g != null && !(g is TKeep)) Object.DestroyImmediate(g);
        }
    }

    internal static class GOExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}

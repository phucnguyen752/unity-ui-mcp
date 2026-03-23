using UnityEngine;
using UnityMCP.Core;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public static class SetRectHandler
    {
        public static object Execute(SetRectParams p)
        {
            var go = ElementRegistry.GetElement(p.element_id)
                ?? throw new System.Exception($"element_id '{p.element_id}' không tồn tại.");

            var rt = go.GetComponent<RectTransform>()
                ?? throw new System.Exception($"GameObject '{go.name}' không có RectTransform.");

            // Pivot
            rt.pivot = new Vector2(p.pivot_x, p.pivot_y);

            // Apply anchor preset
            ApplyAnchorPreset(rt, p.anchor_preset ?? "middle-center");

            // Position / Size
            bool hStretch = IsHorizontalStretch(p.anchor_preset);
            bool vStretch = IsVerticalStretch(p.anchor_preset);

            if (hStretch)
            {
                float left   = p.offset_left   ?? 0;
                float right  = p.offset_right  ?? 0;
                rt.offsetMin = new Vector2(left, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
            }
            else
            {
                if (p.width.HasValue)
                {
                    var sd = rt.sizeDelta;
                    rt.sizeDelta = new Vector2(p.width.Value, sd.y);
                }
                if (p.pos_x.HasValue)
                {
                    var ap = rt.anchoredPosition;
                    rt.anchoredPosition = new Vector2(p.pos_x.Value, ap.y);
                }
            }

            if (vStretch)
            {
                float top    = p.offset_top    ?? 0;
                float bottom = p.offset_bottom ?? 0;
                rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
                rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
            }
            else
            {
                if (p.height.HasValue)
                {
                    var sd = rt.sizeDelta;
                    rt.sizeDelta = new Vector2(sd.x, p.height.Value);
                }
                if (p.pos_y.HasValue)
                {
                    var ap = rt.anchoredPosition;
                    rt.anchoredPosition = new Vector2(ap.x, p.pos_y.Value);
                }
            }

            return new
            {
                element_id = p.element_id,
                name = go.name,
                anchor_preset = p.anchor_preset,
                anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y },
                anchoredPosition = new { x = rt.anchoredPosition.x, y = rt.anchoredPosition.y },
                sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y },
                pivot = new { x = rt.pivot.x, y = rt.pivot.y }
            };
        }

        private static void ApplyAnchorPreset(RectTransform rt, string preset)
        {
            (Vector2 min, Vector2 max) = preset switch
            {
                "top-left"       => (new Vector2(0, 1),    new Vector2(0, 1)),
                "top-center"     => (new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
                "top-right"      => (new Vector2(1, 1),    new Vector2(1, 1)),
                "middle-left"    => (new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
                "middle-center"  => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
                "middle-right"   => (new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
                "bottom-left"    => (new Vector2(0, 0),    new Vector2(0, 0)),
                "bottom-center"  => (new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
                "bottom-right"   => (new Vector2(1, 0),    new Vector2(1, 0)),
                "top-stretch"    => (new Vector2(0, 1),    new Vector2(1, 1)),
                "middle-stretch" => (new Vector2(0, 0.5f), new Vector2(1, 0.5f)),
                "bottom-stretch" => (new Vector2(0, 0),    new Vector2(1, 0)),
                "left-stretch"   => (new Vector2(0, 0),    new Vector2(0, 1)),
                "center-stretch" => (new Vector2(0.5f, 0), new Vector2(0.5f, 1)),
                "right-stretch"  => (new Vector2(1, 0),    new Vector2(1, 1)),
                "full-stretch"   => (Vector2.zero,          Vector2.one),
                _                => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f))
            };
            rt.anchorMin = min;
            rt.anchorMax = max;
        }

        private static bool IsHorizontalStretch(string preset) =>
            preset is "top-stretch" or "middle-stretch" or "bottom-stretch" or "full-stretch";

        private static bool IsVerticalStretch(string preset) =>
            preset is "left-stretch" or "center-stretch" or "right-stretch" or "full-stretch";
    }
}

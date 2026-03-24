// SetStyleHandler.cs
using System;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityMCP.Core;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public static class SetStyleHandler
    {
        public static object Execute(SetStyleParams p)
        {
            var go = ElementRegistry.GetElement(p.element_id)
                ?? throw new Exception($"element_id '{p.element_id}' does not exist.");

            var changed = new System.Collections.Generic.List<string>();

            // Color → Image or TMP_Text
            if (!string.IsNullOrEmpty(p.color))
            {
                if (ColorUtility.TryParseHtmlString(p.color, out var c))
                {
                    if (go.GetComponent<Image>() is Image img)       { img.color = c; changed.Add("color(Image)"); }
                    if (go.GetComponent<TMP_Text>() is TMP_Text tmp) { tmp.color = c; changed.Add("color(Text)"); }
                }
            }

            // Font size
            if (p.font_size.HasValue)
            {
                if (go.GetComponent<TMP_Text>() is TMP_Text tmp) { tmp.fontSize = p.font_size.Value; changed.Add("fontSize"); }
                // Traverse children for label inside Button
                foreach (var child in go.GetComponentsInChildren<TMP_Text>())
                    child.fontSize = p.font_size.Value;
            }

            // Font style
            if (!string.IsNullOrEmpty(p.font_style))
            {
                var style = p.font_style switch
                {
                    "Bold"       => FontStyles.Bold,
                    "Italic"     => FontStyles.Italic,
                    "BoldItalic" => FontStyles.Bold | FontStyles.Italic,
                    _            => FontStyles.Normal
                };
                foreach (var t in go.GetComponentsInChildren<TMP_Text>())
                    t.fontStyle = style;
                changed.Add("fontStyle");
            }

            // Text alignment
            if (!string.IsNullOrEmpty(p.text_alignment))
            {
                var align = p.text_alignment switch
                {
                    "Left"      => TextAlignmentOptions.Left,
                    "Right"     => TextAlignmentOptions.Right,
                    "Justified" => TextAlignmentOptions.Justified,
                    _           => TextAlignmentOptions.Center
                };
                foreach (var t in go.GetComponentsInChildren<TMP_Text>())
                    t.alignment = align;
                changed.Add("textAlignment");
            }

            // Sprite
            if (!string.IsNullOrEmpty(p.sprite_path))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p.sprite_path);
                if (sprite != null && go.GetComponent<Image>() is Image img)
                {
                    img.sprite = sprite;
                    img.type = p.image_type switch
                    {
                        "Sliced" => Image.Type.Sliced,
                        "Tiled"  => Image.Type.Tiled,
                        "Filled" => Image.Type.Filled,
                        _        => Image.Type.Simple
                    };
                    if (p.pixels_per_unit_multiplier.HasValue)
                    {
                        img.pixelsPerUnitMultiplier = p.pixels_per_unit_multiplier.Value;
                        changed.Add($"pixelsPerUnitMultiplier({p.pixels_per_unit_multiplier.Value})");
                    }
                    changed.Add($"sprite({p.sprite_path})");
                }
                else if (sprite == null)
                    throw new Exception($"Sprite not found at '{p.sprite_path}'");
            }

            // Raycast target
            if (go.GetComponent<Image>() is Image imgRay)
                imgRay.raycastTarget = p.raycast_target;

            return new { element_id = p.element_id, name = go.name, applied = changed };
        }
    }

    public static class SetCanvasScalerHandler
    {
        public static object Execute(SetCanvasScalerParams p)
        {
            var go = ElementRegistry.GetElement(p.element_id)
                ?? throw new Exception($"element_id '{p.element_id}' does not exist.");

            var scaler = go.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = p.scale_mode switch
            {
                "ScaleWithScreenSize"  => CanvasScaler.ScaleMode.ScaleWithScreenSize,
                "ConstantPhysicalSize" => CanvasScaler.ScaleMode.ConstantPhysicalSize,
                _                      => CanvasScaler.ScaleMode.ConstantPixelSize
            };

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.referenceResolution = new Vector2(p.reference_width, p.reference_height);
                scaler.matchWidthOrHeight  = p.match_width_or_height;
                scaler.screenMatchMode = p.screen_match_mode switch
                {
                    "Expand" => CanvasScaler.ScreenMatchMode.Expand,
                    "Shrink" => CanvasScaler.ScreenMatchMode.Shrink,
                    _        => CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                };
            }

            return new
            {
                element_id = p.element_id,
                scale_mode = p.scale_mode,
                reference_resolution = new { width = p.reference_width, height = p.reference_height },
                match = p.match_width_or_height
            };
        }
    }

    public static class SavePrefabHandler
    {
        public static object Execute(SavePrefabParams p)
        {
            if (!ElementRegistry.TryGetPrefab(p.prefab_id, out var root, out var savePath))
                throw new Exception($"prefab_id '{p.prefab_id}' does not exist.");

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(savePath.TrimEnd('/')))
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.Combine(Application.dataPath, "..", savePath));

            var assetPath = $"{savePath}{root.name}.prefab";

            if (!p.overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                throw new Exception($"Prefab already exists at '{assetPath}'. Use overwrite: true to overwrite.");

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            UnityEngine.Object.DestroyImmediate(root); // destroy temporary scene object
            ElementRegistry.RemovePrefab(p.prefab_id);

            return new
            {
                prefab_id  = p.prefab_id,
                asset_path = assetPath,
                message    = $"Prefab saved at '{assetPath}'."
            };
        }
    }

    public static class QueryHierarchyHandler
    {
        public static object Execute(QueryHierarchyParams p)
        {
            if (!ElementRegistry.TryGetPrefab(p.prefab_id, out var root, out _))
                throw new Exception($"prefab_id '{p.prefab_id}' does not exist.");

            return new
            {
                prefab_id = p.prefab_id,
                hierarchy = BuildNode(root.transform, 0, p.depth)
            };
        }

        private static object BuildNode(Transform t, int depth, int maxDepth)
        {
            var components = new System.Collections.Generic.List<string>();
            foreach (var c in t.GetComponents<Component>())
            {
                var name = c.GetType().Name;
                if (name != "Transform") components.Add(name);
            }

            var children = new System.Collections.Generic.List<object>();
            if (depth < maxDepth)
                for (int i = 0; i < t.childCount; i++)
                    children.Add(BuildNode(t.GetChild(i), depth + 1, maxDepth));

            var rt = t.GetComponent<RectTransform>();
            return new
            {
                name = t.name,
                components,
                rect = rt != null ? new
                {
                    anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                    anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y },
                    sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y }
                } : null,
                children
            };
        }
    }
}

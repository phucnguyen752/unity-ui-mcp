using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityMCP.Core;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public static class CreatePrefabHandler
    {
        public static object Execute(CreatePrefabParams p)
        {
            var savePath = p.save_path.TrimEnd('/') + "/";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", savePath));

            bool isCanvas = p.root_type == "Canvas";

            if (isCanvas)
            {
                return CreateWithCanvas(p, savePath);
            }
            else
            {
                return CreateWithoutCanvas(p, savePath);
            }
        }

        /// <summary>
        /// Create prefab with Canvas root (legacy) — used when the prefab needs to be a full screen overlay.
        /// </summary>
        private static object CreateWithCanvas(CreatePrefabParams p, string savePath)
        {
            var root = new GameObject(p.prefab_name);

            // Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = p.canvas_render_mode switch
            {
                "Camera"     => RenderMode.ScreenSpaceCamera,
                "WorldSpace" => RenderMode.WorldSpace,
                _            => RenderMode.ScreenSpaceOverlay
            };

            // CanvasScaler
            var scaler = root.AddComponent<CanvasScaler>();
            if (p.reference_resolution != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(
                    p.reference_resolution.width,
                    p.reference_resolution.height
                );
                scaler.matchWidthOrHeight = p.match_width_or_height;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }

            root.AddComponent<GraphicRaycaster>();

            var prefabId = ElementRegistry.RegisterPrefab(root, savePath);
            var elemId   = ElementRegistry.Register(root);

            return new
            {
                prefab_id  = prefabId,
                root_element_id = elemId,
                prefab_name = p.prefab_name,
                save_path  = savePath,
                root_type  = "Canvas",
                message    = $"Prefab '{p.prefab_name}' (Canvas root) created. Call save_prefab when done."
            };
        }

        /// <summary>
        /// Create prefab WITHOUT Canvas — just a GameObject + RectTransform + Image.
        /// When dragged into the scene, it will automatically be placed under an existing Canvas.
        /// </summary>
        private static object CreateWithoutCanvas(CreatePrefabParams p, string savePath)
        {
            var root = new GameObject(p.prefab_name, typeof(RectTransform));

            // Add Image component if root_type is Panel or Image
            if (p.root_type == "Panel" || p.root_type == "Image")
            {
                var img = root.AddComponent<Image>();
                img.color = p.root_type == "Panel"
                    ? new Color(1, 1, 1, 0.5f)
                    : Color.white;
            }

            // Set full stretch by default
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var prefabId = ElementRegistry.RegisterPrefab(root, savePath);
            var elemId   = ElementRegistry.Register(root);

            return new
            {
                prefab_id  = prefabId,
                root_element_id = elemId,
                prefab_name = p.prefab_name,
                save_path  = savePath,
                root_type  = p.root_type,
                message    = $"Prefab '{p.prefab_name}' ({p.root_type} root, no Canvas) created. Drag into an existing Canvas in the scene. Call save_prefab when done."
            };
        }
    }
}

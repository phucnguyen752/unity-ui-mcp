using System;
using System.IO;
using UnityEngine;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public class BuildUiResult
    {
        public string message;
        public string prefab_name;
        public string save_path;
        public float target_width;
        public float target_height;
    }

    public static class BuildUiFromJsonHandler
    {
        /// <summary>
        /// Well-known path for the vision JSON file.
        /// AI writes JSON here, then calls build_ui_from_json to process it.
        /// </summary>
        public const string VisionJsonPath = "Assets/UnityMCP/vision_json.json";

        public static BuildUiResult Execute(BuildUiFromJsonParams p)
        {
            // Read JSON from the vision file
            string jsonLayout;
            if (!string.IsNullOrEmpty(p.json_layout))
            {
                jsonLayout = p.json_layout;
            }
            else
            {
                var fullPath = Path.Combine(Application.dataPath, "..", VisionJsonPath);
                if (!File.Exists(fullPath))
                    throw new Exception($"Vision JSON file not found at '{VisionJsonPath}'. Write your JSON layout there first.");

                jsonLayout = File.ReadAllText(fullPath);
            }

            if (string.IsNullOrEmpty(jsonLayout))
                throw new Exception("json_layout is empty (both parameter and file).");

            var tree = LayoutParser.Parse(jsonLayout);
            var dispatcher = new ToolDispatcher();

            var savePath = string.IsNullOrEmpty(p.save_path) ? "Assets/UI/Prefabs/" : p.save_path;
            var prefabName = string.IsNullOrEmpty(p.prefab_name) ? tree.name : p.prefab_name;

            // Fire and forget - will be executed on main thread via EditorApplication.delayCall
            _ = dispatcher.BuildAsync(tree, new Vector2(p.target_width, p.target_height),
                                      savePath, prefabName);

            return new BuildUiResult
            {
                message = $"UI Build scheduled. Prefab '{prefabName}' will be saved to '{savePath}' shortly.",
                prefab_name = prefabName,
                save_path = savePath,
                target_width = p.target_width,
                target_height = p.target_height
            };
        }
    }
}

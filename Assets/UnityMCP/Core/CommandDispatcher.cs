using System;
using System.Collections.Generic;
using UnityMCP.Handlers;
using UnityMCP.Models;

namespace UnityMCP.Core
{
    /// <summary>
    /// Receives raw JSON from server, parses it, calls the corresponding handler, returns JSON response.
    /// Runs on Unity main thread (safe to use UnityEngine + UnityEditor API).
    /// </summary>
    public static class CommandDispatcher
    {
        public static string Dispatch(string rawJson)
        {
            string cmdId = "";
            try
            {
                var dict = MiniJson.DeserializeObject(rawJson);
                cmdId = dict.GetString("id");
                var tool = dict.GetString("tool");
                var paramsJson = MiniJson.Serialize(dict.GetObject("params") ?? new Dictionary<string, object>());

                object result = tool switch
                {
                    "create_prefab_ui"    => CreatePrefabHandler.Execute(MiniJson.DeserializeTo<CreatePrefabParams>(paramsJson)),
                    "add_ui_element"      => AddElementHandler.Execute(MiniJson.DeserializeTo<AddElementParams>(paramsJson)),
                    "set_rect_transform"  => SetRectHandler.Execute(MiniJson.DeserializeTo<SetRectParams>(paramsJson)),
                    "set_layout_group"    => SetLayoutGroupHandler.Execute(MiniJson.DeserializeTo<SetLayoutGroupParams>(paramsJson)),
                    "set_ui_style"        => SetStyleHandler.Execute(MiniJson.DeserializeTo<SetStyleParams>(paramsJson)),
                    "set_canvas_scaler"   => SetCanvasScalerHandler.Execute(MiniJson.DeserializeTo<SetCanvasScalerParams>(paramsJson)),
                    "save_prefab"         => SavePrefabHandler.Execute(MiniJson.DeserializeTo<SavePrefabParams>(paramsJson)),
                    "query_ui_hierarchy"  => QueryHierarchyHandler.Execute(MiniJson.DeserializeTo<QueryHierarchyParams>(paramsJson)),
                    "get_editor_config"   => GetEditorConfigHandler.Execute(),
                    "build_ui_from_json"  => BuildUiFromJsonHandler.Execute(MiniJson.DeserializeTo<BuildUiFromJsonParams>(paramsJson)),
                    _                     => throw new NotSupportedException($"Unknown tool: '{tool}'")
                };

                return MiniJson.Serialize(new Dictionary<string, object>
                {
                    ["id"] = cmdId,
                    ["success"] = true,
                    ["result"] = result
                });
            }
            catch (Exception e)
            {
                return MiniJson.Serialize(new Dictionary<string, object>
                {
                    ["id"] = cmdId,
                    ["success"] = false,
                    ["error"] = e.Message
                });
            }
        }
    }
}

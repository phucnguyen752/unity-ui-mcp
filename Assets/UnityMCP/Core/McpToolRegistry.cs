using System.Collections.Generic;

namespace UnityMCP.Core
{
    /// <summary>
    /// MCP tool schema definitions — equivalent to list_tools() in a Python MCP server.
    /// Uses Dictionary/List instead of Newtonsoft JObject/JArray.
    /// </summary>
    public static class McpToolRegistry
    {
        public static List<object> GetToolDefinitions()
        {
            return new List<object>
            {
                GetEditorConfigTool(),
                CreatePrefabTool(),
                AddElementTool(),
                SetRectTransformTool(),
                SetLayoutGroupTool(),
                SetUiStyleTool(),
                SetCanvasScalerTool(),
                SavePrefabTool(),
                QueryHierarchyTool(),
                BuildUiFromJsonTool()
            };
        }

        // ── Helper builders ──────────────────────────────────────────

        private static Dictionary<string, object> Prop(string type, string desc = null, object defaultVal = null, List<object> enumVals = null)
        {
            var p = new Dictionary<string, object> { ["type"] = type };
            if (desc != null) p["description"] = desc;
            if (defaultVal != null) p["default"] = defaultVal;
            if (enumVals != null) p["enum"] = enumVals;
            return p;
        }

        private static List<object> Enum(params string[] values)
        {
            return new List<object>(values);
        }

        // ── Tool definitions ─────────────────────────────────────────

        private static Dictionary<string, object> GetEditorConfigTool() => new()
        {
            ["name"] = "get_editor_config",
            ["description"] = "Get editor config: target screen resolution, game view size, output path. CALL THIS BEFORE creating UI to get the design resolution.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["required"] = Enum()
            }
        };

        private static Dictionary<string, object> CreatePrefabTool() => new()
        {
            ["name"] = "create_prefab_ui",
            ["description"] = "Create a new UI prefab. MUST call get_editor_config first to get target_screen, then calculate sizeDelta = target_screen × ratio%. root_type='Panel' (default) creates prefab without Canvas. root_type='Canvas' creates prefab with its own Canvas.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_name"] = Prop("string", "Prefab name, e.g. RemoveAdsPopup"),
                    ["save_path"] = Prop("string", "Folder to save prefab in project, e.g. Assets/UI/Prefabs/"),
                    ["root_type"] = Prop("string", "Root type: Panel (default, no Canvas), Image, Canvas (with its own Canvas)", "Panel", Enum("Panel", "Image", "Canvas")),
                    ["canvas_render_mode"] = Prop("string", "Only used when root_type=Canvas", "Overlay", Enum("Overlay", "Camera", "WorldSpace")),
                    ["reference_resolution"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["width"] = Prop("number"),
                            ["height"] = Prop("number")
                        },
                        ["description"] = "Only used when root_type=Canvas. Resolution for CanvasScaler"
                    },
                    ["match_width_or_height"] = Prop("number", "Only used when root_type=Canvas. 0=width, 1=height, 0.5=balanced", 0.5)
                },
                ["required"] = Enum("prefab_name", "save_path")
            }
        };

        private static Dictionary<string, object> AddElementTool() => new()
        {
            ["name"] = "add_ui_element",
            ["description"] = "Add a UI element to a prefab. Returns element_id for subsequent rect/style calls.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string", "ID of the prefab being created"),
                    ["element_type"] = Prop("string", "UI component type. Empty = RectTransform only (no visual)", enumVals: Enum("Empty", "Panel", "Button", "Text", "Image", "InputField", "ScrollView", "Toggle", "Slider", "Dropdown")),
                    ["name"] = Prop("string", "GameObject name in Unity hierarchy"),
                    ["parent_id"] = Prop("string", "element_id of parent. Leave empty = direct child of root"),
                    ["text_content"] = Prop("string", "Text content (for Text, Button, InputField placeholder)")
                },
                ["required"] = Enum("prefab_id", "element_type", "name")
            }
        };

        private static Dictionary<string, object> SetRectTransformTool() => new()
        {
            ["name"] = "set_rect_transform",
            ["description"] = "Set anchor, position and size for an element's RectTransform.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string"),
                    ["anchor_preset"] = Prop("string", "Anchor preset. full-stretch = fill entire parent",
                        enumVals: Enum(
                            "top-left", "top-center", "top-right",
                            "middle-left", "middle-center", "middle-right",
                            "bottom-left", "bottom-center", "bottom-right",
                            "top-stretch", "middle-stretch", "bottom-stretch",
                            "left-stretch", "center-stretch", "right-stretch",
                            "full-stretch")),
                    ["pos_x"] = Prop("number", "anchoredPosition.x (px from anchor)"),
                    ["pos_y"] = Prop("number", "anchoredPosition.y (px from anchor)"),
                    ["width"] = Prop("number", "Width (px). Ignored when using horizontal stretch"),
                    ["height"] = Prop("number", "Height (px). Ignored when using vertical stretch"),
                    ["offset_left"] = Prop("number", "Offset from left anchor (stretch mode)"),
                    ["offset_right"] = Prop("number", "Offset from right anchor (stretch mode)"),
                    ["offset_top"] = Prop("number", "Offset from top anchor (stretch mode)"),
                    ["offset_bottom"] = Prop("number", "Offset from bottom anchor (stretch mode)"),
                    ["pivot_x"] = Prop("number", defaultVal: 0.5),
                    ["pivot_y"] = Prop("number", defaultVal: 0.5)
                },
                ["required"] = Enum("element_id")
            }
        };

        private static Dictionary<string, object> SetLayoutGroupTool() => new()
        {
            ["name"] = "set_layout_group",
            ["description"] = "Attach HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup to an element.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string"),
                    ["layout_type"] = Prop("string", enumVals: Enum("Horizontal", "Vertical", "Grid")),
                    ["spacing"] = Prop("number", defaultVal: 0),
                    ["padding_left"] = Prop("number", defaultVal: 0),
                    ["padding_right"] = Prop("number", defaultVal: 0),
                    ["padding_top"] = Prop("number", defaultVal: 0),
                    ["padding_bottom"] = Prop("number", defaultVal: 0),
                    ["child_alignment"] = Prop("string", defaultVal: "UpperLeft",
                        enumVals: Enum(
                            "UpperLeft", "UpperCenter", "UpperRight",
                            "MiddleLeft", "MiddleCenter", "MiddleRight",
                            "LowerLeft", "LowerCenter", "LowerRight")),
                    ["control_child_width"] = Prop("boolean", defaultVal: false),
                    ["control_child_height"] = Prop("boolean", defaultVal: false),
                    ["force_expand_width"] = Prop("boolean", defaultVal: true),
                    ["force_expand_height"] = Prop("boolean", defaultVal: false),
                    ["cell_width"] = Prop("number", "Grid only: cell width"),
                    ["cell_height"] = Prop("number", "Grid only: cell height"),
                    ["constraint"] = Prop("string", "Grid only", enumVals: Enum("Flexible", "FixedColumnCount", "FixedRowCount")),
                    ["constraint_count"] = Prop("integer", "Grid only: fixed column or row count")
                },
                ["required"] = Enum("element_id", "layout_type")
            }
        };

        private static Dictionary<string, object> SetUiStyleTool() => new()
        {
            ["name"] = "set_ui_style",
            ["description"] = "Apply color, font, sprite to a UI element.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string"),
                    ["color"] = Prop("string", "Hex RGBA: #RRGGBBAA or #RRGGBB. e.g. #FFFFFF80 = white 50%"),
                    ["font_size"] = Prop("integer", "Font size (px)"),
                    ["font_style"] = Prop("string", enumVals: Enum("Normal", "Bold", "Italic", "BoldItalic")),
                    ["text_alignment"] = Prop("string", enumVals: Enum("Left", "Center", "Right", "Justified")),
                    ["sprite_path"] = Prop("string", "Asset path in project, e.g. Assets/UI/Sprites/button_bg.png"),
                    ["image_type"] = Prop("string", enumVals: Enum("Simple", "Sliced", "Tiled", "Filled")),
                    ["pixels_per_unit_multiplier"] = Prop("number", "Pixels Per Unit Multiplier for Image (Sliced). Higher = smaller corners, lower = larger corners"),
                    ["raycast_target"] = Prop("boolean", defaultVal: true)
                },
                ["required"] = Enum("element_id")
            }
        };

        private static Dictionary<string, object> SetCanvasScalerTool() => new()
        {
            ["name"] = "set_canvas_scaler",
            ["description"] = "Configure CanvasScaler separately (if not set during create_prefab_ui).",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string", "Canvas element_id"),
                    ["scale_mode"] = Prop("string", enumVals: Enum("ConstantPixelSize", "ScaleWithScreenSize", "ConstantPhysicalSize")),
                    ["reference_width"] = Prop("number", defaultVal: 1920),
                    ["reference_height"] = Prop("number", defaultVal: 1080),
                    ["match_width_or_height"] = Prop("number", "0=match width, 1=match height, 0.5=balanced"),
                    ["screen_match_mode"] = Prop("string", enumVals: Enum("MatchWidthOrHeight", "Expand", "Shrink"))
                },
                ["required"] = Enum("element_id", "scale_mode")
            }
        };

        private static Dictionary<string, object> SavePrefabTool() => new()
        {
            ["name"] = "save_prefab",
            ["description"] = "Save prefab to a .prefab file. Call after the full hierarchy is created.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string"),
                    ["overwrite"] = Prop("boolean", "True = overwrite if file already exists", false)
                },
                ["required"] = Enum("prefab_id")
            }
        };

        private static Dictionary<string, object> QueryHierarchyTool() => new()
        {
            ["name"] = "query_ui_hierarchy",
            ["description"] = "Read back the prefab hierarchy structure to verify after creation.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string"),
                    ["depth"] = Prop("integer", "Number of hierarchy levels to return", 5)
                },
                ["required"] = Enum("prefab_id")
            }
        };

        private static Dictionary<string, object> BuildUiFromJsonTool() => new()
        {
            ["name"] = "build_ui_from_json",
            ["description"] = "Build complete UI prefab from JSON layout (NO Canvas wrapper — prefab is placed inside existing Canvas). Reads JSON from vision_json.json in project root. Call get_editor_config first (it clears old JSON), write new JSON to vision_json_path, then call this tool. Root type should be 'Empty' (RectTransform only) with Overlay and DialogPanel as children.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_name"] = Prop("string", "Name for the saved prefab (e.g. RemoveAdsPopup)"),
                    ["save_path"] = Prop("string", "Folder to save .prefab file. Defaults to Output Path set in MCP Bridge window"),
                    ["json_layout"] = Prop("string", "Optional: inline JSON string. If empty, reads from Assets/UnityMCP/vision_json.json"),
                    ["target_width"] = Prop("number", "Design width (e.g. 1080)", 1080),
                    ["target_height"] = Prop("number", "Design height (e.g. 1920)", 1920)
                },
                ["required"] = Enum("prefab_name")
            }
        };
    }
}

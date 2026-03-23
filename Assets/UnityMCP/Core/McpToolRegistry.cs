using System.Collections.Generic;

namespace UnityMCP.Core
{
    /// <summary>
    /// Định nghĩa MCP tool schemas — tương đương list_tools() trong Python server.py.
    /// Dùng Dictionary/List thay vì Newtonsoft JObject/JArray.
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
                QueryHierarchyTool()
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
            ["description"] = "Lấy cấu hình editor: target screen resolution, game view size. GỌI TOOL NÀY TRƯỚC KHI TẠO UI để biết kích thước thiết kế chuẩn.",
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
            ["description"] = "Tạo prefab UI mới. PHẢI gọi get_editor_config trước để lấy target_screen, rồi tính sizeDelta = target_screen × ratio%. root_type='Panel' (mặc định) tạo prefab không Canvas. root_type='Canvas' tạo prefab có Canvas riêng.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_name"] = Prop("string", "Tên prefab, VD: RemoveAdsPopup"),
                    ["save_path"] = Prop("string", "Thư mục lưu prefab trong project, VD: Assets/UI/Prefabs/"),
                    ["root_type"] = Prop("string", "Loại root: Panel (mặc định, không Canvas), Image, Canvas (có Canvas riêng)", "Panel", Enum("Panel", "Image", "Canvas")),
                    ["canvas_render_mode"] = Prop("string", "Chỉ dùng khi root_type=Canvas", "Overlay", Enum("Overlay", "Camera", "WorldSpace")),
                    ["reference_resolution"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["width"] = Prop("number"),
                            ["height"] = Prop("number")
                        },
                        ["description"] = "Chỉ dùng khi root_type=Canvas. Resolution cho CanvasScaler"
                    },
                    ["match_width_or_height"] = Prop("number", "Chỉ dùng khi root_type=Canvas. 0=width, 1=height, 0.5=balanced", 0.5)
                },
                ["required"] = Enum("prefab_name", "save_path")
            }
        };

        private static Dictionary<string, object> AddElementTool() => new()
        {
            ["name"] = "add_ui_element",
            ["description"] = "Thêm UI element vào prefab. Trả về element_id để set rect/style tiếp theo.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string", "ID của prefab đang tạo"),
                    ["element_type"] = Prop("string", "Loại UI component", enumVals: Enum("Panel", "Button", "Text", "Image", "InputField", "ScrollView", "Toggle", "Slider", "Dropdown")),
                    ["name"] = Prop("string", "Tên GameObject trong Unity hierarchy"),
                    ["parent_id"] = Prop("string", "element_id của parent. Bỏ trống = con trực tiếp của Canvas"),
                    ["text_content"] = Prop("string", "Nội dung text (cho Text, Button, InputField placeholder)")
                },
                ["required"] = Enum("prefab_id", "element_type", "name")
            }
        };

        private static Dictionary<string, object> SetRectTransformTool() => new()
        {
            ["name"] = "set_rect_transform",
            ["description"] = "Đặt anchor, position và size cho RectTransform của element.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string"),
                    ["anchor_preset"] = Prop("string", "Anchor preset. full-stretch = fill toàn bộ parent",
                        enumVals: Enum(
                            "top-left", "top-center", "top-right",
                            "middle-left", "middle-center", "middle-right",
                            "bottom-left", "bottom-center", "bottom-right",
                            "top-stretch", "middle-stretch", "bottom-stretch",
                            "left-stretch", "center-stretch", "right-stretch",
                            "full-stretch")),
                    ["pos_x"] = Prop("number", "anchoredPosition.x (px từ anchor)"),
                    ["pos_y"] = Prop("number", "anchoredPosition.y (px từ anchor)"),
                    ["width"] = Prop("number", "Chiều rộng (px). Bỏ qua khi dùng stretch ngang"),
                    ["height"] = Prop("number", "Chiều cao (px). Bỏ qua khi dùng stretch dọc"),
                    ["offset_left"] = Prop("number", "Offset từ anchor trái (stretch mode)"),
                    ["offset_right"] = Prop("number", "Offset từ anchor phải (stretch mode)"),
                    ["offset_top"] = Prop("number", "Offset từ anchor trên (stretch mode)"),
                    ["offset_bottom"] = Prop("number", "Offset từ anchor dưới (stretch mode)"),
                    ["pivot_x"] = Prop("number", defaultVal: 0.5),
                    ["pivot_y"] = Prop("number", defaultVal: 0.5)
                },
                ["required"] = Enum("element_id")
            }
        };

        private static Dictionary<string, object> SetLayoutGroupTool() => new()
        {
            ["name"] = "set_layout_group",
            ["description"] = "Gắn HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup lên element.",
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
                    ["cell_width"] = Prop("number", "Grid only: chiều rộng mỗi ô"),
                    ["cell_height"] = Prop("number", "Grid only: chiều cao mỗi ô"),
                    ["constraint"] = Prop("string", "Grid only", enumVals: Enum("Flexible", "FixedColumnCount", "FixedRowCount")),
                    ["constraint_count"] = Prop("integer", "Grid only: số cột hoặc hàng cố định")
                },
                ["required"] = Enum("element_id", "layout_type")
            }
        };

        private static Dictionary<string, object> SetUiStyleTool() => new()
        {
            ["name"] = "set_ui_style",
            ["description"] = "Áp dụng màu sắc, font, sprite cho UI element.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["element_id"] = Prop("string"),
                    ["color"] = Prop("string", "Hex RGBA: #RRGGBBAA hoặc #RRGGBB. VD: #FFFFFF80 = trắng 50%"),
                    ["font_size"] = Prop("integer", "Cỡ chữ (px)"),
                    ["font_style"] = Prop("string", enumVals: Enum("Normal", "Bold", "Italic", "BoldItalic")),
                    ["text_alignment"] = Prop("string", enumVals: Enum("Left", "Center", "Right", "Justified")),
                    ["sprite_path"] = Prop("string", "Asset path trong project, VD: Assets/UI/Sprites/button_bg.png"),
                    ["image_type"] = Prop("string", enumVals: Enum("Simple", "Sliced", "Tiled", "Filled")),
                    ["pixels_per_unit_multiplier"] = Prop("number", "Pixels Per Unit Multiplier cho Image (Sliced). Giá trị cao = bo góc nhỏ hơn, giá trị thấp = bo góc lớn hơn"),
                    ["raycast_target"] = Prop("boolean", defaultVal: true)
                },
                ["required"] = Enum("element_id")
            }
        };

        private static Dictionary<string, object> SetCanvasScalerTool() => new()
        {
            ["name"] = "set_canvas_scaler",
            ["description"] = "Cấu hình CanvasScaler riêng (nếu chưa set lúc create_prefab_ui).",
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
            ["description"] = "Lưu prefab ra file .prefab. Gọi sau khi đã tạo xong toàn bộ hierarchy.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string"),
                    ["overwrite"] = Prop("boolean", "True = ghi đè nếu file đã tồn tại", false)
                },
                ["required"] = Enum("prefab_id")
            }
        };

        private static Dictionary<string, object> QueryHierarchyTool() => new()
        {
            ["name"] = "query_ui_hierarchy",
            ["description"] = "Đọc lại cấu trúc hierarchy của prefab để verify sau khi tạo.",
            ["inputSchema"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["prefab_id"] = Prop("string"),
                    ["depth"] = Prop("integer", "Số cấp hierarchy trả về", 5)
                },
                ["required"] = Enum("prefab_id")
            }
        };
    }
}

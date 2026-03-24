using System.Collections.Generic;

namespace UnityMCP.Models
{
    [System.Serializable]
    public class MCPCommand
    {
        public string id;
        public string tool;
        public string paramsJson; // raw JSON string of params
    }

    [System.Serializable]
    public class MCPResponse
    {
        public string id;
        public bool success;
        public string resultJson;  // serialized result object
        public string error;
    }

    [System.Serializable]
    public class Vector2Data
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class ReferenceResolution
    {
        public float width = 1920;
        public float height = 1080;
    }

    // ── Tool param models ─────────────────────────────────────────────────────

    [System.Serializable]
    public class CreatePrefabParams
    {
        public string prefab_name;
        public string save_path;
        public string root_type = "Panel"; // "Canvas" = has Canvas root, "Panel"/"Image" = RectTransform only
        public string canvas_render_mode = "Overlay";
        public ReferenceResolution reference_resolution;
        public float match_width_or_height = 0.5f;
    }

    [System.Serializable]
    public class AddElementParams
    {
        public string prefab_id;
        public string element_type;
        public string name;
        public string parent_id;
        public string text_content;
    }

    [System.Serializable]
    public class SetRectParams
    {
        public string element_id;
        public string anchor_preset = "middle-center";
        public float? pos_x;
        public float? pos_y;
        public float? width;
        public float? height;
        public float? offset_left;
        public float? offset_right;
        public float? offset_top;
        public float? offset_bottom;
        public float pivot_x = 0.5f;
        public float pivot_y = 0.5f;
    }

    [System.Serializable]
    public class SetLayoutGroupParams
    {
        public string element_id;
        public string layout_type;
        public float spacing = 0;
        public float padding_left = 0;
        public float padding_right = 0;
        public float padding_top = 0;
        public float padding_bottom = 0;
        public string child_alignment = "UpperLeft";
        public bool control_child_width = false;
        public bool control_child_height = false;
        public bool force_expand_width = true;
        public bool force_expand_height = false;
        // Grid only
        public float? cell_width;
        public float? cell_height;
        public string constraint = "Flexible";
        public int constraint_count = 2;
    }

    [System.Serializable]
    public class SetStyleParams
    {
        public string element_id;
        public string color;
        public int? font_size;
        public string font_style;
        public string text_alignment;
        public string sprite_path;
        public string image_type;
        public float? pixels_per_unit_multiplier;
        public bool raycast_target = true;
    }

    [System.Serializable]
    public class SetCanvasScalerParams
    {
        public string element_id;
        public string scale_mode;
        public float reference_width = 1920;
        public float reference_height = 1080;
        public float match_width_or_height = 0.5f;
        public string screen_match_mode = "MatchWidthOrHeight";
    }

    [System.Serializable]
    public class SavePrefabParams
    {
        public string prefab_id;
        public bool overwrite = false;
    }

    [System.Serializable]
    public class QueryHierarchyParams
    {
        public string prefab_id;
        public int depth = 5;
    }

    [System.Serializable]
    public class BuildUiFromJsonParams
    {
        public string json_layout;        // optional: inline JSON (if empty, reads from temp_ui.json)
        public string prefab_name;        // name for the saved prefab
        public string save_path = "Assets/UI/Prefabs/"; // folder to save .prefab
        public float target_width = 1080;
        public float target_height = 1920;
    }
}

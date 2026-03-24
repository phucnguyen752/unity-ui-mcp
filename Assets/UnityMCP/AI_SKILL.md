# Unity UI MCP - AI Instructions

## Project Overview
Unity Editor plugin (MCP Server) that allows AI to create UI prefabs directly in Unity via MCP protocol.

## MCP Server
- Host: `http://localhost:7890/sse`
- Start in Unity: `Window > MCP Bridge > Start MCP Server`

## MANDATORY WORKFLOW when creating UI

### Step 1: Get editor config
```
get_editor_config -> get target_screen (e.g. 1080x1920)
```

### Step 2: Analyze reference image & calculate sizes
```
sizeDelta = target_screen × ratio%
anchoredPosition = target_screen × position_ratio%
fontSize = target_width × font_ratio%
```

**CRITICAL RULES:**
1. EVERY value (size, position, font) MUST be independently analyzed from the reference image.
2. DO NOT reuse example values as defaults. Examples below are ONLY for ONE specific design.
3. DO NOT round percentages (use 78.7% instead of 80% if more accurate).
4. Measure each element's ACTUAL position relative to its anchor, not estimated.
5. Text width should match the TEXT CONTENT width, NOT the parent width.
6. Check if element touches screen edges → use full width/stretch, NOT assumed margins.
7. Container with repeated children: size = n × child_size + (n-1) × spacing. Calculate, don't guess.
8. LayoutGroup defaults: control_child_width=false, control_child_height=false, force_expand=false.

**Analysis tips:**
- POSITION TIP: Add +50px to vertical position estimates (from bottom anchor). Elements are consistently placed too low.
- PPUM TIP: When uncertain between two PPUM values, ALWAYS pick the LOWER value (more rounded).
- SIZE TIP: Do NOT adjust sizes based on bias assumptions. Measure each element independently.

### Step 3: Write JSON to vision_json.json
Write the complete UI layout JSON to `Assets/UnityMCP/vision_json.json`.

**JSON Schema:**
```json
{
  "name": "PrefabName",
  "type": "Panel|Button|Image|Text|InputField|ScrollView|Toggle|Slider",
  "anchor": "stretch-full|top-left|top-center|top-right|middle-left|middle-center|middle-right|bottom-left|bottom-center|bottom-right|stretch-horizontal|stretch-vertical",
  "size": { "width": 1080, "height": 1920 },
  "color": "#RRGGBBAA",
  "text": "text content",
  "fontSize": 32,
  "fontStyle": "normal|bold|italic|bold-italic",
  "spritePath": "Assets/UnityMCP/Sprites/circle-512.png",
  "ppum": 6.25,
  "layout": "None|Horizontal|Vertical|Grid",
  "spacing": 10,
  "padding": { "left": 0, "right": 0, "top": 0, "bottom": 0 },
  "children": [ ... ]
}
```

### Step 4: Call build_ui_from_json
```
build_ui_from_json(prefab_name="MyPopup", save_path="Assets/UI/Prefabs/")
```
This reads from `vision_json.json`, builds the full hierarchy with Canvas, and auto-saves as `.prefab`.

## PPUM Formula (Pixels Per Unit Multiplier)
Every Panel/Button MUST have rounded corners via:
- `spritePath`: "Assets/UnityMCP/Sprites/circle-512.png"
- `ppum`: calculated value

```
PPUM = 250 / corner_radius_px
```

How to calculate:
```
Step 1: Measure element's shorter side (usually height)
Step 2: Estimate corner_ratio (how round vs shorter side)
        - Barely rounded → 0.15~0.25
        - Moderately rounded → 0.3~0.5
        - Pill-like → 0.5
        - Perfect circle → 0.5 (and width = height)
Step 3: corner_radius_px = shorter_side × corner_ratio
Step 4: PPUM = 250 / corner_radius_px
```

⚠ EVERY element MUST have its own PPUM calculation. NEVER reuse another element's PPUM.

## CHECKLIST BEFORE WRITING JSON
- [ ] Called get_editor_config?
- [ ] All sizes calculated from target_screen × ratio%?
- [ ] Each Panel/Button has spritePath + ppum?
- [ ] PPUM calculated independently for each element?
- [ ] Colors, font sizes, font styles set?
- [ ] Root element is stretch-full?
- [ ] Container size = n × child + (n-1) × spacing?

## Available Sprites
- `Assets/UnityMCP/Sprites/circle-512.png` - Circle 512x512, 9-sliced (border 250), for rounded corners

## Standard prefab structure
```json
{
  "name": "PopupName",
  "type": "Image",
  "anchor": "stretch-full",
  "color": "#00000099",
  "children": [
    {
      "name": "DialogPanel",
      "type": "Panel",
      "anchor": "bottom-center",
      "size": { "width": 850, "height": 1036 },
      "color": "#FFFFFF",
      "spritePath": "Assets/UnityMCP/Sprites/circle-512.png",
      "ppum": 6.25,
      "children": [
        { "name": "TxtTitle", "type": "Text", ... },
        { "name": "BtnClose", "type": "Button", "ppum": 1.0, ... },
        { "name": "BtnAction", "type": "Button", "ppum": 3.25, ... }
      ]
    }
  ]
}
```

# Unity UI MCP - AI Instructions

## Project Overview
Unity Editor plugin (MCP Server) that allows AI to create UI prefabs directly in Unity via MCP protocol.

## MCP Server
- Host: `http://localhost:7890/sse`
- Start in Unity: `Window > MCP Bridge > Start MCP Server`

## MANDATORY WORKFLOW when creating UI

### Step 1: Get editor config
```
get_editor_config -> get target_screen, vision_json_path, sprite_path, output_path
```
This also clears old JSON to prevent copying stale data.

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
Write the complete UI layout JSON to the `vision_json_path` returned by `get_editor_config` (project root).

**JSON Schema:**
```json
{
  "name": "PrefabName",
  "type": "Empty|Panel|Button|Image|Text|InputField|ScrollView|Toggle|Slider",
  "anchor": "stretch-full|top-left|top-center|...|stretch-horizontal|stretch-vertical",
  "size": { "width": 1080, "height": 1920 },
  "position": { "x": 0, "y": 0 },
  "color": "#RRGGBBAA",
  "text": "text content",
  "textColor": "#RRGGBB",
  "textAlignment": "Left|Center|Right|Justified",
  "fontSize": 32,
  "fontStyle": "Normal|Bold|Italic|BoldItalic",
  "spritePath": "<sprite_path from get_editor_config>",
  "ppum": 6.25,
  "layout": "None|Horizontal|Vertical|Grid",
  "spacing": 10,
  "controlChildWidth": false,
  "controlChildHeight": false,
  "forceExpandWidth": false,
  "forceExpandHeight": false,
  "childAlignment": "MiddleCenter",
  "padding": { "left": 0, "right": 0, "top": 0, "bottom": 0 },
  "children": [ ... ]
}
```

**Type notes:**
- `Empty`: RectTransform only (no visual component). Use for root container.
- `Panel`: Image with Sliced type (for rounded corners).
- `Image`: Simple Image.
- `Button`: Image + Button component + auto-created Label child with text.

### Step 4: Call build_ui_from_json
```
build_ui_from_json(prefab_name="MyPopup")
```
This reads from `vision_json_path`, builds the hierarchy (NO Canvas — prefab goes inside existing Canvas), and auto-saves to output_path.

## PPUM Formula (Pixels Per Unit Multiplier)
Every Panel/Button MUST have rounded corners via:
- `spritePath`: use the `sprite_path` from `get_editor_config`
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
- [ ] Root is type "Empty" with stretch-full?
- [ ] Overlay is separate Image child (not merged into root)?
- [ ] Container size = n × child + (n-1) × spacing?

## Standard prefab structure (NO Canvas)
```json
{
  "name": "PopupName",
  "type": "Empty",
  "anchor": "stretch-full",
  "children": [
    {
      "name": "Overlay",
      "type": "Image",
      "anchor": "stretch-full",
      "color": "#00000099"
    },
    {
      "name": "DialogPanel",
      "type": "Panel",
      "anchor": "middle-center",
      "size": { "width": 850, "height": 1036 },
      "color": "#FFFFFF",
      "spritePath": "<sprite_path>",
      "ppum": 6.25,
      "children": [
        { "name": "TxtTitle", "type": "Text", "..." : "..." },
        { "name": "BtnClose", "type": "Button", "ppum": 1.0, "..." : "..." },
        { "name": "BtnAction", "type": "Button", "ppum": 3.25, "..." : "..." }
      ]
    }
  ]
}
```

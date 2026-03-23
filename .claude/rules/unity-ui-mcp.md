# Unity UI MCP - Claude Instructions

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

### Step 2: Calculate sizes as % of target_screen
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

**How to analyze a reference image:**
- For each element, estimate its pixel dimensions relative to the full screen
- Convert to percentage: element_px / target_screen_px
- For positions: measure distance from anchor point in pixels, convert to %
- For font: compare text height to screen width

Example with target 1080x1920 (ONE specific popup design - DO NOT copy blindly):
- Dialog popup: 78.7%w × 54%h = 850 × 1036
- Button CTA: 53.2%w × 7.6%h = 575 × 145, pos_y=8.3%h=160 from bottom
- Square icon: 23%w = 248 × 248, pos_y=5.2%h=100 above center
- Description text: 48.6%w × 6.5%h = 525 × 125, pos_y=9.1%h=175 below center
- Banner height: 4%h = 77 (top-stretch)
- Close button: 6.5%w = 70 × 70
- Font title: 4.35%w = 47px
- Font body: 2.8%w = 30px
- Font button: 3.4%w = 37px

### Step 3: Create prefab
```
create_prefab_ui(root_type="Image") -> prefab without Canvas, drag into existing Canvas
create_prefab_ui(root_type="Canvas") -> prefab with its own Canvas
```

### Step 4: ALWAYS set rounded corner sprite for Panel/Button
**NEVER FORGET** - Every Panel, Button, Dialog MUST have:
```
set_ui_style(
  sprite_path = "Assets/UnityMCP/Sprites/circle-512.png",
  image_type = "Sliced",
  pixels_per_unit_multiplier = <value>
)
```

#### PPUM Formula (Pixels Per Unit Multiplier):
```
PPUM = sprite_border / desired_corner_radius_px
```
circle-512.png has border = 250px, so:
```
PPUM = 250 / corner_radius
```

| Desired corner | PPUM | Calculation | Used for |
|---------------|------|-------------|----------|
| 40px | 6.25 | 250/40 | Large Dialog/Panel |
| 50px | 5.0 | 250/50 | Banner/Header |
| 25px | 10.0 | 250/25 | Input field |
| 77px | 3.25 | 250/77 | Large CTA Button (h=145) |
| Full circle | 1.0 | 250/250 | Small/Close button |

IMPORTANT NOTES ON BUTTON PPUM:
- DO NOT default to pill shape (height/2). Analyze actual corner radius from reference image.
- CTA buttons typically have corner ~77px (PPUM=3.25), NOT full pill.
- Only use pill when the reference image clearly shows fully rounded ends.

### Step 5: Save prefab
```
save_prefab(overwrite=true)
```

## CHECKLIST BEFORE SAVE
- [ ] Called get_editor_config?
- [ ] All sizeDelta calculated from target_screen × ratio% (PRECISE, no rounding)?
- [ ] All Panel/Button have sprite circle-512.png + Sliced + PPUM (calculated as 250/corner)?
- [ ] Colors, font sizes, font styles set?
- [ ] Root element is full-stretch?

## Available Sprites
- `Assets/UnityMCP/Sprites/circle-512.png` - Circle 512x512, 9-sliced (border 250), used for rounded corners

## Standard prefab structure (without Canvas)
```
PrefabName (Image - full stretch, dark overlay)
  +-- ContentPanel (White Panel, PPUM=250/corner, e.g. 250/40=6.25)
        +-- Header/Banner (Colored Panel, PPUM=250/corner, e.g. 250/50=5)
        |     +-- TxtTitle (Text)
        +-- BtnClose (Button, PPUM=1, circle)
        +-- Content area...
        +-- BtnAction (Button, PPUM=250/corner from reference, e.g. 250/77=3.25)
```

## Code structure
- `Assets/UnityMCP/Core/` - MCP server, registry, dispatcher
- `Assets/UnityMCP/Handlers/` - Tool handlers (CreatePrefab, AddElement, SetRect, SetStyle...)
- `Assets/UnityMCP/Models/` - Data models
- `Assets/UnityMCP/Sprites/` - UI sprites

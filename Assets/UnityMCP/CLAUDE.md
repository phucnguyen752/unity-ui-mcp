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
6. Check if element touches screen edges → use full width/stretch, NOT assumed margins.
7. Container with repeated children: size = n × child_size + (n-1) × spacing. Calculate, don't guess.
8. LayoutGroup defaults: control_child_width=false, control_child_height=false, force_expand=false. Only enable when children MUST be resized by layout.

**How to analyze a reference image:**
- For each element, estimate its pixel dimensions relative to the full screen
- Convert to percentage: element_px / target_screen_px
- For positions: measure distance from anchor point in pixels, convert to %
- For font: compare text height to screen width
- POSITION TIP: When estimating vertical position, add +50px to your first estimate (from bottom anchor). You CONSISTENTLY place elements too low. This bias has persisted across 5+ iterations.
- POSITION CHECK: After calculating all positions, verify the spacing BETWEEN consecutive elements is proportional to the reference image.
- PPUM TIP: When you have multiple PPUM estimates (e.g. 3.8 vs 4.17), ALWAYS pick the LOWER value (more rounded). You consistently underestimate corner radius. If uncertain between two values, the lower PPUM is almost always correct.
- SIZE TIP: Do NOT adjust sizes based on bias assumptions. Measure each element independently from the reference image. If a value was confirmed correct, do NOT change it.

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

#### How to calculate PPUM from reference image:
```
Step 1: Measure element's shorter side (usually height)
Step 2: Estimate corner_ratio = how round are corners relative to shorter side?
        - Barely rounded → corner_ratio ≈ 0.15~0.25
        - Moderately rounded → corner_ratio ≈ 0.3~0.5
        - Very rounded (pill-like) → corner_ratio ≈ 0.5
        - Perfect circle → corner_ratio = 0.5 (and width = height)
Step 3: corner_radius_px = shorter_side × corner_ratio
Step 4: PPUM = 250 / corner_radius_px
```

**Example calculation (DO NOT copy values, always recalculate):**
```
A panel 850×1030 with small rounded corners:
  shorter_side = 850, corners look ~5% of width → corner = 850 × 0.047 = 40px
  PPUM = 250 / 40 = 6.25

A button 575×145 with moderately rounded corners:
  shorter_side = 145, corners look ~53% of height → corner = 145 × 0.53 = 77px
  PPUM = 250 / 77 = 3.25

A close button 65×65 (perfect circle):
  corner = 65 / 2 = 32.5 → but for circle sprite, just use PPUM = 1.0
```

**⚠ EVERY element MUST have its own PPUM calculation. NEVER reuse another element's PPUM.**

### Step 5: Save prefab
```
save_prefab(overwrite=true)
```

## CHECKLIST BEFORE SAVE
- [ ] Called get_editor_config?
- [ ] All sizeDelta calculated from target_screen × ratio% (PRECISE, no rounding)?
- [ ] **PPUM CHECK: Each element has its OWN PPUM calculated from corner_radius = shorter_side × corner_ratio?**
- [ ] All Panel/Button have sprite circle-512.png + Sliced + PPUM?
- [ ] Colors, font sizes, font styles set?
- [ ] Root element is full-stretch?
- [ ] Elements touching screen edges use full-width/stretch?
- [ ] LayoutGroup: control_child_size OFF, force_expand OFF (unless needed)?
- [ ] Container size = n × child + (n-1) × spacing?
- [ ] Position check: spacing between elements matches reference image proportions?

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

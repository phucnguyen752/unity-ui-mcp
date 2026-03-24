# Unity UI MCP

MCP Server plugin for Unity Editor that allows any AI (Claude, Gemini, ChatGPT) to create UI prefabs directly in Unity via the [Model Context Protocol](https://modelcontextprotocol.io/).

Give it a reference screenshot and it builds the full uGUI hierarchy — panels, buttons, text, images, rounded corners, layout groups — and saves as a `.prefab`.

## Features

- **MCP Server** hosted inside Unity Editor (SSE transport)
- **Works with any AI** that supports MCP: Claude Code, Cursor, Windsurf, ChatGPT, Gemini
- **JSON-driven workflow**: AI writes layout JSON → Unity builds prefab automatically
- **Rounded corners** via 9-sliced sprite with configurable PPUM
- **Auto-save** as `.prefab` ready to drag into any Canvas
- **AI Skill file** with sizing rules, PPUM formulas, and checklists to ensure accurate output

## Requirements

- Unity 2022.3 LTS or newer
- An AI tool that supports MCP (Claude Code, Cursor, Windsurf, etc.)

## Installation

### Via Unity Package Manager (Git URL)

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Paste:
```
https://github.com/phucdn/unity-ui-mcp.git?path=Assets/UnityMCP
```

### Manual

1. Clone this repo
2. Copy the `Assets/UnityMCP/` folder into your project's `Assets/` directory

## Quick Start

### 1. Start MCP Server

Open **Window > MCP Bridge** and click **Start MCP Server**.

The server runs at `http://localhost:7890/sse`.

### 2. Connect your AI

Add this to your AI tool's MCP config:

**Claude Code** (`.mcp.json` at project root):
```json
{
  "mcpServers": {
    "unity-ui-mcp": {
      "type": "sse",
      "url": "http://localhost:7890/sse"
    }
  }
}
```

**Cursor** (`.cursor/mcp.json`):
```json
{
  "mcpServers": {
    "unity-ui-mcp": {
      "serverUrl": "http://localhost:7890/sse"
    }
  }
}
```

Or click **Copy JSON Config** in the MCP Bridge window.

### 3. Install AI Skill (Rules)

In the MCP Bridge window, select your **Target IDE** and click **Install Rules**. This copies the `AI_SKILL.md` file to the correct location for your IDE so the AI knows how to use the MCP tools properly.

### 4. Create UI

Give your AI a reference screenshot and ask it to create the UI:

```
Create a UI prefab from this screenshot
```

The AI will:
1. Call `get_editor_config` to get the target screen resolution
2. Analyze the screenshot and calculate all sizes as percentages
3. Write the layout JSON to `vision_json.json`
4. Call `build_ui_from_json` to build and save the prefab

## MCP Tools

| Tool | Description |
|:-----|:------------|
| `get_editor_config` | Get target screen resolution and clear old layout JSON |
| `build_ui_from_json` | Build prefab from JSON layout (reads `vision_json.json`) |
| `create_prefab_ui` | Create prefab step-by-step (manual mode) |
| `add_ui_element` | Add element to prefab hierarchy |
| `set_rect_transform` | Set anchor, position, size |
| `set_ui_style` | Set color, font, sprite, PPUM |
| `set_layout_group` | Add Horizontal/Vertical/Grid layout |
| `save_prefab` | Save prefab to disk |
| `query_ui_hierarchy` | Inspect current prefab structure |

## JSON Layout Format

```json
{
  "name": "MyPopup",
  "type": "Image",
  "anchor": "stretch-full",
  "color": "#00000099",
  "children": [
    {
      "name": "DialogPanel",
      "type": "Panel",
      "anchor": "middle-center",
      "size": { "width": 850, "height": 1036 },
      "color": "#FFFFFF",
      "spritePath": "Assets/UnityMCP/Sprites/circle-512.png",
      "ppum": 6.25,
      "children": [
        {
          "name": "TxtTitle",
          "type": "Text",
          "anchor": "top-center",
          "text": "Hello World",
          "fontSize": 42,
          "fontStyle": "Bold",
          "color": "#333333"
        },
        {
          "name": "BtnAction",
          "type": "Button",
          "anchor": "bottom-center",
          "size": { "width": 575, "height": 145 },
          "position": { "x": 0, "y": 160 },
          "color": "#6CC88A",
          "spritePath": "Assets/UnityMCP/Sprites/circle-512.png",
          "ppum": 3.25,
          "text": "OK",
          "fontSize": 40
        }
      ]
    }
  ]
}
```

## Supported Element Types

| Type | Unity Components |
|:-----|:----------------|
| Panel | Image (+ optional LayoutGroup) |
| Button | Image + Button + Text child |
| Image | Image |
| Text | TextMeshProUGUI |
| InputField | TMP_InputField + Placeholder + Text |
| ScrollView | ScrollRect + Viewport + Content |
| Toggle | Toggle + Image |
| Slider | Slider + Image |

## PPUM (Rounded Corners)

Rounded corners use a 9-sliced circle sprite with **Pixels Per Unit Multiplier**:

```
PPUM = 250 / corner_radius_px
```

| Corner Radius | PPUM | Use Case |
|:-------------|:-----|:---------|
| 40px | 6.25 | Dialog panel |
| 50px | 5.0 | Banner/header |
| 77px | 3.25 | CTA button |
| 250px | 1.0 | Perfect circle (close button) |

## Project Structure

```
Assets/UnityMCP/
  Core/
    MCPBridgeWindow.cs    - Editor window (start/stop server, config)
    McpHttpServer.cs      - SSE MCP server
    McpToolRegistry.cs    - Tool schema definitions
    CommandDispatcher.cs  - Routes MCP calls to handlers
    LayoutParser.cs       - JSON → ComponentNode tree
    ToolDispatcher.cs     - Builds hierarchy from tree
  Handlers/
    GetEditorConfigHandler.cs     - Returns target screen config
    BuildUiFromJsonHandler.cs     - Reads JSON, builds prefab
  Editor/
    McpEditorWindow.cs    - Chat UI (optional)
    IAiApiClient.cs       - AI client interface
    ClaudeApiClient.cs    - Claude API client
    GeminiApiClient.cs    - Gemini API client
    OpenAIApiClient.cs    - OpenAI API client
  Models/
    CommandModels.cs      - Data models
  Sprites/
    circle-512.png        - 9-sliced circle for rounded corners
  AI_SKILL.md             - AI instructions (sizing rules, PPUM, checklist)
```

## License

MIT

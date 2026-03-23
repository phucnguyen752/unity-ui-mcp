# Unity MCP UI Builder

AI-powered uGUI layout builder. Upload a fakescreen screenshot → Claude Vision analyzes it → full Canvas hierarchy is created in your Unity Editor with LayoutGroups, anchors, and auto-generated C# bindings.

---

## Requirements

- Unity 2022.3 LTS or newer
- TextMeshPro (included via UPM)
- Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`)
- Claude API key (Anthropic)

---

## Installation

### Via Unity Package Manager (local)

1. Copy the `UnityMCP/` folder into your project's `Packages/` directory.
2. In UPM, click **+** → **Add package from disk** → select `package.json`.

### Add Newtonsoft.Json

In `Packages/manifest.json` add:
```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

---

## Setup

1. Open **Edit → Project Settings → Unity MCP**.
2. Paste your **Anthropic Claude API key**.
3. Optionally adjust the default reference resolution.

> ⚠️ Add `Assets/Editor/McpSettings.asset` to `.gitignore` to keep your key private.

---

## Usage

1. Open **Tools → MCP UI Builder**.
2. Drag a **PNG/JPG fakescreen** into the image slot.
3. Select a **target resolution** (1080p, 720p, mobile, or custom).
4. Check which **multiscreen variants** to generate (Portrait / Landscape / Tablet).
5. Click **Analyze & Build**.

The tool will:
- Send the image to Claude Vision
- Parse the returned layout JSON
- Create a Canvas + full hierarchy in your Hierarchy panel
- Generate `Assets/UI/Generated/<Name>References.cs` with typed field references
- (Optional) Save Portrait / Landscape / Tablet prefabs to `Assets/UI/Prefabs/Variants/`

---

## Generated code example

```csharp
// Assets/UI/Generated/LoginScreenReferences.cs  (auto-generated)
public class LoginScreenReferences : MonoBehaviour
{
    [SerializeField] public TMP_InputField usernameInput;
    [SerializeField] public TMP_InputField passwordInput;
    [SerializeField] public Button         loginButton;
    [SerializeField] public TextMeshProUGUI errorLabel;
}

// Your game code
public class LoginController : MonoBehaviour
{
    private LoginScreenReferences _refs;

    void Start()
    {
        _refs = GetComponent<LoginScreenReferences>();
        _refs.loginButton.onClick.AddListener(OnLogin);
    }
}
```

---

## Architecture

```
McpEditorWindow       — Chat UI, image upload, resolution/variant picker
ClaudeApiClient       — Vision analysis + multi-turn chat
LayoutParser          — JSON string → ComponentNode tree
ToolDispatcher        — Walks tree, calls tools on main thread
Tools/
  CreateElementTool   — Instantiates GameObjects + uGUI components
  LayoutTools         — HLayoutGroup / VLayoutGroup / GridLayoutGroup
                        + SetAnchorTool + SetSizeTool + SetStyleTool
CodeGen/
  UIReferencesGenerator — Emits typed C# binding class
Multiscreen/
  ScreenVariantBuilder  — Builds Portrait / Landscape / Tablet prefabs
McpSettings           — ScriptableObject: API key, model, defaults
```

---

## Supported component types

| JSON type    | Unity component(s)                  |
|:------------|:------------------------------------|
| Panel        | Image (+ optional LayoutGroup)      |
| Button       | Image + Button + TMP label child    |
| Image        | Image                               |
| Text         | TextMeshProUGUI                     |
| InputField   | TMP_InputField + Placeholder + Text |
| ScrollView   | ScrollRect + Viewport + Content     |
| Toggle       | Toggle + Image                      |
| Slider       | Slider + Image                      |

---

## Supported layout types

| JSON layout  | Unity component          |
|:------------|:-------------------------|
| horizontal   | HorizontalLayoutGroup    |
| vertical     | VerticalLayoutGroup      |
| grid         | GridLayoutGroup          |

---

## Tips

- Name your fakescreen layers clearly — the AI uses them as GameObject names.
- The JSON `anchor` field supports: `top-left`, `top-center`, `top-right`, `middle-left`, `middle-center`, `middle-right`, `bottom-left`, `bottom-center`, `bottom-right`, `stretch-horizontal`, `stretch-vertical`, `stretch-full`.
- All operations are Undo-safe (Ctrl+Z works).

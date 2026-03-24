using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace UnityMCP
{
    /// <summary>
    /// Creates a GameObject with the correct uGUI components for a given ComponentNode.
    /// </summary>
    public static class CreateElementTool
    {
        public static GameObject Create(ComponentNode node, Transform parent)
        {
            var go = new GameObject(node.name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {node.name}");
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);

            // Every UI element needs a RectTransform (replaces regular Transform)
            go.AddComponent<RectTransform>();

            switch (node.type)
            {
                case UiComponentType.Panel:       SetupPanel(go, node);      break;
                case UiComponentType.Button:      SetupButton(go, node);     break;
                case UiComponentType.Image:       SetupImage(go, node);      break;
                case UiComponentType.Text:        SetupText(go, node);       break;
                case UiComponentType.InputField:  SetupInputField(go, node); break;
                case UiComponentType.ScrollView:  SetupScrollView(go, node); break;
                case UiComponentType.Toggle:      SetupToggle(go, node);     break;
                case UiComponentType.Slider:      SetupSlider(go, node);     break;
            }

            return go;
        }

        // ── Component setups ──────────────────────────────────

        private static void SetupPanel(GameObject go, ComponentNode node)
        {
            var img   = go.AddComponent<Image>();
            img.color = node.color;
            img.type  = Image.Type.Sliced;
        }

        private static void SetupButton(GameObject go, ComponentNode node)
        {
            var img    = go.AddComponent<Image>();
            img.color  = node.color;
            img.type   = Image.Type.Sliced;

            go.AddComponent<Button>();

            // Label child
            var labelGo  = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(labelGo, "Create Label");
            GameObjectUtility.SetParentAndAlign(labelGo, go);

            var rt = labelGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp        = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text       = string.IsNullOrEmpty(node.text) ? "Button" : node.text;
            tmp.fontSize   = node.fontSize;
            tmp.fontStyle  = ConvertFontStyle(node.fontStyle);
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.color      = Color.white;
        }

        private static void SetupImage(GameObject go, ComponentNode node)
        {
            var img   = go.AddComponent<Image>();
            img.color = node.color;
        }

        private static void SetupText(GameObject go, ComponentNode node)
        {
            var tmp       = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = node.text;
            tmp.fontSize  = node.fontSize;
            tmp.fontStyle = ConvertFontStyle(node.fontStyle);
            tmp.color     = node.color;
        }

        private static void SetupInputField(GameObject go, ComponentNode node)
        {
            var bg    = go.AddComponent<Image>();
            bg.color  = node.color;

            var field = go.AddComponent<TMP_InputField>();

            // Placeholder
            var phGo  = new GameObject("Placeholder");
            Undo.RegisterCreatedObjectUndo(phGo, "Create Placeholder");
            GameObjectUtility.SetParentAndAlign(phGo, go);
            var phRt  = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(8, 4);
            phRt.offsetMax = new Vector2(-8, -4);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text      = string.IsNullOrEmpty(node.text) ? "Enter text..." : node.text;
            phTmp.color     = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            phTmp.fontSize  = node.fontSize;

            // Text area
            var taGo  = new GameObject("Text Area");
            Undo.RegisterCreatedObjectUndo(taGo, "Create Text Area");
            GameObjectUtility.SetParentAndAlign(taGo, go);
            var taRt  = taGo.AddComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(8, 4);
            taRt.offsetMax = new Vector2(-8, -4);
            var taTmp = taGo.AddComponent<TextMeshProUGUI>();
            taTmp.fontSize = node.fontSize;
            taTmp.color    = Color.black;

            field.placeholder = phTmp;
            field.textComponent = taTmp;
        }

        private static void SetupScrollView(GameObject go, ComponentNode node)
        {
            var img   = go.AddComponent<Image>();
            img.color = node.color;

            var sv    = go.AddComponent<ScrollRect>();

            // Viewport
            var vpGo  = new GameObject("Viewport");
            Undo.RegisterCreatedObjectUndo(vpGo, "Create Viewport");
            GameObjectUtility.SetParentAndAlign(vpGo, go);
            var vpRt  = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGo = new GameObject("Content");
            Undo.RegisterCreatedObjectUndo(contentGo, "Create Content");
            GameObjectUtility.SetParentAndAlign(contentGo, vpGo);
            var cRt       = contentGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot     = new Vector2(0.5f, 1);
            cRt.sizeDelta = Vector2.zero;

            sv.viewport = vpRt;
            sv.content  = cRt;
        }

        private static void SetupToggle(GameObject go, ComponentNode node)
        {
            var toggle = go.AddComponent<Toggle>();
            var bg     = go.AddComponent<Image>();
            bg.color   = node.color;
            toggle.targetGraphic = bg;
        }

        private static void SetupSlider(GameObject go, ComponentNode node)
        {
            var img   = go.AddComponent<Image>();
            img.color = node.color;
            go.AddComponent<Slider>();
        }

        // ── Helpers ───────────────────────────────────────────
        private static FontStyles ConvertFontStyle(FontStyle s) => s switch
        {
            FontStyle.Bold           => FontStyles.Bold,
            FontStyle.Italic         => FontStyles.Italic,
            FontStyle.BoldAndItalic  => FontStyles.Bold | FontStyles.Italic,
            _                        => FontStyles.Normal,
        };
    }
}

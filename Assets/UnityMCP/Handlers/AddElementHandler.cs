using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityMCP.Core;
using UnityMCP.Models;

namespace UnityMCP.Handlers
{
    public static class AddElementHandler
    {
        public static object Execute(AddElementParams p)
        {
            // Xác định parent
            Transform parent = null;
            if (!string.IsNullOrEmpty(p.parent_id))
            {
                var parentGO = ElementRegistry.GetElement(p.parent_id);
                if (parentGO == null)
                    throw new System.Exception($"parent_id '{p.parent_id}' không tồn tại.");
                parent = parentGO.transform;
            }
            else if (ElementRegistry.TryGetPrefab(p.prefab_id, out var rootGO, out _))
            {
                parent = rootGO.transform;
            }
            else
            {
                throw new System.Exception($"prefab_id '{p.prefab_id}' không tồn tại.");
            }

            GameObject go = p.element_type switch
            {
                "Panel"      => CreatePanel(p.name, parent),
                "Button"     => CreateButton(p.name, parent, p.text_content),
                "Text"       => CreateText(p.name, parent, p.text_content),
                "Image"      => CreateImage(p.name, parent),
                "InputField" => CreateInputField(p.name, parent, p.text_content),
                "ScrollView" => CreateScrollView(p.name, parent),
                "Toggle"     => CreateToggle(p.name, parent, p.text_content),
                "Slider"     => CreateSlider(p.name, parent),
                "Dropdown"   => CreateDropdown(p.name, parent),
                _            => throw new System.NotSupportedException($"element_type '{p.element_type}' không được hỗ trợ.")
            };

            var elemId = ElementRegistry.Register(go);
            var rt = go.GetComponent<RectTransform>();

            return new
            {
                element_id = elemId,
                name = go.name,
                type = p.element_type,
                parent_path = parent.gameObject.name,
                rect = new
                {
                    anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                    anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y },
                    sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y }
                }
            };
        }

        // ── Factory methods ───────────────────────────────────────────────────

        private static GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.5f);
            FullStretch(go);
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            go.AddComponent<Button>();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 40);

            // Label child
            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(label) ? "Button" : label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;
            tmp.color = Color.black;
            FullStretch(textGO);

            return go;
        }

        private static GameObject CreateText(string name, Transform parent, string content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(content) ? "Text" : content;
            tmp.fontSize = 16;
            tmp.color = Color.black;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static GameObject CreateInputField(string name, Transform parent, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 40);

            var bg = go.AddComponent<Image>();
            bg.color = Color.white;

            var field = go.AddComponent<TMP_InputField>();

            var textAreaGO = new GameObject("TextArea", typeof(RectTransform));
            textAreaGO.transform.SetParent(go.transform, false);
            FullStretch(textAreaGO);
            textAreaGO.AddComponent<RectMask2D>();

            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(textAreaGO.transform, false);
            FullStretch(phGO);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = string.IsNullOrEmpty(placeholder) ? "Enter text..." : placeholder;
            ph.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            ph.fontSize = 14;

            var inputTextGO = new GameObject("Text", typeof(RectTransform));
            inputTextGO.transform.SetParent(textAreaGO.transform, false);
            FullStretch(inputTextGO);
            var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.black;

            field.textViewport = textAreaGO.GetComponent<RectTransform>();
            field.textComponent = inputText;
            field.placeholder = ph;

            return go;
        }

        private static GameObject CreateScrollView(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            FullStretch(go);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.1f);

            var scrollRect = go.AddComponent<ScrollRect>();

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            FullStretch(viewport);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 300);

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Register content
            ElementRegistry.Register(content);

            return go;
        }

        private static GameObject CreateToggle(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 30);

            var toggle = go.AddComponent<Toggle>();

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.sizeDelta = new Vector2(20, 20);
            bgRT.anchoredPosition = new Vector2(10, 0);
            bg.AddComponent<Image>().color = Color.white;

            var checkmark = new GameObject("Checkmark", typeof(RectTransform));
            checkmark.transform.SetParent(bg.transform, false);
            FullStretch(checkmark);
            var cm = checkmark.AddComponent<Image>();
            cm.color = new Color(0.2f, 0.6f, 1f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var lblRT = labelGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0, 0); lblRT.anchorMax = new Vector2(1, 1);
            lblRT.offsetMin = new Vector2(26, 0); lblRT.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(label) ? "Toggle" : label;
            tmp.fontSize = 14;
            tmp.color = Color.black;

            toggle.graphic = cm;
            return go;
        }

        private static GameObject CreateSlider(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 20);

            var slider = go.AddComponent<Slider>();

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(go.transform, false);
            FullStretch(bgGO);
            bgGO.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);

            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(go.transform, false);
            FullStretch(fillAreaGO);
            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            FullStretch(fillGO);
            var fill = fillGO.AddComponent<Image>();
            fill.color = new Color(0.2f, 0.6f, 1f);

            var handleGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleGO.transform.SetParent(go.transform, false);
            FullStretch(handleGO);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleGO.transform, false);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 0);
            handle.AddComponent<Image>().color = Color.white;

            slider.fillRect = fillGO.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.value = 0.5f;

            return go;
        }

        private static GameObject CreateDropdown(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            go.AddComponent<Image>().color = Color.white;
            go.AddComponent<TMP_Dropdown>();
            return go;
        }

        private static void FullStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

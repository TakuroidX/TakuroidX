using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Helpers for building uGUI in code (no prefabs or scene editing needed).</summary>
    public static class Ui
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font) return _font;
                // Unity 2022.2+ has LegacyRuntime.ttf; older versions have Arial.ttf
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (ArgumentException) { }
                if (!_font)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (ArgumentException) { }
                }
                if (!_font) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }

        public static RectTransform Node(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Node(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var img = Node(name, parent, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        public static Image FullImage(Transform parent, string name, Color color)
        {
            var img = Stretch(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(Transform parent, string name, string text, int fontSize, Color color, Vector2 pos, Vector2 size,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var t = Node(name, parent, pos, size).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, int fontSize,
            Action onClick, out Text labelText)
        {
            var img = Image(parent, name, pos, size, color, ProceduralSprites.RoundedRect);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            labelText = Text(img.transform, "Label", label, fontSize, Color.white, Vector2.zero, size);
            return button;
        }

        /// <summary>Glowing outline (for the frame and highlights)</summary>
        public static Image Outline(Transform parent, string name, Vector2 pos, Vector2 size, Color color) =>
            Image(parent, name, pos, size, color, ProceduralSprites.RoundedOutline);
    }
}

using SlotSdk;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Display for one symbol. Shows a background + neon outline + label (or shape).
    /// The reel reuses cells (only swapping the symbol, no allocations).
    /// </summary>
    public sealed class SymbolCell
    {
        public RectTransform Root { get; }
        private readonly Image _bg;
        private readonly Image _outline;
        private readonly Image _shape;
        private readonly Text _label;
        private readonly CanvasGroup _group;
        private int _symbol = -1;
        private Color _color;
        private bool _highlight;

        public SymbolCell(Transform parent, float size)
        {
            Root = Ui.Node("Cell", parent, Vector2.zero, new Vector2(size, size));
            _group = Root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            var inner = size - 16f;
            _bg = Ui.Image(Root, "Bg", Vector2.zero, new Vector2(inner, inner), Color.black, ProceduralSprites.RoundedRect);
            _outline = Ui.Outline(Root, "Outline", Vector2.zero, new Vector2(inner, inner), Color.white);
            _shape = Ui.Image(Root, "Shape", Vector2.zero, new Vector2(inner * 0.72f, inner * 0.72f), Color.white);
            _label = Ui.Text(Root, "Label", "", 40, Color.white, Vector2.zero, new Vector2(inner, inner));
        }

        public int Symbol => _symbol;

        public void SetSymbol(MathModel model, int symbol)
        {
            if (symbol == _symbol) return;
            _symbol = symbol;
            var id = model.Symbols[symbol].Id;
            _color = GameSpec.SymbolColor(id);
            _bg.color = new Color(_color.r * 0.16f, _color.g * 0.16f, _color.b * 0.16f + 0.04f, 1f);

            var label = GameSpec.SymbolLabel(id);
            _label.gameObject.SetActive(label != null);
            _shape.gameObject.SetActive(label == null);
            if (label != null)
            {
                _label.text = label;
                _label.fontSize = label.Length <= 1 ? 120 : label.Length <= 4 ? 52 : 36;
                _label.color = _color;
            }
            else
            {
                _shape.sprite = id == "SCATTER" ? ProceduralSprites.Star : ProceduralSprites.Diamond;
                _shape.color = _color;
            }
            ApplyHighlight(0f);
        }

        public void SetY(float y) => Root.anchoredPosition = new Vector2(0f, y);

        public void SetDimmed(bool dimmed) => _group.alpha = dimmed ? GameSpec.DimAlpha : 1f;

        public void SetHighlight(bool on)
        {
            _highlight = on;
            if (!on) Root.localScale = Vector3.one;
            ApplyHighlight(0f);
        }

        /// <summary>Pulse while highlighted (t = elapsed seconds)</summary>
        public void Pulse(float t)
        {
            if (!_highlight) return;
            var k = 0.5f + 0.5f * Mathf.Sin(t * 9f);
            Root.localScale = Vector3.one * (1f + 0.06f * k);
            ApplyHighlight(k);
        }

        private void ApplyHighlight(float k)
        {
            var a = _highlight ? 0.75f + 0.25f * k : 0.55f;
            _outline.color = new Color(_color.r, _color.g, _color.b, a);
        }
    }
}

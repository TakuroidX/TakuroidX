using System.Collections.Generic;
using SlotSdk;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Draws paylines (as line-segment Images) and the line-number markers on both sides of the reels.
    /// </summary>
    public sealed class PaylineView
    {
        private readonly MathModel _model;
        private readonly GameObject[] _lines;
        private readonly Image[] _leftMarkers;
        private readonly Image[] _rightMarkers;

        public PaylineView(Transform reelArea, MathModel model, float cell, float spacing)
        {
            _model = model;
            var n = model.Paylines.Length;
            _lines = new GameObject[n];
            _leftMarkers = new Image[n];
            _rightMarkers = new Image[n];

            var layer = Ui.Stretch("Paylines", reelArea);
            for (var l = 0; l < n; l++)
            {
                var color = GameSpec.LineColor(l, n);
                var root = Ui.Stretch($"Line{l + 1}", layer).gameObject;
                var pts = new List<Vector2>();
                var areaHalf = CenterX(model.ReelCount - 1, model.ReelCount, cell, spacing) + cell / 2f;
                pts.Add(new Vector2(-areaHalf - 10, CenterY(model.Paylines[l][0], model.RowCount, cell)));
                for (var r = 0; r < model.ReelCount; r++)
                    pts.Add(new Vector2(CenterX(r, model.ReelCount, cell, spacing), CenterY(model.Paylines[l][r], model.RowCount, cell)));
                pts.Add(new Vector2(areaHalf + 10, CenterY(model.Paylines[l][model.ReelCount - 1], model.RowCount, cell)));
                for (var i = 0; i + 1 < pts.Count; i++) Segment(root.transform, pts[i], pts[i + 1], color);
                root.SetActive(false);
                _lines[l] = root;
            }

            // Markers: stack the lines that start (end) on the same row vertically
            var x = CenterX(model.ReelCount - 1, model.ReelCount, cell, spacing) + cell / 2f + 34f;
            PlaceMarkers(reelArea, -x, 0, _leftMarkers, cell);
            PlaceMarkers(reelArea, x, model.ReelCount - 1, _rightMarkers, cell);
        }

        public static float CenterX(int reel, int reels, float cell, float spacing) => (reel - (reels - 1) / 2f) * (cell + spacing);

        public static float CenterY(int row, int rows, float cell) => ((rows - 1) / 2f - row) * cell;

        public void Show(int line, bool on)
        {
            _lines[line].SetActive(on);
            var c = on ? GameSpec.LineColor(line, _lines.Length) : new Color(0.25f, 0.27f, 0.35f, 1f);
            _leftMarkers[line].color = c;
            _rightMarkers[line].color = c;
        }

        public void HideAll()
        {
            for (var l = 0; l < _lines.Length; l++) Show(l, false);
        }

        private void PlaceMarkers(Transform parent, float x, int reel, Image[] markers, float cell)
        {
            for (var row = 0; row < _model.RowCount; row++)
            {
                var group = new List<int>();
                for (var l = 0; l < _model.Paylines.Length; l++)
                    if (_model.Paylines[l][reel] == row) group.Add(l);
                for (var j = 0; j < group.Count; j++)
                {
                    var y = CenterY(row, _model.RowCount, cell) + (j - (group.Count - 1) / 2f) * 27f;
                    var img = Ui.Image(parent, $"Marker{group[j] + 1}", new Vector2(x, y), new Vector2(40, 24),
                        new Color(0.25f, 0.27f, 0.35f, 1f), ProceduralSprites.RoundedRect);
                    Ui.Text(img.transform, "N", (group[j] + 1).ToString(), 15, Color.white, Vector2.zero, new Vector2(40, 24));
                    markers[group[j]] = img;
                }
            }
        }

        private static void Segment(Transform parent, Vector2 a, Vector2 b, Color color)
        {
            var d = b - a;
            var img = Ui.Image(parent, "Seg", (a + b) / 2f, new Vector2(d.magnitude + 6f, 7f), color, ProceduralSprites.White);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}

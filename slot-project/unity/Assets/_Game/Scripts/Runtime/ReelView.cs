using System;
using SlotSdk;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// One reel. Stops are decided by the SDK; this class only animates toward them.
    /// Movement math lives in ReelMotion (pure logic).
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        private enum Phase { Idle, SpinUp, Spinning, Stopping, Bouncing }

        private MathModel _model;
        private int[] _strip;
        private int _rows;
        private float _cell;
        private SymbolCell[] _cells;
        private ReelMotion _motion;
        private Image _anticipation;

        private Phase _phase = Phase.Idle;
        private ReelTiming _timing;
        private float _t;
        private float _velocity;
        private double _from;
        private double _to;
        private float _duration;
        private long _target;

        public int ReelIndex { get; private set; }
        public bool IsSpinning => _phase != Phase.Idle;
        public bool IsStopRequested => _phase == Phase.Stopping || _phase == Phase.Bouncing;
        public bool IsAnticipating => _anticipation != null && _anticipation.gameObject.activeSelf;
        public event Action<ReelView> Stopped;

        public void Initialize(MathModel model, int reelIndex, int initialStop, float cellSize)
        {
            _model = model;
            ReelIndex = reelIndex;
            _strip = model.Reels[reelIndex];
            _rows = model.RowCount;
            _cell = cellSize;
            _motion = new ReelMotion(_strip.Length, initialStop);

            var rt = (RectTransform)transform;
            // Viewport (masks outside the window)
            var viewport = Ui.Node("Viewport", rt, Vector2.zero, new Vector2(cellSize, cellSize * _rows));
            viewport.gameObject.AddComponent<RectMask2D>();
            // Content: the top row's center is at y=0
            var content = Ui.Node("Content", viewport, new Vector2(0, cellSize * (_rows - 1) / 2f), new Vector2(cellSize, cellSize));
            _cells = new SymbolCell[_rows + 2];
            for (var i = 0; i < _cells.Length; i++) _cells[i] = new SymbolCell(content, cellSize);

            _anticipation = Ui.Outline(rt, "Anticipation", Vector2.zero, new Vector2(cellSize + 18, cellSize * _rows + 18), GameSpec.Gold);
            _anticipation.gameObject.SetActive(false);
            Render();
        }

        public void StartSpin(ReelTiming timing)
        {
            _timing = timing;
            _phase = Phase.SpinUp;
            _t = 0f;
            _velocity = 0f;
            SetAllHighlights(false);
            SetDimmed(false);
        }

        /// <summary>Starts decelerating toward the stop. quick = immediate (STOP button).</summary>
        public void RequestStop(int stop, bool quick)
        {
            if (_phase == Phase.Idle || IsStopRequested) return;
            _target = _motion.PlanStop(stop, quick ? 0 : 1, _rows);
            _from = _motion.Position;
            _to = _target - GameSpec.BounceOvershoot;
            var speed = Mathf.Max(_velocity, _timing.SpeedSymbolsPerSec * 0.5f);
            // easeOutQuad has an initial slope of 2, so D = 2d/v keeps the speed continuous
            _duration = Mathf.Max(0.08f, 2f * (float)(_from - _to) / speed);
            _t = 0f;
            _phase = Phase.Stopping;
        }

        public void SetAnticipation(bool on)
        {
            if (_anticipation != null) _anticipation.gameObject.SetActive(on);
        }

        /// <summary>The cell at the given row while stopped</summary>
        public SymbolCell CellAtRow(int row) => _cells[row + 1];

        public void SetAllHighlights(bool on)
        {
            foreach (var c in _cells) c.SetHighlight(on);
        }

        public void SetDimmed(bool dimmed)
        {
            foreach (var c in _cells) c.SetDimmed(dimmed);
        }

        public void Pulse(float t)
        {
            foreach (var c in _cells) c.Pulse(t);
            if (_anticipation != null && _anticipation.gameObject.activeSelf)
            {
                var col = GameSpec.Gold;
                col.a = 0.55f + 0.45f * Mathf.Sin(t * 14f);
                _anticipation.color = col;
            }
        }

        private void Update()
        {
            if (_phase == Phase.Idle) return;
            var dt = Time.deltaTime;
            _t += dt;
            switch (_phase)
            {
                case Phase.SpinUp:
                {
                    var u = Mathf.Clamp01(_t / Mathf.Max(0.001f, _timing.SpinUpSeconds));
                    _velocity = _timing.SpeedSymbolsPerSec * u * u;
                    _motion.Position -= _velocity * dt;
                    if (u >= 1f) { _phase = Phase.Spinning; _velocity = _timing.SpeedSymbolsPerSec; }
                    break;
                }
                case Phase.Spinning:
                    _motion.Position -= _velocity * dt;
                    break;
                case Phase.Stopping:
                {
                    var u = Mathf.Clamp01(_t / _duration);
                    var e = 1f - (1f - u) * (1f - u);
                    _motion.Position = _from + (_to - _from) * e;
                    if (u >= 1f)
                    {
                        _phase = Phase.Bouncing;
                        _t = 0f;
                        _from = _motion.Position;
                    }
                    break;
                }
                case Phase.Bouncing:
                {
                    var u = Mathf.Clamp01(_t / Mathf.Max(0.001f, _timing.BounceSeconds));
                    var e = 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI);
                    _motion.Position = _from + (_target - _from) * e;
                    if (u >= 1f)
                    {
                        _motion.Settle(_target);
                        _phase = Phase.Idle;
                        _velocity = 0f;
                        Render();
                        Stopped?.Invoke(this);
                        return;
                    }
                    break;
                }
            }
            Render();
        }

        private void Render()
        {
            var pos = _motion.Position;
            var top = (long)Math.Floor(pos);
            for (var k = 0; k < _cells.Length; k++)
            {
                var index = top - 1 + k;
                var cell = _cells[k];
                cell.SetY(-(float)(index - pos) * _cell);
                cell.SetSymbol(_model, _strip[_motion.StripIndexOf(index)]);
            }
        }
    }
}

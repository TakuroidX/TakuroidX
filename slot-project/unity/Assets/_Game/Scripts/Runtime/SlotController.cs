using System;
using System.Collections;
using System.Linq;
using SlotSdk;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    /// <summary>
    /// Main game controller (game spec "Game Flow" sheet).
    ///
    /// Stops, wins, balance, and free spin state are all decided by the SDK's SlotGame at the start of the spin.
    /// This class only presents those results (reel animation, win presentation). STOP / turbo / skip never change the result.
    /// </summary>
    public sealed class SlotController : MonoBehaviour, SlotUi.IHandler
    {
        private enum State { Boot, Idle, Spinning, Presenting }

        private MathModel _model;
        private SlotGame _game;
        private CryptoRng _rng;
        private SlotUi _ui;
        private SynthAudio _audio;

        private State _state = State.Boot;
        private bool _turbo;
        private bool _auto;
        private bool _spinRequested;
        private bool _stopRequested;
        private bool _skipRequested;
        private bool _overlayActive;
        private bool _forceFeature;
        private bool _inFeature;
        private long _shownBalance;
        private SpinResult _lastResult;
        private Coroutine _lineCycle;

        public SlotGame Game => _game;
        public bool IsIdle => _state == State.Idle;
        /// <summary>Number of paid spins played (for tests and stats)</summary>
        public int PaidSpins { get; private set; }
        public bool Turbo { get => _turbo; set { _turbo = value; Refresh(); } }

        private ReelTiming Timing => _turbo ? GameSpec.Turbo : GameSpec.Normal;

        // ------------------------------------------------------------------ lifecycle
        private void Awake()
        {
            var json = Resources.Load<TextAsset>(GameSpec.MathResource);
            if (json == null)
            {
                Debug.LogError($"Math resource '{GameSpec.MathResource}' not found. Run tools/make_specs.sh to sync it.");
                enabled = false;
                return;
            }
            _model = MathModel.FromJson(json.text);
            _rng = new CryptoRng();
            _game = new SlotGame(_model, _rng, GameSpec.StartingCredits);
            SaveStore.Load(_game, out _turbo);
            _shownBalance = _game.Balance;

            EnsureCamera();
            _audio = gameObject.AddComponent<SynthAudio>();
            _ui = SlotUi.Build(transform, _model, this, r => UnityEngine.Random.Range(0, _model.Reels[r].Length));
        }

        private void Start()
        {
            if (!enabled) return;
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            StartCoroutine(MainLoop());
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnDestroy() => _rng?.Dispose();

        private void Update()
        {
            if (_ui == null) return;
            var t = Time.time;
            foreach (var reel in _ui.Reels) reel.Pulse(t);
            HandleKeyboard();
        }

        private void HandleKeyboard()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) OnSpinPressed();
            if (Input.GetKeyDown(KeyCode.A)) OnToggleAuto();
            if (Input.GetKeyDown(KeyCode.T)) OnToggleTurbo();
            if (Input.GetKeyDown(KeyCode.P)) OnTogglePaytable();
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals)) OnBet(+1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) OnBet(-1);
            if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F9))
            {
                _forceFeature = true;
                _ui.Message.text = "DEBUG: NEXT SPIN TRIGGERS FREE SPINS";
            }
#endif
        }

        // ------------------------------------------------------------------ IHandler (buttons / keys)
        public void OnSpinPressed()
        {
            if (_overlayActive) { _skipRequested = true; return; }
            if (_ui.PaytablePanel.activeSelf) { OnTogglePaytable(); return; }
            switch (_state)
            {
                case State.Idle: _spinRequested = true; break;
                case State.Spinning: _stopRequested = true; break;
                case State.Presenting: _skipRequested = true; break;
            }
        }

        public void OnBet(int delta)
        {
            if (_state != State.Idle || _game.IsInFreeSpins) return;
            var level = _game.BetLevel + delta;
            if (level < 0 || level >= _model.Bet.LineBetLevels.Count) return;
            _game.SetBetLevel(level);
            Save();
            Refresh();
        }

        public void OnToggleAuto()
        {
            _auto = !_auto;
            Refresh();
        }

        public void OnToggleTurbo()
        {
            _turbo = !_turbo;
            Save();
            Refresh();
        }

        public void OnTogglePaytable()
        {
            if (_ui.PaytablePanel.activeSelf) { _ui.PaytablePanel.SetActive(false); return; }
            if (_state != State.Idle || _game.IsInFreeSpins) return;
            _ui.FillPaytable(_model, _game.LineBet, _game.TotalBet);
            _ui.PaytablePanel.SetActive(true);
        }

        public void OnRefill()
        {
            if (_state != State.Idle || _game.IsInFreeSpins || _game.CanSpin) return;
            _game.AddCredits(GameSpec.StartingCredits - _game.Balance);
            _shownBalance = _game.Balance;
            Save();
            _ui.Message.text = "CREDIT REFILLED";
            Refresh();
        }

        public void OnOverlayClicked() => _skipRequested = true;

        /// <summary>Spin request for tests</summary>
        public void RequestSpin() => _spinRequested = true;

        // ------------------------------------------------------------------ flow
        private IEnumerator MainLoop()
        {
            yield return null; // wait one frame for the layout to settle
            if (_game.IsInFreeSpins) yield return FreeSpins(true);

            while (true)
            {
                _state = State.Idle;
                Refresh();
                if (_lastResult != null && _lastResult.TotalWin > 0) _lineCycle = StartCoroutine(CycleLines(_lastResult));

                var autoWait = 0f;
                while (!_spinRequested)
                {
                    if (_auto)
                    {
                        if (!_game.CanSpin) { _auto = false; Refresh(); }
                        else if ((autoWait += Time.deltaTime) >= Timing.AutoIntervalSeconds) break;
                    }
                    yield return null;
                }
                _spinRequested = false;
                StopLineCycle();

                if (!_game.CanSpin)
                {
                    _ui.Message.text = "NOT ENOUGH CREDIT";
                    continue;
                }

                var result = _forceFeature ? _game.SpinWithStops(FeatureStops()) : _game.Spin();
                _forceFeature = false;
                PaidSpins++;
                _shownBalance -= result.TotalBet;
                yield return PlaySpin(result, 0, result.TotalWin);

                var tier = GameSpec.TierOf(result.TotalWin, result.TotalBet);
                if (tier >= WinTier.Big)
                {
                    _auto = false;
                    yield return BigWin(tier, result.TotalWin);
                }
                _lastResult = result;
                Save();

                if (result.FeatureTriggered)
                {
                    _lastResult = null;
                    yield return FreeSpins(false);
                }
            }
        }

        private IEnumerator FreeSpins(bool resumed)
        {
            StopLineCycle();
            _inFeature = true;
            _ui.Background.color = GameSpec.FreeSpinBackground;
            Refresh();
            if (!resumed)
                yield return Banner($"FREE SPINS!\n<size=56>{_model.FreeSpins.AwardSpins} SPINS  ALL WINS x{_model.FreeSpins.Multiplier}</size>", "",
                    GameSpec.FreeSpinIntroSeconds);

            while (_game.IsInFreeSpins)
            {
                yield return WaitOrSkip(Timing.FreeSpinIntervalSeconds, false);
                var before = _game.FeatureTotalWin;
                var r = _game.Spin();
                Refresh();
                yield return PlaySpin(r, before, r.FeatureTotalWin);

                var tier = GameSpec.TierOf(r.TotalWin, r.TotalBet);
                if (tier >= WinTier.Big) yield return BigWin(tier, r.TotalWin);
                Save();

                if (r.FreeSpinsAwarded > 0 && !r.FeatureTriggered)
                    yield return Banner($"+{r.FreeSpinsAwarded} SPINS!", "", GameSpec.RetriggerBannerSeconds);
                if (r.FeatureEnded)
                    yield return Banner("TOTAL WIN", r.FeatureTotalWin.ToString("N0"), GameSpec.FreeSpinTotalSeconds);
            }

            _inFeature = false;
            _ui.Background.color = GameSpec.Background;
            _ui.Paylines.HideAll();
            foreach (var reel in _ui.Reels) { reel.SetAllHighlights(false); reel.SetDimmed(false); }
            Refresh();
        }

        /// <summary>Reel animation → win presentation for one spin</summary>
        private IEnumerator PlaySpin(SpinResult r, long winFrom, long winTo)
        {
            _state = State.Spinning;
            _stopRequested = false;
            _skipRequested = false;
            _ui.Paylines.HideAll();
            _ui.Message.text = r.IsFreeSpin ? $"FREE SPIN  x{r.Multiplier}" : "GOOD LUCK!";
            _ui.WinText.text = winFrom.ToString("N0");
            Refresh();

            var timing = Timing;
            var reels = _ui.Reels;
            var stopped = 0;
            Action<ReelView> onStopped = rv =>
            {
                stopped++;
                rv.SetAnticipation(false);
                _audio.Play(Sfx.ReelStop);
                if (ScattersOnReel(r, rv.ReelIndex) > 0) _audio.Play(Sfx.ScatterLand);
                if (!reels.Any(x => x.IsSpinning && x.IsAnticipating)) _audio.StopLoop();
            };
            foreach (var reel in reels) reel.Stopped += onStopped;

            _audio.Play(Sfx.SpinStart);
            foreach (var reel in reels) reel.StartSpin(timing);

            for (var i = 0; i < reels.Length; i++)
            {
                var anticipate = !_stopRequested && i > 0 && ScattersBefore(r, i) >= _model.ScatterTriggerCount - 1;
                var delay = (i == 0 ? timing.MinSpinSeconds : timing.StopIntervalSeconds) + (anticipate ? timing.AnticipationSeconds : 0f);
                if (anticipate)
                {
                    reels[i].SetAnticipation(true);
                    _audio.Play(Sfx.Anticipation);
                }
                var t = 0f;
                while (t < delay && !_stopRequested)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                if (_stopRequested)
                {
                    for (var j = i; j < reels.Length; j++)
                    {
                        reels[j].SetAnticipation(false);
                        reels[j].RequestStop(r.Stops[j], true);
                    }
                    break;
                }
                reels[i].RequestStop(r.Stops[i], false);
            }
            while (stopped < reels.Length) yield return null;
            foreach (var reel in reels) reel.Stopped -= onStopped;
            _audio.StopLoop();

            // ---- Win presentation ----
            _state = State.Presenting;
            _skipRequested = false;
            Refresh();
            if (r.TotalWin > 0)
            {
                _audio.Play(Sfx.Win);
                HighlightAll(r);
                _ui.Message.text = r.IsFreeSpin ? $"WIN {r.TotalWin:N0}  (x{r.Multiplier})" : $"WIN {r.TotalWin:N0}";
                var tier = GameSpec.TierOf(r.TotalWin, r.TotalBet);
                var countSeconds = tier == WinTier.Normal ? GameSpec.NormalCountUpSeconds : 0.2f; // big wins count up in the overlay
                var hold = Mathf.Max(countSeconds, GameSpec.AllLinesSeconds) * (_turbo ? 0.5f : 1f);
                yield return CountUp(winFrom, winTo, _shownBalance, _game.Balance, countSeconds * (_turbo ? 0.5f : 1f), hold);
            }
            else
            {
                ClearHighlights();
                _shownBalance = _game.Balance;
                Refresh();
            }
        }

        private IEnumerator CountUp(long winFrom, long winTo, long balFrom, long balTo, float seconds, float hold)
        {
            var t = 0f;
            while (t < hold && !_skipRequested)
            {
                t += Time.deltaTime;
                var u = seconds <= 0 ? 1f : Mathf.Clamp01(t / seconds);
                _ui.WinText.text = ((long)Math.Round(winFrom + (winTo - winFrom) * (double)u)).ToString("N0");
                _shownBalance = (long)Math.Round(balFrom + (balTo - balFrom) * (double)u);
                _ui.CreditText.text = _shownBalance.ToString("N0");
                yield return null;
            }
            _skipRequested = false;
            _ui.WinText.text = winTo.ToString("N0");
            _shownBalance = balTo;
            Refresh();
        }

        private IEnumerator BigWin(WinTier tier, long amount)
        {
            string title;
            Color color;
            switch (tier)
            {
                case WinTier.Epic: title = "EPIC WIN"; color = GameSpec.Cyan; break;
                case WinTier.Mega: title = "MEGA WIN"; color = GameSpec.TitlePink; break;
                default: title = "BIG WIN"; color = GameSpec.Gold; break;
            }
            ShowOverlay(title, "0", color);
            var seconds = GameSpec.CountUpSeconds(tier) * (_turbo ? 0.5f : 1f);
            _skipRequested = false;
            var t = 0f;
            while (t < seconds && !_skipRequested)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / seconds);
                _ui.OverlayAmount.text = ((long)(amount * (1 - (1 - u) * (1 - u)))).ToString("N0");
                _ui.OverlayTitle.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 8f));
                yield return null;
            }
            _ui.OverlayAmount.text = amount.ToString("N0");
            _ui.OverlayTitle.transform.localScale = Vector3.one;
            yield return WaitOrSkip(0.8f, true);
            HideOverlay();
        }

        private IEnumerator Banner(string title, string amount, float seconds)
        {
            ShowOverlay(title, amount, GameSpec.Gold);
            yield return WaitOrSkip(seconds, true);
            HideOverlay();
        }

        private IEnumerator WaitOrSkip(float seconds, bool skippable)
        {
            _skipRequested = false;
            var t = 0f;
            while (t < seconds && !(skippable && _skipRequested))
            {
                t += Time.deltaTime;
                yield return null;
            }
            _skipRequested = false;
        }

        // ------------------------------------------------------------------ win highlight
        private IEnumerator CycleLines(SpinResult r)
        {
            var wait = GameSpec.EachLineSeconds;
            while (true)
            {
                foreach (var w in r.LineWins)
                {
                    ClearHighlights();
                    DimAll(true);
                    HighlightLine(w);
                    _ui.Message.text = $"LINE {w.LineIndex + 1}  {_model.Symbols[w.Symbol].Id} x{w.Count}  WIN {w.Win:N0}";
                    yield return new WaitForSeconds(wait);
                }
                if (r.ScatterWin > 0)
                {
                    ClearHighlights();
                    DimAll(true);
                    HighlightScatters(r);
                    _ui.Message.text = $"SCATTER x{r.ScatterCount}  WIN {r.ScatterWin:N0}";
                    yield return new WaitForSeconds(wait);
                }
                if (r.LineWins.Count + (r.ScatterWin > 0 ? 1 : 0) <= 1) yield break; // just one: keep showing it
            }
        }

        private void StopLineCycle()
        {
            if (_lineCycle != null) StopCoroutine(_lineCycle);
            _lineCycle = null;
            ClearHighlights();
        }

        private void HighlightAll(SpinResult r)
        {
            ClearHighlights();
            DimAll(true);
            foreach (var w in r.LineWins) HighlightLine(w);
            if (r.ScatterWin > 0) HighlightScatters(r);
        }

        private void HighlightLine(LineWin w)
        {
            _ui.Paylines.Show(w.LineIndex, true);
            var line = _model.Paylines[w.LineIndex];
            for (var k = 0; k < w.Count; k++) Highlight(k, line[k]);
        }

        private void HighlightScatters(SpinResult r)
        {
            foreach (var p in r.ScatterPositions) Highlight(p.Reel, p.Row);
        }

        private void Highlight(int reel, int row)
        {
            var cell = _ui.Reels[reel].CellAtRow(row);
            cell.SetDimmed(false);
            cell.SetHighlight(true);
        }

        private void DimAll(bool dim)
        {
            foreach (var reel in _ui.Reels) reel.SetDimmed(dim);
        }

        private void ClearHighlights()
        {
            _ui.Paylines.HideAll();
            foreach (var reel in _ui.Reels)
            {
                reel.SetAllHighlights(false);
                reel.SetDimmed(false);
            }
        }

        // ------------------------------------------------------------------ helpers
        private int ScattersOnReel(SpinResult r, int reel) => r.Window[reel].Count(s => s == _model.ScatterSymbol);

        private int ScattersBefore(SpinResult r, int reel)
        {
            var n = 0;
            for (var i = 0; i < reel; i++) n += ScattersOnReel(r, i);
            return n;
        }

        /// <summary>For debugging: stops that trigger FS (SCATTER in the top row of reels 1, 3, 5)</summary>
        private int[] FeatureStops()
        {
            var stops = new int[_model.ReelCount];
            for (var r = 0; r < _model.ReelCount; r++)
            {
                var strip = _model.Reels[r];
                if (r % 2 == 0 && Array.IndexOf(strip, _model.ScatterSymbol) >= 0)
                {
                    stops[r] = Array.IndexOf(strip, _model.ScatterSymbol);
                    continue;
                }
                // A stop with no SCATTER in view
                for (var s = 0; s < strip.Length; s++)
                {
                    var visible = false;
                    for (var k = 0; k < _model.RowCount; k++) visible |= strip[(s + k) % strip.Length] == _model.ScatterSymbol;
                    if (!visible) { stops[r] = s; break; }
                }
            }
            return stops;
        }

        private void ShowOverlay(string title, string amount, Color color)
        {
            _overlayActive = true;
            _ui.OverlayTitle.text = title;
            _ui.OverlayTitle.color = color;
            _ui.OverlayAmount.text = amount;
            _ui.Overlay.SetActive(true);
        }

        private void HideOverlay()
        {
            _overlayActive = false;
            _ui.Overlay.SetActive(false);
        }

        private void Save()
        {
            if (_game != null) SaveStore.Save(_game, _turbo);
        }

        private void Refresh()
        {
            if (_ui == null) return;
            var idle = _state == State.Idle;
            var fs = _game.IsInFreeSpins;

            _ui.CreditText.text = _shownBalance.ToString("N0");
            _ui.BetText.text = _game.TotalBet.ToString("N0");
            _ui.FreeSpinText.text = _inFeature
                ? $"FREE SPINS {_game.FreeSpinsPlayed}/{_game.FreeSpinsPlayed + _game.FreeSpinsRemaining}  x{_model.FreeSpins.Multiplier}"
                : "";

            _ui.SpinLabel.text = _state == State.Spinning ? "STOP" : "SPIN";
            _ui.SpinButton.interactable = (idle && !fs && _game.CanSpin) || _state == State.Spinning || _state == State.Presenting;

            var canRefill = idle && !fs && !_game.CanSpin;
            _ui.RefillButton.gameObject.SetActive(canRefill);

            var level = _game.BetLevel;
            _ui.BetMinusButton.interactable = idle && !fs && level > 0;
            _ui.BetPlusButton.interactable = idle && !fs && level < _model.Bet.LineBetLevels.Count - 1;
            _ui.PaytableButton.interactable = idle && !fs;
            _ui.AutoImage.color = _auto ? GameSpec.ButtonActive : GameSpec.Button;
            _ui.TurboImage.color = _turbo ? GameSpec.ButtonActive : GameSpec.Button;
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameSpec.Background;
        }
    }
}

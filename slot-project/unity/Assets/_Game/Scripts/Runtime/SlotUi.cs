using System;
using SlotSdk;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Builds the whole screen in code (game spec "Screen Layout" sheet) and holds the references.
    /// Holds no logic; SlotController drives it.
    /// </summary>
    public sealed class SlotUi
    {
        public Canvas Canvas;
        public Image Background;
        public Text FreeSpinText;
        public Text CreditText;
        public ReelView[] Reels;
        public PaylineView Paylines;
        public Text Message;
        public Button PaytableButton, BetMinusButton, BetPlusButton, AutoButton, TurboButton, SpinButton, RefillButton;
        public Text SpinLabel, BetText, WinText;
        public Image AutoImage, TurboImage;

        public GameObject Overlay;
        public Text OverlayTitle;
        public Text OverlayAmount;

        public GameObject PaytablePanel;
        public RectTransform PaytableContent;

        public interface IHandler
        {
            void OnSpinPressed();
            void OnBet(int delta);
            void OnToggleAuto();
            void OnToggleTurbo();
            void OnTogglePaytable();
            void OnRefill();
            void OnOverlayClicked();
        }

        public static SlotUi Build(Transform parent, MathModel model, IHandler h, Func<int, int> initialStop)
        {
            var ui = new SlotUi();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            ui.Canvas = canvasGo.GetComponent<Canvas>();
            ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GameSpec.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            ui.Background = Ui.FullImage(root, "Background", GameSpec.Background);

            // ---- Top bar (UI-01..03) ----
            Ui.Image(root, "TopBar", new Vector2(0, 485), new Vector2(1920, 110), GameSpec.Panel);
            Ui.Text(root, "Title", "NEON FORTUNE", 56, GameSpec.TitlePink, new Vector2(-640, 485), new Vector2(600, 100));
            ui.FreeSpinText = Ui.Text(root, "FreeSpins", "", 40, GameSpec.Gold, new Vector2(0, 485), new Vector2(700, 100));
            Ui.Text(root, "CreditLabel", "CREDIT", 24, GameSpec.Cyan, new Vector2(700, 505), new Vector2(400, 40));
            ui.CreditText = Ui.Text(root, "Credit", "0", 44, Color.white, new Vector2(700, 465), new Vector2(400, 60));

            // ---- Reel area (UI-04, 05) ----
            var cell = GameSpec.CellSize;
            var spacing = GameSpec.ReelSpacing;
            var areaW = model.ReelCount * cell + (model.ReelCount - 1) * spacing;
            var areaH = model.RowCount * cell;
            var area = Ui.Node("ReelArea", root, GameSpec.ReelAreaCenter, new Vector2(areaW, areaH));
            Ui.Image(area, "Frame", Vector2.zero, new Vector2(areaW + 100, areaH + 40), GameSpec.PanelDark, ProceduralSprites.RoundedRect);
            Ui.Outline(area, "FrameGlow", Vector2.zero, new Vector2(areaW + 100, areaH + 40), GameSpec.TitlePink * new Color(1, 1, 1, 0.8f));
            ui.Reels = new ReelView[model.ReelCount];
            for (var r = 0; r < model.ReelCount; r++)
            {
                var x = PaylineView.CenterX(r, model.ReelCount, cell, spacing);
                Ui.Image(area, $"ReelBg{r + 1}", new Vector2(x, 0), new Vector2(cell, areaH), new Color(0.09f, 0.08f, 0.2f, 1f), ProceduralSprites.RoundedRect);
                var reelNode = Ui.Node($"Reel{r + 1}", area, new Vector2(x, 0), new Vector2(cell, areaH));
                var view = reelNode.gameObject.AddComponent<ReelView>();
                view.Initialize(model, r, initialStop(r), cell);
                ui.Reels[r] = view;
            }
            ui.Paylines = new PaylineView(area, model, cell, spacing);
            ui.Paylines.HideAll();

            // ---- Message bar (UI-06) ----
            Ui.Image(root, "MessageBar", new Vector2(0, -318), new Vector2(areaW + 100, 56), GameSpec.PanelDark, ProceduralSprites.RoundedRect);
            ui.Message = Ui.Text(root, "Message", "GOOD LUCK!", 30, Color.white, new Vector2(0, -318), new Vector2(areaW + 80, 56));

            // ---- Bottom bar (UI-07..13, 17) ----
            const float y = -440f;
            Ui.Image(root, "BottomBar", new Vector2(0, y), new Vector2(1920, 180), GameSpec.Panel);
            ui.PaytableButton = Ui.Button(root, "Paytable", "PAYTABLE", new Vector2(-780, y), new Vector2(220, 100), GameSpec.Button, 28, h.OnTogglePaytable, out _);
            ui.BetMinusButton = Ui.Button(root, "BetMinus", "-", new Vector2(-580, y), new Vector2(100, 100), GameSpec.Button, 56, () => h.OnBet(-1), out _);
            Ui.Image(root, "BetBox", new Vector2(-430, y), new Vector2(180, 100), GameSpec.PanelDark, ProceduralSprites.RoundedRect);
            Ui.Text(root, "BetLabel", "BET", 22, GameSpec.BetYellow, new Vector2(-430, y + 26), new Vector2(180, 30));
            ui.BetText = Ui.Text(root, "Bet", "0", 40, Color.white, new Vector2(-430, y - 14), new Vector2(180, 50));
            ui.BetPlusButton = Ui.Button(root, "BetPlus", "+", new Vector2(-280, y), new Vector2(100, 100), GameSpec.Button, 56, () => h.OnBet(+1), out _);

            Ui.Image(root, "WinBox", new Vector2(-10, y), new Vector2(340, 110), GameSpec.PanelDark, ProceduralSprites.RoundedRect);
            Ui.Text(root, "WinLabel", "WIN", 22, GameSpec.WinGreen, new Vector2(-10, y + 30), new Vector2(340, 30));
            ui.WinText = Ui.Text(root, "Win", "0", 48, GameSpec.WinGreen, new Vector2(-10, y - 12), new Vector2(340, 60));
            ui.RefillButton = Ui.Button(root, "Refill", "REFILL", new Vector2(-10, y), new Vector2(340, 110), GameSpec.ButtonActive, 36, h.OnRefill, out _);

            ui.AutoButton = Ui.Button(root, "Auto", "AUTO", new Vector2(270, y), new Vector2(170, 100), GameSpec.Button, 30, h.OnToggleAuto, out _);
            ui.AutoImage = ui.AutoButton.GetComponent<Image>();
            ui.TurboButton = Ui.Button(root, "Turbo", "TURBO", new Vector2(460, y), new Vector2(170, 100), GameSpec.Button, 30, h.OnToggleTurbo, out _);
            ui.TurboImage = ui.TurboButton.GetComponent<Image>();
            ui.SpinButton = Ui.Button(root, "Spin", "SPIN", new Vector2(730, y), new Vector2(300, 140), GameSpec.Spin, 52, h.OnSpinPressed, out ui.SpinLabel);

            // ---- Overlays (UI-14..16) ----
            BuildPaytable(ui, root, h);
            BuildOverlay(ui, root, h);
            return ui;
        }

        private static void BuildOverlay(SlotUi ui, Transform root, IHandler h)
        {
            var bg = Ui.FullImage(root, "Overlay", new Color(0, 0, 0, 0.78f));
            bg.raycastTarget = true;
            bg.gameObject.AddComponent<Button>().onClick.AddListener(h.OnOverlayClicked);
            ui.Overlay = bg.gameObject;
            ui.OverlayTitle = Ui.Text(bg.transform, "Title", "", 110, GameSpec.Gold, new Vector2(0, 80), new Vector2(1800, 300));
            ui.OverlayAmount = Ui.Text(bg.transform, "Amount", "", 96, Color.white, new Vector2(0, -150), new Vector2(1800, 140));
            Ui.Text(bg.transform, "Hint", "CLICK / SPACE TO SKIP", 22, new Color(1, 1, 1, 0.5f), new Vector2(0, -420), new Vector2(800, 40));
            ui.Overlay.SetActive(false);
        }

        private static void BuildPaytable(SlotUi ui, Transform root, IHandler h)
        {
            var bg = Ui.FullImage(root, "PaytableOverlay", new Color(0, 0, 0, 0.8f));
            bg.raycastTarget = true;
            ui.PaytablePanel = bg.gameObject;
            Ui.Image(bg.transform, "Panel", Vector2.zero, new Vector2(1500, 900), GameSpec.Panel, ProceduralSprites.RoundedRect);
            Ui.Text(bg.transform, "Title", "PAYTABLE", 56, GameSpec.TitlePink, new Vector2(0, 390), new Vector2(800, 80));
            ui.PaytableContent = Ui.Node("Content", bg.transform, new Vector2(0, 20), new Vector2(1400, 660));
            Ui.Button(bg.transform, "Close", "CLOSE", new Vector2(0, -385), new Vector2(240, 80), GameSpec.Button, 30, h.OnTogglePaytable, out _);
            ui.PaytablePanel.SetActive(false);
        }

        /// <summary>Rebuilds the paytable contents for the current bet (converted to credits)</summary>
        public void FillPaytable(MathModel m, long lineBet, long totalBet)
        {
            for (var i = PaytableContent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(PaytableContent.GetChild(i).gameObject);

            var normal = new System.Collections.Generic.List<SymbolDef>();
            foreach (var s in m.Symbols)
                if (s.Type == SymbolType.Normal) normal.Add(s);

            // Normal symbols in 2 columns x 4 rows
            for (var i = 0; i < normal.Count; i++)
            {
                var s = normal[i];
                var col = i / 4;
                var row = i % 4;
                var pos = new Vector2(-350 + col * 700, 250 - row * 110);
                var label = GameSpec.SymbolLabel(s.Id) ?? s.Id;
                Ui.Text(PaytableContent, "Sym", label, label.Length <= 1 ? 72 : 34, GameSpec.SymbolColor(s.Id), pos + new Vector2(-220, 0), new Vector2(200, 100));
                var sb = new System.Text.StringBuilder();
                for (var k = m.ReelCount; k >= 1; k--)
                    if (m.Paytable[s.Index][k] > 0) sb.Append($"{k}x  {m.Paytable[s.Index][k] * lineBet:N0}\n");
                Ui.Text(PaytableContent, "Pays", sb.ToString().TrimEnd(), 26, Color.white, pos + new Vector2(60, 0), new Vector2(360, 110),
                    TextAnchor.MiddleLeft, FontStyle.Normal);
            }

            var sc = new System.Text.StringBuilder("SCATTER (anywhere):  ");
            for (var k = m.ReelCount; k >= 1; k--)
                if (m.ScatterPays[k] > 0) sc.Append($"{k}x {m.ScatterPays[k] * totalBet:N0}   ");
            var rules =
                sc + "\n" +
                $"{m.ScatterTriggerCount}+ SCATTER = {m.FreeSpins.AwardSpins} FREE SPINS, ALL WINS x{m.FreeSpins.Multiplier} (retrigger +{m.FreeSpins.RetriggerSpins})\n" +
                "WILD substitutes for all symbols except SCATTER (reels 2-4)\n" +
                $"{m.Bet.Lines} lines, left to right. Highest win per line. Play credits only.";
            Ui.Text(PaytableContent, "Rules", rules, 26, GameSpec.Gold, new Vector2(0, -250), new Vector2(1400, 170), TextAnchor.MiddleCenter, FontStyle.Normal);
        }
    }
}

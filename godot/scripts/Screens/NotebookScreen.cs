using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core.Systems;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 여울의 수첩 (웹 NotebookScene). 쿨러, 어종 도감, 미각 회복, 조황일지 네 탭.
    /// 줄 쳐진 종이 위에 쓰고, 제목 줄 옆에는 고등어(SD)가 웅크리고 있다.
    /// </summary>
    public partial class NotebookScreen : PlaceScreen
    {
        private const int CoolerPage = 8;
        private int _coolerPage;
        private TextureRect _cat;

        private Dictionary<string, string> T
        {
            get { return G.Data.Texts.Notebook; }
        }

        private string F(string key, params object[] args)
        {
            return string.Format(T[key], args);
        }

        protected override string Backdrop
        {
            get { return null; }
        }

        protected override string TitleText
        {
            get { return "여울의 수첩"; }
        }

        protected override IList<string> Tabs
        {
            get { return new[] { T["tabCooler"], T["tabDex"], T["tabTaste"], T["tabJournal"] }; }
        }

        protected override void Decorate(ArtStage stage)
        {
            // 나무 책상 빛깔 바탕
            var desk = UiKit.VerticalShade(new Color("3a2c22"), new Color("221912"));
            desk.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            stage.AddChild(desk);
        }

        protected override void BuildHeader()
        {
            _cat = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://art/cat/loaf.png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(150, 110),
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            _cat.Resized += () => _cat.PivotOffset = new Vector2(_cat.Size.X / 2, _cat.Size.Y);
            _cat.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) TapCat();
            };
            HeaderRight.AddChild(_cat);
            var tw = _cat.CreateTween().SetLoops();
            tw.TweenProperty(_cat, "scale", new Vector2(1, 1.03f), 1.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tw.TweenProperty(_cat, "scale", Vector2.One, 1.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }

        private void TapCat()
        {
            var idle = G.Data.Dialogue.CatIdle;
            var r = _cat.GetGlobalRect();
            Bubble(Toasts, idle[(int)(GameState.I.Rng.Next() * idle.Count)], new Vector2(r.Position.X + r.Size.X / 2, r.Position.Y + 6), 1.3f);
        }

        protected override Control BuildPage()
        {
            var paper = new NotePaper();
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 16);
            paper.AddChild(col);
            switch (Tab)
            {
                case 0: Cooler(col); break;
                case 1: Dex(col); break;
                case 2: Taste(col); break;
                default: Journal(col); break;
            }
            return paper;
        }

        private static Control Heading(string title, string sub)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddChild(UiKit.Text(title, TextSize.H1, Palette.Ink, true));
            row.AddChild(Spacer());
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UiKit.Text(sub, TextSize.Tiny + 2, Palette.InkSoft);
                s.SizeFlagsVertical = SizeFlags.ShrinkEnd;
                row.AddChild(s);
            }
            return row;
        }

        // ───────── 쿨러 ─────────

        private void Cooler(VBoxContainer col)
        {
            var cooler = G.Save.Cooler;
            col.AddChild(Heading(F("cooler", cooler.Count, G.CoolerCapacity()), T["coolerSub"]));
            if (cooler.Count == 0)
            {
                var e = UiKit.Text(T["coolerEmpty"], TextSize.Body, Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Center);
                e.SizeFlagsVertical = SizeFlags.ExpandFill;
                e.VerticalAlignment = VerticalAlignment.Center;
                col.AddChild(e);
                return;
            }
            var list = cooler.OrderByDescending(f => f.Fresh).ToList();
            int pages = (list.Count + CoolerPage - 1) / CoolerPage;
            _coolerPage = Mathf.Clamp(_coolerPage, 0, pages - 1);
            foreach (var f in list.Skip(_coolerPage * CoolerPage).Take(CoolerPage))
            {
                var stage = G.Fresh.StageOf(f.Fresh);
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 18);
                var icon = FishIcon(f.Species, 120);
                if (stage == FreshStage.Spoiled) icon.Modulate = new Color(0.7f, 0.62f, 0.55f);
                row.AddChild(icon);
                var name = UiKit.Text(G.Data.SpeciesById(f.Species).Name + " " + f.Size.ToString("0.0", CultureInfo.InvariantCulture) + "cm", TextSize.Small, Palette.Ink, true);
                name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                name.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(name);
                row.AddChild(FreshBar(f.Fresh, stage));
                var pill = MarketScreen.StagePill(stage);
                pill.CustomMinimumSize = new Vector2(100, 0);
                row.AddChild(pill);
                var value = UiKit.Text(UiKit.Won(G.FishValue(f)), TextSize.Small, Palette.Ink, false, false, 0, null, HorizontalAlignment.Right);
                value.CustomMinimumSize = new Vector2(150, 0);
                value.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(value);
                col.AddChild(row);
            }
            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
            if (pages > 1)
            {
                col.AddChild(Pager(_coolerPage, pages, p =>
                {
                    _coolerPage = p;
                    Redraw();
                }));
            }
        }

        /// <summary>신선도 막대. 남은 만큼 단계 색으로 찬다</summary>
        private static Control FreshBar(int fresh, FreshStage stage)
        {
            var track = new Panel { CustomMinimumSize = new Vector2(130, 26), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            track.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Ink, 13));
            var fill = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            fill.AddThemeStyleboxOverride("panel", UiKit.Box(MarketScreen.StageColor(stage), 9));
            track.AddChild(fill);
            track.Resized += () =>
            {
                float w = (track.Size.X - 8) * Mathf.Clamp(fresh / 100f, 0, 1);
                fill.Position = new Vector2(4, 4);
                fill.Size = new Vector2(Mathf.Max(fresh > 0 ? 18 : 0, w), track.Size.Y - 8);
            };
            return track;
        }

        // ───────── 도감 ─────────

        private void Dex(VBoxContainer col)
        {
            int caught = G.Taste.SpeciesCaught(G.Save);
            col.AddChild(Heading(F("dex", caught, G.Data.Species.Count), T["dexSub"]));
            foreach (var sp in G.Data.Species)
            {
                G.Save.Dex.TryGetValue(sp.Id, out var e);
                bool known = e != null && e.Count > 0;
                var card = new Button { Flat = true, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = known ? CursorShape.PointingHand : CursorShape.Arrow, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 170) };
                var face = UiKit.Box(new Color(Palette.PaperLight, 0.6f), 26);
                card.AddThemeStyleboxOverride("normal", face);
                card.AddThemeStyleboxOverride("hover", face);
                card.AddThemeStyleboxOverride("pressed", UiKit.Box(new Color(Palette.PaperDark, 0.6f), 26));
                card.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 22);
                row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 14);
                card.AddChild(row);
                row.AddChild(FishIcon(sp.Id, 200, !known));
                var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 4);
                row.AddChild(text);
                if (!known)
                {
                    text.AddChild(UiKit.Text(T["unknown"], TextSize.H1, Palette.InkSoft, true));
                    text.AddChild(UiKit.Text(sp.Rare ? T["unknownRare"] : T["unknownSub"], TextSize.Small, Palette.InkSoft));
                }
                else
                {
                    var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                    head.AddChild(UiKit.Text(sp.Name + (sp.Rare ? " ★" : ""), TextSize.H1, Palette.Ink, true));
                    head.AddChild(Spacer());
                    head.AddChild(UiKit.Text(F("count", e.Count), TextSize.Small, Palette.InkSoft));
                    text.AddChild(head);
                    text.AddChild(UiKit.Text(sp.Hint, TextSize.Small, Palette.InkSoft));
                    var recipe = G.RecipeForSpecies(sp.Id);
                    bool cooked = recipe != null && G.Save.RecipesCooked.Contains(recipe.Id);
                    string best = F("best", e.Best.ToString("0.0", CultureInfo.InvariantCulture));
                    text.AddChild(UiKit.Text(cooked ? best + " · " + F("recipe", recipe.Name) : best, TextSize.Small, cooked ? Palette.RedDark : Palette.InkSoft));
                    var id = sp.Id;
                    card.Pressed += () => DexDetail(id);
                }
                col.AddChild(card);
            }
        }

        private void DexDetail(string id)
        {
            var sp = G.Data.SpeciesById(id);
            var lines = new List<string> { sp.Desc, "" };
            lines.Add(F("dexSize", sp.MinSize, sp.MaxSize) + " · " + T["dexPower"] + " " + new string('●', sp.Strength) + new string('○', Math.Max(0, 5 - sp.Strength)));
            if (sp.Aging) lines.Add(T["dexAging"]);
            var pic = FishIcon(id, 520);
            pic.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            var holder = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            holder.AddChild(pic);
            pic.Resized += () => pic.PivotOffset = pic.Size / 2;
            var tw = pic.CreateTween().SetLoops();
            tw.TweenProperty(pic, "rotation_degrees", 3f, 0.9).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tw.TweenProperty(pic, "rotation_degrees", -3f, 0.9).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            Modal(sp.Name + (sp.Rare ? " ★" : ""), string.Join("\n", lines), new[] { new ModalButton(T["close"], Palette.GrayDark) }, holder);
        }

        // ───────── 미각 ─────────

        private void Taste(VBoxContainer col)
        {
            int lv = G.TasteLevel();
            int caught = G.Taste.SpeciesCaught(G.Save);
            col.AddChild(Heading(T["taste"], F("tasteSub", caught)));
            var stages = G.Data.TasteStages;
            var list = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            list.AddThemeConstantOverride("separation", 0);
            col.AddChild(list);
            for (int i = 0; i < stages.Count; i++)
            {
                var st = stages[i];
                bool done = i <= lv;
                bool known = done || i == lv + 1;
                var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 150), MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 26);
                row.AddChild(new TasteNode(i, done, i == lv + 1, i < stages.Count - 1, i < lv));
                var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 2);
                var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                head.AddChild(UiKit.Text(known ? st.Name : T["unknown"], TextSize.H1 - 4, done ? Palette.Ink : Palette.InkSoft, true));
                head.AddChild(Spacer());
                string cond = st.NeedSpecies.HasValue && !done ? F("tasteProgress", st.Cond, Math.Min(caught, st.NeedSpecies.Value), st.NeedSpecies.Value) : st.Cond;
                var c = UiKit.Text(cond, TextSize.Tiny + 2, done ? Palette.GreenDark : st.NeedSpecies.HasValue ? Palette.RedDark : Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Right);
                c.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                head.AddChild(c);
                text.AddChild(head);
                text.AddChild(UiKit.Text(known ? st.Effect : T["unknown"], TextSize.Small, Palette.InkSoft).Ellipsis());
                row.AddChild(text);
                list.AddChild(row);
            }
        }

        // ───────── 조황일지 ─────────

        private void Journal(VBoxContainer col)
        {
            int have = G.Save.JournalPages;
            col.AddChild(Heading(F("journal", have), null));
            var grid = new GridContainer { Columns = 4, MouseFilter = MouseFilterEnum.Ignore };
            grid.AddThemeConstantOverride("h_separation", 22);
            grid.AddThemeConstantOverride("v_separation", 22);
            col.AddChild(grid);
            for (int i = 0; i < 12; i++)
            {
                bool got = i < have;
                var page = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 170), MouseFilter = MouseFilterEnum.Ignore };
                var sb = got
                    ? UiKit.Box(new Color("f8f1de"), 14).WithBorder(Palette.Ink, 4, 8).WithShadow(0.2f, 6, 3)
                    : UiKit.Box(new Color(Palette.PaperDark, 0.7f), 14).WithBorder(new Color(Palette.InkSoft, 0.45f), 3);
                page.AddThemeStyleboxOverride("panel", sb);
                var l = UiKit.Text(got ? F("journalPage", i + 1) : "?", TextSize.H1, got ? Palette.Ink : new Color(Palette.InkSoft, 0.7f), true, false, 0, null, HorizontalAlignment.Center);
                l.VerticalAlignment = VerticalAlignment.Center;
                page.AddChild(l);
                // 얻은 장은 살짝 기울어 붙인 쪽지처럼
                if (got)
                {
                    page.Resized += () => page.PivotOffset = page.Size / 2;
                    page.RotationDegrees = i % 2 == 0 ? -3 : 2;
                }
                grid.AddChild(page);
            }
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12), MouseFilter = MouseFilterEnum.Ignore });
            col.AddChild(Rule());
            if (have > 0)
            {
                col.AddChild(UiKit.Text(T["journal1Title"], TextSize.Body, Palette.Wood, true));
                col.AddChild(UiKit.Text(T["journal1"], TextSize.Small + 2, Palette.InkSoft, false, true));
            }
            else
            {
                col.AddChild(UiKit.Text(T["journalNone"], TextSize.Small + 2, Palette.InkSoft, false, true));
            }
        }

        // ───────── CI 사진 ─────────

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "dex":
                    ShowTab(1);
                    break;
                case "detail":
                    ShowTab(1);
                    DexDetail("bollak");
                    break;
                case "taste":
                    ShowTab(2);
                    break;
                case "journal":
                    G.Save.JournalPages = 1;
                    ShowTab(3);
                    break;
            }
        }

        /// <summary>미각 단계 동그라미와 다음 단계로 이어지는 선</summary>
        private partial class TasteNode : Control
        {
            private readonly int _n;
            private readonly bool _done, _next, _line, _lineDone;

            public TasteNode() { }

            public TasteNode(int n, bool done, bool next, bool line, bool lineDone)
            {
                _n = n;
                _done = done;
                _next = next;
                _line = line;
                _lineDone = lineDone;
                CustomMinimumSize = new Vector2(100, 0);
                MouseFilter = MouseFilterEnum.Ignore;
            }

            public override void _Draw()
            {
                var c = new Vector2(Size.X / 2, 44);
                if (_line) DrawLine(c, new Vector2(c.X, Size.Y + 44), _lineDone ? Palette.Gold : Palette.PaperDark, 10);
                DrawCircle(c, 40, Palette.Ink);
                DrawCircle(c, 33, _done ? Palette.Gold : _next ? Palette.PaperDark : Palette.GrayDark);
                var font = Fonts.Title;
                string s = _n.ToString();
                var size = font.GetStringSize(s, HorizontalAlignment.Left, -1, TextSize.H1 - 6);
                DrawString(font, new Vector2(c.X - size.X / 2, c.Y + size.Y * 0.32f), s, HorizontalAlignment.Left, -1, TextSize.H1 - 6, _done || _next ? Palette.Ink : Palette.PaperDark);
            }
        }

        /// <summary>줄 쳐진 수첩 종이. 왼쪽에 빨간 여백 선</summary>
        private partial class NotePaper : PanelContainer
        {
            public NotePaper()
            {
                MouseFilter = MouseFilterEnum.Ignore;
                AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 30, 44, 34).WithBorder(Palette.PaperDark, 0, 10).WithShadow(0.45f, 26, 12));
            }

            public override void _Draw()
            {
                var line = new Color(Palette.PaperDark, 0.55f);
                for (float y = 120; y < Size.Y - 30; y += 64) DrawLine(new Vector2(24, y), new Vector2(Size.X - 24, y), line, 2);
                DrawLine(new Vector2(26, 20), new Vector2(26, Size.Y - 26), new Color(Palette.Red, 0.35f), 3);
            }
        }
    }
}

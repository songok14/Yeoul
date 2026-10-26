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
        protected override string Music
        {
            get { return "town"; }
        }

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
            get { return new[] { T["tabCooler"], T["tabDex"], T["tabTaste"], T["tabJournal"], T["tabSettings"] }; }
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
                case 3: Journal(col); break;
                default: SettingsPage(col); break;
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
            // 위판장 쿨러처럼 쪽 넘김 대신 스크롤. 수첩은 한 마리씩 크기와 신선도 막대까지 보여 준다
            var list = cooler.OrderByDescending(f => f.Fresh).ToList();
            var scroll = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
                ScrollDeadzone = 24,
                MouseFilter = MouseFilterEnum.Pass,
            };
            col.AddChild(scroll);
            var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
            rows.AddThemeConstantOverride("separation", 10);
            scroll.AddChild(rows);
            foreach (var f in list)
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
                rows.AddChild(row);
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

        // 도감은 3열 × 4줄 카드, 쪽 넘김 (71종)
        private const int DexCols = 3;
        private const int DexRows = 4;
        private int _dexPage;

        private void Dex(VBoxContainer col)
        {
            int caught = G.Taste.SpeciesCaught(G.Save);
            var all = G.Data.Species;
            col.AddChild(Heading(F("dex", caught, all.Count), T["dexSub"]));
            int per = DexCols * DexRows;
            int pages = (all.Count + per - 1) / per;
            _dexPage = Mathf.Clamp(_dexPage, 0, pages - 1);
            var grid = new GridContainer { Columns = DexCols, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ExpandFill };
            grid.AddThemeConstantOverride("h_separation", 16);
            grid.AddThemeConstantOverride("v_separation", 16);
            col.AddChild(grid);
            foreach (var sp in all.Skip(_dexPage * per).Take(per))
            {
                G.Save.Dex.TryGetValue(sp.Id, out var e);
                bool known = e != null && e.Count > 0;
                var card = new Button
                {
                    Flat = true,
                    FocusMode = FocusModeEnum.None,
                    MouseDefaultCursorShape = known ? CursorShape.PointingHand : CursorShape.Arrow,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    CustomMinimumSize = new Vector2(0, 210),
                };
                var face = UiKit.Box(new Color(Palette.PaperLight, known ? 0.75f : 0.4f), 24);
                card.AddThemeStyleboxOverride("normal", face);
                card.AddThemeStyleboxOverride("hover", face);
                card.AddThemeStyleboxOverride("pressed", UiKit.Box(new Color(Palette.PaperDark, 0.6f), 24));
                card.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
                var inner = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
                inner.AddThemeConstantOverride("separation", 2);
                inner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 10);
                card.AddChild(inner);
                var icon = FishIcon(sp.Id, 120, !known);
                icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                inner.AddChild(icon);
                string stars = new string('★', sp.Stars);
                inner.AddChild(UiKit.Text(known ? sp.Name : T["unknown"], TextSize.Small + 2, known ? Palette.Ink : Palette.InkSoft, true, false, 0, null, HorizontalAlignment.Center).Ellipsis());
                inner.AddChild(UiKit.Text(known ? stars + "  " + F("count", e.Count) : stars, TextSize.Tiny + 2, sp.Stars == 3 ? Palette.GoldDark : Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Center));
                if (known)
                {
                    var id = sp.Id;
                    card.Pressed += () => DexDetail(id);
                }
                grid.AddChild(card);
            }
            if (pages > 1)
            {
                col.AddChild(Pager(_dexPage, pages, p =>
                {
                    _dexPage = p;
                    Redraw();
                }));
            }
        }

        private void DexDetail(string id)
        {
            var sp = G.Data.SpeciesById(id);
            G.Save.Dex.TryGetValue(id, out var e);
            var lines = new List<string> { sp.Desc, "" };
            lines.Add(sp.Hint);
            lines.Add(F("dexSize", sp.MinSize, sp.MaxSize) + " · " + T["dexPower"] + " " + new string('●', sp.Strength) + new string('○', Math.Max(0, 5 - sp.Strength)));
            if (e != null && e.Count > 0) lines.Add(F("count", e.Count) + " · " + F("best", e.Best.ToString("0.0", CultureInfo.InvariantCulture)));
            var recipe = G.RecipeForSpecies(id);
            if (recipe != null && G.Save.RecipesCooked.Contains(recipe.Id)) lines.Add(F("recipe", recipe.Name));
            if (sp.Aging) lines.Add(T["dexAging"]);
            var pic = FishIcon(id, 520);
            pic.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            var holder = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            holder.AddChild(pic);
            pic.Resized += () => pic.PivotOffset = pic.Size / 2;
            var tw = pic.CreateTween().SetLoops();
            tw.TweenProperty(pic, "rotation_degrees", 3f, 0.9).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tw.TweenProperty(pic, "rotation_degrees", -3f, 0.9).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            Modal(sp.Name + " " + new string('★', sp.Stars), string.Join("\n", lines), new[] { new ModalButton(T["close"], Palette.GrayDark) }, holder);
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

        // ───────── 설정 ─────────

        private void SettingsPage(VBoxContainer col)
        {
            col.AddChild(Heading(T["settings"], T["settingsSub"]));
            col.AddChild(Rule());
            col.AddChild(Toggle(T["sfx"], T["sfxSub"], Settings.Sfx, on => Settings.Sfx = on));
            col.AddChild(Rule());
            col.AddChild(Toggle(T["music"], T["musicSub"], Settings.Music, on =>
            {
                Settings.Music = on;
                Sound.RefreshMusic();
            }));
            col.AddChild(Rule());
            col.AddChild(Toggle(T["vibrate"], T["vibrateSub"], Settings.Vibrate, on =>
            {
                Settings.Vibrate = on;
                if (on) Haptics.Pulse(60, 0.6f);
            }));
            if (Settings.Vibrate)
            {
                // 진동 세기: 약 · 보통 · 강 (누르면 그 세기로 한 번 떨린다)
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 16);
                var label = UiKit.Text(T["vibeLevel"], TextSize.Small + 2, Palette.InkSoft, false, false);
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                label.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(label);
                var names = T["vibeLevels"].Split(',');
                for (int i = 0; i < names.Length; i++)
                {
                    int lv = i;
                    var b = SmallButton(names[i], Settings.VibeLevel == lv ? Palette.Green : Palette.GrayDark, () =>
                    {
                        Settings.VibeLevel = lv;
                        Haptics.Pattern((50, 0.6f, 60), (90, 0.8f, 0));
                        Redraw();
                    }, 130, 84);
                    row.AddChild(b);
                }
                col.AddChild(row);
            }
        }

        /// <summary>이름, 설명, 오른쪽에 켜짐/꺼짐 단추</summary>
        private Control Toggle(string title, string sub, bool on, Action<bool> set)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 20);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 4);
            text.AddChild(UiKit.Text(title, TextSize.Body, Palette.Ink, true));
            text.AddChild(UiKit.Text(sub, TextSize.Small, Palette.InkSoft, false, true));
            row.AddChild(text);
            var b = SmallButton(on ? T["on"] : T["off"], on ? Palette.Green : Palette.GrayDark, () =>
            {
                set(!on);
                Redraw();
            }, 200, 96);
            b.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(b);
            return row;
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
                case "settings":
                    ShowTab(4);
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

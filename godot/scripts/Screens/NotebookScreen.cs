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
    /// 여울의 수첩 (웹 NotebookScene). 보관(쿨러·활어 수조), 내 장비, 의뢰, 어종 도감, 미각 회복, 조황일지.
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
            // 설정은 탭이 아니라 제목 줄 단추 (탭 여섯, 두 글자씩이라 넉넉하다)
            get { return new[] { T["tabCooler"], T["tabGear"], T["tabQuest"], T["tabDex"], T["tabTaste"], T["tabJournal"] }; }
        }

        /// <summary>설정 쪽 번호 (탭 줄에는 없다)</summary>
        private const int SettingsTab = 6;

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
            var settings = new GameButton(T["tabSettings"], Palette.GrayDark, () => ShowTab(SettingsTab), null, TextSize.Small, 84)
            {
                CustomMinimumSize = new Vector2(150, 84),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            HeaderRight.AddChild(settings);
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
                case 0: Storage(col); break;
                case 1: GearPage(col); break;
                case 2: Quests(col); break;
                case 3: Dex(col); break;
                case 4: Taste(col); break;
                case 5: Journal(col); break;
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

        // ───────── 보관 (쿨러와 활어 수조) ─────────

        /// <summary>보관 탭에서 보는 곳 (0 쿨러, 1 수조)</summary>
        private int _store;

        private void Storage(VBoxContainer col)
        {
            if (!G.HasTank()) _store = 0;
            if (G.HasTank())
            {
                // 쿨러 | 수조 고르는 두 알약
                var pick = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                pick.AddThemeConstantOverride("separation", 16);
                pick.AddChild(StoreButton(0, F("cooler", G.Save.Cooler.Count, G.CoolerCapacity())));
                pick.AddChild(StoreButton(1, F("tank", G.TankFish().Count, G.TankCapacity())));
                col.AddChild(pick);
            }
            if (_store == 1) Tank(col);
            else Cooler(col);
        }

        private Control StoreButton(int which, string label)
        {
            bool on = _store == which;
            var b = new GameButton(label, on ? Palette.Teal : Palette.GrayDark, () =>
            {
                if (_store == which) return;
                _store = which;
                Sound.Play("page", -3);
                Redraw();
            }, null, TextSize.Small, 84) { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            if (!on) b.Modulate = new Color(1, 1, 1, 0.75f);
            return b;
        }

        private void Cooler(VBoxContainer col)
        {
            var cooler = G.Save.Cooler;
            col.AddChild(Heading(F("cooler", cooler.Count, G.CoolerCapacity()), G.HasTank() ? T["coolerSubTank"] : T["coolerSub"]));
            if (cooler.Count == 0)
            {
                col.AddChild(EmptyNote(T["coolerEmpty"]));
                return;
            }
            // 위판장 쿨러처럼 쪽 넘김 대신 스크롤. 수첩은 한 마리씩 크기와 신선도 막대까지 보여 준다
            var rows = FishList(col);
            foreach (var f in cooler.OrderByDescending(f => f.Fresh).ToList())
            {
                Control right = null;
                if (G.HasTank())
                {
                    var fish = f;
                    var b = SmallButton(T["toTank"], Palette.Teal, () =>
                    {
                        if (!G.MoveToTank(fish.Uid)) return;
                        Sound.Play("plop", -4);
                        Redraw();
                    }, 160, 84);
                    b.SetEnabled(G.CanMoveToTank(f));
                    right = b;
                }
                rows.AddChild(FishLine(f, right));
            }
        }

        /// <summary>활어 수조: 지금 수조와 넓히기, 든 고기 (꺼내면 쿨러로)</summary>
        private void Tank(VBoxContainer col)
        {
            var tank = G.TankNow();
            var fish = G.TankFish();
            col.AddChild(Heading(F("tank", fish.Count, G.TankCapacity()), T["tankSub"]));
            // 수조 단계와 넓히기
            var box = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeStyleboxOverride("panel", UiKit.Box(new Color("8cd3e0", 0.3f), 24, 24, 14).WithBorder(new Color("3f7a8a", 0.6f), 3));
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 18);
            box.AddChild(row);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 2);
            text.AddChild(UiKit.Text(F("tankLevel", G.Save.Tank.Level + 1, tank.Name), TextSize.Small + 2, Palette.Ink, true).Ellipsis());
            text.AddChild(UiKit.Text(tank.Desc, TextSize.Tiny + 2, Palette.InkSoft, false, true));
            var next = G.TankNext();
            if (next != null) text.AddChild(UiKit.Text(F("tankNext", next.Name, next.Capacity), TextSize.Tiny + 2, Palette.Teal, true, true));
            row.AddChild(text);
            if (next != null)
            {
                var up = SmallButton(UiKit.Won(next.Price), Palette.Blue, () =>
                {
                    if (!G.UpgradeTank()) return;
                    Sound.Play("coin", -3);
                    HudBar.Refresh();
                    Toast(F("tankUp", G.TankNow().Name, G.TankCapacity()), Palette.Gold);
                    Redraw();
                }, 210, 96);
                up.SetEnabled(G.Save.Money >= next.Price);
                row.AddChild(up);
            }
            else row.AddChild(Pill(T["tankBest"], Palette.GreenDark, Colors.White));
            col.AddChild(box);
            if (fish.Count == 0)
            {
                col.AddChild(EmptyNote(T["tankEmpty"]));
                return;
            }
            var rows = FishList(col);
            foreach (var f in fish.OrderByDescending(f => G.FishValue(f)).ToList())
            {
                var item = f;
                var b = SmallButton(T["toCooler"], Palette.GrayDark, () =>
                {
                    if (!G.MoveToCooler(item.Uid)) return;
                    Sound.Play("tick", -4);
                    Redraw();
                }, 160, 84);
                b.SetEnabled(!G.CoolerFull());
                rows.AddChild(FishLine(f, b));
            }
        }

        private static Control EmptyNote(string text)
        {
            var e = UiKit.Text(text, TextSize.Body, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center);
            e.SizeFlagsVertical = SizeFlags.ExpandFill;
            e.VerticalAlignment = VerticalAlignment.Center;
            return e;
        }

        /// <summary>손가락으로 밀어 내리는 고기 목록</summary>
        private static VBoxContainer FishList(VBoxContainer col)
        {
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
            return rows;
        }

        /// <summary>고기 한 줄: 그림 · 이름과 크기, 값 · 신선도 막대와 단계 · 오른쪽 단추</summary>
        private Control FishLine(Hanjeopsi.Core.State.FishItem f, Control right)
        {
            var stage = G.Fresh.StageOf(f.Fresh);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 16);
            var icon = FishIcon(f.Species, 110);
            if (stage == FreshStage.Spoiled) icon.Modulate = new Color(0.7f, 0.62f, 0.55f);
            row.AddChild(icon);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(UiKit.Text(G.Data.SpeciesById(f.Species).Name + " " + f.Size.ToString("0.0", CultureInfo.InvariantCulture) + "cm", TextSize.Small, Palette.Ink, true).Ellipsis());
            text.AddChild(UiKit.Text(UiKit.Won(G.FishValue(f)), TextSize.Tiny + 2, Palette.InkSoft));
            row.AddChild(text);
            var fresh = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            fresh.AddThemeConstantOverride("separation", 6);
            var pill = MarketScreen.StagePill(stage);
            pill.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            fresh.AddChild(pill);
            fresh.AddChild(FreshBar(f.Fresh, stage));
            row.AddChild(fresh);
            if (right != null) row.AddChild(right);
            return row;
        }

        // ───────── 내 장비 ─────────

        /// <summary>
        /// 지금 쓰는 장비를 한눈에: 낚싯대·릴·쿨러 그림과 이름, 소품 단계, 쓰는 미끼와 가진 미끼, 장비로 정해지는 능력치 막대
        /// </summary>
        private void GearPage(VBoxContainer col)
        {
            col.AddChild(Heading(T["gearHead"], T["gearSub"]));
            var scroll = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
                ScrollDeadzone = 24,
                MouseFilter = MouseFilterEnum.Pass,
            };
            col.AddChild(scroll);
            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
            body.AddThemeConstantOverride("separation", 14);
            scroll.AddChild(body);

            // 큰 장비 셋: 그림 카드 세 장
            var g = G.Data.Gear;
            var big = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            big.AddThemeConstantOverride("separation", 14);
            big.AddChild(GearCard("rod", G.Save.Rod + 1, T["rod"], g.Rods[G.Save.Rod].Name));
            big.AddChild(GearCard("reel", G.Save.Reel + 1, T["reel"], g.Reels[G.Save.Reel].Name));
            big.AddChild(GearCard("cooler", G.Save.Tackle + 1, T["coolerName"], g.Tackles[G.Save.Tackle].Name));
            body.AddChild(big);

            // 능력치
            body.AddChild(SubHead(T["abilityHead"]));
            foreach (var a in G.Abilities()) body.AddChild(AbilityRow(a));

            // 미끼
            body.AddChild(SubHead(T["baitHead"]));
            var bait = G.BaitNow();
            string pickName = bait != null ? bait.Name : g.Bait.Name;
            body.AddChild(UiKit.Text(F("baitNow", pickName, G.BaitLeft()), TextSize.Small, Palette.GoldDark, true, true));
            if (G.Save.Kit != null)
            {
                var have = g.Baits.Where(b => G.BaitCount(b.Id) > 0 && (bait == null || b.Id != bait.Id)).Select(b => F("baitHave", b.Name, G.BaitCount(b.Id))).ToList();
                if (have.Count > 0) body.AddChild(UiKit.Text(string.Join("  ·  ", have), TextSize.Tiny + 2, Palette.InkSoft, false, true));
            }

            // 소품 (줄·바늘·찌·뜰채 ...)
            if (g.Kits.Count > 0)
            {
                body.AddChild(SubHead(T["kitHead"]));
                var grid = new GridContainer { Columns = 2, MouseFilter = MouseFilterEnum.Ignore };
                grid.AddThemeConstantOverride("h_separation", 14);
                grid.AddThemeConstantOverride("v_separation", 10);
                foreach (var k in g.Kits) grid.AddChild(KitCell(k));
                body.AddChild(grid);
            }
        }

        private static Control SubHead(string text)
        {
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 6);
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
            col.AddChild(UiKit.Text(text, TextSize.Body, Palette.Ink, true));
            col.AddChild(Rule());
            return col;
        }

        private static TextureRect GearArt(string kind, int tier, float size)
        {
            return new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://art/gear/" + kind + "-" + tier + ".png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(size, size),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }

        /// <summary>장비 그림 카드: 그림 · 종류와 단계 · 이름</summary>
        private Control GearCard(string kind, int tier, string label, string name)
        {
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.Ink, 0.06f), 24, 12, 12).WithBorder(new Color(Palette.Ink, 0.12f), 3));
            var c = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            c.AddThemeConstantOverride("separation", 2);
            card.AddChild(c);
            c.AddChild(GearArt(kind, tier, 150));
            c.AddChild(UiKit.Text(F("gearTier", label, tier), TextSize.Tiny, Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Center));
            c.AddChild(UiKit.Text(name, TextSize.Tiny + 2, Palette.Ink, true, true, 0, null, HorizontalAlignment.Center));
            return card;
        }

        /// <summary>능력치 한 줄: 이름 · 막대 · 값</summary>
        private Control AbilityRow(Hanjeopsi.Core.AbilityLine a)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 16);
            var name = UiKit.Text(T["stat" + a.Id], TextSize.Small, Palette.Ink, true);
            name.CustomMinimumSize = new Vector2(200, 0);
            name.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(name);
            var track = new Panel { CustomMinimumSize = new Vector2(0, 30), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            track.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.Ink, 0.16f), 15));
            var fill = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            // 막대가 찰수록 파랑 → 초록 → 금빛
            var color = a.Fill >= 0.85 ? Palette.Gold : a.Fill >= 0.45 ? Palette.Green : Palette.SeaLight;
            fill.AddThemeStyleboxOverride("panel", UiKit.Box(color, 11));
            track.AddChild(fill);
            float k = (float)a.Fill;
            track.Resized += () =>
            {
                fill.Position = new Vector2(4, 4);
                fill.Size = new Vector2(Mathf.Max(22, (track.Size.X - 8) * k), track.Size.Y - 8);
            };
            row.AddChild(track);
            var value = UiKit.Text(F("statv" + a.Id, a.Value.ToString("0.##", CultureInfo.InvariantCulture)), TextSize.Small, Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Right);
            value.CustomMinimumSize = new Vector2(190, 0);
            value.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(value);
            return row;
        }

        /// <summary>소품 한 칸: 그림 · 이름과 단계 (없으면 흐리게)</summary>
        private Control KitCell(Hanjeopsi.Core.Data.KitKind k)
        {
            int lv = G.KitLevel(k.Id);
            var cell = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            cell.AddThemeConstantOverride("separation", 10);
            var art = GearArt(k.Id, Math.Max(1, lv), 84);
            if (lv == 0) art.Modulate = new Color(0.3f, 0.27f, 0.24f, 0.35f);
            cell.AddChild(art);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(UiKit.Text(lv > 0 ? F("gearTier", k.Name, lv) : k.Name, TextSize.Tiny, Palette.InkSoft).Ellipsis());
            text.AddChild(UiKit.Text(lv > 0 ? k.Levels[lv - 1].Name : T["kitNone"], TextSize.Tiny + 2, lv > 0 ? Palette.Ink : Palette.InkSoft, lv > 0).Ellipsis());
            cell.AddChild(text);
            return cell;
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

        // ───────── 의뢰 ─────────

        /// <summary>맡은 의뢰, 마을에서 기다리는 의뢰, 이번 주 목표</summary>
        private void Quests(VBoxContainer col)
        {
            var reqs = G.Save.Requests;
            col.AddChild(Heading(T["questHead"], T["questSub"]));
            if (reqs.Count == 0) col.AddChild(UiKit.Text(T["questNone"], TextSize.Small + 2, Palette.InkSoft, false, true));
            foreach (var req in reqs.OrderByDescending(r => r.IsAccepted()))
            {
                var t = G.Data.RequestTemplates[req.Template];
                var sp = G.Data.SpeciesById(t.Species);
                int have = Math.Min(G.RequestFishHave(t.Species), t.Count);
                int left = req.Deadline - G.Save.Day;
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 18);
                row.AddChild(FishIcon(t.Species, 110));
                var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 2);
                text.AddChild(UiKit.Text(t.Client + " · " + sp.Name + " " + t.Count + "마리", TextSize.Small + 2, Palette.Ink, true));
                string state = !req.IsAccepted() ? T["questWaiting"] : have >= t.Count ? F("questGo", t.Client) : F("questHave", have, t.Count);
                text.AddChild(UiKit.Text(state + " · " + (left <= 0 ? T["questToday"] : F("questDays", left)) + " · " + UiKit.Won(req.Reward), TextSize.Tiny + 2, !req.IsAccepted() ? Palette.GoldDark : have >= t.Count ? Palette.GreenDark : Palette.InkSoft, false, true));
                row.AddChild(text);
                col.AddChild(row);
            }
            if (G.IsWeekly && G.Save.Debt > 0)
            {
                col.AddChild(Rule());
                col.AddChild(Heading(F("goalHead", G.Save.Week.No), T["goalSub"]));
                var goals = G.WeekGoals();
                for (int i = 0; i < goals.Count; i++)
                {
                    var p = G.GoalProgress(goals[i]);
                    string prog = goals[i].Type == "species" ? "" : "  " + Math.Min(p.Now, p.Need).ToString("#,0") + "/" + p.Need.ToString("#,0");
                    col.AddChild(UiKit.Text((G.GoalDone(i) ? "✔ " : "· ") + goals[i].Text + prog + "  (+" + UiKit.Won(goals[i].Reward) + ")", TextSize.Small, G.GoalDone(i) ? Palette.GreenDark : Palette.Ink, false, true));
                }
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
                    ShowTab(3);
                    break;
                case "detail":
                    ShowTab(3);
                    DexDetail("bollak");
                    break;
                case "taste":
                    ShowTab(4);
                    break;
                case "journal":
                    G.Save.JournalPages = 1;
                    ShowTab(5);
                    break;
                case "quest":
                    if (G.Save.Requests.Count > 0) G.AcceptRequest(G.Save.Requests[0].Id);
                    ShowTab(2);
                    break;
                case "tank":
                    // 활어 둘을 수조에 넣어 둔 모습
                    foreach (var f in G.Save.Cooler.Where(f => G.Fresh.StageOf(f.Fresh) == FreshStage.Live).Take(2).ToList()) G.MoveToTank(f.Uid);
                    _store = 1;
                    ShowTab(0);
                    break;
                case "gear":
                    ShowTab(1);
                    break;
                case "settings":
                    ShowTab(SettingsTab);
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

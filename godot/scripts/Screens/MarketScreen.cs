using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 물비늘포 위판장 (웹 MarketScene). 경매 탭: 오늘 시세판과 내 쿨러, 묶음마다 팔기.
    /// 납품 의뢰 탭: 식당 의뢰 카드와 납품. 노씨 의뢰를 채우면 노씨가 대화창으로 고맙다고 한다.
    /// </summary>
    public partial class MarketScreen : PlaceScreen
    {
        protected override string Music
        {
            get { return "market"; }
        }

        /// <summary>쿨러 한 줄 높이(사이 간격 포함)와 줄 말고 쿨러 상자가 쓰는 높이 (시세판, 제목, 쪽 넘김)</summary>
        private const float RowHeight = 126;
        private const float FixedHeight = 640;
        private int _page;
        private bool _selling;

        private Dictionary<string, string> T
        {
            get { return G.Data.Texts.Auction; }
        }

        private string F(string key, params object[] args)
        {
            return string.Format(T[key], args);
        }

        protected override bool FitsToHeight
        {
            get { return true; }
        }

        /// <summary>화면 높이에 맞춰 한 쪽에 들어가는 줄 수</summary>
        private int PageSize
        {
            get { return Mathf.Max(2, (int)((Content.Size.Y - FixedHeight) / RowHeight)); }
        }

        protected override string Backdrop
        {
            get { return "backgrounds/market"; }
        }

        protected override string TitleText
        {
            get { return T["title"]; }
        }

        protected override IList<string> Tabs
        {
            get { return new[] { T["tabAuction"], F("tabRequest", G.Save.Requests.Count) }; }
        }

        protected override void BuildHeader()
        {
            bool open = G.IsMarketOpen();
            HeaderRight.AddChild(Pill(open ? T["open"] : T["closed"], open ? Palette.GreenDark : Palette.RedDark, Colors.White));
        }

        protected override Control BuildPage()
        {
            SetTabLabel(1, F("tabRequest", G.Save.Requests.Count));
            return Tab == 0 ? AuctionPage() : RequestPage();
        }

        // ───────── 경매 ─────────

        private Control AuctionPage()
        {
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 22);
            col.AddChild(PriceBoard());

            var paper = Paper(36, 30);
            paper.SizeFlagsVertical = SizeFlags.ExpandFill;
            col.AddChild(paper);
            var list = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            list.AddThemeConstantOverride("separation", 14);
            paper.AddChild(list);

            var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            head.AddChild(UiKit.Text(F("cooler", G.Save.Cooler.Count, G.CoolerCapacity()), TextSize.H1, Palette.Ink, true));
            list.AddChild(head);

            var groups = G.CoolerGroups();
            if (groups.Count == 0)
            {
                var empty = UiKit.Text(T["empty"], TextSize.Body, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center);
                empty.SizeFlagsVertical = SizeFlags.ExpandFill;
                empty.VerticalAlignment = VerticalAlignment.Center;
                list.AddChild(empty);
                return col;
            }

            int size = PageSize;
            int pages = (groups.Count + size - 1) / size;
            _page = Mathf.Clamp(_page, 0, pages - 1);
            bool open = G.IsMarketOpen();
            int i = 0;
            foreach (var grp in groups.Skip(_page * size).Take(size)) list.AddChild(GroupRow(grp, i++, open));
            list.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            if (pages > 1)
            {
                list.AddChild(Pager(_page, pages, p =>
                {
                    _page = p;
                    Redraw();
                }));
            }
            if (!open)
            {
                list.AddChild(UiKit.Text(T["closedNote"], TextSize.Small, Palette.RedDark, false, false, 0, null, HorizontalAlignment.Center));
            }
            else
            {
                var sellable = G.SellableFish();
                if (sellable.Count > 0 && pages == 1)
                {
                    int total = sellable.Sum(f => G.FishValue(f));
                    var all = new GameButton(F("sellAll", UiKit.Won(total)), Palette.Red, () => Auction(sellable), null, TextSize.Body, 104);
                    list.AddChild(all);
                }
            }
            return col;
        }

        /// <summary>오늘 시세판: 다섯 어종 그림과 값, 어제보다 오르내린 정도</summary>
        private Control PriceBoard()
        {
            var board = Board(30, 24);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 6);
            board.AddChild(col);
            var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            head.AddChild(UiKit.Text(T["board"], TextSize.Body, Palette.Gold, true));
            head.AddChild(Spacer());
            var sub = UiKit.Text(T["boardSub"], TextSize.Tiny, Palette.GrayText);
            sub.VerticalAlignment = VerticalAlignment.Center;
            sub.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            head.AddChild(sub);
            col.AddChild(head);

            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 4);
            col.AddChild(row);
            int day = G.Save.Day;
            foreach (var sp in G.BoardSpecies())
            {
                bool known = G.Save.Dex.ContainsKey(sp.Id) && G.Save.Dex[sp.Id].Count > 0;
                var cell = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                cell.AddThemeConstantOverride("separation", 0);
                var icon = FishIcon(sp.Id, 150);
                icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                if (!known) icon.Modulate = new Color(0.75f, 0.78f, 0.85f, 0.7f);
                cell.AddChild(icon);
                cell.AddChild(UiKit.Text(sp.Name, TextSize.Tiny + 2, known ? Palette.Paper : Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center));
                cell.AddChild(UiKit.Text(UiKit.Won(G.Market.TodayBasePrice(sp.Id, day)), TextSize.Tiny + 2, Palette.Paper, true, false, 0, null, HorizontalAlignment.Center));
                int c = G.Market.ChangePercent(sp.Id, day);
                string arrow = c > 0 ? "▲" + c + "%" : c < 0 ? "▼" + (-c) + "%" : "-";
                cell.AddChild(UiKit.Text(arrow, TextSize.Tiny, c > 0 ? Palette.RedText : c < 0 ? Palette.BlueText : Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center));
                row.AddChild(cell);
            }
            return board;
        }

        private Control GroupRow(CoolerGroup grp, int i, bool open)
        {
            var bg = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            bg.AddThemeStyleboxOverride("panel", UiKit.Box(i % 2 == 0 ? new Color(Palette.PaperDark, 0.45f) : new Color(Palette.PaperLight, 0.7f), 26, 18, 8));
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 18);
            bg.AddChild(row);

            var icon = FishIcon(grp.Species, 136);
            if (grp.Stage == FreshStage.Spoiled) icon.Modulate = new Color(0.7f, 0.62f, 0.55f);
            row.AddChild(icon);
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 4);
            var sp = G.Data.SpeciesById(grp.Species);
            text.AddChild(UiKit.Text(sp.Name + " ×" + grp.Items.Count, TextSize.Body, Palette.Ink, true));
            var meta = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            meta.AddThemeConstantOverride("separation", 14);
            meta.AddChild(StagePill(grp.Stage));
            meta.AddChild(UiKit.Text(UiKit.Won(grp.Value), TextSize.Small, Palette.InkSoft));
            text.AddChild(meta);
            row.AddChild(text);

            var items = grp.Items;
            var sell = SmallButton(T["sell"], Palette.Red, () => Auction(items), 180, 92);
            sell.SetEnabled(open && grp.Stage != FreshStage.Spoiled);
            row.AddChild(sell);
            return bg;
        }

        /// <summary>신선도 단계 알약 (활어 초록, 선어 파랑, 숙성 금색, 상함 갈색)</summary>
        public static Control StagePill(FreshStage stage)
        {
            var g = GameState.I.Game;
            return Pill(g.Fresh.StageName(stage), StageColor(stage), StageInk(stage), TextSize.Tiny);
        }

        public static Color StageColor(FreshStage stage)
        {
            switch (stage)
            {
                case FreshStage.Live: return new Color("7fd17e");
                case FreshStage.Fresh: return new Color("8cc8ff");
                case FreshStage.Aged: return new Color("f2c14e");
                default: return new Color("c9b8a8");
            }
        }

        public static Color StageInk(FreshStage stage)
        {
            switch (stage)
            {
                case FreshStage.Live: return new Color("24572c");
                case FreshStage.Fresh: return new Color("1f3d66");
                case FreshStage.Aged: return new Color("6b4a0e");
                default: return new Color("5e4f42");
            }
        }

        /// <summary>
        /// 중매인들이 값을 부르는 짧은 경매. 값이 일곱 번 뛰다가 멈추고 "낙찰!" 도장이 쾅 찍힌다.
        /// 낙찰 배율은 0.92~1.15 (웹과 같다)
        /// </summary>
        private void Auction(List<FishItem> items)
        {
            if (_selling) return;
            int baseValue = items.Sum(f => G.FishValue(f));
            if (baseValue <= 0) return;
            _selling = true;
            double finalMul = 0.92 + GameState.I.Rng.Next() * (1.15 - 0.92);
            var uids = items.Select(f => f.Uid).ToList();
            string firstSpecies = items[0].Species;

            var root = new Control { MouseFilter = MouseFilterEnum.Stop };
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var dim = new ColorRect { Color = new Color(Palette.NightDeep, 0.66f), MouseFilter = MouseFilterEnum.Ignore };
            dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddChild(dim);
            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddChild(center);
            var panel = Paper(56, 50);
            panel.CustomMinimumSize = new Vector2(Mathf.Min(900, GetViewportRect().Size.X - 100), 0);
            center.AddChild(panel);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 16);
            panel.AddChild(col);
            var title = UiKit.Text(T["calling"], TextSize.Small, Palette.InkSoft, false, false, 0, null, HorizontalAlignment.Center);
            col.AddChild(title);
            var icon = FishIcon(firstSpecies, 300);
            icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            col.AddChild(icon);
            var price = UiKit.Text(UiKit.Won(Round100(baseValue * 0.8)), TextSize.Title + 26, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center);
            col.AddChild(price);

            // 낙찰 도장: 처음엔 숨겨 두고 마지막에 크게 찍힌다
            var stamp = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
            stamp.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(1, 1, 1, 0), 22, 34, 6).WithBorder(Palette.Red, 8));
            stamp.AddChild(UiKit.Text(T["sold"], TextSize.Title, Palette.Red, true, false, 0, null, HorizontalAlignment.Center));
            var stampHolder = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, 110) };
            stampHolder.AddChild(stamp);
            col.AddChild(stampHolder);
            Popups.AddChild(root);

            panel.Resized += () => panel.PivotOffset = panel.Size / 2;
            stamp.Resized += () => stamp.PivotOffset = stamp.Size / 2;
            dim.Modulate = new Color(1, 1, 1, 0);
            panel.Scale = new Vector2(0.9f, 0.9f);
            var tw = root.CreateTween().SetParallel(true);
            tw.TweenProperty(dim, "modulate:a", 1f, 0.2);
            tw.TweenProperty(panel, "scale", Vector2.One, 0.32).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

            // 값이 툭툭 뛴다 (일곱 걸음)
            var steps = root.CreateTween();
            const int n = 7;
            for (int k = 1; k <= n; k++)
            {
                double v = 0.8 + (finalMul - 0.8) * k / n;
                steps.TweenInterval(0.16);
                steps.TweenCallback(Callable.From(() =>
                {
                    price.Text = UiKit.Won(Round100(baseValue * v));
                    Sound.Play("tick", -2, 1 + (float)(v - 0.8));
                    price.PivotOffset = price.Size / 2;
                    price.Scale = new Vector2(1.06f, 1.06f);
                    price.CreateTween().TweenProperty(price, "scale", Vector2.One, 0.12);
                }));
            }
            steps.TweenCallback(Callable.From(() =>
            {
                int total = G.SellFish(uids, finalMul);
                Sound.Play("stamp");
                Sound.Play("sold", -3);
                Haptics.Pulse(40, 0.7f);
                price.Text = UiKit.Won(total);
                stamp.Scale = new Vector2(2.4f, 2.4f);
                stamp.RotationDegrees = -16;
                var st = stamp.CreateTween().SetParallel(true);
                st.TweenProperty(stamp, "modulate:a", 1f, 0.08);
                st.TweenProperty(stamp, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                st.TweenProperty(stamp, "rotation_degrees", -8f, 0.22);
                Thump(panel);
                if (GameState.I.ShotMode) return;
                GetTree().CreateTimer(0.85).Timeout += () =>
                {
                    var close = root.CreateTween();
                    close.TweenProperty(root, "modulate:a", 0f, 0.16);
                    close.TweenCallback(Callable.From(root.QueueFree));
                    _selling = false;
                    HudBar.Refresh();
                    FloatText("+" + UiKit.Won(total), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 420), Palette.Gold);
                    Sound.Play("coin");
                    Redraw();
                };
            }));
        }

        /// <summary>도장 찍힐 때 상자가 쿵 흔들린다</summary>
        private static void Thump(Control c)
        {
            var tw = c.CreateTween();
            tw.TweenProperty(c, "position:y", c.Position.Y + 10, 0.05);
            tw.TweenProperty(c, "position:y", c.Position.Y, 0.16).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        }

        private static int Round100(double v)
        {
            return (int)(Math.Round(v / 100) * 100);
        }

        // ───────── 납품 의뢰 ─────────

        private Control RequestPage()
        {
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 26);
            var reqs = G.Save.Requests;
            if (reqs.Count == 0)
            {
                var paper = Paper();
                paper.CustomMinimumSize = new Vector2(0, 420);
                var l = UiKit.Text(T["noRequest"], TextSize.Body, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center);
                l.VerticalAlignment = VerticalAlignment.Center;
                paper.AddChild(l);
                col.AddChild(paper);
                return col;
            }
            foreach (var req in reqs.ToList()) col.AddChild(RequestCard(req));
            return col;
        }

        private Control RequestCard(DeliveryRequest req)
        {
            var t = G.Data.RequestTemplates[req.Template];
            var sp = G.Data.SpeciesById(t.Species);
            var card = Paper(44, 34);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 16);
            card.AddChild(col);

            var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var client = UiKit.Text(t.Client, TextSize.H1, Palette.Ink, true);
            client.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            head.AddChild(client);
            int left = req.Deadline - G.Save.Day;
            head.AddChild(Pill(left <= 0 ? T["today"] : F("dday", left), left <= 1 ? Palette.Red : Palette.GrayDark, Colors.White));
            col.AddChild(head);
            col.AddChild(UiKit.Text("“" + t.Text + "”", TextSize.Small + 2, Palette.InkSoft, false, true));
            col.AddChild(Rule());

            var mid = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            mid.AddThemeConstantOverride("separation", 24);
            mid.AddChild(FishIcon(t.Species, 230));
            var need = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            need.AddThemeConstantOverride("separation", 6);
            need.AddChild(UiKit.Text(F("need", sp.Name, t.Count), TextSize.Body + 2, Palette.Ink, true));
            need.AddChild(UiKit.Text(T["needSub"], TextSize.Tiny, Palette.InkSoft));
            int have = G.RequestFishHave(t.Species);
            need.AddChild(UiKit.Text(F("have", have), TextSize.Small, have >= t.Count ? Palette.GreenDark : Palette.RedDark, true));
            mid.AddChild(need);
            col.AddChild(mid);

            var foot = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var reward = UiKit.Text(F("reward", UiKit.Won(req.Reward)), TextSize.H1, Palette.RedDark, true);
            reward.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            reward.VerticalAlignment = VerticalAlignment.Center;
            foot.AddChild(reward);
            var btn = SmallButton(T["deliver"], Palette.Green, () => ConfirmDeliver(req), 260, 100);
            btn.SetEnabled(G.FishForRequest(req.Id) != null);
            foot.AddChild(btn);
            col.AddChild(foot);
            return card;
        }

        private void ConfirmDeliver(DeliveryRequest req)
        {
            var t = G.Data.RequestTemplates[req.Template];
            var sp = G.Data.SpeciesById(t.Species);
            Modal(F("deliverTitle", t.Client), F("deliverBody", sp.Name, t.Count), new[]
            {
                new ModalButton(T["cancel"], Palette.GrayDark),
                new ModalButton(T["deliverOk"], Palette.Green, () =>
                {
                    int reward = G.DeliverRequest(req.Id);
                    if (reward <= 0) return;
                    HudBar.Refresh();
                    FloatText("+" + UiKit.Won(reward), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 420), Palette.Gold);
                    Sound.Play("coin");
                    Redraw();
                    // 노씨는 대화창으로, 다른 손님은 짧은 알림으로
                    if (!string.IsNullOrEmpty(t.Flag) && t.Client == G.Data.Dialogue.SpeakerById("nossi").Name)
                        Say("nossi", "default", G.Data.Dialogue.NossiSays["favor"]);
                    else Toast(t.Client + ": " + T["thanks"]);
                }),
            });
        }

        // ───────── CI 사진 ─────────

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "auction":
                    Auction(G.CoolerGroups()[0].Items);
                    break;
                case "request":
                    ShowTab(1);
                    break;
            }
        }
    }
}

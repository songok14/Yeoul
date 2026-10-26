using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 물비늘포 마을 (웹 TownScene). 시간대별 배경, 건물 간판, SD 여울과 고등어, 트럭, 아래 메뉴 여섯 개.
    /// 들어올 때 1장 결말과 어제 요약을 확인한다. 할머니댁에서 빚을 갚고 잠을 잔다.
    /// </summary>
    public partial class TownScreen : Screen
    {
        protected override string Music
        {
            get { return "town"; }
        }

        private static readonly string[] YeoulPoses = { "front", "wave", "three-quarter", "idle", "side" };
        private static readonly string[] CatRest = { "sit-front", "sit-side", "loaf" };

        private CoreGame G
        {
            get { return GameState.I.Game; }
        }

        private ArtStage _stage;
        private Hud _hud;
        private PanelContainer _menu;
        private TextureRect _yeoul, _cat, _truck;
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private float _time;
        private float _catTimer = 5;
        private float _yeoulReset;
        private float _footY = 636;
        /// <summary>간판을 눌러 여울이 그쪽으로 걸어가는 중 (다른 누름은 받지 않는다)</summary>
        private bool _walking;
        private float _truckX = 330;
        /// <summary>배경 그림 속 푸드트럭 자리 (논리 좌표). 비 그림 두 장은 트럭이 다른 곳에 있다</summary>
        private static readonly Rect2 TruckArea = new Rect2(232, 290, 208, 115);
        private static readonly Dictionary<string, Rect2> TruckAreaByArt = new Dictionary<string, Rect2>
        {
            { "village-rain", new Rect2(218, 270, 226, 105) },
            { "village-rain-dusk", new Rect2(230, 342, 214, 113) },
        };
        private int _yeoulLine;

        protected override void Build()
        {
            var s = G.Save;

            // ── 그림 ──
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            PaintWeather(s.Slot);
            Art.AddChild(_stage);

            // 간판은 그림 속 건물 위 (2026-10-22 눈높이 마을 그림). 긴 화면에서 좌우가 잘려도 보이게 x 90~370 안에 둔다
            _stage.Pin(new SignButton("위판장", OpenMarket), new Vector2(150, 222));
            _stage.Pin(new SignButton("미끼가게", OpenShop), new Vector2(96, 318));
            _stage.Pin(new SignButton("할머니댁", Restaurant), new Vector2(370, 262));
            _stage.Pin(new SignButton("방파제 →", GoFishing), new Vector2(330, 196));

            // 여울호는 배경 그림 속 푸드트럭. 그 자리에 보이지 않는 누름 영역과 간판을 둔다
            Rect2 truckArea;
            if (!TruckAreaByArt.TryGetValue(BackgroundName(), out truckArea)) truckArea = TruckArea;
            _truck = _stage.Place(Clear(truckArea.Size), truckArea.End, truckArea.Size.Y, new Vector2(1, 1));
            _truckX = truckArea.GetCenter().X;
            Tappable(_truck, OpenTruck);
            _stage.Pin(new SignButton("여울호", OpenTruck), new Vector2(truckArea.GetCenter().X - 4, truckArea.End.Y + 15));
            _cat = _stage.Place(Tex("cat/sit-front"), new Vector2(118, 636), 58);
            Tappable(_cat, TapCat);
            _yeoul = _stage.Place(Tex("yeoul/front"), new Vector2(205, 636), 138);
            Tappable(_yeoul, TapYeoul);

            // ── 위: HUD 와 오늘 소식 ──
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 18);
            Safe.AddChild(col);
            _hud = new Hud(G);
            col.AddChild(_hud);
            col.AddChild(NewsRibbon());
            if (G.IsWeekly && s.Debt > 0) col.AddChild(GoalsPill());
            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            // ── 아래: 메뉴 ──
            _menu = new PanelContainer();
            col.AddChild(_menu);
            var grid = new GridContainer { Columns = 3 };
            grid.AddThemeConstantOverride("h_separation", 22);
            grid.AddThemeConstantOverride("v_separation", 22);
            _menu.AddChild(grid);
            var fishing = new GameButton("방파제", Palette.Blue, GoFishing, !G.CanFishToday() ? "태풍 · 출입 금지" : G.Save.Slot == G.Data.Balance.Time.SlotsPerDay - 1 ? "밤낚시 · 1칸" : "낚시 · 1칸");
            fishing.SetEnabled(G.CanFishToday());
            grid.AddChild(fishing);
            grid.AddChild(new GameButton("위판장", Palette.Teal, OpenMarket, G.IsMarketOpen() ? "경매 중" : "경매 마감"));
            var truck = new GameButton("여울호", Palette.Red, OpenTruck, G.IsTruckOpen() ? "손님 " + G.CustomersNow() + " · 1칸" : "새벽엔 쉼");
            truck.SetEnabled(G.IsTruckOpen());
            grid.AddChild(truck);
            var shop = new GameButton("미끼가게", Palette.GreenDark, OpenShop, G.IsShopOpen() ? "장비 · 미끼" : "밤엔 닫음");
            shop.SetEnabled(G.IsShopOpen());
            grid.AddChild(shop);
            grid.AddChild(new GameButton("할머니댁", Palette.Wood, Restaurant, "상환 · 휴식"));
            grid.AddChild(new GameButton("수첩", Palette.Purple, OpenNotebook, "도감 · 미각"));
            _menu.Resized += PlaceOnFloor;
            OnSafeAreaChanged();

            // 메뉴가 아래에서 올라오고 HUD 가 위에서 내려온다
            _menu.Modulate = new Color(1, 1, 1, 0);
            _hud.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetParallel(true);
            tw.TweenProperty(_menu, "modulate:a", 1f, 0.45).SetDelay(0.1);
            tw.TweenProperty(_hud, "modulate:a", 1f, 0.45);

            CallDeferred(MethodName.OnEnter);
        }

        /// <summary>
        /// 오늘 날씨에 맞는 마을 그림과 비·눈·안개. 날씨 그림은 낮·밤 두 장이라 새벽과 저녁은 색만 입힌다.
        /// 날씨 효과는 배경 바로 위, 간판·캐릭터 아래에 둔다
        /// </summary>
        private void PaintWeather(int slot)
        {
            string id = G.WeatherToday().Id;
            bool night = slot == G.Data.Balance.Time.SlotsPerDay - 1;
            string art = G.TownArt();
            Color tint = Colors.White;
            var dayTint = slot == 0 ? new Color("c8c6e8") : slot == 3 ? new Color("ffd6bc") : Colors.White;
            switch (id)
            {
                case "rain":
                    // 비 그림은 낮과 노을. 밤비는 밤 그림을 더 어둡게
                    if (night)
                    {
                        art = "village-night";
                        tint = new Color("aab0cc");
                    }
                    else art = slot == 3 ? "village-rain-dusk" : "village-rain";
                    if (slot == 0) tint = dayTint;
                    break;
                case "typhoon":
                case "snow":
                case "fog":
                    art = "village-" + id + (night ? "-night" : "");
                    if (!night) tint = dayTint;
                    break;
            }
            _stage.Background = ArtStage.LoadBackground(art);
            if (tint != Colors.White)
            {
                _stage.KeepClouds = false;
                _stage.Tint = tint;
            }
            Control fx = null;
            if (id == "rain" || id == "typhoon") fx = new RainOverlay { Name = "RainOverlay", Intensity = night ? 1 : 1.6f };
            else if (id == "snow") fx = new SnowOverlay { Name = "SnowOverlay" };
            else if (id == "fog") fx = new FogOverlay { Name = "FogOverlay", Center = 0.3f, Strength = 0.35f, Tone = night ? new Color("8890b0") : new Color("f2f2f4") };
            if (fx == null) return;
            fx.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.AddChild(fx);
        }

        /// <summary>HUD 는 노치 밑까지, 메뉴는 제스처 바 밑까지 칠한다. 글자와 버튼만 안전 영역 안에 둔다</summary>
        protected override void OnSafeAreaChanged()
        {
            if (_menu == null) return;
            Safe.AddThemeConstantOverride("margin_top", 0);
            Safe.AddThemeConstantOverride("margin_bottom", 0);
            var hudBox = (StyleBoxFlat)_hud.GetThemeStylebox("panel").Duplicate();
            hudBox.ContentMarginTop = 22 + SafeTop;
            _hud.AddThemeStyleboxOverride("panel", hudBox);
            var sb = UiKit.Box(new Color(Palette.Panel, 0.97f), 0, 30, 30).WithShadow(0.4f, 24, -4);
            sb.CornerRadiusTopLeft = 44;
            sb.CornerRadiusTopRight = 44;
            sb.BorderColor = new Color(1, 1, 1, 0.08f);
            sb.BorderWidthTop = 3;
            sb.ContentMarginBottom = 30 + SafeBottom;
            _menu.AddThemeStyleboxOverride("panel", sb);
        }

        private Control NewsRibbon()
        {
            var closed = G.ClosedToday();
            var ev = G.EventToday();
            string headline = ev != null ? ev.Name + "! " + ev.Note : closed.Count > 0 ? string.Join("·", closed) + " 금어기" : G.HeadlineToday();
            var pill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.62f), 30, 30, 10));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 16);
            row.AddChild(UiKit.Text(G.DateLabel(), TextSize.Small - 2, Palette.Gold, true));
            // 날씨가 궂으면 날짜 옆에 날씨 이름
            var w = G.WeatherToday();
            if (w.Id != "clear") row.AddChild(UiKit.Text(w.Name, TextSize.Small - 2, new Color("9fd0ff"), true));
            var news = UiKit.Text(headline, TextSize.Small - 2, Palette.Paper);
            news.CustomMinimumSize = new Vector2(Mathf.Min(UiKit.Measure(headline, TextSize.Small - 2) + 4, 760), 0);
            news.Ellipsis();
            row.AddChild(news);
            pill.AddChild(row);
            return Riser(pill);
        }

        /// <summary>이번 주 목표 몇 개 했는지. 누르면 목표 목록</summary>
        private List<WeekGoal> _newGoals = new List<WeekGoal>();

        private Control GoalsPill()
        {
            _newGoals = G.CheckGoals();
            var goals = G.WeekGoals();
            int done = Enumerable.Range(0, goals.Count).Count(G.GoalDone);
            var pill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.7f), 30, 28, 8).WithBorder(new Color(Palette.Gold, 0.7f), 3));
            pill.AddChild(UiKit.Text(G.Save.Week.No + "주 목표 " + done + "/" + goals.Count, TextSize.Small - 2, done == goals.Count ? Palette.Gold : Palette.Paper, true));
            pill.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) ShowGoals();
            };
            return Riser(pill);
        }

        /// <summary>여울과 고등어, 트럭이 메뉴 위 길에 서도록. 화면이 길면 그림 속 원래 자리</summary>
        private void PlaceOnFloor()
        {
            float menuTop = _menu.GetGlobalRect().Position.Y - _stage.GetGlobalRect().Position.Y;
            _footY = Mathf.Min(640, _stage.ToLogical(new Vector2(0, menuTop)).Y + 6);
            _stage.Move(_yeoul, new Vector2(205, _footY));
            _stage.Move(_cat, new Vector2(118, _footY));
        }

        private Texture2D Tex(string path)
        {
            Texture2D t;
            if (!_tex.TryGetValue(path, out t))
            {
                // 최적화 배경은 WebP를 우선 사용하고 기존 PNG도 지원한다.
                t = GD.Load<Texture2D>("res://art/" + path + ".webp");
                if (t == null) t = GD.Load<Texture2D>("res://art/" + path + ".png");
                _tex[path] = t;
            }
            return t;
        }

        /// <summary>투명한 빈 그림 (누름 영역용)</summary>
        private static Texture2D Clear(Vector2 size)
        {
            return ImageTexture.CreateFromImage(Image.CreateEmpty((int)size.X, (int)size.Y, false, Image.Format.Rgba8));
        }

        private string BackgroundName()
        {
            var path = _stage.Background != null ? _stage.Background.ResourcePath : "";
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private static void Tappable(Control c, Action onTap)
        {
            c.MouseFilter = MouseFilterEnum.Stop;
            c.MouseDefaultCursorShape = CursorShape.PointingHand;
            c.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) onTap();
            };
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _time += dt;
            // 숨 쉬기
            _yeoul.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.2f) * 0.012f);
            _cat.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.8f + 1) * 0.02f);
            _stage.Zoom = 1.01f + Mathf.Sin(_time * 0.1f) * 0.01f;

            // 여울은 손 흔든 뒤 정면으로
            if (_yeoulReset > 0)
            {
                _yeoulReset -= dt;
                if (_yeoulReset <= 0) _stage.Swap(_yeoul, Tex("yeoul/front"));
            }
            // 고등어는 가끔 자세를 바꾼다
            _catTimer -= dt;
            if (_catTimer <= 0)
            {
                _catTimer = 4 + (float)GameState.I.Rng.Next() * 5;
                _stage.Swap(_cat, Tex("cat/" + CatRest[(int)(GameState.I.Rng.Next() * CatRest.Length)]));
            }
        }

        // ───────── 들어올 때 ─────────

        private void OnEnter()
        {
            var gs = GameState.I;
            var outcome = G.CheckChapter();
            if (outcome == ChapterOutcome.Clear)
            {
                G.Persist();
                gs.GoStory("clear");
                return;
            }
            if (outcome == ChapterOutcome.Fail)
            {
                gs.GoStory("fail");
                return;
            }
            if (gs.PendingTalk != null)
            {
                string talk = gs.PendingTalk;
                gs.PendingTalk = null;
                TalkScene(talk);
                return;
            }
            var s = G.Save;
            if (s.PendingSummary != null)
            {
                var sum = s.PendingSummary;
                s.PendingSummary = null;
                G.Persist();
                var lines = new List<string> { "어제 번 돈 " + UiKit.Won(sum.Earned) };
                if (sum.Spoiled > 0) lines.Add("쿨러에서 " + sum.Spoiled + "마리가 상했다");
                if (sum.ExpiredRequests > 0) lines.Add("의뢰 " + sum.ExpiredRequests + "건이 기한을 넘겼다");
                lines.Add("위판장 시세가 바뀌었다");
                if (sum.Trap != null && sum.Trap.Count > 0) lines.Add("통발에 " + string.Join("·", sum.Trap.GroupBy(n => n).Select(g => g.Key + (g.Count() > 1 ? " " + g.Count() : ""))) + " 들었다");
                else if (sum.Trap != null) lines.Add("통발이 비었다");
                var w = G.WeatherToday();
                if (w.Id != "clear") lines.Add(w.Note);
                if (sum.Rival.HasValue) lines.Add(sum.Rival.Value > 0 ? "차세라와 대결에서 이겼다 (" + sum.Rival.Value.ToString("0.#") + "cm)" : "차세라와 대결에서 졌다");
                var ev = G.EventToday();
                if (ev != null) lines.Add(ev.Note);
                if (G.IsWeekly && s.Debt > 0)
                {
                    int d = G.DaysToDue();
                    lines.Add(s.Week.No + "주 상환까지 " + (d > 0 ? d + "일 · " : "오늘 · ") + UiKit.Won(G.DueNow()));
                }
                else
                {
                    int left = G.DaysLeft();
                    lines.Add(s.Debt > 0 ? "상환까지 " + (left > 0 ? left + "일" : "오늘이 마지막 날") : "1차 상환은 끝났다");
                }
                var week = sum.Week;
                Modal(s.Day + "일차 " + G.SlotName(), string.Join("\n", lines), new[] { new ModalButton("좋아", Palette.Green, () => { if (week != null) PayDay(week); }) });
            }
            // 보상 장비가 생기면 노씨가 미끼가게에서 기다린다
            if (_newGoals.Count > 0)
            {
                Toast("목표 달성! " + string.Join(", ", _newGoals.Select(x => x.Text)) + " (+" + UiKit.Won(_newGoals.Sum(x => x.Reward)) + ")", Palette.Gold, SafeTop + 520);
                Sound.Play("good", -4);
                _hud.Refresh();
            }
            else if (G.HasGift()) Toast("노씨가 미끼가게에서 부른다", Palette.Gold, SafeTop + 520);
        }

        /// <summary>
        /// 상환일 장면: 은행 직원이 다녀간 뒤 할머니와 여울. 다 냈으면 다음 주 이자와 몫,
        /// 못 냈으면 연체료와 경고. 차압은 결말 이야기가 맡는다
        /// </summary>
        private void PayDay(WeekResult r)
        {
            if (r.Seized) return;
            var lines = new List<DialogueLine>();
            if (r.Paid)
            {
                lines.Add(new DialogueLine { Text = r.No + "주 상환일. 은행 직원이 통지서에 도장을 찍고 갔다." });
                lines.Add(new DialogueLine { Speaker = "grandma", Face = "default", Text = "애썼다. 밥 먹고 해." });
                if (r.NextDue > 0)
                    lines.Add(new DialogueLine { Speaker = "yeoul", Face = "default", Text = "다음 주는 " + UiKit.Won(r.NextDue) + ".\n이자 " + UiKit.Won(r.NextInterest) + "도 붙었다." });
            }
            else
            {
                lines.Add(new DialogueLine { Text = r.No + "주 상환일. " + UiKit.Won(r.Short) + "이 모자랐다.\n연체료 " + UiKit.Won(r.LateFee) + "이 붙었다." });
                lines.Add(new DialogueLine { Text = "은행 직원이 말했다. 한 번 더 밀리면 기다려 줄 수 없다고." });
                lines.Add(new DialogueLine { Speaker = "grandma", Face = "default", Text = "괜찮다. 아직 안 끝났어." });
                lines.Add(new DialogueLine { Speaker = "yeoul", Face = "default", Text = "이번 주에 " + UiKit.Won(r.NextDue) + ".\n꼭 낸다." });
            }
            Talk(lines);
        }

        /// <summary>이번 주 목표: 진행과 보상. 다 한 목표는 금빛</summary>
        private void ShowGoals()
        {
            G.CheckGoals();
            _hud.Refresh();
            var goals = G.WeekGoals();
            var rows = new List<string>();
            for (int i = 0; i < goals.Count; i++)
            {
                var p = G.GoalProgress(goals[i]);
                string mark = G.GoalDone(i) ? "✔ " : "· ";
                string prog = goals[i].Type == "species" ? "" : "  " + Math.Min(p.Now, p.Need).ToString("#,0") + "/" + p.Need.ToString("#,0");
                rows.Add(mark + goals[i].Text + prog + "  (+" + UiKit.Won(goals[i].Reward) + ")");
            }
            var ev = G.Data.Weekly.Events.FirstOrDefault(e => e.Day > G.Save.Day - 1 && e.Day <= G.DueDay());
            if (ev != null) rows.Add("\n" + ev.Day + "일 " + ev.Name);
            Modal(G.Save.Week.No + "주 목표", string.Join("\n", rows), new[] { new ModalButton("좋아", Palette.Green) });
        }

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "restaurant":
                    Restaurant();
                    break;
                case "welcome":
                    TalkScene("welcome");
                    break;
                case "clear":
                    TalkScene("chapterClear");
                    break;
                case "yeoul":
                    TapYeoul();
                    TapCat();
                    break;
                case "goals":
                    ShowGoals();
                    break;
                case "paid":
                    PayDay(new WeekResult { No = 1, Paid = true, NextInterest = 78_000, NextDue = 678_000 });
                    break;
                case "late":
                    PayDay(new WeekResult { No = 1, Paid = false, Short = 120_000, LateFee = 12_000, NextDue = 810_000 });
                    break;
            }
        }

        // ───────── 캐릭터 ─────────

        private void TapYeoul()
        {
            string pose = YeoulPoses[1 + _yeoulLine % (YeoulPoses.Length - 1)];
            _stage.Swap(_yeoul, Tex("yeoul/" + pose));
            _yeoulReset = 2.2f;
            var lines = G.Data.Dialogue.YeoulIdle;
            string line = lines[_yeoulLine++ % lines.Count];
            Hop(_yeoul, 10);
            Bubble(Art, line, _stage.ToLocal(new Vector2(205, _footY - 142)), 2.2f);
        }

        private void TapCat()
        {
            int bad = G.SpoiledFish().Count;
            if (bad > 0)
            {
                Modal("고등어가 쿨러를 쳐다본다", "상한 생선 " + bad + "마리를 고등어에게 줄까?\n친해지면 좋은 자리를 더 정확히 알려 준다.", new[]
                {
                    new ModalButton("아니", Palette.GrayDark),
                    new ModalButton("주기", Palette.Green, () =>
                    {
                        G.FeedCat();
                        Sound.Play("meow");
                        _stage.Swap(_cat, Tex("cat/eat"));
                        _catTimer = 3;
                        Toast("고등어가 골골거린다 (호감 " + G.Save.CatAffinity + "/" + G.Data.Balance.Cat.MaxAffinity + ")");
                    }),
                });
                return;
            }
            var idle = G.Data.Dialogue.CatIdle;
            _stage.Swap(_cat, Tex("cat/meow-1"));
            _catTimer = 1.6f;
            Hop(_cat, 14);
            Bubble(Art, idle[(int)(GameState.I.Rng.Next() * idle.Count)], _stage.ToLocal(new Vector2(118, _footY - 62)), 1.3f);
        }

        /// <summary>제자리에서 톡 뛴다 (발밑 기준 크기 튕김)</summary>
        private static void Hop(Control c, float px)
        {
            var tw = c.CreateTween();
            float y = c.Position.Y;
            tw.TweenProperty(c, "position:y", y - px, 0.1).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(c, "position:y", y, 0.16).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        }

        // ───────── 메뉴 ─────────

        private void GoFishing()
        {
            if (!G.CanFishToday())
            {
                Toast("태풍이 와서 방파제 출입이 막혔다");
                return;
            }
            if (G.BaitLeft() <= 0 && !G.IsShopOpen())
            {
                Modal("미끼가 없다", "미끼가게는 문을 닫았다.\n내일 새벽에 사 오자.", new[] { new ModalButton("닫기", Palette.GrayDark) });
                return;
            }
            if (G.BaitLeft() <= 0)
            {
                Modal("미끼가 없다", "노씨 가게에서 크릴을 사 오자.", new[]
                {
                    new ModalButton("닫기", Palette.GrayDark),
                    new ModalButton("미끼가게", Palette.GreenDark, OpenShop),
                });
                return;
            }
            WalkThen(430, () => GameState.I.GoTo("fishing"));
        }

        /// <summary>
        /// 여울이 그 건물 쪽으로 걸어가고(고등어가 뒤따른다) 화면이 바뀐다. 사진 모드는 바로 간다
        /// </summary>
        private void WalkThen(float x, Action then)
        {
            if (_walking) return;
            if (GameState.I.ShotMode)
            {
                then();
                return;
            }
            _walking = true;
            _yeoulReset = 0;
            var from = new Vector2(205, _footY);
            var to = new Vector2(Mathf.Clamp(x, 40, ArtStage.Logical.X - 40), _footY - 26);
            float dist = from.DistanceTo(to);
            double time = Mathf.Clamp(dist / 260f, 0.35f, 0.8f);
            // 그림은 왼쪽을 보고 걷는다
            _yeoul.FlipH = to.X > from.X;
            _cat.FlipH = to.X > from.X;
            var catFrom = new Vector2(118, _footY);
            var tw = CreateTween();
            tw.TweenMethod(Callable.From<float>(t =>
            {
                _stage.Move(_yeoul, from.Lerp(to, t));
                // 고등어는 조금 늦게 따라간다
                _stage.Move(_cat, catFrom.Lerp(to + new Vector2(to.X > from.X ? -60 : 60, 4), Mathf.Max(0, t * 1.1f - 0.1f)));
                int frame = (int)(t * dist / 22) % 2;
                _stage.Swap(_yeoul, Tex("yeoul/walk-" + (frame + 1)));
                _stage.Swap(_cat, Tex("cat/walk-" + (2 - frame)));
            }), 0f, 1f, time);
            Sound.Play("step", -6);
            tw.TweenCallback(Callable.From(() =>
            {
                _walking = false;
                then();
            }));
        }

        /// <summary>대화만 하고 돌아올 때 (할머니댁) 제자리로</summary>
        private void WalkBack()
        {
            _yeoul.FlipH = false;
            _cat.FlipH = false;
            _stage.Swap(_yeoul, Tex("yeoul/front"));
            _stage.Swap(_cat, Tex("cat/sit-front"));
            PlaceOnFloor();
        }

        private void OpenMarket()
        {
            WalkThen(150, () => GameState.I.GoTo("market"));
        }

        private void OpenTruck()
        {
            if (!G.IsTruckOpen())
            {
                Toast("새벽엔 손님이 없다");
                return;
            }
            WalkThen(_truckX, () => GameState.I.GoTo("truck"));
        }

        private void OpenShop()
        {
            if (!G.IsShopOpen())
            {
                Toast("미끼가게는 문을 닫았다");
                return;
            }
            WalkThen(80, () => GameState.I.GoTo("shop"));
        }

        private void OpenNotebook()
        {
            GameState.I.GoTo("notebook");
        }

        /// <summary>할머니댁: 할머니가 한마디 하고, 대화창 위에 갚기·잠자기 선택지가 뜬다</summary>
        private void Restaurant()
        {
            WalkThen(370, RestaurantTalk);
        }

        private void RestaurantTalk()
        {
            var s = G.Save;
            int canPay = Math.Min(s.Money, s.Debt);
            int step = G.Data.Balance.Chapter1.RepayStep;
            var lines = G.Data.Dialogue.Grandma;
            string line = lines[(int)(GameState.I.Rng.Next() * lines.Count)];
            Say("grandma", "default", line, new[]
            {
                new ModalButton(G.IsWeekly ? "이번 주 몫 갚기 (" + UiKit.Won(Math.Min(s.Money, G.DueNow())) + ")" : "10만 원 갚기", Palette.GoldDark,
                    () => Pay(G.IsWeekly ? Math.Min(s.Money, G.DueNow()) : step),
                    G.IsWeekly ? G.DueNow() <= 0 || s.Money <= 0 : s.Debt <= 0 || s.Money < step),
                new ModalButton("있는 돈 전부 갚기 (" + UiKit.Won(canPay) + ")", Palette.Red, () => Pay(canPay), canPay <= 0),
                new ModalButton("하루 마치기 (잠자기)", Palette.BlueDark, Sleep),
                new ModalButton("나가기", Palette.GrayDark, WalkBack),
            });
        }

        private void Pay(int amount)
        {
            int paid = G.Repay(amount);
            if (paid <= 0) return;
            _hud.Refresh();
            FloatText("-" + UiKit.Won(paid), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 420), Palette.RedText);
            if (G.Save.Debt <= 0) GetTree().CreateTimer(0.9).Timeout += OnEnter;
            else Toast("할머니가 말없이 손을 잡아 주셨다");
        }

        private void Sleep()
        {
            Modal("오늘을 마칠까요?", G.SlotName() + " 이후의 시간이 지나가고\n다음 날 새벽이 됩니다.\n쿨러의 생선 신선도가 떨어져요.", new[]
            {
                new ModalButton("아니", Palette.GrayDark),
                new ModalButton("잠자기", Palette.BlueDark, () =>
                {
                    G.EndDay();
                    GameState.I.GoTo("town");
                }),
            });
        }
    }
}

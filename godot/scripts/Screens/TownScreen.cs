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
        private FootShadows _shadows;
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private float _time;
        private float _catTimer = 5;
        private float _yeoulReset;
        private float _footY = 636;
        /// <summary>간판을 눌러 여울이 그쪽으로 걸어가는 중 (다른 누름은 받지 않는다)</summary>
        private bool _walking;

        /// <summary>마당을 지나가는 마을 사람 (걷기 그림은 오른쪽을 본다)</summary>
        private sealed class Walker
        {
            public Villager Def;
            public TextureRect Node;
            public float X, Lane, Dir, Frame, Stop, Speed;
            public float Height;
        }

        private readonly List<Walker> _walkers = new List<Walker>();
        /// <summary>지나가는 줄 (여울 발밑 기준 위로). 줄마다 한 사람만 걸어 서로 겹치지 않는다. 뒤 줄일수록 작다</summary>
        private static readonly float[] Lanes = { -84, -112, -140 };
        /// <summary>마당 SD 키 (논리 좌표). 넓은 앞마당 그림에 묻히지 않게 2026-10-27 키웠다 (여울 138→168)</summary>
        private const float YeoulSize = 168, CatSize = 66, VillagerSize = 122, ClientSize = 126;

        /// <summary>고등어 포즈 바꾸기. 포즈 그림마다 높이가 달라(식빵 굽기는 낮다) 같은 몸 크기로 보이게 그림 높이에 비례해 맞춘다</summary>
        private void SwapCat(Texture2D tex)
        {
            _stage.Swap(_cat, tex, CatSize * tex.GetHeight() / 302f, new Vector2(0.5f, 1));
        }
        /// <summary>마을 사람은 이 자리 아래에 끼워 넣는다 (의뢰인·여울·고등어·간판보다 뒤)</summary>
        private Control _walkerLayer;
        private static readonly float[] StartXs = { 115, 285, 355 };
        private int _walkersWanted;
        private float _spawnTimer;

        /// <summary>마을에 서 있는 의뢰인과 머리 위 표시 (! 새 의뢰, 볼락 1/2 모으는 중, ✔ 건넬 수 있음)</summary>
        private readonly List<(int Id, TextureRect Node, Control Mark)> _clients = new List<(int, TextureRect, Control)>();
        /// <summary>의뢰인 자리: 마당 양옆, 여울보다 조금 뒤</summary>
        private Vector2 ClientSpot(int i)
        {
            return i == 0 ? new Vector2(58, _footY - 46) : new Vector2(404, _footY - 34);
        }
        private Control _tracker;
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
            // 하늘에 갈매기 (밤과 궂은 날은 빼고)
            string w = G.WeatherToday().Id;
            if (s.Slot != G.Data.Balance.Time.SlotsPerDay - 1 && w != "typhoon" && w != "snow")
            {
                var gulls = new Gulls(_stage, 4, s.Slot == 3 ? new Color("4a3a40") : new Color("33404d"));
                gulls.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _stage.AddChild(gulls);
            }

            // 마당에 선 사람들 발밑 그림자 (모두 이 위에 선다)
            _shadows = new FootShadows(_stage);
            _shadows.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.AddChild(_shadows);

            // 간판은 그림 속 건물 위 (2026-10-22 눈높이 마을 그림). 긴 화면에서 좌우가 잘려도 보이게 x 90~370 안에 둔다
            // 간판에 지금 상태를 함께 적는다 (아래 버튼판 대신). 닫힌 곳은 흐리게
            _stage.Pin(Sign("위판장", G.IsMarketOpen() ? "경매 중" : "경매 마감", true, OpenMarket), new Vector2(150, 222));
            _stage.Pin(Sign("미끼가게", G.IsShopOpen() ? null : "닫음", G.IsShopOpen(), OpenShop), new Vector2(96, 318));
            _stage.Pin(Sign("할머니댁", "상환 · 잠", true, Restaurant), new Vector2(370, 262));
            _stage.Pin(Sign("방파제 →", !G.CanFishToday() ? "출입 금지" : G.Save.Slot == G.Data.Balance.Time.SlotsPerDay - 1 ? "밤낚시" : null, G.CanFishToday(), GoFishing), new Vector2(330, 196));

            // 여울호는 배경 그림 속 푸드트럭. 그 자리에 보이지 않는 누름 영역과 간판을 둔다
            Rect2 truckArea;
            if (!TruckAreaByArt.TryGetValue(BackgroundName(), out truckArea)) truckArea = TruckArea;
            _truck = _stage.Place(Clear(truckArea.Size), truckArea.End, truckArea.Size.Y, new Vector2(1, 1));
            _truckX = truckArea.GetCenter().X;
            Tappable(_truck, OpenTruck);
            _stage.Pin(Sign("여울호", G.IsTruckOpen() ? "손님 " + G.CustomersNow() : "새벽엔 쉼", G.IsTruckOpen(), OpenTruck), new Vector2(truckArea.GetCenter().X - 4, truckArea.End.Y + 15));
            AddVillagers();
            RefreshClients();
            _cat = _stage.Place(Tex("cat/sit-front"), new Vector2(118, 636), CatSize);
            Tappable(_cat, TapCat);
            _yeoul = _stage.Place(Tex("yeoul/front"), new Vector2(205, 636), YeoulSize);
            Tappable(_yeoul, TapYeoul);
            _shadows.Add(_yeoul, 58);
            _shadows.Add(_cat, 44);
            // 간판은 걸어 다니는 사람들보다 늘 위에
            foreach (var sign in _stage.GetChildren())
                if (sign is SignButton sb) sb.MoveToFront();

            // ── 위: HUD 와 오늘 소식 ──
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 18);
            Safe.AddChild(col);
            _hud = new Hud(G);
            col.AddChild(_hud);
            col.AddChild(NewsRibbon());
            if (G.IsWeekly && s.Debt > 0) col.AddChild(GoalsPill());
            _tracker = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddChild(_tracker);
            RefreshTracker();
            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            // ── 아래: 메뉴 ──
            _menu = new PanelContainer();
            col.AddChild(_menu);
            // 아래 판은 짧게: 안내 한 줄과 수첩. 장소는 그림 속 간판을 누르면 여울이 걸어간다
            var bar = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            bar.AddThemeConstantOverride("separation", 22);
            _menu.AddChild(bar);
            var hint = UiKit.Text("간판을 누르면 여울이 걸어가요", TextSize.Small, Palette.GrayText, false, true);
            hint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hint.VerticalAlignment = VerticalAlignment.Center;
            bar.AddChild(hint);
            bar.AddChild(new GameButton("수첩", Palette.Purple, OpenNotebook, "도감 · 미각", TextSize.Body, 110) { CustomMinimumSize = new Vector2(280, 110), SizeFlagsHorizontal = SizeFlags.ShrinkEnd });
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
            for (int i = 0; i < _clients.Count; i++)
            {
                _stage.Move(_clients[i].Node, ClientSpot(i));
                _stage.Move(_clients[i].Mark, ClientSpot(i) + new Vector2(0, -MarkUp));
            }
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

        // ───────── 의뢰 ─────────

        /// <summary>의뢰인을 다시 세운다 (의뢰를 맡거나 건넨 뒤)</summary>
        private void RefreshClients()
        {
            foreach (var c in _clients)
            {
                _stage.Remove(c.Node);
                _stage.Remove(c.Mark);
            }
            _clients.Clear();
            // 한 사람에 의뢰 하나 (예전 세이브에 같은 사람 의뢰가 둘이면 앞의 것만 세운다)
            var reqs = G.Save.Requests
                .Where(r => !string.IsNullOrEmpty(G.Data.RequestTemplates[r.Template].Sd))
                .GroupBy(r => G.Data.RequestTemplates[r.Template].Client)
                .Select(g => g.First()).ToList();
            for (int i = 0; i < reqs.Count && i < 2; i++)
            {
                var req = reqs[i];
                var t = G.Data.RequestTemplates[req.Template];
                var at = ClientSpot(i);
                var node = _stage.Place(Tex(t.Sd + "-stand"), at, ClientSize);
                node.FlipH = at.X > ArtStage.Logical.X / 2;
                _shadows.Add(node, 50);
                int id = req.Id;
                Tappable(node, () => TapClient(id));
                var mark = ClientMark(req);
                Tappable(mark, () => TapClient(id));
                _stage.Pin(mark, at + new Vector2(0, -MarkUp));
                _clients.Add((id, node, mark));
            }
            foreach (var sign in _stage.GetChildren())
                if (sign is SignButton sb) sb.MoveToFront();
        }

        private float _nodTimer = 3, _nodT = -1;
        private int _nodWho;

        /// <summary>의뢰인은 숨 쉬고, 가끔 한 사람이 고개를 까딱한다</summary>
        private void UpdateClients(float dt)
        {
            if (_clients.Count == 0) return;
            _nodTimer -= dt;
            if (_nodTimer <= 0 && _nodT < 0)
            {
                _nodT = 0;
                _nodWho = (int)(GameState.I.Rng.Next() * _clients.Count);
                _nodTimer = 3 + (float)GameState.I.Rng.Next() * 4;
            }
            for (int i = 0; i < _clients.Count; i++)
            {
                var n = _clients[i].Node;
                if (i == _nodWho && _nodT >= 0) SdMotion.Nod(n, _nodT, n.FlipH);
                else SdMotion.Breathe(n, _time, 2 + i * 1.7f, 2.0f);
            }
            if (_nodT >= 0)
            {
                _nodT += dt / 0.5f;
                if (_nodT > 1) _nodT = -1;
            }
        }

        /// <summary>머리 위 표시가 통통 튀는 높이 (논리 좌표가 아니라 화면 px)</summary>
        private const float MarkHop = 26;
        /// <summary>의뢰인 발밑에서 머리 위 표시까지 (논리 좌표)</summary>
        private const float MarkUp = 156;

        /// <summary>
        /// 의뢰인 머리 위 표시. 새 의뢰 ! 와 건넬 수 있는 ✔ 는 크게, 통통 튄다.
        /// 위아래 빈칸의 합이 늘 같아서 표시만 오르내리고 자리는 그대로다
        /// </summary>
        private Control ClientMark(DeliveryRequest req)
        {
            var t = G.Data.RequestTemplates[req.Template];
            bool ready = req.IsAccepted() && G.FishForRequest(req.Id) != null;
            bool loud = !req.IsAccepted() || ready;
            string text = !req.IsAccepted() ? "!" : ready ? "✔" : G.Data.SpeciesById(t.Species).Name + " " + Math.Min(G.RequestFishHave(t.Species), t.Count) + "/" + t.Count;
            var color = !req.IsAccepted() ? Palette.Gold : ready ? Palette.Green : Palette.PaperLight;
            var pill = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(color, loud ? 44 : 30, loud ? 26 : 18, loud ? 2 : 4).WithBorder(Palette.Ink, 4).WithShadow(0.35f, 10, 4));
            pill.AddChild(UiKit.Text(text, loud ? TextSize.Title + 22 : TextSize.Tiny + 2, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center));
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
            box.AddThemeConstantOverride("separation", 0);
            var top = new Control { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, MarkHop) };
            var bottom = new Control { MouseFilter = MouseFilterEnum.Ignore };
            box.AddChild(top);
            box.AddChild(pill);
            box.AddChild(bottom);
            if (!loud) return box;
            Action<float> lift = h =>
            {
                top.CustomMinimumSize = new Vector2(0, MarkHop - h);
                bottom.CustomMinimumSize = new Vector2(0, h);
            };
            var tw = box.CreateTween().SetLoops();
            tw.TweenMethod(Callable.From(lift), 0f, MarkHop, 0.26).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenMethod(Callable.From(lift), MarkHop, 0f, 0.26).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tw.TweenMethod(Callable.From(lift), 0f, MarkHop * 0.3f, 0.12).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenMethod(Callable.From(lift), MarkHop * 0.3f, 0f, 0.12).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tw.TweenInterval(0.55);
            return box;
        }

        private void TapClient(int id)
        {
            var req = G.Save.Requests.FirstOrDefault(r => r.Id == id);
            if (req == null) return;
            var spot = _clients.FirstOrDefault(c => c.Id == id);
            float x = spot.Node != null ? (spot.Node.FlipH ? ClientSpot(1).X - 60 : ClientSpot(0).X + 60) : 205;
            WalkThen(x, () => ClientTalk(id));
        }

        /// <summary>의뢰인과 이야기: 새 의뢰면 듣고 맡기, 모았으면 건네기, 아직이면 무엇이 남았는지</summary>
        private void ClientTalk(int id)
        {
            var req = G.Save.Requests.FirstOrDefault(r => r.Id == id);
            if (req == null) return;
            var t = G.Data.RequestTemplates[req.Template];
            var sp = G.Data.SpeciesById(t.Species);
            int left = req.Deadline - G.Save.Day;
            string due = left <= 0 ? "오늘까지" : left + "일 안에";
            if (!req.IsAccepted())
            {
                // 조건도 의뢰인 줄에 붙인다. 해설 줄로 나누면 의뢰인 그림이 빠져 선택지가 마당 사람을 덮었다
                Talk(new[]
                {
                    ClientLine(t, t.Text + "\n(" + sp.Name + " " + t.Count + "마리 · " + due + " · 보상 " + UiKit.Won(req.Reward) + ")"),
                }, new[]
                {
                    new ModalButton("맡을게요", Palette.Green, () =>
                    {
                        G.AcceptRequest(id);
                        Sound.Play("good", -4);
                        Toast("의뢰를 맡았다: " + sp.Name + " " + t.Count + "마리", Palette.Gold);
                        AfterQuest();
                    }),
                    new ModalButton("다음에", Palette.GrayDark, WalkBack),
                });
                return;
            }
            if (G.FishForRequest(id) != null)
            {
                Talk(new[] { new DialogueLine { Speaker = "yeoul", Face = "default", Text = "부탁하신 " + sp.Name + " " + t.Count + "마리 가져왔어요." } }, new[]
                {
                    new ModalButton("건네기 (" + UiKit.Won(req.Reward) + ")", Palette.Green, () => Deliver(id)),
                    new ModalButton("나중에", Palette.GrayDark, WalkBack),
                });
                return;
            }
            int have = G.RequestFishHave(t.Species);
            Talk(new[] { ClientLine(t, sp.Name + " " + t.Count + "마리, " + due + " 부탁해요.\n(지금 " + have + "마리)") }, null, WalkBack);
        }

        private DialogueLine ClientLine(RequestTemplate t, string text)
        {
            // 노씨는 대화 일러스트가 있다
            if (t.Client == G.Data.Dialogue.SpeakerById("nossi").Name) return new DialogueLine { Speaker = "nossi", Face = "default", Text = text };
            return new DialogueLine { Name = t.Client, Art = t.Sd + "-stand", Text = text };
        }

        private void Deliver(int id)
        {
            var req = G.Save.Requests.FirstOrDefault(r => r.Id == id);
            if (req == null) return;
            var t = G.Data.RequestTemplates[req.Template];
            int reward = G.DeliverRequest(id);
            if (reward <= 0) return;
            _hud.Refresh();
            Sound.Play("coin");
            Haptics.Pattern((40, 0.5f, 50), (70, 0.7f, 0));
            FloatText("+" + UiKit.Won(reward), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 420), Palette.Gold);
            Talk(new[] { ClientLine(t, string.IsNullOrEmpty(t.Thanks) ? "고마워요!" : t.Thanks) }, null, () =>
            {
                AfterQuest();
                // 보상 장비(노씨 부탁 등)가 생겼으면 알린다
                if (G.HasGift()) Toast("노씨가 미끼가게에서 부른다", Palette.Gold);
            });
        }

        private void AfterQuest()
        {
            WalkBack();
            RefreshClients();
            RefreshTracker();
        }

        /// <summary>맡은 의뢰 추적 카드: 마을 소식 아래 한 줄씩 (어종 n/m · 의뢰인 · D-n)</summary>
        private void RefreshTracker()
        {
            foreach (Node c in _tracker.GetChildren()) c.QueueFree();
            foreach (var line in QuestLines(G)) _tracker.AddChild(Riser(TrackerPill(line)));
        }

        /// <summary>맡은 의뢰 한 줄씩 (낚시 화면도 쓴다)</summary>
        public static List<string> QuestLines(CoreGame g)
        {
            var lines = new List<string>();
            foreach (var req in g.Save.Requests.Where(r => r.IsAccepted()))
            {
                var t = g.Data.RequestTemplates[req.Template];
                int left = req.Deadline - g.Save.Day;
                int have = Math.Min(g.RequestFishHave(t.Species), t.Count);
                lines.Add((have >= t.Count ? "✔ " : "") + g.Data.SpeciesById(t.Species).Name + " " + have + "/" + t.Count + " · " + t.Client + " · " + (left <= 0 ? "오늘까지" : "D-" + left));
            }
            return lines;
        }

        /// <summary>
        /// 맡은 의뢰를 한 줄로 (낚시 화면은 바다를 가리지 않게 한 줄만).
        /// 하나면 자세히, 여럿이면 어종과 마릿수만 늘어놓고 가장 급한 기한 하나
        /// </summary>
        public static string QuestSummary(CoreGame g)
        {
            var lines = QuestLines(g);
            if (lines.Count <= 1) return lines.FirstOrDefault();
            var parts = new List<string>();
            int soonest = int.MaxValue;
            foreach (var req in g.Save.Requests.Where(r => r.IsAccepted()))
            {
                var t = g.Data.RequestTemplates[req.Template];
                int have = Math.Min(g.RequestFishHave(t.Species), t.Count);
                parts.Add((have >= t.Count ? "✔" : "") + g.Data.SpeciesById(t.Species).Name + " " + have + "/" + t.Count);
                soonest = Math.Min(soonest, req.Deadline - g.Save.Day);
            }
            return string.Join(" · ", parts) + " · " + (soonest <= 0 ? "오늘까지" : "D-" + soonest);
        }

        public static Control TrackerPill(string text)
        {
            var pill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.62f), 26, 26, 6).WithBorder(new Color(Palette.Gold, 0.6f), 2));
            var label = UiKit.Text(text, TextSize.Tiny + 2, text.StartsWith("✔") ? Palette.GreenText : Palette.Paper, false, false, 0, null, HorizontalAlignment.Center);
            // 길면 한 줄 안에서 줄임표
            label.CustomMinimumSize = new Vector2(Mathf.Min(UiKit.Measure(text, TextSize.Tiny + 2) + 4, 880), 0);
            label.Ellipsis();
            pill.AddChild(label);
            return pill;
        }

        /// <summary>
        /// 마을 사람: 시간대와 날씨만큼(새벽·밤·궂은 날은 적게) 마당을 지나간다. 처음엔 이미 걷고 있고,
        /// 나간 사람이 있으면 잠시 뒤 다른 사람이 화면 가장자리에서 걸어 들어온다
        /// </summary>
        private void AddVillagers()
        {
            _walkerLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _stage.AddChild(_walkerLayer);
            _walkersWanted = Math.Min(G.VillagersWanted(), Lanes.Length);
            var rng = GameState.I.Rng;
            // 처음 자리는 흩어 놓는다. 여울 바로 뒤(가운데)와 양옆 의뢰인 자리는 피한다
            var starts = StartXs.OrderBy(_ => rng.Next()).ToList();
            for (int i = 0; i < _walkersWanted; i++) SpawnWalker(starts[i % starts.Count] + ((float)rng.Next() - 0.5f) * 30);
            _spawnTimer = NextSpawn();
        }

        private float NextSpawn()
        {
            var t = G.Data.Balance.Town;
            return (float)(t.SpawnMin + GameState.I.Rng.Next() * (t.SpawnMax - t.SpawnMin));
        }

        /// <summary>빈 줄에 이 시간대 사람 하나. x 가 없으면 화면 가장자리 밖에서 들어온다</summary>
        private void SpawnWalker(float? x = null)
        {
            var rng = GameState.I.Rng;
            var free = Lanes.Where(l => _walkers.All(w => w.Lane != l)).ToList();
            if (free.Count == 0) return;
            // 의뢰인으로 서 있는 사람과 지금 걷는 사람은 빼고
            var busy = new HashSet<string>(G.Save.Requests.Select(r => G.Data.RequestTemplates[r.Template].Sd ?? ""));
            var pool = G.Data.Dialogue.Villagers
                .Where(v => v.Slots.Contains(G.Save.Slot) && !busy.Contains("villagers/" + v.Id) && _walkers.All(w => w.Def != v))
                .ToList();
            if (pool.Count == 0) return;
            var v = pool[(int)(rng.Next() * pool.Count)];
            float lane = free[(int)(rng.Next() * free.Count)];
            float dir = rng.Next() < 0.5 ? -1 : 1;
            float edge = 40;
            var w = new Walker
            {
                Def = v,
                X = x ?? (dir > 0 ? -edge : ArtStage.Logical.X + edge),
                Lane = lane,
                Dir = dir,
                Frame = (float)rng.Next(),
                Speed = 17 + (float)rng.Next() * 8,
                // 뒤 줄일수록 조금 작게
                Height = VillagerSize * v.Height * (1 + (lane - Lanes[0]) * 0.003f),
            };
            w.Node = _stage.Place(Tex("villagers/" + v.Id + "-walk-1"), new Vector2(w.X, _footY + w.Lane), w.Height);
            w.Node.FlipH = w.Dir < 0;
            var ww = w;
            Tappable(w.Node, () => TapVillager(ww));
            _shadows.Add(w.Node, w.Height * 0.46f);
            _walkers.Add(w);
            // 먼 줄이 뒤로 가게 줄 순서대로 의뢰인·여울 아래에 끼운다
            foreach (var o in _walkers.OrderBy(o => o.Lane))
            {
                int at = _walkerLayer.GetIndex();
                _stage.MoveChild(o.Node, o.Node.GetIndex() < at ? at - 1 : at);
            }
        }

        private void TapVillager(Walker w)
        {
            var lines = w.Def.Lines;
            if (lines.Count == 0) return;
            w.Stop = 2.4f;
            _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-stand"));
            w.Node.FlipH = false;
            Sound.Play("tap", -10);
            Bubble(Art, lines[(int)(GameState.I.Rng.Next() * lines.Count)], _stage.ToLocal(new Vector2(w.X, _footY + w.Lane - w.Height - 6)), 2.2f);
        }

        private void UpdateVillagers(float dt)
        {
            var rng = GameState.I.Rng;
            foreach (var w in _walkers.ToList())
            {
                if (w.Stop > 0)
                {
                    w.Stop -= dt;
                    if (w.Stop <= 0) w.Frame = 0;
                    SdMotion.Breathe(w.Node, _time, w.Lane);
                    _stage.Move(w.Node, new Vector2(w.X, _footY + w.Lane));
                    continue;
                }
                w.X += w.Dir * w.Speed * dt;
                // 반대쪽 가장자리 밖으로 나가면 사라진다
                if (w.X < -50 || w.X > ArtStage.Logical.X + 50)
                {
                    _stage.Remove(w.Node);
                    _walkers.Remove(w);
                    continue;
                }
                w.Frame += dt;
                int f = (int)(w.Frame / 0.24f) % 2;
                _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-walk-" + (f + 1)));
                w.Node.FlipH = w.Dir < 0;
                SdMotion.Walk(w.Node, w.Frame / 0.24f, 0.8f);
                _stage.Move(w.Node, new Vector2(w.X, _footY + w.Lane));
                // 가끔 멈춰 선다. 여울 바로 뒤와 의뢰인 자리에서는 멈추지 않는다
                bool open = (w.X > 95 && w.X < 160) || (w.X > 255 && w.X < 365);
                if (open && rng.Next() < dt * 0.08)
                {
                    w.Stop = 1.5f + (float)rng.Next() * 2;
                    _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-stand"));
                    w.Node.FlipH = false;
                }
            }
            // 모자라면 잠시 뒤 새 사람이 들어온다
            if (_walkers.Count < _walkersWanted)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0)
                {
                    _spawnTimer = NextSpawn();
                    SpawnWalker();
                }
            }
        }

        /// <summary>그림 속 종이 간판: 이름과 (있으면) 아래 작은 상태 한 줄</summary>
        private static SignButton Sign(string name, string state, bool open, Action onPress)
        {
            var b = new SignButton(string.IsNullOrEmpty(state) ? name : name + "\n" + state, onPress);
            if (!string.IsNullOrEmpty(state)) b.AddThemeFontSizeOverride("font_size", TextSize.Small - 4);
            if (!open) b.SetClosed();
            return b;
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
            // 숨 쉬기 (걷는 동안은 걸음 움직임이 맡는다)
            if (!_walking)
            {
                SdMotion.Breathe(_yeoul, _time);
                SdMotion.Breathe(_cat, _time, 1, 2.8f, 0.02f);
            }
            UpdateClients(dt);
            _stage.Zoom = 1.01f + Mathf.Sin(_time * 0.1f) * 0.01f;

            UpdateVillagers(dt);
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
                SwapCat(Tex("cat/" + CatRest[(int)(GameState.I.Rng.Next() * CatRest.Length)]));
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
                Modal(s.Day + "일차 " + G.SlotName(), string.Join("\n", lines), new[] { new ModalButton("좋아", Palette.Green, () =>
                {
                    if (week != null && !week.Seized) PayDay(week, PlayBeat);
                    else PlayBeat();
                }) });
            }
            else PlayBeat();
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
        private void PayDay(WeekResult r, Action then = null)
        {
            if (r.Seized) return;
            var lines = new List<DialogueLine>();
            if (r.Paid)
            {
                lines.Add(new DialogueLine { Speaker = "banker", Face = "stamp", Text = r.No + "주 상환일이라 들렀어요.\n몫 확인했습니다. 도장 찍어 드릴게요." });
                if (r.NextDue > 0)
                    lines.Add(new DialogueLine { Speaker = "banker", Face = "default", Text = "다음 주는 " + UiKit.Won(r.NextDue) + "이에요.\n이자 " + UiKit.Won(r.NextInterest) + "이 들어 있습니다." });
                lines.Add(new DialogueLine { Speaker = "grandma", Face = "default", Text = "애썼다. 밥 먹고 해." });
            }
            else
            {
                lines.Add(new DialogueLine { Speaker = "banker", Face = "sorry", Text = r.No + "주 상환일인데 " + UiKit.Won(r.Short) + "이 모자라네요.\n연체료 " + UiKit.Won(r.LateFee) + "이 붙습니다." });
                lines.Add(new DialogueLine { Speaker = "banker", Face = "worried", Text = "한 번 더 밀리면...\n저도 더는 기다려 드릴 수가 없어요." });
                lines.Add(new DialogueLine { Speaker = "grandma", Face = "default", Text = "괜찮다. 아직 안 끝났어." });
                lines.Add(new DialogueLine { Speaker = "yeoul", Face = "default", Text = "이번 주에 " + UiKit.Won(r.NextDue) + ".\n꼭 낸다." });
            }
            Talk(lines, null, then);
        }

        /// <summary>그날 볼 이야기 장면이 있으면 한 번 보여 준다 (고등어 찌, 노씨 쪽지, 차세라)</summary>
        private void PlayBeat()
        {
            // 사진 모드는 장면마다 따로 찍는다 (--shot-action=<장면 이름>)
            if (GameState.I.ShotMode) return;
            string beat = G.NextBeat();
            if (beat == null) return;
            G.MarkBeat(beat);
            TalkScene(beat);
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
                case "quest":
                    if (G.Save.Requests.Count > 0) G.AcceptRequest(G.Save.Requests[0].Id);
                    RefreshClients();
                    RefreshTracker();
                    break;
                case "questTalk":
                    if (G.Save.Requests.Count > 1) ClientTalk(G.Save.Requests[1].Id);
                    break;
                case "late":
                    PayDay(new WeekResult { No = 1, Paid = false, Short = 120_000, LateFee = 12_000, NextDue = 810_000 });
                    break;
                default:
                    // 이야기 장면 (catFloat, nossiNote, seraMeet ...)
                    if (G.Data.Dialogue.Scenes.ContainsKey(action)) TalkScene(action);
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
            Bubble(Art, line, _stage.ToLocal(new Vector2(205, _footY - YeoulSize - 6)), 2.2f);
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
                        SwapCat(Tex("cat/eat"));
                        _catTimer = 3;
                        Toast("고등어가 골골거린다 (호감 " + G.Save.CatAffinity + "/" + G.Data.Balance.Cat.MaxAffinity + ")");
                    }),
                });
                return;
            }
            var idle = G.Data.Dialogue.CatIdle;
            SwapCat(Tex("cat/meow-1"));
            _catTimer = 1.6f;
            Hop(_cat, 14);
            Bubble(Art, idle[(int)(GameState.I.Rng.Next() * idle.Count)], _stage.ToLocal(new Vector2(118, _footY - CatSize - 4)), 1.3f);
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
            // 걷기 그림은 오른쪽을 본다. 왼쪽으로 갈 때만 뒤집는다
            _yeoul.FlipH = to.X < from.X;
            _cat.FlipH = to.X < from.X;
            var catFrom = new Vector2(118, _footY);
            int lastFrame = -1;
            var tw = CreateTween();
            tw.TweenMethod(Callable.From<float>(t =>
            {
                _stage.Move(_yeoul, from.Lerp(to, t));
                // 고등어는 조금 늦게 따라간다
                _stage.Move(_cat, catFrom.Lerp(to + new Vector2(to.X > from.X ? -60 : 60, 4), Mathf.Max(0, t * 1.1f - 0.1f)));
                int step = (int)(t * dist / 22);
                int frame = step % 2;
                // 발이 땅에 닿을 때마다 발소리
                if (step != lastFrame)
                {
                    lastFrame = step;
                    Sound.Play("step", -8 - (step % 2) * 2);
                }
                _stage.Swap(_yeoul, Tex("yeoul/walk-" + (frame + 1)));
                SwapCat(Tex("cat/walk-" + (2 - frame)));
                SdMotion.Walk(_yeoul, t * dist / 22);
                SdMotion.Walk(_cat, t * dist / 16 + 0.5f, 0.7f);
            }), 0f, 1f, time);
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
            SwapCat(Tex("cat/sit-front"));
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

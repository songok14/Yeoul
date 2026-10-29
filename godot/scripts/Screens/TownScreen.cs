using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 물비늘포 마을 (웹 TownScene). 2026-10-28부터 걷는 언덕 마을: 내려다본 지도(시간대마다 한 장)를 크게 보고
    /// 누른 곳까지 길을 따라 걸으며 카메라가 따라간다(길 그물은 townmap.json, 길 찾기는 코어 TownPath). 간판을 누르면 그곳에 들어간다.
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

        /// <summary>걷는 언덕 마을 길 (코어 TownPath, townmap.json)</summary>
        private TownPath P
        {
            get { return G.Town; }
        }

        private TownMap M
        {
            get { return G.Data.TownMap; }
        }

        private ArtStage _stage;
        private Hud _hud;
        private PanelContainer _menu;
        private TextureRect _yeoul, _cat;
        private FootShadows _shadows;
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private float _time;
        private float _catTimer = 5;
        private float _yeoulReset;
        /// <summary>여울이 걷는 중 (다른 장소 누름은 받지 않는다. 땅을 누르면 길을 바꾼다)</summary>
        private bool _walking;
        /// <summary>여울과 고등어 발밑 (지도 논리 좌표)</summary>
        private Vector2 _yeoulAt, _catAt;
        /// <summary>여울이 따라 걷는 남은 길 점들과 닿으면 할 일</summary>
        private readonly List<Vector2> _route = new List<Vector2>();
        private Action _arrive;
        private float _step;
        /// <summary>카메라 중심 (여울을 부드럽게 따라간다)</summary>
        private Vector2 _camera;
        /// <summary>마지막으로 들어간 곳의 문 앞 점. 마을로 돌아오면 거기서 시작한다</summary>
        private static string _lastNode;
        /// <summary>땅을 누르는 판 (배경 바로 위, 사람·간판 아래)</summary>
        private Control _ground;

        /// <summary>길 위를 오가는 마을 사람 (걷기 그림은 오른쪽을 본다)</summary>
        private sealed class Walker
        {
            public Villager Def;
            public TextureRect Node;
            public Vector2 At;
            public readonly List<Vector2> Route = new List<Vector2>();
            public float Frame, Stop, Speed;
        }

        private readonly List<Walker> _walkers = new List<Walker>();
        private int _walkersWanted;

        /// <summary>원근: 언덕 위일수록 작게 그린 키</summary>
        private float HeightAt(float baseHeight, float y)
        {
            return baseHeight * P.ScaleAt(y);
        }

        /// <summary>고등어 포즈 바꾸기. 포즈 그림마다 높이가 달라(식빵 굽기는 낮다) 같은 몸 크기로 보이게 그림 높이에 비례해 맞춘다</summary>
        private void SwapCat(Texture2D tex)
        {
            _stage.Swap(_cat, tex, HeightAt(M.CatHeight, _catAt.Y) * tex.GetHeight() / 302f, new Vector2(0.5f, 1));
        }

        private void SwapYeoul(Texture2D tex)
        {
            _stage.Swap(_yeoul, tex, HeightAt(M.YeoulHeight, _yeoulAt.Y), new Vector2(0.5f, 1));
        }

        /// <summary>마을에 서 있는 의뢰인과 머리 위 표시 (! 새 의뢰, 볼락 1/2 모으는 중, ✔ 건넬 수 있음)</summary>
        private readonly List<(int Id, TextureRect Node, Control Mark)> _clients = new List<(int, TextureRect, Control)>();

        /// <summary>의뢰인 자리 (townmap.json clientSpots)</summary>
        private Vector2 ClientSpot(int i)
        {
            var p = M.ClientSpots[Math.Min(i, M.ClientSpots.Count - 1)];
            return new Vector2(p.X, p.Y);
        }

        private Control _tracker;
        private int _yeoulLine;

        protected override void Build()
        {
            var s = G.Save;

            // ── 그림: 내려다본 언덕 마을 지도를 크게 보고 여울을 따라간다 ──
            // 지도 꼭대기를 위 띠 아래로 내려 볼 때 지도 위 여백은 짙은 남색 (HUD 와 같은 색)
            var backdrop = new ColorRect { Color = Palette.NightDeep, MouseFilter = MouseFilterEnum.Ignore };
            backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Art.AddChild(backdrop);
            _stage = new ArtStage { ClampPins = false };
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            PaintWeather(s.Slot);
            Art.AddChild(_stage);
            _stage.Zoom = M.Zoom;
            // 땅 누르기: 누른 곳에서 가장 가까운 길 위 점까지 걷는다
            _ground = new Control { MouseFilter = MouseFilterEnum.Stop };
            _ground.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _ground.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) TapGround(_stage.ToLogical(mb.Position));
            };
            _stage.AddChild(_ground);
            // 하늘에 갈매기 (밤과 궂은 날은 빼고)
            string w = G.WeatherToday().Id;
            if (!G.IsNight() && w != "typhoon" && w != "snow")
            {
                var gulls = new Gulls(_stage, 4, G.IsTruckOpen() ? new Color("4a3a40") : new Color("33404d")) { MouseFilter = MouseFilterEnum.Ignore };
                gulls.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _stage.AddChild(gulls);
            }

            // 땅에 선 사람들 발밑 그림자 (모두 이 위에 선다)
            _shadows = new FootShadows(_stage);
            _shadows.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.AddChild(_shadows);

            // 여울과 고등어: 마지막으로 들어갔던 곳 문 앞에서 시작한다
            string start = _lastNode ?? M.Start;
            if (!M.Nodes.Any(n => n.Id == start)) start = M.Start;
            _yeoulAt = PosOf(start);
            _catAt = _yeoulAt + new Vector2(-14, 4);
            _cat = _stage.Place(Tex("cat/sit-front"), _catAt, HeightAt(M.CatHeight, _catAt.Y));
            Tappable(_cat, TapCat);
            _yeoul = _stage.Place(Tex("yeoul/front"), _yeoulAt, HeightAt(M.YeoulHeight, _yeoulAt.Y));
            Tappable(_yeoul, TapYeoul);
            _shadows.Add(_yeoul, 20);
            _shadows.Add(_cat, 14);
            AddVillagers();
            RefreshClients();

            // 간판은 그림 속 건물 위. 지금 상태를 함께 적는다 (닫힌 곳은 바랜 종이)
            foreach (var place in M.Places)
            {
                var pl = place;
                _stage.Pin(PlaceSign(pl), new Vector2(pl.SignX, pl.SignY));
            }
            _camera = _yeoulAt;
            _stage.Focus = CameraTarget();
            SortByDepth();

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
            // 아래 판은 짧게: 안내 한 줄과 수첩. 땅을 누르면 걷고, 간판을 누르면 그곳에 들어간다
            var bar = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            bar.AddThemeConstantOverride("separation", 22);
            _menu.AddChild(bar);
            var hint = UiKit.Text("누른 곳으로 걸어가요\n간판을 누르면 들어가요", TextSize.Small, Palette.GrayText, false, true);
            hint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hint.VerticalAlignment = VerticalAlignment.Center;
            bar.AddChild(hint);
            bar.AddChild(new GameButton("수첩", Palette.Purple, OpenNotebook, "도감 · 미각", TextSize.Body, 110) { CustomMinimumSize = new Vector2(280, 110), SizeFlagsHorizontal = SizeFlags.ShrinkEnd });
            OnSafeAreaChanged();

            // 메뉴가 아래에서 올라오고 HUD 가 위에서 내려온다
            _menu.Modulate = new Color(1, 1, 1, 0);
            _hud.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetParallel(true);
            tw.TweenProperty(_menu, "modulate:a", 1f, 0.45).SetDelay(0.1);
            tw.TweenProperty(_hud, "modulate:a", 1f, 0.45);

            CallDeferred(MethodName.OnEnter);
        }

        /// <summary>장소 간판: 이름과 지금 상태 한 줄, 누르면 그곳까지 걸어가 들어간다</summary>
        private SignButton PlaceSign(TownPlace p)
        {
            switch (p.Id)
            {
                case "market": return Sign(p.Name, G.IsMarketOpen() ? "경매 중" : "경매 마감", true, OpenMarket);
                case "shop": return Sign(p.Name, G.IsShopOpen() ? null : "닫음", G.IsShopOpen(), OpenShop);
                case "grandma": return Sign(p.Name, "상환 · 잠", true, Restaurant);
                case "truck": return Sign(p.Name, TruckSignState(), G.IsTruckOpen(), OpenTruck);
                case "breakwater": return Sign(p.Name + " →", FishSignState(), G.CanFishNow(), GoFishing);
                default: return Sign(p.Name, null, true, () => WalkToPlace(p.Id, () => PlaceLine(p)));
            }
        }

        /// <summary>들어갈 곳이 없는 장소(전망대): 도착하면 여울이 한마디</summary>
        private void PlaceLine(TownPlace p)
        {
            if (string.IsNullOrEmpty(p.Line)) return;
            Bubble(Art, p.Line, _stage.ToLocal(_yeoulAt - new Vector2(0, HeightAt(M.YeoulHeight, _yeoulAt.Y) + 4)), 2.4f);
        }

        /// <summary>카메라가 볼 곳: 여울 조금 위. 위 HUD 가 화면 위를 더 가려서 여울이 화면 가운데보다 조금 아래에 선다</summary>
        private Vector2 CameraTarget()
        {
            return _yeoulAt - new Vector2(0, 20);
        }

        /// <summary>사진 확인용: 여울을 그 점으로 옮기고 카메라도 바로 따라간다</summary>
        private void Teleport(string node)
        {
            _yeoulAt = PosOf(node);
            _catAt = _yeoulAt + new Vector2(-14, 4);
            _stage.Move(_yeoul, _yeoulAt);
            _stage.Move(_cat, _catAt);
            SwapYeoul(Tex("yeoul/front"));
            SwapCat(Tex("cat/sit-front"));
            _camera = CameraTarget();
            _stage.Focus = _camera;
        }

        /// <summary>
        /// 오늘 날씨와 시간대에 맞는 마을 지도. 지도는 시간대마다 한 장이고 날씨는 색과 비·눈·안개로 입힌다.
        /// 날씨 효과는 배경 바로 위, 간판·캐릭터 아래에 둔다
        /// </summary>
        private void PaintWeather(int slot)
        {
            string id = G.WeatherToday().Id;
            bool night = G.IsNight();
            string map = M.Maps[Math.Min(slot, M.Maps.Count - 1)];
            _stage.Background = ArtStage.LoadBackground(map);
            // 꼭대기를 내려 볼 때 그림 위 끝이 잘려 보이지 않게 지도 속 하늘을 이어 붙인 그림 (scripts/town_sky.py)
            string sky = "res://art/backgrounds/" + map + "-sky.webp";
            _stage.SkyTexture = ResourceLoader.Exists(sky) ? GD.Load<Texture2D>(sky) : null;
            _stage.KeepClouds = false;
            switch (id)
            {
                case "rain": _stage.Tint = night ? new Color("aab0cc") : new Color("a4adbf"); break;
                case "typhoon": _stage.Tint = night ? new Color("8a90a8") : new Color("8e98aa"); break;
                case "snow": _stage.Tint = night ? new Color("c8d2ea") : new Color("eef2f8"); break;
                case "fog": _stage.Tint = night ? new Color("b8bccc") : new Color("eceef0"); break;
                default: _stage.Tint = Colors.White; break;
            }
            Control fx = null;
            if (id == "rain" || id == "typhoon") fx = new RainOverlay { Name = "RainOverlay", Intensity = night ? 1 : 1.6f };
            else if (id == "snow") fx = new SnowOverlay { Name = "SnowOverlay" };
            else if (id == "fog") fx = new FogOverlay { Name = "FogOverlay", Center = 0.3f, Strength = 0.35f, Tone = night ? new Color("8890b0") : new Color("f2f2f4") };
            if (fx == null) return;
            fx.MouseFilter = MouseFilterEnum.Ignore;
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

        /// <summary>방파제 간판 아래 줄: 태풍, 저녁 장사 시간, 밤낚시가 열렸는지</summary>
        private string FishSignState()
        {
            if (!G.CanFishToday()) return "출입 금지";
            if (G.IsTruckOpen()) return "저녁엔 장사";
            if (G.IsNight()) return G.NightFishingOpen() ? "밤낚시" : "밤엔 위험";
            return null;
        }

        /// <summary>여울호 간판 아래 줄: 저녁이면 오늘 손님 수, 낮에는 여는 시간</summary>
        private string TruckSignState()
        {
            if (G.IsTruckOpen()) return "손님 " + G.GuestsTonight();
            return G.IsNight() ? "오늘 장사 끝" : "저녁에 열어요";
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

        private Vector2 PosOf(string node)
        {
            var v = P.PosOf(node);
            return new Vector2(v.X, v.Y);
        }

        /// <summary>코어 길 찾기(System.Numerics)를 고닷 좌표로</summary>
        private List<Vector2> RouteTo(Vector2 from, Vector2 to)
        {
            return P.Route(new System.Numerics.Vector2(from.X, from.Y), new System.Numerics.Vector2(to.X, to.Y)).Select(v => new Vector2(v.X, v.Y)).ToList();
        }

        private Texture2D Tex(string path)
        {
            Texture2D t;
            if (!_tex.TryGetValue(path, out t))
            {
                // 최적화 배경은 WebP를 우선 사용하고 기존 PNG도 지원한다.
                string webp = "res://art/" + path + ".webp";
                t = ResourceLoader.Exists(webp) ? GD.Load<Texture2D>(webp) : GD.Load<Texture2D>("res://art/" + path + ".png");
                _tex[path] = t;
            }
            return t;
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
            for (int i = 0; i < reqs.Count && i < M.ClientSpots.Count; i++)
            {
                var req = reqs[i];
                var t = G.Data.RequestTemplates[req.Template];
                var at = ClientSpot(i);
                float h = HeightAt(M.VillagerHeight, at.Y);
                var node = _stage.Place(Tex(t.Sd + "-stand"), at, h);
                // 광장 가운데 쪽을 본다 (그림은 왼쪽을 본다)
                node.FlipH = at.X < PosOf(M.Start).X;
                _shadows.Add(node, 16);
                int id = req.Id;
                Tappable(node, () => TapClient(id));
                var mark = ClientMark(req);
                Tappable(mark, () => TapClient(id));
                _stage.Pin(mark, at - new Vector2(0, h + MarkGap));
                _clients.Add((id, node, mark));
            }
            if (_stage.Focus != null) SortByDepth();
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
        /// <summary>의뢰인 발밑에서 표시 가운데까지 키에 더하는 높이 (논리 좌표). 표시는 가운데를 맞춰 다니 반 높이만큼 더 띄운다</summary>
        private const float MarkGap = 24;

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
            var foot = spot.Node != null ? _stage.FootOf(spot.Node) ?? _yeoulAt : _yeoulAt;
            // 의뢰인 앞(광장 가운데 쪽)에 서서 이야기한다
            float side = spot.Node != null && spot.Node.FlipH ? 22 : -22;
            WalkTo(foot + new Vector2(side, 6), () =>
            {
                _yeoul.FlipH = side > 0;
                ClientTalk(id);
            });
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
            // 오늘 저녁 VIP 예약: 낮 동안 그 고기를 노릴 수 있게 미리 보여 준다
            if (!G.IsNight())
                foreach (var v in G.VipsToday())
                    _tracker.AddChild(Riser(TrackerPill("VIP " + v.Vip.Name + " · " + v.Recipe.Name + " (" + G.Data.Texts.Truck["fresh" + v.Vip.Fresh] + ")")));
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
        /// 마을 사람: 시간대와 날씨만큼(밤·궂은 날은 적게) 길 위를 오간다. 길 점 하나에서 다른 점까지 걷고
        /// 잠시 섰다가 또 걷는다. 의뢰인으로 선 사람은 걷지 않는다
        /// </summary>
        private void AddVillagers()
        {
            _walkersWanted = G.VillagersWanted();
            for (int i = 0; i < _walkersWanted; i++) SpawnWalker();
        }

        /// <summary>길 점 하나 (문 앞과 방파제 끝은 빼고)</summary>
        private Vector2 RandomSpot()
        {
            var doors = new HashSet<string>(M.Places.Select(p => p.Node));
            var nodes = M.Nodes.Where(n => !doors.Contains(n.Id)).ToList();
            var n = nodes[(int)(GameState.I.Rng.Next() * nodes.Count)];
            return new Vector2(n.X, n.Y);
        }

        private void SpawnWalker()
        {
            var rng = GameState.I.Rng;
            // 의뢰인으로 서 있는 사람과 지금 걷는 사람은 빼고
            var busy = new HashSet<string>(G.Save.Requests.Select(r => G.Data.RequestTemplates[r.Template].Sd ?? ""));
            var pool = G.Data.Dialogue.Villagers
                .Where(v => v.Slots.Contains(G.Save.Slot) && !busy.Contains("villagers/" + v.Id) && _walkers.All(w => w.Def != v))
                .ToList();
            if (pool.Count == 0) return;
            var v = pool[(int)(rng.Next() * pool.Count)];
            var w = new Walker
            {
                Def = v,
                At = RandomSpot(),
                Frame = (float)rng.Next(),
                Speed = 0.55f + (float)rng.Next() * 0.2f,
                Stop = (float)rng.Next() * 2,
            };
            w.Node = _stage.Place(Tex("villagers/" + v.Id + "-stand"), w.At, HeightAt(M.VillagerHeight * v.Height, w.At.Y));
            var ww = w;
            Tappable(w.Node, () => TapVillager(ww));
            _shadows.Add(w.Node, 16);
            _walkers.Add(w);
        }

        private void TapVillager(Walker w)
        {
            var lines = w.Def.Lines;
            if (lines.Count == 0) return;
            w.Stop = 2.4f;
            w.Route.Clear();
            _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-stand"));
            Sound.Play("tap", -10);
            float h = HeightAt(M.VillagerHeight * w.Def.Height, w.At.Y);
            Bubble(Art, lines[(int)(GameState.I.Rng.Next() * lines.Count)], _stage.ToLocal(w.At - new Vector2(0, h + 4)), 2.2f);
        }

        private void UpdateVillagers(float dt)
        {
            var rng = GameState.I.Rng;
            foreach (var w in _walkers)
            {
                float h = HeightAt(M.VillagerHeight * w.Def.Height, w.At.Y);
                if (w.Stop > 0)
                {
                    w.Stop -= dt;
                    SdMotion.Breathe(w.Node, _time, w.Speed * 10);
                    if (w.Stop <= 0)
                    {
                        w.Route.Clear();
                        w.Route.AddRange(RouteTo(w.At, RandomSpot()));
                    }
                    continue;
                }
                if (w.Route.Count == 0)
                {
                    // 닿았으면 잠시 선다
                    w.Stop = 1.5f + (float)rng.Next() * 3;
                    _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-stand"), h, new Vector2(0.5f, 1));
                    continue;
                }
                var next = w.Route[0];
                var d = next - w.At;
                float stepLen = M.WalkSpeed * w.Speed * P.ScaleAt(w.At.Y) * dt;
                if (d.Length() <= stepLen)
                {
                    w.At = next;
                    w.Route.RemoveAt(0);
                }
                else w.At += d.Normalized() * stepLen;
                w.Frame += dt;
                int f = (int)(w.Frame / 0.26f) % 2;
                _stage.Swap(w.Node, Tex("villagers/" + w.Def.Id + "-walk-" + (f + 1)), h, new Vector2(0.5f, 1));
                if (Mathf.Abs(d.X) > 0.5f) w.Node.FlipH = d.X < 0;
                SdMotion.Walk(w.Node, w.Frame / 0.26f, 0.8f);
                _stage.Move(w.Node, w.At);
            }
        }

        /// <summary>앞(아래)에 선 사람이 뒤 사람을 가리게: 발밑이 아래일수록 위에 그린다. 간판·의뢰 표시는 늘 위</summary>
        private void SortByDepth()
        {
            var people = new List<(Control Node, float Y)>
            {
                (_yeoul, _yeoulAt.Y),
                (_cat, _catAt.Y - 0.5f),
            };
            foreach (var w in _walkers) people.Add((w.Node, w.At.Y));
            foreach (var c in _clients)
            {
                var f = _stage.FootOf(c.Node);
                if (f != null) people.Add((c.Node, f.Value.Y));
            }
            int at = _shadows.GetIndex() + 1;
            foreach (var p in people.Where(p => IsInstanceValid(p.Node)).OrderBy(p => p.Y))
                _stage.MoveChild(p.Node, at++);
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
            if (_walking) StepYeoul(dt);
            else
            {
                // 숨 쉬기 (걷는 동안은 걸음 움직임이 맡는다)
                SdMotion.Breathe(_yeoul, _time);
                SdMotion.Breathe(_cat, _time, 1, 2.8f, 0.02f);
            }
            UpdateClients(dt);
            UpdateVillagers(dt);
            // 카메라는 여울을 부드럽게 따라간다. 지도 꼭대기(전망대)는 위 HUD 아래까지 내려 보인다
            // 마을 소식·목표·의뢰 띠까지 비켜서 전망대가 가리지 않게
            float top = Mathf.Max(_hud.GetGlobalRect().End.Y, _tracker.GetGlobalRect().End.Y);
            _stage.PadTop = Mathf.Max(0, top - _stage.GetGlobalRect().Position.Y);
            _camera = _camera.Lerp(CameraTarget(), 1 - Mathf.Exp(-dt * 5));
            _stage.Focus = _camera;
            SortByDepth();
            // 여울은 손 흔든 뒤 정면으로
            if (_yeoulReset > 0)
            {
                _yeoulReset -= dt;
                if (_yeoulReset <= 0) SwapYeoul(Tex("yeoul/front"));
            }
            // 고등어는 가끔 자세를 바꾼다
            if (!_walking)
            {
                _catTimer -= dt;
                if (_catTimer <= 0)
                {
                    _catTimer = 4 + (float)GameState.I.Rng.Next() * 5;
                    SwapCat(Tex("cat/" + CatRest[(int)(GameState.I.Rng.Next() * CatRest.Length)]));
                }
            }
        }

        // ───────── 걷기 ─────────

        /// <summary>땅을 눌렀다: 가장 가까운 길 위 점까지 걷는다 (걷는 중이면 길을 바꾼다)</summary>
        private void TapGround(Vector2 at)
        {
            WalkTo(at, null);
            Sound.Play("tick", -14, 1.3f);
        }

        /// <summary>장소 문 앞까지 걸어가서 들어간다. 다음에 마을로 돌아오면 그 문 앞에서 시작한다</summary>
        private void WalkToPlace(string place, Action then)
        {
            var p = M.PlaceById(place);
            WalkTo(PosOf(p.Node), () =>
            {
                _lastNode = p.Node;
                then?.Invoke();
            });
        }

        /// <summary>길을 따라 그 점(가장 가까운 길 위 점)까지 걷고 닿으면 then. 사진 모드는 바로 간다</summary>
        private void WalkTo(Vector2 at, Action then)
        {
            _route.Clear();
            _route.AddRange(RouteTo(_yeoulAt, at));
            _arrive = then;
            if (GameState.I.ShotMode && then != null)
            {
                _yeoulAt = _route[_route.Count - 1];
                _route.Clear();
                _stage.Move(_yeoul, _yeoulAt);
                then();
                return;
            }
            _walking = true;
            _yeoulReset = 0;
        }

        /// <summary>한 프레임 걷기: 길 점을 하나씩 따라가고, 고등어는 조금 뒤에서 따라온다</summary>
        private void StepYeoul(float dt)
        {
            if (_route.Count == 0)
            {
                _walking = false;
                _yeoul.FlipH = false;
                SwapYeoul(Tex("yeoul/front"));
                SwapCat(Tex("cat/sit-front"));
                _catTimer = 3;
                var then = _arrive;
                _arrive = null;
                then?.Invoke();
                return;
            }
            var next = _route[0];
            var d = next - _yeoulAt;
            float k = P.ScaleAt(_yeoulAt.Y);
            float stepLen = M.WalkSpeed * k * dt;
            if (d.Length() <= stepLen)
            {
                _yeoulAt = next;
                _route.RemoveAt(0);
            }
            else _yeoulAt += d.Normalized() * stepLen;
            // 걸음: 옆으로 가면 벌림·모음 두 장(왼쪽이면 뒤집기), 위로 오르면 뒷모습, 내려오면 앞모습 걷기 두 장 (scripts/sd_step.py)
            int before = (int)_step;
            _step += stepLen / (16 * k);
            if ((int)_step != before) Sound.Play("step", -10 - ((int)_step % 2) * 2);
            string pose;
            if (Mathf.Abs(d.Y) > Mathf.Abs(d.X) * 1.4f)
            {
                pose = (d.Y < 0 ? "yeoul/walk-back-" : "yeoul/walk-front-") + ((int)_step % 2 + 1);
                _yeoul.FlipH = false;
            }
            else
            {
                // 옆 걷기는 네 박자: 다리 벌림 → 모음(옆모습) → 벌림 → 모음. 벌린 그림 두 장만 번갈면 다리가 오가지 않아 미끄러져 보였다
                // (walk-2 는 웃는 얼굴이라 섞으면 얼굴이 깜빡인다)
                // side 는 걷기 그림과 달리 왼쪽을 본다. 그대로 쓰면 한 걸음마다 얼굴이 앞뒤로 돌아서 그 장만 반대로 뒤집는다
                bool legsTogether = (int)_step % 2 == 1;
                pose = legsTogether ? "yeoul/side" : "yeoul/walk-1";
                _yeoul.FlipH = legsTogether ? d.X >= 0 : d.X < 0;
            }
            SwapYeoul(Tex(pose));
            SdMotion.Walk(_yeoul, _step);
            _stage.Move(_yeoul, _yeoulAt);

            // 고등어: 여울과 거리가 벌어지면 따라 걷는다
            var toYeoul = _yeoulAt - _catAt;
            float gap = 16 * k;
            if (toYeoul.Length() > gap)
            {
                float catStep = Mathf.Min(toYeoul.Length() - gap, M.WalkSpeed * 1.15f * k * dt);
                _catAt += toYeoul.Normalized() * catStep;
                int f = (int)(_step * 1.3f) % 2;
                SwapCat(Tex("cat/walk-" + (2 - f)));
                if (Mathf.Abs(toYeoul.X) > 0.5f) _cat.FlipH = toYeoul.X < 0;
                SdMotion.Walk(_cat, _step * 1.3f + 0.5f, 0.7f);
                _stage.Move(_cat, _catAt);
            }
        }

        /// <summary>대화만 하고 돌아올 때 (할머니댁, 의뢰인) 정면으로</summary>
        private void WalkBack()
        {
            _yeoul.FlipH = false;
            _cat.FlipH = false;
            SwapYeoul(Tex("yeoul/front"));
            SwapCat(Tex("cat/sit-front"));
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
                case "walk":
                    // 광장에서 할머니댁 쪽으로 걷는 중
                    TapGround(PosOf("mid3"));
                    break;
                case "top":
                    Teleport("lookout");
                    break;
                case "sea":
                    Teleport("breakwater1");
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
            if (_walking) return;
            string pose = YeoulPoses[1 + _yeoulLine % (YeoulPoses.Length - 1)];
            SwapYeoul(Tex("yeoul/" + pose));
            _yeoulReset = 2.2f;
            var lines = G.Data.Dialogue.YeoulIdle;
            string line = lines[_yeoulLine++ % lines.Count];
            Hop(_yeoul, 10);
            Bubble(Art, line, _stage.ToLocal(_yeoulAt - new Vector2(0, HeightAt(M.YeoulHeight, _yeoulAt.Y) + 4)), 2.2f);
        }

        private void TapCat()
        {
            if (_walking) return;
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
            Bubble(Art, idle[(int)(GameState.I.Rng.Next() * idle.Count)], _stage.ToLocal(_catAt - new Vector2(0, HeightAt(M.CatHeight, _catAt.Y) + 4)), 1.3f);
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
            if (G.IsTruckOpen())
            {
                Toast("저녁엔 여울호 장사 시간이다");
                return;
            }
            if (!G.IsFishingTime())
            {
                Toast("밤 방파제는 아직 위험하다");
                return;
            }
            if (G.BaitLeft() <= 0 && !G.IsShopOpen())
            {
                Modal("미끼가 없다", "미끼가게는 문을 닫았다.\n내일 아침에 사 오자.", new[] { new ModalButton("닫기", Palette.GrayDark) });
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
            WalkToPlace("breakwater", () => GameState.I.GoTo("fishing"));
        }

        private void OpenMarket()
        {
            WalkToPlace("market", () => GameState.I.GoTo("market"));
        }

        private void OpenTruck()
        {
            if (!G.IsTruckOpen())
            {
                Toast(G.IsNight() ? "오늘 장사는 끝났다" : "여울호는 저녁에 연다");
                return;
            }
            WalkToPlace("truck", () => GameState.I.GoTo("truck"));
        }

        private void OpenShop()
        {
            if (!G.IsShopOpen())
            {
                Toast("미끼가게는 문을 닫았다");
                return;
            }
            WalkToPlace("shop", () => GameState.I.GoTo("shop"));
        }

        private void OpenNotebook()
        {
            GameState.I.GoTo("notebook");
        }

        /// <summary>할머니댁: 할머니가 한마디 하고, 대화창 위에 갚기·잠자기 선택지가 뜬다</summary>
        private void Restaurant()
        {
            WalkToPlace("grandma", RestaurantTalk);
        }

        private void RestaurantTalk()
        {
            var s = G.Save;
            int canPay = Math.Min(s.Money, s.Debt);
            int step = G.Data.Balance.Chapter1.RepayStep;
            var lines = G.Data.Dialogue.Grandma;
            string line = lines[(int)(GameState.I.Rng.Next() * lines.Count)];
            var choices = new List<ModalButton>
            {
                new ModalButton(G.IsWeekly ? "이번 주 몫 갚기 (" + UiKit.Won(Math.Min(s.Money, G.DueNow())) + ")" : "10만 원 갚기", Palette.GoldDark,
                    () => Pay(G.IsWeekly ? Math.Min(s.Money, G.DueNow()) : step),
                    G.IsWeekly ? G.DueNow() <= 0 || s.Money <= 0 : s.Debt <= 0 || s.Money < step),
                new ModalButton("있는 돈 전부 갚기 (" + UiKit.Won(canPay) + ")", Palette.Red, () => Pay(canPay), canPay <= 0),
            };
            // 저녁 장사 전에는 쉬었다 갈 수 있다 (태풍이거나 미끼가 떨어졌을 때)
            if (CanRest()) choices.Add(new ModalButton("저녁까지 쉬기", Palette.GreenDark, RestUntilEvening));
            choices.Add(new ModalButton("하루 마치기 (잠자기)", Palette.BlueDark, Sleep));
            choices.Add(new ModalButton("나가기", Palette.GrayDark, WalkBack));
            Say("grandma", "default", line, choices);
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

        /// <summary>저녁 장사 전이면 쉴 수 있다 (태풍이나 미끼가 떨어져 낚시를 못 갈 때)</summary>
        private bool CanRest()
        {
            var t = G.Data.Balance.Time;
            return t.TruckSlots.Count > 0 && G.Save.Slot < t.TruckSlots.Min();
        }

        private void RestUntilEvening()
        {
            Modal("저녁까지 쉴까요?", G.SlotName() + "부터 저녁까지 할머니댁에서 쉽니다.\n저녁이 되면 여울호를 열 수 있어요.", new[]
            {
                new ModalButton("아니", Palette.GrayDark),
                new ModalButton("쉬기", Palette.GreenDark, () =>
                {
                    G.RestUntilTruck();
                    GameState.I.GoTo("town");
                }),
            });
        }

        private void Sleep()
        {
            Modal("오늘을 마칠까요?", G.SlotName() + " 이후의 시간이 지나가고\n다음 날 아침이 됩니다.\n쿨러의 생선 신선도가 떨어져요.", new[]
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

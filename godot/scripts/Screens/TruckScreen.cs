using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Systems;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 여울호 푸드트럭 장사 (웹 TruckScene). 오늘의 메뉴 고르기 → 장사 시작 → SD 손님이 걸어와 메뉴에서 주문 → 간 맞추기 게이지 → 손님 반응과 계산 → 다음 손님.
    /// 메뉴 칸과 손님 수는 여울호 등급(평판)과 시간대, 단골로 정한다(코어 Diner). 손님을 다 받거나 메뉴 재료가 떨어지면 장사가 끝나고 시간이 한 칸 지난다.
    /// 손님은 SD(그림은 왼쪽을 보고 있어서 트럭 쪽으로 걸어올 때는 뒤집는다), 반응은 포즈와 말풍선으로 한다.
    /// </summary>
    public partial class TruckScreen : Screen
    {
        protected override string Music
        {
            get { return "truck"; }
        }

        // 배경(노을 포차) 기준 논리 좌표. 손님은 트럭 앞 왼쪽에 선다
        private const float CustomerX = 150;
        private const float CustomerHeight = 150;
        private const float OffLeft = -80;
        /// <summary>장사 중 판 안쪽 높이 (간 맞추기 판이 들어가는 만큼)</summary>
        private const float ShiftPanelHeight = 540;
        // 새벽 · 아침 · 점심 · 저녁 · 밤
        private static readonly Color[] SlotTint = { new Color("b8b8e0"), new Color("fff2e2"), new Color("fff8ee"), Colors.White, new Color("8a90c4") };

        private CoreGame G
        {
            get { return GameState.I.Game; }
        }

        private Dictionary<string, string> T
        {
            get { return G.Data.Texts.Truck; }
        }

        private string F(string key, params object[] args)
        {
            return string.Format(T[key], args);
        }

        private ArtStage _stage;
        private Hud _hud;
        private PanelContainer _panel;
        private VBoxContainer _body;
        private Label _counter;
        private PanelContainer _counterPill;
        private TextureRect _customer;
        private TruckCustomer _who;
        private Control _bubble;
        private SeasonGauge _gauge;
        private float _footY = 600;
        private float _time;
        private int _served, _sales;
        private bool _walking;
        // 사진 모드: 간 맞추기를 저절로 (good 정답, bad 반대쪽 끝)
        private string _auto;
        // 오늘의 메뉴 (등급만큼 고른다)와 이번 장사 손님 차례
        private readonly List<Recipe> _menu = new List<Recipe>();
        private List<string> _plan = new List<string>();
        private Label _chalk;

        protected override void Build()
        {
            var s = G.Save;
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = GD.Load<Texture2D>("res://art/backgrounds/truckstop.png");
            _stage.KeepClouds = true;
            _stage.Tint = SlotTint[Mathf.Clamp(s.Slot, 0, SlotTint.Length - 1)];
            Art.AddChild(_stage);

            // 그림 속 빈 간판과 칠판에 게임 글자를 쓴다
            _stage.Pin(UiKit.Text(T["truckName"], TextSize.H1, new Color("2f4a7a"), true), new Vector2(330, 228));
            _chalk = UiKit.Text(T["boardEmpty"], TextSize.Tiny - 4, new Color("f4f0e6"), false, false, 0, null, HorizontalAlignment.Center);
            _chalk.LabelSettings.LineSpacing = 4;
            _stage.Pin(_chalk, new Vector2(390, 392));
            _menu.AddRange(G.SuggestMenu());

            _customer = _stage.Place(Tex("fisher-walk"), new Vector2(OffLeft, _footY), CustomerHeight);
            _customer.Visible = false;

            // 위: HUD, 제목 줄 / 아래: 장사판
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 0);
            Safe.AddChild(col);
            _hud = new Hud(G);
            col.AddChild(_hud);

            var headPad = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            headPad.AddThemeConstantOverride("margin_left", 36);
            headPad.AddThemeConstantOverride("margin_right", 36);
            headPad.AddThemeConstantOverride("margin_top", 22);
            col.AddChild(headPad);
            var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            head.AddThemeConstantOverride("separation", 14);
            headPad.AddChild(head);
            var title = UiKit.Shadowed(T["title"], TextSize.H1 + 4, Palette.Paper, true, 6, HorizontalAlignment.Left);
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            head.AddChild(title);
            int lv = G.TasteLevel();
            head.AddChild(PlaceScreen.Pill(F("taste", G.Data.TasteStages[lv].Name), new Color(Palette.NightDeep, 0.8f), Palette.Gold));
            _counterPill = PlaceScreen.Pill("", new Color(Palette.NightDeep, 0.8f), Palette.Paper);
            _counter = (Label)_counterPill.GetChild(0);
            _counterPill.Visible = false;
            head.AddChild(_counterPill);

            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
            _panel = new PanelContainer();
            col.AddChild(_panel);
            _body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _body.AddThemeConstantOverride("separation", 20);
            _panel.AddChild(_body);
            // 크기가 바뀐 직후에는 자리가 아직 옛 값이라 한 박자 늦게 잰다
            _panel.Resized += () => CallDeferred(MethodName.PlaceOnFloor);
            OnSafeAreaChanged();

            ShowMenu();
        }

        protected override void OnSafeAreaChanged()
        {
            if (_panel == null) return;
            Safe.AddThemeConstantOverride("margin_top", 0);
            Safe.AddThemeConstantOverride("margin_bottom", 0);
            var hudBox = (StyleBoxFlat)_hud.GetThemeStylebox("panel").Duplicate();
            hudBox.ContentMarginTop = 22 + SafeTop;
            _hud.AddThemeStyleboxOverride("panel", hudBox);
            var sb = UiKit.Box(new Color(Palette.Panel, 0.96f), 0, 44, 34).WithShadow(0.45f, 26, -6);
            sb.CornerRadiusTopLeft = 44;
            sb.CornerRadiusTopRight = 44;
            sb.BorderColor = Palette.Wood;
            sb.BorderWidthTop = 6;
            sb.ContentMarginBottom = 34 + SafeBottom;
            _panel.AddThemeStyleboxOverride("panel", sb);
        }

        /// <summary>손님이 장사판 바로 위 바닥에 서도록. 화면이 길면 그림 속 원래 자리</summary>
        private void PlaceOnFloor()
        {
            float top = _panel.GetGlobalRect().Position.Y - _stage.GetGlobalRect().Position.Y;
            _footY = Mathf.Min(640, _stage.ToLogical(new Vector2(0, top)).Y + 10);
            if (!_walking) _stage.Move(_customer, new Vector2(_customer.Visible ? CustomerX : OffLeft, _footY));
        }

        private static Texture2D Tex(string name)
        {
            return GD.Load<Texture2D>("res://art/customers/" + name + ".png");
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            // 서 있는 손님은 천천히 숨 쉰다
            if (_customer.Visible && !_walking) _customer.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.4f) * 0.012f);
        }

        /// <summary>판을 비운다. 장사 중에는 주문·결과 판 높이를 같게 둬서 손님이 서는 바닥이 흔들리지 않는다</summary>
        private void ClearBody(bool shift = true)
        {
            foreach (var c in _body.GetChildren()) c.QueueFree();
            _body.CustomMinimumSize = new Vector2(0, shift ? ShiftPanelHeight : 0);
        }

        /// <summary>판 안 내용이 바뀔 때 살짝 떠오르며 나타난다</summary>
        private void Rise()
        {
            _body.Modulate = new Color(1, 1, 1, 0);
            var tw = _body.CreateTween();
            tw.TweenProperty(_body, "modulate:a", 1f, 0.22);
        }

        // ───────── 메뉴판 ─────────

        private void ShowMenu(bool rise = true)
        {
            ClearBody(false);
            var s = G.Save;
            int slots = G.Diner.MenuSlots(s);
            _menu.RemoveAll(r => G.Truck.BestFishFor(s, r) == null);
            _body.AddThemeConstantOverride("separation", 10);
            _body.AddChild(UiKit.Text(F("pickMenu", _menu.Count, slots), TextSize.H1, Palette.Gold, true, false, 0, null, HorizontalAlignment.Center));
            var rank = G.Diner.Rank(s);
            var next = G.Diner.NextRank(s);
            string repText = next != null ? F("repTo", s.Diner.Rep, next.Rep) : F("repMax", s.Diner.Rep);
            _body.AddChild(UiKit.Text(F("rankLine", rank.Name, repText, G.CustomersNow()), TextSize.Small, Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center));
            // 재료가 있는 요리 먼저
            var rows = G.Data.Recipes.Recipes
                .OrderByDescending(r => G.Truck.BestFishFor(s, r) != null)
                .ToList();
            foreach (var r in rows) _body.AddChild(MenuRow(r, slots));
            UpdateChalk();
            var buttons = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            buttons.AddThemeConstantOverride("separation", 24);
            buttons.AddChild(new GameButton(T["toTown"], Palette.BlueDark, () => GameState.I.GoTo("town"), null, TextSize.Body, 120));
            bool can = _menu.Count > 0;
            var start = new GameButton(T["start"], Palette.Red, StartShift, can ? F("startSub", G.CustomersNow()) : G.Truck.Cookable(s).Count > 0 ? T["pickFirst"] : T["emptyCooler"], TextSize.Body, 120);
            start.SetEnabled(can);
            buttons.AddChild(start);
            _body.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
            _body.AddChild(buttons);
            if (rise) Rise();
        }

        /// <summary>메뉴판 한 줄. 재료가 있으면 눌러서 오늘 메뉴에 올리고 내린다 (등급만큼)</summary>
        private Control MenuRow(Recipe r, int slots)
        {
            var s = G.Save;
            int n = s.Cooler.Count(f => f.Species == r.Species && G.Fresh.StageOf(f.Fresh) != FreshStage.Spoiled);
            bool known = s.Dex.ContainsKey(r.Species) && s.Dex[r.Species].Count > 0;
            bool on = _menu.Contains(r);
            var box = new PanelContainer { MouseFilter = n > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
            var sb = on ? UiKit.Box(new Color(Palette.Gold, 0.16f), 22, 14, 2).WithBorder(Palette.Gold, 3) : UiKit.Box(new Color(1, 1, 1, 0), 22, 14, 2).WithBorder(new Color(1, 1, 1, 0), 3);
            box.AddThemeStyleboxOverride("panel", sb);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 20);
            box.AddChild(row);
            // 아직 못 낚은 고기의 요리는 그림자로
            var icon = PlaceScreen.DishIcon(r, 110, !known);
            if (known && n == 0) icon.Modulate = new Color(1, 1, 1, 0.35f);
            row.AddChild(icon);
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(UiKit.Text(known ? r.Name : T["unknown"], TextSize.Body, on ? Palette.Gold : n > 0 ? Palette.Paper : Palette.Gray, true));
            string flavors = string.Join(" · ", r.Steps.Select(st => G.Taste.CanTaste(s, st.Flavor) ? G.Data.FlavorById(st.Flavor).Name : T["unknown"]));
            text.AddChild(UiKit.Text(flavors, TextSize.Tiny, Palette.GrayText));
            row.AddChild(text);
            var stock = UiKit.Text(on ? F("onMenu", n) : n > 0 ? F("stock", n) : T["noStock"], TextSize.Small, on ? Palette.Gold : n > 0 ? Palette.GreenText : Palette.Gray);
            stock.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(stock);
            if (n > 0)
            {
                box.MouseDefaultCursorShape = CursorShape.PointingHand;
                box.GuiInput += e =>
                {
                    if (!(e is InputEventMouseButton mb) || mb.ButtonIndex != MouseButton.Left || mb.Pressed) return;
                    if (on) _menu.Remove(r);
                    else if (_menu.Count < slots) _menu.Add(r);
                    else
                    {
                        Toast(F("menuFull", slots));
                        return;
                    }
                    Sound.Play("tick", -6, on ? 0.9f : 1.15f);
                    CallDeferred(MethodName.ShowMenuQuiet);
                };
            }
            return box;
        }

        private void ShowMenuQuiet()
        {
            ShowMenu(false);
        }

        /// <summary>그림 속 칠판에 오늘의 메뉴를 쓴다</summary>
        private void UpdateChalk()
        {
            _chalk.Text = _menu.Count > 0 ? string.Join("\n", _menu.Take(5).Select(r => r.Name)) : T["boardEmpty"];
        }

        // ───────── 손님 ─────────

        private void StartShift()
        {
            StartShift(null);
        }

        /// <summary>first 가 있으면 그 손님이 맨 먼저 온다 (사진 확인용)</summary>
        private void StartShift(string first)
        {
            if (_menu.Count == 0) return;
            _served = 0;
            _sales = 0;
            _plan = G.PlanGuests(GameState.I.Rng);
            if (first != null) _plan.Insert(0, first);
            _body.AddThemeConstantOverride("separation", 20);
            NextCustomer();
        }

        private void NextCustomer()
        {
            // 오늘 메뉴 중 재료가 남은 것
            var menu = _menu.Where(r => G.Truck.BestFishFor(G.Save, r) != null).ToList();
            int max = _plan.Count;
            if (_served >= max || menu.Count == 0)
            {
                EndShift(menu.Count == 0 && _served < max);
                return;
            }
            var rng = GameState.I.Rng;
            string guest = _plan[_served];
            _who = G.Data.Dialogue.Customers.First(c => c.Id == guest);
            var recipe = G.Diner.Order(guest, menu, rng);
            _counter.Text = F("customer", _served + 1, max);
            _counterPill.Visible = true;

            // 판을 비우고 손님이 왼쪽에서 트럭 쪽으로 걸어온다
            ClearBody();
            // 손님마다 키가 다르다 (꼬마는 작게)
            _stage.Swap(_customer, Tex(_who.Id + "-walk"), CustomerHeight * _who.Height, new Vector2(0.5f, 1));
            _customer.FlipH = true;
            _customer.Visible = true;
            Walk(OffLeft, CustomerX, _auto != null ? 0.4 : 0.9, () =>
            {
                _stage.Swap(_customer, Tex(_who.Id + "-stand"));
                TakeOrder(recipe);
            });
        }

        /// <summary>논리 x 를 따라 걷는다. 걸음마다 통통 튀고 몸이 살짝 기운다</summary>
        private int _stepCount;

        private void Walk(float from, float to, double seconds, Action then)
        {
            _walking = true;
            _stepCount = 0;
            var tw = CreateTween();
            tw.TweenMethod(Callable.From<float>(t =>
            {
                float x = Mathf.Lerp(from, to, t);
                float bob = Mathf.Abs(Mathf.Sin(t * Mathf.Pi * 5)) * 7;
                // 발이 땅에 닿을 때마다 사뿐
                int stepNow = (int)(t * 5 + 0.5f);
                if (stepNow != _stepCount)
                {
                    _stepCount = stepNow;
                    Sound.Play("step", -6, 1, 0.12f);
                }
                _stage.Move(_customer, new Vector2(x, _footY - bob));
                _customer.RotationDegrees = Mathf.Sin(t * Mathf.Pi * 5) * 2.5f;
            }), 0f, 1f, seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tw.TweenCallback(Callable.From(() =>
            {
                _walking = false;
                _customer.RotationDegrees = 0;
                _stage.Move(_customer, new Vector2(to, _footY));
                then();
            }));
        }

        private Vector2 OverHead()
        {
            float h = CustomerHeight * (_who != null ? _who.Height : 1);
            return _stage.ToLocal(new Vector2(CustomerX + 18, _footY - h - 4));
        }

        private void SetBubble(string text)
        {
            if (_bubble != null && IsInstanceValid(_bubble)) _bubble.QueueFree();
            _bubble = text == null ? null : Bubble(Art, text, OverHead(), 0);
        }

        private void TakeOrder(Recipe recipe)
        {
            bool known = G.Save.Dex.ContainsKey(recipe.Species) && G.Save.Dex[recipe.Species].Count > 0;
            string dish = known ? recipe.Name : F("unknownDish", G.Data.SpeciesById(recipe.Species).Name);
            // 단골은 늘 하던 말로 시킨다
            bool regular = G.Diner.IsRegular(G.Save, _who.Id);
            SetBubble(string.Format(regular ? G.Data.Diner.GuestById(_who.Id).Regular : _who.Order, dish));
            Cook(recipe, 0, new List<SeasonResult>());
        }

        // ───────── 간 맞추기 ─────────

        private void Cook(Recipe recipe, int step, List<SeasonResult> results)
        {
            if (step >= recipe.Steps.Count)
            {
                Serve(recipe, results);
                return;
            }
            ClearBody();
            var st = recipe.Steps[step];
            if (step == 0) Sound.Play("sizzle", -4);
            var flavor = G.Data.FlavorById(st.Flavor);
            bool visible = G.Taste.CanTaste(G.Save, st.Flavor);
            var rng = GameState.I.Rng;
            double target = G.Truck.RollTarget(st, rng);

            // 요리 그림은 제목 옆에 작게 둔다. 판이 장사판 높이를 넘으면 손님이 판에 가린다
            var head = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            head.AddThemeConstantOverride("separation", 16);
            var dishArt = PlaceScreen.DishIcon(recipe, 120);
            dishArt.CustomMinimumSize = new Vector2(120, 84);
            head.AddChild(dishArt);
            var stepTitle = UiKit.Text(F("step", recipe.Name, step + 1, recipe.Steps.Count), TextSize.H1, Palette.Gold, true);
            stepTitle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            head.AddChild(stepTitle);
            _body.AddChild(head);
            _body.AddChild(UiKit.Text(visible ? T["hint"] : T["hintBlind"], TextSize.Small, Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center));
            _body.AddChild(UiKit.Text(visible ? F("addFlavor", flavor.Tool, flavor.Name) : F("addFlavor", flavor.Tool, T["unknown"]), TextSize.Body, Palette.Paper, true));
            _gauge = new SeasonGauge
            {
                Target = target,
                ZoneHalf = G.Data.Balance.Truck.ZoneHalf,
                ZoneVisible = visible,
                Speed = G.Truck.RollGaugeSpeed(rng),
                LessText = T["less"],
                MoreText = T["more"],
            };
            _body.AddChild(_gauge);
            var gauge = _gauge;
            GameButton put = null;
            put = new GameButton(T["put"], Palette.Red, () =>
            {
                if (!gauge.Running) return;
                put.SetEnabled(false);
                var r = G.Truck.Judge(gauge.Pos, target);
                Color c = r.Quality >= 0.8 ? Palette.Green : r.Quality >= 0.45 ? Palette.Gold : Palette.Red;
                gauge.Stop(c);
                Sound.Play(r.Quality >= 0.8 ? "okay" : "tick", -4, r.Quality >= 0.8 ? 1.2f : 0.8f);
                Haptics.Pulse(20, 0.4f);
                // 아는 맛이면 바로 느낌이 온다
                if (visible) FloatText(r.Quality >= 0.8 ? T["good"] : r.Quality >= 0.45 ? T["near"] : T["bad"], gauge.GetGlobalRect().GetCenter() - new Vector2(0, 90), c);
                var next = new List<SeasonResult>(results) { r };
                GetTree().CreateTimer(_auto != null ? 0.3 : 0.55).Timeout += () => Cook(recipe, step + 1, next);
            }, null, TextSize.H1, 130);
            put.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            put.CustomMinimumSize = new Vector2(520, 130);
            _body.AddChild(put);
            Rise();

            if (_auto != null)
            {
                gauge.Place(_auto == "good" ? target : target > 0.5 ? 0.02 : 0.98);
                GetTree().CreateTimer(0.05).Timeout += () => put.EmitSignal(BaseButton.SignalName.Pressed);
            }
        }

        // ───────── 손님 반응과 계산 ─────────

        private void Serve(Recipe recipe, List<SeasonResult> results)
        {
            var dish = G.ServeGuest(recipe, results, _who.Id);
            if (dish == null)
            {
                NextCustomer();
                return;
            }
            _served += 1;
            _sales += dish.Pay;
            _hud.Refresh();

            int mood = G.Truck.MoodOf(dish.Avg);
            var lines = G.Truck.ReviewLines(G.Save, recipe, results, dish.Stage, mood);
            _stage.Swap(_customer, Tex(_who.Id + (mood == 2 ? "-happy" : mood == 0 ? "-sad" : "-stand")));
            React(mood);
            Sound.Play(mood == 2 ? "good" : mood == 0 ? "bad" : "okay", -2);
            Sound.Play("coin", -3);
            SetBubble(string.Join("\n", lines.Select(l => "“" + l + "”")));
            FloatText("+" + UiKit.Won(dish.Pay), OverHead() - new Vector2(0, 260), Palette.Gold);

            ClearBody();
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 28);
            row.AddChild(PlaceScreen.DishIcon(recipe, 260));
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 6);
            text.AddChild(UiKit.Text(F("done", recipe.Name), TextSize.H1, Palette.Paper, true));
            if (dish.FirstTime) text.AddChild(UiKit.Text(T["firstRecipe"], TextSize.Small, Palette.Gold));
            text.AddChild(UiKit.Text(F("pay", UiKit.Won(dish.Pay)), TextSize.Body, Palette.Gold, true));
            if (dish.Guest != null) text.AddChild(GuestLine(dish.Guest));
            row.AddChild(text);
            _body.AddChild(row);
            _body.AddChild(Stars(dish.Avg));

            bool last = _served >= _plan.Count || !_menu.Any(r => G.Truck.BestFishFor(G.Save, r) != null);
            var next = new GameButton(last ? T["finish"] : T["next"], Palette.Green, Leave, null, TextSize.Body, 120);
            next.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            next.CustomMinimumSize = new Vector2(520, 120);
            _body.AddChild(next);
            Rise();
        }

        /// <summary>손님 친밀도 한 줄. 단골이 되는 순간은 금빛으로</summary>
        private Control GuestLine(GuestResult g)
        {
            var guest = G.Data.Diner.GuestById(_who.Id);
            if (g.BecameRegular)
            {
                Sound.Play("good", -4, 1.2f);
                return UiKit.Text(F("becameRegular", guest.Name + IGa(guest.Name)), TextSize.Small, Palette.Gold, true);
            }
            string key = G.Diner.IsRegular(G.Save, _who.Id) ? "regularLine" : "affinityLine";
            string rep = g.RepGain >= 0 ? "+" + g.RepGain : g.RepGain.ToString();
            return UiKit.Text(F(key, guest.Name, g.Affinity, G.Data.Diner.RegularAt, rep), TextSize.Small, g.RepGain >= 0 ? Palette.GreenText : Palette.RedText);
        }

        /// <summary>받침이 있으면 "이", 없으면 "가"</summary>
        private static string IGa(string word)
        {
            char c = word[word.Length - 1];
            return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0 ? "이" : "가";
        }

        /// <summary>간 맞추기 정확도를 별 다섯 개로</summary>
        private static Control Stars(double avg)
        {
            int n = Mathf.Clamp((int)Math.Round(avg * 5), 0, 5);
            var l = UiKit.Text(new string('★', n) + new string('☆', 5 - n), TextSize.H1, Palette.Gold, false, false, 0, null, HorizontalAlignment.Center);
            return l;
        }

        /// <summary>만족하면 톡톡 뛰고, 실망하면 어깨가 축 처진다</summary>
        private void React(int mood)
        {
            var tw = _customer.CreateTween();
            float y = _customer.Position.Y;
            if (mood == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    tw.TweenProperty(_customer, "position:y", y - 22, 0.12).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                    tw.TweenProperty(_customer, "position:y", y, 0.16).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
                }
            }
            else if (mood == 0)
            {
                tw.TweenProperty(_customer, "position:y", y + 8, 0.35).SetTrans(Tween.TransitionType.Sine);
                tw.TweenProperty(_customer, "position:y", y + 4, 0.4).SetTrans(Tween.TransitionType.Sine);
            }
        }

        /// <summary>손님이 왼쪽으로 걸어 나가고 다음 손님</summary>
        private void Leave()
        {
            if (_walking) return;
            SetBubble(null);
            ClearBody();
            _stage.Swap(_customer, Tex(_who.Id + "-walk"));
            _customer.FlipH = false;
            Walk(CustomerX, OffLeft, 0.7, () =>
            {
                _customer.Visible = false;
                NextCustomer();
            });
        }

        private void EndShift(bool ranOut)
        {
            _counterPill.Visible = false;
            _customer.Visible = false;
            SetBubble(null);
            G.UseSlot();
            _hud.Refresh();
            string body = F("endBody", _served, UiKit.Won(_sales));
            var up = G.CheckDinerRankUp();
            if (up != null)
            {
                body += "\n\n" + F("rankUp", up.Name, up.Menu, up.Guests);
                Sound.Play("good", -2);
            }
            Modal(up != null ? T["rankUpTitle"] : ranOut ? T["ranOut"] : T["endTitle"], body, new[]
            {
                new ModalButton(T["toTown"], Palette.Blue, () => GameState.I.GoTo("town")),
            });
        }

        // ───────── CI 사진 ─────────

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "cook":
                    StartShift();
                    break;
                case "good":
                case "bad":
                    _auto = action;
                    StartShift();
                    break;
                case "rankup":
                    // 평판이 다음 등급에 닿은 채로 장사를 마친다
                    G.Save.Diner.Rep = G.Data.Diner.Ranks[1].Rep;
                    _served = 4;
                    _sales = 48_600;
                    EndShift(false);
                    break;
                case "regular":
                    // 이 시간대 손님이 모두 단골이면 늘 하던 말로 시킨다
                    foreach (var g in G.Data.Diner.Guests) G.Save.Diner.Affinity[g.Id] = G.Data.Diner.RegularAt;
                    _auto = "good";
                    StartShift();
                    break;
                case "boy":
                    // 꼬마 손님은 어른보다 작게 선다
                    StartShift("boy");
                    break;
                case "end":
                    _served = 5;
                    _sales = 61_200;
                    EndShift(false);
                    break;
            }
        }
    }
}

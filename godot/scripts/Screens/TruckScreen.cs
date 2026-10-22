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
    /// 여울호 푸드트럭 장사 (웹 TruckScene). 메뉴판 → 장사 시작 → SD 손님이 걸어와 주문 → 간 맞추기 게이지 → 손님 반응과 계산 → 다음 손님.
    /// 손님 다섯 명을 받으면(재료가 떨어져도) 장사가 끝나고 시간이 한 칸 지난다.
    /// 손님은 SD(그림은 왼쪽을 보고 있어서 트럭 쪽으로 걸어올 때는 뒤집는다), 반응은 포즈와 말풍선으로 한다.
    /// </summary>
    public partial class TruckScreen : Screen
    {
        // 배경(노을 포차) 기준 논리 좌표. 손님은 트럭 앞 왼쪽에 선다
        private const float CustomerX = 150;
        private const float CustomerHeight = 150;
        private const float OffLeft = -80;
        /// <summary>장사 중 판 안쪽 높이 (간 맞추기 판이 들어가는 만큼)</summary>
        private const float ShiftPanelHeight = 540;
        private static readonly Color[] SlotTint = { new Color("b8b8e0"), new Color("fff2e2"), new Color("fff8ee"), Colors.White };

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

        protected override void Build()
        {
            var s = G.Save;
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = GD.Load<Texture2D>("res://art/backgrounds/truckstop.png");
            _stage.Tint = SlotTint[Mathf.Clamp(s.Slot, 0, 3)];
            Art.AddChild(_stage);

            // 그림 속 빈 간판과 칠판에 게임 글자를 쓴다
            _stage.Pin(UiKit.Text(T["truckName"], TextSize.H1, new Color("2f4a7a"), true), new Vector2(330, 228));
            var board = G.Truck.Cookable(s).Take(4).Select(r => r.Name).ToList();
            var chalk = UiKit.Text(board.Count > 0 ? string.Join("\n", board) : T["boardEmpty"], TextSize.Tiny - 4, new Color("f4f0e6"), false, false, 0, null, HorizontalAlignment.Center);
            chalk.LabelSettings.LineSpacing = 4;
            _stage.Pin(chalk, new Vector2(390, 392));

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

        private void ShowMenu()
        {
            ClearBody(false);
            var s = G.Save;
            _body.AddChild(UiKit.Text(T["menu"], TextSize.H1, Palette.Gold, true, false, 0, null, HorizontalAlignment.Center));
            foreach (var r in G.Data.Recipes.Recipes)
            {
                int n = s.Cooler.Count(f => f.Species == r.Species && G.Fresh.StageOf(f.Fresh) != FreshStage.Spoiled);
                bool known = s.Dex.ContainsKey(r.Species) && s.Dex[r.Species].Count > 0;
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 20);
                var icon = PlaceScreen.FishIcon(r.Species, 120);
                if (n == 0) icon.Modulate = new Color(1, 1, 1, 0.35f);
                row.AddChild(icon);
                var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 0);
                text.AddChild(UiKit.Text(known ? r.Name : T["unknown"], TextSize.Body, n > 0 ? Palette.Paper : Palette.Gray, true));
                string flavors = string.Join(" · ", r.Steps.Select(st => G.Taste.CanTaste(s, st.Flavor) ? G.Data.FlavorById(st.Flavor).Name : T["unknown"]));
                text.AddChild(UiKit.Text(flavors, TextSize.Tiny, Palette.GrayText));
                row.AddChild(text);
                var stock = UiKit.Text(n > 0 ? F("stock", n) : T["noStock"], TextSize.Small, n > 0 ? Palette.GreenText : Palette.Gray);
                stock.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                row.AddChild(stock);
                _body.AddChild(row);
            }
            bool can = G.Truck.Cookable(s).Count > 0;
            var buttons = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            buttons.AddThemeConstantOverride("separation", 24);
            buttons.AddChild(new GameButton(T["toTown"], Palette.BlueDark, () => GameState.I.GoTo("town"), null, TextSize.Body, 120));
            var start = new GameButton(T["start"], Palette.Red, StartShift, can ? T["startSub"] : T["emptyCooler"], TextSize.Body, 120);
            start.SetEnabled(can);
            buttons.AddChild(start);
            _body.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
            _body.AddChild(buttons);
            Rise();
        }

        // ───────── 손님 ─────────

        private void StartShift()
        {
            _served = 0;
            _sales = 0;
            NextCustomer();
        }

        private void NextCustomer()
        {
            var menu = G.Truck.Cookable(G.Save);
            int max = G.Data.Balance.Truck.CustomersPerShift;
            if (_served >= max || menu.Count == 0)
            {
                EndShift(menu.Count == 0 && _served < max);
                return;
            }
            var rng = GameState.I.Rng;
            var recipe = menu[(int)(rng.Next() * menu.Count)];
            var all = G.Data.Dialogue.Customers;
            var pick = all.Where(c => _who == null || c.Id != _who.Id).ToList();
            _who = pick[(int)(rng.Next() * pick.Count)];
            _counter.Text = F("customer", _served + 1, max);
            _counterPill.Visible = true;

            // 판을 비우고 손님이 왼쪽에서 트럭 쪽으로 걸어온다
            ClearBody();
            _stage.Swap(_customer, Tex(_who.Id + "-walk"));
            _customer.FlipH = true;
            _customer.Visible = true;
            Walk(OffLeft, CustomerX, _auto != null ? 0.4 : 0.9, () =>
            {
                _stage.Swap(_customer, Tex(_who.Id + "-stand"));
                TakeOrder(recipe);
            });
        }

        /// <summary>논리 x 를 따라 걷는다. 걸음마다 통통 튀고 몸이 살짝 기운다</summary>
        private void Walk(float from, float to, double seconds, Action then)
        {
            _walking = true;
            var tw = CreateTween();
            tw.TweenMethod(Callable.From<float>(t =>
            {
                float x = Mathf.Lerp(from, to, t);
                float bob = Mathf.Abs(Mathf.Sin(t * Mathf.Pi * 5)) * 7;
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
            return _stage.ToLocal(new Vector2(CustomerX + 18, _footY - CustomerHeight - 4));
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
            SetBubble(string.Format(_who.Order, dish));
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
            var flavor = G.Data.FlavorById(st.Flavor);
            bool visible = G.Taste.CanTaste(G.Save, st.Flavor);
            var rng = GameState.I.Rng;
            double target = G.Truck.RollTarget(st, rng);

            _body.AddChild(UiKit.Text(F("step", recipe.Name, step + 1, recipe.Steps.Count), TextSize.H1, Palette.Gold, true, false, 0, null, HorizontalAlignment.Center));
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
            var dish = G.ServeDish(recipe, results);
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
            SetBubble(string.Join("\n", lines.Select(l => "“" + l + "”")));
            FloatText("+" + UiKit.Won(dish.Pay), OverHead() - new Vector2(0, 260), Palette.Gold);

            ClearBody();
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 28);
            row.AddChild(PlaceScreen.FishIcon(recipe.Species, 240));
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 6);
            text.AddChild(UiKit.Text(F("done", recipe.Name), TextSize.H1, Palette.Paper, true));
            if (dish.FirstTime) text.AddChild(UiKit.Text(T["firstRecipe"], TextSize.Small, Palette.Gold));
            text.AddChild(UiKit.Text(F("pay", UiKit.Won(dish.Pay)), TextSize.Body, Palette.Gold, true));
            row.AddChild(text);
            _body.AddChild(row);
            _body.AddChild(Stars(dish.Avg));

            bool last = _served >= G.Data.Balance.Truck.CustomersPerShift || G.Truck.Cookable(G.Save).Count == 0;
            var next = new GameButton(last ? T["finish"] : T["next"], Palette.Green, Leave, null, TextSize.Body, 120);
            next.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            next.CustomMinimumSize = new Vector2(520, 120);
            _body.AddChild(next);
            Rise();
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
            Modal(ranOut ? T["ranOut"] : T["endTitle"], F("endBody", _served, UiKit.Won(_sales)), new[]
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
                case "end":
                    _served = 5;
                    _sales = 61_200;
                    EndShift(false);
                    break;
            }
        }
    }
}

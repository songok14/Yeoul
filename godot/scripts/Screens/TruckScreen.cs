using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Systems;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 여울호 푸드트럭 장사 (웹 TruckScene). 오늘의 메뉴 고르기 → 장사 시작 → SD 손님이 걸어와 메뉴에서 주문 → 간 맞추기 게이지 → 손님 반응과 계산 → 다음 손님.
    /// 장사는 저녁에만 한다. 메뉴 칸과 손님 수는 평판(명성)으로, 손님 등급은 친밀도와 예약으로 정한다(코어 Diner).
    /// VIP 는 예약한 요리를 메뉴와 상관없이 시키고, 특별 손님(이야기 인물)은 대화창 일러스트로 나온다.
    /// 손님을 다 받거나 메뉴 재료가 떨어지면 장사가 끝나고 시간이 한 칸 지난다.
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
        // 아침 · 점심 · 저녁 · 밤 (장사는 저녁에만 하지만 사진 확인용으로 모두 둔다)
        private static readonly Color[] SlotTint = { new Color("fff2e2"), new Color("fff8ee"), Colors.White, new Color("8a90c4") };

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
        /// <summary>지금 손님 (일반·단골·VIP·특별 손님)</summary>
        private PlannedGuest _guest;
        /// <summary>VIP 머리 위 금빛 표</summary>
        private Control _badge;
        private Control _bubble;
        private SeasonGauge _gauge;
        private float _footY = 600;
        private float _time;
        private int _served, _sales;
        private bool _walking;
        // 사진 모드: 간 맞추기를 저절로 (good 정답, bad 반대쪽 끝)
        private string _auto;
        // 오늘의 메뉴판 (등급만큼 칸, 칸마다 요리와 접시 수)과 이번 장사 손님 차례
        private readonly List<MenuEntry> _menu = new List<MenuEntry>();
        private List<PlannedGuest> _plan = new List<PlannedGuest>();
        private VBoxContainer _chalk;

        protected override void Build()
        {
            var s = G.Save;
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = GD.Load<Texture2D>("res://art/backgrounds/truckstop.png");
            _stage.KeepClouds = true;
            _stage.Tint = SlotTint[Mathf.Clamp(s.Slot, 0, SlotTint.Length - 1)];
            Art.AddChild(_stage);

            // 간판 "여울호"는 그림에 원근대로 칠해 두었다 (2026-10-27). 칠판에는 분필로 오늘의 메뉴를 쓴다
            _chalk = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(150, 0) };
            _chalk.AddThemeConstantOverride("separation", 2);
            _stage.Pin(_chalk, new Vector2(387, 384));
            _menu.AddRange(G.SuggestMenuPlan());

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

        /// <summary>
        /// 메뉴판: 등급만큼 칸이 있고, 칸을 눌러 요리와 접시 수를 정한다.
        /// 한 고기는 한 접시만 되니 같은 어종을 쓰는 요리끼리 남은 고기를 나눠 잡는다 (2026-10-29 사용자 요청)
        /// </summary>
        private void ShowMenu(bool rise = true)
        {
            ClearBody(false);
            var s = G.Save;
            int slots = G.Diner.MenuSlots(s);
            G.FitMenu(_menu);
            _body.AddThemeConstantOverride("separation", 12);
            _body.AddChild(UiKit.Text(F("pickMenu", _menu.Count, slots), TextSize.H1, Palette.Gold, true, false, 0, null, HorizontalAlignment.Center));
            var rank = G.Diner.Rank(s);
            var next = G.Diner.NextRank(s);
            string repText = next != null ? F("repTo", s.Diner.Rep, next.Rep) : F("repMax", s.Diner.Rep);
            _body.AddChild(UiKit.Text(F("rankLine", rank.Name, repText, G.CustomersNow()), TextSize.Small, Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center));
            // 오늘 저녁 VIP 예약과 특별 손님 (메뉴에 없어도 VIP 는 그 요리를 시킨다)
            foreach (var v in G.VipsToday())
                _body.AddChild(UiKit.Text(F("vipBooked", v.Vip.Name, v.Recipe.Name, T["fresh" + v.Vip.Fresh]), TextSize.Small, Palette.Gold, true, true, 0, null, HorizontalAlignment.Center));
            var special = G.SpecialToday();
            if (special != null)
                _body.AddChild(UiKit.Text(F("specialComing", special.Name + IGa(special.Name)), TextSize.Small, Palette.Gold, true, true, 0, null, HorizontalAlignment.Center));
            bool any = G.Truck.Cookable(s).Count > 0;
            if (!any) _body.AddChild(UiKit.Text(T["noReady"], TextSize.Body, Palette.GrayText, false, true, 0, null, HorizontalAlignment.Center));
            // 칸: 채운 칸 먼저, 빈 칸은 점선 상자
            for (int i = 0; i < slots; i++)
                _body.AddChild(i < _menu.Count ? FilledSlot(_menu[i]) : EmptySlot(any));
            UpdateChalk();
            var buttons = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            buttons.AddThemeConstantOverride("separation", 24);
            buttons.AddChild(new GameButton(T["toTown"], Palette.BlueDark, () => GameState.I.GoTo("town"), null, TextSize.Body, 120));
            bool can = _menu.Count > 0;
            GameButton start;
            if (!any)
            {
                // 요리할 고기가 없으면 장사를 쉬고 밤으로 넘어간다
                start = new GameButton(T["skip"], Palette.GrayDark, SkipShift, T["skipSub"], TextSize.Body, 120);
            }
            else
            {
                start = new GameButton(T["start"], Palette.Red, StartShift, can ? F("startSub", G.GuestsTonight(), _menu.Sum(e => e.Left)) : T["pickFirst"], TextSize.Body, 120);
                start.SetEnabled(can);
            }
            buttons.AddChild(start);
            _body.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
            _body.AddChild(buttons);
            if (rise) Rise();
        }

        /// <summary>누를 수 있는 메뉴판 칸 상자</summary>
        private PanelContainer SlotBox(bool filled, Action onTap)
        {
            var box = new PanelContainer { MouseFilter = onTap != null ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
            var sb = filled
                ? UiKit.Box(new Color(Palette.Gold, 0.14f), 22, 18, 6).WithBorder(Palette.Gold, 3)
                : UiKit.Box(new Color(1, 1, 1, 0.04f), 22, 18, 14).WithBorder(new Color(Palette.Paper, 0.28f), 3);
            box.AddThemeStyleboxOverride("panel", sb);
            if (onTap == null) return box;
            box.MouseDefaultCursorShape = CursorShape.PointingHand;
            box.GuiInput += e =>
            {
                if (!(e is InputEventMouseButton mb) || mb.ButtonIndex != MouseButton.Left || mb.Pressed) return;
                Sound.Play("tick", -6, 1.1f);
                onTap();
            };
            return box;
        }

        /// <summary>요리가 올라간 칸: 요리 그림 · 이름과 맛 · 접시 수. 누르면 접시 수를 바꾸거나 내린다</summary>
        private Control FilledSlot(MenuEntry e)
        {
            var box = SlotBox(true, () => AskCount(e.Recipe, e));
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 20);
            box.AddChild(row);
            row.AddChild(PlaceScreen.DishIcon(e.Recipe, 110));
            row.AddChild(DishText(e.Recipe, Palette.Gold));
            var count = UiKit.Text(F("slotCount", e.Left), TextSize.H1, Palette.Gold, true, false, 0, null, HorizontalAlignment.Right);
            count.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(count);
            return box;
        }

        /// <summary>빈 칸. 누르면 요리를 고른다</summary>
        private Control EmptySlot(bool any)
        {
            var box = SlotBox(false, any ? PickDish : (Action)null);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, 62), Alignment = BoxContainer.AlignmentMode.Center };
            box.AddChild(col);
            col.AddChild(UiKit.Text(any ? T["slotEmpty"] : T["slotNone"], TextSize.Body, any ? Palette.Paper : Palette.Gray, true, false, 0, null, HorizontalAlignment.Center));
            return box;
        }

        /// <summary>요리 이름과 맛 두 줄</summary>
        private Control DishText(Recipe r, Color nameColor)
        {
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(UiKit.Text(r.Name, TextSize.Body, nameColor, true));
            string flavors = string.Join(" · ", r.Steps.Select(st => G.Taste.CanTaste(G.Save, st.Flavor) ? G.Data.FlavorById(st.Flavor).Name : T["unknown"]));
            text.AddChild(UiKit.Text(flavors, TextSize.Tiny, Palette.GrayText));
            return text;
        }

        /// <summary>빈 칸에 올릴 요리 고르기. 재료를 다른 칸이 다 잡은 요리는 흐리게</summary>
        private void PickDish()
        {
            var s = G.Save;
            // 아직 메뉴에 없는 요리 중 재료가 있는 것. 특별 요리 먼저, 생선구이는 아래로
            var ready = G.Truck.Cookable(s)
                .Where(r => !_menu.Any(e => e.Recipe == r))
                .OrderBy(r => IsGrill(r))
                .ThenByDescending(r => G.MenuFree(_menu, r))
                .ThenByDescending(r => r.Price)
                .ToList();
            Control root = null;
            var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
            list.AddThemeConstantOverride("separation", 10);
            foreach (var r in ready)
            {
                var recipe = r;
                int free = G.MenuFree(_menu, r);
                var item = new PanelContainer { MouseFilter = MouseFilterEnum.Pass };
                item.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.PaperDark, free > 0 ? 0.5f : 0.2f), 22, 16, 6));
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 18);
                item.AddChild(row);
                var icon = PlaceScreen.DishIcon(r, 100);
                if (free <= 0) icon.Modulate = new Color(1, 1, 1, 0.35f);
                row.AddChild(icon);
                var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 0);
                text.AddChild(UiKit.Text(r.Name, TextSize.Body, free > 0 ? Palette.Ink : Palette.InkSoft, true));
                text.AddChild(UiKit.Text(free > 0 ? F("dishFree", free) : T["dishTaken"], TextSize.Tiny + 2, free > 0 ? Palette.GreenDark : Palette.InkSoft, false, true));
                row.AddChild(text);
                if (free > 0)
                    PlaceScreen.OnTap(item, () =>
                    {
                        Sound.Play("tick", -6, 1.15f);
                        CloseModal(root);
                        AskCount(recipe, null);
                    });
                list.AddChild(item);
            }
            Control extra = list;
            if (ready.Count == 0) extra = UiKit.Text(T["pickNone"], TextSize.Body, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center);
            else
            {
                // 요리가 많으면 손가락으로 밀어 본다
                var scroll = new ScrollContainer
                {
                    HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                    VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
                    ScrollDeadzone = 24,
                    MouseFilter = MouseFilterEnum.Pass,
                    CustomMinimumSize = new Vector2(0, Mathf.Min(ready.Count * 132, GetViewportRect().Size.Y * 0.5f)),
                };
                scroll.AddChild(list);
                extra = scroll;
            }
            root = Modal(T["pickDish"], T["pickDishSub"], new[] { new ModalButton(T["cancel"], Palette.GrayDark) }, extra);
        }

        /// <summary>
        /// 접시 수 정하기 (− 몇 접시 +). 남은 재료만큼까지. entry 가 있으면 이미 올린 칸을 고친다 (내리기, 다른 요리로)
        /// </summary>
        private void AskCount(Recipe recipe, MenuEntry entry)
        {
            int max = G.MenuFree(_menu, recipe, entry);
            int n = entry != null ? Mathf.Clamp(entry.Left, 1, Mathf.Max(1, max)) : max;
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 14);
            var icon = PlaceScreen.DishIcon(recipe, 220);
            icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            col.AddChild(icon);
            var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 34);
            col.AddChild(row);
            var count = UiKit.Text(F("slotCount", n), TextSize.Title, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center);
            count.CustomMinimumSize = new Vector2(220, 0);
            count.VerticalAlignment = VerticalAlignment.Center;
            GameButton minus = null, plus = null;
            void Set(int v)
            {
                n = Mathf.Clamp(v, 1, Mathf.Max(1, max));
                count.Text = F("slotCount", n);
                minus.SetEnabled(n > 1);
                plus.SetEnabled(n < max);
            }
            minus = PlaceScreen.SmallButton("−", Palette.GrayDark, () => { Sound.Play("tick", -6, 0.9f); Set(n - 1); }, 130, 110);
            plus = PlaceScreen.SmallButton("+", Palette.GrayDark, () => { Sound.Play("tick", -6, 1.15f); Set(n + 1); }, 130, 110);
            row.AddChild(minus);
            row.AddChild(count);
            row.AddChild(plus);
            col.AddChild(UiKit.Text(F("countSub", G.Data.SpeciesById(recipe.Species).Name, max), TextSize.Small, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center));
            Set(n);

            void Apply()
            {
                if (max <= 0) return;
                if (entry != null) entry.Count = entry.Served + n;
                else _menu.Add(new MenuEntry { Recipe = recipe, Count = n });
                Sound.Play("tick", -6, 1.2f);
                ShowMenu(false);
            }
            var buttons = new List<ModalButton>();
            if (entry != null)
            {
                buttons.Add(new ModalButton(T["menuSet"], Palette.Green, Apply));
                buttons.Add(new ModalButton(T["menuOther"], Palette.Blue, () =>
                {
                    _menu.Remove(entry);
                    ShowMenu(false);
                    PickDish();
                }));
                buttons.Add(new ModalButton(T["menuRemove"], Palette.Red, () =>
                {
                    _menu.Remove(entry);
                    Sound.Play("tick", -6, 0.9f);
                    ShowMenu(false);
                }));
            }
            else
            {
                buttons.Add(new ModalButton(T["cancel"], Palette.GrayDark));
                buttons.Add(new ModalButton(T["menuAdd"], Palette.Green, Apply, max <= 0));
            }
            Modal(recipe.Name, null, buttons, col);
        }

        /// <summary>생선구이는 고르기 목록 아래로</summary>
        private static bool IsGrill(Recipe r)
        {
            return r.Id.EndsWith("-gui");
        }

        /// <summary>그림 속 칠판에 오늘의 메뉴를 쓴다</summary>
        private void UpdateChalk()
        {
            foreach (Node c in _chalk.GetChildren()) c.QueueFree();
            var chalk = new Color(0.96f, 0.95f, 0.9f, 0.92f);
            var title = UiKit.Text(T["boardTitle"], TextSize.Tiny - 6, new Color(0.98f, 0.88f, 0.55f, 0.95f), true, false, 0, null, HorizontalAlignment.Center);
            _chalk.AddChild(title);
            // 분필로 그은 짧은 밑줄
            _chalk.AddChild(new ColorRect { Color = new Color(chalk, 0.45f), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });
            _chalk.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
            if (_menu.Count == 0)
                _chalk.AddChild(UiKit.Text(T["boardEmpty"], TextSize.Tiny - 7, chalk, false, false, 0, null, HorizontalAlignment.Center));
            foreach (var e in _menu.Take(5))
                _chalk.AddChild(UiKit.Text(F("boardDish", e.Recipe.Name, e.Left), TextSize.Tiny - 7, chalk, false, false, 0, null, HorizontalAlignment.Center));
        }

        // ───────── 손님 ─────────

        private void StartShift()
        {
            StartShift(null);
        }

        /// <summary>first 가 있으면 그 손님이 맨 먼저 온다 (사진 확인용)</summary>
        private void StartShift(PlannedGuest first)
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
            // 오늘 메뉴 중 접시와 재료가 남은 것
            var menu = G.MenuOpen(_menu);
            int max = _plan.Count;
            if (_served >= max || menu.Count == 0)
            {
                EndShift(menu.Count == 0 && _served < max);
                return;
            }
            var rng = GameState.I.Rng;
            _guest = _plan[_served];
            var recipe = G.Diner.Order(_guest, menu, rng, G.Data.RecipeById);
            _counter.Text = F("customer", _served + 1, max);
            _counterPill.Visible = true;

            // 판을 비우고 손님이 왼쪽에서 트럭 쪽으로 걸어온다. 특별 손님은 대화창 일러스트로 온다
            ClearBody();
            if (_guest.Kind == GuestKind.Special)
            {
                _who = null;
                var sp = G.Diner.SpecialById(_guest.Id);
                Sound.Play("good", -6, 0.8f);
                Talk(new[] { new DialogueLine { Speaker = sp.Speaker, Face = sp.Face, Text = sp.Order } }, null, () => Cook(recipe, 0, new List<SeasonResult>()));
                return;
            }
            // VIP 는 아직 그림이 없어 손님 그림을 빌려 쓴다
            string art = _guest.Kind == GuestKind.Vip ? G.Diner.VipById(_guest.Id).Art : _guest.Id;
            _who = G.Data.Dialogue.Customers.First(c => c.Id == art);
            // 손님마다 키가 다르다 (꼬마는 작게)
            _stage.Swap(_customer, Tex(_who.Id + "-walk"), CustomerHeight * _who.Height, new Vector2(0.5f, 1));
            // 걷기 그림은 오른쪽을 본다 (트럭 쪽)
            _customer.FlipH = false;
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
            // 걸음 가운데(몸이 가장 높을 때)는 다리를 모은 그림 (scripts/sd_pass.py). 없는 손님(지팡이, 엄마와 아이)은 한 장 그대로
            Texture2D stride = _who != null ? Tex(_who.Id + "-walk") : null;
            Texture2D pass = _who != null && ResourceLoader.Exists("res://art/customers/" + _who.Id + "-pass.png") ? Tex(_who.Id + "-pass") : null;
            bool together = false;
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
                float phase = t * 5 - Mathf.Floor(t * 5);
                bool now = pass != null && phase > 0.25f && phase < 0.75f;
                if (now != together)
                {
                    together = now;
                    _stage.Swap(_customer, together ? pass : stride);
                }
                _stage.Move(_customer, new Vector2(x, _footY - bob));
                _customer.RotationDegrees = Mathf.Sin(t * Mathf.Pi * 5) * 2.5f;
            }), 0f, 1f, seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tw.TweenCallback(Callable.From(() =>
            {
                _walking = false;
                _customer.RotationDegrees = 0;
                if (together) _stage.Swap(_customer, stride);
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
            if (_guest.Kind == GuestKind.Vip)
            {
                var vip = G.Diner.VipById(_guest.Id);
                ShowBadge(T["vipBadge"]);
                // 바라는 신선도의 재료가 없으면 실망하고 돌아간다
                if (G.VipFish(_guest) == null)
                {
                    VipMissed(vip, dish);
                    return;
                }
                SetBubble(string.Format(vip.Order, dish));
                Cook(recipe, 0, new List<SeasonResult>());
                return;
            }
            // 단골은 늘 하던 말로 시킨다
            if (_guest.Kind == GuestKind.Regular) ShowBadge(T["regularBadge"]);
            SetBubble(string.Format(_guest.Kind == GuestKind.Regular ? G.Data.Diner.GuestById(_guest.Id).Regular : _who.Order, dish));
            Cook(recipe, 0, new List<SeasonResult>());
        }

        /// <summary>손님 머리 위 작은 표 (VIP, 단골)</summary>
        private void ShowBadge(string text)
        {
            HideBadge();
            var pill = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Gold, 28, 18, 4).WithBorder(Palette.Ink, 4).WithShadow(0.35f, 10, 4));
            pill.AddChild(UiKit.Text(text, TextSize.Body, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center));
            float h = CustomerHeight * (_who != null ? _who.Height : 1);
            _badge = pill;
            // 말풍선은 머리 오른쪽 위에 뜨니 표는 머리 왼쪽 옆에 둔다
            _stage.Pin(pill, new Vector2(CustomerX - 78, _footY - h * 0.78f));
        }

        private void HideBadge()
        {
            if (_badge != null && IsInstanceValid(_badge)) _stage.Remove(_badge);
            _badge = null;
        }

        /// <summary>VIP 가 바란 요리를 못 냈다: 실망한 얼굴로 한마디, 명성이 깎인다</summary>
        private void VipMissed(DinerVip vip, string dish)
        {
            int rep = G.MissVip(_guest);
            _served += 1;
            _stage.Swap(_customer, Tex(_who.Id + "-sad"));
            React(0);
            Sound.Play("bad", -2);
            SetBubble(string.Format(vip.Miss, dish));
            ClearBody();
            _body.AddChild(UiKit.Text(F("vipMissTitle", vip.Name + IGa(vip.Name)), TextSize.H1, Palette.Paper, true, true, 0, null, HorizontalAlignment.Center));
            _body.AddChild(UiKit.Text(F("vipMissLine", T["fresh" + vip.Fresh], rep), TextSize.Small, Palette.RedText, false, true, 0, null, HorizontalAlignment.Center));
            _body.AddChild(NextButton());
            Rise();
        }

        private GameButton NextButton()
        {
            bool last = _served >= _plan.Count || G.MenuOpen(_menu).Count == 0;
            var next = new GameButton(last ? T["finish"] : T["next"], Palette.Green, Leave, null, TextSize.Body, 120);
            next.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            next.CustomMinimumSize = new Vector2(520, 120);
            return next;
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
            var dish = G.ServeGuest(recipe, results, _guest);
            if (dish == null)
            {
                _served += 1;
                NextCustomer();
                return;
            }
            _served += 1;
            CoreGame.MarkServed(_menu, recipe);
            UpdateChalk();
            int tip = dish.Guest != null ? dish.Guest.Tip : 0;
            _sales += dish.Pay + tip;
            _hud.Refresh();

            int mood = G.Truck.MoodOf(dish.Avg);
            Sound.Play(mood == 2 ? "good" : mood == 0 ? "bad" : "okay", -2);
            Sound.Play("coin", -3);
            if (_guest.Kind == GuestKind.Special)
            {
                // 특별 손님은 대화창에서 한마디 하고 결과를 본다
                var sp = G.Diner.SpecialById(_guest.Id);
                Talk(new[] { new DialogueLine { Speaker = sp.Speaker, Face = sp.Faces[mood], Text = sp.Lines[mood] } }, null, () => ShowResult(recipe, dish));
                return;
            }
            var lines = G.Truck.ReviewLines(G.Save, recipe, results, dish.Stage, mood);
            // VIP 는 바란 대로 먹었으면 고맙다는 말, 아니면 아쉽다는 말
            if (_guest.Kind == GuestKind.Vip)
            {
                var vip = G.Diner.VipById(_guest.Id);
                lines = new List<string> { dish.Guest.VipOk ? vip.Thanks : string.Format(vip.Miss, recipe.Name) };
            }
            _stage.Swap(_customer, Tex(_who.Id + (mood == 2 ? "-happy" : mood == 0 ? "-sad" : "-stand")));
            React(mood);
            SetBubble(string.Join("\n", lines.Select(l => "“" + l + "”")));
            FloatText("+" + UiKit.Won(dish.Pay + tip), OverHead() - new Vector2(0, 260), Palette.Gold);
            ShowResult(recipe, dish);
        }

        /// <summary>요리 결과 판: 요리 그림, 받은 돈과 팁, 손님 한 줄, 별</summary>
        private void ShowResult(Recipe recipe, ServedDish dish)
        {

            ClearBody();
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 28);
            row.AddChild(PlaceScreen.DishIcon(recipe, 260));
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 6);
            text.AddChild(UiKit.Text(F("done", recipe.Name), TextSize.H1, Palette.Paper, true));
            if (dish.FirstTime) text.AddChild(UiKit.Text(T["firstRecipe"], TextSize.Small, Palette.Gold));
            text.AddChild(UiKit.Text(F("pay", UiKit.Won(dish.Pay)), TextSize.Body, Palette.Gold, true));
            if (dish.Guest != null && dish.Guest.Tip > 0) text.AddChild(UiKit.Text(F("tip", UiKit.Won(dish.Guest.Tip)), TextSize.Small, Palette.Gold, true));
            if (dish.Guest != null) text.AddChild(GuestLine(dish.Guest));
            row.AddChild(text);
            _body.AddChild(row);
            _body.AddChild(Stars(dish.Avg));
            _body.AddChild(NextButton());
            Rise();
        }

        /// <summary>손님 친밀도 한 줄. 단골이 되는 순간은 금빛으로</summary>
        private Control GuestLine(GuestResult g)
        {
            string rep0 = g.RepGain >= 0 ? "+" + g.RepGain : g.RepGain.ToString();
            if (g.Kind == GuestKind.Vip)
                return UiKit.Text(F(g.VipOk ? "vipOkLine" : "vipBadLine", G.Diner.VipById(_guest.Id).Name, rep0), TextSize.Small, g.VipOk ? Palette.Gold : Palette.RedText, true);
            if (g.Kind == GuestKind.Special)
                return UiKit.Text(F("specialLine", G.Diner.SpecialById(_guest.Id).Name, rep0), TextSize.Small, g.RepGain >= 0 ? Palette.Gold : Palette.RedText, true);
            var guest = G.Data.Diner.GuestById(_guest.Id);
            if (g.BecameRegular)
            {
                Sound.Play("good", -4, 1.2f);
                return UiKit.Text(F("becameRegular", guest.Name + IGa(guest.Name)), TextSize.Small, Palette.Gold, true);
            }
            string key = G.Diner.IsRegular(G.Save, _guest.Id) ? "regularLine" : "affinityLine";
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
            HideBadge();
            ClearBody();
            if (_who == null)
            {
                NextCustomer();
                return;
            }
            _stage.Swap(_customer, Tex(_who.Id + "-walk"));
            _customer.FlipH = true;
            Walk(CustomerX, OffLeft, 0.7, () =>
            {
                _customer.Visible = false;
                NextCustomer();
            });
        }

        private void SkipShift()
        {
            if (G.IsTruckOpen()) G.UseSlot();
            GameState.I.GoTo("town");
        }

        private void EndShift(bool ranOut)
        {
            _counterPill.Visible = false;
            _customer.Visible = false;
            SetBubble(null);
            HideBadge();
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
                case "pick":
                    // 빈 칸을 눌러 요리를 고르는 창
                    if (_menu.Count > 0) _menu.RemoveAt(_menu.Count - 1);
                    ShowMenu(false);
                    PickDish();
                    break;
                case "count":
                    // 올린 칸을 눌러 접시 수를 고치는 창
                    if (_menu.Count > 0) AskCount(_menu[0].Recipe, _menu[0]);
                    break;
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
                    StartShift(new PlannedGuest { Id = "boy" });
                    break;
                case "booked":
                    // 줄 서는 집: 메뉴판에 VIP 예약이 보인다
                    G.Save.Diner.Rep = G.Data.Diner.Ranks[3].Rep;
                    ShowMenu(false);
                    break;
                case "vip":
                {
                    // VIP 가 활어 요리를 바란다 (재료는 쿨러에 넣어 둔다)
                    var vip = G.Data.Diner.Vips[0];
                    var wish = G.Data.RecipeById(vip.Wants[0]);
                    G.AddToCooler(wish.Species, G.Data.SpeciesById(wish.Species).AvgSize + 3);
                    StartShift(new PlannedGuest { Id = vip.Id, Kind = GuestKind.Vip, Wish = wish.Id });
                    break;
                }
                case "sera":
                    // 특별 손님 차세라는 대화창 일러스트로 온다
                    StartShift(new PlannedGuest { Id = "sera", Kind = GuestKind.Special });
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

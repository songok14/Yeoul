using Godot;
using Hanjeopsi.Core.State;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 화면 위 상태 막대 (웹 ui/hud.ts). 날짜, 시간대 칩, D-day, 소지금, 남은 빚과 상환 막대.
    /// 돈이 바뀌면 숫자가 굴러가며 바뀐다.
    /// </summary>
    public partial class Hud : PanelContainer
    {
        public const int Height = 236;

        private readonly CoreGame _game;
        private Label _day, _dday, _money, _debt;
        private PanelContainer _ddayBox;
        private readonly Label[] _slots = new Label[5];
        private readonly PanelContainer[] _slotBoxes = new PanelContainer[5];
        private Control _barFill;
        private Control _barTrack;
        private double _shownMoney = -1;
        private double _shownDebt = -1;
        private Tween _countTween;

        public Hud() { }

        public Hud(CoreGame game)
        {
            _game = game;
            MouseFilter = MouseFilterEnum.Stop;
            var sb = UiKit.Box(new Color(Palette.NightDeep, 0.9f), 0, 36, 22).WithShadow(0.35f, 18, 6);
            sb.CornerRadiusBottomLeft = 40;
            sb.CornerRadiusBottomRight = 40;
            sb.BorderColor = Palette.Gold;
            sb.BorderWidthBottom = 5;
            AddThemeStyleboxOverride("panel", sb);

            var col = new VBoxContainer();
            col.AddThemeConstantOverride("separation", 10);
            AddChild(col);

            // 1줄: 날짜 · 시간대 칩 · D-day
            var top = new HBoxContainer();
            top.AddThemeConstantOverride("separation", 18);
            _day = UiKit.Text("", TextSize.H1 + 4, Palette.Paper, true);
            top.AddChild(_day);
            var chips = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
            chips.AddThemeConstantOverride("separation", 6);
            for (int i = 0; i < game.Data.Texts.Slots.Count && i < _slots.Length; i++)
            {
                var box = new PanelContainer();
                var label = UiKit.Text(game.Data.Texts.Slots[i], TextSize.Tiny + 2, Palette.GrayText, false, false, 0, null, HorizontalAlignment.Center);
                box.AddChild(label);
                chips.AddChild(box);
                _slots[i] = label;
                _slotBoxes[i] = box;
            }
            top.AddChild(chips);
            _ddayBox = new PanelContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
            _dday = UiKit.Text("", TextSize.Small + 4, Colors.White, true, false, 0, null, HorizontalAlignment.Center);
            _ddayBox.AddChild(_dday);
            top.AddChild(_ddayBox);
            col.AddChild(top);

            // 2줄: 소지금 · 남은 빚
            var mid = new HBoxContainer();
            _money = UiKit.Text("", TextSize.Body, Palette.Gold, true);
            _money.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _debt = UiKit.Text("", TextSize.Small, Palette.GrayText, false, false, 0, null, HorizontalAlignment.Right);
            mid.AddChild(_money);
            mid.AddChild(_debt);
            col.AddChild(mid);

            // 3줄: 상환 막대 (갚은 만큼 금색이 찬다)
            _barTrack = new Panel { CustomMinimumSize = new Vector2(0, 14), MouseFilter = MouseFilterEnum.Ignore };
            _barTrack.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(1, 1, 1, 0.13f), 7));
            _barFill = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            _barFill.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Gold, 7));
            _barTrack.AddChild(_barFill);
            _barTrack.Resized += () => SetBar(_shownDebt);
            col.AddChild(_barTrack);

            Refresh(false);
        }

        /// <summary>상태를 다시 그린다. animate 면 돈과 막대가 부드럽게 바뀐다</summary>
        public void Refresh(bool animate = true)
        {
            var s = _game.Save;
            _day.Text = s.Day + "일차";
            for (int i = 0; i < _slots.Length; i++)
            {
                bool now = i == s.Slot;
                var sb = UiKit.Box(now ? Palette.Gold : new Color(1, 1, 1, 0), 22, 18, 4);
                _slotBoxes[i].AddThemeStyleboxOverride("panel", sb);
                _slots[i].LabelSettings.FontColor = now ? Palette.Ink : i < s.Slot ? new Color(Palette.GrayText, 0.4f) : Palette.GrayText;
            }

            // 주간 상환이면 이번 주 상환일까지, 아니면 1장 기한까지
            int left = _game.IsWeekly ? _game.DaysToDue() : _game.DaysLeft();
            bool done = s.Debt <= 0;
            string pre = _game.IsWeekly ? s.Week.No + "주 " : "";
            _dday.Text = done ? "상환 완료" : left > 0 ? pre + "D-" + left : left == 0 ? pre + "상환일" : "기한 지남";
            _ddayBox.AddThemeStyleboxOverride("panel", UiKit.Box(done ? Palette.GreenDark : left <= 3 ? Palette.Red : Palette.RedDark, 26, 22, 6));

            if (!animate || _shownMoney < 0)
            {
                ShowMoney(s.Money);
                ShowDebt(s.Debt);
                return;
            }
            _countTween?.Kill();
            _countTween = CreateTween().SetParallel(true);
            _countTween.TweenMethod(Callable.From<double>(ShowMoney), _shownMoney, (double)s.Money, 0.6).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _countTween.TweenMethod(Callable.From<double>(ShowDebt), _shownDebt, (double)s.Debt, 0.8).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        private void ShowMoney(double v)
        {
            _shownMoney = v;
            _money.Text = "소지금 " + UiKit.Won((long)Mathf.Round(v));
        }

        private void ShowDebt(double v)
        {
            _shownDebt = v;
            if (v > 0.5 && _game.IsWeekly)
            {
                // 이번 주에 더 낼 돈과 남은 빚 (다 냈으면 "이번 주 끝")
                int due = _game.DueNow();
                _debt.Text = (due > 0 ? "이번 주 " + UiKit.Won(due) : "이번 주 완료") + " · 빚 " + UiKit.Won((long)Mathf.Round(v));
            }
            else _debt.Text = v > 0.5 ? "남은 빚 " + UiKit.Won((long)Mathf.Round(v)) : "1차 상환 끝";
            SetBar(v);
        }

        private void SetBar(double debt)
        {
            if (_barFill == null || debt < 0) return;
            double total = _game.Data.Balance.Chapter1.Debt;
            float paid = (float)Mathf.Clamp(1 - debt / total, 0, 1);
            float w = _barTrack.Size.X * paid;
            _barFill.Position = Vector2.Zero;
            _barFill.Size = new Vector2(paid > 0 ? Mathf.Max(14, w) : 0, _barTrack.Size.Y);
        }
    }
}

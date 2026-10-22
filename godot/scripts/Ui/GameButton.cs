using System;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 게임 버튼 (웹 ui.ts Button). 위에 큰 글씨, 아래에 작은 설명 한 줄.
    /// 아래쪽이 두꺼운 테두리라 도톰해 보이고, 누르면 살짝 눌렸다가 튕겨 돌아온다.
    /// </summary>
    public partial class GameButton : Button
    {
        private readonly Color _color;
        private Label _title;
        private Label _sub;
        private Tween _tween;
        private Action _onPress;

        public GameButton() { }

        public GameButton(string title, Color color, Action onPress, string sub = null, int titleSize = TextSize.Body, int height = 120)
        {
            _color = color;
            _onPress = onPress;
            CustomMinimumSize = new Vector2(0, height);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            FocusMode = FocusModeEnum.None;
            ActionMode = ActionModeEnum.Release;

            AddThemeStyleboxOverride("normal", Face(color, 10));
            AddThemeStyleboxOverride("hover", Face(color, 10));
            AddThemeStyleboxOverride("pressed", Face(color.Darkened(0.12f), 4));
            AddThemeStyleboxOverride("disabled", Face(color.Lerp(Palette.GrayDark, 0.75f), 10));
            AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

            var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 0);
            col.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            col.OffsetBottom = -8;
            _title = UiKit.Text(title, titleSize, Colors.White, true, false, 0, null, HorizontalAlignment.Center);
            _title.LabelSettings.ShadowSize = 2;
            _title.LabelSettings.ShadowColor = new Color(0, 0, 0, 0.25f);
            _title.LabelSettings.ShadowOffset = new Vector2(0, 2);
            col.AddChild(_title);
            if (!string.IsNullOrEmpty(sub))
            {
                _sub = UiKit.Text(sub, TextSize.Tiny, new Color(1, 1, 1, 0.82f), false, false, 0, null, HorizontalAlignment.Center);
                col.AddChild(_sub);
            }
            AddChild(col);

            ButtonDown += () =>
            {
                Squash(0.94f, 0.07);
                Sound.Play("tap", -3);
                Haptics.Tap();
            };
            ButtonUp += () => Squash(1f, 0.28);
            Pressed += () => _onPress?.Invoke();
            Resized += () => PivotOffset = Size / 2;
        }

        /// <summary>버튼 얼굴. 아래 테두리가 그림자 겸 두께다. 누르면 두께가 줄어 내려간 것처럼 보인다</summary>
        private static StyleBoxFlat Face(Color c, int lip)
        {
            var sb = UiKit.Box(c, 30, 16, 6).WithBorder(c.Darkened(0.32f), 0, lip).WithShadow(0.28f, 10, 6);
            sb.ContentMarginBottom = 6 + lip;
            // 위쪽에 아주 옅은 빛을 둔다
            sb.BorderBlend = false;
            return sb;
        }

        public void SetSub(string sub)
        {
            if (_sub != null) _sub.Text = sub;
        }

        public void SetTitle(string title)
        {
            _title.Text = title;
        }

        public void SetEnabled(bool enabled)
        {
            Disabled = !enabled;
            Modulate = enabled ? Colors.White : new Color(1, 1, 1, 0.72f);
            MouseDefaultCursorShape = enabled ? CursorShape.PointingHand : CursorShape.Arrow;
        }

        private void Squash(float to, double time)
        {
            _tween?.Kill();
            _tween = CreateTween();
            var t = _tween.TweenProperty(this, "scale", new Vector2(to, to), time);
            t.SetTrans(to < 1 ? Tween.TransitionType.Quad : Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
    }
}

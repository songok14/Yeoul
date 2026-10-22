using System;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>그림 속 건물 위에 다는 종이 간판 (웹 TownScene sign). 누르면 살짝 눌렸다 튕긴다</summary>
    public partial class SignButton : Button
    {
        private Tween _tween;

        public SignButton() { }

        public SignButton(string text, Action onPress)
        {
            FocusMode = FocusModeEnum.None;
            ActionMode = ActionModeEnum.Release;
            MouseDefaultCursorShape = CursorShape.PointingHand;
            var face = UiKit.Box(new Color(Palette.PaperLight, 0.96f), 40, 30, 10).WithBorder(new Color(Palette.Ink, 0.85f), 3).WithShadow(0.3f, 10, 5);
            AddThemeStyleboxOverride("normal", face);
            AddThemeStyleboxOverride("hover", face);
            AddThemeStyleboxOverride("pressed", UiKit.Box(Palette.PaperDark, 40, 30, 10).WithBorder(new Color(Palette.Ink, 0.85f), 3));
            AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            AddThemeFontOverride("font", Fonts.Title);
            AddThemeFontSizeOverride("font_size", TextSize.Small);
            AddThemeColorOverride("font_color", Palette.Ink);
            AddThemeColorOverride("font_hover_color", Palette.Ink);
            AddThemeColorOverride("font_pressed_color", Palette.Ink);
            Text = text;
            ButtonDown += () => Squash(0.92f, 0.06);
            ButtonUp += () => Squash(1f, 0.3);
            Pressed += () => onPress();
        }

        private void Squash(float to, double time)
        {
            _tween?.Kill();
            _tween = CreateTween();
            _tween.TweenProperty(this, "scale", new Vector2(to, to), time).SetTrans(to < 1 ? Tween.TransitionType.Quad : Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
    }
}

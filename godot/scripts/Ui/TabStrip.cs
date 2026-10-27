using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 탭 줄. 어두운 알약 안에 글자 칸이 나란히 있고, 고른 칸 밑으로 금색 알약이 미끄러져 간다.
    /// </summary>
    public partial class TabStrip : PanelContainer
    {
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<Label> _labels = new List<Label>();
        private readonly Action<int> _onPick;
        private Panel _knob;
        private HBoxContainer _row;
        private int _selected = -1;
        private Tween _tween;

        public TabStrip() { }

        public TabStrip(IList<string> labels, Action<int> onPick)
        {
            _onPick = onPick;
            AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.82f), 46, 10, 10).WithShadow(0.3f, 14, 6));

            var holder = new Control { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, 92) };
            AddChild(holder);
            _knob = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            _knob.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Gold, 40).WithBorder(Palette.GoldDark, 0, 6));
            holder.AddChild(_knob);
            _row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _row.AddThemeConstantOverride("separation", 0);
            _row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holder.AddChild(_row);

            for (int i = 0; i < labels.Count; i++)
            {
                int idx = i;
                var b = new Button { SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.None, Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
                foreach (var st in new[] { "normal", "hover", "pressed", "focus", "hover_pressed" }) b.AddThemeStyleboxOverride(st, new StyleBoxEmpty());
                var l = UiKit.Text(labels[i], TextSize.Body - 2, Palette.GrayText, true, false, 0, null, HorizontalAlignment.Center);
                l.VerticalAlignment = VerticalAlignment.Center;
                l.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                b.AddChild(l);
                b.Pressed += () =>
                {
                    if (idx == _selected) return;
                    Sound.Play("page", -3);
                    _onPick(idx);
                };
                _row.AddChild(b);
                _buttons.Add(b);
                _labels.Add(l);
            }
            _row.Resized += () => MoveKnob(false);
        }

        public void Select(int i)
        {
            bool first = _selected < 0;
            _selected = i;
            for (int k = 0; k < _labels.Count; k++) _labels[k].LabelSettings.FontColor = k == i ? Palette.Ink : Palette.GrayText;
            // 탭 줄 밖 쪽(수첩 설정처럼 제목 줄 단추로 여는 쪽)이면 고른 탭이 없다
            _knob.Visible = i >= 0 && i < _buttons.Count;
            if (!_knob.Visible) return;
            MoveKnob(!first);
        }

        public void SetLabel(int i, string label)
        {
            _labels[i].Text = label;
        }

        private void MoveKnob(bool animate)
        {
            if (_selected < 0 || _selected >= _buttons.Count || _row.Size.X <= 0) return;
            float w = _row.Size.X / _buttons.Count;
            var pos = new Vector2(w * _selected + 4, 0);
            var size = new Vector2(w - 8, _row.Size.Y);
            _tween?.Kill();
            if (!animate)
            {
                _knob.Position = pos;
                _knob.Size = size;
                return;
            }
            _knob.Size = size;
            _tween = CreateTween();
            _tween.TweenProperty(_knob, "position", pos, 0.28).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
    }
}

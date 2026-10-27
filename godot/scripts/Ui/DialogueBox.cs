using System;
using System.Collections.Generic;
using Godot;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 대화창. 말하는 사람의 감정 일러스트가 옆에서 들어오고, 아래 종이 상자에 이름표와 글이 타자기처럼 찍힌다.
    /// 화면을 누르면 다음 줄 (글이 찍히는 중이면 한 번에 다 보인다). 마지막 줄에 선택지를 달 수 있다.
    /// 인게임 캐릭터는 SD, 대화와 감정은 일러스트로 한다 (docs/GODOT_PLAN.md 8절).
    /// </summary>
    public partial class DialogueBox : Control
    {
        private const float CharsPerSecond = 32;
        private const float BoxHeight = 430;

        private readonly Dialogue _data;
        private readonly IList<DialogueLine> _lines;
        private readonly IList<ModalButton> _choices;
        private readonly Action _onDone;
        private readonly int _safeBottom;

        private TextureRect _portrait;
        private string _portraitKey;
        private string _speakerId;
        /// <summary>지금 그림이 SD (의뢰인)라 작게 세운다</summary>
        private bool _sd;
        private PanelContainer _box, _nameTag;
        private Label _name, _text;
        private Polygon2D _next;
        private VBoxContainer _choiceBox;
        private Tween _typeTween;
        private int _index = -1;
        private float _time;
        private bool _closing;

        public DialogueBox() { }

        /// <param name="choices">마지막 줄에 다는 선택지. 누르면 대화창을 닫고 그 행동을 부른다</param>
        public DialogueBox(Dialogue data, IList<DialogueLine> lines, IList<ModalButton> choices, Action onDone, int safeBottom)
        {
            _data = data;
            _lines = lines;
            _choices = choices;
            _onDone = onDone;
            _safeBottom = safeBottom;
            MouseFilter = MouseFilterEnum.Stop;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }

        public override void _Ready()
        {
            // 마을을 살짝 가라앉혀 일러스트가 앞으로 나오게 하고, 아래쪽은 더 어둡게
            var dim = new ColorRect { Color = new Color(Palette.NightDeep, 0.32f), MouseFilter = MouseFilterEnum.Ignore };
            dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(dim);
            var shade = UiKit.VerticalShade(new Color(Palette.NightDeep, 0), new Color(Palette.NightDeep, 0.78f));
            shade.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            shade.OffsetTop = -1150;
            AddChild(shade);

            _portrait = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspect, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_portrait);

            _box = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            _box.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            _box.OffsetTop = -(BoxHeight + _safeBottom);
            var sb = UiKit.Box(Palette.Paper, 0, 64, 64).WithShadow(0.35f, 26, -6);
            sb.CornerRadiusTopLeft = 46;
            sb.CornerRadiusTopRight = 46;
            sb.ContentMarginTop = 70;
            sb.ContentMarginBottom = 50 + _safeBottom;
            _box.AddThemeStyleboxOverride("panel", sb);
            _text = UiKit.Text("", TextSize.Body + 4, Palette.Ink, false, true);
            _text.VerticalAlignment = VerticalAlignment.Top;
            _text.LabelSettings.LineSpacing = 14;
            _box.AddChild(_text);
            AddChild(_box);

            // 이름표: 상자 위쪽 모서리에 걸친다
            _nameTag = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            _name = UiKit.Text("", TextSize.H1 - 4, Colors.White, true);
            _nameTag.AddChild(_name);
            AddChild(_nameTag);

            // 다음 줄 표시: 오른쪽 아래에서 깜빡이는 작은 삼각형
            _next = new Polygon2D { Polygon = new[] { new Vector2(-18, -10), new Vector2(18, -10), new Vector2(0, 12) }, Color = Palette.InkSoft };
            AddChild(_next);

            _choiceBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _choiceBox.AddThemeConstantOverride("separation", 22);
            AddChild(_choiceBox);

            Resized += Layout;
            Modulate = new Color(1, 1, 1, 0);
            CreateTween().TweenProperty(this, "modulate:a", 1f, 0.22);
            Next();
        }

        private void Layout()
        {
            var size = Size;
            float boxTop = size.Y - BoxHeight - _safeBottom;
            _nameTag.ResetSize();
            bool right = Side() == "right";
            float tagX = right ? size.X - _nameTag.Size.X - 64 : 64;
            _nameTag.Position = new Vector2(tagX, boxTop - _nameTag.Size.Y / 2);
            _next.Position = new Vector2(size.X - 74, size.Y - _safeBottom - 60);

            // 일러스트: 화면 높이의 70% 크기, 발은 상자 아래로 숨긴다
            var tex = _portrait.Texture;
            if (tex != null)
            {
                // SD(대화 일러스트가 없는 의뢰인)는 마당 사람 크기와 어울리게 작게, 이름표 위 오른쪽에
                float h = _sd ? Mathf.Min(size.Y * 0.24f, 470) : size.Y * 0.7f;
                float w = tex.GetWidth() * h / tex.GetHeight();
                _portrait.Size = new Vector2(w, h);
                float cx = _sd ? size.X * 0.74f : right ? size.X * 0.66f : size.X * 0.34f;
                _portrait.Position = new Vector2(cx - w / 2, _sd ? boxTop - h + 30 : boxTop + 120 - h * 0.72f);
            }

            // 선택지는 말하는 사람 반대쪽에 세워 얼굴을 가리지 않는다
            float cw = Mathf.Min(620, size.X * 0.58f);
            _choiceBox.Size = new Vector2(cw, 0);
            _choiceBox.ResetSize();
            _choiceBox.Size = new Vector2(cw, _choiceBox.Size.Y);
            float chx = right ? 48 : size.X - cw - 48;
            _choiceBox.Position = new Vector2(chx, boxTop - _choiceBox.Size.Y - 60);
        }

        private string Side()
        {
            if (_sd) return "right";
            if (string.IsNullOrEmpty(_speakerId)) return "left";
            return _data.SpeakerById(_speakerId).Side;
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            bool typing = _typeTween != null && _typeTween.IsRunning();
            _next.Visible = !typing && !_closing && _choiceBox.GetChildCount() == 0;
            _next.Position = new Vector2(_next.Position.X, Size.Y - _safeBottom - 60 + Mathf.Sin(_time * 6) * 5);
        }

        public override void _GuiInput(InputEvent e)
        {
            // 닫히는 동안에도 누름을 삼켜 아래 버튼(상점 가격 등)이 눌리지 않게 한다
            if (_closing)
            {
                AcceptEvent();
                return;
            }
            // 폰은 터치가 마우스 누름으로도 한 번 더 오므로 마우스 누름만 본다 (터치만 보면 뒤따른 누름이 아래로 샌다)
            bool tap = e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left;
            if (e is InputEventScreenTouch || e is InputEventMouseButton) AcceptEvent();
            if (!tap) return;
            if (_typeTween != null && _typeTween.IsRunning())
            {
                // 찍히는 중이면 한 번에 다 보이게
                _typeTween.Kill();
                _text.VisibleRatio = 1;
                ShowChoicesIfLast();
                return;
            }
            if (_choiceBox.GetChildCount() > 0) return;
            Next();
        }

        private void Next()
        {
            _index++;
            if (_index >= _lines.Count)
            {
                Close(_onDone);
                return;
            }
            var line = _lines[_index];
            SetSpeaker(line);
            _text.Text = line.Text;
            _text.VisibleRatio = 0;
            _typeTween?.Kill();
            _typeTween = CreateTween();
            double secs = Math.Max(0.25, line.Text.Length / CharsPerSecond);
            _typeTween.TweenProperty(_text, "visible_ratio", 1f, secs);
            _typeTween.TweenCallback(Callable.From(ShowChoicesIfLast));
        }

        /// <summary>말하는 사람이 바뀌면 일러스트가 옆에서 미끄러져 들어오고, 감정만 바뀌면 살짝 바꿔 낀다</summary>
        private void SetSpeaker(DialogueLine line)
        {
            bool narrator = string.IsNullOrEmpty(line.Speaker);
            if (narrator && !string.IsNullOrEmpty(line.Name))
            {
                // 마을 의뢰인: 이름표와 SD 그림
                _speakerId = null;
                _nameTag.Visible = true;
                _name.Text = line.Name;
                _nameTag.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.WoodDark, 34, 40, 12).WithShadow(0.3f, 10, 4));
                string artKey = "art:" + line.Art;
                if (artKey != _portraitKey)
                {
                    _portraitKey = artKey;
                    _sd = true;
                    _portrait.Texture = string.IsNullOrEmpty(line.Art) ? null : GD.Load<Texture2D>("res://art/" + line.Art + ".png");
                    _portrait.Modulate = new Color(1, 1, 1, 0);
                    CreateTween().TweenProperty(_portrait, "modulate:a", 1f, 0.25);
                }
                Layout();
                return;
            }
            _sd = false;
            if (narrator)
            {
                _nameTag.Visible = false;
                if (_portraitKey != null) CreateTween().TweenProperty(_portrait, "modulate:a", 0f, 0.2);
                _portraitKey = null;
                _speakerId = null;
                Layout();
                return;
            }
            var sp = _data.SpeakerById(line.Speaker);
            string key = sp.Face(line.Face);
            bool newSpeaker = line.Speaker != _speakerId;
            _speakerId = line.Speaker;
            _nameTag.Visible = true;
            _name.Text = sp.Name;
            _nameTag.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(sp.Color), 34, 40, 12).WithShadow(0.3f, 10, 4));
            if (key == _portraitKey) return;
            _portraitKey = key;
            _portrait.Texture = GD.Load<Texture2D>("res://art/portraits/" + key + ".png");
            Layout();

            float baseX = _portrait.Position.X;
            if (newSpeaker)
            {
                float from = sp.Side == "right" ? 90 : -90;
                _portrait.Position = new Vector2(baseX + from, _portrait.Position.Y);
                _portrait.Modulate = new Color(1, 1, 1, 0);
                var tw = CreateTween().SetParallel(true);
                tw.TweenProperty(_portrait, "position:x", baseX, 0.4).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                tw.TweenProperty(_portrait, "modulate:a", 1f, 0.3);
            }
            else
            {
                // 같은 사람, 다른 감정: 살짝 비쳤다가 또렷해진다
                _portrait.Modulate = new Color(1, 1, 1, 0.55f);
                CreateTween().TweenProperty(_portrait, "modulate:a", 1f, 0.22);
            }
        }

        private void ShowChoicesIfLast()
        {
            if (_index != _lines.Count - 1 || _choices == null || _choices.Count == 0 || _choiceBox.GetChildCount() > 0) return;
            for (int i = 0; i < _choices.Count; i++)
            {
                var c = _choices[i];
                var btn = new GameButton(c.Label, c.Color, () => Close(c.OnClick), null, TextSize.Body, 112);
                btn.SetEnabled(!c.Disabled);
                _choiceBox.AddChild(btn);
                btn.Modulate = new Color(1, 1, 1, 0);
                CreateTween().TweenProperty(btn, "modulate:a", 1f, 0.25).SetDelay(0.05 * i);
            }
            Layout();
        }

        private void Close(Action then)
        {
            if (_closing) return;
            _closing = true;
            var tw = CreateTween();
            tw.TweenProperty(this, "modulate:a", 0f, 0.2);
            tw.TweenCallback(Callable.From(QueueFree));
            then?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using Godot;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.App.Ui
{
    /// <summary>모달 버튼 하나</summary>
    public sealed class ModalButton
    {
        public string Label;
        public Color Color;
        public Action OnClick;
        public bool Disabled;

        public ModalButton(string label, Color color, Action onClick = null, bool disabled = false)
        {
            Label = label;
            Color = color;
            OnClick = onClick;
            Disabled = disabled;
        }
    }

    /// <summary>
    /// 화면 하나의 바탕. 층을 나눠 둔다: Art(배경·캐릭터) → Ui(HUD·메뉴, 안전 영역 안) → Popups(모달) → Toasts.
    /// 공통 연출(모달, 토스트, 말풍선, 떠오르는 숫자)을 여기서 만든다 (웹 ui.ts modal, toast, bubble, floatText).
    /// </summary>
    public abstract partial class Screen : Control
    {
        protected Control Art { get; private set; }
        /// <summary>노치와 제스처 바를 피한 영역</summary>
        protected MarginContainer Safe { get; private set; }
        protected Control Popups { get; private set; }
        protected Control Toasts { get; private set; }

        public int SafeTop { get; private set; }
        public int SafeBottom { get; private set; }

        private readonly List<Control> _modals = new List<Control>();
        private Control _toast;

        public bool ModalOpen
        {
            get { return _modals.Count > 0; }
        }

        public override void _Ready()
        {
            Theme = UiKit.Theme;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;

            Art = Layer("Art");
            Safe = new MarginContainer { Name = "Safe", MouseFilter = MouseFilterEnum.Ignore };
            Safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(Safe);
            Popups = Layer("Popups");
            Toasts = Layer("Toasts");

            ApplySafeArea();
            GetViewport().SizeChanged += ApplySafeArea;
            Build();
            Sound.Music(Music);
            GameState.I.ScreenReady(this);
        }

        /// <summary>이 화면 배경음 (godot/audio/bgm 이름). null 이면 이전 곡을 그대로 둔다</summary>
        protected virtual string Music
        {
            get { return null; }
        }

        public override void _ExitTree()
        {
            Sound.StopLoops();
            GetViewport().SizeChanged -= ApplySafeArea;
        }

        /// <summary>화면을 만든다. Art, Safe 에 붙인다</summary>
        protected abstract void Build();

        /// <summary>CI 사진 모드에서 찍기 전에 할 일 (모달 열기 등). 이름은 --shot-action= 값</summary>
        public virtual void ShotSetup(string action) { }

        private Control Layer(string name)
        {
            var c = new Control { Name = name, MouseFilter = MouseFilterEnum.Ignore };
            c.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(c);
            return c;
        }

        /// <summary>노치와 아래 제스처 바. 화면 픽셀을 뷰포트 좌표로 바꿔 여백을 준다</summary>
        private void ApplySafeArea()
        {
            var view = GetViewportRect().Size;
            var win = DisplayServer.WindowGetSize();
            var safe = DisplayServer.GetDisplaySafeArea();
            float k = win.X > 0 ? view.X / win.X : 1;
            SafeTop = Mathf.Max(0, (int)(safe.Position.Y * k));
            SafeBottom = Mathf.Max(0, (int)((win.Y - safe.End.Y) * k));
            if (safe.Size.X <= 0 || safe.Size.Y <= 0)
            {
                SafeTop = 0;
                SafeBottom = 0;
            }
            Safe.AddThemeConstantOverride("margin_left", 0);
            Safe.AddThemeConstantOverride("margin_right", 0);
            Safe.AddThemeConstantOverride("margin_top", SafeTop);
            Safe.AddThemeConstantOverride("margin_bottom", SafeBottom);
            OnSafeAreaChanged();
        }

        protected virtual void OnSafeAreaChanged() { }

        // ───────── 등장 연출 ─────────

        /// <summary>
        /// 컨테이너 안에서 미끄러져 올라오게 하려면 감싸야 한다 (컨테이너는 자식 위치를 다시 정한다).
        /// 감싼 상자는 안쪽 크기만큼 자리를 차지하고, 안쪽은 FadeUp 으로 움직인다.
        /// </summary>
        public static Control Riser(Control content)
        {
            var holder = new Control { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = content.SizeFlagsHorizontal };
            holder.AddChild(content);
            content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            void Fit() => holder.CustomMinimumSize = content.GetCombinedMinimumSize();
            content.MinimumSizeChanged += Fit;
            Fit();
            return holder;
        }

        /// <summary>delay 뒤에 rise 픽셀 아래에서 떠오르며 나타난다 (Riser 로 감싼 것에)</summary>
        public static void FadeUp(Control content, double delay, float rise, double time = 0.8)
        {
            content.Modulate = new Color(1, 1, 1, 0);
            content.OffsetTop = rise;
            content.OffsetBottom = rise;
            var tw = content.CreateTween().SetParallel(true);
            tw.TweenProperty(content, "modulate:a", 1f, time * 0.8).SetDelay(delay).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(content, "offset_top", 0f, time).SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(content, "offset_bottom", 0f, time).SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        // ───────── 모달 ─────────

        /// <summary>
        /// 가운데 종이 상자. 버튼을 누르면 닫히고 OnClick 을 부른다.
        /// 버튼이 둘 이하이고 짧으면 가로로, 아니면 세로로 쌓는다.
        /// </summary>
        public Control Modal(string title, string body, IList<ModalButton> buttons, Control extra = null)
        {
            var root = new Control { MouseFilter = MouseFilterEnum.Stop };
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var dim = new ColorRect { Color = new Color(Palette.NightDeep, 0.62f), MouseFilter = MouseFilterEnum.Ignore };
            dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddChild(dim);

            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddChild(center);

            float width = Mathf.Min(940, GetViewportRect().Size.X - 80);
            var panel = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
            panel.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 40, 52, 46).WithBorder(Palette.PaperDark, 0, 10).WithShadow(0.4f, 30, 14));
            center.AddChild(panel);

            var col = new VBoxContainer();
            col.AddThemeConstantOverride("separation", 26);
            panel.AddChild(col);
            if (!string.IsNullOrEmpty(title)) col.AddChild(UiKit.Text(title, TextSize.Title - 6, Palette.Ink, true, true, 0, null, HorizontalAlignment.Center));
            if (!string.IsNullOrEmpty(body)) col.AddChild(UiKit.Text(body, TextSize.Body, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center));
            if (extra != null) col.AddChild(extra);

            bool row = buttons.Count <= 2 && AllShort(buttons);
            BoxContainer box = row ? new HBoxContainer() : (BoxContainer)new VBoxContainer();
            box.AddThemeConstantOverride("separation", 22);
            if (buttons.Count > 0) col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
            foreach (var b in buttons)
            {
                var mb = b;
                var btn = new GameButton(mb.Label, mb.Color, () => CloseModal(root, mb.OnClick), null, TextSize.Body, row ? 116 : 108);
                btn.SetEnabled(!mb.Disabled);
                box.AddChild(btn);
            }
            col.AddChild(box);

            Popups.AddChild(root);
            _modals.Add(root);

            // 어두운 막이 깔리고 종이가 톡 튀어나온다
            Sound.Play("pop", -4);
            dim.Modulate = new Color(1, 1, 1, 0);
            panel.Modulate = new Color(1, 1, 1, 0);
            panel.Scale = new Vector2(0.9f, 0.9f);
            panel.Resized += () => panel.PivotOffset = panel.Size / 2;
            var tw = root.CreateTween().SetParallel(true);
            tw.TweenProperty(dim, "modulate:a", 1f, 0.22);
            tw.TweenProperty(panel, "modulate:a", 1f, 0.16);
            tw.TweenProperty(panel, "scale", Vector2.One, 0.34).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            return root;
        }

        private static bool AllShort(IList<ModalButton> buttons)
        {
            foreach (var b in buttons)
                if (b.Label.Length > 7) return false;
            return true;
        }

        public void CloseModal(Control root, Action then = null)
        {
            if (!_modals.Remove(root)) return;
            root.MouseFilter = MouseFilterEnum.Ignore;
            var tw = root.CreateTween();
            tw.TweenProperty(root, "modulate:a", 0f, 0.14);
            tw.TweenCallback(Callable.From(root.QueueFree));
            then?.Invoke();
        }

        // ───────── 대화 (일러스트) ─────────

        /// <summary>대화창을 띄운다. 다 읽으면 onDone. choices 가 있으면 마지막 줄에 선택지</summary>
        public DialogueBox Talk(IList<DialogueLine> lines, IList<ModalButton> choices = null, Action onDone = null)
        {
            var box = new DialogueBox(GameState.I.Data.Dialogue, lines, choices, onDone, SafeBottom);
            Popups.AddChild(box);
            return box;
        }

        /// <summary>dialogue.json scenes 의 이름 붙은 대화</summary>
        public DialogueBox TalkScene(string scene, Action onDone = null)
        {
            return Talk(GameState.I.Data.Dialogue.Scenes[scene], null, onDone);
        }

        /// <summary>한 사람의 한 줄</summary>
        public DialogueBox Say(string speaker, string face, string text, IList<ModalButton> choices = null, Action onDone = null)
        {
            return Talk(new[] { new DialogueLine { Speaker = speaker, Face = face, Text = text } }, choices, onDone);
        }

        // ───────── 토스트, 말풍선, 떠오르는 글자 ─────────

        /// <summary>위쪽에 잠깐 떴다 사라지는 알림 (웹 toast)</summary>
        public void Toast(string text, Color? color = null, float y = -1)
        {
            if (_toast != null && IsInstanceValid(_toast)) _toast.QueueFree();
            var pill = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.9f), 44, 40, 20).WithShadow(0.3f, 14, 6));
            int size = TextSize.Small + 2;
            var label = UiKit.Text(text, size, color ?? Palette.Paper, false, false, 0, null, HorizontalAlignment.Center);
            if (UiKit.Measure(text, size) > 820)
            {
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                label.CustomMinimumSize = new Vector2(820, 0);
            }
            pill.AddChild(label);
            Toasts.AddChild(pill);
            _toast = pill;

            float w = GetViewportRect().Size.X;
            float top = y >= 0 ? y : SafeTop + 300;
            // 글자 폭에 맞춰 가운데에 놓는다
            pill.ResetSize();
            pill.Position = new Vector2((w - pill.Size.X) / 2, top - 20);
            pill.Modulate = new Color(1, 1, 1, 0);
            var tw = pill.CreateTween();
            tw.SetParallel(true);
            tw.TweenProperty(pill, "modulate:a", 1f, 0.2);
            tw.TweenProperty(pill, "position:y", top, 0.32).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.Chain().TweenInterval(1.7);
            tw.Chain().TweenProperty(pill, "modulate:a", 0f, 0.3);
            tw.Chain().TweenCallback(Callable.From(pill.QueueFree));
        }

        /// <summary>말풍선. foot 는 꼬리 끝 위치. 톡 튀어나왔다가 seconds 뒤 사라진다 (0이면 그대로)</summary>
        public Control Bubble(Control layer, string text, Vector2 foot, float seconds = 1.6f)
        {
            var root = new Control { MouseFilter = MouseFilterEnum.Ignore };
            var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.PaperLight, 34, 34, 18).WithBorder(Palette.Ink, 4).WithShadow(0.25f, 8, 4));
            var label = UiKit.Text(text, TextSize.Small, Palette.Ink, false, false, 0, null, HorizontalAlignment.Center);
            panel.AddChild(label);
            root.AddChild(panel);
            var tail = new Polygon2D { Polygon = new[] { new Vector2(-18, -4), new Vector2(18, -4), new Vector2(0, 26) }, Color = Palette.Ink };
            var tailIn = new Polygon2D { Polygon = new[] { new Vector2(-12, -6), new Vector2(12, -6), new Vector2(0, 17) }, Color = Palette.PaperLight };
            root.AddChild(tail);
            root.AddChild(tailIn);
            layer.AddChild(root);

            panel.ResetSize();
            var size = panel.Size;
            float x = Mathf.Clamp(foot.X - size.X / 2, 16, GetViewportRect().Size.X - size.X - 16);
            panel.Position = new Vector2(x - foot.X, -size.Y - 22);
            root.Position = foot;
            tail.Position = new Vector2(0, -26);
            tailIn.Position = new Vector2(0, -26);

            root.Scale = new Vector2(0.4f, 0.4f);
            root.Modulate = new Color(1, 1, 1, 0);
            var tw = root.CreateTween().SetParallel(true);
            tw.TweenProperty(root, "scale", Vector2.One, 0.3).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(root, "modulate:a", 1f, 0.12);
            // 사진 모드에서는 찍힐 때까지 남겨 둔다
            if (seconds > 0 && !GameState.I.ShotMode)
            {
                tw.Chain().TweenInterval(seconds);
                tw.Chain().TweenProperty(root, "modulate:a", 0f, 0.25);
                tw.Chain().TweenCallback(Callable.From(root.QueueFree));
            }
            return root;
        }

        /// <summary>돈이 오르내릴 때 떠오르는 숫자 (웹 floatText)</summary>
        public void FloatText(string text, Vector2 at, Color color)
        {
            var l = UiKit.Text(text, TextSize.H1, color, true, false, 8, Palette.NightDeep, HorizontalAlignment.Center);
            Toasts.AddChild(l);
            l.ResetSize();
            l.Position = at - l.Size / 2;
            var tw = l.CreateTween().SetParallel(true);
            tw.TweenProperty(l, "position:y", l.Position.Y - 110, 1.1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(l, "modulate:a", 0f, 0.4).SetDelay(0.7);
            tw.Chain().TweenCallback(Callable.From(l.QueueFree));
        }
    }
}

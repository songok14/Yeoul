using System;
using System.Collections.Generic;
using Godot;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 마을 안 가게 화면의 바탕 (위판장, 미끼가게, 수첩).
    /// 위에서부터 HUD → 제목 줄 → 탭 → 내용 → 아래 "마을로" 막대. 배경 그림은 살짝 어둡게 깔아 종이 상자가 떠 보이게 한다.
    /// 내용은 화면 안에 다 들어가게 짜고(쪽 넘김), 스크롤은 쓰지 않는다.
    /// </summary>
    public abstract partial class PlaceScreen : Screen
    {
        protected CoreGame G
        {
            get { return GameState.I.Game; }
        }

        protected Hud HudBar { get; private set; }
        protected ArtStage Stage { get; private set; }
        /// <summary>제목 줄 오른쪽 (상태 알약, 말 걸기 버튼 등)</summary>
        protected HBoxContainer HeaderRight { get; private set; }
        /// <summary>내용 자리. 탭을 바꾸면 안을 갈아 끼운다</summary>
        protected Control Content { get; private set; }

        private Label _title;
        private PanelContainer _bottom;
        private TabStrip _tabs;
        private Control _page;

        /// <summary>배경 그림 (art/ 아래 경로, 확장자 없이). null 이면 단색</summary>
        protected abstract string Backdrop { get; }

        protected abstract string TitleText { get; }

        /// <summary>탭 이름들. 없으면 탭 줄을 만들지 않는다</summary>
        protected virtual IList<string> Tabs
        {
            get { return null; }
        }

        protected int Tab { get; private set; }

        /// <summary>지금 탭 내용을 만든다. Content 크기 안에 들어가게</summary>
        protected abstract Control BuildPage();

        /// <summary>배경 위 장식 (SD 캐릭터 등). 기본은 없음</summary>
        protected virtual void Decorate(ArtStage stage) { }

        protected override void Build()
        {
            Stage = new ArtStage();
            Stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            if (Backdrop != null) Stage.Background = GD.Load<Texture2D>("res://art/" + Backdrop + ".png");
            Art.AddChild(Stage);
            var shade = UiKit.VerticalShade(new Color(Palette.NightDeep, 0.25f), new Color(Palette.NightDeep, 0.6f));
            shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Art.AddChild(shade);
            Decorate(Stage);

            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 0);
            Safe.AddChild(col);
            HudBar = new Hud(G);
            col.AddChild(HudBar);

            // 가운데: 제목 줄, 탭, 내용 (양옆 여백)
            var mid = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            mid.AddThemeConstantOverride("margin_left", 36);
            mid.AddThemeConstantOverride("margin_right", 36);
            mid.AddThemeConstantOverride("margin_top", 26);
            mid.AddThemeConstantOverride("margin_bottom", 26);
            col.AddChild(mid);
            var inner = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            inner.AddThemeConstantOverride("separation", 22);
            mid.AddChild(inner);

            var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            head.AddThemeConstantOverride("separation", 16);
            _title = UiKit.Shadowed(TitleText, TextSize.Title, Palette.Paper, true, 6, HorizontalAlignment.Left);
            _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _title.VerticalAlignment = VerticalAlignment.Center;
            head.AddChild(_title);
            HeaderRight = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
            HeaderRight.AddThemeConstantOverride("separation", 14);
            head.AddChild(HeaderRight);
            inner.AddChild(head);

            var tabs = Tabs;
            if (tabs != null)
            {
                _tabs = new TabStrip(tabs, ShowTab);
                inner.AddChild(_tabs);
            }

            Content = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            inner.AddChild(Content);

            // 아래: 마을로
            _bottom = new PanelContainer();
            col.AddChild(_bottom);
            var back = new GameButton("마을로", Palette.BlueDark, () => GameState.I.GoTo("town"), null, TextSize.Body, 112);
            back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            back.CustomMinimumSize = new Vector2(420, 112);
            _bottom.AddChild(back);
            OnSafeAreaChanged();

            HudBar.Modulate = new Color(1, 1, 1, 0);
            _bottom.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetParallel(true);
            tw.TweenProperty(HudBar, "modulate:a", 1f, 0.4);
            tw.TweenProperty(_bottom, "modulate:a", 1f, 0.4).SetDelay(0.1);

            BuildHeader();
            Content.Resized += FitPage;
            ShowTab(0);
        }

        /// <summary>제목 줄 오른쪽을 채운다 (HeaderRight)</summary>
        protected virtual void BuildHeader() { }

        /// <summary>HUD 는 노치 밑까지, 아래 막대는 제스처 바 밑까지 칠한다 (TownScreen 과 같다)</summary>
        protected override void OnSafeAreaChanged()
        {
            if (_bottom == null) return;
            Safe.AddThemeConstantOverride("margin_top", 0);
            Safe.AddThemeConstantOverride("margin_bottom", 0);
            var hudBox = (StyleBoxFlat)HudBar.GetThemeStylebox("panel").Duplicate();
            hudBox.ContentMarginTop = 22 + SafeTop;
            HudBar.AddThemeStyleboxOverride("panel", hudBox);
            var sb = UiKit.Box(new Color(Palette.Panel, 0.97f), 0, 30, 24).WithShadow(0.4f, 24, -4);
            sb.CornerRadiusTopLeft = 44;
            sb.CornerRadiusTopRight = 44;
            sb.BorderColor = new Color(1, 1, 1, 0.08f);
            sb.BorderWidthTop = 3;
            sb.ContentMarginBottom = 24 + SafeBottom;
            _bottom.AddThemeStyleboxOverride("panel", sb);
        }

        /// <summary>탭을 바꾼다. 같은 탭이어도 다시 그린다 (값이 바뀌었을 때)</summary>
        protected void ShowTab(int tab)
        {
            Tab = tab;
            _tabs?.Select(tab);
            Redraw();
        }

        /// <summary>지금 탭을 다시 그린다. 새 내용은 살짝 떠오르며 나타난다</summary>
        protected void Redraw()
        {
            if (_page != null) _page.QueueFree();
            _page = BuildPage();
            Content.AddChild(_page);
            FitPage();
            _page.Modulate = new Color(1, 1, 1, 0);
            _page.Position = new Vector2(0, 24);
            var tw = _page.CreateTween().SetParallel(true);
            tw.TweenProperty(_page, "modulate:a", 1f, 0.22);
            tw.TweenProperty(_page, "position:y", 0f, 0.32).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        private void FitPage()
        {
            if (_page == null) return;
            _page.Size = Content.Size;
            // 쪽마다 들어가는 줄 수가 화면 높이에 따라 달라지는 화면은 자리가 정해진 뒤 한 번 더 그린다
            if (FitsToHeight && Mathf.Abs(Content.Size.Y - _drawnHeight) > 1)
            {
                _drawnHeight = Content.Size.Y;
                CallDeferred(MethodName.Redraw);
            }
        }

        private float _drawnHeight = -1;

        /// <summary>BuildPage 가 Content 높이를 보고 줄 수를 정하면 true</summary>
        protected virtual bool FitsToHeight
        {
            get { return false; }
        }

        /// <summary>탭 제목을 바꾼다 (납품 의뢰 수처럼 숫자가 바뀔 때)</summary>
        protected void SetTabLabel(int i, string label)
        {
            _tabs?.SetLabel(i, label);
        }

        // ───────── 자주 쓰는 조각 ─────────

        /// <summary>종이 상자 (수첩, 쿨러). 아래쪽 테두리가 두꺼워 도톰하다</summary>
        public static PanelContainer Paper(int padX = 40, int padY = 34)
        {
            var p = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            p.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 36, padX, padY).WithBorder(Palette.PaperDark, 0, 10).WithShadow(0.35f, 22, 10));
            return p;
        }

        /// <summary>내용만큼만 높이를 차지하게 위로 붙여 감싼다 (긴 화면에서 빈 종이가 늘어나지 않게)</summary>
        public static Control TopAligned(Control c)
        {
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddChild(c);
            return col;
        }

        /// <summary>어두운 칠판 상자 (시세판)</summary>
        public static PanelContainer Board(int padX = 34, int padY = 26)
        {
            var p = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            p.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.Night, 0.94f), 32, padX, padY).WithBorder(new Color(Palette.Paper, 0.85f), 4).WithShadow(0.35f, 20, 8));
            return p;
        }

        /// <summary>물고기 그림. dark 면 아직 모르는 어종이라 실루엣만</summary>
        public static TextureRect FishIcon(string id, float width, bool dark = false)
        {
            var r = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://art/fish/" + id + ".png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(width, width * 0.7f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            if (dark) r.Modulate = new Color(0.22f, 0.19f, 0.17f, 0.85f);
            return r;
        }

        /// <summary>작은 둥근 알약 글자 (상태 표시)</summary>
        public static PanelContainer Pill(string text, Color bg, Color fg, int size = TextSize.Small - 4)
        {
            var p = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            p.AddThemeStyleboxOverride("panel", UiKit.Box(bg, 26, 24, 8));
            p.AddChild(UiKit.Text(text, size, fg, false, false, 0, null, HorizontalAlignment.Center));
            return p;
        }

        /// <summary>가로로 꽉 채우는 빈칸 (줄 안에서 오른쪽 정렬할 때)</summary>
        public static Control Spacer(float w = 0)
        {
            return new Control { SizeFlagsHorizontal = w > 0 ? SizeFlags.Fill : SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(w, 0), MouseFilter = MouseFilterEnum.Ignore };
        }

        /// <summary>종이 위 가는 구분선</summary>
        public static Control Rule()
        {
            var r = new ColorRect { Color = new Color(Palette.PaperDark, 0.9f), CustomMinimumSize = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore };
            return r;
        }

        /// <summary>쪽 넘김 줄 (◀ 1/3 ▶)</summary>
        public static Control Pager(int page, int pages, Action<int> go)
        {
            var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 30);
            var prev = new GameButton("◀", Palette.GrayDark, () => go((page + pages - 1) % pages), null, TextSize.Small, 84);
            prev.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            prev.CustomMinimumSize = new Vector2(130, 84);
            var next = new GameButton("▶", Palette.GrayDark, () => go((page + 1) % pages), null, TextSize.Small, 84);
            next.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            next.CustomMinimumSize = new Vector2(130, 84);
            row.AddChild(prev);
            row.AddChild(UiKit.Text((page + 1) + " / " + pages, TextSize.Small, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center));
            row.AddChild(next);
            return row;
        }

        /// <summary>줄 안에 놓는 작은 버튼 (팔기, 사기)</summary>
        public static GameButton SmallButton(string label, Color color, Action onPress, float width = 210, int height = 96)
        {
            var b = new GameButton(label, color, onPress, null, TextSize.Small, height);
            b.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            b.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            b.CustomMinimumSize = new Vector2(width, height);
            return b;
        }
    }
}

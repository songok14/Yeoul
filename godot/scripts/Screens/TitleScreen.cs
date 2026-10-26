using Godot;
using Hanjeopsi.App.Ui;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 타이틀 (웹 TitleScene). 노을 지는 마을, 잠든 고등어, 제목과 세 줄 머리말이 차례로 떠오르고 버튼이 올라온다.
    /// 새로 시작 / 이어하기. 아래 작은 글씨를 누르면 점검 화면.
    /// </summary>
    public partial class TitleScreen : Screen
    {
        protected override string Music
        {
            get { return "title"; }
        }

        private ArtStage _stage;
        private TextureRect _cat;
        private GameButton _newBtn, _contBtn;
        private float _time;

        protected override void Build()
        {
            var gs = GameState.I;

            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = ArtStage.LoadBackground("village-3");
            Art.AddChild(_stage);

            // 위·아래를 어둡게 덮어 글자가 잘 읽히게
            var topShade = UiKit.VerticalShade(new Color(Palette.NightDeep, 0.72f), new Color(Palette.NightDeep, 0));
            topShade.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
            topShade.OffsetBottom = 900;
            Art.AddChild(topShade);
            var bottomShade = UiKit.VerticalShade(new Color(Palette.NightDeep, 0), new Color(Palette.NightDeep, 0.85f));
            bottomShade.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            bottomShade.OffsetTop = -820;
            Art.AddChild(bottomShade);

            // 바다 위 빛 알갱이가 천천히 떠오른다
            Art.AddChild(Motes());

            // 잠든 고등어 (숨 쉬기)
            _cat = _stage.Place(GD.Load<Texture2D>("res://art/cat/sleep.png"), new Vector2(352, 572), 46);

            // 제목과 머리말
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 0);
            Safe.AddChild(col);
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 230), MouseFilter = MouseFilterEnum.Ignore });

            var title = UiKit.Shadowed("한 접시의 바다", TextSize.Hero, Palette.Paper, true, 14);
            title.LabelSettings.OutlineSize = 6;
            title.LabelSettings.OutlineColor = new Color(Palette.NightDeep, 0.6f);
            col.AddChild(Riser(title));
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 44), MouseFilter = MouseFilterEnum.Ignore });

            var lines = new[] { "나는 맛을 잃었다.", "다시 맛을 느낄 수 있는 건", "내 손으로 낚은 물고기뿐이다." };
            var lineLabels = new Label[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                var l = UiKit.Shadowed(lines[i], TextSize.Body, new Color("fbf3e0"), false, 8);
                l.LabelSettings.LineSpacing = 0;
                col.AddChild(Riser(l));
                lineLabels[i] = l;
            }

            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            // 버튼
            var btnBox = new VBoxContainer();
            btnBox.AddThemeConstantOverride("separation", 30);
            bool saved = gs.HasSave();
            _newBtn = new GameButton("새로 시작", Palette.Red, OnNew, null, TextSize.H1, 140);
            _contBtn = new GameButton("이어하기", Palette.Blue, OnContinue, saved ? null : "저장된 기록이 없어요", TextSize.H1, 140);
            _contBtn.SetEnabled(saved);
            foreach (var b in new[] { _newBtn, _contBtn })
            {
                // 가운데 620 폭
                b.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                b.CustomMinimumSize = new Vector2(620, b.CustomMinimumSize.Y);
                btnBox.AddChild(b);
            }
            col.AddChild(Riser(btnBox));

            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 70), MouseFilter = MouseFilterEnum.Ignore });
            var foot = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            var version = UiKit.Text("v" + GameState.Version + " · 1장 물비늘포", TextSize.Tiny, new Color("d8d0c0"), false, false, 0, null, HorizontalAlignment.Center);
            foot.AddChild(version);
            var lab = new Button { Text = "  점검  ", Flat = true, FocusMode = FocusModeEnum.None };
            lab.AddThemeFontSizeOverride("font_size", TextSize.Tiny);
            lab.AddThemeColorOverride("font_color", new Color(Palette.GrayText, 0.7f));
            lab.Pressed += () => gs.GoTo("core_lab");
            foot.AddChild(lab);
            col.AddChild(Riser(foot));
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 40), MouseFilter = MouseFilterEnum.Ignore });

            // 등장 연출: 제목 → 머리말 한 줄씩 → 버튼
            FadeUp(title, 0.15, 40);
            for (int i = 0; i < lineLabels.Length; i++) FadeUp(lineLabels[i], 0.7 + i * 0.32, 18);
            FadeUp(btnBox, 1.6, 60);
            FadeUp(foot, 1.9, 0);
        }

        private Control Motes()
        {
            var p = new CpuParticles2D
            {
                Amount = 26,
                Lifetime = 7,
                Preprocess = 7,
                Texture = UiKit.SoftDot(48),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(560, 160),
                Direction = new Vector2(0, -1),
                Spread = 25,
                Gravity = new Vector2(0, -6),
                InitialVelocityMin = 10,
                InitialVelocityMax = 34,
                ScaleAmountMin = 0.12f,
                ScaleAmountMax = 0.4f,
            };
            var ramp = new Gradient();
            ramp.SetColor(0, new Color(1, 0.9f, 0.7f, 0));
            ramp.SetColor(1, new Color(1, 0.9f, 0.7f, 0));
            ramp.AddPoint(0.3f, new Color(1, 0.92f, 0.75f, 0.75f));
            ramp.AddPoint(0.7f, new Color(1, 0.92f, 0.75f, 0.5f));
            p.ColorRamp = ramp;
            var holder = new Control { MouseFilter = MouseFilterEnum.Ignore };
            holder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holder.AddChild(p);
            holder.Resized += () => p.Position = new Vector2(holder.Size.X / 2, holder.Size.Y * 0.56f);
            return holder;
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            if (_cat != null) _cat.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.1f) * 0.03f);
            if (_stage != null) _stage.Zoom = 1.015f + Mathf.Sin(_time * 0.12f) * 0.015f;
        }

        private void OnNew()
        {
            var gs = GameState.I;
            if (gs.HasSave())
            {
                Modal("새로 시작할까요?", "지금까지의 기록이 지워져요.", new[]
                {
                    new ModalButton("취소", Palette.GrayDark),
                    new ModalButton("새로 시작", Palette.Red, StartNew),
                });
                return;
            }
            StartNew();
        }

        private void StartNew()
        {
            var gs = GameState.I;
            gs.NewGame();
            // 프롤로그 → 튜토리얼 낚시 → 프롤로그 2부 → 마을
            gs.GoStory("p1");
        }

        private void OnContinue()
        {
            var gs = GameState.I;
            if (!gs.Continue())
            {
                Toast("기록을 읽지 못했어요", Palette.RedText);
                return;
            }
            // 프롤로그 중간에 껐으면 그 자리부터
            var save = gs.Game.Save;
            if (save.Flag("prologueDone")) gs.GoTo("town");
            else if (save.Flag("tutorialFish")) gs.GoStory("p2");
            else gs.GoStory("p1");
        }
    }
}

using System;
using System.Collections.Generic;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core.Data;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 이야기 장면 (웹 PrologueScene). 위는 9:16 그림 한 장(수채화 배경 + 감정 일러스트), 아래 30%는 글 상자.
    /// 글은 타자기처럼 한 자씩 나오고, 누르면 다 보이고, 한 번 더 누르면 다음 장면으로 넘어간다.
    /// p1 → 튜토리얼 낚시 → p2(소금 간 맞추기, 고등어) → 마을. clear 는 마을로, fail 은 타이틀로.
    /// 글과 장면은 godot/data/story.json, 어느 이야기인지는 GameState.StoryPart.
    /// </summary>
    public partial class StoryScreen : Screen
    {
        private const double CharSeconds = 0.038;

        private CoreGame G
        {
            get { return GameState.I.Game; }
        }

        private Story S
        {
            get { return GameState.I.Data.Story; }
        }

        private string _part;
        private List<StoryPage> _pages;
        private int _index;
        private string _look;
        private Control _scene;
        /// <summary>누르면 넘어가는 막. 장면은 언제나 이 막 바로 아래에 둔다 (글 상자를 덮지 않게)</summary>
        private Control _tap;
        private ArtStage _stage;
        private Tween _zoomTween;
        private PanelContainer _panel;
        private VBoxContainer _panelCol;
        private Label _text, _next;
        private double _typeT;
        private bool _typing;
        private bool _blocked;
        private SeasonGauge _gauge;
        private ColorRect _flash;
        private GameButton _skip;

        protected override string Music
        {
            get { return GameState.I.StoryPart == "clear" ? "town" : "title"; }
        }

        protected override void Build()
        {
            _part = GameState.I.StoryPart ?? "p1";
            _pages = S.Parts[_part];
            _index = 0;

            // 누르면 넘어가는 막 (그림 전체)
            var tap = _tap = new Control { MouseFilter = MouseFilterEnum.Stop };
            tap.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            tap.GuiInput += OnTap;
            Art.AddChild(tap);

            _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            _panelCol = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _panelCol.AddThemeConstantOverride("separation", 18);
            _text = UiKit.Text("", TextSize.Body, Palette.Paper, false, true);
            _text.SizeFlagsVertical = SizeFlags.ExpandFill;
            _text.VerticalAlignment = VerticalAlignment.Top;
            _text.LabelSettings.LineSpacing = 14;
            _panelCol.AddChild(_text);
            _next = UiKit.Text("▼", TextSize.Small, Palette.Gold, false, false, 0, null, HorizontalAlignment.Right);
            _panelCol.AddChild(_next);
            _panel.AddChild(_panelCol);
            Art.AddChild(_panel);
            var bob = _next.CreateTween().SetLoops();
            bob.TweenProperty(_next, "modulate:a", 0.25f, 0.45);
            bob.TweenProperty(_next, "modulate:a", 1f, 0.45);

            _flash = new ColorRect { Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
            _flash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Art.AddChild(_flash);

            // 프롤로그는 건너뛸 수 있다
            if (_part == "p1" || _part == "p2")
            {
                var top = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
                top.AddThemeConstantOverride("margin_top", 26);
                top.AddThemeConstantOverride("margin_right", 30);
                top.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
                _skip = new GameButton(S.Ui["skip"], Palette.GrayDark, Finish, null, TextSize.Small, 92) { CustomMinimumSize = new Vector2(220, 92) };
                top.AddChild(_skip);
                var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
                row.AddChild(top);
                var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
                col.AddChild(row);
                Safe.AddChild(col);
            }

            OnSafeAreaChanged();
            ShowPage();
        }

        /// <summary>글 상자 높이: 화면의 30%</summary>
        private float PanelHeight
        {
            get { return Mathf.Max(520, GetViewportRect().Size.Y * 0.3f); }
        }

        protected override void OnSafeAreaChanged()
        {
            if (_panel == null) return;
            _panel.OffsetTop = -(PanelHeight + SafeBottom);
            var sb = UiKit.Box(new Color(Palette.NightDeep, 0.94f), 0, 64, 44).WithShadow(0.4f, 26, -6);
            sb.CornerRadiusTopLeft = 46;
            sb.CornerRadiusTopRight = 46;
            sb.BorderColor = Palette.Gold;
            sb.BorderWidthTop = 4;
            sb.ContentMarginBottom = 40 + SafeBottom;
            _panel.AddThemeStyleboxOverride("panel", sb);
            if (_scene != null) LayoutPic(_scene);
        }

        // ───────── 장면 ─────────

        private void ShowPage()
        {
            var page = _pages[_index];
            string look = page.Bg + "/" + (page.Pic ?? "");
            if (look != _look)
            {
                _look = look;
                SwapScene(BuildScene(page));
            }
            _text.Text = page.Text;
            _text.VisibleCharacters = 0;
            _typeT = 0;
            _typing = true;
            _next.Visible = false;
        }

        /// <summary>새 장면을 위에 깔고 스르륵 나타나게 한다. 옛 장면은 그 뒤에 지운다</summary>
        private void SwapScene(Control scene)
        {
            var old = _scene;
            _scene = scene;
            // 옛 장면 위, 누르는 막·글 상자 아래. 몇 번을 넘겨도 순서가 같다
            Art.AddChild(scene);
            Art.MoveChild(scene, _tap.GetIndex());
            if (old != null)
            {
                scene.Modulate = new Color(1, 1, 1, 0);
                var tw = scene.CreateTween();
                tw.TweenProperty(scene, "modulate:a", 1f, 0.5).SetTrans(Tween.TransitionType.Sine);
                tw.TweenCallback(Callable.From(old.QueueFree));
            }
            // 천천히 다가가는 카메라 (그림이 살아 있는 느낌)
            _zoomTween?.Kill();
            if (_stage != null)
            {
                var st = _stage;
                _zoomTween = CreateTween();
                _zoomTween.TweenMethod(Callable.From<float>(z => st.Zoom = z), 1f, 1.07f, 9).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            }
        }

        private Control BuildScene(StoryPage page)
        {
            var root = new Control { MouseFilter = MouseFilterEnum.Ignore };
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage = null;
            switch (page.Bg)
            {
                case "harbor": Backdrop(root, "village-1", Colors.White); break;
                case "breakwater": Backdrop(root, "breakwater", new Color("ffd8b8")); break;
                case "dawn": Backdrop(root, "village-2", new Color("fff6ea")); break;
                case "night": Backdrop(root, "village-night", new Color("d8dcf0")); break;
                case "phone": Backdrop(root, "village-night", new Color("8890b8")); break;
                case "storage": Backdrop(root, "truckstop", new Color("8f7a6a")); break;
                case "seize":
                    Backdrop(root, "village-1", new Color("8a8a90"));
                    Seize(root);
                    break;
                case "memory":
                    Backdrop(root, "village-3", new Color("f0d8b0"));
                    // 오래된 기억: 빛바랜 세피아와 가장자리 어둠
                    root.AddChild(Fill(new Color(Palette.Wood, 0.3f)));
                    root.AddChild(Vignette(0.55f));
                    break;
                case "grill":
                    Backdrop(root, "breakwater", new Color("ff9f78"));
                    root.AddChild(Fill(new Color(0.08f, 0.05f, 0.1f, 0.35f)));
                    root.AddChild(Embers(new Color("ffb04a"), 40, -160, 0.6f));
                    break;
                case "kitchen":
                    root.AddChild(UiKit.VerticalShade(new Color("3a1610"), new Color("120807")));
                    root.AddChild(Glow(new Color("ff7a2a"), 0.55f));
                    root.AddChild(Embers(new Color("ffb04a"), 70, -260, 1f));
                    break;
                case "ash":
                    root.AddChild(UiKit.VerticalShade(new Color("2c2c32"), new Color("101014")));
                    root.AddChild(Embers(new Color(0.75f, 0.75f, 0.78f, 0.7f), 50, 50, 0.4f));
                    break;
                case "room":
                    root.AddChild(UiKit.VerticalShade(new Color("4a3324"), new Color("1e140e")));
                    root.AddChild(Glow(new Color("ffd9a0"), 0.35f));
                    break;
                case "notice":
                    root.AddChild(UiKit.VerticalShade(new Color("3a2c22"), new Color("161009")));
                    Notice(root);
                    break;
            }
            foreach (Control c in root.GetChildren())
                if (c is TextureRect tr && c != _stage) tr.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            if (!string.IsNullOrEmpty(page.Pic)) AddPic(root, page.Pic, page.Bg == "notice");
            return root;
        }

        private void Backdrop(Control root, string bg, Color tint)
        {
            _stage = new ArtStage
            {
                Background = ArtStage.LoadBackground(bg),
                Tint = tint,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddChild(_stage);
        }

        private static ColorRect Fill(Color c)
        {
            var r = new ColorRect { Color = c, MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            return r;
        }

        /// <summary>가운데가 밝은 둥근 빛 (일러스트 뒤에 깐다)</summary>
        private static TextureRect Glow(Color c, float alpha)
        {
            var g = new TextureRect
            {
                Texture = UiKit.SoftDot(256),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Modulate = new Color(c, alpha),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            g.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            return g;
        }

        /// <summary>가장자리를 어둡게 (기억 장면): 어두운 막 위에 가운데만 따뜻한 빛을 더한다</summary>
        private static Control Vignette(float alpha)
        {
            var holder = new Control { MouseFilter = MouseFilterEnum.Ignore };
            holder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holder.AddChild(Fill(new Color(0.1f, 0.06f, 0.03f, alpha)));
            var light = Glow(new Color("ffe2b0"), alpha * 0.6f);
            light.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            holder.AddChild(light);
            return holder;
        }

        /// <summary>불티나 재가 떠다닌다. speed 가 음수면 위로 오른다</summary>
        private Control Embers(Color color, int amount, float speed, float scale)
        {
            var holder = new Control { MouseFilter = MouseFilterEnum.Ignore };
            holder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var size = GetViewportRect().Size;
            var p = new CpuParticles2D
            {
                Amount = amount,
                Lifetime = 4,
                Preprocess = 4,
                Texture = UiKit.SoftDot(32),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(size.X / 2, 40),
                Position = new Vector2(size.X / 2, speed < 0 ? size.Y * 0.72f : -40),
                Direction = new Vector2(0, Mathf.Sign(speed)),
                Spread = 25,
                Gravity = Vector2.Zero,
                InitialVelocityMin = Mathf.Abs(speed) * 0.5f,
                InitialVelocityMax = Mathf.Abs(speed),
                ScaleAmountMin = 0.3f * scale,
                ScaleAmountMax = 0.9f * scale,
                Color = color,
            };
            var fade = new Gradient();
            fade.SetColor(0, new Color(1, 1, 1, 0));
            fade.AddPoint(0.2f, Colors.White);
            fade.SetColor(fade.GetPointCount() - 1, new Color(1, 1, 1, 0));
            p.ColorRamp = fade;
            holder.AddChild(p);
            return holder;
        }

        /// <summary>차압 예고 통지서: 위쪽 왼편에 종이 한 장, 빨간 도장</summary>
        private void Notice(Control root)
        {
            var paper = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, RotationDegrees = -4 };
            paper.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 10, 44, 40).WithShadow(0.45f, 24, 10));
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 22);
            col.AddChild(UiKit.Text(S.Ui["noticeTitle"], TextSize.H1, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center));
            for (int i = 0; i < 7; i++)
            {
                var line = new ColorRect { Color = Palette.PaperDark, CustomMinimumSize = new Vector2(380 - (i % 3) * 70, 12), SizeFlagsHorizontal = SizeFlags.ShrinkBegin, MouseFilter = MouseFilterEnum.Ignore };
                col.AddChild(line);
            }
            var stamp = UiKit.Text(S.Ui["noticeStamp"], TextSize.Title, Palette.Red, true, false, 0, null, HorizontalAlignment.Right);
            col.AddChild(stamp);
            paper.AddChild(col);
            paper.Position = new Vector2(60, SafeTop + 220);
            root.AddChild(paper);
        }

        /// <summary>식당 문에 붙은 빨간 압류 딱지</summary>
        private void Seize(Control root)
        {
            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            center.OffsetBottom = -PanelHeight;
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 40);
            var sign = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            sign.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 8, 50, 22).WithBorder(Palette.WoodDark, 6).WithShadow(0.4f, 16, 8));
            sign.AddChild(UiKit.Text(S.Ui["seizeSign"], TextSize.Title, Palette.Ink, true, false, 0, null, HorizontalAlignment.Center));
            col.AddChild(sign);
            var red = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, RotationDegrees = -6 };
            red.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Red, 6, 70, 30).WithShadow(0.45f, 18, 8));
            red.AddChild(UiKit.Text(S.Ui["seizeStamp"], TextSize.Title + 20, Colors.White, true, false, 0, null, HorizontalAlignment.Center));
            red.Resized += () => red.PivotOffset = red.Size / 2;
            col.AddChild(red);
            center.AddChild(col);
            root.AddChild(center);
        }

        /// <summary>일러스트를 글 상자 바로 위에 발을 맞춰 세운다. 뒤에 은은한 빛을 깐다</summary>
        private void AddPic(Control root, string pic, bool right)
        {
            // "fish/bollak" 처럼 폴더가 붙으면 art 아래 그림 (작게), 아니면 감정 일러스트
            bool prop = pic.Contains('/');
            var tex = GD.Load<Texture2D>(prop ? "res://art/" + pic + ".png" : "res://art/portraits/" + pic + ".png");
            var area = new Control { Name = "PicArea", MouseFilter = MouseFilterEnum.Ignore };
            area.SetMeta("right", right);
            area.SetMeta("prop", prop);
            root.AddChild(area);
            var glow = new TextureRect
            {
                Name = "Glow",
                Texture = UiKit.SoftDot(256),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Modulate = new Color(1, 0.95f, 0.85f, 0.32f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            area.AddChild(glow);
            var r = new TextureRect
            {
                Name = "Pic",
                Texture = tex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            area.AddChild(r);
            LayoutPic(root);
            // 아래에서 살짝 떠오르며 나타난다
            r.Modulate = new Color(1, 1, 1, 0);
            float y = r.Position.Y;
            r.Position = new Vector2(r.Position.X, y + 40);
            var tw = r.CreateTween().SetParallel(true);
            tw.TweenProperty(r, "modulate:a", 1f, 0.6).SetDelay(0.15);
            tw.TweenProperty(r, "position:y", y, 0.8).SetDelay(0.15).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        /// <summary>그림 자리: 위 건너뛰기 단추 아래부터 글 상자 위까지. 그림 높이를 그 칸에 맞춘다</summary>
        private void LayoutPic(Control root)
        {
            var area = root.GetNodeOrNull<Control>("PicArea");
            if (area == null) return;
            var view = GetViewportRect().Size;
            float top = SafeTop + 150;
            float bottom = view.Y - PanelHeight - SafeBottom + 30;
            bool right = (bool)area.GetMeta("right");
            bool prop = (bool)area.GetMeta("prop");
            var pic = area.GetNode<TextureRect>("Pic");
            var tex = pic.Texture;
            float h = bottom - top;
            float maxW = view.X * (right ? 0.5f : prop ? 0.62f : 0.94f);
            // 소품(구운 볼락 등)은 그림 칸 가운데쯤 떠 있게
            if (prop) bottom -= h * 0.25f;
            float k = Mathf.Min(h / tex.GetHeight(), maxW / tex.GetWidth());
            var size = tex.GetSize() * k;
            float x = right ? view.X - size.X - 30 : (view.X - size.X) / 2;
            pic.Size = size;
            pic.Position = new Vector2(x, bottom - size.Y);
            var glow = area.GetNode<TextureRect>("Glow");
            glow.Size = new Vector2(size.X * 1.6f, size.Y * 1.1f);
            glow.Position = new Vector2(x + size.X / 2 - glow.Size.X / 2, bottom - glow.Size.Y * 0.95f);
        }

        // ───────── 글과 입력 ─────────

        public override void _Process(double delta)
        {
            if (!_typing) return;
            _typeT += delta;
            int n = (int)(_typeT / CharSeconds);
            int total = _text.GetTotalCharacterCount();
            _text.VisibleCharacters = Math.Min(n, total);
            if (n >= total)
            {
                _typing = false;
                _next.Visible = true;
            }
        }

        private void OnTap(InputEvent e)
        {
            if (e is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
            if (_blocked || ModalOpen) return;
            Advance();
        }

        private void Advance()
        {
            if (_typing)
            {
                _typing = false;
                _text.VisibleCharacters = -1;
                _next.Visible = true;
                return;
            }
            var page = _pages[_index];
            if (page.Event == "salt")
            {
                SaltEvent();
                return;
            }
            if (page.Event == "cat") CatRuns();
            Sound.Play("page", -6);
            _index += 1;
            if (_index >= _pages.Count) Finish();
            else ShowPage();
        }

        /// <summary>미각 0단계의 간 맞추기. 정답 구간이 보이지 않는다. 멈추면 번쩍하고 구간이 드러난다</summary>
        private void SaltEvent()
        {
            _blocked = true;
            _text.Visible = false;
            _next.Visible = false;
            Sound.Play("sizzle", -4);
            var gauge = new SeasonGauge { Target = 0.58, ZoneHalf = 0.1, ZoneVisible = false, Speed = 0.9, LessText = "", MoreText = "" };
            _gauge = gauge;
            var label = UiKit.Text(S.Ui["saltLabel"], TextSize.Body, Palette.Gold, true, false, 0, null, HorizontalAlignment.Center);
            GameButton btn = null;
            btn = new GameButton(S.Ui["salt"], Palette.Red, () =>
            {
                if (!gauge.Running) return;
                btn.SetEnabled(false);
                gauge.Stop(Palette.Gold);
                Haptics.Pulse(30, 0.5f);
                Later(0.5, () =>
                {
                    Sound.Play("good");
                    var fl = _flash.CreateTween();
                    fl.TweenProperty(_flash, "modulate:a", 0.85f, 0.08);
                    fl.TweenProperty(_flash, "modulate:a", 0f, 0.5);
                    gauge.ZoneVisible = true;
                    gauge.QueueRedraw();
                    Later(1.1, () =>
                    {
                        label.QueueFree();
                        gauge.QueueFree();
                        btn.QueueFree();
                        _gauge = null;
                        _text.Visible = true;
                        _blocked = false;
                        _index += 1;
                        ShowPage();
                    });
                });
            }, null, TextSize.H1, 120);
            _panelCol.AddChild(label);
            _panelCol.MoveChild(label, 0);
            _panelCol.AddChild(gauge);
            _panelCol.MoveChild(gauge, 1);
            _panelCol.AddChild(btn);
            _panelCol.MoveChild(btn, 2);
        }

        /// <summary>고등어가 남은 볼락을 물고 화면을 가로질러 달아난다</summary>
        private void CatRuns()
        {
            var view = GetViewportRect().Size;
            var frames = new[] { GD.Load<Texture2D>("res://art/cat/walk-1.png"), GD.Load<Texture2D>("res://art/cat/walk-2.png") };
            var cat = new TextureRect
            {
                Texture = frames[0],
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                // 걷기 그림은 오른쪽을 본다
                FlipH = false,
                MouseFilter = MouseFilterEnum.Ignore,
                Size = new Vector2(300, 257),
            };
            float y = view.Y - PanelHeight - SafeBottom - 230;
            cat.Position = new Vector2(-320, y);
            Art.AddChild(cat);
            Art.MoveChild(cat, _panel.GetIndex());
            Sound.Play("meow");
            int frame = 0;
            var step = cat.CreateTween().SetLoops(12);
            step.TweenInterval(0.12);
            step.TweenCallback(Callable.From(() => cat.Texture = frames[++frame % 2]));
            var run = cat.CreateTween();
            run.TweenProperty(cat, "position:x", view.X * 0.45f, 0.5).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            run.TweenInterval(0.3);
            run.TweenProperty(cat, "position", new Vector2(view.X + 40, y - 160), 0.6).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            run.TweenCallback(Callable.From(cat.QueueFree));
        }

        private void Later(double seconds, Action then)
        {
            GetTree().CreateTimer(seconds).Timeout += () =>
            {
                if (IsInstanceValid(this) && IsInsideTree()) then();
            };
        }

        // ───────── 끝 ─────────

        private void Finish()
        {
            _blocked = true;
            var gs = GameState.I;
            switch (_part)
            {
                case "p1":
                    // 방파제에서 첫 볼락을 낚는다 (튜토리얼 낚시)
                    gs.Tutorial = true;
                    gs.GoTo("fishing");
                    break;
                case "p2":
                    G.CompletePrologue();
                    Sound.Play("newfish");
                    Modal(S.Ui["tasteTitle"], S.Ui["tasteBody"], new[]
                    {
                        new ModalButton(S.Ui["toTown"], Palette.Green, () =>
                        {
                            gs.PendingTalk = "welcome";
                            gs.GoTo("town");
                        }),
                    });
                    break;
                case "clear":
                    gs.GoTo("town");
                    break;
                default:
                    gs.DeleteSave();
                    gs.GoTo("title");
                    break;
            }
        }

        // ───────── CI 사진 ─────────

        /// <summary>action: "p1:2" 처럼 이야기와 장면 번호. "p2:salt" 는 소금 간 맞추기</summary>
        public override void ShotSetup(string action)
        {
            var parts = action.Split(':');
            _part = parts[0];
            GameState.I.StoryPart = _part;
            _pages = S.Parts[_part];
            Sound.Music(Music);
            if (parts.Length > 1 && parts[1] == "walk")
            {
                // 사람이 누르듯 여러 장면을 넘긴다 (장면이 글 상자를 덮지 않는지 보려고)
                _index = 0;
                _look = null;
                ShowPage();
                for (int i = 0; i < 6; i++)
                {
                    Advance();
                    Advance();
                }
                return;
            }
            bool salt = parts.Length > 1 && parts[1] == "salt";
            _index = salt ? 0 : Math.Min(parts.Length > 1 ? int.Parse(parts[1]) : 0, _pages.Count - 1);
            _look = null;
            ShowPage();
            // 사진에서는 글을 다 보여 준다 (넘치는지 보려고)
            _typing = false;
            _text.VisibleCharacters = -1;
            _next.Visible = true;
            if (_skip != null) _skip.Visible = _part == "p1" || _part == "p2";
            if (salt)
            {
                _typing = false;
                SaltEvent();
                _gauge?.Place(0.4);
            }
            else if (parts.Length > 2 && parts[2] == "cat")
            {
                // 찍는 순간(2.6초 뒤) 고등어가 가운데쯤 오게
                Later(1.75, CatRuns);
            }
        }
    }
}

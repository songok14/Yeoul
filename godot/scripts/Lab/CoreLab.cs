using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Hanjeopsi.App.Boot;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Lab
{
    /// <summary>
    /// 점검 화면. 씬을 옮기기 전에 폰에서 코어 규칙을 직접 돌려 본다.
    /// 미니게임 없이 버튼으로 출조, 경매, 트럭 장사, 납품, 잠자기를 하고
    /// SD 여울·고등어와 마을 배경, 한글 글꼴, 세이브 파일이 폰에서 되는지 본다.
    /// "웹 일치 점검"은 웹 버전에서 뽑은 값과 같은지 폰에서 확인한다 (○ 같음, × 다름).
    /// </summary>
    public partial class CoreLab : Control
    {
        private const int LogLines = 30;
        private const int LogHeight = 214;

        private static readonly string[] YeoulPoses = { "front", "wave", "three-quarter", "side", "walk", "back", "idle" };
        private static readonly string[] CatPoses = { "sit-front", "meow-1", "walk", "loaf", "eat", "jump", "sleep", "stretch", "sit-side", "back" };

        // 웹 config.ts 의 글자 색
        private static readonly Color Paper = new Color("f4ecd8");
        private static readonly Color Gold = new Color("f2c14e");
        private static readonly Color Red = new Color("e0605b");
        private static readonly Color Blue = new Color("8cc8ff");
        private static readonly Color Gray = new Color("a7adb5");
        private static readonly Color Green = new Color("7fd17e");

        private GameData _data;
        private CoreGame _game;
        private IRandom _rng = new SystemRandom();
        private readonly List<string> _log = new List<string>();
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();

        private TextureRect _bg, _yeoul, _cat;
        private Label _date, _dday, _money, _headline, _cooler;
        private readonly List<Label> _prices = new List<Label>();
        private VBoxContainer _requests;
        private RichTextLabel _logView;
        private PanelContainer _bottom;
        private MarginContainer _safe;
        private Control _checks;
        private RichTextLabel _checkText;

        private int _yeoulPose;
        private int _catPose;
        private float _time;

        // 사진 모드 (CI 화면 확인용): -- --shot=<경로> [--shot-checks]
        private string _shotPath;
        private bool _shotChecks;
        private int _shotFrames;

        public override void _Ready()
        {
            GameState.I?.Reveal();
            Theme = MakeTheme();
            ReadShotArgs();
            try
            {
                _data = GameDataLoader.Load();
                if (_shotPath != null)
                {
                    StartShotGame();
                    BuildUi();
                    Refresh();
                    if (_shotChecks) ShowChecks();
                    return;
                }
                var save = SaveStore.Load(_data);
                if (save != null)
                {
                    _game = new CoreGame(_data, save);
                    Log("이어하기: " + _game.Save.Day + "일차 " + _game.SlotName());
                }
                else
                {
                    StartNewGame();
                }
                _game.Saved += SaveStore.Write;
                BuildUi();
                Refresh();
            }
            catch (Exception e)
            {
                GD.PushError(e.ToString());
                var err = MakeLabel("데이터를 읽지 못했다\n" + e.GetType().Name + ": " + e.Message, 36, Paper, true);
                err.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 40);
                AddChild(err);
            }
        }

        public override void _Process(double delta)
        {
            if (_game == null) return;
            _time += (float)delta;
            PlaceCharacters();
            if (_shotPath != null && ++_shotFrames == 30) SaveShot();
        }

        // ───────── 사진 모드 ─────────

        private void ReadShotArgs()
        {
            foreach (var a in OS.GetCmdlineUserArgs())
            {
                if (a.StartsWith("--shot=")) _shotPath = a.Substring("--shot=".Length);
                else if (a == "--shot-checks") _shotChecks = true;
            }
        }

        /// <summary>세이브를 건드리지 않는 체험 판. 시드와 난수를 고정해 매번 같은 화면이 나온다</summary>
        private void StartShotGame()
        {
            _rng = new Mulberry32(2026);
            _game = CoreGame.NewGame(_data, 12345);
            _game.CompletePrologue();
            Log("사진 모드: 시드 12345, 출조 두 번과 경매, 장사 한 번");
            GoFishing();
            SellAll();
            GoFishing();
            RunTruck();
            _yeoulPose = Array.IndexOf(YeoulPoses, "wave");
        }

        private void SaveShot()
        {
            var img = GetViewport().GetTexture().GetImage();
            var err = img.SavePng(_shotPath);
            GD.Print("사진 저장 " + _shotPath + " " + img.GetWidth() + "x" + img.GetHeight() + " " + err);
            GetTree().Quit(err == Error.Ok ? 0 : 1);
        }

        // ───────── 화면 만들기 ─────────

        private Theme MakeTheme()
        {
            var t = new Theme();
            t.DefaultFont = GD.Load<Font>("res://fonts/GowunDodum-Regular.ttf");
            t.DefaultFontSize = 38;
            t.SetColor("font_color", "Label", Paper);
            t.SetColor("default_color", "RichTextLabel", Paper);
            t.SetConstant("line_spacing", "Label", 4);
            return t;
        }

        private static StyleBoxFlat Box(Color bg, int radius, int padX, int padY)
        {
            var sb = new StyleBoxFlat { BgColor = bg };
            sb.SetCornerRadiusAll(radius);
            sb.ContentMarginLeft = padX;
            sb.ContentMarginRight = padX;
            sb.ContentMarginTop = padY;
            sb.ContentMarginBottom = padY;
            return sb;
        }

        /// <summary>기본은 한 줄. 넘치면 말줄임표. 여러 줄이 필요하면 wrap</summary>
        private static Label MakeLabel(string text, int size, Color color, bool wrap = false)
        {
            var l = new Label { Text = text };
            if (wrap) l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            else
            {
                l.AutowrapMode = TextServer.AutowrapMode.Off;
                l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
                l.ClipText = true;
            }
            l.AddThemeFontSizeOverride("font_size", size);
            l.AddThemeColorOverride("font_color", color);
            return l;
        }

        private static Button MakeButton(string text, Color color, Action onPress, int size = 36, int height = 112)
        {
            var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, height), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var normal = Box(color, 22, 12, 8);
            normal.ShadowColor = new Color(0, 0, 0, 0.35f);
            normal.ShadowSize = 6;
            normal.ShadowOffset = new Vector2(0, 5);
            var pressed = Box(color.Darkened(0.25f), 22, 12, 8);
            b.AddThemeStyleboxOverride("normal", normal);
            b.AddThemeStyleboxOverride("hover", normal);
            b.AddThemeStyleboxOverride("pressed", pressed);
            b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            b.AddThemeFontSizeOverride("font_size", size);
            b.AddThemeColorOverride("font_color", Colors.White);
            b.AddThemeColorOverride("font_hover_color", Colors.White);
            b.AddThemeColorOverride("font_pressed_color", Colors.White);
            b.AddThemeConstantOverride("line_spacing", -2);
            b.Pressed += () => onPress();
            return b;
        }

        private void BuildUi()
        {
            _bg = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
            _bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_bg);

            _yeoul = MakeCharacter(() =>
            {
                _yeoulPose = (_yeoulPose + 1) % YeoulPoses.Length;
            });
            _cat = MakeCharacter(() =>
            {
                _catPose = (_catPose + 1) % CatPoses.Length;
                Log(_catPose % 2 == 0 ? "냐옹" : "(꼬리를 흔든다)");
                RefreshLog();
            });

            _safe = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            _safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(_safe);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 14);
            _safe.AddChild(col);

            // 상태 막대
            var hud = new PanelContainer();
            hud.AddThemeStyleboxOverride("panel", Box(new Color(0.08f, 0.12f, 0.19f, 0.84f), 28, 30, 18));
            var hudCol = new VBoxContainer();
            hudCol.AddThemeConstantOverride("separation", 2);
            var top = new HBoxContainer();
            _date = MakeLabel("", 46, Paper);
            _date.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _dday = MakeLabel("", 46, Red);
            _dday.ClipText = false;
            _dday.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            top.AddChild(_date);
            top.AddChild(_dday);
            hudCol.AddChild(top);
            _money = MakeLabel("", 34, Paper);
            hudCol.AddChild(_money);
            hud.AddChild(hudCol);
            col.AddChild(hud);

            var tools = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End, MouseFilter = MouseFilterEnum.Ignore };
            tools.AddThemeConstantOverride("separation", 16);
            var newGame = MakeButton("새 게임", new Color("5c6269"), () => { StartNewGame(); Refresh(); }, 32, 76);
            newGame.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            newGame.CustomMinimumSize = new Vector2(200, 76);
            var check = MakeButton("웹 일치 점검", new Color("6a5a8a"), ShowChecks, 32, 76);
            check.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            check.CustomMinimumSize = new Vector2(240, 76);
            var back = MakeButton("타이틀로", new Color("2f5689"), () => GameState.I.GoTo("title"), 32, 76);
            back.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            back.CustomMinimumSize = new Vector2(200, 76);
            tools.AddChild(back);
            tools.AddChild(newGame);
            tools.AddChild(check);
            col.AddChild(tools);

            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            // 아래 패널
            _bottom = new PanelContainer();
            _bottom.AddThemeStyleboxOverride("panel", Box(new Color(0.12f, 0.17f, 0.24f, 0.95f), 32, 28, 24));
            var body = new VBoxContainer();
            body.AddThemeConstantOverride("separation", 10);
            _headline = MakeLabel("", 34, Gold);
            body.AddChild(_headline);
            var grid = new GridContainer { Columns = 2 };
            grid.AddThemeConstantOverride("h_separation", 24);
            grid.AddThemeConstantOverride("v_separation", 4);
            foreach (var _ in _data.Species)
            {
                var l = MakeLabel("", 32, Paper);
                l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                _prices.Add(l);
                grid.AddChild(l);
            }
            body.AddChild(grid);
            _cooler = MakeLabel("", 32, Gray);
            body.AddChild(_cooler);
            _requests = new VBoxContainer();
            _requests.AddThemeConstantOverride("separation", 0);
            body.AddChild(_requests);
            // 기록은 높이를 고정하고 마지막 줄을 따라간다. 길어져도 패널이 커지지 않는다
            _logView = new RichTextLabel { BbcodeEnabled = true, FitContent = false, ScrollActive = false, ScrollFollowing = true, ClipContents = true, CustomMinimumSize = new Vector2(0, LogHeight) };
            _logView.AddThemeFontSizeOverride("normal_font_size", 30);
            _logView.AddThemeConstantOverride("line_separation", 2);
            body.AddChild(_logView);

            var buttons = new GridContainer { Columns = 3 };
            buttons.AddThemeConstantOverride("h_separation", 18);
            buttons.AddThemeConstantOverride("v_separation", 18);
            buttons.AddChild(MakeButton("출조\n6번 던지기 · 1칸", new Color("4a7fc1"), () => Act(GoFishing)));
            buttons.AddChild(MakeButton("경매\n전부 팔기", new Color("3f7a8a"), () => Act(SellAll)));
            buttons.AddChild(MakeButton("트럭 장사\n손님 5명 · 1칸", new Color("d9534f"), () => Act(RunTruck)));
            buttons.AddChild(MakeButton("납품", new Color("3f8a4a"), () => Act(Deliver)));
            buttons.AddChild(MakeButton("고등어 밥", new Color("a8761a"), () => Act(FeedCat)));
            buttons.AddChild(MakeButton("장비 사기", new Color("2f5689"), () => Act(BuyGear)));
            buttons.AddChild(MakeButton("시간 보내기", new Color("5e3b26"), () => Act(NextSlot)));
            buttons.AddChild(MakeButton("잠자기", new Color("1b2a41"), () => Act(Sleep)));
            buttons.AddChild(MakeButton("빚 10만 원\n갚기", new Color("8a5a3b"), () => Act(Repay)));
            body.AddChild(buttons);
            _bottom.AddChild(body);
            col.AddChild(_bottom);

            BuildChecks();
            GetViewport().SizeChanged += ApplySafeArea;
            ApplySafeArea();
        }

        private TextureRect MakeCharacter(Action onTap)
        {
            var t = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspect, MouseFilter = MouseFilterEnum.Stop };
            t.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) onTap();
            };
            AddChild(t);
            return t;
        }

        /// <summary>노치와 아래 제스처 바를 피한다. 화면 픽셀을 뷰포트 좌표로 바꿔 여백을 준다</summary>
        private void ApplySafeArea()
        {
            var view = GetViewportRect().Size;
            var win = DisplayServer.WindowGetSize();
            var safe = DisplayServer.GetDisplaySafeArea();
            float k = win.X > 0 ? view.X / win.X : 1;
            int topPad = Mathf.Max(18, (int)(safe.Position.Y * k) + 10);
            int bottomPad = Mathf.Max(18, (int)((win.Y - safe.End.Y) * k) + 10);
            _safe.AddThemeConstantOverride("margin_left", 20);
            _safe.AddThemeConstantOverride("margin_right", 20);
            _safe.AddThemeConstantOverride("margin_top", topPad);
            _safe.AddThemeConstantOverride("margin_bottom", bottomPad);
        }

        private void BuildChecks()
        {
            _checks = new Control { Visible = false };
            _checks.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
            dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _checks.AddChild(dim);
            var center = new CenterContainer();
            center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _checks.AddChild(center);
            var panel = new PanelContainer { CustomMinimumSize = new Vector2(980, 0) };
            panel.AddThemeStyleboxOverride("panel", Box(new Color("f4ecd8"), 28, 40, 34));
            var col = new VBoxContainer();
            col.AddThemeConstantOverride("separation", 20);
            col.AddChild(MakeLabel("웹 일치 점검", 50, new Color("2b2b2b"), true));
            _checkText = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false };
            _checkText.AddThemeFontSizeOverride("normal_font_size", 33);
            _checkText.AddThemeColorOverride("default_color", new Color("2b2b2b"));
            col.AddChild(_checkText);
            col.AddChild(MakeButton("닫기", new Color("4a7fc1"), () => _checks.Visible = false, 38, 100));
            panel.AddChild(col);
            center.AddChild(panel);
            AddChild(_checks);
        }

        private Texture2D Tex(string path)
        {
            Texture2D t;
            if (!_tex.TryGetValue(path, out t))
            {
                t = GD.Load<Texture2D>("res://art/" + path + ".png");
                _tex[path] = t;
            }
            return t;
        }

        /// <summary>여울과 고등어를 아래 패널 위에 세운다. 발이 같은 바닥선에 닿는다</summary>
        private void PlaceCharacters()
        {
            if (_bottom == null) return;
            float w = GetViewportRect().Size.X;
            float floor = _bottom.GetGlobalRect().Position.Y + 18;
            float hudBottom = _safe.GetThemeConstant("margin_top") + 260;
            float room = Mathf.Max(160, floor - hudBottom);

            string yp = YeoulPoses[_yeoulPose];
            if (yp == "walk") yp = Mathf.FloorToInt(_time / 0.35f) % 2 == 0 ? "walk-1" : "walk-2";
            float bob = yp.StartsWith("walk") ? 0 : Mathf.Sin(_time * 2.2f) * 5;
            Stand(_yeoul, Tex("yeoul/" + yp), new Vector2(w * 0.36f, floor), Mathf.Min(420, room), 490, bob);

            string cp = CatPoses[_catPose];
            if (cp == "walk") cp = Mathf.FloorToInt(_time / 0.3f) % 2 == 0 ? "walk-1" : "walk-2";
            Stand(_cat, Tex("cat/" + cp), new Vector2(w * 0.7f, floor), Mathf.Min(420, room) * 0.53f, 310, 0);
        }

        /// <summary>시트 원래 비율을 지키려고 기준 높이(여울 490px, 고양이 310px)로 맞춘다</summary>
        private static void Stand(TextureRect r, Texture2D t, Vector2 foot, float height, float refHeight, float bob)
        {
            if (t == null) return;
            r.Texture = t;
            float k = height / refHeight;
            var size = new Vector2(t.GetWidth() * k, t.GetHeight() * k);
            r.Size = size;
            r.Position = new Vector2(foot.X - size.X / 2, foot.Y - size.Y + bob);
        }

        private void Refresh()
        {
            if (_date == null) return;
            var s = _game.Save;
            _bg.Texture = Tex("backgrounds/village-" + Mathf.Clamp(s.Slot, 0, 3));
            _date.Text = _game.DateLabel() + " · " + s.Day + "일차 · " + _game.SlotName();
            _dday.Text = "D-" + Math.Max(0, _game.DaysLeft());
            _money.Text = JsMath.Won(s.Money) + "   빚 " + JsMath.Won(s.Debt) + "   미끼 " + s.Bait + "   미각 " + _game.Taste.Stage(_game.TasteLevel()).Name;

            _headline.Text = "오늘 시세 · " + _game.Market.Headline(s.Day);
            for (int i = 0; i < _data.Species.Count; i++)
            {
                var sp = _data.Species[i];
                int c = _game.Market.ChangePercent(sp.Id, s.Day);
                string arrow = c > 0 ? " ▲" + c + "%" : c < 0 ? " ▼" + (-c) + "%" : "";
                _prices[i].Text = sp.Name + "  " + JsMath.Won(_game.Market.TodayBasePrice(sp.Id, s.Day)) + arrow;
                _prices[i].AddThemeColorOverride("font_color", c > 0 ? new Color("ffb0a8") : c < 0 ? Blue : Paper);
            }

            var stages = s.Cooler.GroupBy(f => _game.Fresh.StageOf(f.Fresh)).OrderBy(g => g.Key).Select(g => _game.Fresh.StageName(g.Key) + " " + g.Count());
            _cooler.Text = "쿨러 " + s.Cooler.Count + "/" + _game.CoolerCapacity() + "  " + string.Join(" · ", stages) + "   고등어 호감 " + s.CatAffinity;

            foreach (var child in _requests.GetChildren()) child.QueueFree();
            foreach (var r in s.Requests)
            {
                var t = _data.RequestTemplates[r.Template];
                int left = r.Deadline - s.Day;
                _requests.AddChild(MakeLabel("의뢰 · " + t.Client + ": " + _data.SpeciesById(t.Species).Name + " " + t.Count + "마리 · " + (left <= 0 ? "오늘까지" : "D-" + left) + " · " + JsMath.Won(r.Reward), 31, left <= 1 ? Red : Gray));
            }
            RefreshLog();
        }

        private void RefreshLog()
        {
            if (_logView == null) return;
            _logView.Text = string.Join("\n", _log.Skip(Math.Max(0, _log.Count - LogLines)).Select(l => Esc(l)));
        }

        private static string Esc(string s)
        {
            return s.Replace("[", "[lb]");
        }

        // ───────── 행동 ─────────

        private void Act(Action action)
        {
            action();
            Refresh();
        }

        private void StartNewGame()
        {
            if (_game != null) _game.Saved -= SaveStore.Write;
            _game = CoreGame.NewGame(_data, (int)(GD.Randi() % 999999999) + 1);
            _game.CompletePrologue();
            _game.Saved += SaveStore.Write;
            SaveStore.Write(_game.Save);
            _log.Clear();
            Log("새 게임 (프롤로그는 건너뜀). 시드 " + _game.Save.Seed);
        }

        /// <summary>출조 한 번 = 캐스팅 6번 = 시간 한 칸. 미니게임 대신 던지면 다 낚는다</summary>
        private void GoFishing()
        {
            var s = _game.Save;
            int caught = 0;
            for (int i = 0; i < _data.Balance.Chapter1.CastsPerTrip; i++)
            {
                if (s.Bait <= 0)
                {
                    Log("미끼가 떨어졌다. 장비 사기에서 미끼를 산다");
                    break;
                }
                _game.UseBait();
                bool hot = _rng.RangeInt(-1, 1) == _rng.RangeInt(-1, 1);
                int zone = _game.Fishing.ZoneOf(_rng.Next());
                var sp = _game.Fishing.RollSpecies(zone, s.Slot, hot, _rng);
                double size = _game.Fishing.RollSize(sp, hot, _rng);
                string reason = _game.ReleaseReason(sp.Id, size);
                bool released = reason != null || _game.CoolerFull();
                bool isNew = _game.RecordCatch(sp.Id, size, released);
                if (!released) _game.AddToCooler(sp.Id, size);
                caught++;
                string line = sp.Name + " " + size.ToString("0.0", CultureInfo.InvariantCulture) + "cm" + (isNew ? " (새 어종!)" : "");
                if (reason != null) line += " → " + reason;
                else if (released) line += " → 쿨러가 가득 차서 놓아줬다";
                Log(line);
                var unlocked = _game.CheckTasteUnlock();
                if (unlocked != null) Log("미각이 돌아온다: " + unlocked.Name + ". " + unlocked.Effect);
            }
            if (caught > 0)
            {
                _game.UseSlot();
                AfterTime();
            }
        }

        private void SellAll()
        {
            if (!_game.IsMarketOpen())
            {
                Log("경매는 새벽과 아침에만 열린다");
                return;
            }
            var uids = _game.Save.Cooler.Where(f => _game.Fresh.StageOf(f.Fresh) != FreshStage.Spoiled).Select(f => f.Uid).ToList();
            if (uids.Count == 0)
            {
                Log("팔 물고기가 없다");
                return;
            }
            double mul = _rng.Range(0.92, 1.15);
            int total = _game.SellFish(uids, mul);
            Log("경매 " + uids.Count + "마리 낙찰 " + JsMath.Won(total) + " (시세의 " + Math.Round(mul * 100) + "%)");
        }

        /// <summary>장사 한 번 = 손님 5명 = 시간 한 칸. 아는 맛은 정답 근처, 모르는 맛은 감으로 멈춘다</summary>
        private void RunTruck()
        {
            if (!_game.IsTruckOpen())
            {
                Log("새벽엔 손님이 없다");
                return;
            }
            if (_game.Truck.Cookable(_game.Save).Count == 0)
            {
                Log("쿨러에 요리할 재료가 없다");
                return;
            }
            int served = 0, sales = 0;
            for (int i = 0; i < _data.Balance.Chapter1.CustomersPerShift; i++)
            {
                var menu = _game.Truck.Cookable(_game.Save);
                if (menu.Count == 0) break;
                var recipe = menu[(int)(_rng.Next() * menu.Count)];
                var results = new List<SeasonResult>();
                foreach (var step in recipe.Steps)
                {
                    double target = _game.Truck.RollTarget(step, _rng);
                    bool known = _game.Taste.CanTaste(_game.Save, step.Flavor);
                    double pos = known ? JsMath.Clamp(target + _rng.Range(-0.12, 0.12), 0, 1) : _rng.Next();
                    results.Add(_game.Truck.Judge(pos, target));
                }
                var dish = _game.ServeDish(recipe, results);
                if (dish == null) break;
                served++;
                sales += dish.Pay;
                var lines = _game.Truck.ReviewLines(_game.Save, recipe, results, dish.Stage, _game.Truck.MoodOf(dish.Avg));
                Log(recipe.Name + " " + JsMath.Won(dish.Pay) + " \"" + string.Join(" ", lines) + "\"");
            }
            Log("장사 끝: 손님 " + served + "명, 매출 " + JsMath.Won(sales));
            _game.UseSlot();
            AfterTime();
        }

        private void Deliver()
        {
            foreach (var r in _game.Save.Requests)
            {
                if (_game.FishForRequest(r.Id) == null) continue;
                var t = _data.RequestTemplates[r.Template];
                Log(t.Client + "에게 납품 " + JsMath.Won(_game.DeliverRequest(r.Id)));
                return;
            }
            Log("납품할 수 있는 의뢰가 없다 (선어 이상, 마릿수)");
        }

        private void FeedCat()
        {
            int n = _game.FeedCat();
            Log(n > 0 ? "고등어가 골골거린다 (호감 " + _game.Save.CatAffinity + "/" + _data.Balance.Cat.MaxAffinity + ")" : "상한 생선이 없다");
            if (n > 0) _catPose = Array.IndexOf(CatPoses, "eat");
        }

        /// <summary>살 수 있는 다음 단계 장비 중 가장 싼 것. 못 사면 미끼라도</summary>
        private void BuyGear()
        {
            GearSlot? best = null;
            int bestPrice = int.MaxValue;
            foreach (var slot in new[] { GearSlot.Rod, GearSlot.Reel, GearSlot.Tackle })
            {
                int next = _game.GearLevel(slot) + 1;
                if (next >= _game.GearCount(slot)) continue;
                int price = _game.GearPrice(slot, next);
                if (price < bestPrice)
                {
                    bestPrice = price;
                    best = slot;
                }
            }
            if (best != null && _game.UpgradeGear(best.Value)) Log(GearName(best.Value) + " 장만! (" + JsMath.Won(bestPrice) + ")");
            else if (_game.BuyBait()) Log("장비는 아직 비싸서 미끼만 샀다 (+" + _data.Gear.Bait.Amount + ")");
            else Log("돈이 모자라다");
        }

        private string GearName(GearSlot slot)
        {
            var g = _data.Gear;
            switch (slot)
            {
                case GearSlot.Rod: return g.Rods[_game.Save.Rod].Name;
                case GearSlot.Reel: return g.Reels[_game.Save.Reel].Name;
                default: return g.Tackles[_game.Save.Tackle].Name;
            }
        }

        private void Repay()
        {
            int paid = _game.Repay(_data.Balance.Chapter1.RepayStep);
            Log(paid > 0 ? "빚을 " + JsMath.Won(paid) + " 갚았다. 할머니가 말없이 손을 잡아 주셨다" : "갚을 돈이 없다");
            AfterTime();
        }

        private void NextSlot()
        {
            _game.UseSlot();
            AfterTime();
        }

        private void Sleep()
        {
            _game.EndDay();
            AfterTime();
        }

        /// <summary>시간이 지난 뒤: 어제 요약과 1장 결말 확인 (웹 TownScene)</summary>
        private void AfterTime()
        {
            var s = _game.Save;
            if (s.PendingSummary != null)
            {
                var sum = s.PendingSummary;
                s.PendingSummary = null;
                string line = s.Day + "일차가 밝았다. 어제 번 돈 " + JsMath.Won(sum.Earned);
                if (sum.Spoiled > 0) line += ", " + sum.Spoiled + "마리 상함";
                if (sum.ExpiredRequests > 0) line += ", 의뢰 " + sum.ExpiredRequests + "건 놓침";
                Log(line);
                _game.Persist();
            }
            var outcome = _game.CheckChapter();
            if (outcome == ChapterOutcome.Clear)
            {
                _game.Persist();
                Log("1차 상환 완료! 조황일지 1장을 얻었다");
            }
            else if (outcome == ChapterOutcome.Fail)
            {
                SaveStore.Delete();
                StartNewGame();
                Log("30일이 지났다. 여울식당에 빨간 딱지가 붙었다. 처음부터 다시");
            }
        }

        private void Log(string line)
        {
            _log.Add(line);
            while (_log.Count > 40) _log.RemoveAt(0);
        }

        // ───────── 웹 일치 점검 ─────────

        private void ShowChecks()
        {
            var lines = RunChecks();
            int ok = lines.Count(c => c.StartsWith("○"));
            _checkText.Text = "[b]" + ok + " / " + lines.Count + " 같음[/b]\n\n" + string.Join("\n", lines.Select(l => (l.StartsWith("○") ? "[color=#3f8a4a]" : "[color=#9e3330]") + Esc(l) + "[/color]"));
            _checks.Visible = true;
        }

        /// <summary>웹 버전 코드로 뽑은 값(core/golden)이 폰에서도 같은지</summary>
        private List<string> RunChecks()
        {
            var list = new List<string>();
            void Check(string label, bool ok) => list.Add((ok ? "○ " : "× ") + label);
            var d = _data;
            var fresh = new Freshness(d);
            // 어종은 FishingPlan 도감 71종 (앞 다섯은 웹과 같은 순서라 시세 시드가 같다)
            Check("데이터: 어종 " + d.Species.Count + " (웹 5종 먼저) · 요리 5 · 의뢰 8", d.Species.Count == 71 && d.Species.Take(5).Select(x => x.Id).SequenceEqual(new[] { "bollak", "godeungeo", "noraemi", "hakgongchi", "gamseongdom" }) && d.Recipes.Recipes.Count == 5 && d.RequestTemplates.Count == 8);
            Check("난수 mulberry32(12345) 첫 값", new Mulberry32(12345).Next() == 0.9797282677609473);

            var m = new Market(d, fresh, 12345);
            int[] prices = d.Species.Select(sp => m.TodayBasePrice(sp.Id, 4)).ToArray();
            Check("시세 시드 12345 · 4일차 " + string.Join(", ", prices), prices.SequenceEqual(new[] { 7900, 3400, 3800, 4900, 46400 }));
            var web5 = _game.Data.Species.Take(5).ToList();
            Check("소식 \"" + m.Headline(4, web5) + "\"", m.Headline(4, web5) == "볼락 값이 38% 뛰었다");
            int v1 = m.FishValue(new FishItem(0, "noraemi", 23.6, 1, 66), 28);
            int v2 = new Market(d, fresh, 987654321).FishValue(new FishItem(0, "bollak", 16.3, 1, 68), 37);
            Check("물고기 값 " + JsMath.Won(v1) + ", " + JsMath.Won(v2), v1 == 2200 && v2 == 6600);

            var g = CoreGame.NewGame(d, 12345);
            var req = g.Save.Requests;
            Check("새 게임 첫 의뢰 보상 " + string.Join(", ", req.Select(r => JsMath.Won(r.Reward))),
                req.Count == 2 && req[0].Template == 3 && req[0].Reward == 19400 && req[0].Deadline == 5 && req[1].Template == 7 && req[1].Reward == 19200 && req[1].Deadline == 6);

            var rules = new Rules(d);
            Check("16일차는 " + rules.DateLabel(16), rules.DateLabel(16) == "5월 1일");
            Check("감성돔 20cm: " + rules.ReleaseReason("gamseongdom", 20, 16), rules.ReleaseReason("gamseongdom", 20, 16) == "금지체장 25cm 이하라 놓아줘야 한다");
            Check("감성돔 30cm 5월: " + rules.ReleaseReason("gamseongdom", 30, 16), rules.ReleaseReason("gamseongdom", 30, 16) == "금어기(5/1~5/31)라 놓아줘야 한다");

            string json = SaveJson.ToJson(_game.Save);
            var back = SaveJson.FromJson(json, d);
            Check("세이브 JSON 왕복 (" + json.Length + "자)", back != null && SaveJson.ToJson(back) == json);
            if (_shotPath != null) Check("세이브 파일 (사진 모드라 건너뜀)", true);
            else Check("세이브 파일 " + (SaveStore.Has() ? "있음" : "없음"), SaveStore.Has());
            return list;
        }
    }
}

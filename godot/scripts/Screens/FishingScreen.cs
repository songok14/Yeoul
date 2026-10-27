using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 방파제 찌낚시 (웹 FishingScene). 부채꼴 방향 바늘 → 파워 게이지 → 입질 대기(속임 입질, 고등어 도둑) → 챔질 → 릴링 → 결과 → 철수.
    /// 고등어가 가리키는 반짝이는 지점에 가까이 던질수록 귀한 고기와 큰 놈이 잘 온다. 입질 시간은 어디든 같다.
    /// 판정은 코어 SpotRules·ReelRules·BiteWait·TugFight(힘싸움) 가 하고, 여기서는 그리기, 소리, 진동, 입력만 한다.
    /// 여울은 SD 뒷모습, 처음 낚은 어종은 감정 일러스트 대화로 기뻐한다.
    /// </summary>
    public partial class FishingScreen : Screen
    {
        protected override string Music
        {
            get { return "sea"; }
        }

        private enum Phase
        {
            /// <summary>던지기 단추를 기다린다 (재촉하지 않게 바늘은 아직 안 움직인다)</summary>
            Ready,
            Aim,
            Power,
            Flying,
            Wait,
            Bite,
            Reel,
            Result,
            Busy,
        }

        // 배경 그림(방파제) 기준 좌표 (웹과 같다)
        private const float Horizon = 290;
        private const float Shore = 560;
        private const float PanelHeight = 300;
        // 여울은 방파제 가운데 서서 바다를 본다. 고등어는 왼쪽, 쿨러는 오른쪽
        private static readonly Vector2 YeoulFoot = new Vector2(225, 690);
        /// <summary>던지기 부채꼴이 퍼지기 시작하는 곳 (여울 앞 물가, 방파제 가운데)</summary>
        private static readonly Vector2 FanApex = new Vector2(225, 560);
        private const float YeoulHeight = 168;
        private static readonly Vector2 CatFoot = new Vector2(140, 686);
        // 쿨러는 여울 오른쪽 방파제 위 (긴 화면에서도 잘리지 않게). 고등어는 여울 앞을 지나 쿨러 옆까지 살금살금 간다
        private static readonly Vector2 CoolerFoot = new Vector2(318, 668);
        private static readonly Vector2 CatThiefFoot = new Vector2(280, 682);
        // 시간대 [새벽, 아침, 낮, 저녁] 배경 색
        // 새벽 · 아침 · 점심 · 저녁 · 밤
        private static readonly Color[] SlotTint = { new Color("c4c0e2"), new Color("fff4e6"), Colors.White, new Color("ffcfb2"), new Color("5a66a6") };

        private CoreGame G
        {
            get { return GameState.I.Game; }
        }

        private Dictionary<string, string> T
        {
            get { return G.Data.Texts.Fishing; }
        }

        private ArtStage _stage;
        private FishingField _field;
        private Hud _hud;
        private PanelContainer _panel;
        private Label _msg, _sub, _info;
        private ReelGauge _gauge;
        private GameButton _exit;
        private TextureRect _yeoul, _cat;
        private Control _catBubble;

        private Phase _phase;
        private int _castsLeft;
        private int _castsDone;
        private int _caught;
        // 방향 바늘 (-1 왼쪽 ~ 1 오른쪽)
        private double _angle;
        private int _aimDir = 1;
        private CastSpot _hotSpot, _hintSpot;
        /// <summary>찌가 떨어진 자리가 물 좋은 지점에 가까운 정도 (0~1)</summary>
        private double _close;
        private bool _big;
        // 고등어 도둑: 찌가 떨어지고 몇 초 뒤 (없으면 -1), 진행 시간
        private double _thiefAt = -1;
        private float _thiefT;
        private bool _thiefActive, _thiefDone, _freezeThief;
        private float _waitT;
        private float _buzz;

        private double _gaugePos;
        private int _gaugeDir = 1;
        private double _dist01;
        private Vector2 _target;
        private float _targetScale;
        private BiteWait _wait;
        private double _biteWindow;
        private double _biteElapsed;
        private Species _species;
        private double _size;
        private TugFight _fight;
        private bool _holding;
        /// <summary>누른 자리: -1 화면 왼쪽 절반, 1 오른쪽 절반</summary>
        private int _side = 1;
        /// <summary>손맛 세기 0.15~1 (진동). 입질 때 어종·크기로 정한다</summary>
        private float _feel = 0.3f;
        /// <summary>질주 때 물고기 자리가 옆으로 끌려간 정도 (논리 x)</summary>
        private float _runOffset;
        private float _braceBuzz;
        private bool _autoHold;
        private float _wobble;
        private float _time;
        private float _bobT;
        private float _shake, _shakeAmp = 1;
        /// <summary>힘싸움 중 물고기가 머리를 흔드는 다음 때, 질주 중 드랙이 딸깍이는 다음 때</summary>
        private float _headShakeIn, _dragTick;
        /// <summary>입질 때 머리 위 "!"</summary>
        private Label _mark;
        private GameButton _throw;
        private VBoxContainer _quests;

        private void RefreshQuests()
        {
            if (_tutorial || _quests == null) return;
            foreach (Node c in _quests.GetChildren()) c.QueueFree();
            // 바다를 가리지 않게 한 줄로
            string line = TownScreen.QuestSummary(G);
            if (line != null) _quests.AddChild(TownScreen.TrackerPill(line));
        }
        private Tween _zoomTween, _nibbleTween;
        // SD 포즈와 기울기(도). 기울기는 목표값을 부드럽게 따라간다
        private string _pose;
        private float _lean, _leanTarget, _leanRate = 10;
        /// <summary>포즈를 좌우로 뒤집었나 (찌·물고기가 왼쪽)</summary>
        private bool _poseFlip;
        // 옆으로 버티는 포즈를 잠깐 유지하는 시간 (텐션이 오르내릴 때 깜빡이지 않게)
        private float _pullHold;
        private bool _shotPull;
        /// <summary>기다리는 동안 다음 작은 볼거리까지 (먼 물이 톡 튀거나 고등어가 딴짓을 한다)</summary>
        private float _idleT;
        private bool _catIdle;
        /// <summary>프롤로그 튜토리얼: 저녁 방파제, 볼락 한 마리, 고등어와 쿨러는 아직 없다, 줄이 터질 뻔하면 구해 준다</summary>
        private bool _tutorial;

        protected override void Build()
        {
            var s = G.Save;
            _tutorial = GameState.I.Tutorial;
            _castsLeft = _tutorial ? 1 : G.Data.Balance.Chapter1.CastsPerTrip;

            // ── 그림: 방파제 배경, 여울 뒷모습(SD), 고등어 ──
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            int tintSlot = _tutorial ? 3 : Mathf.Clamp(s.Slot, 0, SlotTint.Length - 1);
            bool night = tintSlot == SlotTint.Length - 1;
            // 밤 그림(breakwater-night)이 있으면 그대로 쓰고, 없으면 낮 그림을 흰 구름까지 어둡게 칠한다
            bool nightArt = night && (ResourceLoader.Exists("res://art/backgrounds/breakwater-night.png") || ResourceLoader.Exists("res://art/backgrounds/breakwater-night.webp"));
            _stage.Background = ArtStage.LoadBackground(nightArt ? "breakwater-night" : "breakwater");
            _stage.KeepClouds = !night;
            _stage.Tint = nightArt ? Colors.White : SlotTint[tintSlot];
            // 날씨: 눈은 눈 그림, 비는 어둑하게 칠하고 빗줄기, 안개는 수평선에 해무 (튜토리얼은 맑은 저녁)
            string weather = _tutorial ? "clear" : G.WeatherToday().Id;
            Control fx = null;
            if (weather == "snow")
            {
                _stage.Background = ArtStage.LoadBackground(night ? "breakwater-snow-night" : "breakwater-snow");
                _stage.Tint = night ? Colors.White : SlotTint[tintSlot];
                fx = new SnowOverlay();
            }
            else if (weather == "rain")
            {
                _stage.KeepClouds = false;
                _stage.Tint = _stage.Tint * new Color("9ca6b8");
                fx = new RainOverlay { Intensity = 2.6f };
            }
            else if (weather == "fog")
            {
                // 해무: 섬과 수평선이 하얗게 묻히고 발밑 바다는 보인다
                _stage.KeepClouds = false;
                fx = new FogOverlay { Center = 0.3f, Strength = 1.6f, Veil = night ? 0.45f : 0.62f, Tone = night ? new Color("7c86a8") : new Color("eef0f2") };
            }
            if (fx != null)
            {
                fx.MouseFilter = MouseFilterEnum.Ignore;
                fx.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _stage.AddChild(fx);
            }
            Art.AddChild(_stage);
            // 하늘에 갈매기 (밤과 궂은 날은 빼고)
            if (!night && weather != "typhoon" && weather != "snow" && weather != "rain")
            {
                var gulls = new Gulls(_stage, 3, tintSlot == 3 ? new Color("4a3a40") : new Color("33404d"));
                gulls.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                _stage.AddChild(gulls);
            }
            // 발밑 그림자 (방파제 위에 떠 보이지 않게)
            var shadows = new FootShadows(_stage);
            shadows.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.AddChild(shadows);
            _cat = _stage.Place(GD.Load<Texture2D>("res://art/cat/sit-side.png"), CatFoot, 62);
            _yeoul = _stage.Place(GD.Load<Texture2D>("res://art/yeoul/fish-ready.png"), YeoulFoot, YeoulHeight);
            SetPose("fish-ready");
            shadows.Add(_yeoul, 62);
            shadows.Add(_cat, 44);

            _field = new FishingField(_stage)
            {
                PosAt = PosFor,
                CoolerAt = _tutorial ? Vector2.Zero : CoolerFoot,
            };
            // 고등어는 프롤로그 2부에서 처음 만난다
            _cat.Visible = !_tutorial;
            _field.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _field.GuiInput += OnFieldInput;
            Art.AddChild(_field);

            // ── 아래 안내판: 큰 글, 작은 글, 게이지 ──
            _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 8);
            _msg = UiKit.Text("", TextSize.H1, Palette.Paper, true, true, 0, null, HorizontalAlignment.Center);
            _sub = UiKit.Text("", TextSize.Small, Palette.GrayText, false, true, 0, null, HorizontalAlignment.Center);
            _gauge = new ReelGauge();
            _gauge.ZoneNames = G.Data.Texts.Zones.ToArray();
            col.AddChild(_msg);
            col.AddChild(_sub);
            col.AddChild(_gauge);
            _throw = new GameButton(T["throw"], Palette.Red, BeginAim, null, TextSize.H1, 120)
            {
                CustomMinimumSize = new Vector2(440, 120),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                Visible = false,
            };
            col.AddChild(_throw);
            _panel.AddChild(col);
            Art.AddChild(_panel);

            // ── 위: HUD, 낚시 정보, 철수 ──
            var top = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            top.AddThemeConstantOverride("separation", 14);
            Safe.AddChild(top);
            _hud = new Hud(G);
            top.AddChild(_hud);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 16);
            var rowPad = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            rowPad.AddThemeConstantOverride("margin_left", 34);
            rowPad.AddThemeConstantOverride("margin_right", 30);
            rowPad.AddChild(row);
            top.AddChild(rowPad);
            // 맡은 의뢰 추적 카드
            _quests = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _quests.AddThemeConstantOverride("separation", 6);
            top.AddChild(_quests);
            RefreshQuests();
            _info = UiKit.Text("", TextSize.Small, Palette.Paper, false, false, 8, new Color(Palette.NightDeep, 0.8f));
            _info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _info.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(_info);
            _exit = new GameButton(T["exit"], Palette.GrayDark, ConfirmExit, null, TextSize.Small, 96)
            {
                CustomMinimumSize = new Vector2(190, 96),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            };
            row.AddChild(_exit);

            OnSafeAreaChanged();
            // 위는 내려오고 아래 안내판은 올라온다
            _hud.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetParallel(true);
            tw.TweenProperty(_hud, "modulate:a", 1f, 0.4).SetDelay(0.1);
            _panel.Modulate = new Color(1, 1, 1, 0);
            tw.TweenProperty(_panel, "modulate:a", 1f, 0.4).SetDelay(0.2);

            StartAim();
        }

        protected override void OnSafeAreaChanged()
        {
            if (_panel == null) return;
            _panel.OffsetTop = -(PanelHeight + SafeBottom);
            var sb = UiKit.Box(new Color(Palette.NightDeep, 0.9f), 0, 56, 26).WithShadow(0.35f, 22, -4);
            sb.CornerRadiusTopLeft = 46;
            sb.CornerRadiusTopRight = 46;
            sb.BorderColor = Palette.Gold;
            sb.BorderWidthTop = 4;
            sb.ContentMarginBottom = 30 + SafeBottom;
            _panel.AddThemeStyleboxOverride("panel", sb);
        }

        // ───────── 좌표와 글 ─────────

        /// <summary>
        /// 방향(-1~1)과 거리(0 여울 발밑, 1 수평선)로 물 위 자리. z 는 그 거리에서의 크기.
        /// 방파제 가운데(FanApex)에서 좌우 대칭 부채꼴로 퍼진다: 멀수록 좌우로 넓고, 가운데 줄은 똑바로 바다로 향한다
        /// </summary>
        private static Vector3 PosFor(float angle, float d)
        {
            float cx = FanApex.X;
            float spread = d * 215 * (1 - 0.3f * d);
            // 같은 거리라도 옆으로 갈수록 조금 가까이(아래로) 와서 끝이 둥근 부채꼴이 된다
            return new Vector3(
                cx + angle * spread,
                Shore - 14 - d * (Shore - Horizon - 40) + angle * angle * d * 46,
                1.6f - d * 0.9f);
        }

        private string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T[key], args);
        }

        /// <summary>SD 포즈를 바꾼다. 포즈마다 그림 크기가 달라도 키와 발 위치는 그대로</summary>
        private void SetPose(string name)
        {
            if (_pose == name) return;
            _pose = name;
            var p = SdPoses.Get(name);
            var tex = GD.Load<Texture2D>("res://art/yeoul/" + name + ".png");
            float height = tex.GetHeight() * YeoulHeight / p.Body;
            float fx = p.Foot[0] / tex.GetWidth();
            _yeoul.FlipH = _poseFlip;
            _stage.Swap(_yeoul, tex, height, new Vector2(_poseFlip ? 1 - fx : fx, p.Foot[1] / tex.GetHeight()));
        }

        /// <summary>
        /// 포즈 그림은 대를 오른쪽으로 내민다. 찌나 물고기가 왼쪽에 있으면 좌우를 뒤집어 그쪽을 본다.
        /// 가운데 근처에서 깜빡이지 않게 넘어가는 자리를 조금 벌려 둔다
        /// </summary>
        private void FaceToward(float x)
        {
            bool flip = _poseFlip ? x < YeoulFoot.X + 25 : x < YeoulFoot.X - 25;
            if (flip == _poseFlip) return;
            _poseFlip = flip;
            _lean = -_lean;
            string pose = _pose;
            _pose = null;
            SetPose(pose);
        }

        /// <summary>포즈 쪽 기울기 부호 (뒤집었으면 -1)</summary>
        private float Face
        {
            get { return _poseFlip ? -1 : 1; }
        }

        /// <summary>그림 픽셀을 뒤집은 포즈에 맞춘다</summary>
        private Vector2 Mirror(Vector2 px)
        {
            return _poseFlip ? new Vector2(_yeoul.Texture.GetWidth() - px.X, px.Y) : px;
        }

        /// <summary>그림 속 낚싯대 끝 (논리 좌표). 기울기와 숨쉬기까지 반영한다</summary>
        private Vector2 RodTip()
        {
            var tex = _yeoul.Texture;
            var p = SdPoses.Get(_pose);
            var local = Mirror(p.TipPx) * (_yeoul.Size / tex.GetSize());
            return _stage.ToLogical(_yeoul.GetTransform() * local);
        }

        /// <summary>그림 픽셀 → 논리 좌표 (기울기와 숨쉬기 반영)</summary>
        private Vector2 PoseToLogical(Vector2 px)
        {
            var local = px * (_yeoul.Size / _yeoul.Texture.GetSize());
            return _stage.ToLogical(_yeoul.GetTransform() * local);
        }

        /// <summary>감기·버티기 포즈는 대를 화면이 그린다: 손잡이, 방향, 길이를 바다 그림에 넘긴다</summary>
        private void UpdateRod()
        {
            var p = SdPoses.Get(_pose);
            _field.RodDrawn = p.DrawnRod;
            if (!p.DrawnRod)
            {
                _field.RodTip = RodTip();
                return;
            }
            var grip = new Vector2(p.Grip[0], p.Grip[1]);
            var end = grip + new Vector2(p.RodDir[0], p.RodDir[1]) * p.RodLen;
            var g = PoseToLogical(Mirror(grip));
            var e = PoseToLogical(Mirror(end));
            _field.RodGrip = g;
            _field.RodDir = (e - g).Normalized();
            _field.RodLen = (e - g).Length();
        }

        private void Tell(string main, string sub = "", Color? color = null)
        {
            _msg.Text = main;
            _msg.LabelSettings.FontColor = color ?? Palette.Paper;
            _sub.Text = sub;
            _sub.Visible = !string.IsNullOrEmpty(sub);
        }

        private void SetInfo()
        {
            var s = G.Save;
            if (_tutorial)
            {
                _info.Text = T["tutorialInfo"];
                _exit.Visible = false;
                return;
            }
            var bait = G.BaitNow();
            _info.Text = F("info", _castsLeft, G.Data.Balance.Chapter1.CastsPerTrip, (bait != null ? bait.Name + " " : "") + G.BaitLeft(), s.Cooler.Count, G.CoolerCapacity());
            _hud.Refresh();
        }

        private double Reach
        {
            get { return G.EffectiveRod().Reach; }
        }

        /// <summary>화면이 바뀐 뒤 불려도 괜찮은 지연 호출</summary>
        private void Later(double seconds, Action then)
        {
            GetTree().CreateTimer(seconds).Timeout += () =>
            {
                if (IsInstanceValid(this) && IsInsideTree()) then();
            };
        }

        /// <summary>손맛 세기에 맞춘 진동. 작은 고기는 짧고 약하게, 큰 고기는 길고 세게</summary>
        private void Buzz(int ms, float amp)
        {
            Haptics.Pulse((int)(ms * (0.6f + 0.6f * _feel)), amp * (0.3f + 0.7f * _feel));
        }

        // ───────── 입력 ─────────

        private void OnFieldInput(InputEvent e)
        {
            // 누른 채로 손가락을 옮기면 버티는 쪽도 바뀐다
            if (e is InputEventMouseMotion mm && _holding)
            {
                _side = mm.Position.X < _field.Size.X / 2 ? -1 : 1;
                return;
            }
            if (e is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left) return;
            if (!mb.Pressed)
            {
                _holding = false;
                return;
            }
            _side = mb.Position.X < _field.Size.X / 2 ? -1 : 1;
            bool onCat = !_tutorial && _cat.GetRect().Grow(40).HasPoint(mb.Position);
            switch (_phase)
            {
                case Phase.Ready:
                    if (onCat) ShowCatHint();
                    break;
                case Phase.Aim:
                    if (onCat)
                    {
                        ShowCatHint();
                        return;
                    }
                    // 하늘(위쪽 정보 줄 근처)을 누른 건 무시한다
                    if (_stage.ToLogical(mb.Position).Y < Horizon - 60) return;
                    StartPower();
                    break;
                case Phase.Power:
                    Cast();
                    break;
                case Phase.Wait:
                    // 고등어가 쿨러를 노리는 동안에는 고등어만 누른다 (헛챔질 하지 않게)
                    if (_thiefActive)
                    {
                        if (onCat) StopThief();
                        return;
                    }
                    EarlyStrike();
                    break;
                case Phase.Bite:
                    Strike();
                    break;
                case Phase.Reel:
                    _holding = true;
                    break;
            }
        }

        // ───────── 단계 ─────────

        /// <summary>던질 준비: 아래 판에 "던지기" 단추. 누르면 방향 바늘이 움직이기 시작한다</summary>
        private void StartAim()
        {
            _phase = Phase.Ready;
            _gauge.Showing = ReelGauge.Mode.None;
            _field.BobberVisible = false;
            _field.ShadowVisible = false;
            _field.LineTo = null;
            _field.Bend = 0;
            _field.Fighting = false;
            SetPose("fish-ready");
            _leanTarget = 0;
            _exit.SetEnabled(true);
            SetInfo();

            var rng = GameState.I.Rng;
            var spots = G.Spot.RollSpots(G.Save.CatAffinity, Reach, rng, _tutorial ? 0 : G.HintPenalty());
            _hotSpot = spots.Hot;
            _hintSpot = spots.Hint;
            // 바늘은 한쪽 끝에서 출발한다
            _aimDir = rng.Next() < 0.5 ? 1 : -1;
            _angle = -_aimDir;
            _field.ShowFan = false;
            _field.FanReach = (float)Reach;
            _field.FanLabel = F("maxCast", G.Reel.CastMeters(Reach).ToString("0.0", CultureInfo.InvariantCulture));
            _field.FloatTex = FloatTexture();
            _field.NeedleFixed = false;
            _field.Aiming = null;
            _field.Needle = (float)_angle;
            Tell(T["ready"], _tutorial ? T["readyTutorial"] : T["readySub"]);
            ShowThrow(true);
            if (!_tutorial) ShowCatHint();
        }

        private void ShowThrow(bool on)
        {
            _throw.Visible = on;
            // 단추가 있는 동안은 게이지 자리를 비워 안내판이 넘치지 않게
            _gauge.Visible = !on;
            if (!on) return;
            _throw.PivotOffset = _throw.Size / 2;
            _throw.Scale = new Vector2(0.9f, 0.9f);
            _throw.CreateTween().TweenProperty(_throw, "scale", Vector2.One, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        /// <summary>던지기 단추: 여울 발밑에서 퍼지는 부채꼴 위로 방향 바늘이 오간다</summary>
        private void BeginAim()
        {
            if (_phase != Phase.Ready) return;
            _phase = Phase.Aim;
            ShowThrow(false);
            Sound.Play("tap");
            Haptics.Tap();
            _field.ShowFan = true;
            Tell(T["aim"], _tutorial ? T["aimTutorial"] : T["aimSub"]);
        }

        /// <summary>고등어가 울며 반짝이는 지점 쪽을 본다</summary>
        private void ShowCatHint()
        {
            if (_phase != Phase.Ready && _phase != Phase.Aim && _phase != Phase.Power) return;
            HideCatBubble();
            double a = _hintSpot.Angle;
            string dir = a < -0.3 ? T["left"] : a > 0.3 ? T["right"] : T["front"];
            _catBubble = Bubble(Art, F("catHint", dir), _stage.ToLocal(CatFoot + new Vector2(-30, -60)), 0);
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/" + (Math.Abs(a) <= 0.3 ? "sit-front" : "sit-side") + ".png"));
            _cat.FlipH = a > 0.3;
            Hop(_cat, 12);
            Sound.Play("meow-short", -4);
            _field.HintAngle = (float)_hintSpot.Angle;
            _field.HintDist = (float)_hintSpot.Dist;
            _field.ShowHint = true;
        }

        private void HideCatBubble()
        {
            if (_catBubble != null && IsInstanceValid(_catBubble)) _catBubble.QueueFree();
            _catBubble = null;
        }

        private static void Hop(Control c, float px)
        {
            var tw = c.CreateTween();
            float y = c.Position.Y;
            tw.TweenProperty(c, "position:y", y - px, 0.12).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(c, "position:y", y, 0.2).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        }

        /// <summary>바늘을 멈춰 방향을 정하고 파워 게이지로. 반짝이는 지점은 계속 보인다</summary>
        private void StartPower()
        {
            if (!_tutorial && G.BaitLeft() <= 0)
            {
                _phase = Phase.Busy;
                Tell(T["noBait"], T["noBaitSub"]);
                Later(1.4, EndTrip);
                return;
            }
            _phase = Phase.Power;
            _exit.SetEnabled(false);
            HideCatBubble();
            _field.ShowFan = true;
            _field.NeedleFixed = true;
            // 힘은 바다 위가 아니라 아래 게이지로만 본다
            _field.Aiming = null;
            ShowThrow(false);
            Sound.Play("tap");
            Haptics.Tap();
            _gaugePos = 0;
            _gaugeDir = 1;
            float reach = (float)Reach;
            var zones = G.Data.Balance.Fishing;
            _gauge.Bound1 = (float)zones.NearBelow;
            _gauge.Bound2 = (float)zones.MidBelow;
            _gauge.Reach = reach;
            _gauge.ReachLabel = F("maxCast", G.Reel.CastMeters(Reach).ToString("0.0", CultureInfo.InvariantCulture));
            _gauge.Needle = 0;
            _gauge.Showing = ReelGauge.Mode.Power;
            Tell(T["power"], T["powerSub"]);
        }

        private void Cast()
        {
            _dist01 = G.Reel.CastDistance(_gaugePos, Reach);
            ShowThrow(false);
            if (!_tutorial)
            {
                G.UseBaitNow();
                _castsLeft -= 1;
                _castsDone += 1;
            }
            SetInfo();
            _gauge.Showing = ReelGauge.Mode.None;
            _field.ShowFan = false;
            _field.ShowHint = false;
            _field.Aiming = null;
            HideCatBubble();
            _phase = Phase.Flying;
            _close = G.Spot.Closeness(new CastSpot(_angle, _dist01), _hotSpot);
            Sound.Play("whoosh");
            Haptics.Pulse(20, 0.22f);

            var to = PosFor((float)_angle, (float)_dist01);
            _target = new Vector2(to.X, to.Y);
            _targetScale = to.Z;
            Tell(F("cast", G.Data.Texts.Zones[G.Fishing.ZoneOf(_dist01)]));

            // 대를 머리 위로 휘둘러 던지고, 찌가 날아가는 동안 앞으로 내민다. 찌가 떨어질 쪽을 본다
            FaceToward(_target.X);
            SetPose("fish-cast");
            _lean = -6;
            _leanRate = 26;
            _leanTarget = 10;
            Later(0.3, () =>
            {
                if (_phase != Phase.Flying && _phase != Phase.Wait) return;
                // 찌가 자리를 잡으면 기다린다. 낚시 의자가 있으면 앉고, 없으면 선 채로
                SetPose(G.KitLevel("chair") > 0 ? "fish-sit" : "fish-wait");
                _leanRate = 8;
                _leanTarget = 0;
            });
            // 찌가 포물선을 그리며 날아간다
            var from = _field.RodTip;
            float peak = Mathf.Min(from.Y, to.Y) - 120;
            _field.BobberVisible = true;
            _field.BobberFloating = false;
            _field.BobberSink = 0;
            var tw = CreateTween();
            tw.TweenMethod(Callable.From<float>(t =>
            {
                float x = Mathf.Lerp(from.X, _target.X, t);
                float y = (1 - t) * (1 - t) * from.Y + 2 * (1 - t) * t * peak + t * t * _target.Y;
                _field.BobberAt = new Vector2(x, y);
                _field.BobberScale = Mathf.Lerp(1.3f, _targetScale, t);
                _field.LineTo = _field.BobberAt;
            }), 0f, 1f, 0.65).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tw.TweenCallback(Callable.From(Landed));
        }

        private void Landed()
        {
            if (_phase != Phase.Flying) return;
            _field.AddSplash(_target, _targetScale);
            _field.BobberFloating = true;
            _field.BobberAt = _target;
            _field.LineTo = _target;
            _phase = Phase.Wait;
            _bobT = 0;
            _waitT = 0;
            Sound.Play("plop");
            Haptics.Pulse(12, 0.18f);
            var rng = GameState.I.Rng;
            // 입질까지 기다리는 시간은 어디에 던지든 같다
            _wait = G.Reel.StartWait(false, _tutorial, rng);
            if (!_tutorial) _wait.Scale(G.WaitMul());
            _thiefAt = _tutorial ? -1 : G.Spot.RollThief(G.Save.Cooler.Count, rng);
            _thiefDone = false;
            _idleT = 3 + (float)rng.Next() * 3;
            TellWait();
        }

        /// <summary>
        /// 기다리는 동안 작은 볼거리: 먼 물에서 잔고기가 톡 튀거나(물고기 모습은 없이 물보라만),
        /// 고등어가 기지개를 켜고 식빵을 굽고 하품을 한다
        /// </summary>
        private void WaitIdle()
        {
            var rng = GameState.I.Rng;
            _idleT = 5 + (float)rng.Next() * 6;
            double r = rng.Next();
            if (_tutorial || r < 0.55)
            {
                var at = PosFor((float)(rng.Next() * 2 - 1), 0.3f + (float)rng.Next() * 0.65f);
                var p = new Vector2(at.X, at.Y);
                // 찌 바로 옆은 피한다 (입질로 헷갈리지 않게)
                if (p.DistanceTo(_target) < 40) p.X += p.X < _target.X ? -50 : 50;
                _field.AddSplash(p, 0.3f * at.Z);
                Sound.Play("plop", -18, 1.35f);
                return;
            }
            string[] poses = { "stretch", "loaf", "meow-1" };
            string pose = poses[(int)(rng.Next() * poses.Length)];
            _catIdle = true;
            _cat.FlipH = false;
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/" + pose + ".png"));
            if (pose == "meow-1")
            {
                var idle = G.Data.Dialogue.CatIdle;
                if (idle.Count > 0) Bubble(Art, idle[(int)(rng.Next() * idle.Count)], _stage.ToLocal(CatFoot + new Vector2(-30, -60)), 1.4f);
                Sound.Play("meow-short", -12);
            }
            Later(pose == "loaf" ? 4 : 2.2, CatSitBack);
        }

        /// <summary>딴짓하던 고등어가 다시 앉아 찌를 본다</summary>
        private void CatSitBack()
        {
            if (!_catIdle || _thiefActive) return;
            _catIdle = false;
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/sit-side.png"));
            _cat.FlipH = false;
        }

        /// <summary>떨어진 자리가 좋은지 알려 준다</summary>
        private void TellWait()
        {
            if (_close >= 0.6) Tell(T["wait"], T["waitHot"], Palette.Gold);
            else if (_close >= 0.25) Tell(T["wait"], T["waitNear"]);
            else Tell(T["wait"], T["waitSub"]);
        }

        private void EarlyStrike()
        {
            if (_tutorial)
            {
                // 튜토리얼은 놓쳐도 다시 기다린다
                Tell(T["early"], T["earlyTutorial"], Palette.Gold);
                Sound.Play("nibble", -4, 0.8f);
                _wait.Retry();
                Later(1.2, () =>
                {
                    if (_phase == Phase.Wait) Tell(T["wait"], T["earlyTutorial"]);
                });
                return;
            }
            _phase = Phase.Busy;
            Tell(T["early"], T["earlySub"]);
            _field.AddSplash(_target, _targetScale * 0.8f);
            _field.BobberVisible = false;
            _field.LineTo = null;
            Sound.Play("slip", -4);
            Settle(-10);
            FinishCast();
        }

        // ───────── 고등어 도둑 ─────────

        /// <summary>고등어가 살금살금 쿨러 쪽으로 간다. 제때 누르면 말린다</summary>
        private void StartThief()
        {
            _thiefActive = true;
            _thiefT = 0;
            Sound.Play("sneak", -2);
            Haptics.Pulse(25, 0.25f);
            // 오른쪽으로 걸어가며 여울 앞을 지난다 (걷기 그림은 오른쪽을 본다)
            _cat.FlipH = false;
            _stage.MoveChild(_cat, _stage.GetChildCount() - 1);
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/walk-1.png"));
            Tell(T["thief"], T["thiefSub"], Palette.Gold);
        }

        private void UpdateThief(float dt)
        {
            if (!_freezeThief) _thiefT += dt;
            float window = (float)G.Spot.ThiefWindow;
            float k = Mathf.Clamp(_thiefT / window, 0, 1);
            _stage.Move(_cat, CatFoot.Lerp(CatThiefFoot, Mathf.SmoothStep(0, 1, k)));
            int frame = (int)(_thiefT / 0.2f) % 2;
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/walk-" + (frame + 1) + ".png"));
            _field.CoolerLid = k > 0.7f ? Mathf.Abs(Mathf.Sin(_thiefT * 18)) : 0;
            if (_thiefT >= window) Steal();
        }

        private void StopThief()
        {
            _thiefActive = false;
            _thiefDone = true;
            _field.CoolerLid = 0;
            Sound.Play("hiss");
            Haptics.Tap();
            // 뛰기·먹기 그림은 왼쪽을 본다. 쿨러 쪽(오른쪽)을 보게 뒤집는다
            _cat.FlipH = true;
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/jump.png"));
            Hop(_cat, 20);
            Toast(T["thiefStopped"], Palette.Paper, SafeTop + 420);
            Later(0.5, ReturnCat);
            TellWait();
        }

        private void Steal()
        {
            _thiefActive = false;
            _thiefDone = true;
            _field.CoolerLid = 0;
            var fish = G.CatSteal();
            SetInfo();
            _cat.FlipH = true;
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/eat.png"));
            Sound.Play("steal");
            Haptics.Pulse(60, 0.3f);
            if (fish != null) Toast(F("thiefStole", G.Data.SpeciesById(fish.Species).Name), Palette.RedText, SafeTop + 420);
            Later(1.2, ReturnCat);
            TellWait();
        }

        /// <summary>고등어가 제자리로 돌아와 앉는다</summary>
        private void ReturnCat()
        {
            _stage.Move(_cat, CatFoot);
            _stage.MoveChild(_cat, _yeoul.GetIndex());
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/sit-side.png"));
            _cat.FlipH = false;
        }

        // ───────── 입질과 챔질 ─────────

        private void StartBite()
        {
            _phase = Phase.Bite;
            _biteElapsed = 0;
            CatSitBack();
            var rng = GameState.I.Rng;
            int zone = G.Fishing.ZoneOf(_dist01);
            if (_tutorial)
            {
                // 손바닥만 한 볼락 한 마리 (웹과 같다)
                _big = false;
                _species = G.Data.SpeciesById("bollak");
                _size = 19.4;
            }
            else
            {
                _big = G.Spot.IsBigBite(_close, rng, G.BigAdd());
                _species = G.Fishing.RollSpeciesNear(zone, G.Save.Slot, G.MonthNow(), G.SeaNow(), _close, rng, G.BaitBias(), G.SpotNow());
                _size = G.Fishing.RollSizeNear(_species, _close, _big, rng);
            }
            _biteWindow = G.Reel.BiteWindow(G.EffectiveRod(), _species, _tutorial);
            _feel = (float)G.Spot.Feel(_species, _size, _big);
            _nibbleTween?.Kill();
            // 찌가 쑥 빨려 들어가며 물이 튄다. 줄이 한 번 팽 당겨진다
            _field.BobberSink = 1;
            _field.AddSplash(_target, _targetScale * (_big ? 2.4f : 1.6f));
            _field.AddSpray(_target, 1.2f, _big ? 14 : 8);
            _field.Flash = _big ? 0.5f : 0.25f;
            Shake(_big ? 0.35f : 0.18f, _big ? 1.5f : 0.9f);
            Punch(_big ? 1.06f : 1.035f, 1, 0.06, 0.3);
            Sound.Play("bite");
            Sound.Play("plop", -6, 0.8f);
            if (_big)
            {
                Sound.Play("big");
                Haptics.Pattern((80, 0.9f, 50), (60, 0.7f, 50), (160, 1f, 0));
                Toast(T["big"], Palette.Gold, SafeTop + 420);
            }
            else
            {
                // 투둑, 쑥: 작은 고기도 손에 분명히 온다
                Haptics.Pattern(((int)(35 + 35 * _feel), 0.5f + 0.4f * _feel, 45), ((int)(25 + 25 * _feel), 0.4f + 0.4f * _feel, 70), ((int)(60 + 60 * _feel), 0.6f + 0.4f * _feel, 0));
            }
            // 의자에 앉아 있다가 깜짝 놀라 대를 붙잡는다
            if (_pose == "fish-sit") SetPose("fish-bite");
            else if (_pose == "fish-wait") SetPose("fish-ready");
            // 대 끝이 물 쪽으로 쿡 끌려간다
            _leanRate = 20;
            _leanTarget = _big ? 9 : 5;
            Tell(T["bite"], T["biteSub"], Palette.Gold);
            Exclaim();
        }

        /// <summary>입질 순간 여울 머리 위에 "!" 가 톡 튀어 오른다</summary>
        private void Exclaim()
        {
            var r = _yeoul.GetGlobalRect();
            if (_mark != null && IsInstanceValid(_mark)) _mark.QueueFree();
            var mark = UiKit.Text("!", TextSize.H1 + 56, new Color("ff8a1c"), true, false, 14, new Color("3a1a05"));
            mark.MouseFilter = MouseFilterEnum.Ignore;
            Art.AddChild(mark);
            _mark = mark;
            var at = r.Position - Art.GetGlobalRect().Position + new Vector2(r.Size.X * 0.62f, -10);
            mark.Position = at;
            mark.PivotOffset = new Vector2(30, 100);
            mark.Scale = new Vector2(0.3f, 0.3f);
            var tw = mark.CreateTween();
            tw.TweenProperty(mark, "scale", new Vector2(1.15f, 1.15f), 0.12).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.Parallel().TweenProperty(mark, "position:y", at.Y - 26, 0.12);
            tw.TweenProperty(mark, "scale", Vector2.One, 0.1);
            tw.TweenInterval(0.45);
            tw.TweenProperty(mark, "modulate:a", 0f, 0.2);
            tw.TweenCallback(Callable.From(mark.QueueFree));
        }

        private void Strike()
        {
            bool perfect = G.Reel.IsPerfectStrike(_biteElapsed);
            StartReel(perfect);
        }

        private void StartReel(bool perfect)
        {
            _phase = Phase.Reel;
            ShowThrow(false);
            // 걸었으면 "!" 는 바로 걷는다
            if (_mark != null && IsInstanceValid(_mark)) _mark.QueueFree();
            _mark = null;
            // 대를 확 들어 올려 챔질한다
            _pullHold = 0;
            SetPose("fish-reel");
            _lean = -12;
            _leanRate = 12;
            _field.BobberVisible = false;
            _field.ShadowVisible = true;
            _field.Big = _big;
            _field.ClearTrail();
            // 챔질: 대가 휘청, 화면이 번쩍 다가가고 줄 끝에서 물이 터진다
            Sound.Play("strike");
            Sound.Play("surge", -3, 0.9f);
            Haptics.Pattern(((int)(70 + 50 * _feel), 0.8f + 0.2f * _feel, 35), ((int)(40 + 40 * _feel), 0.55f + 0.3f * _feel, 0));
            _field.RodKick = 1.4f;
            _field.Flash = perfect ? 0.7f : 0.45f;
            _headShakeIn = 0.5f;
            _dragTick = 0;
            var s = G.Save;
            _runOffset = 0;
            // 장비(줄·바늘·찌·뜰채)까지 더한 힘싸움
            _fight = G.Spot.StartTug(G.Fight(_species, _size, _dist01, perfect, _tutorial), GameState.I.Rng);
            _wobble = (float)GameState.I.Rng.Next() * 10;
            _field.ShadowSize = ShadowScale(0);
            var hookAt = PosFor((float)_angle, (float)_dist01);
            _field.AddBurst(new Vector2(hookAt.X, hookAt.Y), _big ? 1.8f : 1.2f);
            Shake(0.22f, _big ? 1.6f : 1.1f);
            // 좋은 릴일수록 초록(알맞음)이 넓고 빨강(위험)이 좁다
            _gauge.Slack = (float)_fight.SlackAt;
            _gauge.Danger = (float)_fight.DangerAt;
            _gauge.Drag = (float)_fight.Setup.Reel.Drag;
            _gauge.Tension = (float)_fight.Tension;
            _gauge.Showing = ReelGauge.Mode.Tension;
            Toast(perfect ? T["perfect"] : T["hooked"], perfect ? Palette.Gold : Palette.Paper, SafeTop + 420);
            Tell(T["reel"], _big ? T["bigSub"] : T["reelSub"]);
            _field.FishStamina = 1;
            _field.ShowStamina = true;
            // 화면이 확 다가갔다가 조금 물러난다. 큰 놈이면 더
            Punch(_big ? 1.14f : 1.1f, _big ? 1.08f : 1.05f, 0.08, 0.45);
        }

        /// <summary>그림자 크기: 고기 크기에 따라, 가까이 올수록 크게</summary>
        private float ShadowScale(float progress)
        {
            return Mathf.Clamp((float)(_size / _species.AvgSize), 0.7f, 1.7f) * Mathf.Lerp(0.6f, 1.3f, progress);
        }

        private void UpdateReel(float dt)
        {
            var f = _fight;
            double danger = f.DangerAt, slack = f.SlackAt;
            // 사진 모드 자동 감기: 잠잠하면 텐션을 지키고, 질주하면 반대쪽을 눌러 버틴다
            if (_autoHold)
            {
                if (f.Phase == TugPhase.Calm) _holding = _shotPull ? f.Tension < (_holding ? 50 : 30) : f.Tension < (_holding ? 72 : 52);
                else
                {
                    _side = -f.RunDir;
                    _holding = f.Tension < (_holding ? 82 : 60);
                }
            }
            var r = f.Step(_holding, _side, dt);
            double str = f.Strength();
            bool running = f.Phase == TugPhase.Run;
            // 감는 소리. 질주 중 손을 떼면 줄이 풀려 나가는 높은 소리
            if (running && !_holding) Sound.Loop("reel", true, -4, 1.5f);
            else Sound.Loop("reel", _holding, -6, 0.85f + (float)f.Tension / 260);

            if (r.WarnStart)
            {
                // 물고기가 한쪽으로 몸을 튼다: 그쪽에 물보라, 짧은 진동
                Sound.Play("surge", -2);
                Buzz(50, 0.5f);
                Shake(0.08f);
                _field.AddSplash(_field.ShadowAt + new Vector2(f.RunDir * 18, 0), 1.1f);
                Tell(T[f.RunDir < 0 ? "runLeft" : "runRight"], T[f.RunDir < 0 ? "braceRight" : "braceLeft"], Palette.Gold);
            }
            if (r.RunStart)
            {
                // 차고 나간다: 드랙이 울고 대가 확 끌려가며 화면이 흔들린다
                Sound.Play("whoosh", 0, 0.8f);
                Sound.Play("surge", -2, 1.1f);
                Haptics.Pattern(((int)(60 + 60 * _feel), 0.55f + 0.45f * _feel, 40), ((int)(60 + 60 * _feel), 0.5f + 0.5f * _feel, 0));
                _field.RodKick = 1.2f;
                _field.AddSplash(_field.ShadowAt, _big ? 1.8f : 1.3f);
                Shake(0.25f, _big ? 1.3f : 0.9f);
                if (GameState.I.Rng.Next() < (_big ? 0.7 : 0.4)) Thrash();
            }
            if (f.Phase == TugPhase.Run)
            {
                // 질주 중: 드랙이 딸깍딸깍 손에 오고 물살이 튄다. 버티면 덜하다
                _dragTick -= dt;
                if (_dragTick <= 0)
                {
                    _dragTick = r.Bracing ? 0.16f : 0.08f;
                    if (!r.Bracing && !r.WrongSide) Haptics.Pulse(14, 0.3f + 0.4f * _feel);
                    _field.AddSpray(_field.ShadowAt, 1, 2);
                    Shake(0.06f, r.Bracing ? 0.35f : 0.6f);
                    _field.RodKick = Mathf.Max(_field.RodKick, 0.45f);
                }
            }
            else if (f.Phase == TugPhase.Calm && !f.Tired && _holding)
            {
                // 감는 동안 물고기가 가끔 머리를 흔든다: 대 끝이 툭툭, 손에 툭
                _headShakeIn -= dt;
                if (_headShakeIn <= 0)
                {
                    var hr = GameState.I.Rng;
                    _headShakeIn = (float)hr.Range(0.45, 1.1) / (0.7f + 0.6f * _feel);
                    _field.RodKick = 0.7f + 0.5f * _feel;
                    _field.AddSpray(_field.ShadowAt, 1, 3);
                    Haptics.Pattern(((int)(18 + 20 * _feel), 0.35f + 0.45f * _feel, 40), ((int)(14 + 14 * _feel), 0.3f + 0.4f * _feel, 0));
                    Shake(0.05f, 0.4f);
                }
            }
            if (r.RunHeld)
            {
                Sound.Play("okay", -4, 1.3f);
                Toast(T["held"], Palette.Gold, SafeTop + 420);
                FloatText(T["held"], _stage.ToLocal(_field.ShadowAt) + new Vector2(0, -80), Palette.Gold);
            }
            if (r.Exhausted)
            {
                // 다 지쳤다: 저항이 툭 풀린다. 이제 쭉 감으면 끌려온다
                Sound.Play("good", -4);
                Toast(T["tired"], Palette.GreenText, SafeTop + 420);
                Haptics.Pattern((40, 0.5f, 60), (20, 0.25f, 0));
                _field.AddSplash(_field.ShadowAt, 0.9f);
            }
            // 버티는 동안은 손에 꾹꾹 저항이, 잘못 당기면 날카롭게 떨린다
            _braceBuzz -= dt;
            if (_braceBuzz <= 0 && (r.Bracing || r.WrongSide))
            {
                _braceBuzz = r.WrongSide ? 0.12f : 0.26f;
                Buzz(r.WrongSide ? 30 : 22, r.WrongSide ? 0.75f : 0.4f);
            }
            // 줄이 위험하면 손에서 계속 떨린다
            if (f.Tension >= danger && !r.WrongSide)
            {
                _buzz -= dt;
                if (_buzz <= 0)
                {
                    _buzz = 0.18f;
                    Buzz(28, 0.35f + 0.4f * Mathf.Clamp((float)(f.Tension - danger) / 15, 0, 1));
                }
            }
            // 튜토리얼: 줄이 터지거나 바늘이 빠질 뻔하면 구해 주고 알려 준다
            if (r.RescueSnap) Toast(T["rescueSnap"], Palette.RedText, SafeTop + 420);
            if (r.RescueSlack) Toast(T["rescueSlack"], Palette.BlueText, SafeTop + 420);
            if (r.End == FightEnd.Snap)
            {
                Sound.Play("snap");
                Buzz(200, 0.9f);
                Lose(T["snap"], T["snapSub"]);
                return;
            }
            if (r.End == FightEnd.Slack)
            {
                Sound.Play("slip");
                Buzz(50, 0.3f);
                Lose(T["slack"], T["slackSub"]);
                return;
            }
            if (r.End == FightEnd.Landed)
            {
                LandFish();
                return;
            }

            // 물속 몸부림: 질주하면 그쪽으로 확 끌려간다
            float p = (float)f.Progress();
            float runTarget = f.Phase == TugPhase.Run ? f.RunDir * 70 : f.Phase == TugPhase.Warn ? f.RunDir * 14 : 0;
            _runOffset = Mathf.Lerp(_runOffset, runTarget, 1 - Mathf.Exp(-(running ? 3.2f : 2f) * dt));
            _wobble += dt * (float)(2 + str * 0.6 + (running ? 6 : 0));
            var from = PosFor((float)_angle, (float)_dist01);
            float fx = Mathf.Lerp(from.X, FanApex.X + 20, p) + _runOffset * (1 - p * 0.5f) + Mathf.Sin(_wobble) * (16 + (running ? 10 : 0)) * (1 - p * 0.6f);
            fx = Mathf.Clamp(fx, 30, ArtStage.Logical.X - 30);
            float fy = Mathf.Lerp(from.Y, Shore - 16, p);
            _field.ShadowAt = new Vector2(fx, fy);
            FaceToward(fx);
            _field.ShadowWobble = _wobble;
            _field.ShadowSize = ShadowScale(p) * (running ? 1.2f : 1f);
            _field.LineTo = _field.ShadowAt;
            _field.Bend = Mathf.Clamp((float)f.Tension / 100, 0, 1.1f);
            _field.Fighting = true;
            _field.FishStamina = (float)f.Stamina;
            _field.RunDir = f.Phase == TugPhase.Calm ? 0 : f.RunDir;
            _field.RunWarn = f.Phase == TugPhase.Warn;
            _field.Bracing = r.Bracing;
            // 버틸 때는 반대쪽으로 몸을 젖히고, 아니면 텐션만큼 물고기 쪽으로 끌려간다
            bool strain = _shotPull || r.Bracing || r.WrongSide || f.Tension >= danger - 4;
            _pullHold = strain ? 0.5f : _pullHold - dt;
            SetPose(_pullHold > 0 ? "fish-pull" : "fish-reel");
            // 질주 쪽 기울기는 화면 기준이라 포즈를 뒤집었으면 부호를 맞춘다
            if (r.Bracing) _leanTarget = -f.RunDir * 7 * Face;
            else if (_pullHold > 0) _leanTarget = 3 + (running ? f.RunDir * 6 * Face : 0);
            else _leanTarget = -6 + 15 * _field.Bend + (running ? f.RunDir * 8 * Face : 0);
            _gauge.Tension = (float)f.Tension;
            if (f.Phase == TugPhase.Calm)
            {
                _msg.Text = T[f.Tired ? "tired" : "reel"];
                _sub.Text = F("distance", Math.Max(0, f.Distance).ToString("0.0", CultureInfo.InvariantCulture));
            }
            // 텐션이 위험하면 큰 글이 빨개진다
            _msg.LabelSettings.FontColor = f.Phase != TugPhase.Calm ? Palette.Gold : f.Tension >= danger ? Palette.RedText : f.Tension < slack ? Palette.BlueText : Palette.Paper;
        }

        /// <summary>물고기가 물을 철썩 박찬다: 줄 끝에 물기둥과 물방울 (물고기 모습은 보이지 않는다)</summary>
        private void Thrash()
        {
            _field.AddBurst(_field.ShadowAt, _big ? 2.2f : 1.6f);
            Toast(T["jump"], Palette.Gold, SafeTop + 420);
        }

        private void Lose(string main, string sub)
        {
            _phase = Phase.Busy;
            Sound.Loop("reel", false);
            _gauge.Showing = ReelGauge.Mode.None;
            _field.ShowStamina = false;
            _field.RunDir = 0;
            _field.ShadowVisible = false;
            _field.AddSplash(_field.ShadowAt, 1.5f);
            _field.LineTo = null;
            _field.Bend = 0;
            _field.Fighting = false;
            ZoomTo(1, 0.6);
            Settle(-14);
            Tell(main, sub);
            FinishCast();
        }

        /// <summary>끌어올렸다! 발밑에서 물이 크게 솟고 줄을 거둔다</summary>
        private void LandFish()
        {
            _phase = Phase.Result;
            Sound.Loop("reel", false);
            Sound.Play("land");
            Sound.Play("plop", -2, 0.7f);
            Haptics.Pattern(((int)(50 + 40 * _feel), 0.6f + 0.3f * _feel, 50), ((int)(30 + 30 * _feel), 0.45f + 0.3f * _feel, 50), ((int)(110 + 80 * _feel), 0.75f + 0.25f * _feel, 0));
            _field.ShowStamina = false;
            _field.RunDir = 0;
            _gauge.Showing = ReelGauge.Mode.None;
            _field.Bend = 0;
            _field.Fighting = false;
            Punch(_big ? 1.16f : 1.12f, 1, 0.08, 0.7);
            Shake(0.35f, _big ? 1.8f : 1.3f);
            Tell("");
            _field.ShadowVisible = false;
            _field.Flash = 0.8f;
            _field.RodKick = 1.5f;
            _field.AddBurst(_field.ShadowAt, _big ? 3f : 2.4f);
            var landAt = _field.ShadowAt;
            Later(0.14, () =>
            {
                // 한 번 더 철썩: 물 밖으로 들려 나오며 물방울이 쏟아진다
                _field.AddBurst(landAt + new Vector2(0, -8), _big ? 2.2f : 1.6f);
                _field.AddSpray(landAt + new Vector2(0, -20), 1.4f, 12);
            });
            Later(0.35, () =>
            {
                _field.LineTo = null;
                Celebrate(_tutorial ? TutorialCatch : ShowResult);
            });
        }

        /// <summary>튜토리얼 볼락: 도감에 남기고 그림을 보여 준 뒤 프롤로그 2부로</summary>
        private void TutorialCatch()
        {
            G.RecordCatch(_species.Id, _size, false);
            G.Save.Flags["tutorialFish"] = true;
            G.Persist();
            Sound.Play("newfish");
            Modal(_species.Name, F("tutorialCatch", _size.ToString("0.0", CultureInfo.InvariantCulture)), new[]
            {
                new ModalButton(T["tutorialGo"], Palette.Green, () =>
                {
                    GameState.I.Tutorial = false;
                    GameState.I.GoStory("p2");
                }),
            }, FishPicture(_species.Id));
        }

        /// <summary>낚았다! 앞을 보고 대를 든 채 활짝 웃으며 톡 뛴다. 잠깐 뒤 결과를 띄운다</summary>
        private void Celebrate(Action then)
        {
            HideCatBubble();
            SetPose("fish-joy");
            Sound.Play("catch");
            _lean = 0;
            _leanTarget = 0;
            _leanRate = 10;
            var tw = _yeoul.CreateTween();
            float y = _yeoul.Position.Y;
            tw.TweenProperty(_yeoul, "position:y", y - 26, 0.14).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(_yeoul, "position:y", y, 0.24).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
            Later(0.75, then);
        }

        /// <summary>낚은 고기. 처음 보는 어종이면 여울이 먼저 기뻐한다 (감정 일러스트)</summary>
        private void ShowResult()
        {
            DexEntry e;
            bool isNew = !G.Save.Dex.TryGetValue(_species.Id, out e) || e.Count == 0;
            if (isNew)
            {
                Sound.Play("newfish");
                Say("yeoul", "peace", T["newSpecies"], null, () =>
                {
                    Toast(T["dexNew"], Palette.Gold, SafeTop + 300);
                    ResultModal(true);
                });
                return;
            }
            ResultModal(false);
        }

        private void ResultModal(bool isNew)
        {
            var sp = _species;
            double size = _size;
            int value = G.FishValue(new FishItem(0, sp.Id, size, G.Save.Day, G.Data.Balance.Fresh.Start));
            bool full = G.CoolerFull();
            // 금지체장 미달이거나 금어기면 챙길 수 없다
            string rule = G.ReleaseReason(sp.Id, size);
            var tags = new List<string> { size.ToString("0.0", CultureInfo.InvariantCulture) + "cm" };
            if (_big) tags.Add(T["big"].TrimEnd('!'));
            if (sp.Rare) tags.Add(T["rare"]);
            var body = new List<string> { string.Join(" · ", tags) };
            body.Add(F("value", UiKit.Won(value)));
            if (rule != null) body.Add(rule);
            else if (full) body.Add(T["full"]);

            ResultCard(sp, isNew, string.Join("\n", body), rule != null ? T["mustRelease"] : full ? T["coolerFull"] : T["keep"], full || rule != null);
        }

        /// <summary>
        /// 결과 카드: 화면 가운데를 가리지 않게 아래 안내판 자리에 작게 뜬다. 물고기 그림 · 이름과 값 · 놓아주기/챙기기.
        /// 하나를 누르면 바로 다음 던지기로
        /// </summary>
        private void ResultCard(Species sp, bool isNew, string body, string keepLabel, bool keepOff)
        {
            var card = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
            card.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            card.OffsetTop = -(PanelHeight + SafeBottom + 120);
            card.OffsetLeft = 28;
            card.OffsetRight = -28;
            card.OffsetBottom = -(SafeBottom + 20);
            card.AddThemeStyleboxOverride("panel", UiKit.Box(Palette.Paper, 40, 34, 28).WithBorder(Palette.PaperDark, 0, 8).WithShadow(0.4f, 24, 10));
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 18);
            card.AddChild(col);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 24);
            col.AddChild(row);
            var pic = FishPicture(sp.Id);
            pic.CustomMinimumSize = new Vector2(250, 190);
            row.AddChild(pic);
            var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
            text.AddThemeConstantOverride("separation", 6);
            text.AddChild(UiKit.Text(sp.Name + (isNew ? "  " + T["newFish"] : ""), TextSize.Title - 8, isNew ? Palette.GoldDark : Palette.Ink, true));
            text.AddChild(UiKit.Text(body, TextSize.Small, Palette.InkSoft, false, true));
            row.AddChild(text);
            var buttons = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            buttons.AddThemeConstantOverride("separation", 20);
            col.AddChild(buttons);
            void Pick(bool released)
            {
                if (!IsInstanceValid(card)) return;
                card.MouseFilter = MouseFilterEnum.Ignore;
                var tw = card.CreateTween();
                tw.TweenProperty(card, "modulate:a", 0f, 0.15);
                tw.TweenCallback(Callable.From(card.QueueFree));
                Finish(released);
            }
            var release = new GameButton(T["release"], Palette.BlueDark, () => Pick(true), null, TextSize.Body, 104) { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var keep = new GameButton(keepLabel, Palette.Green, () => Pick(false), null, TextSize.Body, 104) { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            keep.SetEnabled(!keepOff);
            buttons.AddChild(release);
            buttons.AddChild(keep);
            Popups.AddChild(card);
            Sound.Play("pop", -4);
            // 아래에서 쏙 올라온다
            card.Modulate = new Color(1, 1, 1, 0);
            var up = card.CreateTween().SetParallel(true);
            up.TweenProperty(card, "modulate:a", 1f, 0.18);
            float y = card.OffsetTop;
            card.OffsetTop = y + 80;
            card.OffsetBottom += 80;
            up.TweenProperty(card, "offset_top", y, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            up.TweenProperty(card, "offset_bottom", card.OffsetBottom - 80, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        /// <summary>결과 상자 안 물고기 그림. 파닥파닥 흔들린다</summary>
        private static Control FishPicture(string id)
        {
            var holder = new Control { CustomMinimumSize = new Vector2(0, 330), MouseFilter = MouseFilterEnum.Ignore };
            var pic = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://art/fish/" + id + ".png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            pic.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holder.AddChild(pic);
            pic.Resized += () => pic.PivotOffset = pic.Size / 2;
            pic.RotationDegrees = -4;
            var tw = pic.CreateTween().SetLoops();
            tw.TweenProperty(pic, "rotation_degrees", 4f, 0.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tw.TweenProperty(pic, "rotation_degrees", -4f, 0.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            return holder;
        }

        private void Finish(bool released)
        {
            var s = G.Save;
            G.RecordCatch(_species.Id, _size, released);
            _caught += 1;
            if (!released)
            {
                G.AddToCooler(_species.Id, _size);
                // 맡은 의뢰 고기면 알린다
                var req = G.AcceptedRequestFor(_species.Id);
                if (req != null)
                {
                    var t = G.Data.RequestTemplates[req.Template];
                    int have = Math.Min(G.RequestFishHave(t.Species), t.Count);
                    Toast(have >= t.Count ? F("questReady", t.Client) : F("questFish", _species.Name, have, t.Count), have >= t.Count ? Palette.GreenText : Palette.Gold, SafeTop + 420);
                }
                RefreshQuests();
            }
            else s.Flags["released"] = true;
            Sound.Play(released ? "plop" : "coin", -3);
            SetInfo();
            var unlocked = G.CheckTasteUnlock();
            G.Persist();
            if (unlocked != null)
            {
                Modal(F("tasteTitle", unlocked.Name), F("tasteBody", unlocked.Effect), new[] { new ModalButton(T["ok"], Palette.Green, FinishCast) });
                return;
            }
            FinishCast();
        }

        private void FinishCast()
        {
            _phase = Phase.Busy;
            Later(0.35, () =>
            {
                if (_castsLeft <= 0 || G.BaitLeft() <= 0) EndTrip();
                else StartAim();
            });
        }

        private void ConfirmExit()
        {
            if (_phase != Phase.Aim && _phase != Phase.Ready) return;
            bool cast = _castsDone > 0;
            Modal(cast ? T["exitTitle"] : T["backTitle"], cast ? T["exitBody"] : T["backBody"], new[]
            {
                new ModalButton(T["keepFishing"], Palette.GrayDark),
                new ModalButton(T["toTown"], Palette.Blue, Leave),
            });
        }

        private void EndTrip()
        {
            _phase = Phase.Busy;
            Modal(_castsLeft <= 0 ? T["tideOver"] : T["enough"], F("tripBody", _caught, G.Save.Cooler.Count, G.CoolerCapacity()), new[]
            {
                new ModalButton(T["toTown"], Palette.Blue, Leave),
            });
        }

        private void Leave()
        {
            _phase = Phase.Busy;
            Sound.StopLoops();
            if (_castsDone > 0) G.UseSlot();
            GameState.I.GoTo("town");
        }

        // ───────── 연출 ─────────

        /// <summary>화면 흔들림. strength 1 이 가로 8px</summary>
        private void Shake(float seconds, float strength = 1)
        {
            if (_shake <= 0 || strength >= _shakeAmp) _shakeAmp = strength;
            _shake = Mathf.Max(_shake, seconds);
        }

        /// <summary>순간 확대했다가 돌아온다 (챔질, 끌어올림)</summary>
        private void Punch(float peak, float settle, double up = 0.07, double down = 0.35)
        {
            _zoomTween?.Kill();
            _zoomTween = CreateTween();
            _zoomTween.TweenMethod(Callable.From<float>(z => _stage.Zoom = z), _stage.Zoom, peak, up).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            _zoomTween.TweenMethod(Callable.From<float>(z => _stage.Zoom = z), peak, settle, down).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }

        /// <summary>가진 찌 그림 (없으면 null: 기본 빨간 찌)</summary>
        private Texture2D FloatTexture()
        {
            int lv = _tutorial ? 0 : G.KitLevel("float");
            if (lv <= 0) return null;
            string path = "res://art/gear/float-" + lv + ".png";
            return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        }

        private void ZoomTo(float zoom, double time)
        {
            _zoomTween?.Kill();
            _zoomTween = CreateTween();
            _zoomTween.TweenMethod(Callable.From<float>(z => _stage.Zoom = z), _stage.Zoom, zoom, time).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }

        public override void _Process(double delta)
        {
            float dt = Mathf.Min(0.05f, (float)delta);
            _time += dt;
            // 숨 쉬기
            _yeoul.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.2f) * 0.01f);
            if (_phase != Phase.Aim && _phase != Phase.Ready && !_thiefActive) _cat.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.8f + 1) * 0.02f);

            if (_shake > 0)
            {
                _shake -= dt;
                var rng = GameState.I.Rng;
                float a = _shakeAmp * Mathf.Clamp(_shake / 0.12f, 0.35f, 1);
                Art.Position = _shake > 0 ? new Vector2((float)rng.Range(-8, 8) * a, (float)rng.Range(-6, 6) * a) : Vector2.Zero;
            }

            switch (_phase)
            {
                case Phase.Aim:
                {
                    var a = G.Spot.StepAim(_angle, _aimDir, dt);
                    _angle = a.Angle;
                    _aimDir = a.Dir;
                    _field.Needle = (float)_angle;
                    // 바늘 쪽으로 대를 살짝 돌린다
                    _leanRate = 6;
                    _leanTarget = (float)_angle * 4;
                    break;
                }
                case Phase.Power:
                {
                    var g = G.Reel.StepPower(_gaugePos, _gaugeDir, dt);
                    _gaugePos = g.Pos;
                    _gaugeDir = g.Dir;
                    _gauge.Needle = (float)G.Reel.CastDistance(_gaugePos, Reach);
                    // 게이지가 찰수록 대를 뒤로 젖힌다
                    _leanRate = 14;
                    _leanTarget = -16 * (float)_gaugePos;
                    break;
                }
                case Phase.Wait:
                {
                    _bobT += dt;
                    _field.BobberAt = _target + new Vector2(0, Mathf.Sin(_bobT * 3) * 2);
                    if (_thiefActive)
                    {
                        UpdateThief(dt);
                        break;
                    }
                    _waitT += dt;
                    if (!_thiefDone && _thiefAt >= 0 && _waitT >= _thiefAt)
                    {
                        _catIdle = false;
                        StartThief();
                        break;
                    }
                    _idleT -= dt;
                    if (_idleT <= 0) WaitIdle();
                    var w = _wait.Step(dt, GameState.I.Rng);
                    if (w.Nibble) Nibble();
                    if (w.Bite) StartBite();
                    break;
                }
                case Phase.Bite:
                {
                    _biteElapsed += dt;
                    _field.BobberAt = _target + new Vector2(0, Mathf.Sin((float)_biteElapsed * 30) * 2);
                    if (G.Reel.BiteMissed(_biteElapsed, _biteWindow) && _tutorial)
                    {
                        // 튜토리얼은 늦어도 다시 기다린다
                        _phase = Phase.Wait;
                        _field.BobberSink = 0;
                        _wait.Retry();
                        Tell(T["missed"], T["earlyTutorial"], Palette.Gold);
                    }
                    else if (G.Reel.BiteMissed(_biteElapsed, _biteWindow))
                    {
                        _phase = Phase.Busy;
                        _field.BobberVisible = false;
                        _field.LineTo = null;
                        Settle(-8);
                        Sound.Play("slip", -4);
                        Tell(T["missed"], T["missedSub"]);
                        FinishCast();
                    }
                    break;
                }
                case Phase.Reel:
                    UpdateReel(dt);
                    break;
            }

            // 포즈 기울기: 목표를 부드럽게 따라가고, 기다릴 때는 살랑인다
            float sway = _phase == Phase.Wait ? Mathf.Sin(_time * 1.3f) * 1.2f : 0;
            if (_phase == Phase.Reel && _fight != null && _fight.Tension >= _fight.DangerAt) sway = Mathf.Sin(_time * 40) * 1.5f;
            _lean = Mathf.Lerp(_lean, _leanTarget + sway, 1 - Mathf.Exp(-_leanRate * dt));
            _yeoul.RotationDegrees = _lean * Face;
            UpdateRod();
        }

        /// <summary>줄을 거두고 대를 세운 포즈로 돌아간다. kick 만큼 뒤로 젖혔다가 천천히 바로 선다</summary>
        private void Settle(float kick)
        {
            SetPose("fish-ready");
            _lean = kick;
            _leanRate = 6;
            _leanTarget = 0;
        }

        /// <summary>속임 입질: 찌가 톡 잠겼다 올라온다</summary>
        private void Nibble()
        {
            Sound.Play("nibble");
            Haptics.Pulse(14, 0.15f);
            _nibbleTween?.Kill();
            _nibbleTween = CreateTween();
            _nibbleTween.TweenMethod(Callable.From<float>(v => _field.BobberSink = v), 0f, 0.45f, 0.06);
            _nibbleTween.TweenMethod(Callable.From<float>(v => _field.BobberSink = v), 0.45f, 0f, 0.25).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            var l = UiKit.Text(T["nibble"], TextSize.Small, Palette.Paper, true, false, 8, Palette.NightDeep);
            Toasts.AddChild(l);
            l.ResetSize();
            l.Position = _stage.ToLocal(_target) + new Vector2(30, -90);
            var t2 = l.CreateTween().SetParallel(true);
            t2.TweenProperty(l, "position:y", l.Position.Y - 40, 0.5);
            t2.TweenProperty(l, "modulate:a", 0f, 0.5);
            t2.Chain().TweenCallback(Callable.From(l.QueueFree));
        }

        // ───────── CI 사진 ─────────

        /// <summary>사진용: 방향과 거리를 정해 던지고 바로 입질을 받아 챔질한다</summary>
        private void ShotHook(string species, double size, bool big, double angle, double power)
        {
            _angle = angle;
            _gaugePos = power;
            Cast();
            _phase = Phase.Flying;
            Landed();
            _thiefAt = -1;
            StartBite();
            _species = G.Data.SpeciesById(species);
            _size = size;
            _big = big;
            _autoHold = true;
            StartReel(true);
        }

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "power":
                    // 반짝이는 지점 쪽으로 바늘을 멈추고 파워를 모으는 중
                    _angle = _hintSpot.Angle;
                    _field.Needle = (float)_angle;
                    StartPower();
                    break;
                case "reel":
                    // 던지고, 물고, 챔질한 뒤 텐션을 알맞게 유지하며 감는다.
                    // 첫 질주(0.7~1.4초)보다 먼저 찍히게 늦게 건다
                    Later(2.3, () => ShotHook("godeungeo", 33.5, false, 0, 1));
                    break;
                case "big":
                    // 큰 놈은 세서 사진 동안 줄이 터지지 않게 일찍 손을 뗀다 (버티는 포즈)
                    _shotPull = true;
                    ShotHook("gamseongdom", 52, true, 0.3, 1);
                    break;
                case "jump":
                    ShotHook("noraemi", 30, false, -0.2, 1);
                    // 사진 찍기 직전에 철썩
                    Later(2.35, () =>
                    {
                        _phase = Phase.Busy;
                        Thrash();
                    });
                    break;
                case "leap":
                    ShotHook("godeungeo", 34, false, 0.1, 1);
                    Later(2.35, () =>
                    {
                        _phase = Phase.Result;
                        _gauge.Showing = ReelGauge.Mode.None;
                        _field.ShadowVisible = false;
                        _field.AddBurst(_field.ShadowAt, 2.4f);
                    });
                    break;
                case "wait":
                    // 의자에 앉아 찌를 지켜본다 (입질이 사진 뒤로 밀리게 오래 기다린다)
                    _angle = 0.1;
                    _gaugePos = 0.8;
                    Cast();
                    _phase = Phase.Flying;
                    Landed();
                    _thiefAt = -1;
                    _wait.Scale(20);
                    break;
                case "bite":
                    // 앉아 있다가 찌가 쑥: 머리 위 "!"
                    _angle = 0.1;
                    _gaugePos = 0.8;
                    Cast();
                    _phase = Phase.Flying;
                    Landed();
                    _thiefAt = -1;
                    _wait.Scale(20);
                    Later(2.55, () =>
                    {
                        StartBite();
                        _biteWindow = 99;
                    });
                    break;
                case "thief":
                    _angle = 0.2;
                    _gaugePos = 0.6;
                    Cast();
                    _phase = Phase.Flying;
                    Landed();
                    _freezeThief = true;
                    StartThief();
                    _thiefT = (float)G.Spot.ThiefWindow * 0.55f;
                    break;
                case "result":
                    _species = G.Data.SpeciesById("noraemi");
                    _size = 27.3;
                    _phase = Phase.Result;
                    Celebrate(ShowResult);
                    break;
                case "joy":
                    _phase = Phase.Result;
                    Celebrate(() => { });
                    break;
                case "run":
                    // 물고기가 질주하는 순간 반대쪽을 눌러 버틴다 (첫 질주가 1.3~2초에 온다)
                    ShotHook("noraemi", 32, false, 0.2, 1);
                    break;
                case "pull":
                    // reel 과 같지만 세게 버티는 포즈로
                    _shotPull = true;
                    ShotSetup("reel");
                    break;
                case "tutorialCatch":
                    _species = G.Data.SpeciesById("bollak");
                    _size = 19.4;
                    _phase = Phase.Result;
                    TutorialCatch();
                    break;
                case "new":
                    _species = G.Data.SpeciesById("gamseongdom");
                    _size = 38.4;
                    _phase = Phase.Result;
                    ShowResult();
                    break;
            }
        }
    }
}

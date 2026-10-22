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
    /// 방파제 찌낚시 (웹 FishingScene). 방향 고르기 → 파워 게이지 → 입질 대기(속임 입질) → 챔질 → 릴링 → 결과 → 철수.
    /// 판정은 코어 ReelRules·BiteWait·ReelFight 가 하고, 여기서는 그리기와 입력만 한다.
    /// 여울은 SD 뒷모습, 처음 낚은 어종은 감정 일러스트 대화로 기뻐한다.
    /// </summary>
    public partial class FishingScreen : Screen
    {
        private enum Phase
        {
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
        private static readonly Vector2 YeoulFoot = new Vector2(186, 684);
        private const float YeoulHeight = 168;
        private static readonly Vector2 CatFoot = new Vector2(362, 684);
        // 시간대 [새벽, 아침, 낮, 저녁] 배경 색
        private static readonly Color[] SlotTint = { new Color("c4c0e2"), new Color("fff4e6"), Colors.White, new Color("ffcfb2") };

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
        private int _lane;
        private int _hotLane;
        private int _hintLane;
        private double _gaugePos;
        private int _gaugeDir = 1;
        private double _dist01;
        private Vector2 _target;
        private float _targetScale;
        private bool _hot;
        private BiteWait _wait;
        private double _biteWindow;
        private double _biteElapsed;
        private Species _species;
        private double _size;
        private ReelFight _fight;
        private bool _holding;
        private bool _autoHold;
        private float _wobble;
        private float _time;
        private float _bobT;
        private float _shake;
        private Tween _zoomTween, _nibbleTween;

        protected override void Build()
        {
            var s = G.Save;
            _castsLeft = G.Data.Balance.Chapter1.CastsPerTrip;

            // ── 그림: 방파제 배경, 여울 뒷모습(SD), 고등어 ──
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = GD.Load<Texture2D>("res://art/backgrounds/breakwater.png");
            _stage.Tint = SlotTint[Mathf.Clamp(s.Slot, 0, 3)];
            Art.AddChild(_stage);
            _yeoul = _stage.Place(GD.Load<Texture2D>("res://art/yeoul/back.png"), YeoulFoot, YeoulHeight);
            _cat = _stage.Place(GD.Load<Texture2D>("res://art/cat/sit-side.png"), CatFoot, 62);

            // 손 위치는 SD 뒷모습 그림에서 오른손 자리
            float w = YeoulHeight * 222f / 491f;
            var hand = new Vector2(YeoulFoot.X - w / 2 + w * 0.86f, YeoulFoot.Y - YeoulHeight * 0.31f);
            _field = new FishingField(_stage)
            {
                Hand = hand,
                TipIdle = new Vector2(276, 486),
                LanePos = (lane, d) => PosFor(lane, d),
            };
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

        /// <summary>방향과 거리(0 가까이, 1 수평선)로 물 위 자리. z 는 그 거리에서의 크기</summary>
        private static Vector3 PosFor(int lane, float d)
        {
            return new Vector3(
                ArtStage.Logical.X / 2 + lane * (44 + 74 * (1 - d)),
                Shore - 14 - d * (Shore - Horizon - 40),
                1.6f - d * 0.9f);
        }

        private string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T[key], args);
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
            _info.Text = F("info", _castsLeft, G.Data.Balance.Chapter1.CastsPerTrip, s.Bait, s.Cooler.Count, G.CoolerCapacity());
            _hud.Refresh();
        }

        private double Reach
        {
            get { return G.Data.Gear.Rods[G.Save.Rod].Reach; }
        }

        /// <summary>화면이 바뀐 뒤 불려도 괜찮은 지연 호출</summary>
        private void Later(double seconds, Action then)
        {
            GetTree().CreateTimer(seconds).Timeout += () =>
            {
                if (IsInstanceValid(this) && IsInsideTree()) then();
            };
        }

        // ───────── 입력 ─────────

        private void OnFieldInput(InputEvent e)
        {
            if (e is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left) return;
            if (!mb.Pressed)
            {
                _holding = false;
                return;
            }
            var p = _stage.ToLogical(mb.Position);
            switch (_phase)
            {
                case Phase.Aim:
                    if (_cat.GetRect().HasPoint(mb.Position))
                    {
                        ShowCatHint();
                        return;
                    }
                    if (p.Y > Shore + 10 || p.Y < Horizon) return;
                    _lane = p.X < 150 ? -1 : p.X > 300 ? 1 : 0;
                    StartPower();
                    break;
                case Phase.Power:
                    Cast();
                    break;
                case Phase.Wait:
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

        private void StartAim()
        {
            _phase = Phase.Aim;
            _gauge.Showing = ReelGauge.Mode.None;
            _field.BobberVisible = false;
            _field.ShadowVisible = false;
            _field.LineTo = null;
            _field.Bend = 0;
            _field.ShowLanes = true;
            _field.SelectedLane = null;
            _exit.SetEnabled(true);
            SetInfo();

            var lanes = G.Reel.RollLanes(G.Save.CatAffinity, GameState.I.Rng);
            _hotLane = lanes.Hot;
            _hintLane = lanes.Hint;
            Tell(T["aim"], T["aimSub"]);
            ShowCatHint();
        }

        /// <summary>고등어가 울며 한쪽을 본다. 그쪽 물결이 일렁인다</summary>
        private void ShowCatHint()
        {
            if (_phase != Phase.Aim) return;
            HideCatBubble();
            string dir = _hintLane == -1 ? T["left"] : _hintLane == 1 ? T["right"] : T["front"];
            _catBubble = Bubble(Art, F("catHint", dir), _stage.ToLocal(CatFoot + new Vector2(-8, -60)), 0);
            _stage.Swap(_cat, GD.Load<Texture2D>("res://art/cat/" + (_hintLane == 0 ? "sit-front" : "sit-side") + ".png"));
            _cat.FlipH = _hintLane == 1;
            Hop(_cat, 12);
            _field.HintLane = _hintLane;
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

        private void StartPower()
        {
            if (G.Save.Bait <= 0)
            {
                _phase = Phase.Busy;
                Tell(T["noBait"], T["noBaitSub"]);
                Later(1.4, EndTrip);
                return;
            }
            _phase = Phase.Power;
            _exit.SetEnabled(false);
            HideCatBubble();
            _field.ShowHint = false;
            _field.SelectedLane = _lane;
            _gaugePos = 0;
            _gaugeDir = 1;
            float reach = (float)Reach;
            _gauge.Bound1 = Mathf.Min(1, 0.35f / reach);
            _gauge.Bound2 = Mathf.Min(1, 0.7f / reach);
            _gauge.Needle = 0;
            _gauge.Showing = ReelGauge.Mode.Power;
            Tell(T["power"], T["powerSub"]);
        }

        private void Cast()
        {
            _dist01 = G.Reel.CastDistance(_gaugePos, Reach);
            G.UseBait();
            _castsLeft -= 1;
            _castsDone += 1;
            SetInfo();
            _gauge.Showing = ReelGauge.Mode.None;
            _field.ShowLanes = false;
            _phase = Phase.Flying;
            _hot = _lane == _hotLane;

            var to = PosFor(_lane, (float)_dist01);
            _target = new Vector2(to.X, to.Y);
            _targetScale = to.Z;
            Tell(F("cast", G.Data.Texts.Zones[G.Fishing.ZoneOf(_dist01)]));

            // 찌가 포물선을 그리며 날아간다
            var from = _field.TipIdle;
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
                _field.BobberScale = Mathf.Lerp(1.4f, _targetScale, t);
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
            _wait = G.Reel.StartWait(_hot, false, GameState.I.Rng);
            Tell(T["wait"], _hot ? T["waitHot"] : T["waitSub"]);
        }

        private void EarlyStrike()
        {
            _phase = Phase.Busy;
            Tell(T["early"], T["earlySub"]);
            _field.AddSplash(_target, _targetScale * 0.8f);
            _field.BobberVisible = false;
            _field.LineTo = null;
            FinishCast();
        }

        private void StartBite()
        {
            _phase = Phase.Bite;
            _biteElapsed = 0;
            int zone = G.Fishing.ZoneOf(_dist01);
            _species = G.Fishing.RollSpecies(zone, G.Save.Slot, _hot, GameState.I.Rng);
            _size = G.Fishing.RollSize(_species, _hot, GameState.I.Rng);
            _biteWindow = G.Reel.BiteWindow(G.Data.Gear.Rods[G.Save.Rod], _species, false);
            _nibbleTween?.Kill();
            _field.BobberSink = 1;
            _field.AddSplash(_target, _targetScale * 1.4f);
            Shake(0.12f);
            Tell(T["bite"], T["biteSub"], Palette.Gold);
        }

        private void Strike()
        {
            bool perfect = G.Reel.IsPerfectStrike(_biteElapsed);
            StartReel(perfect);
        }

        private void StartReel(bool perfect)
        {
            _phase = Phase.Reel;
            _field.BobberVisible = false;
            _field.ShadowVisible = true;
            var s = G.Save;
            _fight = G.Reel.StartFight(new FightSetup
            {
                Species = _species,
                Size = _size,
                Dist01 = _dist01,
                Perfect = perfect,
                Rod = G.Data.Gear.Rods[s.Rod],
                Reel = G.Data.Gear.Reels[s.Reel],
                Tutorial = false,
            }, GameState.I.Rng);
            _wobble = (float)GameState.I.Rng.Next() * 10;
            _field.ShadowSize = Mathf.Clamp((float)(_size / _species.AvgSize), 0.7f, 1.6f);
            var b = G.Data.Balance.Reel;
            _gauge.Slack = (float)b.Slack;
            _gauge.Danger = (float)b.Danger;
            _gauge.Drag = (float)_fight.Setup.Reel.Drag;
            _gauge.Tension = (float)_fight.Tension;
            _gauge.Showing = ReelGauge.Mode.Tension;
            Toast(perfect ? T["perfect"] : T["hooked"], perfect ? Palette.Gold : Palette.Paper, SafeTop + 420);
            Tell(T["reel"], T["reelSub"]);
            // 화면이 살짝 다가간다
            ZoomTo(1.05f, 0.5);
        }

        private void UpdateReel(float dt)
        {
            var f = _fight;
            if (_autoHold) _holding = f.Tension < (_holding ? 72 : 52);
            var r = f.Step(_holding, dt, GameState.I.Rng);
            double str = f.Strength();
            if (r.Surge)
            {
                Shake(0.1f);
                Toast(T["surge"], Palette.RedText, SafeTop + 420);
                _field.AddSplash(_field.ShadowAt, 1.2f);
            }
            if (r.End == FightEnd.Snap)
            {
                Lose(T["snap"], T["snapSub"]);
                return;
            }
            if (r.End == FightEnd.Slack)
            {
                Lose(T["slack"], T["slackSub"]);
                return;
            }
            if (r.End == FightEnd.Landed)
            {
                LandFish();
                return;
            }

            // 그림자는 좌우로 몸부림치며 다가온다
            float p = (float)f.Progress();
            _wobble += dt * (float)(2 + str * 0.6 + f.SurgeKick * 20);
            var from = PosFor(_lane, (float)_dist01);
            float fx = Mathf.Lerp(from.X, ArtStage.Logical.X / 2 + 10, p) + Mathf.Sin(_wobble) * (18 + (float)f.SurgeKick * 40) * (1 - p * 0.6f);
            float fy = Mathf.Lerp(from.Y, Shore - 16, p);
            _field.ShadowAt = new Vector2(fx, fy);
            _field.ShadowWobble = _wobble;
            _field.ShadowSize = Mathf.Clamp((float)(_size / _species.AvgSize), 0.7f, 1.6f) * Mathf.Lerp(0.6f, 1.3f, p);
            _field.LineTo = _field.ShadowAt;
            _field.Bend = Mathf.Clamp((float)f.Tension / 100, 0, 1);
            _gauge.Tension = (float)f.Tension;
            _sub.Text = F("distance", Math.Max(0, f.Distance).ToString("0.0", CultureInfo.InvariantCulture));
            // 텐션이 위험하면 큰 글이 빨개진다
            _msg.LabelSettings.FontColor = f.Tension >= G.Data.Balance.Reel.Danger ? Palette.RedText : f.Tension < G.Data.Balance.Reel.Slack ? Palette.BlueText : Palette.Paper;
        }

        private void Lose(string main, string sub)
        {
            _phase = Phase.Busy;
            _gauge.Showing = ReelGauge.Mode.None;
            _field.ShadowVisible = false;
            _field.AddSplash(_field.ShadowAt, 1.5f);
            _field.LineTo = null;
            _field.Bend = 0;
            ZoomTo(1, 0.6);
            Tell(main, sub);
            FinishCast();
        }

        private void LandFish()
        {
            _phase = Phase.Result;
            _gauge.Showing = ReelGauge.Mode.None;
            _field.ShadowVisible = false;
            _field.AddSplash(_field.ShadowAt, 1.6f);
            _field.LineTo = null;
            _field.Bend = 0;
            ZoomTo(1, 0.6);
            Tell("");
            ShowResult();
        }

        /// <summary>낚은 고기. 처음 보는 어종이면 여울이 먼저 기뻐한다 (감정 일러스트)</summary>
        private void ShowResult()
        {
            DexEntry e;
            bool isNew = !G.Save.Dex.TryGetValue(_species.Id, out e) || e.Count == 0;
            if (isNew)
            {
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
            var body = new List<string> { size.ToString("0.0", CultureInfo.InvariantCulture) + "cm" + (sp.Rare ? " · " + T["rare"] : "") };
            body.Add(F("value", UiKit.Won(value)));
            if (rule != null) body.Add(rule);
            else if (full) body.Add(T["full"]);

            Modal(sp.Name + (isNew ? " (새 어종!)" : ""), string.Join("\n", body), new[]
            {
                new ModalButton(T["release"], Palette.BlueDark, () => Finish(true)),
                new ModalButton(rule != null ? T["mustRelease"] : full ? T["coolerFull"] : T["keep"], Palette.Green, () => Finish(false), full || rule != null),
            }, FishPicture(sp.Id));
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
            if (!released) G.AddToCooler(_species.Id, _size);
            else s.Flags["released"] = true;
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
            Later(1.2, () =>
            {
                if (_castsLeft <= 0 || G.Save.Bait <= 0) EndTrip();
                else StartAim();
            });
        }

        private void ConfirmExit()
        {
            if (_phase != Phase.Aim) return;
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
            if (_castsDone > 0) G.UseSlot();
            GameState.I.GoTo("town");
        }

        // ───────── 연출 ─────────

        private void Shake(float seconds)
        {
            _shake = Mathf.Max(_shake, seconds);
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
            if (_phase != Phase.Aim) _cat.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.8f + 1) * 0.02f);

            if (_shake > 0)
            {
                _shake -= dt;
                var rng = GameState.I.Rng;
                Art.Position = _shake > 0 ? new Vector2((float)rng.Range(-8, 8), (float)rng.Range(-6, 6)) : Vector2.Zero;
            }

            switch (_phase)
            {
                case Phase.Power:
                {
                    var g = G.Reel.StepPower(_gaugePos, _gaugeDir, dt);
                    _gaugePos = g.Pos;
                    _gaugeDir = g.Dir;
                    _gauge.Needle = (float)_gaugePos;
                    break;
                }
                case Phase.Wait:
                {
                    _bobT += dt;
                    _field.BobberAt = _target + new Vector2(0, Mathf.Sin(_bobT * 3) * 2);
                    var w = _wait.Step(dt, GameState.I.Rng);
                    if (w.Nibble) Nibble();
                    if (w.Bite) StartBite();
                    break;
                }
                case Phase.Bite:
                {
                    _biteElapsed += dt;
                    _field.BobberAt = _target + new Vector2(0, Mathf.Sin((float)_biteElapsed * 30) * 2);
                    if (G.Reel.BiteMissed(_biteElapsed, _biteWindow))
                    {
                        _phase = Phase.Busy;
                        _field.BobberVisible = false;
                        _field.LineTo = null;
                        Tell(T["missed"], T["missedSub"]);
                        FinishCast();
                    }
                    break;
                }
                case Phase.Reel:
                    UpdateReel(dt);
                    break;
            }
        }

        /// <summary>속임 입질: 찌가 톡 잠겼다 올라온다</summary>
        private void Nibble()
        {
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

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "power":
                    _lane = 1;
                    StartPower();
                    break;
                case "reel":
                    // 던지고, 물고, 챔질한 뒤 텐션을 알맞게 유지하며 감는다
                    _lane = 0;
                    _gaugePos = 0.62;
                    Cast();
                    _phase = Phase.Flying;
                    Landed();
                    StartBite();
                    _species = G.Data.SpeciesById("godeungeo");
                    _size = 33.5;
                    _autoHold = true;
                    StartReel(true);
                    break;
                case "result":
                    _species = G.Data.SpeciesById("noraemi");
                    _size = 27.3;
                    _phase = Phase.Result;
                    ShowResult();
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

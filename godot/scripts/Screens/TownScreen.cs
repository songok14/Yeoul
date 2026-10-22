using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 물비늘포 마을 (웹 TownScene). 시간대별 배경, 건물 간판, SD 여울과 고등어, 트럭, 아래 메뉴 여섯 개.
    /// 들어올 때 1장 결말과 어제 요약을 확인한다. 여울식당에서 빚을 갚고 잠을 잔다.
    /// </summary>
    public partial class TownScreen : Screen
    {
        private static readonly string[] YeoulPoses = { "front", "wave", "three-quarter", "idle", "side" };
        private static readonly string[] CatRest = { "sit-front", "sit-side", "loaf" };

        private CoreGame G
        {
            get { return GameState.I.Game; }
        }

        private ArtStage _stage;
        private Hud _hud;
        private PanelContainer _menu;
        private TextureRect _yeoul, _cat, _truck;
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private float _time;
        private float _catTimer = 5;
        private float _yeoulReset;
        private float _footY = 636;
        private int _yeoulLine;

        protected override void Build()
        {
            var s = G.Save;

            // ── 그림 ──
            _stage = new ArtStage();
            _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.Background = Tex("backgrounds/village-" + Mathf.Clamp(s.Slot, 0, 3));
            Art.AddChild(_stage);

            _stage.Pin(new SignButton("위판장", OpenMarket), new Vector2(70, 300));
            _stage.Pin(new SignButton("미끼가게", OpenShop), new Vector2(118, 448));
            _stage.Pin(new SignButton("여울식당", Restaurant), new Vector2(372, 352));
            _stage.Pin(new SignButton("방파제 →", GoFishing), new Vector2(365, 262));

            _truck = _stage.Place(Tex("props/truck"), new Vector2(300, 600), 78);
            Tappable(_truck, OpenTruck);
            _cat = _stage.Place(Tex("cat/sit-front"), new Vector2(118, 636), 58);
            Tappable(_cat, TapCat);
            _yeoul = _stage.Place(Tex("yeoul/front"), new Vector2(205, 636), 138);
            Tappable(_yeoul, TapYeoul);

            // ── 위: HUD 와 오늘 소식 ──
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 18);
            Safe.AddChild(col);
            _hud = new Hud(G);
            col.AddChild(_hud);
            col.AddChild(NewsRibbon());
            col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            // ── 아래: 메뉴 ──
            _menu = new PanelContainer();
            col.AddChild(_menu);
            var grid = new GridContainer { Columns = 3 };
            grid.AddThemeConstantOverride("h_separation", 22);
            grid.AddThemeConstantOverride("v_separation", 22);
            _menu.AddChild(grid);
            grid.AddChild(new GameButton("방파제", Palette.Blue, GoFishing, "낚시 · 1칸"));
            grid.AddChild(new GameButton("위판장", Palette.Teal, OpenMarket, G.IsMarketOpen() ? "경매 중" : "경매 마감"));
            var truck = new GameButton("여울호", Palette.Red, OpenTruck, G.IsTruckOpen() ? "장사 · 1칸" : "새벽엔 쉼");
            truck.SetEnabled(G.IsTruckOpen());
            grid.AddChild(truck);
            grid.AddChild(new GameButton("미끼가게", Palette.GreenDark, OpenShop, "장비 · 미끼"));
            grid.AddChild(new GameButton("여울식당", Palette.Wood, Restaurant, "상환 · 휴식"));
            grid.AddChild(new GameButton("수첩", Palette.Purple, OpenNotebook, "도감 · 미각"));
            _menu.Resized += PlaceOnFloor;
            OnSafeAreaChanged();

            // 메뉴가 아래에서 올라오고 HUD 가 위에서 내려온다
            _menu.Modulate = new Color(1, 1, 1, 0);
            _hud.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetParallel(true);
            tw.TweenProperty(_menu, "modulate:a", 1f, 0.45).SetDelay(0.1);
            tw.TweenProperty(_hud, "modulate:a", 1f, 0.45);

            CallDeferred(MethodName.OnEnter);
        }

        /// <summary>HUD 는 노치 밑까지, 메뉴는 제스처 바 밑까지 칠한다. 글자와 버튼만 안전 영역 안에 둔다</summary>
        protected override void OnSafeAreaChanged()
        {
            if (_menu == null) return;
            Safe.AddThemeConstantOverride("margin_top", 0);
            Safe.AddThemeConstantOverride("margin_bottom", 0);
            var hudBox = (StyleBoxFlat)_hud.GetThemeStylebox("panel").Duplicate();
            hudBox.ContentMarginTop = 22 + SafeTop;
            _hud.AddThemeStyleboxOverride("panel", hudBox);
            var sb = UiKit.Box(new Color(Palette.Panel, 0.97f), 0, 30, 30).WithShadow(0.4f, 24, -4);
            sb.CornerRadiusTopLeft = 44;
            sb.CornerRadiusTopRight = 44;
            sb.BorderColor = new Color(1, 1, 1, 0.08f);
            sb.BorderWidthTop = 3;
            sb.ContentMarginBottom = 30 + SafeBottom;
            _menu.AddThemeStyleboxOverride("panel", sb);
        }

        private Control NewsRibbon()
        {
            var closed = G.ClosedToday();
            string headline = closed.Count > 0 ? string.Join("·", closed) + " 금어기" : G.Market.Headline(G.Save.Day);
            var pill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            pill.AddThemeStyleboxOverride("panel", UiKit.Box(new Color(Palette.NightDeep, 0.62f), 30, 30, 10));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 16);
            row.AddChild(UiKit.Text(G.DateLabel(), TextSize.Small - 2, Palette.Gold, true));
            var news = UiKit.Text(headline, TextSize.Small - 2, Palette.Paper);
            news.CustomMinimumSize = new Vector2(Mathf.Min(UiKit.Measure(headline, TextSize.Small - 2) + 4, 760), 0);
            news.Ellipsis();
            row.AddChild(news);
            pill.AddChild(row);
            return Riser(pill);
        }

        /// <summary>여울과 고등어, 트럭이 메뉴 위 길에 서도록. 화면이 길면 그림 속 원래 자리</summary>
        private void PlaceOnFloor()
        {
            float menuTop = _menu.GetGlobalRect().Position.Y - _stage.GetGlobalRect().Position.Y;
            _footY = Mathf.Min(640, _stage.ToLogical(new Vector2(0, menuTop)).Y + 6);
            _stage.Move(_yeoul, new Vector2(205, _footY));
            _stage.Move(_cat, new Vector2(118, _footY));
            _stage.Move(_truck, new Vector2(300, _footY - 36));
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

        private static void Tappable(Control c, Action onTap)
        {
            c.MouseFilter = MouseFilterEnum.Stop;
            c.MouseDefaultCursorShape = CursorShape.PointingHand;
            c.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) onTap();
            };
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _time += dt;
            // 숨 쉬기
            _yeoul.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.2f) * 0.012f);
            _cat.Scale = new Vector2(1, 1 + Mathf.Sin(_time * 2.8f + 1) * 0.02f);
            _stage.Zoom = 1.01f + Mathf.Sin(_time * 0.1f) * 0.01f;

            // 여울은 손 흔든 뒤 정면으로
            if (_yeoulReset > 0)
            {
                _yeoulReset -= dt;
                if (_yeoulReset <= 0) _stage.Swap(_yeoul, Tex("yeoul/front"));
            }
            // 고등어는 가끔 자세를 바꾼다
            _catTimer -= dt;
            if (_catTimer <= 0)
            {
                _catTimer = 4 + (float)GameState.I.Rng.Next() * 5;
                _stage.Swap(_cat, Tex("cat/" + CatRest[(int)(GameState.I.Rng.Next() * CatRest.Length)]));
            }
        }

        // ───────── 들어올 때 ─────────

        private void OnEnter()
        {
            var gs = GameState.I;
            var outcome = G.CheckChapter();
            if (outcome == ChapterOutcome.Clear)
            {
                G.Persist();
                TalkScene("chapterClear", () => Modal("1차 상환 완료!", "조황일지 1장을 얻었다.\n이야기는 다음 빌드에서 이어져요.", new[] { new ModalButton("좋아", Palette.Green) }));
                return;
            }
            if (outcome == ChapterOutcome.Fail)
            {
                TalkScene("chapterFail", () =>
                {
                    gs.DeleteSave();
                    gs.GoTo("title");
                });
                return;
            }
            if (gs.PendingTalk != null)
            {
                string talk = gs.PendingTalk;
                gs.PendingTalk = null;
                TalkScene(talk);
                return;
            }
            var s = G.Save;
            if (s.PendingSummary != null)
            {
                var sum = s.PendingSummary;
                s.PendingSummary = null;
                G.Persist();
                var lines = new List<string> { "어제 번 돈 " + UiKit.Won(sum.Earned) };
                if (sum.Spoiled > 0) lines.Add("쿨러에서 " + sum.Spoiled + "마리가 상했다");
                if (sum.ExpiredRequests > 0) lines.Add("의뢰 " + sum.ExpiredRequests + "건이 기한을 넘겼다");
                lines.Add("위판장 시세가 바뀌었다");
                int left = G.DaysLeft();
                lines.Add(s.Debt > 0 ? "상환까지 " + (left > 0 ? left + "일" : "오늘이 마지막 날") : "1차 상환은 끝났다");
                Modal(s.Day + "일차 " + G.SlotName(), string.Join("\n", lines), new[] { new ModalButton("좋아", Palette.Green) });
            }
        }

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "restaurant":
                    Restaurant();
                    break;
                case "welcome":
                    TalkScene("welcome");
                    break;
                case "clear":
                    TalkScene("chapterClear");
                    break;
                case "yeoul":
                    TapYeoul();
                    TapCat();
                    break;
            }
        }

        // ───────── 캐릭터 ─────────

        private void TapYeoul()
        {
            string pose = YeoulPoses[1 + _yeoulLine % (YeoulPoses.Length - 1)];
            _stage.Swap(_yeoul, Tex("yeoul/" + pose));
            _yeoulReset = 2.2f;
            var lines = G.Data.Dialogue.YeoulIdle;
            string line = lines[_yeoulLine++ % lines.Count];
            Hop(_yeoul, 10);
            Bubble(Art, line, _stage.ToLocal(new Vector2(205, _footY - 142)), 2.2f);
        }

        private void TapCat()
        {
            int bad = G.SpoiledFish().Count;
            if (bad > 0)
            {
                Modal("고등어가 쿨러를 쳐다본다", "상한 생선 " + bad + "마리를 고등어에게 줄까?\n친해지면 입질 방향을 더 정확히 알려 준다.", new[]
                {
                    new ModalButton("아니", Palette.GrayDark),
                    new ModalButton("주기", Palette.Green, () =>
                    {
                        G.FeedCat();
                        _stage.Swap(_cat, Tex("cat/eat"));
                        _catTimer = 3;
                        Toast("고등어가 골골거린다 (호감 " + G.Save.CatAffinity + "/" + G.Data.Balance.Cat.MaxAffinity + ")");
                    }),
                });
                return;
            }
            var idle = G.Data.Dialogue.CatIdle;
            _stage.Swap(_cat, Tex("cat/meow-1"));
            _catTimer = 1.6f;
            Hop(_cat, 14);
            Bubble(Art, idle[(int)(GameState.I.Rng.Next() * idle.Count)], _stage.ToLocal(new Vector2(118, _footY - 62)), 1.3f);
        }

        /// <summary>제자리에서 톡 뛴다 (발밑 기준 크기 튕김)</summary>
        private static void Hop(Control c, float px)
        {
            var tw = c.CreateTween();
            float y = c.Position.Y;
            tw.TweenProperty(c, "position:y", y - px, 0.1).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(c, "position:y", y, 0.16).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        }

        // ───────── 메뉴 ─────────

        private void GoFishing()
        {
            if (G.Save.Bait <= 0)
            {
                Modal("미끼가 없다", "노씨 가게에서 크릴을 사 오자.", new[]
                {
                    new ModalButton("닫기", Palette.GrayDark),
                    new ModalButton("미끼가게", Palette.GreenDark, OpenShop),
                });
                return;
            }
            GameState.I.GoTo("fishing");
        }

        private void OpenMarket()
        {
            GameState.I.GoTo("market");
        }

        private void OpenTruck()
        {
            if (!G.IsTruckOpen())
            {
                Toast("새벽엔 손님이 없다");
                return;
            }
            GameState.I.GoTo("truck");
        }

        private void OpenShop()
        {
            GameState.I.GoTo("shop");
        }

        private void OpenNotebook()
        {
            GameState.I.GoTo("notebook");
        }

        /// <summary>여울식당: 할머니가 한마디 하고, 대화창 위에 갚기·잠자기 선택지가 뜬다</summary>
        private void Restaurant()
        {
            var s = G.Save;
            int canPay = Math.Min(s.Money, s.Debt);
            int step = G.Data.Balance.Chapter1.RepayStep;
            var lines = G.Data.Dialogue.Grandma;
            string line = lines[(int)(GameState.I.Rng.Next() * lines.Count)];
            Say("grandma", "default", line, new[]
            {
                new ModalButton("10만 원 갚기", Palette.GoldDark, () => Pay(step), s.Debt <= 0 || s.Money < step),
                new ModalButton("있는 돈 전부 갚기 (" + UiKit.Won(canPay) + ")", Palette.Red, () => Pay(canPay), canPay <= 0),
                new ModalButton("하루 마치기 (잠자기)", Palette.BlueDark, Sleep),
                new ModalButton("나가기", Palette.GrayDark),
            });
        }

        private void Pay(int amount)
        {
            int paid = G.Repay(amount);
            if (paid <= 0) return;
            _hud.Refresh();
            FloatText("-" + UiKit.Won(paid), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 420), Palette.RedText);
            if (G.Save.Debt <= 0) GetTree().CreateTimer(0.9).Timeout += OnEnter;
            else Toast("할머니가 말없이 손을 잡아 주셨다");
        }

        private void Sleep()
        {
            Modal("오늘을 마칠까요?", G.SlotName() + " 이후의 시간이 지나가고\n다음 날 새벽이 됩니다.\n쿨러의 생선 신선도가 떨어져요.", new[]
            {
                new ModalButton("아니", Palette.GrayDark),
                new ModalButton("잠자기", Palette.BlueDark, () =>
                {
                    G.EndDay();
                    GameState.I.GoTo("town");
                }),
            });
        }
    }
}

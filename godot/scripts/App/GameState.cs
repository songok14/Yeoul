using System;
using System.Threading.Tasks;
using Godot;
using Hanjeopsi.App.Boot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.App
{
    /// <summary>
    /// 자동 로드 하나 (project.godot [autoload]). 게임 데이터, 지금 판(Game), 난수, 화면 전환, CI 사진 모드를 맡는다.
    /// 화면은 GameState.I.Game 으로 상태를 읽고 바꾸며, 이동은 GoTo("town") 처럼 한다 (웹 goTo).
    /// </summary>
    public partial class GameState : Node
    {
        public static GameState I { get; private set; }

        public GameData Data { get; private set; }
        public CoreGame Game { get; private set; }
        /// <summary>게임 중 난수 (웹 Math.random). 사진 모드에서는 시드 고정</summary>
        public IRandom Rng { get; private set; } = new SystemRandom();

        // 사진 모드: -- --shot=<경로> --screen=<화면> [--shot-action=<이름>] [--demo=<종류>]
        public string ShotPath { get; private set; }
        public string ShotScreen { get; private set; }
        public string ShotAction { get; private set; }
        public string Demo { get; private set; }
        /// <summary>화면 확인용 날씨 인자: rain, rain-dusk, snow, typhoon, fog (게임 날씨는 아직 없다)</summary>
        public string Weather { get; private set; }
        public bool ShotMode
        {
            get { return ShotPath != null; }
        }

        private CanvasLayer _fadeLayer;
        private ColorRect _fade;
        private Tween _fadeTween;
        private bool _shotTaken;

        public override void _EnterTree()
        {
            I = this;
            foreach (var a in OS.GetCmdlineUserArgs())
            {
                if (a.StartsWith("--shot=")) ShotPath = a.Substring("--shot=".Length);
                else if (a.StartsWith("--screen=")) ShotScreen = a.Substring("--screen=".Length);
                else if (a.StartsWith("--shot-action=")) ShotAction = a.Substring("--shot-action=".Length);
                else if (a.StartsWith("--demo=")) Demo = a.Substring("--demo=".Length);
                else if (a.StartsWith("--weather=")) Weather = a.Substring("--weather=".Length);
                else if (a == "--tutorial") Tutorial = true;
            }
            if (ShotMode) Rng = new Mulberry32(2026);
            AddChild(new Sound { Name = "Sound" });

            // 화면 전환용 막. 앱이 켜질 때는 덮인 채로 시작해서 첫 화면이 스르륵 나타난다
            _fadeLayer = new CanvasLayer { Layer = 100 };
            _fade = new ColorRect { Color = Palette.NightDeep, MouseFilter = Control.MouseFilterEnum.Stop };
            _fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _fadeLayer.AddChild(_fade);
            AddChild(_fadeLayer);
        }

        /// <summary>데이터를 읽는다. 실패하면 예외 (Boot 가 화면에 보여 준다)</summary>
        public void LoadData()
        {
            Data ??= GameDataLoader.Load();
        }

        // ───────── 세이브 ─────────

        public bool HasSave()
        {
            return !ShotMode && SaveStore.Has();
        }

        public void NewGame()
        {
            int seed = (int)(GD.Randi() % 999_999_999) + 1;
            Attach(CoreGame.NewGame(Data, seed));
            if (!ShotMode) SaveStore.Write(Game.Save);
        }

        /// <summary>저장된 판을 불러온다. 못 읽으면 false</summary>
        public bool Continue()
        {
            SaveData save;
            try
            {
                save = SaveStore.Load(Data);
            }
            catch (Exception e)
            {
                GD.PushWarning("세이브를 읽지 못했다: " + e.Message);
                return false;
            }
            if (save == null) return false;
            Attach(new CoreGame(Data, save));
            return true;
        }

        public void DeleteSave()
        {
            if (!ShotMode) SaveStore.Delete();
        }

        private void Attach(CoreGame g)
        {
            if (Game != null) Game.Saved -= Write;
            Game = g;
            Game.Saved += Write;
            // 사진 확인용 날씨 고정 (rain-dusk 는 저녁 비)
            if (!string.IsNullOrEmpty(Weather)) Game.ForcedWeather = Weather == "rain-dusk" ? "rain" : Weather;
        }

        private void Write(SaveData save)
        {
            if (!ShotMode) SaveStore.Write(save);
        }

        /// <summary>
        /// 사진과 화면 확인용 체험 판 (웹 BootScene setupDemo). 4일차, 소지금 86,000원, 빚 264만 원, 쿨러 6마리.
        /// kind: lunch 면 점심, evening 이면 저녁, night 면 밤, summary 면 새벽에 어제 요약이 뜬다
        /// </summary>
        public void SetupDemo(string kind)
        {
            Attach(CoreGame.NewGame(Data, 12345));
            var s = Game.Save;
            Game.CompletePrologue();
            s.Money = 86_000;
            s.Day = 4;
            s.Slot = kind == "night" ? 4 : kind == "lunch" ? 2 : kind == "evening" ? 3 : kind == "summary" ? 0 : 1;
            s.Debt = 2_640_000;
            var catches = new (string Species, double Size, int Fresh)[]
            {
                ("bollak", 19.5, 100), ("bollak", 22.1, 60), ("godeungeo", 31.2, 100),
                ("noraemi", 26.4, 35), ("noraemi", 21, 100), ("hakgongchi", 33.3, 60),
            };
            foreach (var c in catches)
            {
                s.Cooler.Add(new FishItem(s.NextUid++, c.Species, c.Size, s.Day, c.Fresh));
                DexEntry e;
                if (!s.Dex.TryGetValue(c.Species, out e)) e = new DexEntry();
                e.Count += 1;
                e.Best = Math.Max(e.Best, c.Size);
                s.Dex[c.Species] = e;
            }
            s.Flags["taste2"] = true;
            if (kind == "gear" && s.Kit != null)
            {
                // 장비를 갖춘 판: 3단계 낚싯대·릴, 4단계 찌
                s.Rod = 2;
                s.Reel = 2;
                s.Kit.Own["float"] = 4;
            }
            if (kind == "summary") s.PendingSummary = new DaySummary { Day = 3, Earned = 124_300, Spoiled = 1, ExpiredRequests = 0 };
            Game.Persist();
        }

        // ───────── 화면 전환 ─────────

        /// <summary>어둡게 덮고 다음 화면으로. 이름은 scenes/&lt;이름&gt;.tscn</summary>
        public void GoTo(string screen)
        {
            _fade.MouseFilter = Control.MouseFilterEnum.Stop;
            _fadeTween?.Kill();
            _fadeTween = CreateTween();
            _fadeTween.TweenProperty(_fade, "modulate:a", 1f, 0.26).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            _fadeTween.TweenCallback(Callable.From(() => GetTree().ChangeSceneToFile("res://scenes/" + screen + ".tscn")));
        }

        /// <summary>덮인 막을 걷는다 (Screen 이 아닌 점검 화면도 부른다)</summary>
        public void Reveal()
        {
            _fadeTween?.Kill();
            _fadeTween = CreateTween();
            _fadeTween.TweenProperty(_fade, "modulate:a", 0f, 0.42).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            _fadeTween.TweenCallback(Callable.From(() => _fade.MouseFilter = Control.MouseFilterEnum.Ignore));
        }

        /// <summary>이야기 화면이 보여 줄 이야기 (story.json parts: p1, p2, clear, fail)</summary>
        public string StoryPart { get; set; } = "p1";

        /// <summary>다음 낚시는 프롤로그 튜토리얼 (볼락 한 마리를 낚으면 이야기 p2 로)</summary>
        public bool Tutorial { get; set; }

        /// <summary>이야기 화면으로 (fade)</summary>
        public void GoStory(string part)
        {
            StoryPart = part;
            GoTo("story");
        }

        /// <summary>다음 화면에 들어가자마자 할 대화 (dialogue.json scenes 이름)</summary>
        public string PendingTalk { get; set; }

        /// <summary>다음 화면에서 띄울 토스트 (화면을 옮기면서 알릴 때)</summary>
        public string PendingToast { get; set; }

        /// <summary>새 화면이 다 만들어지면 부른다. 막을 걷고, 사진 모드면 찍는다</summary>
        public void ScreenReady(Screen screen)
        {
            Reveal();
            if (PendingToast != null)
            {
                screen.Toast(PendingToast);
                PendingToast = null;
            }

            if (ShotMode && !_shotTaken && string.Equals(screen.Name, ShotScreen, StringComparison.OrdinalIgnoreCase))
            {
                _shotTaken = true;
                _ = TakeShot(screen);
            }
        }

        /// <summary>연출이 끝날 때쯤 화면을 찍고 끈다</summary>
        private async Task TakeShot(Screen screen)
        {
            // 레이아웃이 자리 잡은 뒤에 사진용 동작(모달 열기 등)을 하고, 연출이 끝나면 찍는다
            await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            if (!string.IsNullOrEmpty(ShotAction)) screen.ShotSetup(ShotAction);
            await ToSignal(GetTree().CreateTimer(2.6), SceneTreeTimer.SignalName.Timeout);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var img = GetViewport().GetTexture().GetImage();
            var err = img.SavePng(ShotPath);
            GD.Print("사진 저장 " + ShotPath + " " + img.GetWidth() + "x" + img.GetHeight() + " " + err);
            GetTree().Quit(err == Error.Ok ? 0 : 1);
        }

        /// <summary>앱 버전 (project.godot config/version, CI 가 빌드 번호를 넣는다)</summary>
        public static string Version
        {
            get { return (string)ProjectSettings.GetSetting("application/config/version", "0"); }
        }
    }
}

using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App
{
    /// <summary>
    /// 효과음과 배경음 (소리는 scripts/sound.py 가 합성해 godot/audio 에 둔다).
    /// GameState 가 하나 만들어 붙인다. 화면은 Sound.Play("plop"), 배경음은 Screen.Music 으로 고른다.
    /// </summary>
    public partial class Sound : Node
    {
        public static Sound I { get; private set; }

        private const float MusicDb = -9f;
        private const int Voices = 10;

        private readonly Dictionary<string, AudioStream> _cache = new Dictionary<string, AudioStream>();
        private readonly List<AudioStreamPlayer> _voices = new List<AudioStreamPlayer>();
        private readonly Dictionary<string, AudioStreamPlayer> _loops = new Dictionary<string, AudioStreamPlayer>();
        private readonly RandomNumberGenerator _rand = new RandomNumberGenerator();
        private AudioStreamPlayer _musicA, _musicB;
        private string _musicName;
        private Tween _musicTween;
        private int _next;

        public override void _EnterTree()
        {
            I = this;
            ProcessMode = ProcessModeEnum.Always;
            for (int i = 0; i < Voices; i++)
            {
                var p = new AudioStreamPlayer();
                AddChild(p);
                _voices.Add(p);
            }
            _musicA = new AudioStreamPlayer { VolumeDb = -80 };
            _musicB = new AudioStreamPlayer { VolumeDb = -80 };
            AddChild(_musicA);
            AddChild(_musicB);
        }

        private AudioStream Load(string path)
        {
            AudioStream s;
            if (_cache.TryGetValue(path, out s)) return s;
            s = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
            _cache[path] = s;
            return s;
        }

        /// <summary>효과음 한 번. pitch 는 높낮이 배율, jitter 만큼 매번 조금씩 다르게</summary>
        public static void Play(string name, float volumeDb = 0, float pitch = 1, float jitter = 0.04f)
        {
            if (I == null || !Settings.Sfx) return;
            var s = I.Load("res://audio/sfx/" + name + ".ogg");
            if (s == null) return;
            var p = I._voices[I._next];
            I._next = (I._next + 1) % I._voices.Count;
            p.Stream = s;
            p.VolumeDb = volumeDb;
            p.PitchScale = pitch * (1 + I._rand.RandfRange(-jitter, jitter));
            p.Play();
        }

        /// <summary>계속 도는 효과음 (릴 감기 등). 이미 돌고 있으면 높낮이만 바꾼다</summary>
        public static void Loop(string name, bool on, float volumeDb = 0, float pitch = 1)
        {
            if (I == null) return;
            AudioStreamPlayer p;
            I._loops.TryGetValue(name, out p);
            if (!on || !Settings.Sfx)
            {
                if (p != null && p.Playing) p.Stop();
                return;
            }
            if (p == null)
            {
                var s = I.Load("res://audio/sfx/" + name + ".ogg");
                if (s == null) return;
                if (s is AudioStreamOggVorbis ogg) ogg.Loop = true;
                p = new AudioStreamPlayer { Stream = s };
                I.AddChild(p);
                I._loops[name] = p;
            }
            p.VolumeDb = volumeDb;
            p.PitchScale = pitch;
            if (!p.Playing) p.Play();
        }

        /// <summary>모든 반복 효과음을 멈춘다 (화면을 떠날 때)</summary>
        public static void StopLoops()
        {
            if (I == null) return;
            foreach (var p in I._loops.Values) p.Stop();
        }

        /// <summary>배경음을 바꾼다. 같은 곡이면 그대로 두고, 다르면 천천히 바꿔 튼다. null 이면 멈춘다</summary>
        public static void Music(string name)
        {
            if (I == null) return;
            I.SetMusic(name);
        }

        /// <summary>설정에서 배경음을 켜고 끌 때</summary>
        public static void RefreshMusic()
        {
            if (I == null) return;
            string name = I._musicName;
            I._musicName = null;
            I.SetMusic(name);
        }

        private void SetMusic(string name)
        {
            if (!Settings.Music) name = null;
            if (name == _musicName) return;
            _musicName = name;
            _musicTween?.Kill();
            _musicTween = CreateTween().SetParallel(true);
            // 지금 곡은 줄이고, 다른 플레이어로 새 곡을 올린다
            var old = _musicA.Playing ? _musicA : null;
            var fresh = old == _musicA ? _musicB : _musicA;
            if (old != null)
            {
                _musicTween.TweenProperty(old, "volume_db", -60f, 1.0);
                _musicTween.Chain().TweenCallback(Callable.From(old.Stop));
            }
            if (name == null) return;
            var s = Load("res://audio/bgm/" + name + ".ogg");
            if (s == null) return;
            if (s is AudioStreamOggVorbis ogg) ogg.Loop = true;
            fresh.Stream = s;
            fresh.VolumeDb = -40;
            fresh.Play();
            var up = CreateTween();
            up.TweenProperty(fresh, "volume_db", MusicDb, 1.4).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
    }

    /// <summary>
    /// 진동 (안드로이드). 짧은 톡, 투둑 두 번, 길게 한 번. 설정에서 끌 수 있다.
    /// amplitude 는 0~1 (기기가 세기를 못 바꾸면 무시된다)
    /// </summary>
    public static class Haptics
    {
        private static ulong _lastMs;
        /// <summary>전체 세기 (폰 진동은 생각보다 세다. 2026-10-22 사용자 의견으로 낮춤)</summary>
        private const float Master = 0.7f;
        /// <summary>설정의 진동 세기(약·보통·강)에 따른 세기와 길이 배율</summary>
        private static readonly float[] LevelAmp = { 0.5f, 1f, 1.4f };
        private static readonly float[] LevelLen = { 0.75f, 1f, 1.2f };

        private static float Amp(float a)
        {
            return Mathf.Clamp(a * Master * LevelAmp[Settings.VibeLevel], 0.04f, 1f);
        }

        private static int Len(int ms)
        {
            return Mathf.Max(5, (int)(ms * LevelLen[Settings.VibeLevel]));
        }

        public static void Pulse(int ms, float amplitude = 0.5f)
        {
            if (!Settings.Vibrate || !OS.HasFeature("mobile")) return;
            // 너무 자주 부르면 계속 떨려서 지저분하다
            ulong now = Time.GetTicksMsec();
            if (now - _lastMs < 40) return;
            _lastMs = now;
            Input.VibrateHandheld(Len(ms), Amp(amplitude));
        }

        /// <summary>(길이 ms, 세기, 다음까지 쉬는 ms) 를 차례로</summary>
        public static void Pattern(params (int Ms, float Amp, int Gap)[] steps)
        {
            if (!Settings.Vibrate || !OS.HasFeature("mobile") || Sound.I == null) return;
            double at = 0;
            foreach (var s in steps)
            {
                var step = s;
                float amp = Amp(step.Amp);
                int len = Len(step.Ms);
                if (at <= 0) Input.VibrateHandheld(len, amp);
                else Sound.I.GetTree().CreateTimer(at).Timeout += () => Input.VibrateHandheld(len, amp);
                at += (step.Ms + step.Gap) / 1000.0;
            }
            _lastMs = Time.GetTicksMsec();
        }

        /// <summary>버튼 누름: 아주 짧게</summary>
        public static void Tap()
        {
            Pulse(8, 0.15f);
        }
    }
}

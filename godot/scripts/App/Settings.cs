using Godot;

namespace Hanjeopsi.App
{
    /// <summary>
    /// 기기 설정 (효과음, 배경음, 진동). 세이브와 따로 user://settings.cfg 에 둔다.
    /// 새 게임을 시작해도 그대로 남는다
    /// </summary>
    public static class Settings
    {
        private const string Path = "user://settings.cfg";
        private static bool _loaded;
        private static bool _sfx = true, _music = true, _vibrate = true;
        private static int _vibeLevel = 1;

        public static bool Sfx
        {
            get { Load(); return _sfx; }
            set { _sfx = value; Save(); }
        }

        public static bool Music
        {
            get { Load(); return _music; }
            set { _music = value; Save(); }
        }

        public static bool Vibrate
        {
            get { Load(); return _vibrate; }
            set { _vibrate = value; Save(); }
        }

        /// <summary>진동 세기 0 약, 1 보통, 2 강</summary>
        public static int VibeLevel
        {
            get { Load(); return _vibeLevel; }
            set { _vibeLevel = Mathf.Clamp(value, 0, 2); Save(); }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var cfg = new ConfigFile();
            if (cfg.Load(Path) != Error.Ok) return;
            _sfx = (bool)cfg.GetValue("sound", "sfx", true);
            _music = (bool)cfg.GetValue("sound", "music", true);
            _vibrate = (bool)cfg.GetValue("sound", "vibrate", true);
            _vibeLevel = (int)cfg.GetValue("sound", "vibeLevel", 1);
        }

        private static void Save()
        {
            if (GameState.I != null && GameState.I.ShotMode) return;
            var cfg = new ConfigFile();
            cfg.SetValue("sound", "sfx", _sfx);
            cfg.SetValue("sound", "music", _music);
            cfg.SetValue("sound", "vibrate", _vibrate);
            cfg.SetValue("sound", "vibeLevel", _vibeLevel);
            cfg.Save(Path);
        }
    }
}

using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>색 모음 (웹 config.ts 의 C, T). 화면 코드는 여기 색만 쓴다</summary>
    public static class Palette
    {
        public static readonly Color Night = new Color("1b2a41");
        public static readonly Color NightDeep = new Color("0f1a2b");
        public static readonly Color Panel = new Color("1f2b3d");
        public static readonly Color Sea = new Color("2e6f95");
        public static readonly Color SeaLight = new Color("4ea3c7");
        public static readonly Color Foam = new Color("d8f1f7");
        public static readonly Color Paper = new Color("f4ecd8");
        public static readonly Color PaperLight = new Color("fbf6ea");
        public static readonly Color PaperDark = new Color("d9cba8");
        public static readonly Color Ink = new Color("2b2b2b");
        public static readonly Color InkSoft = new Color("5b5148");
        public static readonly Color Wood = new Color("8a5a3b");
        public static readonly Color WoodDark = new Color("5e3b26");
        public static readonly Color Red = new Color("d9534f");
        public static readonly Color RedText = new Color("e0605b");
        public static readonly Color RedDark = new Color("9e3330");
        public static readonly Color Gold = new Color("f2c14e");
        public static readonly Color GoldDark = new Color("a8761a");
        public static readonly Color Green = new Color("6abf69");
        public static readonly Color GreenText = new Color("7fd17e");
        public static readonly Color GreenDark = new Color("3f8a4a");
        public static readonly Color Blue = new Color("4a7fc1");
        public static readonly Color BlueText = new Color("8cc8ff");
        public static readonly Color BlueDark = new Color("2f5689");
        public static readonly Color Teal = new Color("3f7a8a");
        public static readonly Color Purple = new Color("6a5a8a");
        public static readonly Color Gray = new Color("8d939b");
        public static readonly Color GrayText = new Color("a7adb5");
        public static readonly Color GrayDark = new Color("5c6269");
    }

    /// <summary>
    /// 글꼴: 본문 나눔스퀘어라운드 굵게, 제목 주아 (둘 다 OFL, 2026-10-29).
    /// 주아에 없는 기호(· × 화살표 ...)는 본문 글꼴이 대신 그린다
    /// </summary>
    public static class Fonts
    {
        private static Font _body;
        private static Font _title;

        public static Font Body
        {
            get { return _body ??= GD.Load<Font>("res://fonts/NanumSquareRoundB.ttf"); }
        }

        public static Font Title
        {
            get
            {
                if (_title == null)
                {
                    var f = GD.Load<FontFile>("res://fonts/Jua-Regular.ttf");
                    f.Fallbacks = new Godot.Collections.Array<Font> { Body };
                    _title = f;
                }
                return _title;
            }
        }
    }

    /// <summary>글자 크기 단계 (1080 폭 기준 픽셀). 화면마다 제각각 쓰지 않도록 이 값만 쓴다</summary>
    public static class TextSize
    {
        public const int Hero = 128;
        public const int Title = 60;
        public const int H1 = 48;
        public const int Body = 40;
        public const int Small = 34;
        public const int Tiny = 28;
    }
}

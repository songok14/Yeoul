using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>글자, 상자 모양, 테마. 웹 ui.ts 의 txt()와 drawPanel 자리</summary>
    public static class UiKit
    {
        private static Theme _theme;

        /// <summary>모든 화면 뿌리 Control 에 건다. 기본 글꼴, 글자 색, 스크롤 막대 숨김</summary>
        public static Theme Theme
        {
            get
            {
                if (_theme != null) return _theme;
                var t = new Theme { DefaultFont = Fonts.Body, DefaultFontSize = TextSize.Body };
                t.SetColor("font_color", "Label", Palette.Paper);
                t.SetConstant("line_spacing", "Label", 6);
                t.SetColor("default_color", "RichTextLabel", Palette.Paper);
                t.SetStylebox("focus", "Button", new StyleBoxEmpty());
                _theme = t;
                return t;
            }
        }

        /// <summary>
        /// 글자 하나. 그림 위에 올릴 때는 outline 을 주면 어떤 배경에서도 읽힌다.
        /// wrap 이 false 면 줄을 바꾸지 않고 글자 폭만큼 차지한다. 칸이 정해진 곳은 Ellipsis 로 말줄임표를 켠다.
        /// </summary>
        public static Label Text(string text, int size, Color color, bool title = false, bool wrap = false, int outline = 0, Color? outlineColor = null, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            var settings = new LabelSettings
            {
                Font = title ? Fonts.Title : Fonts.Body,
                FontSize = size,
                FontColor = color,
                LineSpacing = Mathf.Round(size * 0.18f),
            };
            if (outline > 0)
            {
                settings.OutlineSize = outline;
                settings.OutlineColor = outlineColor ?? Palette.NightDeep;
            }
            var l = new Label { Text = text, LabelSettings = settings, HorizontalAlignment = align, MouseFilter = Control.MouseFilterEnum.Ignore };
            if (wrap) l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            else l.AutowrapMode = TextServer.AutowrapMode.Off;
            return l;
        }

        /// <summary>칸보다 길면 말줄임표 (칸 폭은 부모가 정한다)</summary>
        public static Label Ellipsis(this Label l)
        {
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            l.ClipText = true;
            return l;
        }

        /// <summary>글자 한 줄의 폭 (줄바꿈이 있으면 가장 긴 줄)</summary>
        public static float Measure(string text, int size, bool title = false)
        {
            float w = 0;
            var font = title ? Fonts.Title : Fonts.Body;
            foreach (var line in text.Split('\n')) w = Mathf.Max(w, font.GetStringSize(line, HorizontalAlignment.Left, -1, size).X);
            return w;
        }

        /// <summary>그림자 진 글자 (제목처럼 크게 그림 위에 얹을 때)</summary>
        public static Label Shadowed(string text, int size, Color color, bool title, int shadow, HorizontalAlignment align = HorizontalAlignment.Center)
        {
            var l = Text(text, size, color, title, false, 0, null, align);
            l.LabelSettings.ShadowSize = shadow;
            l.LabelSettings.ShadowColor = new Color(Palette.NightDeep, 0.55f);
            l.LabelSettings.ShadowOffset = new Vector2(0, shadow * 0.5f);
            return l;
        }

        /// <summary>둥근 상자. border 는 아래쪽만 두껍게 주면 눌리는 버튼처럼 보인다</summary>
        public static StyleBoxFlat Box(Color bg, int radius, int padX = 0, int padY = 0)
        {
            var sb = new StyleBoxFlat { BgColor = bg, AntiAliasing = true };
            sb.SetCornerRadiusAll(radius);
            sb.ContentMarginLeft = padX;
            sb.ContentMarginRight = padX;
            sb.ContentMarginTop = padY;
            sb.ContentMarginBottom = padY;
            return sb;
        }

        public static StyleBoxFlat WithShadow(this StyleBoxFlat sb, float alpha, int size, int offsetY)
        {
            sb.ShadowColor = new Color(0, 0, 0, alpha);
            sb.ShadowSize = size;
            sb.ShadowOffset = new Vector2(0, offsetY);
            return sb;
        }

        public static StyleBoxFlat WithBorder(this StyleBoxFlat sb, Color color, int width, int bottom = -1)
        {
            sb.BorderColor = color;
            sb.SetBorderWidthAll(width);
            if (bottom >= 0) sb.BorderWidthBottom = bottom;
            return sb;
        }

        /// <summary>위아래로 색이 바뀌는 그림 (화면 위·아래를 어둡게 덮을 때)</summary>
        public static TextureRect VerticalShade(Color top, Color bottom)
        {
            var g = new Gradient();
            g.SetColor(0, top);
            g.SetColor(1, bottom);
            var tex = new GradientTexture2D { Gradient = g, Width = 4, Height = 256, FillFrom = new Vector2(0, 0), FillTo = new Vector2(0, 1) };
            return new TextureRect { Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = Control.MouseFilterEnum.Ignore };
        }

        /// <summary>가운데가 밝고 가장자리가 투명한 둥근 점 (반짝임, 빛 알갱이)</summary>
        public static Texture2D SoftDot(int size = 64)
        {
            var g = new Gradient();
            g.SetColor(0, new Color(1, 1, 1, 1));
            g.SetColor(1, new Color(1, 1, 1, 0));
            return new GradientTexture2D { Gradient = g, Width = size, Height = size, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f) };
        }

        /// <summary>받침에 맞춘 은/는 ("위판장은", "미끼가게는")</summary>
        public static string Topic(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            char c = word[word.Length - 1];
            bool batchim = c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0;
            return word + (batchim ? "은" : "는");
        }

        /// <summary>천 단위 쉼표 원 (웹 won)</summary>
        public static string Won(long n)
        {
            return Core.Util.JsMath.Won(n);
        }
    }
}

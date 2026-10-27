using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 낚시 화면 아래 게이지. 던질 때는 파워 게이지(근거리·중거리·원거리 구간 위를 바늘이 오간다.
    /// 낚싯대가 닿지 않는 먼 쪽은 어둡게 막고 최대 거리를 적는다),
    /// 릴링 때는 텐션 게이지(파랑 느슨함, 초록 알맞음, 빨강 위험, 금색 선은 드랙. 구간은 릴마다 다르다).
    /// </summary>
    public partial class ReelGauge : Control
    {
        public enum Mode
        {
            None,
            Power,
            Tension,
        }

        private Mode _mode = Mode.None;

        // 파워: 구간 경계(0~1)와 이름, 바늘 위치
        public float Bound1, Bound2;
        public string[] ZoneNames = new string[3];
        public float Needle;
        /// <summary>낚싯대가 닿는 끝 (0~1). 그 너머는 어둡게</summary>
        public float Reach = 1;
        public string ReachLabel;

        // 텐션: 0~100 기준 값
        public float Slack, Danger, Drag, Tension;

        public ReelGauge()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            CustomMinimumSize = new Vector2(0, 92);
        }

        public Mode Showing
        {
            get { return _mode; }
            set
            {
                if (_mode == value) return;
                _mode = value;
                // 바뀔 때 살짝 튀어 오른다
                PivotOffset = Size / 2;
                Scale = value == Mode.None ? Vector2.One : new Vector2(0.92f, 0.92f);
                Modulate = new Color(1, 1, 1, value == Mode.None ? 0 : 0.4f);
                var tw = CreateTween().SetParallel(true);
                tw.TweenProperty(this, "scale", Vector2.One, 0.28).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                tw.TweenProperty(this, "modulate:a", value == Mode.None ? 0f : 1f, 0.18);
            }
        }

        public override void _Process(double delta)
        {
            if (_mode != Mode.None) QueueRedraw();
        }

        public override void _Draw()
        {
            if (_mode == Mode.None) return;
            float h = 44;
            var bar = new Rect2(0, (Size.Y - h) / 2, Size.X, h);
            DrawStyleBox(UiKit.Box(Palette.Ink, 22).WithShadow(0.35f, 8, 3), bar.Grow(6));
            if (_mode == Mode.Power) DrawPower(bar);
            else DrawTension(bar);
        }

        private void Segment(Rect2 bar, float from, float to, Color color)
        {
            if (to <= from) return;
            var r = new Rect2(bar.Position.X + bar.Size.X * from, bar.Position.Y, bar.Size.X * (to - from), bar.Size.Y);
            var sb = UiKit.Box(color, 0);
            if (from <= 0) { sb.CornerRadiusTopLeft = 20; sb.CornerRadiusBottomLeft = 20; }
            if (to >= 1) { sb.CornerRadiusTopRight = 20; sb.CornerRadiusBottomRight = 20; }
            DrawStyleBox(sb, r);
        }

        private void Label(string text, float center, Rect2 bar)
        {
            var font = Fonts.Body;
            int size = TextSize.Tiny;
            float w = 300;
            var pos = new Vector2(bar.Position.X + bar.Size.X * center - w / 2, bar.Position.Y + bar.Size.Y / 2 + size * 0.36f);
            DrawStringOutline(font, pos, text, HorizontalAlignment.Center, w, size, 8, new Color(Palette.NightDeep, 0.85f));
            DrawString(font, pos, text, HorizontalAlignment.Center, w, size, Colors.White);
        }

        private void Needle01(Rect2 bar, float t, Color color)
        {
            float x = bar.Position.X + bar.Size.X * Mathf.Clamp(t, 0, 1);
            DrawStyleBox(UiKit.Box(color, 5).WithBorder(Palette.Ink, 3).WithShadow(0.3f, 4, 2), new Rect2(x - 7, bar.Position.Y - 16, 14, bar.Size.Y + 32));
        }

        private void DrawPower(Rect2 bar)
        {
            float reach = Mathf.Clamp(Reach, 0.05f, 1);
            Segment(bar, 0, Mathf.Min(Bound1, reach), Palette.SeaLight);
            Segment(bar, Bound1, Mathf.Min(Bound2, reach), Palette.Sea);
            Segment(bar, Bound2, reach, new Color("1b4466"));
            // 닿지 않는 먼 물: 어둡게 막고 빗금
            if (reach < 0.995f)
            {
                Segment(bar, reach, 1, new Color(0.12f, 0.13f, 0.16f));
                float x0 = bar.Position.X + bar.Size.X * reach, x1 = bar.End.X - 14;
                for (float x = x0 + 8; x < x1; x += 14)
                    DrawLine(new Vector2(x, bar.End.Y - 6), new Vector2(x + 10, bar.Position.Y + 6), new Color(1, 1, 1, 0.12f), 3, true);
                DrawRect(new Rect2(x0 - 2, bar.Position.Y - 4, 4, bar.Size.Y + 8), Palette.Gold);
            }
            Label(ZoneNames[0], Mathf.Min(Bound1, reach) / 2, bar);
            Label(ZoneNames[1], (Bound1 + Mathf.Min(Bound2, reach)) / 2, bar);
            if (Bound2 < reach - 0.08f) Label(ZoneNames[2], (Bound2 + reach) / 2, bar);
            if (!string.IsNullOrEmpty(ReachLabel))
            {
                // 이 낚싯대의 최대 거리. 막힌 칸이 넉넉하면 그 안에, 아니면 막대 위 오른쪽 (위 안내 글과 겹치지 않게)
                var font = Fonts.Body;
                int size = TextSize.Tiny;
                float w = 300;
                float cx = bar.Position.X + bar.Size.X * reach;
                float blocked = bar.End.X - cx;
                float textW = font.GetStringSize(ReachLabel, HorizontalAlignment.Left, -1, size).X;
                Vector2 pos;
                if (reach < 0.995f && blocked >= textW + 24)
                    pos = new Vector2(cx + blocked / 2 - w / 2, bar.Position.Y + bar.Size.Y / 2 + size * 0.36f);
                else
                    pos = new Vector2(Mathf.Min(cx - w / 2, bar.End.X - w), bar.Position.Y - 22);
                DrawStringOutline(font, pos, ReachLabel, reach < 0.995f ? HorizontalAlignment.Center : HorizontalAlignment.Right, w, size, 8, new Color(Palette.NightDeep, 0.85f));
                DrawString(font, pos, ReachLabel, reach < 0.995f ? HorizontalAlignment.Center : HorizontalAlignment.Right, w, size, Palette.Gold);
            }
            Needle01(bar, Needle, Palette.Gold);
        }

        private void DrawTension(Rect2 bar)
        {
            Segment(bar, 0, Slack / 100, Palette.BlueDark);
            Segment(bar, Slack / 100, Danger / 100, Palette.GreenDark);
            Segment(bar, Danger / 100, 1, Palette.RedDark);
            // 드랙: 이 선을 넘으면 감아도 텐션이 덜 오른다
            if (Drag <= 100)
            {
                float x = bar.Position.X + bar.Size.X * Drag / 100;
                DrawRect(new Rect2(x - 2, bar.Position.Y, 4, bar.Size.Y), new Color(Palette.Gold, 0.85f));
            }
            float t = Mathf.Clamp(Tension / 100, 0, 1);
            var col = Tension >= Danger ? Palette.Red : Tension < Slack ? Palette.BlueText : Palette.Green;
            if (t > 0.01f)
            {
                var fill = new Rect2(bar.Position.X, bar.Position.Y + 11, bar.Size.X * t, bar.Size.Y - 22);
                DrawStyleBox(UiKit.Box(col, 11), fill);
            }
            Needle01(bar, t, Colors.White);
        }
    }
}

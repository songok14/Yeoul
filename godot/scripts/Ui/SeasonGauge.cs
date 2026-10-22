using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 간 맞추기 게이지 (웹 ui/gauge.ts). 금색 바늘이 좌우로 오가고, 누르면 멈춘다.
    /// 미각이 돌아온 맛이면 초록 정답 구간이 보이고, 아니면 물음표만 흐릿하게 보인다.
    /// 판정은 코어 Truck.Judge 가 하고, 여기서는 그리기와 바늘 움직임만 한다.
    /// </summary>
    public partial class SeasonGauge : Control
    {
        private const float TrackH = 64;

        public double Target;
        public double ZoneHalf;
        public bool ZoneVisible;
        public double Speed = 0.9;
        public string LessText = "", MoreText = "";

        public double Pos { get; private set; }
        public bool Running { get; private set; } = true;

        private int _dir = 1;
        private float _flash;
        private Color _flashColor;
        private float _time;

        public SeasonGauge()
        {
            CustomMinimumSize = new Vector2(0, TrackH + 70);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        /// <summary>바늘을 멈추고 그 자리를 돌려준다. 멈춘 자리에서 판정 색으로 반짝인다</summary>
        public double Stop(Color flash)
        {
            Running = false;
            _flash = 1;
            _flashColor = flash;
            return Pos;
        }

        /// <summary>사진 모드 등에서 바늘을 정한 자리에 둔다</summary>
        public void Place(double pos)
        {
            Pos = Mathf.Clamp(pos, 0, 1);
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _time += dt;
            if (Running)
            {
                Pos += _dir * Speed * dt;
                if (Pos >= 1)
                {
                    Pos = 1;
                    _dir = -1;
                }
                else if (Pos <= 0)
                {
                    Pos = 0;
                    _dir = 1;
                }
            }
            if (_flash > 0) _flash = Mathf.Max(0, _flash - dt * 1.4f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float w = Size.X;
            float top = 22;
            var track = new Rect2(0, top, w, TrackH);

            // 바탕: 둥근 홈과 눈금
            DrawStyleBox(UiKit.Box(Palette.Ink, 22), track.Grow(6));
            DrawStyleBox(UiKit.Box(new Color("3a3f47"), 18), track);
            for (int i = 1; i < 10; i++) DrawRect(new Rect2(w * i / 10f - 2, top, 4, 14), new Color("5c6269"));

            var font = Fonts.Body;
            if (ZoneVisible)
            {
                float zx = (float)(Target - ZoneHalf) * w;
                float zw = (float)(ZoneHalf * 2) * w;
                var zone = new Rect2(zx, top, zw, TrackH);
                // 정답 구간은 숨 쉬듯 살짝 밝아졌다 어두워진다
                float glow = 0.85f + Mathf.Sin(_time * 3) * 0.1f;
                DrawStyleBox(UiKit.Box(new Color(Palette.Green, glow), 12), zone);
                DrawRect(new Rect2(zx + 6, top + 4, zw - 12, 8), new Color(1, 1, 1, 0.35f));
            }
            else
            {
                string q = "?   ?   ?   ?   ?";
                var sz = font.GetStringSize(q, HorizontalAlignment.Left, -1, TextSize.Body);
                DrawString(font, new Vector2((w - sz.X) / 2, top + TrackH / 2 + sz.Y * 0.32f), q, HorizontalAlignment.Left, -1, TextSize.Body, new Color(Palette.GrayText, 0.7f));
            }

            // 바늘: 위에 작은 삼각형, 금색 막대
            float x = (float)Pos * w;
            if (_flash > 0) DrawCircle(new Vector2(x, top + TrackH / 2), 30 + (1 - _flash) * 40, new Color(_flashColor, _flash * 0.55f));
            DrawRect(new Rect2(x - 9, top - 14, 18, TrackH + 28), Palette.Ink);
            DrawRect(new Rect2(x - 5, top - 10, 10, TrackH + 20), Palette.Gold);
            DrawColoredPolygon(new[] { new Vector2(x - 20, top - 30), new Vector2(x + 20, top - 30), new Vector2(x, top - 8) }, Palette.Gold);

            // 아래 양끝 글
            float by = top + TrackH + 46;
            DrawString(font, new Vector2(0, by), LessText, HorizontalAlignment.Left, -1, TextSize.Tiny, Palette.GrayText);
            var ms = font.GetStringSize(MoreText, HorizontalAlignment.Left, -1, TextSize.Tiny);
            DrawString(font, new Vector2(w - ms.X, by), MoreText, HorizontalAlignment.Left, -1, TextSize.Tiny, Palette.GrayText);
        }
    }
}

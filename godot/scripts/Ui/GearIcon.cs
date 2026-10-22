using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 미끼가게 줄 앞에 붙는 작은 그림 (장비 그림이 오기 전까지 코드로 그린다).
    /// 둥근 바탕 위에 미끼, 낚싯대, 릴, 쿨러, 찌, 대나무대를 단순한 선과 면으로.
    /// </summary>
    public partial class GearIcon : Control
    {
        public enum Kind
        {
            Bait,
            Rod,
            Reel,
            Tackle,
            Rig,
            Bamboo,
        }

        private readonly Kind _kind;
        private readonly Color _bg;

        public GearIcon() { }

        public GearIcon(Kind kind, float size = 132)
        {
            _kind = kind;
            CustomMinimumSize = new Vector2(size, size);
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            MouseFilter = MouseFilterEnum.Ignore;
            switch (kind)
            {
                case Kind.Bait: _bg = new Color("f6d6c8"); break;
                case Kind.Rod: _bg = new Color("d6e6f2"); break;
                case Kind.Reel: _bg = new Color("dde3ea"); break;
                case Kind.Tackle: _bg = new Color("d4eef0"); break;
                case Kind.Rig: _bg = new Color("f4e2c4"); break;
                default: _bg = new Color("e8dcc4"); break;
            }
        }

        public override void _Draw()
        {
            float s = Size.X;
            var c = new Vector2(s / 2, s / 2);
            DrawCircle(c, s * 0.5f, _bg.Darkened(0.12f));
            DrawCircle(c - new Vector2(0, 3), s * 0.47f, _bg);
            var ink = Palette.Ink;
            float w = s * 0.045f;
            switch (_kind)
            {
                case Kind.Bait:
                    // 크릴 봉지: 둥근 봉지와 분홍 새우 두 마리
                    DrawRoundBox(new Rect2(s * 0.24f, s * 0.24f, s * 0.52f, s * 0.56f), new Color("4a7fc1"), ink, w);
                    DrawRect(new Rect2(s * 0.24f, s * 0.24f, s * 0.52f, s * 0.1f), new Color("2f5689"));
                    DrawArc(new Vector2(s * 0.44f, s * 0.56f), s * 0.11f, Mathf.Pi * 0.1f, Mathf.Pi * 1.3f, 16, new Color("ff8f7a"), s * 0.07f, true);
                    DrawArc(new Vector2(s * 0.58f, s * 0.62f), s * 0.09f, Mathf.Pi * 0.1f, Mathf.Pi * 1.3f, 16, new Color("ffb39f"), s * 0.06f, true);
                    break;
                case Kind.Rod:
                    // 비스듬한 낚싯대와 손잡이, 끝에서 늘어진 줄
                    DrawLine(new Vector2(s * 0.24f, s * 0.8f), new Vector2(s * 0.8f, s * 0.2f), new Color("5e3b26"), s * 0.05f, true);
                    DrawLine(new Vector2(s * 0.24f, s * 0.8f), new Vector2(s * 0.4f, s * 0.63f), ink, s * 0.09f, true);
                    DrawArc(new Vector2(s * 0.8f, s * 0.42f), s * 0.22f, -Mathf.Pi / 2, 0, 12, new Color(ink, 0.6f), s * 0.015f, true);
                    DrawCircle(new Vector2(s * 0.37f, s * 0.7f), s * 0.07f, new Color("a7adb5"));
                    break;
                case Kind.Reel:
                    // 스풀과 손잡이
                    DrawCircle(c, s * 0.24f, ink);
                    DrawCircle(c, s * 0.19f, new Color("a7adb5"));
                    DrawCircle(c, s * 0.07f, ink);
                    DrawLine(c, c + new Vector2(s * 0.24f, -s * 0.16f), ink, w, true);
                    DrawCircle(c + new Vector2(s * 0.26f, -s * 0.17f), s * 0.06f, new Color("d9534f"));
                    DrawLine(c + new Vector2(-s * 0.06f, s * 0.24f), c + new Vector2(-s * 0.06f, s * 0.34f), ink, w * 1.4f);
                    break;
                case Kind.Tackle:
                    // 쿨러: 흰 상자와 파란 뚜껑
                    DrawRoundBox(new Rect2(s * 0.2f, s * 0.36f, s * 0.6f, s * 0.38f), new Color("fbf6ea"), ink, w);
                    DrawRoundBox(new Rect2(s * 0.18f, s * 0.28f, s * 0.64f, s * 0.12f), new Color("4ea3c7"), ink, w);
                    DrawLine(new Vector2(s * 0.4f, s * 0.24f), new Vector2(s * 0.6f, s * 0.24f), ink, w * 1.2f, true);
                    DrawRect(new Rect2(s * 0.28f, s * 0.5f, s * 0.44f, s * 0.05f), new Color("4ea3c7"));
                    break;
                case Kind.Rig:
                    // 찌: 빨간 머리, 흰 몸통, 아래로 줄
                    DrawLine(new Vector2(s * 0.5f, s * 0.14f), new Vector2(s * 0.5f, s * 0.86f), new Color(ink, 0.55f), s * 0.015f, true);
                    DrawCircle(new Vector2(s * 0.5f, s * 0.42f), s * 0.15f, ink);
                    DrawCircle(new Vector2(s * 0.5f, s * 0.42f), s * 0.125f, new Color("fbf6ea"));
                    DrawArc(new Vector2(s * 0.5f, s * 0.42f), s * 0.065f, Mathf.Pi, Mathf.Tau, 12, new Color("d9534f"), s * 0.125f);
                    DrawLine(new Vector2(s * 0.5f, s * 0.27f), new Vector2(s * 0.5f, s * 0.16f), new Color("d9534f"), s * 0.04f, true);
                    DrawCircle(new Vector2(s * 0.5f, s * 0.78f), s * 0.035f, new Color("5c6269"));
                    break;
                default:
                    // 대나무대: 마디 있는 대가 가운데서 꺾였다
                    var bamboo = new Color("b88a4a");
                    DrawLine(new Vector2(s * 0.2f, s * 0.78f), new Vector2(s * 0.5f, s * 0.5f), bamboo, s * 0.07f, true);
                    DrawLine(new Vector2(s * 0.54f, s * 0.5f), new Vector2(s * 0.82f, s * 0.36f), bamboo, s * 0.055f, true);
                    foreach (var t in new[] { 0.3f, 0.6f })
                    {
                        var p = new Vector2(s * 0.2f, s * 0.78f).Lerp(new Vector2(s * 0.5f, s * 0.5f), t);
                        DrawCircle(p, s * 0.045f, new Color("8a5a3b"));
                    }
                    DrawLine(new Vector2(s * 0.49f, s * 0.44f), new Vector2(s * 0.55f, s * 0.56f), new Color("d9534f"), s * 0.02f, true);
                    break;
            }
        }

        private void DrawRoundBox(Rect2 r, Color fill, Color line, float width)
        {
            var sb = new StyleBoxFlat { BgColor = fill, AntiAliasing = true, BorderColor = line };
            sb.SetCornerRadiusAll((int)(Size.X * 0.06f));
            sb.SetBorderWidthAll((int)Mathf.Max(2, width));
            DrawStyleBox(sb, r);
        }
    }
}

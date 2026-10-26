using System;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 마을 하늘을 천천히 가로지르는 갈매기 몇 마리 (날갯짓하는 V 모양). 마을이 멈춰 보이지 않게.
    /// 좌표는 ArtStage 논리 좌표(450×800). 밤에는 쓰지 않는다
    /// </summary>
    public partial class Gulls : Control
    {
        private sealed class Gull
        {
            public float X, Y, Speed, Size, Phase, Bob;
        }

        private readonly ArtStage _stage;
        private readonly Gull[] _gulls;
        private readonly Random _rand = new Random();
        private readonly Color _color;
        private float _time;

        public Gulls() { }

        public Gulls(ArtStage stage, int count, Color color)
        {
            _stage = stage;
            _color = color;
            MouseFilter = MouseFilterEnum.Ignore;
            _gulls = new Gull[count];
            for (int i = 0; i < count; i++)
            {
                _gulls[i] = NewGull();
                _gulls[i].X = (float)_rand.NextDouble() * ArtStage.Logical.X;
            }
        }

        private Gull NewGull()
        {
            bool right = _rand.NextDouble() < 0.6;
            return new Gull
            {
                X = right ? -30 : ArtStage.Logical.X + 30,
                Y = 70 + (float)_rand.NextDouble() * 120,
                Speed = (right ? 1 : -1) * (14 + (float)_rand.NextDouble() * 14),
                Size = 5 + (float)_rand.NextDouble() * 4,
                Phase = (float)_rand.NextDouble() * 6,
                Bob = (float)_rand.NextDouble() * 6,
            };
        }

        public override void _Process(double delta)
        {
            if (_stage == null) return;
            float dt = (float)delta;
            _time += dt;
            for (int i = 0; i < _gulls.Length; i++)
            {
                var g = _gulls[i];
                g.X += g.Speed * dt;
                if (g.X < -40 || g.X > ArtStage.Logical.X + 40) _gulls[i] = NewGull();
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_stage == null) return;
            float k = _stage.K;
            foreach (var g in _gulls)
            {
                // 날개를 퍼덕이다가 가끔 활공한다
                float flap = Mathf.Sin(_time * 7 + g.Phase);
                if (Mathf.Sin(_time * 0.8f + g.Phase) > 0.4f) flap = 0.3f;
                var c = _stage.ToLocal(new Vector2(g.X, g.Y + Mathf.Sin(_time * 1.3f + g.Bob) * 3));
                float s = g.Size * k * 0.5f;
                float lift = flap * s * 0.7f;
                var pts = new[]
                {
                    c + new Vector2(-s * 1.6f, -lift),
                    c + new Vector2(-s * 0.7f, -s * 0.35f - lift * 0.4f),
                    c,
                    c + new Vector2(s * 0.7f, -s * 0.35f - lift * 0.4f),
                    c + new Vector2(s * 1.6f, -lift),
                };
                DrawPolyline(pts, _color, Mathf.Max(2, s * 0.32f), true);
            }
        }
    }
}

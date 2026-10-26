using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>배경 위에 천천히 흩날리는 눈송이. 누르기는 그냥 지나간다</summary>
    public partial class SnowOverlay : Control
    {
        private struct Flake
        {
            public Vector2 Position;
            public float Speed;
            public float Size;
            public float Phase;
        }

        private readonly List<Flake> _flakes = new List<Flake>();
        private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
        private float _clock;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _rng.Randomize();
        }

        public override void _Process(double delta)
        {
            if (Size.X <= 0 || Size.Y <= 0) return;
            float dt = Mathf.Min((float)delta, 0.05f);
            _clock += dt;
            if (_flakes.Count == 0)
                for (int i = 0; i < 110; i++) _flakes.Add(NewFlake(_rng.RandfRange(0, Size.Y)));
            for (int i = 0; i < _flakes.Count; i++)
            {
                var f = _flakes[i];
                f.Position += new Vector2(Mathf.Sin(_clock * 0.9f + f.Phase) * 18 * dt - 8 * dt, f.Speed * dt);
                if (f.Position.Y > Size.Y + 10) f = NewFlake(-10);
                _flakes[i] = f;
            }
            QueueRedraw();
        }

        private Flake NewFlake(float y)
        {
            float size = _rng.RandfRange(1.6f, 4.6f);
            return new Flake
            {
                Position = new Vector2(_rng.RandfRange(0, Size.X), y),
                // 큰 눈송이는 가까이 있어 빨리 떨어진다
                Speed = 40 + size * 22,
                Size = size,
                Phase = _rng.RandfRange(0, Mathf.Tau),
            };
        }

        public override void _Draw()
        {
            foreach (var f in _flakes)
            {
                float a = 0.45f + f.Size / 4.6f * 0.45f;
                DrawCircle(f.Position, f.Size, new Color(1, 1, 1, a));
            }
        }
    }

    /// <summary>수평선 쪽에 낮게 깔려 천천히 흘러가는 해무. 누르기는 그냥 지나간다</summary>
    public partial class FogOverlay : Control
    {
        /// <summary>안개가 가장 짙은 높이 (화면 높이 비율)</summary>
        public float Center = 0.38f;
        /// <summary>짙기 0~1</summary>
        public float Strength = 0.5f;
        /// <summary>밤 안개는 푸르스름하다</summary>
        public Color Tone = new Color(0.94f, 0.95f, 0.97f);
        /// <summary>화면 전체에 까는 뿌연 막 (위가 짙고 아래로 옅다). 0 이면 없다</summary>
        public float Veil;

        private float _clock;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta)
        {
            _clock += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (Size.X <= 0 || Size.Y <= 0) return;
            if (Veil > 0)
            {
                var top = new Color(Tone, Veil);
                var mid = new Color(Tone, Veil * 0.75f);
                var low = new Color(Tone, Veil * 0.2f);
                float y1 = Size.Y * (Center + 0.12f);
                DrawPolygon(new[] { Vector2.Zero, new Vector2(Size.X, 0), new Vector2(Size.X, y1), new Vector2(0, y1) }, new[] { top, top, mid, mid });
                DrawPolygon(new[] { new Vector2(0, y1), new Vector2(Size.X, y1), new Vector2(Size.X, Size.Y), new Vector2(0, Size.Y) }, new[] { mid, mid, low, low });
            }
            // 위아래로 옅어지는 띠 몇 겹이 서로 다른 빠르기로 흐른다
            for (int b = 0; b < 4; b++)
            {
                float h = Size.Y * (0.16f + b * 0.05f);
                float cy = Size.Y * (Center + (b - 1.5f) * 0.07f) + Mathf.Sin(_clock * (0.15f + b * 0.04f) + b) * 10;
                float a = Strength * (0.32f - b * 0.05f);
                Band(cy, h, a, _clock * (12 + b * 7) + b * 140);
            }
        }

        private void Band(float cy, float h, float a, float drift)
        {
            const int n = 24;
            var clear = new Color(Tone, 0);
            var full = new Color(Tone, a);
            for (int i = 0; i < n; i++)
            {
                float x0 = Size.X * i / n, x1 = Size.X * (i + 1) / n;
                // 가로로도 짙고 옅은 덩어리가 지나간다
                float k0 = 0.65f + 0.35f * Mathf.Sin((x0 + drift) * 0.012f);
                float k1 = 0.65f + 0.35f * Mathf.Sin((x1 + drift) * 0.012f);
                var mid0 = new Color(Tone, a * k0);
                var mid1 = new Color(Tone, a * k1);
                DrawPolygon(new[] { new Vector2(x0, cy - h / 2), new Vector2(x1, cy - h / 2), new Vector2(x1, cy), new Vector2(x0, cy) }, new[] { clear, clear, mid1, mid0 });
                DrawPolygon(new[] { new Vector2(x0, cy), new Vector2(x1, cy), new Vector2(x1, cy + h / 2), new Vector2(x0, cy + h / 2) }, new[] { mid0, mid1, clear, clear });
            }
        }
    }
}

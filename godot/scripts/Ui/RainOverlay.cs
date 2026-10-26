using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>마을 배경 위에 비와 젖은 바닥의 잔물결·조명 반사를 그린다.</summary>
    public partial class RainOverlay : Control
    {
        private struct Drop
        {
            public Vector2 Position;
            public float Speed;
            public float Length;
            public float Width;
        }

        private struct Ripple
        {
            public Vector2 Position;
            public float Age;
            public float Life;
            public float Radius;
        }

        private readonly List<Drop> _drops = new List<Drop>();
        private readonly List<Ripple> _ripples = new List<Ripple>();
        private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
        private float _clock;
        private float _rippleTimer;
        private bool _initialized;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _rng.Randomize();
            SetProcess(true);
        }

        public override void _Process(double delta)
        {
            if (Size.X <= 0 || Size.Y <= 0) return;
            float dt = Mathf.Min((float)delta, 0.05f);
            _clock += dt;

            if (!_initialized) InitializeDrops();

            for (int i = 0; i < _drops.Count; i++)
            {
                Drop drop = _drops[i];
                drop.Position += new Vector2(-drop.Speed * 0.22f * dt, drop.Speed * dt);
                if (drop.Position.Y > Size.Y + 24)
                {
                    drop.Position = new Vector2(_rng.RandfRange(0, Size.X), -_rng.RandfRange(8, 180));
                    drop.Speed = _rng.RandfRange(430, 760);
                    drop.Length = _rng.RandfRange(9, 23);
                    drop.Width = _rng.RandfRange(0.8f, 1.6f);
                }
                _drops[i] = drop;
            }

            _rippleTimer -= dt;
            if (_rippleTimer <= 0)
            {
                _rippleTimer = _rng.RandfRange(0.045f, 0.095f);
                _ripples.Add(new Ripple
                {
                    Position = new Vector2(_rng.RandfRange(0, Size.X), _rng.RandfRange(Size.Y * 0.62f, Size.Y * 0.98f)),
                    Age = 0,
                    Life = _rng.RandfRange(0.55f, 0.95f),
                    Radius = _rng.RandfRange(3.5f, 8.5f)
                });
            }

            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                Ripple ripple = _ripples[i];
                ripple.Age += dt;
                if (ripple.Age >= ripple.Life) _ripples.RemoveAt(i);
                else _ripples[i] = ripple;
            }

            QueueRedraw();
        }

        private void InitializeDrops()
        {
            _initialized = true;
            for (int i = 0; i < 125; i++)
            {
                _drops.Add(new Drop
                {
                    Position = new Vector2(_rng.RandfRange(0, Size.X), _rng.RandfRange(0, Size.Y)),
                    Speed = _rng.RandfRange(430, 760),
                    Length = _rng.RandfRange(9, 23),
                    Width = _rng.RandfRange(0.8f, 1.6f)
                });
            }
        }

        public override void _Draw()
        {
            if (Size.X <= 0 || Size.Y <= 0) return;

            // 비스듬히 떨어지는 빗줄기. 색을 옅게 유지해 배경의 디테일을 가리지 않는다.
            foreach (Drop drop in _drops)
            {
                float alpha = Mathf.Clamp(0.12f + (drop.Speed - 430f) / 330f * 0.12f, 0.12f, 0.24f);
                DrawLine(
                    drop.Position,
                    drop.Position + new Vector2(-drop.Length * 0.22f, drop.Length),
                    new Color(0.78f, 0.86f, 0.98f, alpha),
                    drop.Width,
                    true);
            }

            DrawWetReflections();

            // 바닥에 닿은 빗방울이 퍼지는 타원형 파문
            foreach (Ripple ripple in _ripples)
            {
                float progress = ripple.Age / ripple.Life;
                float radiusX = ripple.Radius * (0.45f + progress * 1.45f);
                float radiusY = radiusX * 0.28f;
                float alpha = 0.27f * (1f - progress);
                Color color = new Color(0.76f, 0.86f, 0.97f, alpha);
                DrawEllipse(ripple.Position, radiusX, radiusY, color, 1.15f);
            }
        }

        private void DrawWetReflections()
        {
            // 가게 조명이 돌바닥에 번져 보이도록 짧고 낮은 채도의 빛을 흔들어 준다.
            for (int i = 0; i < 30; i++)
            {
                float phase = _clock * (1.3f + (i % 4) * 0.17f) + i * 1.71f;
                float x = Size.X * (0.06f + (i % 6) * 0.17f) + Mathf.Sin(phase) * 11f;
                float y = Size.Y * (0.66f + (i / 6) * 0.065f) + Mathf.Sin(phase * 0.7f) * 3f;
                float length = 5f + (i % 5) * 2.4f;
                float alpha = 0.045f + (0.5f + 0.5f * Mathf.Sin(phase)) * 0.105f;
                Color color = i % 3 == 0
                    ? new Color(1f, 0.69f, 0.35f, alpha)
                    : new Color(0.68f, 0.79f, 0.94f, alpha * 0.9f);
                DrawLine(new Vector2(x, y), new Vector2(x + length, y + 0.7f), color, 1.6f, true);
            }
        }

        private void DrawEllipse(Vector2 center, float radiusX, float radiusY, Color color, float width)
        {
            const int segments = 18;
            Vector2 previous = center + new Vector2(radiusX, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Tau * i / segments;
                Vector2 next = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                DrawLine(previous, next, color, width, true);
                previous = next;
            }
        }
    }
}
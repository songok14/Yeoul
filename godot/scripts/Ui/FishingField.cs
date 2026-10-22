using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 방파제 바다 위에 그리는 것들: 던질 방향 부채꼴과 바늘, 고등어가 가리키는 반짝이는 지점, 찌,
    /// 줄 끝에서 물고기가 몸부림치는 물(물고기 모습은 그리지 않는다), 물살, 물보라와 물기둥, 낚싯줄, 쿨러.
    /// 좌표는 ArtStage 논리 좌표(450×800)로 받아 그릴 때 화면 픽셀로 바꾼다. 상태는 FishingScreen 이 정한다.
    /// </summary>
    public partial class FishingField : Control
    {
        private sealed class Drop
        {
            public Vector2 P;
            public Vector2 V;
            public float Life;
            public float Max;
            public float Size;
        }

        private sealed class Ring
        {
            public Vector2 At;
            public float Scale;
            public float T;
            public float Speed;
        }

        private readonly ArtStage _stage;
        private readonly List<Drop> _drops = new List<Drop>();
        private readonly List<Ring> _rings = new List<Ring>();
        private readonly List<Vector2> _trail = new List<Vector2>();
        private readonly Random _rand = new Random(7);
        private float _time;
        private float _trailTimer;

        /// <summary>방향(-1~1)과 거리(0~1) → 논리 좌표와 그 거리에서의 크기(z)</summary>
        public Func<float, float, Vector3> PosAt;

        // 부채꼴: 바늘이 좌우로 오가고, 누르면 멈춘다. 파워 단계에서는 바늘 위로 떨어질 자리가 움직인다
        public bool ShowFan;
        public float FanReach = 1;
        public float Needle;
        public bool NeedleFixed;
        /// <summary>파워 게이지가 가리키는 거리 (없으면 null)</summary>
        public float? Aiming;

        // 고등어가 가리키는 반짝이는 지점
        public bool ShowHint;
        public float HintAngle, HintDist;

        // 찌
        public bool BobberVisible;
        public Vector2 BobberAt;
        public float BobberScale = 1;
        /// <summary>물에 떠 있으면 true (날아가는 중에는 물결을 그리지 않는다)</summary>
        public bool BobberFloating;
        /// <summary>1이면 찌가 물속으로 들어갔다</summary>
        public float BobberSink;

        // 줄 끝 물속에서 몸부림치는 자리 (물고기 모습은 그리지 않고 물만 들끓는다)
        public bool ShadowVisible;
        public Vector2 ShadowAt;
        public float ShadowSize = 1;
        public float ShadowWobble;
        /// <summary>큰 놈이면 물이 더 크게 들끓는다</summary>
        public bool Big;

        // 낚싯줄. 낚싯대는 SD 그림에 있고, 화면이 그림 속 대 끝 자리를 RodTip 에 넣어 준다
        public Vector2 RodTip;
        /// <summary>텐션 0~1. 팽팽할수록 줄이 곧게 선다</summary>
        public float Bend;
        /// <summary>릴링 중이면 텐션에 따라 줄이 떨리고 색이 바뀐다</summary>
        public bool Fighting;
        public Vector2? LineTo;

        // 쿨러 (고등어가 노린다)
        public Vector2 CoolerAt;
        public float CoolerLid;

        public FishingField() { }

        public FishingField(ArtStage stage)
        {
            _stage = stage;
            MouseFilter = MouseFilterEnum.Stop;
        }

        /// <summary>찌가 떨어지거나 고기가 차고 나갈 때 물보라. scale 이 클수록 물방울이 많고 높이 튄다</summary>
        public void AddSplash(Vector2 at, float scale)
        {
            _rings.Add(new Ring { At = at, Scale = scale, T = 0, Speed = 1 / 0.7f });
            _rings.Add(new Ring { At = at, Scale = scale * 0.6f, T = -0.15f, Speed = 1 / 0.6f });
            int n = (int)(8 + 10 * scale);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Pi * (1.08f + 0.84f * (float)_rand.NextDouble());
                float sp = (30 + 60 * (float)_rand.NextDouble()) * Mathf.Sqrt(scale);
                _drops.Add(new Drop
                {
                    P = at,
                    V = new Vector2(Mathf.Cos(a) * sp * 0.8f, Mathf.Sin(a) * sp * 1.4f),
                    Life = 0,
                    Max = 0.45f + 0.35f * (float)_rand.NextDouble(),
                    Size = (1.2f + 1.6f * (float)_rand.NextDouble()) * Mathf.Sqrt(scale),
                });
            }
        }

        /// <summary>물고기가 물을 박차는 물기둥: 물보라에 위로 높이 솟는 물방울을 더한다</summary>
        public void AddBurst(Vector2 at, float scale)
        {
            AddSplash(at, scale);
            int n = (int)(10 + 8 * scale);
            for (int i = 0; i < n; i++)
            {
                float a = -Mathf.Pi / 2 + (float)(_rand.NextDouble() - 0.5) * 0.7f;
                float sp = (70 + 70 * (float)_rand.NextDouble()) * Mathf.Sqrt(scale);
                _drops.Add(new Drop
                {
                    P = at + new Vector2((float)(_rand.NextDouble() - 0.5) * 8 * scale, 0),
                    V = new Vector2(Mathf.Cos(a) * sp * 0.6f, Mathf.Sin(a) * sp * 1.3f),
                    Max = 0.6f + 0.35f * (float)_rand.NextDouble(),
                    Size = (1.4f + 1.8f * (float)_rand.NextDouble()) * Mathf.Sqrt(scale),
                });
            }
        }

        /// <summary>물방울 몇 개만 (물고기가 지나가며 튀기는 것)</summary>
        public void AddSpray(Vector2 at, float scale, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Pi * (1.2f + 0.6f * (float)_rand.NextDouble());
                float sp = 20 + 30 * (float)_rand.NextDouble();
                _drops.Add(new Drop { P = at, V = new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp * 1.2f), Max = 0.4f, Size = 1.1f * scale });
            }
        }

        public void ClearTrail()
        {
            _trail.Clear();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _time += dt;
            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                _rings[i].T += dt * _rings[i].Speed;
                if (_rings[i].T >= 1) _rings.RemoveAt(i);
            }
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                d.Life += dt;
                d.V += new Vector2(0, 160 * dt);
                d.P += d.V * dt;
                if (d.Life >= d.Max) _drops.RemoveAt(i);
            }
            // 그림자가 지나간 자리 (물살)
            _trailTimer -= dt;
            if (ShadowVisible && _trailTimer <= 0)
            {
                _trailTimer = 0.05f;
                _trail.Insert(0, ShadowAt);
                if (_trail.Count > 16) _trail.RemoveAt(_trail.Count - 1);
            }
            if (!ShadowVisible && _trail.Count > 0 && _trailTimer <= 0)
            {
                _trailTimer = 0.05f;
                _trail.RemoveAt(_trail.Count - 1);
            }
            QueueRedraw();
        }

        private Vector2 P(Vector2 logical)
        {
            return _stage.ToLocal(logical);
        }

        private Vector2 P(Vector3 logical)
        {
            return _stage.ToLocal(new Vector2(logical.X, logical.Y));
        }

        private float K
        {
            get { return _stage.K; }
        }

        public override void _Draw()
        {
            if (_stage == null) return;
            if (ShowFan && PosAt != null) DrawFan();
            if (ShowHint && PosAt != null) DrawHint();
            DrawTrail();
            foreach (var r in _rings) DrawRing(r);
            if (ShadowVisible) DrawShadow();
            if (BobberVisible) DrawBobber();
            DrawLine();
            foreach (var d in _drops) DrawDrop(d);
            DrawCooler();
        }

        // ───────── 부채꼴 ─────────

        private void DrawFan()
        {
            const int n = 24;
            float near = 0.04f;
            float far = FanReach;
            var pts = new Vector2[2 * (n + 1)];
            for (int i = 0; i <= n; i++)
            {
                float a = -1 + 2f * i / n;
                pts[i] = P(PosAt(a, far));
                pts[2 * n + 1 - i] = P(PosAt(a, near));
            }
            float pulse = NeedleFixed ? 0.13f : 0.15f + 0.04f * Mathf.Sin(_time * 3);
            DrawColoredPolygon(pts, new Color(1, 1, 1, pulse));
            // 테두리와 거리 구간 (근거리 | 중거리 | 원거리)
            DrawPolyline(Arc(far, n), new Color(1, 1, 1, 0.75f), 4, true);
            DrawLine(P(PosAt(-1, near)), P(PosAt(-1, far)), new Color(1, 1, 1, 0.6f), 3.5f, true);
            DrawLine(P(PosAt(1, near)), P(PosAt(1, far)), new Color(1, 1, 1, 0.6f), 3.5f, true);
            foreach (float zone in new[] { 0.35f, 0.7f })
                if (zone < far - 0.03f) DashedArc(zone, new Color(1, 1, 1, 0.45f));

            // 바늘: 발밑에서 수평선 쪽으로 뻗은 빛줄기
            float w = 0.035f;
            var ray = new[]
            {
                P(PosAt(Needle - w * 1.6f, near)), P(PosAt(Needle + w * 1.6f, near)),
                P(PosAt(Needle + w * 0.5f, far)), P(PosAt(Needle - w * 0.5f, far)),
            };
            var gold = Palette.Gold;
            DrawColoredPolygon(ray, new Color(gold, NeedleFixed ? 0.6f : 0.45f));
            DrawLine(P(PosAt(Needle, near)), P(PosAt(Needle, far)), new Color(1, 0.97f, 0.85f, 1), 4, true);

            // 파워 단계: 떨어질 자리 동그라미
            if (Aiming != null)
            {
                var at = PosAt(Needle, Aiming.Value);
                var c = P(at);
                float r = 13 * at.Z * K;
                Ellipse(c, r * (1.15f + 0.1f * Mathf.Sin(_time * 10)), 0.36f, new Color(gold, 0.95f), 3.5f);
                Ellipse(c, r * 0.45f, 0.36f, new Color(1, 1, 1, 0.9f), 2.5f);
            }
        }

        private Vector2[] Arc(float d, int n)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++) pts[i] = P(PosAt(-1 + 2f * i / n, d));
            return pts;
        }

        private void DashedArc(float d, Color c)
        {
            const int n = 30;
            for (int i = 0; i < n; i += 2)
                DrawLine(P(PosAt(-1 + 2f * i / n, d)), P(PosAt(-1 + 2f * (i + 1) / n, d)), c, 3, true);
        }

        // ───────── 고등어 지점: 물비늘이 반짝이고 물결이 번진다 ─────────

        private void DrawHint()
        {
            var at = PosAt(HintAngle, HintDist);
            var c = P(at);
            float z = at.Z * K;
            // 금빛으로 일렁이는 물 (가운데가 밝다)
            float glow = 0.22f + 0.08f * Mathf.Sin(_time * 2.5f);
            DrawSetTransform(c, 0, new Vector2(1, 0.34f));
            DrawCircle(Vector2.Zero, 20 * z, new Color(Palette.Gold, glow * 0.6f));
            DrawCircle(Vector2.Zero, 11 * z, new Color(1, 0.95f, 0.75f, glow));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            for (int i = 0; i < 3; i++)
            {
                float ph = (_time / 1.6f + i / 3f) % 1f;
                Ellipse(c, 12 * (1 + 1.8f * ph) * z, 0.34f, new Color(i == 0 ? Palette.Gold : Palette.Foam, 1 - ph), 3.5f);
            }
            // 반짝이는 별 여섯
            for (int i = 0; i < 6; i++)
            {
                float seed = i * 1.7f;
                float tw = Mathf.Max(0, Mathf.Sin(_time * 3.2f + seed * 2.3f));
                var off = new Vector2(Mathf.Sin(seed * 3.1f) * 24, Mathf.Cos(seed * 2.2f) * 7 - 3) * z;
                Sparkle(c + off, (6 + 8 * tw) * z * 0.6f, new Color(1, 0.98f, 0.85f, 0.45f + 0.55f * tw));
            }
            // 가끔 작은 고기가 톡 튀어 오른다
            float jt = (_time % 2.6f) / 0.5f;
            if (jt < 1)
            {
                var j = c + new Vector2(10 * z, -Mathf.Sin(jt * Mathf.Pi) * 10 * z);
                DrawCircle(j, 1.6f * z, new Color(0.9f, 0.95f, 1, 0.8f));
            }
        }

        private void Sparkle(Vector2 c, float r, Color col)
        {
            var pts = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.Pi / 4 * i;
                float rr = i % 2 == 0 ? r : r * 0.22f;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
            }
            DrawColoredPolygon(pts, col);
        }

        // ───────── 물보라 ─────────

        private void DrawRing(Ring r)
        {
            if (r.T < 0) return;
            var c = P(r.At);
            float t = r.T;
            float ease = 1 - (1 - t) * (1 - t);
            float u = K * r.Scale;
            Ellipse(c, (8 + 30 * ease) * u, 0.3f, new Color(Palette.Foam, 0.9f * (1 - t)), 3);
            // 흰 물거품 덩어리 (처음에만)
            if (t < 0.35f)
            {
                DrawSetTransform(c, 0, new Vector2(1, 0.4f));
                DrawCircle(Vector2.Zero, (6 + 10 * t) * u, new Color(1, 1, 1, 0.55f * (1 - t / 0.35f)));
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            }
        }

        private void DrawDrop(Drop d)
        {
            float k = 1 - d.Life / d.Max;
            var c = P(d.P);
            float r = Mathf.Max(1.4f, d.Size * K * 0.6f);
            DrawCircle(c, r, new Color(Palette.Foam, 0.9f * k));
            DrawCircle(c - new Vector2(r * 0.3f, r * 0.3f), r * 0.4f, new Color(1, 1, 1, 0.9f * k));
        }

        // ───────── 물속 몸부림 ─────────

        /// <summary>그림자가 지나간 자리: 하얀 물살 V 와 거품</summary>
        private void DrawTrail()
        {
            for (int i = 1; i < _trail.Count; i++)
            {
                float k = 1 - i / (float)_trail.Count;
                var a = P(_trail[i - 1]);
                var b = P(_trail[i]);
                var dir = (a - b).Normalized();
                var side = new Vector2(-dir.Y, dir.X) * (5 + i * 2.2f) * K * 0.5f * ShadowSize;
                var col = new Color(Palette.Foam, 0.35f * k * (Big ? 1.3f : 1));
                DrawLine(b + side, a + side * 0.6f, col, 2, true);
                DrawLine(b - side, a - side * 0.6f, col, 2, true);
                if (i % 3 == 0) DrawCircle(b + side * 0.3f * Mathf.Sin(i + _time * 4), 1.6f * K * 0.5f, new Color(1, 1, 1, 0.5f * k));
            }
        }

        /// <summary>
        /// 줄 끝 물속에서 무언가 몸부림친다: 물빛이 어둡게 가라앉고, 거품 고리가 좌우로 휘돌며, 물방울이 톡톡 솟는다.
        /// 물고기 모습은 그리지 않는다
        /// </summary>
        private void DrawShadow()
        {
            var c = P(ShadowAt);
            float r = 22 * ShadowSize * K * (Big ? 1.3f : 1);
            // 깊은 물빛 (가장자리로 갈수록 옅다)
            DrawSetTransform(c, 0, new Vector2(1, 0.36f));
            DrawCircle(Vector2.Zero, r * 1.25f, new Color(0.03f, 0.16f, 0.24f, 0.16f));
            DrawCircle(Vector2.Zero, r * 0.8f, new Color(0.02f, 0.12f, 0.2f, 0.18f));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            // 휘도는 거품 조각 세 개
            float spin = ShadowWobble * 1.6f;
            for (int i = 0; i < 3; i++)
            {
                float a0 = spin + i * Mathf.Tau / 3;
                float rr = r * (0.75f + 0.18f * Mathf.Sin(ShadowWobble * 2 + i));
                FoamArc(c, rr, a0, a0 + 1.3f, new Color(1, 1, 1, 0.75f), 3.5f);
                FoamArc(c, rr * 0.55f, -a0, -a0 + 0.9f, new Color(Palette.Foam, 0.6f), 2.5f);
            }
            // 바깥으로 번지는 물결
            for (int i = 0; i < 2; i++)
            {
                float ph = (_time * 1.4f + i * 0.5f) % 1f;
                Ellipse(c, r * (1 + 0.9f * ph), 0.34f, new Color(Palette.Foam, 0.5f * (1 - ph)), 2.5f);
            }
            // 거품 알갱이
            for (int i = 0; i < 6; i++)
            {
                float t = (_time * 1.8f + i / 6f) % 1f;
                float a = i * 2.1f + ShadowWobble;
                var at = c + new Vector2(Mathf.Cos(a) * r * 0.7f, Mathf.Sin(a) * r * 0.25f - t * 10 * K * 0.5f);
                DrawCircle(at, (1.4f + 1.4f * (1 - t)) * K * 0.5f, new Color(1, 1, 1, 0.8f * (1 - t)));
            }
        }

        /// <summary>납작한 원 위의 짧은 호 (거품 줄기)</summary>
        private void FoamArc(Vector2 c, float r, float from, float to, Color col, float width)
        {
            const int n = 10;
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(from, to, (float)i / n);
                pts[i] = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.34f);
            }
            DrawPolyline(pts, col, width, true);
        }

        // ───────── 찌와 줄 ─────────

        private void DrawBobber()
        {
            var c = P(BobberAt);
            float u = K * BobberScale * 0.9f;
            float sink = BobberSink;
            float alpha = 1 - 0.6f * sink;
            if (BobberFloating)
            {
                float w = 1 + 0.12f * Mathf.Sin(_time * 3);
                Ellipse(c, 11 * u * w, 0.28f, new Color(Palette.Foam, 0.55f), 2);
            }
            var o = c + new Vector2(0, sink * 9 * u);
            // 안테나와 몸통 (위는 빨강, 아래는 흰색)
            DrawLine(o + new Vector2(0, -6 * u), o + new Vector2(0, -19 * u), new Color(Palette.Ink, alpha), Mathf.Max(2, 1.4f * u));
            DrawCircle(o + new Vector2(0, -19.5f * u), Mathf.Max(2, 1.6f * u), new Color(Palette.Gold, alpha));
            var body = o + new Vector2(0, -4 * u);
            float r = 4.6f * u;
            DrawCircle(body, r, new Color(Colors.White, alpha));
            var top = new Vector2[18];
            for (int i = 0; i < top.Length; i++)
            {
                float a = Mathf.Pi + Mathf.Pi * i / (top.Length - 1);
                top[i] = body + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            DrawColoredPolygon(top, new Color(Palette.Red, alpha));
            DrawArc(body, r, 0, Mathf.Tau, 28, new Color(Palette.Ink, alpha * 0.9f), Mathf.Max(1.5f, 0.7f * u), true);
            // 물에 잠긴 아래쪽은 물빛으로 덮는다
            if (BobberFloating)
            {
                var cover = new Vector2[] { c + new Vector2(-r * 1.3f, 0), c + new Vector2(r * 1.3f, 0), c + new Vector2(r * 1.3f, r * 1.4f), c + new Vector2(-r * 1.3f, r * 1.4f) };
                DrawColoredPolygon(cover, new Color(0.17f, 0.45f, 0.6f, 0.55f));
            }
        }

        /// <summary>
        /// 낚싯줄. 느슨하면 크게 처져 흔들리고, 팽팽할수록 곧게 펴진다.
        /// 릴링 중(Fighting) 위험 가까이 가면 줄이 파르르 떨리고 금빛을 지나 빨갛게 달아오른다
        /// </summary>
        private void DrawLine()
        {
            if (LineTo == null) return;
            var tip = P(RodTip);
            var to = P(LineTo.Value);
            float u = K / 2.4f;
            float bend = Mathf.Clamp(Bend, 0, 1.1f);
            float slackness = Mathf.Clamp(1 - bend / 0.55f, 0, 1);
            // 처짐: 느슨하면 깊게, 팽팽하면 거의 없다. 느슨할 때는 바람에 살랑인다
            float sag = (6 + 64 * slackness * slackness) * u + Mathf.Sin(_time * 2.2f) * 6 * slackness * u;
            var mid = (tip + to) / 2 + new Vector2(0, sag);
            var pts = Bezier(tip, mid, to, 28);
            // 팽팽하면 줄이 파르르 떨린다 (가운데가 가장 크게)
            float shiver = Fighting ? Mathf.Clamp((bend - 0.62f) / 0.3f, 0, 1) : 0;
            if (shiver > 0)
            {
                var dir = (to - tip).Normalized();
                var nrm = new Vector2(-dir.Y, dir.X);
                for (int i = 1; i < pts.Length - 1; i++)
                {
                    float t = (float)i / (pts.Length - 1);
                    pts[i] += nrm * Mathf.Sin(_time * 70 + t * 9) * Mathf.Sin(t * Mathf.Pi) * 3.2f * shiver * u;
                }
            }
            // 색: 흰색 → 금빛 → 빨강
            var white = new Color(1, 1, 1, 0.92f);
            var hot = new Color(1f, 0.82f, 0.36f, 1f);
            var danger = new Color(1f, 0.35f, 0.3f, 1f);
            var col = !Fighting ? white : bend < 0.85f ? white.Lerp(hot, Mathf.Clamp((bend - 0.6f) / 0.25f, 0, 1)) : hot.Lerp(danger, Mathf.Clamp((bend - 0.85f) / 0.15f, 0, 1));
            float width = (Fighting ? 2.6f + 1.2f * Mathf.Clamp(bend, 0, 1) : 2.2f) * Mathf.Max(1, u);
            // 밝은 물 위에서도 보이게 어두운 테두리를 먼저
            DrawPolyline(pts, new Color(0.05f, 0.12f, 0.18f, 0.35f), width + 2.4f * u, true);
            DrawPolyline(pts, col, width, true);
            // 위험하면 줄 둘레가 은은히 빛난다
            if (Fighting && bend >= 0.85f) DrawPolyline(pts, new Color(danger, 0.25f + 0.15f * Mathf.Sin(_time * 18)), width + 6 * u, true);
        }

        // ───────── 쿨러 ─────────

        /// <summary>발밑 쿨러: 둥근 파란 몸통에 흰 뚜껑과 손잡이. 고등어가 노리면 뚜껑이 들썩인다</summary>
        private void DrawCooler()
        {
            if (CoolerAt == Vector2.Zero) return;
            var c = P(CoolerAt);
            float u = K * 1.1f;
            var ink = new Color("1f3b55");
            // 바닥 그림자
            DrawSetTransform(c + new Vector2(2 * u, 0), 0, new Vector2(1, 0.25f));
            DrawCircle(Vector2.Zero, 26 * u, new Color(0, 0, 0, 0.22f));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            // 몸통: 위는 밝고 아래는 짙은 두 겹
            var body = new Rect2(c.X - 21 * u, c.Y - 25 * u, 42 * u, 25 * u);
            DrawStyleBox(Box(new Color("3f86c2"), ink, 4 * u, 1.6f * u), body);
            DrawStyleBox(Box(new Color("2f6aa0"), new Color(0, 0, 0, 0), 4 * u, 0), new Rect2(body.Position.X + 1.6f * u, body.End.Y - 8 * u, body.Size.X - 3.2f * u, 6.4f * u));
            // 이름표와 빛
            DrawStyleBox(Box(new Color(1, 1, 1, 0.85f), new Color(0, 0, 0, 0), 1.5f * u, 0), new Rect2(c.X - 8 * u, c.Y - 17 * u, 16 * u, 5 * u));
            DrawLine(new Vector2(body.Position.X + 4 * u, body.Position.Y + 4 * u), new Vector2(body.Position.X + 4 * u, body.End.Y - 10 * u), new Color(1, 1, 1, 0.35f), 1.6f * u);
            // 옆 손잡이
            DrawArc(new Vector2(body.Position.X, body.Position.Y + 9 * u), 4 * u, Mathf.Pi * 0.5f, Mathf.Pi * 1.5f, 10, ink, 1.6f * u, true);
            DrawArc(new Vector2(body.End.X, body.Position.Y + 9 * u), 4 * u, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f, 10, ink, 1.6f * u, true);
            // 뚜껑 (경첩은 왼쪽 아래)
            float lift = CoolerLid * 4 * u;
            var hinge = new Vector2(c.X - 23 * u, c.Y - 24 * u - lift);
            DrawSetTransform(hinge, -CoolerLid * 0.3f, Vector2.One);
            DrawStyleBox(Box(new Color("f4f1e8"), ink, 3 * u, 1.6f * u), new Rect2(0, -8 * u, 46 * u, 8 * u));
            DrawLine(new Vector2(4 * u, -2.6f * u), new Vector2(42 * u, -2.6f * u), new Color("d6d0c2"), 1.4f * u);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }

        private static StyleBoxFlat Box(Color fill, Color border, float radius, float borderWidth)
        {
            var sb = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
            sb.SetCornerRadiusAll((int)Mathf.Round(radius));
            sb.SetBorderWidthAll((int)Mathf.Round(borderWidth));
            return sb;
        }

        private static Vector2[] Bezier(Vector2 a, Vector2 c, Vector2 b, int n)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                pts[i] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;
            }
            return pts;
        }

        /// <summary>납작한 타원 테두리 (세로 비율 ry)</summary>
        private void Ellipse(Vector2 c, float rx, float ry, Color color, float width)
        {
            var pts = new Vector2[41];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = Mathf.Tau * i / (pts.Length - 1);
                pts[i] = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * rx * ry);
            }
            DrawPolyline(pts, color, width, true);
        }
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 방파제 바다 위에 그리는 것들: 던질 방향 부채꼴과 바늘, 고등어가 가리키는 반짝이는 지점, 찌,
    /// 물고기 그림자(어종 그림을 어둡게 눌러 물속에서 몸을 흔든다)와 물살, 튀어 오르는 물고기, 물보라, 낚싯줄, 쿨러.
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

        // 물고기 그림자: 어종 그림을 어둡게 눌러 그린다
        public bool ShadowVisible;
        public Vector2 ShadowAt;
        public float ShadowSize = 1;
        public float ShadowWobble;
        public Texture2D FishTexture;
        /// <summary>큰 놈이면 더 진하고 물살이 크다</summary>
        public bool Big;

        // 물 위로 튀어 오르는 물고기 (저항, 끌어올리기)
        public bool Jumping;
        public Vector2 JumpAt;
        public float JumpScale = 1;
        public float JumpSpin;

        // 낚싯줄. 낚싯대는 SD 그림에 있고, 화면이 그림 속 대 끝 자리를 RodTip 에 넣어 준다
        public Vector2 RodTip;
        /// <summary>텐션 0~1. 팽팽할수록 줄이 곧게 선다</summary>
        public float Bend;
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
            if (Jumping) DrawJump();
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
            float pulse = NeedleFixed ? 0.06f : 0.07f + 0.02f * Mathf.Sin(_time * 3);
            DrawColoredPolygon(pts, new Color(1, 1, 1, pulse));
            // 테두리와 거리 구간 (근거리 | 중거리 | 원거리)
            DrawPolyline(Arc(far, n), new Color(1, 1, 1, 0.38f), 2.5f, true);
            DrawLine(P(PosAt(-1, near)), P(PosAt(-1, far)), new Color(1, 1, 1, 0.3f), 2, true);
            DrawLine(P(PosAt(1, near)), P(PosAt(1, far)), new Color(1, 1, 1, 0.3f), 2, true);
            foreach (float zone in new[] { 0.35f, 0.7f })
                if (zone < far - 0.03f) DashedArc(zone, new Color(1, 1, 1, 0.22f));

            // 바늘: 발밑에서 수평선 쪽으로 뻗은 빛줄기
            float w = 0.035f;
            var ray = new[]
            {
                P(PosAt(Needle - w * 1.6f, near)), P(PosAt(Needle + w * 1.6f, near)),
                P(PosAt(Needle + w * 0.5f, far)), P(PosAt(Needle - w * 0.5f, far)),
            };
            var gold = Palette.Gold;
            DrawColoredPolygon(ray, new Color(gold, NeedleFixed ? 0.45f : 0.32f));
            DrawLine(P(PosAt(Needle, near)), P(PosAt(Needle, far)), new Color(1, 0.97f, 0.85f, NeedleFixed ? 0.95f : 0.8f), 3, true);

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
                DrawLine(P(PosAt(-1 + 2f * i / n, d)), P(PosAt(-1 + 2f * (i + 1) / n, d)), c, 2, true);
        }

        // ───────── 고등어 지점: 물비늘이 반짝이고 물결이 번진다 ─────────

        private void DrawHint()
        {
            var at = PosAt(HintAngle, HintDist);
            var c = P(at);
            float z = at.Z * K;
            for (int i = 0; i < 3; i++)
            {
                float ph = (_time / 1.6f + i / 3f) % 1f;
                Ellipse(c, 12 * (1 + 1.8f * ph) * z, 0.34f, new Color(Palette.Foam, 0.85f * (1 - ph)), 2.5f);
            }
            // 반짝이는 별 다섯
            for (int i = 0; i < 5; i++)
            {
                float seed = i * 1.7f;
                float tw = Mathf.Max(0, Mathf.Sin(_time * 3.2f + seed * 2.3f));
                var off = new Vector2(Mathf.Sin(seed * 3.1f) * 22, Mathf.Cos(seed * 2.2f) * 6 - 2) * z;
                Sparkle(c + off, (3 + 4 * tw) * z * 0.6f, new Color(1, 0.98f, 0.85f, 0.35f + 0.65f * tw));
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

        // ───────── 물고기 ─────────

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
        /// 물속 물고기: 어종 그림을 몸통 띠로 잘라 S자로 흔들고, 짙은 물빛으로 눌러 칠한다.
        /// 여러 번 살짝 어긋나게 그려서 물에 번진 듯 흐리게 한다
        /// </summary>
        private void DrawShadow()
        {
            var c = P(ShadowAt);
            float len = 46 * ShadowSize * K * (Big ? 1.25f : 1);
            if (FishTexture == null)
            {
                DrawSetTransform(c, 0, new Vector2(1, 0.34f));
                DrawCircle(Vector2.Zero, len * 0.4f, new Color(0.04f, 0.1f, 0.16f, 0.5f));
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                return;
            }
            // 좌우로 몸부림치는 방향에 따라 머리가 그쪽을 본다 (그림은 왼쪽을 본다)
            float swim = Mathf.Cos(ShadowWobble);
            bool faceRight = swim > 0;
            float heading = Mathf.Sin(ShadowWobble * 1.7f) * 0.22f;
            float alpha = Big ? 0.62f : 0.5f;
            var tint = new Color(0.03f, 0.1f, 0.16f, alpha * 0.35f);
            // 물 위로 비치는 밝은 테두리
            Ellipse(c, len * 0.62f, 0.3f, new Color(Palette.Foam, 0.12f + 0.08f * Mathf.Sin(_time * 6)), 2);
            for (int k = 0; k < 4; k++)
            {
                var off = new Vector2(Mathf.Cos(k * 1.57f), Mathf.Sin(k * 1.57f)) * 1.6f * K * 0.5f;
                FishMesh(c + off, len, faceRight, heading, tint);
            }
            FishMesh(c, len * 0.94f, faceRight, heading, new Color(0.02f, 0.07f, 0.12f, alpha * 0.55f));
        }

        private void FishMesh(Vector2 c, float len, bool faceRight, float heading, Color tint)
        {
            const int n = 8;
            float h = len * FishTexture.GetHeight() / FishTexture.GetWidth() * 0.55f;
            var pts = new Vector2[2 * (n + 1)];
            var uvs = new Vector2[2 * (n + 1)];
            var cols = new Color[2 * (n + 1)];
            float cos = Mathf.Cos(heading), sin = Mathf.Sin(heading);
            for (int i = 0; i <= n; i++)
            {
                float u = (float)i / n;
                // 꼬리 쪽일수록 크게 흔들린다 (그림의 오른쪽이 꼬리)
                float bend = Mathf.Sin(ShadowWobble * 3 - u * 3.2f) * u * u * h * 0.55f;
                float x = (u - 0.5f) * len * (faceRight ? -1 : 1);
                var top = new Vector2(x, -h / 2 + bend);
                var bot = new Vector2(x, h / 2 + bend);
                pts[i] = c + Rot(top, cos, sin);
                pts[2 * n + 1 - i] = c + Rot(bot, cos, sin);
                uvs[i] = new Vector2(u, 0);
                uvs[2 * n + 1 - i] = new Vector2(u, 1);
            }
            for (int i = 0; i < cols.Length; i++) cols[i] = tint;
            DrawPolygon(pts, cols, uvs, FishTexture);
        }

        private static Vector2 Rot(Vector2 v, float cos, float sin)
        {
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        /// <summary>물 밖으로 튀어 오른 물고기 (제 색 그대로)</summary>
        private void DrawJump()
        {
            if (FishTexture == null) return;
            var c = P(JumpAt);
            float len = 58 * JumpScale * K;
            float h = len * FishTexture.GetHeight() / FishTexture.GetWidth();
            DrawSetTransform(c, JumpSpin, Vector2.One);
            DrawTextureRect(FishTexture, new Rect2(-len / 2, -h / 2, len, h), false);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            // 몸에서 떨어지는 물방울 반짝
            for (int i = 0; i < 3; i++)
            {
                float t = (_time * 3 + i / 3f) % 1f;
                DrawCircle(c + new Vector2((i - 1) * len * 0.25f, h * 0.3f + t * 18 * K * 0.5f), 2.2f * K * 0.5f, new Color(1, 1, 1, 0.8f * (1 - t)));
            }
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

        private void DrawLine()
        {
            if (LineTo == null) return;
            var tip = P(RodTip);
            var to = P(LineTo.Value);
            // 느슨하면 아래로 처진다
            var lm = (tip + to) / 2 + new Vector2(0, (1 - Bend) * 34 * (K / 2.4f));
            DrawPolyline(Bezier(tip, lm, to, 18), new Color(1, 1, 1, 0.85f), 2.2f, true);
        }

        // ───────── 쿨러 ─────────

        /// <summary>발밑 쿨러: 파란 몸통에 흰 뚜껑. 고등어가 노리면 뚜껑이 들썩인다</summary>
        private void DrawCooler()
        {
            if (CoolerAt == Vector2.Zero) return;
            var c = P(CoolerAt);
            float u = K;
            var body = new Rect2(c.X - 20 * u, c.Y - 24 * u, 40 * u, 24 * u);
            DrawRect(new Rect2(body.Position + new Vector2(2 * u, 3 * u), body.Size), new Color(0, 0, 0, 0.18f));
            DrawRect(body, new Color("3f7fb5"));
            DrawRect(new Rect2(body.Position.X, body.End.Y - 5 * u, body.Size.X, 5 * u), new Color("2f6596"));
            DrawRect(new Rect2(c.X - 6 * u, c.Y - 16 * u, 12 * u, 3 * u), new Color(1, 1, 1, 0.75f));
            float lift = CoolerLid * 5 * u;
            var lid = new Rect2(c.X - 22 * u, c.Y - 30 * u - lift, 44 * u, 7 * u);
            DrawSetTransform(new Vector2(lid.Position.X, lid.End.Y), -CoolerLid * 0.25f, Vector2.One);
            DrawRect(new Rect2(0, -lid.Size.Y, lid.Size.X, lid.Size.Y), new Color("f3f1ea"));
            DrawRect(new Rect2(0, -2 * u, lid.Size.X, 2 * u), new Color("cfcac0"));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
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

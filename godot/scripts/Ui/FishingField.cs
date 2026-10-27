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
        /// <summary>부채꼴 끝에 쓰는 최대 거리 (예: "최대 18m")</summary>
        public string FanLabel;
        public float Needle;
        public bool NeedleFixed;
        /// <summary>파워 게이지가 가리키는 거리 (없으면 null)</summary>
        public float? Aiming;

        // 고등어가 가리키는 반짝이는 지점
        public bool ShowHint;
        public float HintAngle, HintDist;

        // 찌. 가진 찌 그림이 있으면 그 그림 윗부분을 물 위에 띄운다
        public Texture2D FloatTex;
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

        // 힘싸움: 물고기 힘 막대, 질주 방향(0 없음), 예고 중인지, 반대쪽을 눌러 버티는 중인지
        public bool ShowStamina;
        public float FishStamina = 1;
        public int RunDir;
        public bool RunWarn;
        public bool Bracing;

        // 낚싯대 끝 (논리 좌표). 대를 그리면(RodDrawn) 여기서 계산하고, 아니면 화면이 그림 속 대 끝을 넣어 준다
        public Vector2 RodTip;
        // 포즈 그림에서는 대를 지웠다. 손잡이·방향·길이로 여기서 그리고 텐션만큼 물고기 쪽으로 휜다
        public bool RodDrawn;
        public Vector2 RodGrip;
        public Vector2 RodDir;
        public float RodLen;
        /// <summary>텐션 0~1. 팽팽할수록 줄이 곧게 선다</summary>
        public float Bend;
        /// <summary>릴링 중이면 텐션에 따라 줄이 떨리고 색이 바뀐다</summary>
        public bool Fighting;
        public Vector2? LineTo;
        /// <summary>대 끝이 툭 튀는 세기 (물고기가 머리를 흔들거나 챔질할 때). 저절로 줄어든다</summary>
        public float RodKick;
        /// <summary>화면이 하얗게 번쩍이는 세기 0~1. 저절로 줄어든다</summary>
        public float Flash;

        // 쿨러 (고등어가 노린다)
        public Vector2 CoolerAt;
        /// <summary>가진 쿨러 그림 (여백을 자른 것). 없으면 코드로 그린 파란 쿨러</summary>
        public Texture2D CoolerTex;
        /// <summary>낚싯대 색: 몸통, 손잡이, 고리·띠 (가진 대의 색)</summary>
        public Color RodBlank = new Color("3a322b"), RodHandle = new Color("d9a46a"), RodTrim = new Color("c9a24a");
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
            RodKick = Mathf.Max(0, RodKick - dt * 3.5f);
            Flash = Mathf.Max(0, Flash - dt * 2.8f);
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
            ComputeRod();
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
            if (RunDir != 0) DrawBraceZone();
            if (ShowFan && PosAt != null) DrawFan();
            if (ShowHint && PosAt != null) DrawHint();
            DrawTrail();
            foreach (var r in _rings) DrawRing(r);
            if (ShadowVisible) DrawShadow();
            if (BobberVisible) DrawBobber();
            DrawLine();
            if (_rodReady && ReelShown) PaintReel(this);
            if (ShadowVisible && RunDir != 0) DrawRunArrow();
            if (ShadowVisible && ShowStamina) DrawStamina();
            foreach (var d in _drops) DrawDrop(d);
            DrawCooler();
            if (Flash > 0.01f) DrawRect(new Rect2(Vector2.Zero, Size), new Color(1, 0.98f, 0.9f, 0.4f * Flash * Flash));
        }

        // ───────── 부채꼴 ─────────

        private void DrawFan()
        {
            const int n = 24;
            float near = 0.0f;
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
            // 이 낚싯대로는 닿지 않는 먼 물: 옅은 점선으로만 보인다
            if (far < 0.985f) DashedArc(1, new Color(1, 1, 1, 0.2f));
            if (!string.IsNullOrEmpty(FanLabel))
            {
                var lp = P(PosAt(0, far));
                var font = Fonts.Body;
                int fs = TextSize.Tiny;
                var pos = new Vector2(lp.X - 150, lp.Y - 14);
                DrawStringOutline(font, pos, FanLabel, HorizontalAlignment.Center, 300, fs, 8, new Color(Palette.NightDeep, 0.8f));
                DrawString(font, pos, FanLabel, HorizontalAlignment.Center, 300, fs, new Color(1, 0.97f, 0.88f));
            }

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

        /// <summary>질주 반대쪽 화면 절반: 금빛으로 일렁이며 "여기를 눌러" 하고 알린다. 버티는 중이면 더 밝다</summary>
        private void DrawBraceZone()
        {
            float w = Size.X / 2;
            float x0 = RunDir < 0 ? w : 0;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * (RunWarn ? 14 : 8));
            float a = Bracing ? 0.22f : RunWarn ? 0.1f + 0.08f * pulse : 0.12f + 0.1f * pulse;
            // 바깥 가장자리가 가장 밝고 가운데로 갈수록 옅다
            float outer = RunDir < 0 ? Size.X : 0;
            const int n = 14;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + w * i / n;
                float d = Mathf.Abs(x + w / n / 2 - outer) / w;
                float k = (1 - d) * (1 - d);
                DrawRect(new Rect2(x, 0, w / n + 1, Size.Y), new Color(Palette.Gold, a * (0.2f + 0.8f * k)));
            }
            // 가장자리 굵은 빛줄기
            float edge = RunDir < 0 ? Size.X - 6 * K : 0;
            DrawRect(new Rect2(edge, 0, 6 * K, Size.Y), new Color(Palette.Gold, Bracing ? 0.7f : 0.35f + 0.3f * pulse));
        }

        /// <summary>물고기 옆 화살표: 달아나는 쪽</summary>
        private void DrawRunArrow()
        {
            var c = P(ShadowAt);
            float u = K * 0.5f;
            float pulse = Mathf.Sin(_time * 16) * 4 * u;
            float dir = RunDir;
            var basePt = c + new Vector2(dir * (34 * u + pulse), -6 * u);
            var tip = basePt + new Vector2(dir * 26 * u, 0);
            var col = new Color(RunWarn ? Palette.Gold : Palette.Red, 0.95f);
            var shaft = new[]
            {
                basePt + new Vector2(0, -5 * u), tip + new Vector2(-dir * 12 * u, -5 * u),
                tip + new Vector2(-dir * 12 * u, -13 * u), tip, tip + new Vector2(-dir * 12 * u, 13 * u),
                tip + new Vector2(-dir * 12 * u, 5 * u), basePt + new Vector2(0, 5 * u),
            };
            var outline = new Vector2[shaft.Length + 1];
            shaft.CopyTo(outline, 0);
            outline[shaft.Length] = shaft[0];
            DrawColoredPolygon(shaft, col);
            DrawPolyline(outline, new Color(0.1f, 0.08f, 0.06f, 0.8f), 2 * u, true);
        }

        /// <summary>물고기 힘 막대: 몸부림 위에 떠 있다. 초록 → 노랑 → 빨강으로 줄어든다</summary>
        private void DrawStamina()
        {
            var c = P(ShadowAt) + new Vector2(0, -34 * K * 0.5f * Mathf.Clamp(ShadowSize, 0.8f, 1.6f));
            float u = K * 0.5f;
            float w = 64 * u, h = 9 * u;
            var r = new Rect2(c.X - w / 2, c.Y - h / 2, w, h);
            DrawRect(r.Grow(2 * u), new Color(0.05f, 0.08f, 0.12f, 0.75f));
            float v = Mathf.Clamp(FishStamina, 0, 1);
            var col = v > 0.5f ? Palette.Green.Lerp(Palette.Gold, (1 - v) * 2) : Palette.Gold.Lerp(Palette.Red, (0.5f - v) * 2);
            if (v > 0) DrawRect(new Rect2(r.Position, new Vector2(w * v, h)), col);
            // 다 지치면 반짝
            if (v <= 0) DrawRect(r, new Color(1, 1, 1, 0.3f + 0.3f * Mathf.Sin(_time * 10)));
            DrawRect(new Rect2(r.Position, new Vector2(w * v, h * 0.4f)), new Color(1, 1, 1, 0.25f));
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
            // 찌는 멀리 떠 있으니 작게 (사람 키의 1/10 안팎). 물테만 조금 넓게 그려 눈에 띄게 한다
            float u = K * BobberScale * 0.62f;
            float sink = BobberSink;
            float alpha = 1 - 0.6f * sink;
            if (BobberFloating)
            {
                float w = 1 + 0.12f * Mathf.Sin(_time * 3);
                Ellipse(c, 15 * u * w, 0.28f, new Color(Palette.Foam, 0.6f), 2);
            }
            if (FloatTex != null)
            {
                DrawFloatTex(c, u, sink, alpha);
                return;
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
        /// 가진 찌 그림: 날아갈 때는 통째로, 물에 뜨면 몸통 가운데까지만 (아래는 물에 잠긴다). 잠기면 더 내려간다
        /// </summary>
        private void DrawFloatTex(Vector2 c, float u, float sink, float alpha)
        {
            var tex = FloatTex;
            float texH = tex.GetHeight(), texW = tex.GetWidth();
            // 그림 높이 192 중 몸통 가운데(물에 닿는 곳)는 위에서 0.56
            const float waterAt = 0.56f;
            float scale = 30 * u / (texH * waterAt);
            float w = texW * scale;
            float shown = BobberFloating ? texH * Mathf.Lerp(waterAt, waterAt * 0.35f, sink) : texH;
            var top = c + new Vector2(-w / 2, -texH * waterAt * scale + (BobberFloating ? sink * texH * waterAt * 0.65f * scale : 0));
            DrawTextureRectRegion(tex, new Rect2(top, new Vector2(w, shown * scale)), new Rect2(0, 0, texW, shown), new Color(1, 1, 1, alpha));
            if (BobberFloating)
            {
                // 물에 닿은 자리 반짝이는 물테
                DrawSetTransform(c + new Vector2(0, 1), 0, new Vector2(1, 0.3f));
                DrawCircle(Vector2.Zero, w * 0.62f, new Color(0.17f, 0.45f, 0.6f, 0.35f));
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                Ellipse(c + new Vector2(0, 1), w * 0.62f, 0.3f, new Color(1, 1, 1, 0.6f), 2);
            }
        }

        // ───────── 낚싯대와 줄 ─────────
        //
        // 대와 줄은 그림에 넣지 않고 여기서 그린다 (그림 속 가는 대 끝은 줄이면 사라져 줄이 허공에서 나왔다).
        // 실제 찌낚시 대를 따른다:
        // - 빠른 액션: 손잡이 쪽 절반 남짓은 곧고 위쪽만 휜다. 당기면 대 끝이 줄 쪽으로 휘어 줄과 매끈하게 이어진다
        // - 가이드 고리는 대 아래(땅 쪽)에 달리고 끝으로 갈수록 작다. 원줄은 릴에서 고리들을 거쳐 끝 고리로 나간다
        // - 느슨한 줄은 대 끝에서 아래로 처지고(바람에 살랑), 물고기가 당기면 곧게 선다
        // - 던지기 전에는 대 끝에 찌 채비가 매달려 흔들린다
        // 대 몸통은 여울 그림 아래(RodView)에 그려 손이 대를 쥔 것처럼 보이고, 대 끝에서 나가는 줄은 바다 위(여기)에 그린다

        /// <summary>던지기 전: 대 끝에 찌가 매달려 흔들린다</summary>
        public bool Dangle;

        /// <summary>
        /// 릴 (스피닝 레버브레이크 릴). 대 아래에 매달리고 스풀은 대 끝(바다) 쪽을 본다.
        /// 뒤에서 보면 몸통 뒤쪽과 옆으로 나온 핸들만 보인다. ReelAt 은 포즈 그림 속 릴 자리(논리 좌표), ReelWidth 는 너비
        /// </summary>
        public bool ReelShown;
        public Vector2 ReelAt;
        public float ReelWidth;
        /// <summary>포즈를 뒤집었으면 핸들이 반대쪽</summary>
        public bool ReelFlip;
        public Color ReelBody = new Color("b8bcc2"), ReelAccent = new Color("8a5a32");
        /// <summary>
        /// 대 밑동과 가진 릴 그림 (코르크 손잡이·릴 시트·릴, 뒤에서 본 것, 대가 위로). 손잡이 자리(RodGrip)에 그림의 ButtHold 높이를 맞추고
        /// 대 방향으로 세운다. 그림 맨 위 대 몸통에서 게임이 대를 이어 그린다. ButtShown 은 포즈 그림에서 릴을 지운 포즈만
        /// </summary>
        public Texture2D ButtTex;
        public bool ButtShown;
        public float ButtTopX = 0.75f, ButtBlankPx = 31;
        /// <summary>그림 위에서 손이 쥐는 곳까지 (그림 높이 비율)</summary>
        private const float ButtHold = 0.55f;
        /// <summary>밑동 그림 맨 위 대 몸통 굵기 (화면 px, K 2.4 기준). 릴이 잘 보이게 그린 대보다 조금 굵게 시작해 이어 그린 대가 가늘어진다</summary>
        private const float ButtWidth = 9.5f;
        private float _buttScale;

        private readonly Vector2[] _rodPts = new Vector2[RodSegs + 1];
        private bool _rodReady;
        private const int RodSegs = 28;
        /// <summary>
        /// 가이드 고리 자리 (대 위 비율). 찌낚시대(1호 530)는 고리가 열 개 남짓이고 릴 앞 큰 고리에서 끝으로 갈수록 작다. 마지막은 끝 고리
        /// </summary>
        private static readonly float[] GuideAt = { 0.2f, 0.32f, 0.43f, 0.53f, 0.62f, 0.7f, 0.77f, 0.84f, 0.9f, 0.95f, 1f };
        private readonly Vector2[] _guideRing = new Vector2[GuideAt.Length];
        private Vector2 _reelAt, _reelCenter, _rodAlong, _reelSeat;
        private bool _hasButt;
        private Vector2 _gripPx, _d0;

        /// <summary>여울 그림 바로 아래에 끼울 대 그림 (ArtStage 안, 같은 좌표)</summary>
        public Control MakeRodView()
        {
            var v = new FishingRodView(this);
            v.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            return v;
        }

        /// <summary>대 모양을 계산한다 (그리기 전에 매 프레임). 끝 자리를 RodTip 에 넣어 줄이 거기서 나가게 한다</summary>
        private void ComputeRod()
        {
            _rodReady = false;
            if (!RodDrawn || _stage == null || RodLen <= 0) return;
            float b = Mathf.Clamp(Bend, 0, 1.1f);
            var g = RodGrip;
            // 줄이 가는 쪽 (없으면 대 방향)
            var t = LineTo.HasValue ? (LineTo.Value - g).Normalized() : RodDir;
            // 당길 때는 대 전체를 물고기 쪽으로 조금 돌려 잡는다 (최대 35도)
            float turn = Mathf.Clamp(RodDir.AngleTo(t), -0.61f, 0.61f);
            var d0 = RodDir.Rotated(turn * (Fighting ? 0.85f : 0.35f));
            // 길고 가는 대라 쉴 때도 제 무게로 끝이 숙인다 (화면 아래 쪽)
            var droop = (d0 + new Vector2(0, 0.42f)).Normalized();
            // 팽팽할수록 대 끝이 줄 쪽으로 휜다. 찌를 띄워 둔 채 기다릴 때도 줄 무게만큼 조금
            float k = Fighting ? 0.35f + 0.6f * Mathf.Min(1, b) : LineTo.HasValue ? 0.15f : 0;
            var tipDir = droop.Slerp(t, k);
            float shake = (Fighting && b > 0.85f ? 0.03f : 0) + 0.08f * RodKick;
            if (shake > 0) tipDir = tipDir.Rotated(Mathf.Sin(_time * 55) * shake);
            // 휠수록 끝이 손 쪽으로 당겨진다
            float len = RodLen * (1 - 0.12f * Mathf.Min(1, b));
            // 찌낚시대는 허리부터 고르게 휜다: 아래 40% 만 곧고 나머지가 활처럼
            var mid = g + d0 * len * 0.4f;
            var tip = mid + (d0 + tipDir * 1.4f).Normalized() * len * 0.6f;
            RodTip = tip;
            // 손 안쪽에서 시작한다 (여울 그림 아래라 손에 가린다). 밑동 그림이 있으면 그 맨 위에서
            var butt = g - d0 * len * 0.08f;
            bool hasButt = ButtShown && ButtTex != null;
            if (hasButt)
            {
                // 밑동 그림 맨 위 대 몸통 너비를 그린 대 굵기에 맞춘다
                _buttScale = ButtWidth * (K / 2.4f) / ButtBlankPx;
                float hs = ButtTex.GetHeight() * _buttScale / K;
                butt = g + d0 * hs * ButtHold;
                mid = butt + d0 * len * 0.36f;
                tip = mid + (d0 + tipDir * 1.4f).Normalized() * len * 0.56f;
                RodTip = tip;
            }
            _hasButt = hasButt;
            var p0 = P(butt);
            var p1 = P(mid);
            var p2 = P(tip - tipDir * len * 0.3f);
            var p3 = P(tip);
            for (int i = 0; i <= RodSegs; i++)
            {
                float s = (float)i / RodSegs;
                float r = 1 - s;
                _rodPts[i] = r * r * r * p0 + 3 * r * r * s * p1 + 3 * r * s * s * p2 + s * s * s * p3;
            }
            // 가이드 고리는 대 아래에 달린다. 뒤에서 대가 바다 쪽으로 뻗어 있으니 고리는 거의 대 위에 겹쳐 보이고
            // 화면 아래로 아주 조금만 비켜난다. 원줄은 그 고리들을 지나 대를 따라간다
            float u = K / 2.4f;
            for (int j = 0; j < GuideAt.Length; j++)
            {
                float f = GuideAt[j];
                float fi = f * RodSegs;
                int i = Mathf.Clamp((int)fi, 0, RodSegs - 1);
                var at = _rodPts[i].Lerp(_rodPts[i + 1], fi - i);
                _guideRing[j] = at + Down(_rodPts[i + 1] - _rodPts[i]) * (j == GuideAt.Length - 1 ? 0 : Mathf.Lerp(2.2f, 0.6f, f) * u);
            }
            // 릴 다리는 손 바로 위 대 아래에 붙는다. 그림 릴은 거기서 아래로 매달린다
            _reelSeat = _rodPts[2];
            _gripPx = P(g);
            _d0 = d0;
            // 원줄은 릴 스풀 앞(대 끝 쪽)에서 나와 첫 고리로 간다
            var reel = _hasButt ? P(g + d0 * ButtTex.GetHeight() * _buttScale / K * (ButtHold - 0.3f)) :
                ReelShown ? P(ReelAt) : P(g - d0 * len * 0.04f) + Down(p1 - p0) * 6 * u;
            var along = (p1 - p0).Normalized();
            _reelAt = _hasButt ? reel : reel + along * ReelWidth * K * 0.25f;
            _reelCenter = reel;
            _rodAlong = along;
            _rodReady = true;
        }

        /// <summary>대 마디 방향에 수직인 두 쪽 중 화면 아래(땅) 쪽</summary>
        private static Vector2 Down(Vector2 seg)
        {
            var n = new Vector2(-seg.Y, seg.X).Normalized();
            return n.Y >= 0 ? n : -n;
        }

        /// <summary>대 몸통, 가이드 고리, 고리를 지나는 원줄 (RodView 가 여울 그림 아래에 그린다)</summary>
        public void PaintRod(CanvasItem c)
        {
            if (!_rodReady) return;
            float u = K / 2.4f;
            var body = RodBlank;
            var gold = RodTrim;
            // 손잡이 쪽 굵고 끝은 아주 가늘다 (초릿대 0.8mm). 처음 한 토막은 손잡이처럼 굵다
            for (int i = 0; i < RodSegs; i++)
            {
                float f = (float)i / RodSegs;
                bool handle = f < 0.08f && !_hasButt;
                float w = handle ? 6.8f * u : _hasButt ? Mathf.Lerp(ButtWidth * 0.95f, 1.1f, Mathf.Pow(f, 0.7f)) * u : Mathf.Lerp(4.6f, 1.1f, Mathf.Pow((f - 0.08f) / 0.92f, 0.8f)) * u;
                var fill = handle ? RodHandle : body;
                c.DrawLine(_rodPts[i], _rodPts[i + 1], fill.Darkened(0.65f), w + 1.6f * u, true);
                c.DrawLine(_rodPts[i], _rodPts[i + 1], fill, w, true);
            }
            int joint = Mathf.Clamp((int)(0.08f * RodSegs), 1, RodSegs);
            if (!_hasButt) c.DrawCircle(_rodPts[joint], 3.6f * u, gold);
            if (_hasButt)
            {
                // 대 밑동과 릴 그림 (대 방향으로 세운다, 포즈를 뒤집으면 핸들도 반대쪽)
                float w = ButtTex.GetWidth() * _buttScale, h = ButtTex.GetHeight() * _buttScale;
                c.DrawSetTransform(_gripPx, _d0.Angle() + Mathf.Pi / 2, new Vector2(ReelFlip ? -1 : 1, 1));
                c.DrawTextureRect(ButtTex, new Rect2(-ButtTopX * w, -ButtHold * h, w, h), false);
                c.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            }
            // 위쪽(빛 받는 쪽) 가는 빛 한 줄
            for (int i = 3; i < RodSegs - 3; i++)
            {
                var d = _rodPts[i + 1] - _rodPts[i];
                var off = -Down(d) * Mathf.Lerp(1.3f, 0.3f, (float)i / RodSegs) * u;
                c.DrawLine(_rodPts[i] + off, _rodPts[i + 1] + off, new Color(1, 0.95f, 0.85f, 0.24f), 0.9f * u, true);
            }
            // 진출식 마디 이음 (장식 색 띠)
            foreach (float f in new[] { 0.28f, 0.47f, 0.66f, 0.82f })
            {
                int i = Mathf.Clamp((int)(f * RodSegs), 0, RodSegs - 1);
                var d = (_rodPts[i + 1] - _rodPts[i]).Normalized();
                var nrm = new Vector2(-d.Y, d.X) * Mathf.Lerp(2.8f, 1.2f, f) * u;
                c.DrawLine(_rodPts[i] - nrm, _rodPts[i] + nrm, gold, 1.6f * u, true);
            }
            // 원줄: 릴 스풀 앞에서 고리들을 차례로 지나 끝 고리까지 (대를 따라 붙어 간다)
            var line = new Vector2[GuideAt.Length + 1];
            line[0] = _reelAt;
            for (int j = 0; j < GuideAt.Length; j++) line[j + 1] = _guideRing[j];
            c.DrawPolyline(line, LineColor(0.9f), 1f * Mathf.Max(1, u), true);
            // 고리: 뒤에서 보면 대를 가로지르는 작은 금속 고리 (릴 앞 첫 고리가 가장 크다)
            for (int j = 0; j < GuideAt.Length; j++)
            {
                float f = GuideAt[j];
                float r = (j == 0 ? 3.4f : Mathf.Lerp(2.4f, 1.1f, f)) * u;
                var at = _guideRing[j];
                var d = j < GuideAt.Length - 1 ? (_guideRing[j + 1] - at).Normalized() : _rodAlong;
                var nrm = new Vector2(-d.Y, d.X);
                // 납작한 고리 (대 방향으로 눌린 타원)
                var pts = new Vector2[13];
                for (int q = 0; q < pts.Length; q++)
                {
                    float a = Mathf.Tau * q / (pts.Length - 1);
                    pts[q] = at + nrm * Mathf.Cos(a) * r + d * Mathf.Sin(a) * r * 0.35f;
                }
                c.DrawPolyline(pts, new Color("d8dce0"), 1f * u, true);
            }
        }

        /// <summary>
        /// 릴: 뒤에서 본 스피닝 릴. 대 아래 릴 다리에 매달린 둥근 몸통, 옆으로 뻗은 핸들과 노브.
        /// 스풀은 대 끝 쪽을 보니 위쪽에 테만 살짝 보인다. 여울 그림 위에 그려 그림 속 릴을 덮는다
        /// </summary>
        public void PaintReel(CanvasItem c)
        {
            if (!_rodReady || !ReelShown) return;
            float u = K / 2.4f;
            float w = ReelWidth * K;
            var at = _reelCenter;
            var up = _rodAlong;                 // 대 끝 쪽
            var side = new Vector2(-up.Y, up.X); // 화면 오른쪽 쪽
            if (side.X < 0) side = -side;
            var ink = ReelBody.Darkened(0.6f);
            // 릴 다리: 몸통에서 대로
            var foot = at + up * w * 0.05f - side * 0 + (-Down(up)) * w * 0.32f;
            c.DrawLine(at, foot, ink, 2.6f * u, true);
            c.DrawLine(at, foot, ReelBody, 1.4f * u, true);
            // 핸들: 몸통 옆으로 (오른손잡이는 왼쪽 핸들, 포즈를 뒤집으면 반대)
            var hand = ReelFlip ? side : -side;
            var knob = at + hand * w * 0.62f + Down(up) * w * 0.12f;
            c.DrawLine(at, knob, ink, 2.6f * u, true);
            c.DrawLine(at, knob, ReelBody, 1.4f * u, true);
            c.DrawCircle(knob, w * 0.13f + 0.6f * u, ink);
            c.DrawCircle(knob, w * 0.13f, ReelAccent);
            // 스풀 테 (몸통 위, 대 끝 쪽에 살짝)
            c.DrawCircle(at + up * w * 0.16f, w * 0.3f + 0.6f * u, ink);
            c.DrawCircle(at + up * w * 0.16f, w * 0.3f, ReelAccent);
            // 몸통 뒤쪽 (둥근 기어 통)
            c.DrawCircle(at, w * 0.32f + 0.8f * u, ink);
            c.DrawCircle(at, w * 0.32f, ReelBody);
            c.DrawCircle(at - side * w * 0.1f - up * w * 0.08f, w * 0.12f, ReelBody.Lightened(0.35f));
        }

        /// <summary>원줄 색 (형광빛 도는 흰 나일론). 위험하면 금빛을 지나 빨갛게</summary>
        private Color LineColor(float alpha = 0.92f)
        {
            // 찌낚시 원줄은 잘 보이게 형광 노랑을 많이 쓴다
            var white = new Color(1, 0.98f, 0.62f, alpha);
            if (!Fighting) return white;
            float bend = Mathf.Clamp(Bend, 0, 1.1f);
            var hot = new Color(1f, 0.82f, 0.36f, 1f);
            var danger = new Color(1f, 0.35f, 0.3f, 1f);
            return bend < 0.85f ? white.Lerp(hot, Mathf.Clamp((bend - 0.6f) / 0.25f, 0, 1)) : hot.Lerp(danger, Mathf.Clamp((bend - 0.85f) / 0.15f, 0, 1));
        }

        /// <summary>
        /// 대 끝에서 나가는 줄. 느슨하면 아래로 처져(바람에 살랑) 찌까지 늘어지고, 팽팽할수록 곧게 선다.
        /// 릴링 중(Fighting) 위험 가까이 가면 줄이 파르르 떨리고 금빛을 지나 빨갛게 달아오른다.
        /// 던지기 전에는 대 끝에 찌 채비가 매달려 흔들린다
        /// </summary>
        private void DrawLine()
        {
            float u = K / 2.4f;
            var tip = P(RodTip);
            if (LineTo == null)
            {
                if (Dangle && _rodReady) DrawDangle(tip, u);
                return;
            }
            var to = P(LineTo.Value);
            float bend = Mathf.Clamp(Bend, 0, 1.1f);
            float slack = Fighting ? Mathf.Clamp(1 - bend / 0.55f, 0, 1) : 1;
            float len = (to - tip).Length();
            // 처짐 깊이: 느슨하면 줄 길이의 1할쯤, 팽팽하면 거의 없다
            float sag = (len * 0.1f + 8 * u) * slack * slack + 1.5f * u;
            float sway = Mathf.Sin(_time * 1.7f) * len * 0.025f * slack;
            const int n = 32;
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float hump = 4 * t * (1 - t);
                pts[i] = tip.Lerp(to, t) + new Vector2(sway * hump, sag * hump);
            }
            // 팽팽하면 줄이 파르르 떨린다 (가운데가 가장 크게)
            float shiver = Fighting ? Mathf.Max(Mathf.Clamp((bend - 0.62f) / 0.3f, 0, 1), 0.6f * RodKick) : 0;
            if (shiver > 0)
            {
                var dir = (to - tip).Normalized();
                var nrm = new Vector2(-dir.Y, dir.X);
                for (int i = 1; i < n; i++)
                {
                    float t = (float)i / n;
                    pts[i] += nrm * Mathf.Sin(_time * 70 + t * 9) * Mathf.Sin(t * Mathf.Pi) * 3.2f * shiver * u;
                }
            }
            float width = (Fighting ? 2.2f + 1.2f * Mathf.Clamp(bend, 0, 1) : 1.8f) * Mathf.Max(1, u);
            // 밝은 물 위에서도 보이게 어두운 테두리를 먼저
            DrawPolyline(pts, new Color(0.05f, 0.12f, 0.18f, 0.32f), width + 2.2f * u, true);
            DrawPolyline(pts, LineColor(), width, true);
            if (Fighting && bend >= 0.85f) DrawPolyline(pts, new Color(1f, 0.35f, 0.3f, 0.25f + 0.15f * Mathf.Sin(_time * 18)), width + 6 * u, true);
        }

        /// <summary>던지기 전: 대 끝에서 찌 채비가 곧게 매달려 천천히 흔들린다</summary>
        private void DrawDangle(Vector2 tip, float u)
        {
            float swing = Mathf.Sin(_time * 1.9f) * 0.12f + Mathf.Sin(_time * 3.1f) * 0.04f;
            var end = tip + new Vector2(Mathf.Sin(swing), Mathf.Cos(swing)) * 20 * K;
            DrawLine(tip, end, new Color(0.05f, 0.12f, 0.18f, 0.32f), 3.6f * u, true);
            DrawLine(tip, end, LineColor(), 1.6f * Mathf.Max(1, u), true);
            // 찌 (가진 찌 그림이 있으면 그 그림, 작게)
            var floatAt = tip.Lerp(end, 0.55f);
            if (FloatTex != null)
            {
                float texH = FloatTex.GetHeight(), texW = FloatTex.GetWidth();
                float h = 9 * K;
                float w = texW * h / texH;
                DrawTextureRect(FloatTex, new Rect2(floatAt - new Vector2(w / 2, h * 0.5f), new Vector2(w, h)), false);
            }
            else
            {
                DrawCircle(floatAt, 1.3f * K, Colors.White);
                DrawCircle(floatAt + new Vector2(0, -1.2f * K), 1.2f * K, Palette.Red);
            }
            // 봉돌
            DrawCircle(end, 1.6f * u * 2, new Color("4a4a4a"));
        }

        // ───────── 쿨러 ─────────

        /// <summary>발밑 쿨러: 둥근 파란 몸통에 흰 뚜껑과 손잡이. 고등어가 노리면 뚜껑이 들썩인다</summary>
        private void DrawCooler()
        {
            if (CoolerAt == Vector2.Zero) return;
            var c = P(CoolerAt);
            float u = K * 1.1f;
            if (CoolerTex != null)
            {
                DrawCoolerTex(c, u);
                return;
            }
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

        /// <summary>
        /// 가진 쿨러 그림 (상점 그림 그대로). 그림 여백은 잘라 두었다. 고등어가 노리면 뚜껑 쪽이 들썩이듯 흔들린다
        /// </summary>
        private void DrawCoolerTex(Vector2 c, float u)
        {
            var tex = CoolerTex;
            float h = 40 * K;
            float w = tex.GetWidth() * h / tex.GetHeight();
            // 바닥 그림자
            DrawSetTransform(c + new Vector2(2 * u, 0), 0, new Vector2(1, 0.25f));
            DrawCircle(Vector2.Zero, w * 0.55f, new Color(0, 0, 0, 0.24f));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            float jiggle = CoolerLid > 0 ? Mathf.Sin(_time * 40) * 0.05f * CoolerLid : 0;
            DrawSetTransform(c, jiggle, Vector2.One);
            DrawTextureRect(tex, new Rect2(-w / 2, -h + CoolerLid * -3 * u, w, h), false);
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

using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 방파제 바다 위에 그리는 것들: 던질 방향 띠, 고등어가 가리키는 물결, 찌, 물고기 그림자, 물보라, 낚싯대 끝과 낚싯줄.
    /// 좌표는 ArtStage 논리 좌표(450×800)로 받아 그릴 때 화면 픽셀로 바꾼다. 상태는 FishingScreen 이 정한다.
    /// </summary>
    public partial class FishingField : Control
    {
        private sealed class Splash
        {
            public Vector2 At;
            public float Scale;
            public float T;
            public Vector2[] Drops;
        }

        private readonly ArtStage _stage;
        private readonly List<Splash> _splashes = new List<Splash>();
        private readonly Random _rand = new Random(7);
        private float _time;

        // 방향 띠 (-1 왼쪽, 0 앞, 1 오른쪽). Selected 가 null 이면 셋 다 옅게
        public bool ShowLanes;
        public int? SelectedLane;
        public Func<int, float, Vector3> LanePos;

        // 고등어가 가리키는 물결
        public bool ShowHint;
        public int HintLane;

        // 찌
        public bool BobberVisible;
        public Vector2 BobberAt;
        public float BobberScale = 1;
        /// <summary>물에 떠 있으면 true (날아가는 중에는 물결을 그리지 않는다)</summary>
        public bool BobberFloating;
        /// <summary>1이면 찌가 물속으로 들어갔다</summary>
        public float BobberSink;

        // 물고기 그림자
        public bool ShadowVisible;
        public Vector2 ShadowAt;
        public float ShadowSize = 1;
        public float ShadowWobble;

        // 낚싯대와 줄
        public Vector2 Hand;
        public Vector2 TipIdle;
        /// <summary>텐션 0~1. 낚싯대가 줄 쪽으로 휘고 줄이 팽팽해진다</summary>
        public float Bend;
        public Vector2? LineTo;

        public FishingField() { }

        public FishingField(ArtStage stage)
        {
            _stage = stage;
            MouseFilter = MouseFilterEnum.Stop;
        }

        /// <summary>찌가 떨어지거나 고기가 차고 나갈 때 물보라</summary>
        public void AddSplash(Vector2 at, float scale)
        {
            var drops = new Vector2[7];
            for (int i = 0; i < drops.Length; i++)
            {
                float a = Mathf.Pi * (1.1f + 0.8f * (float)_rand.NextDouble());
                drops[i] = new Vector2(Mathf.Cos(a) * (14 + 16 * (float)_rand.NextDouble()), Mathf.Sin(a) * (26 + 18 * (float)_rand.NextDouble()));
            }
            _splashes.Add(new Splash { At = at, Scale = scale, T = 0, Drops = drops });
        }

        /// <summary>낚싯대 끝. 텐션이 걸리면 줄 쪽으로 끌려가며 아래로 휜다</summary>
        public Vector2 Tip()
        {
            if (LineTo == null || Bend <= 0) return TipIdle;
            var to = LineTo.Value;
            return new Vector2(Mathf.Lerp(TipIdle.X, to.X, Bend * 0.22f), Mathf.Lerp(TipIdle.Y, to.Y, Bend * 0.12f) + Bend * 22);
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _time += dt;
            for (int i = _splashes.Count - 1; i >= 0; i--)
            {
                _splashes[i].T += dt / 0.7f;
                if (_splashes[i].T >= 1) _splashes.RemoveAt(i);
            }
            QueueRedraw();
        }

        private Vector2 P(Vector2 logical)
        {
            return _stage.ToLocal(logical);
        }

        private float K
        {
            get { return _stage.K; }
        }

        public override void _Draw()
        {
            if (_stage == null) return;
            if (ShowLanes && LanePos != null) DrawLanes();
            if (ShowHint && LanePos != null) DrawHint();
            foreach (var s in _splashes) DrawSplash(s);
            if (ShadowVisible) DrawShadow();
            if (BobberVisible) DrawBobber();
            DrawRodAndLine();
        }

        private void DrawLanes()
        {
            for (int lane = -1; lane <= 1; lane++)
            {
                var near = LanePos(lane, 0.05f);
                var far = LanePos(lane, 0.95f);
                bool on = SelectedLane == lane;
                float a = SelectedLane == null ? 0.12f + 0.04f * Mathf.Sin(_time * 2.4f + lane) : on ? 0.24f : 0.04f;
                var pts = new[] { P(new Vector2(near.X - 40, near.Y)), P(new Vector2(near.X + 40, near.Y)), P(new Vector2(far.X, far.Y)) };
                DrawColoredPolygon(pts, new Color(1, 1, 1, a));
            }
        }

        private void DrawHint()
        {
            var p = LanePos(HintLane, 0.55f);
            var c = P(new Vector2(p.X, p.Y));
            for (int i = 0; i < 2; i++)
            {
                float ph = (_time / 1.2f + i * 0.5f) % 1f;
                Ellipse(c, 15 * (1 + 1.3f * ph) * K, 0.32f, new Color(Palette.Foam, 0.9f * (1 - ph)), 2.5f);
            }
        }

        private void DrawSplash(Splash s)
        {
            var c = P(s.At);
            float t = s.T;
            float ease = 1 - (1 - t) * (1 - t);
            float u = K * s.Scale;
            Ellipse(c, (10 + 26 * ease) * u, 0.3f, new Color(Palette.Foam, 1 - t), 3);
            Ellipse(c, (4 + 14 * ease) * u, 0.3f, new Color(Palette.Foam, 0.7f * (1 - t)), 2);
            // 튀어 오른 물방울은 포물선으로 떨어진다
            foreach (var d in s.Drops)
            {
                var pos = c + new Vector2(d.X * t, d.Y * t + 60 * t * t) * u * 0.5f;
                DrawCircle(pos, Mathf.Max(1.5f, 2.6f * u * 0.5f * (1 - t)), new Color(Palette.Foam, 1 - t));
            }
        }

        private void DrawShadow()
        {
            var c = P(ShadowAt);
            float rx = 17 * ShadowSize * K;
            float sway = Mathf.Sin(ShadowWobble * 1.7f) * 0.25f;
            // 몸통과 꼬리 (꼬리는 몸통 반대로 흔들린다)
            DrawSetTransform(c, sway, new Vector2(1, 0.34f));
            DrawCircle(Vector2.Zero, rx, new Color(0.04f, 0.1f, 0.16f, 0.5f));
            DrawCircle(new Vector2(rx * 0.95f, Mathf.Sin(ShadowWobble * 3) * rx * 0.25f), rx * 0.42f, new Color(0.04f, 0.1f, 0.16f, 0.42f));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            Ellipse(c, rx * 1.35f, 0.34f, new Color(Palette.Foam, 0.16f + 0.08f * Mathf.Sin(_time * 6)), 2);
        }

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

        private void DrawRodAndLine()
        {
            var hand = P(Hand);
            var tip = P(Tip());
            // 낚싯대: 손에서 끝까지 휘어진 곡선. 텐션이 걸릴수록 많이 휜다
            var mid = (hand + tip) / 2;
            var dir = (tip - hand).Normalized();
            var normal = new Vector2(dir.Y, -dir.X);
            var ctrl = mid + normal * (6 + Bend * 46) * (K / 2.4f);
            var rod = Bezier(hand, ctrl, tip, 20);
            DrawPolyline(rod, new Color("2a1e17"), 7, true);
            DrawPolyline(rod, new Color("6b4a35"), 3, true);
            DrawCircle(hand + dir * 18, 7, new Color("2a1e17"));

            if (LineTo == null) return;
            var to = P(LineTo.Value);
            // 낚싯줄: 느슨하면 아래로 처진다
            var lm = (tip + to) / 2 + new Vector2(0, (1 - Bend) * 34 * (K / 2.4f));
            DrawPolyline(Bezier(tip, lm, to, 18), new Color(1, 1, 1, 0.85f), 2.2f, true);
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

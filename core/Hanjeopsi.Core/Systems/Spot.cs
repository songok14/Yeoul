using System;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>바다 위 한 지점. 방향 -1(왼쪽)~1(오른쪽), 거리 0(발밑)~1(수평선)</summary>
    public struct CastSpot
    {
        public double Angle;
        public double Dist;

        public CastSpot(double angle, double dist)
        {
            Angle = angle;
            Dist = dist;
        }
    }

    /// <summary>
    /// 고닷 낚시 규칙 (웹에는 없다). 부채꼴 위를 오가는 방향 바늘, 고등어가 가리키는 지점,
    /// 지점에 가까운 정도에 따른 귀한 고기·큰 놈 확률, 쿨러를 노리는 고등어.
    /// 입질까지 기다리는 시간은 어디에 던지든 같다 (ReelRules.StartWait 에 hot 을 넘기지 않는다).
    /// </summary>
    public sealed class SpotRules
    {
        private readonly GameData _data;
        private SpotBalance B { get { return _data.Balance.Spot; } }

        public SpotRules(GameData data)
        {
            _data = data;
        }

        /// <summary>방향 바늘 한 프레임. 양 끝에 닿으면 돌아선다</summary>
        public (double Angle, int Dir) StepAim(double angle, int dir, double dt)
        {
            angle += dir * B.AimSpeed * dt;
            if (angle >= 1) return (1, -1);
            if (angle <= -1) return (-1, 1);
            return (angle, dir);
        }

        /// <summary>
        /// 물 좋은 지점(Hot)과 고등어가 가리키는 지점(Hint). 호감이 높을수록 제대로 가리킨다.
        /// 지점은 낚싯대가 닿는 거리 안에서만 고른다
        /// </summary>
        public (CastSpot Hot, CastSpot Hint) RollSpots(int affinity, double reach, IRandom rng)
        {
            var hot = RandomSpot(reach, rng);
            var c = _data.Balance.Cat;
            double accuracy = Math.Min(c.HintMax, c.HintBase + affinity * c.HintPerAffinity);
            if (rng.Next() < accuracy) return (hot, hot);
            // 헛짚으면 물 좋은 곳과 떨어진 엉뚱한 지점을 가리킨다
            var hint = RandomSpot(reach, rng);
            for (int i = 0; i < 8 && Closeness(hint, hot) > 0; i++) hint = RandomSpot(reach, rng);
            return (hot, hint);
        }

        private CastSpot RandomSpot(double reach, IRandom rng)
        {
            double angle = rng.Range(-B.HotAngleMax, B.HotAngleMax);
            double dist = rng.Range(B.HotDistMin, B.HotDistMax) * reach;
            return new CastSpot(angle, dist);
        }

        /// <summary>찌가 떨어진 자리가 지점에 얼마나 가까운지 (1 바로 위, 0 효과 없음)</summary>
        public double Closeness(CastSpot at, CastSpot spot)
        {
            double da = (at.Angle - spot.Angle) / B.AngleRange;
            double dd = (at.Dist - spot.Dist) / B.DistRange;
            return JsMath.Clamp(1 - Math.Sqrt(da * da + dd * dd), 0, 1);
        }

        /// <summary>큰 놈 입질인가. 지점에 가까울수록 잘 온다</summary>
        public bool IsBigBite(double closeness, IRandom rng)
        {
            return rng.Next() < B.BigBase + B.BigPerClose * closeness;
        }

        /// <summary>이번에 고등어가 쿨러를 노리면 찌가 떨어지고 몇 초 뒤인지. 안 노리면 -1</summary>
        public double RollThief(int coolerCount, IRandom rng)
        {
            if (coolerCount <= 0) return -1;
            if (rng.Next() >= B.ThiefChance) return -1;
            return rng.Range(B.ThiefDelayMin, B.ThiefDelayMax);
        }

        public double ThiefWindow
        {
            get { return B.ThiefWindow; }
        }

        /// <summary>힘싸움 시작 (고닷 릴링)</summary>
        public TugFight StartTug(FightSetup setup, IRandom rng)
        {
            return new TugFight(_data.Balance.Reel, _data.Balance.Tug, setup, rng);
        }

        /// <summary>
        /// 손맛 세기 0.15~1 (진동 세기). 작고 흔한 고기는 약하게, 크고 귀하고 힘센 고기와 큰 놈은 세게
        /// </summary>
        public double Feel(Species sp, double size, bool big)
        {
            double s = sp.MaxSize > sp.MinSize ? JsMath.Clamp((size - sp.MinSize) / (sp.MaxSize - sp.MinSize), 0, 1) : 0.5;
            double f = 0.18 + 0.16 * (sp.Strength - 1) / 4.0 + 0.3 * s + (sp.Rare ? 0.14 : 0) + (big ? 0.2 : 0);
            return JsMath.Clamp(f, 0.15, 1);
        }
    }
}

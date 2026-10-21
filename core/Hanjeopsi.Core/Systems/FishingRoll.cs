using System;
using System.Collections.Generic;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>캐스팅 거리 구간, 입질 어종과 크기, 고등어 힌트 (웹 systems/fishing.ts)</summary>
    public sealed class FishingRoll
    {
        public const int Near = 0;
        public const int Mid = 1;
        public const int Far = 2;

        private readonly GameData _data;
        private FishingBalance B { get { return _data.Balance.Fishing; } }

        public FishingRoll(GameData data)
        {
            _data = data;
        }

        /// <summary>던진 거리(0~1) → 근거리 0, 중거리 1, 원거리 2</summary>
        public int ZoneOf(double dist01)
        {
            if (dist01 < B.NearBelow) return Near;
            if (dist01 < B.MidBelow) return Mid;
            return Far;
        }

        /// <summary>거리 구간 × 시간대 가중치로 어종을 뽑는다. 물 좋은 곳(hot)이면 귀한 고기가 잘 나온다</summary>
        public Species RollSpecies(int zone, int slot, bool hot, IRandom rng)
        {
            var list = _data.Species;
            var weights = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                double w = list[i].Zone[zone] * list[i].Slot[slot];
                if (list[i].Rare) w *= hot ? B.RareHotMul : B.RareColdMul;
                weights[i] = w;
            }
            return WeightedPick(list, weights, rng.Next());
        }

        /// <summary>작은 쪽으로 치우친 크기. 물 좋은 곳이면 조금 더 크다. 0.1cm 단위</summary>
        public double RollSize(Species sp, bool hot, IRandom rng)
        {
            double r = Math.Pow(rng.Next(), B.SizePow);
            if (hot) r = Math.Pow(r, B.HotSizePow);
            return JsMath.Round((sp.MinSize + (sp.MaxSize - sp.MinSize) * r) * 10) / 10;
        }

        /// <summary>고등어가 물 좋은 곳을 제대로 가리킬 확률. 호감 1마다 3.5%씩, 최대 95%</summary>
        public double CatHintAccuracy(int affinity)
        {
            var c = _data.Balance.Cat;
            return Math.Min(c.HintMax, c.HintBase + affinity * c.HintPerAffinity);
        }

        /// <summary>utils.ts weightedPick 과 같은 방식 (가중치를 빼 나가다 0 이하가 되는 곳)</summary>
        public static T WeightedPick<T>(IList<T> items, IList<double> weights, double r)
        {
            double total = 0;
            foreach (var w in weights) total += w;
            double x = r * total;
            for (int i = 0; i < items.Count; i++)
            {
                x -= weights[i];
                if (x <= 0) return items[i];
            }
            return items[items.Count - 1];
        }
    }
}

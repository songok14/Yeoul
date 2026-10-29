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
                double w = list[i].Zone[zone] * SlotWeight(list[i], slot);
                if (list[i].Rare) w *= hot ? B.RareHotMul : B.RareColdMul;
                weights[i] = w;
            }
            return WeightedPick(list, weights, rng.Next());
        }

        /// <summary>하루 칸(slot)의 어종 시간대 가중치. 고닷은 아침 칸이 새벽·아침 가중치의 평균이다 (time.speciesSlots)</summary>
        public double SlotWeight(Species sp, int slot)
        {
            var map = _data.Balance.Time.SpeciesSlots;
            if (map == null || slot >= map.Count || map[slot].Count == 0) return sp.Slot[Math.Min(slot, sp.Slot.Length - 1)];
            double sum = 0;
            foreach (int c in map[slot]) sum += sp.Slot[Math.Min(c, sp.Slot.Length - 1)];
            return sum / map[slot].Count;
        }

        /// <summary>작은 쪽으로 치우친 크기. 물 좋은 곳이면 조금 더 크다. 0.1cm 단위</summary>
        public double RollSize(Species sp, bool hot, IRandom rng)
        {
            double r = Math.Pow(rng.Next(), B.SizePow);
            if (hot) r = Math.Pow(r, B.HotSizePow);
            return JsMath.Round((sp.MinSize + (sp.MaxSize - sp.MinSize) * r) * 10) / 10;
        }

        /// <summary>
        /// 고닷 낚시: 이 바다(sea)의 이 낚시터(spot)에 살고 이 달(month 1~12)이 제철일수록, 거리와 시간대가 맞을수록 잘 문다.
        /// 지점에 가까운 정도(0~1)만큼 귀한 고기 가중치가 RareColdMul 에서 RareHotMul 로 오른다
        /// </summary>
        public Species RollSpeciesNear(int zone, int slot, int month, string sea, double closeness, IRandom rng, Func<Species, double> bias = null, string spot = null)
        {
            var list = _data.Species;
            var weights = new double[list.Count];
            double rareMul = B.RareColdMul + (B.RareHotMul - B.RareColdMul) * closeness;
            for (int i = 0; i < list.Count; i++)
            {
                if (!LivesIn(list[i], sea, spot))
                {
                    weights[i] = 0;
                    continue;
                }
                double w = list[i].Zone[zone] * SlotWeight(list[i], slot) * MonthWeight(list[i], month);
                if (list[i].Rare) w *= rareMul;
                if (bias != null) w *= bias(list[i]);
                weights[i] = w;
            }
            return WeightedPick(list, weights, rng.Next());
        }

        /// <summary>이 바다에 살고 이 낚시터에서 낚이는지 (seas, spots 가 없으면 어디든)</summary>
        public static bool LivesIn(Species sp, string sea, string spot = null)
        {
            bool inSea = sp.Seas == null || sea == null || Array.IndexOf(sp.Seas, sea) >= 0;
            bool atSpot = sp.Spots == null || spot == null || Array.IndexOf(sp.Spots, spot) >= 0;
            return inSea && atSpot;
        }

        /// <summary>달 가중치 (months 가 없으면 1)</summary>
        public static double MonthWeight(Species sp, int month)
        {
            return sp.Months == null || month < 1 || month > 12 ? 1 : sp.Months[month - 1];
        }

        /// <summary>고닷 낚시: 가까울수록 조금 크고, 큰 놈이면 크기 범위 윗부분(BigSizeFrom~1)에서 나온다</summary>
        public double RollSizeNear(Species sp, double closeness, bool big, IRandom rng)
        {
            double r = Math.Pow(rng.Next(), B.SizePow);
            r = Math.Pow(r, 1 + (B.HotSizePow - 1) * closeness);
            if (big)
            {
                double from = _data.Balance.Spot.BigSizeFrom;
                r = from + (1 - from) * Math.Max(r, rng.Next());
            }
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

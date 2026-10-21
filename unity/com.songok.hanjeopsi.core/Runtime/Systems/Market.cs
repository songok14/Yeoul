using System;
using System.Collections.Generic;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>
    /// 위판장 시세와 물고기 값 (웹 systems/economy.ts).
    /// 시세는 세이브 시드로 정해지는 랜덤 워크라 같은 시드면 웹과 같은 흐름이 나온다.
    /// </summary>
    public sealed class Market
    {
        private readonly GameData _data;
        private readonly Freshness _fresh;
        private readonly int _seed;
        /// <summary>어종별 날짜별 배율 캐시. 웹은 매번 1일차부터 다시 계산한다(결과는 같다)</summary>
        private readonly List<List<double>> _mulCache = new List<List<double>>();

        private MarketBalance B { get { return _data.Balance.Market; } }

        public int Seed { get { return _seed; } }

        public Market(GameData data, Freshness freshness, int seed)
        {
            _data = data;
            _fresh = freshness;
            _seed = seed;
            for (int i = 0; i < data.Species.Count; i++) _mulCache.Add(new List<double> { 1.0 });
        }

        /// <summary>날짜별 시세 배율. 전날 값에서 조금씩 움직이고 가끔 풍어나 품귀가 터진다</summary>
        public double PriceMul(string speciesId, int day)
        {
            int idx = _data.SpeciesIndex(speciesId);
            var cache = _mulCache[idx];
            // cache[d] = d일차 배율. cache[0] = 1
            for (int d = cache.Count; d <= day; d++)
            {
                double m = cache[d - 1];
                double r = Mulberry32.At(_seed, d, idx);
                m = JsMath.Clamp(m + (r - 0.5) * B.DailySwing, B.MinMul, B.MaxMul);
                double shock = Mulberry32.At(_seed, d, idx, 7);
                if (shock < B.GlutBelow) m = JsMath.Clamp(m * B.GlutMul, B.MinMul, B.MaxMul);
                else if (shock > B.ShortageAbove) m = JsMath.Clamp(m * B.ShortageMul, B.MinMul, B.MaxMul);
                cache.Add(m);
            }
            return cache[Math.Max(0, day)];
        }

        public double SizeFactor(string speciesId, double size)
        {
            return Math.Pow(size / _data.SpeciesById(speciesId).AvgSize, B.SizeExponent);
        }

        /// <summary>평균 크기 활어 기준 오늘 시세</summary>
        public int TodayBasePrice(string speciesId, int day)
        {
            return JsMath.Round100(_data.SpeciesById(speciesId).BasePrice * PriceMul(speciesId, day));
        }

        /// <summary>값 = 기준가 × 크기 배율 × 오늘 시세 × 신선도 배율 (곱하는 순서도 웹과 같게)</summary>
        public int FishValue(FishItem item, int day)
        {
            var stage = _fresh.StageOf(item.Fresh);
            return JsMath.Round100(
                _data.SpeciesById(item.Species).BasePrice
                * SizeFactor(item.Species, item.Size)
                * PriceMul(item.Species, day)
                * _fresh.PriceFactor(item.Species, stage));
        }

        /// <summary>전날 대비 등락(%)</summary>
        public int ChangePercent(string speciesId, int day)
        {
            if (day <= 1) return 0;
            double today = PriceMul(speciesId, day);
            double prev = PriceMul(speciesId, day - 1);
            return JsMath.RoundToInt((today - prev) / prev * 100);
        }

        /// <summary>경매 낙찰가. 묶음 값을 더해 낙찰 배율(0.92~1.15)을 곱하고 100원 단위로 맞춘다</summary>
        public int AuctionTotal(IEnumerable<FishItem> items, int day, double mul)
        {
            double total = 0;
            foreach (var f in items) total += FishValue(f, day);
            return (int)(JsMath.Round(total * mul / 100) * 100);
        }

        /// <summary>오늘의 위판장 소식 한 줄. 가장 크게 움직인 어종을 고른다</summary>
        public string Headline(int day)
        {
            var t = _data.Texts.Market;
            if (day <= 1) return t.FirstDay;
            Species best = _data.Species[0];
            int bestAbs = -1;
            foreach (var sp in _data.Species)
            {
                int c = Math.Abs(ChangePercent(sp.Id, day));
                if (c > bestAbs)
                {
                    bestAbs = c;
                    best = sp;
                }
            }
            int change = ChangePercent(best.Id, day);
            if (Math.Abs(change) < 5) return t.Calm;
            return change > 0 ? string.Format(t.Up, best.Name, change) : string.Format(t.Down, best.Name, -change);
        }
    }
}

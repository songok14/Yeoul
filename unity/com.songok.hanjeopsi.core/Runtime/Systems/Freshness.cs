using System;
using System.Collections.Generic;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>활어 → 선어 → 숙성 → 상함</summary>
    public enum FreshStage
    {
        Live,
        Fresh,
        Aged,
        Spoiled,
    }

    /// <summary>신선도 단계와 배율, 날이 바뀔 때 감소 (웹 systems/economy.ts, time.ts endDay)</summary>
    public sealed class Freshness
    {
        private readonly GameData _data;
        private FreshBalance B { get { return _data.Balance.Fresh; } }

        public Freshness(GameData data)
        {
            _data = data;
        }

        public FreshStage StageOf(int fresh)
        {
            if (fresh > B.LiveAbove) return FreshStage.Live;
            if (fresh > B.FreshAbove) return FreshStage.Fresh;
            if (fresh > B.AgedAbove) return FreshStage.Aged;
            return FreshStage.Spoiled;
        }

        /// <summary>경매 값 배율. 숙성 어종은 숙성 단계에서 오히려 값이 오른다</summary>
        public double PriceFactor(string speciesId, FreshStage stage)
        {
            switch (stage)
            {
                case FreshStage.Live: return B.LiveFactor;
                case FreshStage.Fresh: return B.FreshFactor;
                case FreshStage.Aged: return _data.SpeciesById(speciesId).Aging ? B.AgedFactorAging : B.AgedFactor;
                default: return 0;
            }
        }

        public string StageName(FreshStage stage)
        {
            return _data.Texts.StageNames[Key(stage)];
        }

        /// <summary>JSON과 세이브에 쓰는 이름 (live, fresh, aged, spoiled)</summary>
        public static string Key(FreshStage stage)
        {
            switch (stage)
            {
                case FreshStage.Live: return "live";
                case FreshStage.Fresh: return "fresh";
                case FreshStage.Aged: return "aged";
                default: return "spoiled";
            }
        }

        public static FreshStage ParseKey(string key)
        {
            switch (key)
            {
                case "live": return FreshStage.Live;
                case "fresh": return FreshStage.Fresh;
                case "aged": return FreshStage.Aged;
                case "spoiled": return FreshStage.Spoiled;
                default: throw new ArgumentException("없는 신선도 단계: " + key);
            }
        }

        /// <summary>쿨러 소품의 하루 감소량</summary>
        public int DailyDecay(int tackle)
        {
            return _data.Gear.Tackles[tackle].Decay;
        }

        /// <summary>
        /// 날이 바뀔 때 쿨러 전체 신선도를 깎는다. 이번에 새로 상한 마릿수를 돌려준다.
        /// </summary>
        public int ApplyDailyDecay(IList<FishItem> cooler, int tackle)
        {
            int decay = DailyDecay(tackle);
            int spoiled = 0;
            foreach (var f in cooler)
            {
                bool wasOk = StageOf(f.Fresh) != FreshStage.Spoiled;
                f.Fresh = Math.Max(0, f.Fresh - decay);
                if (wasOk && StageOf(f.Fresh) == FreshStage.Spoiled) spoiled++;
            }
            return spoiled;
        }
    }
}

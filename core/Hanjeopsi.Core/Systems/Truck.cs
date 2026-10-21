using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>여울호 푸드트럭 계산 (웹 systems/truck.ts)</summary>
    public sealed class Truck
    {
        private readonly GameData _data;
        private readonly Freshness _fresh;
        private readonly Taste _taste;
        private TruckBalance B { get { return _data.Balance.Truck; } }

        public Truck(GameData data, Freshness fresh, Taste taste)
        {
            _data = data;
            _fresh = fresh;
            _taste = taste;
        }

        /// <summary>요리 신선도 배율. 회는 신선도에 민감하고 숙성 어종은 숙성일 때 가장 좋다</summary>
        public double DishFresh(Recipe recipe, FreshStage stage)
        {
            var t = recipe.Raw ? B.Raw : B.Cooked;
            switch (stage)
            {
                case FreshStage.Live: return t.Live;
                case FreshStage.Fresh: return t.Fresh;
                case FreshStage.Aged: return _data.SpeciesById(recipe.Species).Aging ? t.AgedAging : t.Aged;
                default: return 0;
            }
        }

        /// <summary>이 요리에 쓸 가장 좋은 물고기. 요리 배율이 높은 것, 같으면 큰 것. 없으면 null</summary>
        public FishItem BestFishFor(SaveData save, Recipe recipe)
        {
            return save.Cooler
                .Where(f => f.Species == recipe.Species && _fresh.StageOf(f.Fresh) != FreshStage.Spoiled)
                .OrderByDescending(f => DishFresh(recipe, _fresh.StageOf(f.Fresh)))
                .ThenByDescending(f => f.Size)
                .FirstOrDefault();
        }

        /// <summary>지금 쿨러로 만들 수 있는 요리 (recipes.json 순서)</summary>
        public List<Recipe> Cookable(SaveData save)
        {
            return _data.Recipes.Recipes.Where(r => BestFishFor(save, r) != null).ToList();
        }

        public static double AverageQuality(IList<SeasonResult> results)
        {
            double sum = 0;
            foreach (var r in results) sum += r.Quality;
            return sum / results.Count;
        }

        /// <summary>요리값 = 메뉴 가격 × 신선도 배율 × (0.4 + 0.8 × 간 맞추기 정확도)</summary>
        public int DishPay(Recipe recipe, FreshStage stage, double avg)
        {
            return JsMath.Round100(recipe.Price * DishFresh(recipe, stage) * (B.PayBase + B.PayQuality * avg));
        }

        /// <summary>손님 기분 0 실망, 1 보통, 2 만족</summary>
        public int MoodOf(double avg)
        {
            return avg >= B.MoodGood ? 2 : avg >= B.MoodOk ? 1 : 0;
        }

        /// <summary>손님 한마디. 모르는 맛은 ??? 로 보이고 감칠맛이 돌아오면 리뷰 전문이 보인다</summary>
        public List<string> ReviewLines(SaveData save, Recipe recipe, IList<SeasonResult> results, FreshStage stage, int mood)
        {
            var t = _data.Texts.Review;
            var lines = new List<string>();
            for (int i = 0; i < recipe.Steps.Count; i++)
            {
                var s = recipe.Steps[i];
                if (!_taste.CanTaste(save, s.Flavor))
                {
                    lines.Add(t.Unknown);
                    continue;
                }
                var r = results[i];
                int idx = r.Side == 0 || r.Quality >= B.ReviewGoodQuality ? 0 : r.Side < 0 ? 1 : 2;
                lines.Add(_data.FlavorById(s.Flavor).Review[idx]);
            }
            if (_taste.Level(save) >= B.FullReviewTaste)
            {
                if (recipe.Raw && stage == FreshStage.Aged && _data.SpeciesById(recipe.Species).Aging) lines.Add(t.WellAged);
                else if (stage == FreshStage.Live) lines.Add(t.VeryFresh);
                lines.Add(t.Mood[mood]);
            }
            return lines;
        }

        /// <summary>간 맞추기 한 단계의 정답 중심. 요리마다 거의 고정이고 ±4% 흔들린다</summary>
        public double RollTarget(RecipeStep step, IRandom rng)
        {
            return JsMath.Clamp(step.Target + rng.Range(-B.TargetJitter, B.TargetJitter), B.TargetMin, B.TargetMax);
        }

        /// <summary>게이지 바늘 속도 (초당 왕복 비율)</summary>
        public double RollGaugeSpeed(IRandom rng)
        {
            return B.GaugeSpeedMin + rng.Next() * B.GaugeSpeedRange;
        }

        public SeasonResult Judge(double pos, double target)
        {
            return SeasonJudge.Judge(_data.Balance.Season, pos, target, B.ZoneHalf);
        }
    }
}

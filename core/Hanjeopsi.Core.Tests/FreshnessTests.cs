using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    public class FreshnessTests
    {
        private readonly Freshness _f = new Freshness(TestData.Game);

        [Fact]
        public void 단계가_0부터_100까지_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["stage"])
                Assert.Equal(Freshness.ParseKey((string)c["stage"]), _f.StageOf((int)c["fresh"]));
        }

        [Theory]
        // GAME_DESIGN.md 5장 표: 활어 76~100, 선어 41~75, 숙성 11~40, 상함 0~10
        [InlineData(100, FreshStage.Live)]
        [InlineData(76, FreshStage.Live)]
        [InlineData(75, FreshStage.Fresh)]
        [InlineData(41, FreshStage.Fresh)]
        [InlineData(40, FreshStage.Aged)]
        [InlineData(11, FreshStage.Aged)]
        [InlineData(10, FreshStage.Spoiled)]
        [InlineData(0, FreshStage.Spoiled)]
        public void 설계표_경계(int fresh, FreshStage stage)
        {
            Assert.Equal(stage, _f.StageOf(fresh));
        }

        [Fact]
        public void 배율이_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["factor"])
                Assert.Equal((double)c["value"], _f.PriceFactor((string)c["species"], Freshness.ParseKey((string)c["stage"])));
        }

        [Fact]
        public void 숙성_어종만_숙성일_때_값이_오른다()
        {
            Assert.Equal(1.3, _f.PriceFactor("noraemi", FreshStage.Aged));
            Assert.Equal(1.3, _f.PriceFactor("gamseongdom", FreshStage.Aged));
            Assert.Equal(0.45, _f.PriceFactor("bollak", FreshStage.Aged));
            Assert.Equal(0, _f.PriceFactor("gamseongdom", FreshStage.Spoiled));
        }

        [Fact]
        public void 단계_이름()
        {
            Assert.Equal("활어", _f.StageName(FreshStage.Live));
            Assert.Equal("상함", _f.StageName(FreshStage.Spoiled));
        }

        [Fact]
        public void 날이_바뀔_때_감소가_웹_endDay와_같다()
        {
            foreach (JObject c in TestData.MarketGolden["decay"])
            {
                int tackle = (int)c["tackle"];
                var cooler = c["start"].Select((v, i) => new FishItem(i, "bollak", 20, 1, (int)v)).ToList<FishItem>();
                foreach (JObject step in c["steps"])
                {
                    int spoiled = _f.ApplyDailyDecay(cooler, tackle);
                    Assert.Equal((int)step["spoiled"], spoiled);
                    Assert.Equal(step["fresh"].ToObject<int[]>(), cooler.Select(f => f.Fresh).ToArray());
                }
            }
        }

        [Fact]
        public void 소품별_하루_감소량()
        {
            Assert.Equal(new[] { 40, 25, 15 }, new[] { 0, 1, 2 }.Select(_f.DailyDecay));
        }
    }
}

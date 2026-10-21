using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>거리 구간, 어종·크기 뽑기 (웹 systems/fishing.ts)</summary>
    public class FishingRollTests
    {
        private readonly FishingRoll _f = new FishingRoll(TestData.Game);

        [Fact]
        public void 거리_구간이_웹과_같다()
        {
            foreach (JObject c in TestData.RulesGolden["zones"])
                Assert.Equal((int)c["zone"], _f.ZoneOf((double)c["d"]));
        }

        [Fact]
        public void 같은_난수면_웹과_같은_고기가_나온다()
        {
            // 웹은 Math.random 을 mulberry32(seed) 로 바꿔 끼워 기록했다
            foreach (JObject run in TestData.RulesGolden["rolls"])
            {
                var rng = new Mulberry32((uint)(int)run["seed"]);
                foreach (JObject c in run["list"])
                {
                    var sp = _f.RollSpecies((int)c["zone"], (int)c["slot"], (bool)c["hot"], rng);
                    Assert.Equal((string)c["species"], sp.Id);
                    Assert.Equal((double)c["size"], _f.RollSize(sp, (bool)c["hot"], rng));
                }
            }
        }

        [Theory]
        [InlineData(0, 0.6)]
        [InlineData(4, 0.74)]
        [InlineData(10, 0.95)]
        public void 고등어_힌트_정확도(int affinity, double expected)
        {
            Assert.Equal(expected, _f.CatHintAccuracy(affinity), 10);
        }
    }
}

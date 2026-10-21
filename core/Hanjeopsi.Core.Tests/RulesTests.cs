using System.Linq;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>달력과 낚시 규정 (웹 systems/calendar.ts, FishingPlan Limits.kt)</summary>
    public class RulesTests
    {
        private readonly Rules _r = new Rules(TestData.Game);

        [Fact]
        public void 달력과_금어기가_웹과_같다()
        {
            var species = TestData.Game.Species;
            foreach (JObject c in TestData.RulesGolden["calendar"])
            {
                int day = (int)c["day"];
                var d = _r.DateOf(day);
                Assert.Equal((int)c["date"]["month"], d.Month);
                Assert.Equal((int)c["date"]["date"], d.Day);
                Assert.Equal((string)c["label"], _r.DateLabel(day));
                Assert.Equal(c["closed"].ToObject<bool[]>(), species.Select(sp => _r.IsClosedSeason(sp.Id, day)).ToArray());
                Assert.Equal(c["closedToday"].ToObject<string[]>(), _r.ClosedToday(day).ToArray());
            }
        }

        [Fact]
        public void 놓아줘야_하는_이유가_웹과_같다()
        {
            foreach (JObject c in TestData.RulesGolden["release"])
                Assert.Equal((string)c["reason"], _r.ReleaseReason((string)c["species"], (double)c["size"], (int)c["day"]));
        }

        [Fact]
        public void 일차와_날짜()
        {
            Assert.Equal("4월 16일", _r.DateLabel(1));
            Assert.Equal("5월 1일", _r.DateLabel(16));
            Assert.Equal("5월 15일", _r.DateLabel(30));
        }

        [Theory]
        // 볼락 15cm "이하", 감성돔 25cm "이하" (FishingPlan 기본표 inclusive)
        [InlineData("bollak", 15.0, true)]
        [InlineData("bollak", 15.1, false)]
        [InlineData("gamseongdom", 25.0, true)]
        [InlineData("gamseongdom", 25.1, false)]
        [InlineData("godeungeo", 10.0, false)]
        public void 금지체장은_이하(string species, double size, bool under)
        {
            Assert.Equal(under, _r.IsUndersize(species, size));
        }

        [Fact]
        public void 감성돔은_5월_한달_금어기()
        {
            Assert.False(_r.IsClosedSeason("gamseongdom", 15)); // 4/30
            Assert.True(_r.IsClosedSeason("gamseongdom", 16));  // 5/1
            Assert.True(_r.IsClosedSeason("gamseongdom", 46));  // 5/31
            Assert.False(_r.IsClosedSeason("gamseongdom", 47)); // 6/1
            Assert.Equal("금어기(5/1~5/31)라 놓아줘야 한다", _r.ReleaseReason("gamseongdom", 30, 16));
            Assert.Equal("금지체장 25cm 이하라 놓아줘야 한다", _r.ReleaseReason("gamseongdom", 20, 16));
        }

        [Fact]
        public void 해를_넘는_금어기도_처리한다()
        {
            // 도다리 12/1~1/31 같은 규정 (2장 이후). 웹에는 아직 없어 FishingPlan 과 같게 맞춘다
            var data = TestData.Game;
            var sp = data.SpeciesById("bollak");
            var saved = sp.Closed;
            try
            {
                sp.Closed = new[] { 12, 1, 1, 31 };
                var r = new Rules(data);
                // 1일차 = 4/16 이니 230일차 = 12/1, 291일차 = 1/31, 292일차 = 2/1
                Assert.False(r.IsClosedSeason("bollak", 229));
                Assert.True(r.IsClosedSeason("bollak", 230));
                Assert.True(r.IsClosedSeason("bollak", 291));
                Assert.False(r.IsClosedSeason("bollak", 292));
            }
            finally
            {
                sp.Closed = saved;
            }
        }
    }
}

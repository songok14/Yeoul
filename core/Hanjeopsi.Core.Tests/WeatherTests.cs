using System.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>하루 날씨 (고닷 weather.json)</summary>
    public class WeatherTests
    {
        [Fact]
        public void 같은_판_같은_날은_늘_같은_날씨()
        {
            var a = Game.NewGame(TestData.Full, 7);
            var b = Game.NewGame(TestData.Full, 7);
            for (int d = 1; d <= 30; d++)
            {
                a.Save.Day = b.Save.Day = d;
                Assert.Equal(a.WeatherToday().Id, b.WeatherToday().Id);
            }
        }

        [Fact]
        public void 봄_1장에는_비와_안개만_오고_태풍과_눈은_없다()
        {
            var seen = new System.Collections.Generic.Dictionary<string, int>();
            for (int seed = 1; seed <= 40; seed++)
            {
                var g = Game.NewGame(TestData.Full, seed);
                for (int d = 1; d <= 30; d++)
                {
                    g.Save.Day = d;
                    string id = g.WeatherToday().Id;
                    seen[id] = seen.TryGetValue(id, out var n) ? n + 1 : 1;
                }
            }
            Assert.True(seen["clear"] > seen["rain"]);
            Assert.True(seen["rain"] > 0 && seen["fog"] > 0);
            Assert.False(seen.ContainsKey("typhoon"));
            Assert.False(seen.ContainsKey("snow"));
        }

        [Fact]
        public void 날씨_효과()
        {
            var g = Game.NewGame(TestData.Full, 3);
            g.Save.Slot = 2;
            int clearGuests = g.CustomersNow();
            g.ForcedWeather = "rain";
            Assert.True(g.WaitMul() < 1);
            Assert.Equal(clearGuests - 1, g.CustomersNow());
            g.ForcedWeather = "fog";
            Assert.True(g.BigAdd() > 0 && g.HintPenalty() > 0);
            g.ForcedWeather = "typhoon";
            Assert.False(g.CanFishToday());
            Assert.True(g.CustomersNow() >= 1);
            g.ForcedWeather = "snow";
            Assert.True(g.WaitMul() > 1);
        }

        [Fact]
        public void 웹_데이터는_늘_맑다()
        {
            var g = Game.NewGame(TestData.Game, 3);
            Assert.Equal("clear", g.WeatherToday().Id);
            Assert.Equal(1, g.WaitMul());
            Assert.True(g.CanFishToday());
        }
    }
}

using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    public class MarketTests
    {
        private static Market MarketFor(int seed)
        {
            var d = TestData.Game;
            return new Market(d, new Freshness(d), seed);
        }

        [Fact]
        public void 시세_흐름이_웹과_완전히_같다()
        {
            var species = TestData.Game.Species;
            foreach (JObject s in TestData.MarketGolden["market"])
            {
                var m = MarketFor((int)s["seed"]);
                foreach (JObject day in s["days"])
                {
                    int d = (int)day["day"];
                    for (int i = 0; i < species.Count; i++)
                    {
                        string id = species[i].Id;
                        Assert.Equal((double)day["priceMul"][i], m.PriceMul(id, d));
                        Assert.Equal((int)day["basePrice"][i], m.TodayBasePrice(id, d));
                        Assert.Equal((int)day["change"][i], m.ChangePercent(id, d));
                    }
                    Assert.Equal((string)day["headline"], m.Headline(d));
                }
            }
        }

        [Fact]
        public void 날짜를_건너뛰어_물어도_같다()
        {
            // 캐시 순서와 상관없이 같은 값
            var a = MarketFor(42);
            var b = MarketFor(42);
            double late = a.PriceMul("gamseongdom", 30);
            for (int d = 1; d <= 30; d++) b.PriceMul("gamseongdom", d);
            Assert.Equal(late, b.PriceMul("gamseongdom", 30));
            Assert.Equal(a.PriceMul("gamseongdom", 3), b.PriceMul("gamseongdom", 3));
        }

        [Fact]
        public void 시세_배율은_범위_안()
        {
            var m = MarketFor(5);
            foreach (var sp in TestData.Game.Species)
                for (int d = 1; d <= 120; d++)
                    Assert.InRange(m.PriceMul(sp.Id, d), 0.55, 1.7);
        }

        [Fact]
        public void 크기_배율이_웹과_같다()
        {
            var m = MarketFor(1);
            foreach (JObject c in TestData.MarketGolden["size"])
                Assert.Equal((double)c["value"], m.SizeFactor((string)c["species"], (double)c["size"]), 12);
        }

        [Fact]
        public void 물고기_값이_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["fish"])
            {
                var m = MarketFor((int)c["seed"]);
                var item = new FishItem(0, (string)c["species"], (double)c["size"], 1, (int)c["fresh"]);
                Assert.Equal((int)c["value"], m.FishValue(item, (int)c["day"]));
            }
        }

        [Fact]
        public void 경매_낙찰가가_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["auction"])
            {
                var m = MarketFor((int)c["seed"]);
                var items = c["items"].Select(f => new FishItem((int)f["uid"], (string)f["species"], (double)f["size"], 1, (int)f["fresh"]));
                Assert.Equal((int)c["total"], m.AuctionTotal(items, (int)c["day"], (double)c["mul"]));
            }
        }

        [Fact]
        public void 상한_물고기는_0원()
        {
            var m = MarketFor(1);
            Assert.Equal(0, m.FishValue(new FishItem(1, "gamseongdom", 50, 1, 10), 3));
        }

        [Fact]
        public void 첫날_소식과_등락()
        {
            var m = MarketFor(1);
            Assert.Equal("물비늘포 위판장 소식: 봄 볼락이 올라오기 시작했다", m.Headline(1));
            Assert.Equal(0, m.ChangePercent("bollak", 1));
        }
    }
}

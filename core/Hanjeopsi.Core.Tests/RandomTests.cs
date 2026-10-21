using Hanjeopsi.Core.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    public class RandomTests
    {
        [Fact]
        public void mulberry32_수열이_웹과_비트까지_같다()
        {
            foreach (JObject c in TestData.MarketGolden["rng"])
            {
                var r = new Mulberry32((uint)(long)c["seed"]);
                foreach (var v in c["values"]) Assert.Equal((double)v, r.Next());
            }
        }

        [Fact]
        public void hash가_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["hash"])
            {
                var args = c["args"].ToObject<long[]>();
                var ints = new int[args.Length];
                for (int i = 0; i < args.Length; i++) ints[i] = (int)args[i];
                Assert.Equal((uint)(long)c["value"], Mulberry32.Hash(ints));
            }
        }

        [Fact]
        public void 범위_난수()
        {
            var r = new Mulberry32(1);
            for (int i = 0; i < 1000; i++)
            {
                double v = r.Range(0.92, 1.15);
                Assert.InRange(v, 0.92, 1.15);
                Assert.InRange(r.RangeInt(-1, 1), -1, 1);
            }
        }
    }
}

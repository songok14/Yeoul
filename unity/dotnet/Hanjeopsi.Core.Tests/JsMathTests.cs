using Hanjeopsi.Core.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    public class JsMathTests
    {
        [Fact]
        public void 반올림이_JS_Math_round와_같다()
        {
            foreach (JObject c in TestData.MarketGolden["round"])
            {
                double x = (double)c["x"];
                Assert.Equal((double)c["round"], JsMath.Round(x));
                Assert.Equal((int)c["round100"], JsMath.Round100(x));
            }
        }

        [Fact]
        public void 음수_반은_올림쪽으로()
        {
            Assert.Equal(-2, JsMath.Round(-2.5));
            Assert.Equal(3, JsMath.Round(2.5));
            Assert.Equal(0, JsMath.Round(0.49999999999999994));
        }

        [Fact]
        public void 돈_표시가_웹과_같다()
        {
            foreach (JObject c in TestData.MarketGolden["won"])
                Assert.Equal((string)c["text"], JsMath.Won((double)c["n"]));
        }
    }
}

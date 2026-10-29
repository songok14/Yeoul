using System.Linq;
using System.Numerics;
using Hanjeopsi.Core.Systems;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>걷는 언덕 마을 길 (townmap.json)</summary>
    public class TownPathTests
    {
        private static TownPath Path()
        {
            return Game.NewGame(TestData.Full, 1).Town;
        }

        [Fact]
        public void 길_그물은_모두_이어져_있고_장소마다_문_앞_점이_있다()
        {
            var p = Path();
            Assert.NotNull(p);
            Assert.True(p.Connected());
            var map = p.Map;
            Assert.Equal(TestData.Full.Balance.Time.SlotsPerDay, map.Maps.Count);
            foreach (var id in new[] { "market", "shop", "grandma", "truck", "breakwater" })
                Assert.NotNull(map.NodeById(map.PlaceById(id).Node));
            map.NodeById(map.Start);
            Assert.All(map.Nodes, n => Assert.InRange(n.X, 0, 450));
            Assert.All(map.Nodes, n => Assert.InRange(n.Y, 0, 800));
        }

        [Fact]
        public void 누른_곳에서_가장_가까운_길_위_점으로_간다()
        {
            var p = Path();
            var plaza = p.PosOf("plaza");
            // 광장 옆 바다를 누르면 광장 가장자리 길 위로
            var near = p.Nearest(new Vector2(240, 700));
            Assert.True(near.At.Y < 680);
            // 길 위 점을 누르면 그 자리
            var on = p.Nearest(plaza);
            Assert.True(Vector2.Distance(on.At, plaza) < 0.01f);
        }

        [Fact]
        public void 광장에서_전망대까지_길을_따라_오른다()
        {
            var p = Path();
            var from = p.PosOf("plaza");
            var route = p.RouteToNode(from, "lookout");
            Assert.True(Vector2.Distance(route.Last(), p.PosOf("lookout")) < 0.01f);
            // 지나는 점이 모두 길 점이고 위로 오른다
            Assert.True(route.Count >= 10);
            Assert.True(route.Last().Y < route.First().Y);
            // 곧장 가는 거리보다 길다 (길을 따라 구불구불)
            Assert.True(TownPath.Length(from, route) > Vector2.Distance(from, route.Last()) * 1.2f);
            // 방파제 끝은 트럭 앞을 지나 간다
            var toSea = p.RouteToNode(p.PosOf("market"), "breakwater");
            Assert.Contains(toSea, v => Vector2.Distance(v, p.PosOf("breakwater0")) < 0.01f);
        }

        [Fact]
        public void 언덕_위일수록_작게_그린다()
        {
            var p = Path();
            Assert.True(p.ScaleAt(p.PosOf("lookout").Y) < p.ScaleAt(p.PosOf("plaza").Y));
            Assert.InRange(p.ScaleAt(p.PosOf("lookout").Y), 0.7f, 0.8f);
            Assert.InRange(p.ScaleAt(p.PosOf("breakwater").Y), 1f, p.Map.Scale.Max);
        }
    }
}

using System.Linq;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>고닷 낚시 규칙: 방향 바늘, 고등어 지점, 가까운 정도, 큰 놈, 고등어 도둑</summary>
    public class SpotTests
    {
        private static readonly Game G = Game.NewGame(TestData.Game, 7);
        private SpotRules S { get { return G.Spot; } }

        [Fact]
        public void 방향_바늘은_양_끝에서_돌아선다()
        {
            double a = 0;
            int dir = 1;
            for (int i = 0; i < 600; i++)
            {
                (a, dir) = S.StepAim(a, dir, 1.0 / 60);
                Assert.InRange(a, -1, 1);
            }
            Assert.Equal((1.0, -1), S.StepAim(0.999, 1, 0.1));
            Assert.Equal((-1.0, 1), S.StepAim(-0.999, -1, 0.1));
        }

        [Fact]
        public void 지점은_낚싯대가_닿는_곳에_있고_호감이_높으면_잘_맞힌다()
        {
            var rng = new Mulberry32(3);
            int right0 = 0, right10 = 0;
            for (int i = 0; i < 2000; i++)
            {
                var (hot, hint) = S.RollSpots(0, 0.8, rng);
                Assert.InRange(hot.Dist, 0, 0.8);
                Assert.InRange(hint.Angle, -1, 1);
                if (hot.Angle == hint.Angle && hot.Dist == hint.Dist) right0++;
                var b = S.RollSpots(10, 0.8, rng);
                if (b.Hot.Angle == b.Hint.Angle) right10++;
            }
            Assert.InRange(right0 / 2000.0, 0.55, 0.65);
            Assert.InRange(right10 / 2000.0, 0.9, 0.98);
        }

        [Fact]
        public void 가까운_정도는_지점_위에서_1_멀면_0()
        {
            var spot = new CastSpot(0.2, 0.5);
            Assert.Equal(1, S.Closeness(spot, spot));
            Assert.Equal(0, S.Closeness(new CastSpot(-0.8, 0.5), spot));
            Assert.Equal(0, S.Closeness(new CastSpot(0.2, 0.05), spot));
            double mid = S.Closeness(new CastSpot(0.35, 0.55), spot);
            Assert.InRange(mid, 0.3, 0.9);
        }

        [Fact]
        public void 지점에_가까우면_큰_놈과_귀한_고기가_잘_나온다()
        {
            var rng = new Mulberry32(11);
            int bigFar = 0, bigNear = 0, rareFar = 0, rareNear = 0;
            for (int i = 0; i < 5000; i++)
            {
                if (S.IsBigBite(0, rng)) bigFar++;
                if (S.IsBigBite(1, rng)) bigNear++;
                if (G.Fishing.RollSpeciesNear(2, 0, 0, rng).Rare) rareFar++;
                if (G.Fishing.RollSpeciesNear(2, 0, 1, rng).Rare) rareNear++;
            }
            Assert.True(bigNear > bigFar * 3);
            Assert.True(rareNear > rareFar * 2);
            var sp = TestData.Game.Species.First();
            for (int i = 0; i < 200; i++)
            {
                double big = G.Fishing.RollSizeNear(sp, 0.5, true, rng);
                Assert.InRange(big, sp.MinSize + (sp.MaxSize - sp.MinSize) * 0.7, sp.MaxSize);
                Assert.InRange(G.Fishing.RollSizeNear(sp, 0, false, rng), sp.MinSize, sp.MaxSize);
            }
        }

        [Fact]
        public void 고등어는_쿨러가_비면_노리지_않고_제일_싼_고기를_물고_간다()
        {
            var rng = new Mulberry32(5);
            Assert.Equal(-1, S.RollThief(0, rng));
            int tries = Enumerable.Range(0, 1000).Count(_ => S.RollThief(3, rng) >= 0);
            Assert.InRange(tries, 100, 230);

            var g = Game.NewGame(TestData.Game, 9);
            g.AddToCooler("godeungeo", 35);
            g.AddToCooler("bollak", 15);
            var cheap = g.Save.Cooler.OrderBy(f => g.FishValue(f)).First();
            var stolen = g.CatSteal();
            Assert.Equal(cheap.Uid, stolen.Uid);
            Assert.Single(g.Save.Cooler);
            g.CatSteal();
            Assert.Null(g.CatSteal());
        }
    }
}

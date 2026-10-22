using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>위판장 쿨러 묶음과 납품·판매에 쓰는 값</summary>
    public class CoolerGroupTests
    {
        private static Game WithCooler(params (string Species, double Size, int Fresh)[] fish)
        {
            var g = Game.NewGame(TestData.Game, 777);
            foreach (var f in fish) g.Save.Cooler.Add(new FishItem(g.Save.NextUid++, f.Species, f.Size, 1, f.Fresh));
            return g;
        }

        [Fact]
        public void 어종_순서와_신선도_단계로_묶는다()
        {
            var g = WithCooler(("noraemi", 24, 100), ("bollak", 18, 60), ("bollak", 20, 100), ("bollak", 19, 100), ("noraemi", 22, 0));
            var groups = g.CoolerGroups();
            Assert.Equal(new[] { "bollak", "bollak", "noraemi", "noraemi" }, groups.Select(x => x.Species));
            Assert.Equal(new[] { FreshStage.Live, FreshStage.Fresh, FreshStage.Live, FreshStage.Spoiled }, groups.Select(x => x.Stage));
            Assert.Equal(2, groups[0].Items.Count);
            Assert.Equal(groups[0].Items.Sum(f => g.FishValue(f)), groups[0].Value);
            Assert.Equal(0, groups[3].Value);
        }

        [Fact]
        public void 상한_것은_팔_것과_의뢰에서_빠진다()
        {
            var g = WithCooler(("bollak", 18, 100), ("bollak", 20, 0), ("bollak", 21, 30));
            Assert.Equal(2, g.SellableFish().Count);
            Assert.Equal(1, g.RequestFishHave("bollak"));
            Assert.Equal(0, g.RequestFishHave("godeungeo"));
        }

        [Fact]
        public void 어종마다_요리가_있다()
        {
            foreach (var sp in TestData.Game.Species) Assert.NotNull(Game.NewGame(TestData.Game, 1).RecipeForSpecies(sp.Id));
        }
    }
}

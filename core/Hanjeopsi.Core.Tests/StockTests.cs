using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>활어 수조, 낚시 중 놓아주기, 메뉴판 접시 수 (고닷만)</summary>
    public class StockTests
    {
        private static Game New()
        {
            var g = Game.NewGame(TestData.Full, 21);
            g.Save.Slot = 2;
            g.Save.Money = 1_000_000;
            return g;
        }

        [Fact]
        public void 새_판에는_수조가_있고_웹_판에는_없다()
        {
            Assert.True(New().HasTank());
            Assert.Equal(TestData.Full.Gear.Tanks[0].Capacity, New().TankCapacity());
            var web = Game.NewGame(TestData.Game, 3);
            Assert.False(web.HasTank());
            Assert.DoesNotContain("tank", SaveJson.ToJson(web.Save));
        }

        [Fact]
        public void 수조에는_활어만_들어가고_날이_바뀌어도_활어다()
        {
            var g = New();
            var live = g.AddToCooler("bollak", 20);
            var old = g.AddToCooler("bollak", 21);
            old.Fresh = 60;
            Assert.True(g.MoveToTank(live.Uid));
            Assert.False(g.MoveToTank(old.Uid));
            for (int i = 0; i < 5; i++) g.EndDay();
            Assert.Equal(FreshStage.Live, g.Fresh.StageOf(live.Fresh));
            Assert.Contains(live, g.TankFish());
            Assert.DoesNotContain(live, g.Save.Cooler);
        }

        [Fact]
        public void 밤에만_자기_전에_수조를_묻고_비싼_활어부터_빈자리만큼_고른다()
        {
            var g = New();
            var small = g.AddToCooler("bollak", 15);
            var big = g.AddToCooler("bollak", 30);
            var old = g.AddToCooler("bollak", 32);
            old.Fresh = 60;
            Assert.False(g.AsksTankBeforeSleep());
            g.Save.Slot = g.Data.Balance.Time.SlotsPerDay - 1;
            Assert.True(g.AsksTankBeforeSleep());
            Assert.Equal(new[] { big, small }, g.TankCandidates());
            Assert.DoesNotContain(old, g.TankCandidates());

            int cap = g.TankCapacity();
            for (int i = 0; i < cap - 1; i++) g.Save.Tank.Fish.Add(new FishItem { Uid = 9000 + i, Species = "bollak", Size = 20, Fresh = 100 });
            Assert.Equal(1, g.TankRoom());
            Assert.Equal(new[] { big }, g.TankPicks());

            Assert.True(g.MoveToTank(big.Uid));
            Assert.False(g.AsksTankBeforeSleep());
            g.UseSlot();
            Assert.Equal(FreshStage.Live, g.Fresh.StageOf(big.Fresh));
        }

        [Fact]
        public void 수조가_차면_더_못_넣고_넓히면_더_들어간다()
        {
            var g = New();
            int cap = g.TankCapacity();
            for (int i = 0; i < cap; i++) Assert.True(g.MoveToTank(g.AddToCooler("bollak", 20 + i).Uid));
            var extra = g.AddToCooler("bollak", 30);
            Assert.False(g.MoveToTank(extra.Uid));
            int money = g.Save.Money;
            Assert.True(g.UpgradeTank());
            Assert.Equal(money - g.Data.Gear.Tanks[1].Price, g.Save.Money);
            Assert.True(g.TankCapacity() > cap);
            Assert.True(g.MoveToTank(extra.Uid));
        }

        [Fact]
        public void 돈이_모자라면_수조를_못_넓힌다()
        {
            var g = New();
            g.Save.Money = 0;
            Assert.False(g.UpgradeTank());
            Assert.Equal(0, g.Save.Tank.Level);
        }

        [Fact]
        public void 수조_고기도_팔고_요리하고_꺼낼_수_있다()
        {
            var g = New();
            var a = g.AddToCooler("bollak", 20);
            var b = g.AddToCooler("godeungeo", 30);
            g.MoveToTank(a.Uid);
            Assert.Equal(2, g.SellableFish().Count);
            Assert.NotNull(g.Truck.BestFishFor(g.Save, g.RecipeForSpecies("bollak")));
            Assert.True(g.MoveToCooler(a.Uid));
            g.MoveToTank(a.Uid);
            int earned = g.SellFish(new[] { a.Uid }, 1);
            Assert.True(earned > 0);
            Assert.Empty(g.TankFish());
            Assert.Single(g.Save.Cooler);
            Assert.Equal(b.Uid, g.Save.Cooler[0].Uid);
        }

        [Fact]
        public void 골라서_한_마리만_팔_수_있다()
        {
            var g = New();
            var small = g.AddToCooler("bollak", 18);
            var big = g.AddToCooler("bollak", 26);
            int value = g.FishValue(big);
            int got = g.SellFish(new[] { big.Uid }, 1);
            Assert.Equal(value, got);
            Assert.Single(g.Save.Cooler);
            Assert.Equal(small.Uid, g.Save.Cooler[0].Uid);
        }

        [Fact]
        public void 쿨러의_활어만_놓아줄_수_있다()
        {
            var g = New();
            g.RecordCatch("bollak", 20, false);
            var live = g.AddToCooler("bollak", 20);
            var old = g.AddToCooler("bollak", 21);
            old.Fresh = 60;
            Assert.Equal(new[] { live.Uid }, g.ReleasableFish().Select(f => f.Uid));
            Assert.False(g.ReleaseFromCooler(old.Uid));
            Assert.True(g.ReleaseFromCooler(live.Uid));
            Assert.Equal(1, g.Save.Dex["bollak"].Released);
            Assert.Single(g.Save.Cooler);
        }

        [Fact]
        public void 고기_한_마리로는_요리_두_칸을_잡지_못한다()
        {
            var g = New();
            g.AddToCooler("ureok", 30);
            var plan = g.SuggestMenuPlan();
            Assert.Single(plan);
            Assert.Equal(1, plan[0].Count);
            // 같은 어종 다른 요리는 남은 접시가 없다
            var other = g.Data.Recipes.Recipes.First(r => r.Species == "ureok" && r != plan[0].Recipe);
            Assert.Equal(0, g.MenuFree(plan, other));
        }

        [Fact]
        public void 같은_어종_두_요리에_접시를_나눠_잡는다()
        {
            var g = New();
            for (int i = 0; i < 3; i++) g.AddToCooler("ureok", 30 + i);
            var recipes = g.Data.Recipes.Recipes.Where(r => r.Species == "ureok").ToList();
            var plan = new List<MenuEntry> { new MenuEntry { Recipe = recipes[0], Count = 2 } };
            Assert.Equal(1, g.MenuFree(plan, recipes[1]));
            plan.Add(new MenuEntry { Recipe = recipes[1], Count = 1 });
            Assert.Equal(2, g.MenuFree(plan, recipes[0], plan[0]));
            // 손님이 접시를 다 먹으면 그 칸은 더 시킬 수 없다
            Game.MarkServed(plan, recipes[1]);
            Assert.DoesNotContain(recipes[1], g.MenuOpen(plan));
            Assert.Contains(recipes[0], g.MenuOpen(plan));
        }

        [Fact]
        public void 고기가_줄면_메뉴판_접시도_준다()
        {
            var g = New();
            var a = g.AddToCooler("bollak", 20);
            g.AddToCooler("bollak", 22);
            var plan = g.SuggestMenuPlan();
            Assert.Equal(2, plan[0].Count);
            g.SellFish(new[] { a.Uid }, 1);
            g.FitMenu(plan);
            Assert.Equal(1, plan[0].Count);
            g.SellFish(g.Save.Cooler.Select(f => f.Uid).ToList(), 1);
            g.FitMenu(plan);
            Assert.Empty(plan);
        }

        [Fact]
        public void 메뉴_칸은_등급만큼()
        {
            var g = New();
            foreach (var sp in new[] { "bollak", "godeungeo", "noraemi", "ureok", "hakgongchi" }) g.AddToCooler(sp, g.Data.SpeciesById(sp).AvgSize + 2);
            Assert.Equal(g.Diner.MenuSlots(g.Save), g.SuggestMenuPlan().Count);
        }
    
        [Fact]
        public void 능력치는_장비가_좋아질수록_찬다()
        {
            var g = New();
            var before = g.Abilities().ToDictionary(a => a.Id, a => a.Fill);
            g.Save.Rod = g.Data.Gear.Rods.Count - 1;
            g.Save.Reel = g.Data.Gear.Reels.Count - 1;
            g.Save.Tackle = g.Data.Gear.Tackles.Count - 1;
            var after = g.Abilities().ToDictionary(a => a.Id, a => a.Fill);
            foreach (var id in new[] { "Cast", "Absorb", "Speed", "Keep", "Fresh" })
            {
                Assert.True(after[id] > before[id], id);
                Assert.Equal(1, after[id], 3);
            }
            Assert.All(g.Abilities(), a => Assert.InRange(a.Fill, 0, 1));
        }
    }
}

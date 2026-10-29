using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>고닷 장비 10단계, 보상, 미끼, 통발 (gear.json)</summary>
    public class KitTests
    {
        private static Game New()
        {
            var g = Game.NewGame(TestData.Full, 5);
            g.Save.Slot = 1;
            g.Save.Money = 2_000_000;
            g.Save.Flags["nossiFavor"] = true;
            return g;
        }

        [Fact]
        public void 장비는_열_단계이고_상점은_4단계까지()
        {
            var gear = TestData.Full.Gear;
            Assert.All(new[] { gear.Rods.Count, gear.Reels.Count, gear.Tackles.Count }, n => Assert.Equal(10, n));
            Assert.Equal(9, gear.Kits.Count);
            Assert.All(gear.Kits, k => Assert.Equal(10, k.Levels.Count));
            Assert.All(gear.Kits, k => Assert.All(k.Levels.Take(4), l => Assert.Equal("shop", l.Unlock)));
            Assert.All(gear.Kits, k => Assert.All(k.Levels.Skip(7), l => Assert.Equal("later", l.Unlock)));
            Assert.All(gear.Kits, k => Assert.Contains("{0}", k.Effect));
            Assert.Equal(10, gear.Baits.Count);
            Assert.Contains(gear.Baits, b => b.Id == "krill");
            // 그림이 다 있다
            string dir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../godot/art/gear");
            foreach (var kind in new[] { "rod", "reel", "cooler", "bait" }.Concat(gear.Kits.Select(k => k.Id)))
                for (int t = 1; t <= 10; t++) Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, kind + "-" + t + ".png")), kind + "-" + t);
        }

        [Fact]
        public void 상점은_차례대로_사고_보상_단계는_못_산다()
        {
            var g = New();
            for (int i = 0; i < 4; i++) Assert.True(g.BuyKit("line"));
            Assert.Equal(4, g.KitLevel("line"));
            Assert.False(g.BuyKit("line"));
            for (int i = 0; i < 3; i++) Assert.True(g.UpgradeGear(GearSlot.Reel));
            Assert.Equal(3, g.Save.Reel);
            Assert.False(g.UpgradeGear(GearSlot.Reel));
        }

        [Fact]
        public void 새_장비는_노씨_부탁을_들어준_뒤에_판다()
        {
            var g = New();
            g.Save.Flags.Remove("nossiFavor");
            Assert.False(g.BuyKit("line"));
            Assert.True(g.UpgradeGear(GearSlot.Reel));
            g.Save.Flags["nossiFavor"] = true;
            Assert.True(g.BuyKit("line"));
        }

        [Fact]
        public void 조건을_채우면_한_단계씩_한_번만_받는다()
        {
            var g = New();
            g.Save.Flags.Remove("nossiFavor");
            Assert.Empty(g.CheckGifts());
            // 노씨 부탁: 쿨러·찌·의자 보상이 걸려 있어도 한 단계씩만
            g.Save.Flags["nossiFavor"] = true;
            var favor = g.CheckGifts();
            Assert.Contains(favor, x => x.Kind == "chair" && x.Tier == 1);
            Assert.Equal(1, g.KitLevel("chair"));
            Assert.Equal(1, g.Save.Tackle);
            Assert.Empty(g.CheckGifts());
            // 다른 보상은 노씨가 주지 않는다
            g.Save.Diner.Rep = g.Data.Diner.Ranks[1].Rep;
            Assert.Empty(g.CheckGifts());
            Assert.Equal(0, g.Save.Reel);
        }

        [Fact]
        public void 보상_조건을_채우면_가게에_들어오고_값을_내고_산다()
        {
            var g = New();
            g.Save.Money = 10000000;
            // 릴 5단계(rank:1)는 4단계까지 산 뒤에야 보인다
            while (g.GearLevel(GearSlot.Reel) < 3) Assert.True(g.UpgradeGear(GearSlot.Reel));
            Assert.False(g.InStock("reel"));
            Assert.False(g.UpgradeGear(GearSlot.Reel));
            Assert.Equal("rank:1", g.NextUnlock("reel"));

            // 조건을 채운 그 자리에서 알리고 미끼를 준다. 한 번만
            int worm = g.BaitCount("worm");
            g.Save.Diner.Rep = g.Data.Diner.Ranks[1].Rep;
            var news = g.CheckStock();
            Assert.Contains(news, x => x.Kind == "reel" && x.Tier == 5);
            Assert.Equal(worm + g.BaitDef("worm").Amount * news.Count, g.BaitCount("worm"));
            Assert.Empty(g.CheckStock());

            // 값을 내고 산다. 조건 하나로 한 단계만
            int money = g.Save.Money;
            Assert.True(g.InStock("reel"));
            Assert.True(g.UpgradeGear(GearSlot.Reel));
            Assert.Equal(4, g.GearLevel(GearSlot.Reel));
            Assert.Equal(money - g.GearPrice(GearSlot.Reel, 4), g.Save.Money);
            Assert.False(g.InStock("reel"));
            Assert.False(g.UpgradeGear(GearSlot.Reel));
            Assert.Equal("regulars:6", g.NextUnlock("reel"));

            // 새 장비도 같다: 낚싯줄 5단계(rank:1)
            g.Save.Flags["nossiFavor"] = true;
            while (g.KitLevel("line") < 4) Assert.True(g.BuyKit("line"));
            Assert.True(g.BuyKit("line"));
            Assert.Equal(5, g.KitLevel("line"));
            Assert.False(g.BuyKit("line"));
        }

        [Fact]
        public void 장비_효과가_낚시에_들어간다()
        {
            var g = New();
            var sp = g.Data.SpeciesById("bollak");
            var before = g.Fight(sp, 20, 0.5, false, false);
            g.BuyKit("line");
            g.BuyKit("hook");
            g.BuyKit("float");
            g.BuyKit("net");
            var after = g.Fight(sp, 20, 0.5, false, false);
            Assert.True(after.Reel.TensionMul < before.Reel.TensionMul);
            Assert.True(after.Rod.HookBonus > before.Rod.HookBonus);
            Assert.True(after.SlackBonus > 0 && after.LandAt > 0);
            // 원본 장비 데이터는 그대로
            Assert.Equal(1, g.Data.Gear.Reels[0].TensionMul);
            double wait = g.WaitMul();
            g.BuyKit("chair");
            Assert.True(g.WaitMul() < wait);
        }

        [Fact]
        public void 미끼를_고르면_좋아하는_고기가_잘_문다()
        {
            var g = New();
            Assert.True(g.BuyBaitKind("shrimp"));
            Assert.Equal(8, g.BaitCount("shrimp"));
            g.PickBait("shrimp");
            Assert.Equal(8, g.BaitLeft());
            var bias = g.BaitBias();
            Assert.Equal(g.Data.Gear.BaitLikeMul, bias(g.Data.SpeciesById("gamseongdom")));
            Assert.Equal(1, bias(g.Data.SpeciesById("hakgongchi")));
            int krill = g.Save.Bait;
            g.UseBaitNow();
            Assert.Equal(7, g.BaitCount("shrimp"));
            Assert.Equal(krill, g.Save.Bait);
            // 다 쓰면 크릴로 돌아간다
            g.Save.Kit.Bait["shrimp"] = 0;
            Assert.Equal(krill, g.BaitLeft());
            Assert.Equal("krill", g.Save.Kit.Pick);
            // 미끼통만큼 한 봉지가 더 간다
            g.BuyKit("baitbox");
            int b0 = g.Save.Bait;
            g.BuyBait();
            Assert.Equal(b0 + g.Data.Gear.Bait.Amount + 1, g.Save.Bait);
        }

        [Fact]
        public void 통발은_밤새_고기를_잡아_온다()
        {
            var g = New();
            g.BuyKit("trap");
            int bait = g.Save.Bait;
            g.EndDay();
            var trap = g.Save.PendingSummary.Trap;
            Assert.NotNull(trap);
            Assert.Equal(bait - 1, g.Save.Bait);
            Assert.True(g.Save.Cooler.Count >= trap.Count);
        }

        [Fact]
        public void 웹_데이터에는_고닷_장비가_없다()
        {
            var g = Game.NewGame(TestData.Game, 5);
            Assert.Null(g.Save.Kit);
            Assert.Empty(g.CheckGifts());
            Assert.Same(g.Data.Gear.Rods[0], g.EffectiveRod());
            Assert.DoesNotContain("kit", SaveJson.ToJson(g.Save));
        }
    }
}

using System.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>고닷 하루 다섯 칸: 새벽 · 아침 · 점심 · 저녁 · 밤</summary>
    public class TimeTests
    {
        private static Game At(int slot)
        {
            var g = Game.NewGame(TestData.Full, 7);
            g.Save.Slot = slot;
            return g;
        }

        [Fact]
        public void 하루는_다섯_칸이고_밤이_지나면_다음_날_새벽()
        {
            var g = At(0);
            Assert.Equal(new[] { "새벽", "아침", "점심", "저녁", "밤" }, g.Data.Texts.Slots);
            int day = g.Save.Day;
            for (int i = 0; i < 4; i++) g.UseSlot();
            Assert.Equal("밤", g.SlotName());
            Assert.Equal(day, g.Save.Day);
            g.UseSlot();
            Assert.Equal(day + 1, g.Save.Day);
            Assert.Equal(0, g.Save.Slot);
        }

        [Fact]
        public void 시간대마다_할_수_있는_일이_다르다()
        {
            // 새벽: 경매, 트럭 쉼 / 아침: 경매, 트럭 / 점심·저녁: 트럭 / 밤: 트럭(야식), 미끼가게 닫음
            Assert.True(At(0).IsMarketOpen());
            Assert.False(At(0).IsTruckOpen());
            Assert.True(At(1).IsMarketOpen());
            Assert.False(At(2).IsMarketOpen());
            Assert.True(At(4).IsTruckOpen());
            Assert.True(At(3).IsShopOpen());
            var night = At(4);
            Assert.False(night.IsShopOpen());
            night.Save.Money = 100_000;
            Assert.False(night.BuyBait());
            Assert.False(night.UpgradeGear(GearSlot.Tackle));
            Assert.Equal(100_000, night.Save.Money);
            // 점심에 손님이 가장 많고 밤엔 적다
            Assert.True(At(2).CustomersNow() > At(1).CustomersNow());
            Assert.True(At(4).CustomersNow() < At(3).CustomersNow());
        }

        [Fact]
        public void 마을_배경은_시간대마다_다르고_밤은_밤_그림()
        {
            var names = Enumerable.Range(0, 5).Select(i => At(i).TownArt()).ToList();
            Assert.Equal(5, names.Distinct().Count());
            Assert.Equal("village-night", names[4]);
        }

        [Fact]
        public void 밤에는_밤고기가_잘_문다()
        {
            var d = TestData.Full;
            var galchi = d.SpeciesById("galchi");
            var hakgongchi = d.SpeciesById("hakgongchi");
            Assert.True(galchi.Slot[4] > galchi.Slot[2]);
            Assert.True(hakgongchi.Slot[4] < hakgongchi.Slot[2]);
            Assert.Contains("밤", d.SpeciesById("bollak").Hint);
        }
    }
}

using System.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>고닷 하루 네 칸: 아침 · 점심 · 저녁 · 밤 (2026-10-28)</summary>
    public class TimeTests
    {
        private static Game At(int slot)
        {
            var g = Game.NewGame(TestData.Full, 7);
            g.Save.Slot = slot;
            return g;
        }

        [Fact]
        public void 하루는_네_칸이고_밤이_지나면_다음_날_아침()
        {
            var g = At(0);
            Assert.Equal(new[] { "아침", "점심", "저녁", "밤" }, g.Data.Texts.Slots);
            int day = g.Save.Day;
            for (int i = 0; i < 3; i++) g.UseSlot();
            Assert.Equal("밤", g.SlotName());
            Assert.Equal(day, g.Save.Day);
            g.UseSlot();
            Assert.Equal(day + 1, g.Save.Day);
            Assert.Equal(0, g.Save.Slot);
        }

        [Fact]
        public void 시간대마다_할_수_있는_일이_다르다()
        {
            // 아침·점심: 낚시, 경매 / 저녁: 장사만 / 밤: 밤낚시(열리면), 미끼가게 닫음
            for (int slot = 0; slot < 2; slot++)
            {
                Assert.True(At(slot).IsMarketOpen());
                Assert.True(At(slot).CanFishNow());
                Assert.False(At(slot).IsTruckOpen());
                Assert.Equal(0, At(slot).CustomersNow());
                Assert.True(At(slot).IsShopOpen());
            }
            Assert.False(At(2).IsMarketOpen());
            Assert.True(At(2).IsTruckOpen());
            Assert.False(At(2).CanFishNow());
            Assert.True(At(2).CustomersNow() > 0);
            Assert.True(At(2).IsShopOpen());
            var night = At(3);
            Assert.False(night.IsTruckOpen());
            Assert.False(night.IsShopOpen());
            night.Save.Money = 100_000;
            Assert.False(night.BuyBait());
            Assert.False(night.UpgradeGear(GearSlot.Tackle));
            Assert.Equal(100_000, night.Save.Money);
        }

        [Fact]
        public void 밤낚시는_노씨가_케미라이트를_건넨_뒤에_열린다()
        {
            var g = At(3);
            Assert.True(g.IsNight());
            Assert.False(g.NightFishingOpen());
            Assert.False(g.CanFishNow());
            // 8일에 마을에 들어오면 노씨 장면이 나온다
            g.Save.Day = 8;
            foreach (var b in new[] { "catFloat", "nossiNote" }) g.MarkBeat(b);
            Assert.Equal("nossiLamp", g.NextBeat());
            g.MarkBeat("nossiLamp");
            Assert.True(g.CanFishNow());
        }

        [Fact]
        public void 낚시를_못_가면_저녁까지_쉰다()
        {
            var g = At(0);
            Assert.True(g.RestUntilTruck());
            Assert.Equal(2, g.Save.Slot);
            Assert.True(g.IsTruckOpen());
            Assert.False(g.RestUntilTruck());
        }

        [Fact]
        public void 마을_배경은_시간대마다_다르고_밤은_밤_그림()
        {
            var names = Enumerable.Range(0, 4).Select(i => At(i).TownArt()).ToList();
            Assert.Equal(4, names.Distinct().Count());
            Assert.Equal("village-night", names[3]);
        }

        [Fact]
        public void 아침_칸은_새벽과_아침_어종_가중치의_평균()
        {
            var g = At(0);
            var sp = g.Data.SpeciesById("chamdom");
            Assert.Equal((sp.Slot[0] + sp.Slot[1]) / 2, g.Fishing.SlotWeight(sp, 0), 6);
            Assert.Equal(sp.Slot[4], g.Fishing.SlotWeight(sp, 3), 6);
        }

        [Fact]
        public void 밤에는_밤고기가_잘_문다()
        {
            var d = TestData.Full;
            var galchi = d.SpeciesById("galchi");
            var hakgongchi = d.SpeciesById("hakgongchi");
            Assert.True(galchi.Slot[4] > galchi.Slot[2]);
            var g = At(3);
            Assert.True(g.Fishing.SlotWeight(galchi, 3) > g.Fishing.SlotWeight(galchi, 1));
            Assert.True(hakgongchi.Slot[4] < hakgongchi.Slot[2]);
            Assert.Contains("밤", d.SpeciesById("bollak").Hint);
        }
    }
}

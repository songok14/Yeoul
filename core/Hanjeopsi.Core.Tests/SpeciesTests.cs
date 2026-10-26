using System;
using System.IO;
using System.Linq;
using Hanjeopsi.Core.Systems;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>고닷 어종 71종 (FishingPlan 도감): 그림, 제철, 바다, 규정</summary>
    public class SpeciesTests
    {
        private static readonly string[] Seas = { "south", "east", "west", "jeju" };

        private static string FishDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "godot", "art", "fish"))) dir = dir.Parent;
            Assert.NotNull(dir);
            return Path.Combine(dir.FullName, "godot", "art", "fish");
        }

        [Fact]
        public void 모든_어종에_그림과_제철과_바다가_있다()
        {
            var all = TestData.Full.Species;
            Assert.Equal(71, all.Count);
            Assert.Equal(all.Count, all.Select(s => s.Id).Distinct().Count());
            Assert.Equal(all.Count, all.Select(s => s.Name).Distinct().Count());
            string dir = FishDir();
            foreach (var sp in all)
            {
                Assert.True(File.Exists(Path.Combine(dir, sp.Id + ".png")), sp.Id + ".png 이 없다");
                Assert.Equal(12, sp.Months.Length);
                Assert.Contains(sp.Months, w => w >= 1);
                Assert.NotEmpty(sp.Seas);
                Assert.All(sp.Seas, s => Assert.Contains(s, Seas));
                Assert.InRange(sp.Stars, 1, 3);
                Assert.Equal(sp.Stars == 3, sp.Rare);
                Assert.True(sp.MinSize < sp.AvgSize && sp.AvgSize < sp.MaxSize, sp.Id + " 크기");
                Assert.InRange(sp.Strength, 1, 5);
                Assert.Equal(3, sp.Zone.Length);
                Assert.Equal(4, sp.Slot.Length);
            }
        }

        [Fact]
        public void 규정은_FishingPlan_앱_기본표를_따른다()
        {
            var d = TestData.Full;
            Assert.Equal(25, d.SpeciesById("gamseongdom").LimitCm);
            Assert.Equal(new[] { 5, 1, 5, 31 }, d.SpeciesById("gamseongdom").Closed);
            Assert.Equal(35, d.SpeciesById("gwangeo").LimitCm);
            Assert.Equal(new[] { 12, 1, 1, 31 }, d.SpeciesById("dodari").Closed);
            Assert.Equal(new[] { 5, 11, 8, 31 }, d.SpeciesById("jukkumi").Closed);
            Assert.Null(d.SpeciesById("galchi").LimitCm);
            Assert.Equal("쥐노래미", d.SpeciesById("noraemi").Name);
        }

        [Fact]
        public void 첫_장_봄_남해에서_여러_어종이_낚인다()
        {
            var d = TestData.Full;
            var spring = d.Species.Where(s => FishingRoll.LivesIn(s, "south") && (FishingRoll.MonthWeight(s, 4) >= 0.5 || FishingRoll.MonthWeight(s, 5) >= 0.5)).ToList();
            Assert.True(spring.Count >= 20, "봄 남해 제철 어종이 " + spring.Count + "종뿐이다");
            // 동해에만 사는 고기는 1장에 안 낚인다
            var g = Game.NewGame(d, 3);
            var rng = new Util.Mulberry32(9);
            for (int i = 0; i < 3000; i++)
            {
                var sp = g.Fishing.RollSpeciesNear(i % 3, i % 4, 4, "south", 0.5, rng);
                Assert.Contains("south", sp.Seas);
            }
        }
    }
}

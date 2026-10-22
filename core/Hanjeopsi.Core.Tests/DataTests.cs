using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>JSON 데이터가 웹 src/data 와 같은지</summary>
    public class DataTests
    {
        [Fact]
        public void 어종_데이터가_웹과_같다()
        {
            var web = (JArray)TestData.MarketGolden["species"];
            var mine = TestData.Game.Species;
            Assert.Equal(web.Count, mine.Count);
            for (int i = 0; i < web.Count; i++)
            {
                var w = (JObject)web[i];
                var s = mine[i];
                Assert.Equal((string)w["id"], s.Id);
                Assert.Equal((string)w["name"], s.Name);
                Assert.Equal((double)w["minSize"], s.MinSize);
                Assert.Equal((double)w["maxSize"], s.MaxSize);
                Assert.Equal((double)w["avgSize"], s.AvgSize);
                Assert.Equal((int)w["basePrice"], s.BasePrice);
                Assert.Equal((int)w["strength"], s.Strength);
                Assert.Equal((bool)w["rare"], s.Rare);
                Assert.Equal((bool)w["aging"], s.Aging);
                Assert.Equal((double?)w["limitCm"], s.LimitCm);
                Assert.Equal(w["closed"]?.ToObject<int[]>(), s.Closed);
                Assert.Equal(w["zone"].ToObject<double[]>(), s.Zone);
                Assert.Equal(w["slot"].ToObject<double[]>(), s.Slot);
                Assert.Equal((string)w["hint"], s.Hint);
                Assert.Equal((string)w["desc"], s.Desc);
            }
        }

        [Fact]
        public void 장비_데이터가_웹과_같다()
        {
            var web = TestData.MarketGolden["gear"];
            var g = TestData.Game.Gear;
            Assert.Equal(web["rods"].Select(r => (double)r["reach"]), g.Rods.Select(r => r.Reach));
            Assert.Equal(web["rods"].Select(r => (int)r["price"]), g.Rods.Select(r => r.Price));
            Assert.Equal(web["reels"].Select(r => (double)r["drag"]), g.Reels.Select(r => r.Drag));
            Assert.Equal(web["tackles"].Select(r => (int)r["capacity"]), g.Tackles.Select(t => t.Capacity));
            Assert.Equal(web["tackles"].Select(r => (int)r["decay"]), g.Tackles.Select(t => t.Decay));
            Assert.Equal((int)web["bait"]["price"], g.Bait.Price);
            Assert.Equal((int)web["bait"]["amount"], g.Bait.Amount);
        }

        [Fact]
        public void 마을_대사가_웹과_같다()
        {
            // 웹 TownScene GRANDMA_LINES, tapCat
            var d = TestData.Game.Dialogue;
            Assert.Equal(5, d.Grandma.Count);
            Assert.Equal("밥은 먹고 다니나. 맛 몰라도 배는 고프다.", d.Grandma[0]);
            Assert.Equal(new[] { "냐옹", "냐아...", "(하품)", "(꼬리를 흔든다)" }, d.CatIdle);
            Assert.NotEmpty(d.YeoulIdle);
        }

        [Fact]
        public void 대화_장면의_화자와_감정_그림이_있다()
        {
            var d = TestData.Game.Dialogue;
            var portraits = System.IO.Path.Combine(System.AppContext.BaseDirectory, "portraits");
            foreach (var sp in d.Speakers)
            {
                Assert.True(sp.Faces.ContainsKey("default"), sp.Id + " 에 default 그림이 없다");
                Assert.Contains(sp.Side, new[] { "left", "right" });
                foreach (var file in sp.Faces.Values)
                    Assert.True(System.IO.File.Exists(System.IO.Path.Combine(portraits, file + ".png")), "그림이 없다: portraits/" + file + ".png");
            }
            foreach (var scene in new[] { "welcome", "chapterClear", "chapterFail" })
            {
                Assert.True(d.Scenes.ContainsKey(scene), "대화 장면이 없다: " + scene);
                foreach (var line in d.Scenes[scene])
                {
                    Assert.False(string.IsNullOrWhiteSpace(line.Text));
                    if (string.IsNullOrEmpty(line.Speaker)) continue;
                    var sp = d.SpeakerById(line.Speaker);
                    Assert.True(string.IsNullOrEmpty(line.Face) || sp.Faces.ContainsKey(line.Face), scene + ": " + line.Speaker + " 의 감정 " + line.Face + " 이 없다");
                }
            }
            // 아버지 말과 1장 기준은 STORY.md 그대로
            Assert.Contains(d.Scenes["welcome"], l => l.Text.Contains("30일 안에 300만 원"));
        }

        [Fact]
        public void 규정이_FishingPlan_기본표와_같다()
        {
            // FishingPlan core/catches/Limits.kt: 볼락 15cm 이하, 감성돔 25cm 이하 · 5/1~5/31
            var d = TestData.Game;
            Assert.Equal(15, d.SpeciesById("bollak").LimitCm);
            Assert.Null(d.SpeciesById("bollak").Closed);
            Assert.Equal(25, d.SpeciesById("gamseongdom").LimitCm);
            Assert.Equal(new[] { 5, 1, 5, 31 }, d.SpeciesById("gamseongdom").Closed);
            Assert.Null(d.SpeciesById("godeungeo").LimitCm);
        }
    }
}

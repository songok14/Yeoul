using Hanjeopsi.Core.State;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>세이브 읽고 쓰기. 웹 세이브 JSON 을 그대로 읽을 수 있어야 한다</summary>
    public class SaveJsonTests
    {
        [Fact]
        public void 웹_세이브를_읽고_다시_쓰면_같다()
        {
            var run = TestData.ScenarioGolden["runs"][0]["steps"];
            var web = (JObject)run[((JArray)run).Count - 1]["state"];
            var save = SaveJson.FromJson(web.ToString(), TestData.Game);
            Assert.NotNull(save);
            JsonAssert.Same(web, SaveJson.ToJObject(save), "웹 세이브 왕복");
        }

        [Fact]
        public void 옛_세이브에_없는_필드는_기본값()
        {
            var save = SaveJson.FromJson("{\"version\":1,\"seed\":7,\"day\":3,\"money\":5000}", TestData.Game);
            Assert.NotNull(save);
            Assert.Equal(7, save.Seed);
            Assert.Equal(3, save.Day);
            Assert.Equal(5000, save.Money);
            Assert.Equal(3_000_000, save.Debt);
            Assert.Equal(12, save.Bait);
            Assert.Empty(save.Cooler);
            Assert.Null(save.PendingSummary);
        }

        [Fact]
        public void 버전이_다르거나_깨진_세이브는_null()
        {
            Assert.Null(SaveJson.FromJson("{\"version\":2,\"seed\":1}", TestData.Game));
            Assert.Null(SaveJson.FromJson("not json", TestData.Game));
        }

        [Fact]
        public void 새_게임은_첫_의뢰_두_건을_받는다()
        {
            var g = Game.NewGame(TestData.Game, 12345);
            Assert.Equal(2, g.Save.Requests.Count);
            Assert.Equal(20_000, g.Save.Money);
            Assert.Equal(30 - 1, g.DaysLeft());
            Assert.Equal("새벽", g.SlotName());
            Assert.True(g.IsMarketOpen());
            Assert.False(g.IsTruckOpen());
        }

        [Fact]
        public void 빚을_다_갚으면_클리어_기한이_지나면_차압()
        {
            var g = Game.NewGame(TestData.Game, 1);
            g.Save.Money = 3_000_000;
            Assert.Equal(ChapterOutcome.None, g.CheckChapter());
            Assert.Equal(3_000_000, g.Repay(9_999_999));
            Assert.Equal(ChapterOutcome.Clear, g.CheckChapter());
            Assert.Equal(1, g.Save.JournalPages);
            Assert.Equal(ChapterOutcome.None, g.CheckChapter());

            var f = Game.NewGame(TestData.Game, 1);
            f.Save.Day = 31;
            Assert.Equal(ChapterOutcome.Fail, f.CheckChapter());
        }
    }
}

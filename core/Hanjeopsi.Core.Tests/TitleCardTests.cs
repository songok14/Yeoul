using Hanjeopsi.Core.Systems;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>타이틀 챕터 부제</summary>
    public class TitleCardTests
    {
        [Fact]
        public void 일곱_챕터_모두_부제와_지역이_있다()
        {
            var t = TestData.Game.Texts.Title;
            Assert.Equal("여울호", t.Name);
            Assert.Equal(7, t.Chapters.Count);
            foreach (var c in t.Chapters)
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Place));
                Assert.False(string.IsNullOrWhiteSpace(c.Subtitle));
            }
            Assert.False(string.IsNullOrWhiteSpace(t.AfterEnding));
        }

        [Fact]
        public void 새_게임과_1장은_1장_부제()
        {
            var data = TestData.Game;
            var t = data.Texts.Title;
            Assert.Equal("짠맛이 돌아온 봄", TitleCard.Subtitle(t, null));
            Assert.Equal("짠맛이 돌아온 봄", TitleCard.Subtitle(t, Game.CreateSave(data, 1)));
            Assert.Equal("1장 물비늘포", TitleCard.Foot(t, 1));
        }

        [Fact]
        public void 챕터마다_부제가_바뀌고_범위_밖은_맞춘다()
        {
            var t = TestData.Game.Texts.Title;
            Assert.Equal("날짜가 틀린 일지", TitleCard.Subtitle(t, 6, false));
            Assert.Equal("물그림자", TitleCard.Subtitle(t, 7, false));
            Assert.Equal("물그림자", TitleCard.Subtitle(t, 9, false));
            Assert.Equal("짠맛이 돌아온 봄", TitleCard.Subtitle(t, 0, false));
            Assert.Equal("6장 제주", TitleCard.Foot(t, 6));
        }

        [Fact]
        public void 트루_엔딩_뒤에는_따로_정한_부제()
        {
            var data = TestData.Game;
            var save = Game.CreateSave(data, 1);
            save.Flags[TitleCard.TrueEndingFlag] = true;
            Assert.Equal("제일 맛있게 먹어주는 사람", TitleCard.Subtitle(data.Texts.Title, save));
        }
    }
}

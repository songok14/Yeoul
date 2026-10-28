using System.Linq;
using Hanjeopsi.Core.State;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>1장 주간 상환 (고닷 weekly.json)</summary>
    public class WeekTests
    {
        private static Game New()
        {
            var g = Game.NewGame(TestData.Full, 9);
            g.CompletePrologue();
            return g;
        }

        private static void ToEndOf(Game g, int day)
        {
            while (g.Save.Day <= day && g.CheckChapter() == ChapterOutcome.None) g.EndDay();
        }

        [Fact]
        public void 첫_주는_이자가_붙고_원금_40만을_갚는다()
        {
            var g = New();
            Assert.True(g.IsWeekly);
            Assert.Equal(1, g.Save.Week.No);
            Assert.Equal(90_000, g.Save.Week.Interest);
            Assert.Equal(3_090_000, g.Save.Debt);
            Assert.Equal(490_000, g.DueNow());
            Assert.Equal(6, g.DaysToDue());
            Assert.Equal(27, g.DaysLeft());
        }

        [Fact]
        public void 제때_갚으면_다음_주로_넘어간다()
        {
            var g = New();
            g.Save.Money = 600_000;
            g.Repay(g.DueNow());
            ToEndOf(g, 7);
            var r = g.Save.PendingSummary.Week;
            Assert.True(r.Paid);
            Assert.Equal(2, g.Save.Week.No);
            // 남은 원금 260만의 3%
            Assert.Equal(78_000, g.Save.Week.Interest);
            Assert.Equal(678_000, g.DueNow());
            Assert.Equal(ChapterOutcome.None, g.CheckChapter());
        }

        [Fact]
        public void 한_번_연체는_봐주고_두_번이면_차압()
        {
            var g = New();
            ToEndOf(g, 7);
            var r = g.Save.PendingSummary.Week;
            Assert.False(r.Paid);
            Assert.Equal(490_000, r.Short);
            Assert.Equal(49_000, r.LateFee);
            Assert.False(r.Seized);
            Assert.Equal(ChapterOutcome.None, g.CheckChapter());
            // 못 낸 돈과 연체료가 이번 주 몫에 더해진다
            Assert.True(g.DueNow() > 600_000 + 490_000);
            ToEndOf(g, 14);
            Assert.True(g.Save.PendingSummary.Week.Seized);
            Assert.Equal(ChapterOutcome.Fail, g.CheckChapter());
        }

        [Fact]
        public void 다_갚으면_바로_클리어()
        {
            var g = New();
            g.Save.Money = 4_000_000;
            g.Repay(g.Save.Debt);
            Assert.Equal(ChapterOutcome.Clear, g.CheckChapter());
        }

        [Fact]
        public void 주간_목표를_채우면_보상()
        {
            var g = New();
            int money = g.Save.Money;
            for (int i = 0; i < 15; i++) g.RecordCatch("bollak", 20, false);
            var done = g.CheckGoals();
            Assert.Equal(2, done.Count);
            Assert.Equal(money + 50_000, g.Save.Money);
            Assert.Empty(g.CheckGoals());
            Assert.True(g.GoalDone(0) && g.GoalDone(2) && !g.GoalDone(1));
        }

        [Fact]
        public void 주말_이벤트()
        {
            var g = New();
            g.Save.Slot = 2;
            int guests = g.CustomersNow();
            g.Save.Day = 6;
            Assert.Equal("market", g.EventToday().Id);
            Assert.Equal(guests + 3, g.CustomersNow());
            g.Save.Day = 13;
            Assert.True(g.BigAdd() >= 0.15);
            // 대결: 40cm 넘는 고기를 낚으면 상금
            g.Save.Day = 20;
            g.Save.Week.No = 3;
            g.RecordCatch("nongeo", 52, false);
            int money = g.Save.Money;
            g.EndDay();
            Assert.Equal(52, g.Save.PendingSummary.Rival);
            Assert.Equal(money + 150_000, g.Save.Money);
        }

        [Fact]
        public void 웹_데이터는_30일_한_번_상환()
        {
            var g = Game.NewGame(TestData.Game, 9);
            Assert.False(g.IsWeekly);
            Assert.Null(g.Save.Week);
            Assert.Equal(3_000_000, g.Save.Debt);
            Assert.DoesNotContain("\"week\"", SaveJson.ToJson(g.Save));
        }
    }
}

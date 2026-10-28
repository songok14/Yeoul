using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Xunit;
using Xunit.Abstractions;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>U4: 새 게임부터 30일을 규칙대로 자동으로 돌린다 (웹 scripts/playtest.mjs 의 코어판)</summary>
    public class AutoPlayTests
    {
        private readonly ITestOutputHelper _out;

        public AutoPlayTests(ITestOutputHelper output)
        {
            _out = output;
        }

        /// <summary>행동 하나마다 세이브가 말이 되는지 본다</summary>
        private static void Invariants(Game g, string at)
        {
            var s = g.Save;
            string where = s.Day + "일 " + s.Slot + "칸 " + at + ": ";
            Assert.True(s.Money >= 0, where + "돈이 음수 " + s.Money);
            Assert.True(s.Debt >= 0, where + "빚이 음수");
            Assert.True(s.Bait >= 0, where + "미끼가 음수");
            Assert.InRange(s.Slot, 0, g.Data.Balance.Time.SlotsPerDay - 1);
            Assert.True(s.Cooler.Count <= g.CoolerCapacity(), where + "쿨러가 넘쳤다");
            Assert.Equal(s.Cooler.Count, s.Cooler.Select(f => f.Uid).Distinct().Count());
            Assert.All(s.Cooler, f =>
            {
                Assert.InRange(f.Fresh, 0, 100);
                Assert.True(f.Uid < s.NextUid, where + "uid 가 NextUid 보다 크다");
                // 낚은 날 기준으로 놓아줘야 했던 고기는 쿨러에 없어야 한다
                Assert.Null(g.Rules.ReleaseReason(f.Species, f.Size, f.CaughtDay));
            });
            Assert.True(s.Requests.Count <= g.Data.Balance.Requests.Max, where + "의뢰가 너무 많다");
            Assert.InRange(s.CatAffinity, 0, g.Data.Balance.Cat.MaxAffinity);
            Assert.InRange(s.Rod, 0, g.Data.Gear.Rods.Count - 1);
            Assert.InRange(s.Reel, 0, g.Data.Gear.Reels.Count - 1);
            Assert.InRange(s.Tackle, 0, g.Data.Gear.Tackles.Count - 1);
            Assert.True(s.Money + s.TotalEarned >= 0);
        }

        private PlayResult Run(int seed, PlayerSkill skill)
        {
            var p = new AutoPlayer(TestData.Full, seed, skill, Invariants);
            var r = p.Play();
            // 하루 끝 세이브가 그대로 저장되고 다시 읽히는지
            string json = SaveJson.ToJson(p.Game.Save);
            var back = SaveJson.FromJson(json, TestData.Full);
            Assert.Equal(json, SaveJson.ToJson(back));
            return r;
        }

        private void Report(string title, IList<PlayResult> rs)
        {
            _out.WriteLine(title);
            foreach (var r in rs)
                _out.WriteLine(string.Format(
                    "  시드 {0,3}: {1,-5} {2,2}일  빚 {3,9:N0}  번 돈 {4,9:N0} (경매 {5:N0}, 트럭 {6:N0}, 의뢰 {7:N0})  장비 {8:N0}  캐스팅 {9} 낚음 {10} 터짐 {11} 빠짐 {12} 놓침 {13}  요리 {14} 손님 {16} VIP {17}/{18}  {15}",
                    r.Seed, r.Outcome, r.ClearDay, r.Debt, r.TotalEarned, r.AuctionIncome, r.TruckIncome, r.RequestIncome, r.GearSpent,
                    r.Casts, r.Landed, r.Snapped, r.Slipped, r.MissedBites, r.Dishes, r.Gear, r.Guests, r.VipServed, r.VipServed + r.VipMissed));
            double casts = rs.Sum(r => r.Casts), fights = rs.Sum(r => r.Fights);
            _out.WriteLine(string.Format("  평균: 가까움 {0:0.00}, 큰 놈 {1:0.0}%, 기다림 {2:0.0}초, 힘싸움 {3:0.0}초, 질주 {6:0.0}번(버팀 {7:0}%), 도둑 {4}번 중 {5}번 뺏김",
                rs.Sum(r => r.Closeness) / casts, 100 * rs.Sum(r => r.BigBites) / casts, rs.Sum(r => r.WaitTime) / casts,
                rs.Sum(r => r.FightTime) / Math.Max(1, fights), rs.Sum(r => r.Thieves), rs.Sum(r => r.Stolen),
                (double)rs.Sum(r => r.Runs) / Math.Max(1, fights), 100.0 * rs.Sum(r => r.RunsHeld) / Math.Max(1, rs.Sum(r => r.Runs))));
        }

        [Fact]
        public void 적당히_잘하면_30일_안에_300만_원을_갚는다()
        {
            var rs = Enumerable.Range(1, 20).Select(seed => Run(seed, PlayerSkill.Decent())).ToList();
            Report("적당히 잘하는 플레이", rs);
            int clear = rs.Count(r => r.Outcome == ChapterOutcome.Clear);
            _out.WriteLine("클리어 " + clear + "/20, 평균 클리어 날 " + rs.Where(r => r.ClearDay > 0).Select(r => r.ClearDay).DefaultIfEmpty(0).Average());
            foreach (var line in rs[0].DayLog) _out.WriteLine("  " + line);
            Assert.True(clear >= 18, "적당히 잘해도 갚기 어렵다: " + clear + "/20");
        }

        [Fact]
        public void 초보도_30일_동안_예외_없이_돌아간다()
        {
            var rs = Enumerable.Range(101, 20).Select(seed => Run(seed, PlayerSkill.Beginner())).ToList();
            Report("초보 플레이", rs);
            _out.WriteLine("클리어 " + rs.Count(r => r.Outcome == ChapterOutcome.Clear) + "/20");
            Assert.All(rs, r => Assert.NotEqual(ChapterOutcome.None, r.Outcome));
        }
    }
}

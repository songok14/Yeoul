using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>2026-10-26 폰 의견: 시세판 순서, 방파제 어종, 지친 물고기, 릴별 게이지, 낚싯대별 거리</summary>
    public class FeedbackTests
    {
        [Fact]
        public void 시세판은_내_쿨러_어종이_먼저_나온다()
        {
            var g = Game.NewGame(TestData.Full, 3);
            g.Save.Cooler.Add(new FishItem(g.Save.NextUid++, "nakji", 20, 1, 100));
            g.Save.Cooler.Add(new FishItem(g.Save.NextUid++, "ssombaengi", 20, 1, 100));
            g.Save.Cooler.Add(new FishItem(g.Save.NextUid++, "ssombaengi", 22, 1, 100));
            var board = g.BoardSpecies().Select(sp => sp.Id).ToList();
            Assert.Equal(5, board.Count);
            Assert.Equal(new[] { "ssombaengi", "nakji" }, board.Take(2));
            Assert.Equal(board.Count, board.Distinct().Count());
        }

        [Fact]
        public void 방파제에서는_배낚시와_갯바위_고기가_안_나온다()
        {
            var g = Game.NewGame(TestData.Full, 3);
            Assert.Equal("breakwater", g.SpotNow());
            Assert.All(TestData.Full.Species, sp => Assert.NotNull(sp.Spots));
            var rng = new Mulberry32(9);
            for (int i = 0; i < 3000; i++)
            {
                var sp = g.Fishing.RollSpeciesNear(i % 3, i % 5, 4 + i % 2, g.SeaNow(), rng.Next(), rng, null, g.SpotNow());
                Assert.Contains("breakwater", sp.Spots);
            }
            foreach (var id in new[] { "agwi", "daegu", "samchi", "bangeo", "doldom" })
                Assert.DoesNotContain(g.InSeason(), sp => sp.Id == id);
            // 요리, 의뢰, 미끼, 통발에 쓰는 고기는 방파제 고기다
            var d = TestData.Full;
            bool AtBreakwater(string id) => d.SpeciesById(id).Spots.Contains("breakwater");
            Assert.All(d.Recipes.Recipes, r => Assert.True(AtBreakwater(r.Species), r.Species));
            Assert.All(d.RequestTemplates, r => Assert.True(AtBreakwater(r.Species), r.Species));
            Assert.All(d.Gear.Baits.SelectMany(b => b.Likes), id => Assert.True(AtBreakwater(id), id));
            Assert.All(d.Gear.TrapPool, id => Assert.True(AtBreakwater(id), id));
        }

        [Fact]
        public void 지친_물고기는_텐션이_거의_안_오르고_쭉_끌려온다()
        {
            var g = Game.NewGame(TestData.Full, 3);
            var sp = TestData.Full.SpeciesById("bollak");
            var fight = g.Spot.StartTug(g.Fight(sp, sp.AvgSize, 0.6, false, false), new Mulberry32(4));
            const double dt = 1.0 / 30;
            double t0 = fight.Tension;
            for (int i = 0; i < 6; i++) fight.Step(true, 1, dt);
            double freshRise = (fight.Tension - t0) / (6 * dt);
            // 반대쪽을 눌러 버티고 알맞게 감아 지치게 만든다
            for (int i = 0; i < 3000 && !fight.Tired && fight.Ended == FightEnd.None; i++)
            {
                bool hold = fight.Phase == TugPhase.Run || fight.Tension < 60;
                fight.Step(hold, fight.Phase == TugPhase.Run ? -fight.RunDir : 1, dt);
            }
            Assert.True(fight.Tired);
            Assert.Equal(FightEnd.None, fight.Ended);
            double t1 = fight.Tension, d1 = fight.Distance;
            for (int i = 0; i < 6; i++) fight.Step(true, 1, dt);
            double tiredRise = (fight.Tension - t1) / (6 * dt);
            Assert.True(tiredRise < freshRise * 0.4, tiredRise + " / " + freshRise);
            Assert.True(d1 - fight.Distance > 6 * dt * g.EffectiveReel().Speed, "지친 물고기는 감는 만큼 다가온다");
        }

        [Fact]
        public void 좋은_릴일수록_초록_구간이_넓다()
        {
            var reels = TestData.Full.Gear.Reels;
            for (int i = 1; i < reels.Count; i++)
            {
                Assert.True(reels[i].Slack <= reels[i - 1].Slack);
                Assert.True(reels[i].Danger >= reels[i - 1].Danger);
                Assert.True(reels[i].Danger - reels[i].Slack > reels[i - 1].Danger - reels[i - 1].Slack);
            }
            Assert.All(reels, r => Assert.InRange(r.Danger, r.Slack + 40, TestData.Full.Balance.Reel.Snap - 1));
        }

        [Fact]
        public void 노씨_부탁은_들어주기_전까지_늘_들어와_있다()
        {
            var g = Game.NewGame(TestData.Full, 3);
            int favor = TestData.Full.RequestTemplates.FindIndex(t => t.Flag == "nossiFavor");
            for (int day = 0; day < 5; day++)
            {
                g.Save.Requests.Clear();
                g.Save.Day += 1;
                g.RefreshRequests();
                Assert.Contains(g.Save.Requests, r => r.Template == favor);
            }
        }

        [Fact]
        public void 한_사람이_의뢰_둘을_들고_오지_않는다()
        {
            // 2026-10-27 폰 의견: 민박집 할머니가 마을 양옆에 둘이 섰다
            var g = Game.NewGame(TestData.Full, 3);
            var t = TestData.Full.RequestTemplates;
            g.Save.Flags["nossiFavor"] = true; // 노씨 부탁이 한 자리를 늘 차지하지 않게
            int pairs = 0;
            for (int day = 0; day < 200; day++)
            {
                g.Save.Requests.Clear();
                g.Save.Day = 1 + day;
                g.Save.Seed = 1000 + day;
                g.RefreshRequests();
                var clients = g.Save.Requests.Select(r => t[r.Template].Client).ToList();
                Assert.Equal(clients.Count, clients.Distinct().Count());
                if (clients.Count == 2) pairs++;
            }
            Assert.True(pairs > 150);
        }

        [Fact]
        public void 좋은_낚싯대일수록_멀리_던진다()
        {
            var rods = TestData.Full.Gear.Rods;
            for (int i = 1; i < rods.Count; i++) Assert.True(rods[i].Reach > rods[i - 1].Reach);
            Assert.InRange(rods[0].Reach, TestData.Full.Balance.Fishing.MidBelow, 1);
            Assert.Equal(1, rods[rods.Count - 1].Reach);
        }
    }
}

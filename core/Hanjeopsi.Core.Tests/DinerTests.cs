using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>여울호 등급과 손님 (고닷 diner.json)</summary>
    public class DinerTests
    {
        private static Game New(int slot = 1)
        {
            var g = Game.NewGame(TestData.Full, 11);
            g.Save.Slot = slot;
            return g;
        }

        private static List<SeasonResult> Perfect(Hanjeopsi.Core.Data.Recipe r)
        {
            return r.Steps.Select(_ => new SeasonResult(1, 0)).ToList();
        }

        [Fact]
        public void 손님은_모두_대사와_그림이_있고_좋아하는_요리가_있다()
        {
            var d = TestData.Full;
            var ids = d.Dialogue.Customers.Select(c => c.Id).ToList();
            Assert.Equal(ids.OrderBy(x => x), d.Diner.Guests.Select(g => g.Id).OrderBy(x => x));
            foreach (var g in d.Diner.Guests)
            {
                Assert.NotEmpty(g.Slots);
                Assert.All(g.Likes, id => d.RecipeById(id));
                Assert.Contains("{0}", g.Regular);
            }
            // 트럭이 여는 시간대마다 손님이 있다
            for (int slot = d.Balance.Time.TruckFirstSlot; slot < d.Balance.Time.SlotsPerDay; slot++)
                Assert.NotEmpty(new Diner(d.Diner).GuestsAt(slot));
        }

        [Fact]
        public void 처음엔_포장마차_메뉴_두_칸()
        {
            var g = New();
            Assert.Equal("포장마차", g.Diner.Rank(g.Save).Name);
            Assert.Equal(2, g.Diner.MenuSlots(g.Save));
            Assert.Equal(g.Data.Diner.Ranks[0].Guests, g.CustomersNow());
        }

        [Fact]
        public void 평판이_쌓이면_등급이_오르고_한_번만_알린다()
        {
            var g = New();
            var next = g.Data.Diner.Ranks[1];
            g.Save.Diner.Rep = next.Rep;
            var up = g.CheckDinerRankUp();
            Assert.Equal(next.Name, up.Name);
            Assert.Null(g.CheckDinerRankUp());
            Assert.Equal(next.Menu, g.Diner.MenuSlots(g.Save));
            Assert.True(g.CustomersNow() > g.Data.Diner.Ranks[0].Guests);
        }

        [Fact]
        public void 만족한_손님은_친해지고_단골이_되면_꼭_온다()
        {
            var g = New(1);
            var recipe = g.Data.RecipeById("ureok-maeun");
            int before = g.CustomersNow();
            GuestResult last = null;
            for (int i = 0; i < 6 && !g.Diner.IsRegular(g.Save, "fisher"); i++)
            {
                g.AddToCooler("ureok", 30);
                last = g.ServeGuest(recipe, Perfect(recipe), "fisher").Guest;
                Assert.True(last.Favorite);
                Assert.True(last.RepGain > 0);
            }
            Assert.True(last.BecameRegular);
            Assert.True(g.Diner.IsRegular(g.Save, "fisher"));
            Assert.Equal(before + 1, g.CustomersNow());
            var plan = g.Diner.PlanGuests(g.Save, 1, new Mulberry32(3));
            Assert.Contains("fisher", plan);
            // 단골은 자기 시간대(아침·밤)에만 손님을 늘린다
            g.Save.Slot = 2;
            Assert.Empty(g.Diner.RegularsAt(g.Save, 2));
        }

        [Fact]
        public void 실망한_손님은_평판과_친밀도를_깎는다()
        {
            var g = New(1);
            var recipe = g.Data.RecipeById("bollak-gui");
            g.Save.Diner.Rep = 10;
            g.Save.Diner.Affinity["mail"] = 2;
            g.AddToCooler("bollak", 20);
            var r = g.ServeGuest(recipe, recipe.Steps.Select(_ => new SeasonResult(0, 1)).ToList(), "mail").Guest;
            Assert.True(r.RepGain < 0);
            Assert.Equal(1, g.Diner.AffinityOf(g.Save, "mail"));
            Assert.True(g.Save.Diner.Rep < 10);
        }

        [Fact]
        public void 손님은_메뉴_안에서_좋아하는_요리를_먼저_시킨다()
        {
            var g = New();
            var menu = new[] { g.Data.RecipeById("bollak-gui"), g.Data.RecipeById("ureok-maeun") };
            var rng = new Mulberry32(5);
            int fav = Enumerable.Range(0, 200).Count(_ => g.Diner.Order("fisher", menu, rng).Id == "ureok-maeun");
            Assert.InRange(fav, 140, 200);
            Assert.All(Enumerable.Range(0, 50), _ => Assert.Contains(g.Diner.Order("student", menu, rng), menu));
        }

        [Fact]
        public void 손님_차례는_잇달아_같은_손님이_없다()
        {
            var g = New(2);
            var rng = new Mulberry32(9);
            for (int k = 0; k < 50; k++)
            {
                var plan = g.Diner.PlanGuests(g.Save, 2, rng);
                Assert.Equal(g.CustomersNow(), plan.Count);
                for (int i = 1; i < plan.Count; i++) Assert.NotEqual(plan[i - 1], plan[i]);
            }
        }

        [Fact]
        public void 납품_의뢰를_마치면_평판이_오른다()
        {
            var g = New();
            var req = g.Save.Requests[0];
            var t = g.Data.RequestTemplates[req.Template];
            for (int i = 0; i < t.Count; i++) g.AddToCooler(t.Species, g.Data.SpeciesById(t.Species).MaxSize - 1);
            int rep = g.Save.Diner.Rep;
            Assert.True(g.DeliverRequest(req.Id) > 0);
            Assert.Equal(rep + g.Data.Diner.RepRequest, g.Save.Diner.Rep);
        }

        [Fact]
        public void 메뉴_추천은_메뉴_칸만큼_만들_수_있는_요리만()
        {
            var g = New();
            foreach (var sp in new[] { "bollak", "godeungeo", "noraemi", "ureok" }) g.AddToCooler(sp, g.Data.SpeciesById(sp).AvgSize + 2);
            var menu = g.SuggestMenu();
            Assert.Equal(2, menu.Count);
            Assert.All(menu, r => Assert.NotNull(g.Truck.BestFishFor(g.Save, r)));
        }

        [Fact]
        public void 웹_세이브에는_여울호_칸이_생기지_않는다()
        {
            var g = Game.NewGame(TestData.Game, 3);
            Assert.Null(g.Diner);
            Assert.Null(g.Save.Diner);
            Assert.DoesNotContain("diner", SaveJson.ToJson(g.Save));
            var full = Game.NewGame(TestData.Full, 3);
            full.Save.Diner.Rep = 17;
            var back = SaveJson.FromJson(SaveJson.ToJson(full.Save), TestData.Full);
            Assert.Equal(17, back.Diner.Rep);
        }
    }
}

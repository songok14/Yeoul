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
        private static Game New(int slot = 3)
        {
            var g = Game.NewGame(TestData.Full, 11);
            g.Save.Slot = slot;
            return g;
        }

        private static List<SeasonResult> Perfect(Hanjeopsi.Core.Data.Recipe r)
        {
            return r.Steps.Select(_ => new SeasonResult(1, 0)).ToList();
        }

        private static PlannedGuest Guest(string id, GuestKind kind = GuestKind.Normal, string wish = null)
        {
            return new PlannedGuest { Id = id, Kind = kind, Wish = wish };
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
            // VIP 는 손님 그림을 빌려 쓰고, 바라는 요리가 모두 있다
            foreach (var v in d.Diner.Vips)
            {
                Assert.Contains(v.Art, ids);
                Assert.NotEmpty(v.Wants);
                Assert.All(v.Wants, id => d.RecipeById(id));
                Assert.Contains("{0}", v.Order);
                Assert.True(v.Rep > 0 && v.MissRep < 0 && v.TipMul > 0);
            }
            // 특별 손님은 대화 일러스트와 반응 셋이 있다
            foreach (var sp in d.Diner.Specials)
            {
                var speaker = d.Dialogue.SpeakerById(sp.Speaker);
                Assert.Equal(3, sp.Lines.Count);
                Assert.Equal(3, sp.Rep.Count);
                Assert.All(sp.Faces.Append(sp.Face), f => Assert.True(speaker.Faces.ContainsKey(f), sp.Id + " 표정 없음: " + f));
                Assert.Contains(d.Dialogue.Beats, b => b.Scene == sp.After);
            }
            Assert.Equal(d.Diner.Ranks.Count, d.Diner.VipChance.Count);
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
        public void 명성이_쌓이면_손님이_한_명씩_는다()
        {
            var g = New();
            var r0 = g.Data.Diner.Ranks[0];
            var r1 = g.Data.Diner.Ranks[1];
            int last = g.CustomersNow();
            for (int rep = 0; rep <= r1.Rep; rep += 5)
            {
                g.Save.Diner.Rep = rep;
                int n = g.CustomersNow();
                Assert.InRange(n - last, 0, 1);
                last = n;
            }
            Assert.Equal(r1.Guests, g.CustomersNow());
            // 등급 중간이면 손님 수도 중간
            g.Save.Diner.Rep = (r0.Rep + r1.Rep) / 2;
            Assert.Equal((r0.Guests + r1.Guests) / 2, g.CustomersNow());
            g.Save.Diner.Rep = 10_000;
            Assert.Equal(g.Data.Diner.Ranks.Last().Guests, g.CustomersNow());
            Assert.True(g.CustomersNow() <= g.Data.Diner.MaxGuests);
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
        public void 만족한_손님은_친해지고_단골이_되면_꼭_오고_팁을_준다()
        {
            var g = New();
            var recipe = g.Data.RecipeById("ureok-maeun");
            GuestResult last = null;
            for (int i = 0; i < 6 && !g.Diner.IsRegular(g.Save, "fisher"); i++)
            {
                g.AddToCooler("ureok", 30);
                last = g.ServeGuest(recipe, Perfect(recipe), Guest("fisher")).Guest;
                Assert.True(last.Favorite);
                Assert.True(last.RepGain > 0);
                Assert.Equal(0, last.Tip);
            }
            Assert.True(last.BecameRegular);
            Assert.True(g.Diner.IsRegular(g.Save, "fisher"));
            var plan = g.Diner.PlanGuests(g.Save, new Mulberry32(3));
            Assert.Contains(plan, p => p.Id == "fisher" && p.Kind == GuestKind.Regular);
            Assert.Single(plan, p => p.Id == "fisher");
            // 단골은 보통 이상이면 요리값의 일부를 팁으로 준다
            g.AddToCooler("ureok", 30);
            int money = g.Save.Money;
            var dish = g.ServeGuest(recipe, Perfect(recipe), Guest("fisher", GuestKind.Regular));
            Assert.Equal(GuestKind.Regular, dish.Guest.Kind);
            Assert.True(dish.Guest.Tip > 0);
            Assert.Equal(money + dish.Pay + dish.Guest.Tip, g.Save.Money);
        }

        [Fact]
        public void 단골이_많으면_단골_수만큼_온다()
        {
            var g = New();
            foreach (var gu in g.Data.Diner.Guests) g.Save.Diner.Affinity[gu.Id] = g.Data.Diner.RegularAt;
            Assert.Equal(System.Math.Min(g.Data.Diner.MaxGuests, g.Data.Diner.Guests.Count), g.CustomersNow());
            var plan = g.PlanGuests(new Mulberry32(1));
            Assert.All(plan.Where(p => p.Kind != GuestKind.Vip && p.Kind != GuestKind.Special), p => Assert.Equal(GuestKind.Regular, p.Kind));
        }

        [Fact]
        public void 실망한_손님은_평판과_친밀도를_깎는다()
        {
            var g = New();
            var recipe = g.Data.RecipeById("bollak-gui");
            g.Save.Diner.Rep = 10;
            g.Save.Diner.Affinity["mail"] = 2;
            g.AddToCooler("bollak", 20);
            var r = g.ServeGuest(recipe, recipe.Steps.Select(_ => new SeasonResult(0, 1)).ToList(), Guest("mail")).Guest;
            Assert.True(r.RepGain < 0);
            Assert.Equal(1, g.Diner.AffinityOf(g.Save, "mail"));
            Assert.True(g.Save.Diner.Rep < 10);
        }

        [Fact]
        public void 손님은_메뉴_안에서_좋아하는_요리를_먼저_시키고_단골은_늘_시킨다()
        {
            var g = New();
            var menu = new[] { g.Data.RecipeById("bollak-gui"), g.Data.RecipeById("ureok-maeun") };
            var rng = new Mulberry32(5);
            int fav = Enumerable.Range(0, 200).Count(_ => g.Diner.Order(Guest("fisher"), menu, rng).Id == "ureok-maeun");
            Assert.InRange(fav, 140, 200);
            Assert.All(Enumerable.Range(0, 50), _ => Assert.Equal("ureok-maeun", g.Diner.Order(Guest("fisher", GuestKind.Regular), menu, rng).Id));
            Assert.All(Enumerable.Range(0, 50), _ => Assert.Contains(g.Diner.Order(Guest("student"), menu, rng), menu));
        }

        [Fact]
        public void 손님_차례는_잇달아_같은_손님이_없다()
        {
            var g = New();
            var rng = new Mulberry32(9);
            for (int k = 0; k < 50; k++)
            {
                var plan = g.Diner.PlanGuests(g.Save, rng);
                Assert.Equal(g.CustomersNow(), plan.Count);
                for (int i = 1; i < plan.Count; i++) Assert.NotEqual(plan[i - 1].Id, plan[i].Id);
            }
        }

        [Fact]
        public void VIP_예약은_등급이_올라야_들어오고_같은_날은_늘_같다()
        {
            var g = New();
            for (int day = 1; day <= 28; day++)
            {
                g.Save.Day = day;
                Assert.Empty(g.VipsToday());
            }
            g.Save.Diner.Rep = g.Data.Diner.Ranks[3].Rep;
            int booked = 0, two = 0;
            for (int day = 1; day <= 28; day++)
            {
                g.Save.Day = day;
                var a = g.VipsToday();
                var b = g.VipsToday();
                Assert.Equal(a.Select(v => v.Vip.Id + v.Recipe.Id), b.Select(v => v.Vip.Id + v.Recipe.Id));
                Assert.InRange(a.Count, 0, 2);
                Assert.Equal(a.Count, a.Select(v => v.Vip.Id).Distinct().Count());
                Assert.All(a, v => Assert.Contains(v.Recipe.Id, v.Vip.Wants));
                booked += a.Count > 0 ? 1 : 0;
                two += a.Count == 2 ? 1 : 0;
            }
            Assert.InRange(booked, 16, 28);
            Assert.True(two > 0);
            // 예약한 VIP 는 손님 차례에 더해진다 (첫 손님은 아니다)
            var plan = g.PlanGuests(new Mulberry32(2));
            Assert.Equal(g.CustomersNow() + g.VipsToday().Count, plan.Count);
            Assert.NotEqual(GuestKind.Vip, plan[0].Kind);
        }

        [Fact]
        public void VIP_는_바란_요리를_바란_신선도로_내면_큰_팁과_명성()
        {
            var g = New();
            var vip = g.Data.Diner.Vips.First(v => v.Fresh == 0);
            var recipe = g.Data.RecipeById(vip.Wants[0]);
            var guest = Guest(vip.Id, GuestKind.Vip, recipe.Id);
            // 메뉴에 없어도 바란 요리를 시킨다
            Assert.Equal(recipe.Id, g.Diner.Order(guest, new[] { g.Data.RecipeById("bollak-gui") }, new Mulberry32(1), g.Data.RecipeById).Id);
            // 선어뿐이면 못 낸다
            g.AddToCooler(recipe.Species, 40);
            g.Save.Cooler.Last().Fresh = 60;
            Assert.Null(g.VipFish(guest));
            g.Save.Diner.Rep = 50;
            Assert.Equal(vip.MissRep, g.MissVip(guest));
            Assert.Equal(50 + vip.MissRep, g.Save.Diner.Rep);
            // 활어가 있으면 그걸 쓴다
            g.AddToCooler(recipe.Species, 30);
            var live = g.VipFish(guest);
            Assert.NotNull(live);
            int rep = g.Save.Diner.Rep;
            int money = g.Save.Money;
            var dish = g.ServeGuest(recipe, Perfect(recipe), guest);
            Assert.True(dish.Guest.VipOk);
            Assert.Equal(FreshStage.Live, dish.Stage);
            Assert.DoesNotContain(g.Save.Cooler, f => f.Uid == live.Uid);
            Assert.Equal(JsMath.Round100(dish.Pay * vip.TipMul), dish.Guest.Tip);
            Assert.Equal(money + dish.Pay + dish.Guest.Tip, g.Save.Money);
            Assert.Equal(rep + vip.Rep, g.Save.Diner.Rep);
        }

        [Fact]
        public void 차세라는_첫_만남_뒤_저녁에_한_번_온다()
        {
            var g = New();
            var sp = g.Data.Diner.Specials.First(s => s.Id == "sera");
            g.Save.Day = sp.Day;
            Assert.Null(g.SpecialToday());
            g.MarkBeat(sp.After);
            Assert.Equal("sera", g.SpecialToday().Id);
            var plan = g.PlanGuests(new Mulberry32(4));
            Assert.Equal(GuestKind.Special, plan.Last().Kind);
            // 메뉴에서 제일 비싼 요리를 시킨다
            var menu = new[] { g.Data.RecipeById("bollak-gui"), g.Data.RecipeById("ureok-maeun") };
            var recipe = g.Diner.Order(plan.Last(), menu, new Mulberry32(1));
            Assert.Equal(menu.OrderByDescending(r => r.Price).First().Id, recipe.Id);
            g.AddToCooler(recipe.Species, 30);
            int rep = g.Save.Diner.Rep;
            var dish = g.ServeGuest(recipe, Perfect(recipe), plan.Last());
            Assert.Equal(GuestKind.Special, dish.Guest.Kind);
            Assert.Equal(rep + sp.Rep[2], g.Save.Diner.Rep);
            Assert.Null(g.SpecialToday());
            g.Save.Day = sp.Until + 1;
            Assert.Null(g.SpecialToday());
        }

        [Fact]
        public void 납품_의뢰를_마치면_평판이_오른다()
        {
            var g = New();
            var req = g.Save.Requests[0];
            var t = g.Data.RequestTemplates[req.Template];
            for (int i = 0; i < t.Count; i++) g.AddToCooler(t.Species, g.Data.SpeciesById(t.Species).MaxSize - 1);
            int rep = g.Save.Diner.Rep;
            // 맡기 전에는 건넬 수 없다
            Assert.Equal(0, g.DeliverRequest(req.Id));
            Assert.True(g.AcceptRequest(req.Id));
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

using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>요리와 간 맞추기 (웹 systems/truck.ts, season.ts)</summary>
    public class TruckTests
    {
        private static Truck NewTruck()
        {
            var d = TestData.Game;
            var f = new Freshness(d);
            return new Truck(d, f, new Taste(d));
        }

        [Fact]
        public void 간_맞추기_판정이_웹과_같다()
        {
            var b = TestData.Game.Balance.Season;
            foreach (JObject c in TestData.RulesGolden["judge"])
            {
                var r = SeasonJudge.Judge(b, (double)c["pos"], (double)c["target"], (double)c["half"]);
                Assert.Equal((double)c["quality"], r.Quality);
                Assert.Equal((int)c["side"], r.Side);
            }
        }

        [Fact]
        public void 요리_신선도_배율이_웹과_같다()
        {
            var t = NewTruck();
            foreach (JObject c in TestData.RulesGolden["dish"])
                Assert.Equal((double)c["value"], t.DishFresh(TestData.Game.RecipeById((string)c["recipe"]), Freshness.ParseKey((string)c["stage"])));
        }

        [Fact]
        public void 요리값과_기분이_웹과_같다()
        {
            var t = NewTruck();
            foreach (JObject c in TestData.RulesGolden["pay"])
            {
                var recipe = TestData.Game.RecipeById((string)c["recipe"]);
                double avg = (double)c["avg"];
                Assert.Equal((int)c["pay"], t.DishPay(recipe, Freshness.ParseKey((string)c["stage"]), avg));
                Assert.Equal((int)c["mood"], t.MoodOf(avg));
            }
        }

        [Fact]
        public void 손님_한마디가_웹과_같다()
        {
            var data = TestData.Game;
            var t = NewTruck();
            var taste = new Taste(data);
            string[] flavors = { "salty", "sweet", "umami", "sour", "bitter" };
            foreach (JObject c in TestData.RulesGolden["reviews"])
            {
                var save = Game.CreateSave(data, 1);
                foreach (var p in ((JObject)c["flags"]).Properties()) save.Flags[p.Name] = (bool)p.Value;
                int caught = (int)c["caught"];
                for (int i = 0; i < caught; i++) save.Dex[data.Species[i].Id] = new DexEntry { Count = 1, Best = 20 };
                var recipe = data.RecipeById((string)c["recipe"]);
                var results = c["results"].Select(r => new SeasonResult((double)r["quality"], (int)r["side"])).ToList();
                var lines = t.ReviewLines(save, recipe, results, Freshness.ParseKey((string)c["stage"]), (int)c["mood"]);
                Assert.Equal(c["lines"].ToObject<string[]>(), lines.ToArray());
                Assert.Equal((int)c["tasteLevel"], taste.Level(save));
                Assert.Equal(c["canTaste"].ToObject<bool[]>(), flavors.Select(f => taste.CanTaste(save, f)).ToArray());
            }
        }

        [Fact]
        public void 재료_고르기가_웹과_같다()
        {
            var data = TestData.Game;
            var t = NewTruck();
            foreach (JObject c in TestData.RulesGolden["best"])
            {
                var save = Game.CreateSave(data, 1);
                save.Cooler = c["cooler"].Select(f => new FishItem((int)f["uid"], (string)f["species"], (double)f["size"], 1, (int)f["fresh"])).ToList();
                var best = data.Recipes.Recipes.Select(r => t.BestFishFor(save, r)?.Uid).ToArray();
                Assert.Equal(c["best"].ToObject<int?[]>(), best);
                Assert.Equal(c["cookable"].ToObject<string[]>(), t.Cookable(save).Select(r => r.Id).ToArray());
            }
        }

        [Fact]
        public void 레시피와_의뢰_데이터가_웹과_같다()
        {
            var web = TestData.RulesGolden["data"];
            var d = TestData.Game;
            var recipes = (JArray)web["recipes"];
            Assert.Equal(recipes.Count, d.Recipes.Recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = d.Recipes.Recipes[i];
                Assert.Equal((string)recipes[i]["id"], r.Id);
                Assert.Equal((int)recipes[i]["price"], r.Price);
                Assert.Equal((bool)recipes[i]["raw"], r.Raw);
                Assert.Equal(recipes[i]["steps"].Select(s => (string)s["flavor"] + (double)s["target"]), r.Steps.Select(s => s.Flavor + s.Target));
            }
            foreach (JObject f in web["flavors"])
            {
                var mine = d.FlavorById((string)f["id"]);
                Assert.Equal((string)f["name"], mine.Name);
                Assert.Equal((string)f["tool"], mine.Tool);
                Assert.Equal(f["review"].ToObject<string[]>(), mine.Review);
            }
            var reqs = (JArray)web["requests"];
            Assert.Equal(reqs.Count, d.RequestTemplates.Count);
            for (int i = 0; i < reqs.Count; i++)
            {
                var q = d.RequestTemplates[i];
                Assert.Equal((string)reqs[i]["client"], q.Client);
                Assert.Equal((string)reqs[i]["text"], q.Text);
                Assert.Equal((string)reqs[i]["species"], q.Species);
                Assert.Equal((int)reqs[i]["count"], q.Count);
                Assert.Equal((double)reqs[i]["mul"], q.Mul);
                // 웹은 client === '노씨' 로 확인한다. 유니티는 데이터의 flag 로
                Assert.Equal((string)reqs[i]["client"] == "노씨", q.Flag == "nossiFavor");
            }
        }
    }
}

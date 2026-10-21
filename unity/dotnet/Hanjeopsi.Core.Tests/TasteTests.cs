using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>미각 회복 (웹 systems/taste.ts)</summary>
    public class TasteTests
    {
        [Fact]
        public void 단계와_해금이_웹과_같다()
        {
            var data = TestData.Game;
            var taste = new Taste(data);
            SaveData save = null;
            bool? first = null;
            foreach (JObject c in TestData.RulesGolden["taste"])
            {
                bool f = (bool)c["first"];
                if (first != f)
                {
                    first = f;
                    save = Game.CreateSave(data, 1);
                    if (f) save.Flags["firstTaste"] = true;
                    // 웹 기록은 어종을 하나씩 늘려 가며 확인한다
                }
                int caught = (int)c["caught"];
                if (caught > 0) save.Dex[data.Species[caught - 1].Id] = new DexEntry { Count = 1, Best = 20 };
                var unlocked = taste.CheckUnlock(save);
                Assert.Equal((string)c["unlocked"], unlocked?.Name);
                Assert.Equal((int)c["level"], taste.Level(save));
                Assert.Equal(caught, taste.SpeciesCaught(save));
                JsonAssert.Same(c["flags"], JObject.FromObject(save.Flags), "미각 플래그");
            }
        }

        [Fact]
        public void 데이터가_웹과_같다()
        {
            var web = (JArray)TestData.RulesGolden["data"]["taste"];
            var mine = TestData.Game.TasteStages;
            Assert.Equal(web.Count, mine.Count);
            for (int i = 0; i < web.Count; i++)
            {
                Assert.Equal((string)web[i]["name"], mine[i].Name);
                Assert.Equal(web[i]["flavors"].ToObject<string[]>(), mine[i].Flavors.ToArray());
                Assert.Equal((int?)web[i]["needSpecies"], mine[i].NeedSpecies);
                Assert.Equal((string)web[i]["effect"], mine[i].Effect);
            }
        }
    }
}

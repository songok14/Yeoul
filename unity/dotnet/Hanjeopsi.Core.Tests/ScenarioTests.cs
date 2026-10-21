using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>
    /// 웹 버전이 새 게임부터 행동을 이어 가며 남긴 기록(scripts/golden/scenario.ts)을
    /// C# 이 똑같이 따라 하고, 매 단계 세이브 전체와 화면 값이 같은지 본다.
    /// </summary>
    public class ScenarioTests
    {
        public static IEnumerable<object[]> Runs()
        {
            var runs = (JArray)TestData.ScenarioGolden["runs"];
            for (int i = 0; i < runs.Count; i++) yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Runs))]
        public void 웹과_같은_판이_된다(int index)
        {
            var run = (JObject)TestData.ScenarioGolden["runs"][index];
            var data = TestData.Game;
            Game game = null;
            int saves = 0;
            int n = 0;
            foreach (JObject step in run["steps"])
            {
                string op = (string)step["op"];
                var args = (JObject)step["args"];
                JToken result = null;
                switch (op)
                {
                    case "new":
                        game = Game.NewGame(data, (int)args["seed"]);
                        game.Saved += _ => saves++;
                        break;
                    case "completePrologue": game.CompletePrologue(); break;
                    case "useBait": game.UseBait(); break;
                    case "buyBait": result = game.BuyBait(); break;
                    case "releaseReason": result = game.ReleaseReason((string)args["species"], (double)args["size"]); break;
                    case "recordCatch": result = game.RecordCatch((string)args["species"], (double)args["size"], (bool)args["released"]); break;
                    case "addToCooler":
                    {
                        var item = game.AddToCooler((string)args["species"], (double)args["size"]);
                        result = item == null ? null : (JToken)item.Uid;
                        break;
                    }
                    case "checkTasteUnlock": result = game.CheckTasteUnlock()?.Name; break;
                    case "sellFish": result = game.SellFish(args["uids"].ToObject<int[]>(), (double)args["mul"]); break;
                    case "serveDish":
                    {
                        var results = args["results"].Select(r => new SeasonResult((double)r["quality"], (int)r["side"])).ToList();
                        var dish = game.ServeDish(data.RecipeById((string)args["recipe"]), results);
                        result = dish == null ? null : new JObject
                        {
                            ["stage"] = Freshness.Key(dish.Stage),
                            ["avg"] = dish.Avg,
                            ["pay"] = dish.Pay,
                            ["firstTime"] = dish.FirstTime,
                        };
                        break;
                    }
                    case "deliverRequest": result = game.DeliverRequest((int)args["id"]); break;
                    case "feedCat": result = game.FeedCat(); break;
                    case "upgradeGear": result = game.UpgradeGear(ParseSlot((string)args["slot"])); break;
                    case "repay": result = game.Repay((int)args["amount"]); break;
                    case "earn": game.Earn((int)args["amount"]); break;
                    case "useSlot": game.UseSlot(); break;
                    case "endDay": game.EndDay(); break;
                    case "chapterOutcome":
                    {
                        var o = game.CheckChapter();
                        result = o == ChapterOutcome.None ? null : (JToken)(o == ChapterOutcome.Clear ? "clear" : "fail");
                        break;
                    }
                    default:
                        throw new Xunit.Sdk.XunitException("모르는 행동: " + op);
                }
                string where = "시드 " + run["seed"] + " " + n + "번째 " + op;
                JsonAssert.Same(step["result"], result, where + " 결과");
                JsonAssert.Same(step["state"], SaveJson.ToJObject(game.Save), where + " 세이브");
                JsonAssert.Same(step["view"], View(game), where + " 화면 값");
                n++;
            }
            Assert.True(n > 300);
            Assert.True(saves > 100);
        }

        private static GearSlot ParseSlot(string s)
        {
            return s == "rod" ? GearSlot.Rod : s == "reel" ? GearSlot.Reel : GearSlot.Tackle;
        }

        private static JObject View(Game g)
        {
            return new JObject
            {
                ["tasteLevel"] = g.TasteLevel(),
                ["speciesCaught"] = g.Taste.SpeciesCaught(g.Save),
                ["daysLeft"] = g.DaysLeft(),
                ["slotName"] = g.SlotName(),
                ["marketOpen"] = g.IsMarketOpen(),
                ["truckOpen"] = g.IsTruckOpen(),
                ["dateLabel"] = g.DateLabel(),
                ["closedToday"] = new JArray(g.ClosedToday()),
                ["coolerFull"] = g.CoolerFull(),
                ["spoiled"] = g.SpoiledFish().Count,
                ["cookable"] = new JArray(g.Truck.Cookable(g.Save).Select(r => r.Id)),
            };
        }
    }
}

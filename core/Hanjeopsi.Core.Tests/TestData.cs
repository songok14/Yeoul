using System;
using System.IO;
using System.Linq;
using Hanjeopsi.Core.Data;
using Newtonsoft.Json.Linq;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>게임 데이터와 웹 버전 기준값(unity/golden) 읽기</summary>
    public static class TestData
    {
        private static readonly string Dir = AppContext.BaseDirectory;
        private static GameData _data;
        private static JObject _market;
        private static JObject _rules;
        private static JObject _scenario;
        private static JObject _reel;

        /// <summary>
        /// 웹 버전과 견주는 테스트용 데이터: 웹에 있는 다섯 어종(웹 이름 그대로)과 웹 REEL_TUNING.
        /// 고닷은 어종을 FishingPlan 도감 71종으로 늘리고 낚시 수치를 바꿨지만, 같은 계산인지는 웹 모양 데이터로 본다
        /// </summary>
        public static GameData Game
        {
            get
            {
                if (_data != null) return _data;
                var d = Load();
                d.Species.RemoveAll(sp => Array.IndexOf(WebSpecies, sp.Id) < 0);
                foreach (var sp in d.Species)
                {
                    if (sp.Id == "noraemi") sp.Name = "노래미";
                    // 고닷은 하루를 다섯 칸으로 늘리고 밤 칸을 붙였다. 웹은 네 칸
                    sp.Slot = sp.Slot.Take(4).ToArray();
                }
                // 고닷이 더한 요리와 의뢰(새 어종)도 뺀다
                d.Recipes.Recipes.RemoveAll(r => Array.IndexOf(WebSpecies, r.Species) < 0);
                // 고닷이 더한 생선구이(웹 어종이라도 웹에 없는 요리)도 뺀다
                d.Recipes.Recipes.RemoveAll(r => r.Id.EndsWith("-gui") && r.Id != "bollak-gui");
                d.RequestTemplates.RemoveAll(r => Array.IndexOf(WebSpecies, r.Species) < 0);
                d.Balance.Time.SlotsPerDay = 4;
                d.Balance.Chapter1.CastsPerTrip = 6;
                d.Balance.Time.ShopLastSlot = 99;
                // 웹은 트럭·낚시 시간 제한과 밤낚시가 없다
                d.Balance.Time.FishSlots.Clear();
                d.Balance.Time.TruckSlots.Clear();
                d.Balance.Time.NightFishBeat = null;
                d.Balance.Time.SpeciesSlots.Clear();
                d.Diner = null;
                d.Weather = null;
                d.Weekly = null;
                d.TownMap = null;
                // 고닷이 늘린 장비(10단계, 새 장비, 미끼)도 웹 세 단계로
                d.Gear.Rods.RemoveRange(3, d.Gear.Rods.Count - 3);
                d.Gear.Reels.RemoveRange(3, d.Gear.Reels.Count - 3);
                d.Gear.Tackles.RemoveRange(3, d.Gear.Tackles.Count - 3);
                // 고닷은 낚싯대 단계마다 닿는 거리를 늘렸다. 웹은 0.8, 1, 1
                var webRods = MarketGolden["gear"]["rods"];
                for (int i = 0; i < d.Gear.Rods.Count; i++) d.Gear.Rods[i].Reach = (double)webRods[i]["reach"];
                foreach (var r in d.Gear.Reels) r.Slack = r.Danger = 0;
                d.Balance.Chapter1.Spot = null;
                d.Gear.Kits.Clear();
                d.Gear.Baits.Clear();
                d.Gear.TrapPool.Clear();
                d.Gear.Tanks.Clear();
                d.Texts.Slots = new System.Collections.Generic.List<string> { "새벽", "아침", "낮", "저녁" };
                d.Texts.SlotAfter = "밤";
                d.Balance.Reel = ReelGolden["tuning"].ToObject<ReelBalance>(Newtonsoft.Json.JsonSerializer.Create(GameData.JsonSettings));
                return _data = d;
            }
        }

        /// <summary>웹 버전에 있는 어종 (species.json 앞 다섯)</summary>
        public static readonly string[] WebSpecies = { "bollak", "godeungeo", "noraemi", "hakgongchi", "gamseongdom" };

        private static GameData _full;

        /// <summary>고닷 게임이 쓰는 그대로의 데이터 (어종 71종, 고닷 낚시 수치). 자동 플레이와 고닷 규칙 테스트용</summary>
        public static GameData Full
        {
            get { return _full ??= Load(); }
        }

        private static GameData Load()
        {
            return GameData.Load(name => File.ReadAllText(Path.Combine(Dir, "data", name + ".json")));
        }

        /// <summary>낚시 계산을 웹 수치로 (Game 과 같다)</summary>
        public static GameData WebReel
        {
            get { return Game; }
        }

        /// <summary>scripts/export-golden.mjs 가 웹 코드로 만든 시세·신선도 기준값</summary>
        public static JObject MarketGolden
        {
            get { return _market ??= Golden("market"); }
        }

        /// <summary>달력·규정, 미각, 어종 뽑기, 요리, 간 맞추기 판정</summary>
        public static JObject RulesGolden
        {
            get { return _rules ??= Golden("rules"); }
        }

        /// <summary>새 게임부터 행동을 이어 가며 남긴 세이브 상태</summary>
        public static JObject ScenarioGolden
        {
            get { return _scenario ??= Golden("scenario"); }
        }

        /// <summary>낚시 미니게임: 힌트 방향, 파워 게이지, 입질 대기, 챔질, 릴링</summary>
        public static JObject ReelGolden
        {
            get { return _reel ??= Golden("reel"); }
        }

        private static JObject Golden(string name)
        {
            return JObject.Parse(File.ReadAllText(Path.Combine(Dir, "golden", name + ".json")));
        }
    }
}

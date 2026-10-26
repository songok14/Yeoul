using System;
using System.IO;
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
                    if (sp.Id == "noraemi") sp.Name = "노래미";
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

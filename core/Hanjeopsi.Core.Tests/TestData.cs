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

        public static GameData Game
        {
            get
            {
                return _data ??= GameData.Load(name => File.ReadAllText(Path.Combine(Dir, "data", name + ".json")));
            }
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

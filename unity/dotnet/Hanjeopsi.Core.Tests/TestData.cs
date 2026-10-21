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
            get { return _market ??= JObject.Parse(File.ReadAllText(Path.Combine(Dir, "golden", "market.json"))); }
        }
    }
}

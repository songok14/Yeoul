using System;
using Hanjeopsi.Core.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Hanjeopsi.Core.State
{
    /// <summary>세이브 JSON 읽고 쓰기. 파일 입출력은 고닷 쪽(SaveStore)이 맡는다</summary>
    public static class SaveJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            // 옛 세이브에 없던 필드는 기본값, 모르는 필드는 무시한다
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
        };

        public static string ToJson(SaveData save, bool indented = false)
        {
            return JsonConvert.SerializeObject(save, indented ? Formatting.Indented : Formatting.None, Settings);
        }

        public static JObject ToJObject(SaveData save)
        {
            return JObject.FromObject(save, JsonSerializer.Create(Settings));
        }

        /// <summary>
        /// 세이브를 읽는다. 웹처럼 새 세이브 기본값 위에 덮어서, 필드가 늘어도 옛 세이브가 깨지지 않는다.
        /// 버전이 다르거나 읽을 수 없으면 null.
        /// </summary>
        public static SaveData FromJson(string json, GameData data)
        {
            try
            {
                var obj = JObject.Parse(json);
                var version = obj["version"];
                if (version == null || (int)version != SaveData.CurrentVersion) return null;
                var seed = obj["seed"];
                var save = Game.CreateSave(data, seed != null ? (int)seed : 0);
                JsonConvert.PopulateObject(json, save, new JsonSerializerSettings
                {
                    ContractResolver = Settings.ContractResolver,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                });
                return save;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

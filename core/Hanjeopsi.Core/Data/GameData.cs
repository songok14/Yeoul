using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// 모든 게임 데이터 묶음. 파일 읽기는 바깥에 맡긴다.
    /// 고닷: name => FileAccess.GetFileAsString("res://data/" + name + ".json")
    /// 테스트: name => File.ReadAllText(dir + name + ".json")
    /// </summary>
    public sealed class GameData
    {
        public static readonly string[] FileNames = { "species", "gear", "balance", "texts", "recipes", "requests", "taste", "dialogue", "story", "diner", "weather" };

        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public List<Species> Species { get; private set; }
        public GearTable Gear { get; private set; }
        public Balance Balance { get; private set; }
        public Texts Texts { get; private set; }
        public RecipeTable Recipes { get; private set; }
        public List<RequestTemplate> RequestTemplates { get; private set; }
        public List<TasteStage> TasteStages { get; private set; }
        public Dialogue Dialogue { get; private set; }
        public Story Story { get; private set; }
        /// <summary>여울호 등급과 손님 (고닷만). 웹 모양 데이터에서는 null 이라 웹과 같은 계산이 된다</summary>
        public DinerTable Diner { get; set; }
        /// <summary>하루 날씨 (고닷만). 웹 모양 데이터에서는 null 이라 늘 맑다</summary>
        public WeatherTable Weather { get; set; }

        private Dictionary<string, Species> _byId;
        private Dictionary<string, int> _indexOf;
        private Dictionary<string, Flavor> _flavors;
        private Dictionary<string, Recipe> _recipes;

        private sealed class SpeciesFile
        {
            public List<Species> Species = new List<Species>();
        }

        /// <param name="readText">파일 이름(확장자 없이)을 받아 JSON 문자열을 돌려주는 함수</param>
        public static GameData Load(Func<string, string> readText)
        {
            var d = new GameData
            {
                Species = Parse<SpeciesFile>(readText("species")).Species,
                Gear = Parse<GearTable>(readText("gear")),
                Balance = Parse<Balance>(readText("balance")),
                Texts = Parse<Texts>(readText("texts")),
                Recipes = Parse<RecipeTable>(readText("recipes")),
                RequestTemplates = Parse<RequestTable>(readText("requests")).Templates,
                TasteStages = Parse<TasteTable>(readText("taste")).Stages,
                Dialogue = Parse<Dialogue>(readText("dialogue")),
                Story = Parse<Story>(readText("story")),
                Diner = Parse<DinerTable>(readText("diner")),
                Weather = Parse<WeatherTable>(readText("weather")),
            };
            d._byId = new Dictionary<string, Species>();
            d._indexOf = new Dictionary<string, int>();
            for (int i = 0; i < d.Species.Count; i++)
            {
                d._byId.Add(d.Species[i].Id, d.Species[i]);
                d._indexOf.Add(d.Species[i].Id, i);
            }
            d._flavors = new Dictionary<string, Flavor>();
            foreach (var f in d.Recipes.Flavors) d._flavors.Add(f.Id, f);
            d._recipes = new Dictionary<string, Recipe>();
            foreach (var r in d.Recipes.Recipes) d._recipes.Add(r.Id, r);
            return d;
        }

        public static T Parse<T>(string json)
        {
            var v = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            if (v == null) throw new FormatException(typeof(T).Name + " 데이터를 읽지 못했다");
            return v;
        }

        public Species SpeciesById(string id)
        {
            Species s;
            if (!_byId.TryGetValue(id, out s)) throw new KeyNotFoundException("없는 어종 id: " + id);
            return s;
        }

        /// <summary>목록 안 순서. 시세 시드에 들어가므로 species.json 순서를 바꾸면 시세 흐름도 바뀐다</summary>
        public int SpeciesIndex(string id)
        {
            int i;
            if (!_indexOf.TryGetValue(id, out i)) throw new KeyNotFoundException("없는 어종 id: " + id);
            return i;
        }
    
        public Flavor FlavorById(string id)
        {
            Flavor f;
            if (!_flavors.TryGetValue(id, out f)) throw new KeyNotFoundException("없는 맛 id: " + id);
            return f;
        }

        public Recipe RecipeById(string id)
        {
            Recipe r;
            if (!_recipes.TryGetValue(id, out r)) throw new KeyNotFoundException("없는 요리 id: " + id);
            return r;
        }
    }
}

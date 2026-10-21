using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>recipes.json (웹 src/data/recipes.ts)</summary>
    public sealed class RecipeTable
    {
        public List<Flavor> Flavors = new List<Flavor>();
        public List<Recipe> Recipes = new List<Recipe>();
    }

    /// <summary>맛 하나. id는 salty, sweet, umami, sour, bitter</summary>
    public sealed class Flavor
    {
        public string Id;
        public string Name;
        /// <summary>간 맞추기 도구 (소금, 설탕 ...)</summary>
        public string Tool;
        /// <summary>손님 한마디 [좋음, 모자람, 지나침]</summary>
        public string[] Review;
    }

    public sealed class RecipeStep
    {
        public string Flavor;
        /// <summary>정답 구간 중심 (0~1). 요리마다 거의 고정이라 감으로도 익힐 수 있다</summary>
        public double Target;
    }

    public sealed class Recipe
    {
        public string Id;
        public string Name;
        public string Species;
        public int Price;
        /// <summary>회 요리는 신선도에 더 민감하다</summary>
        public bool Raw;
        public List<RecipeStep> Steps = new List<RecipeStep>();
    }
}

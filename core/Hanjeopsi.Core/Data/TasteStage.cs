using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>taste.json (웹 systems/taste.ts TASTE_STAGES)</summary>
    public sealed class TasteTable
    {
        public List<TasteStage> Stages = new List<TasteStage>();
    }

    /// <summary>미각 회복 단계</summary>
    public sealed class TasteStage
    {
        public string Name;
        /// <summary>이 단계에서 느낄 수 있는 맛 id</summary>
        public List<string> Flavors = new List<string>();
        public string Cond;
        /// <summary>필요한 어종 수 (1장 축소 기준). 없으면 어종 수로는 오르지 않는 단계</summary>
        public int? NeedSpecies;
        public string Effect;
    }
}

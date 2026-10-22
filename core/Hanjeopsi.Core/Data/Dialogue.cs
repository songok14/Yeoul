using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>dialogue.json. 화면에 흩어진 짧은 대사 (웹 TownScene GRANDMA_LINES 등)</summary>
    public sealed class Dialogue
    {
        /// <summary>여울식당 막순 할머니</summary>
        public List<string> Grandma = new List<string>();
        /// <summary>고등어를 눌렀을 때 (상한 생선이 없을 때)</summary>
        public List<string> CatIdle = new List<string>();
        /// <summary>마을에서 여울을 눌렀을 때</summary>
        public List<string> YeoulIdle = new List<string>();
    }
}

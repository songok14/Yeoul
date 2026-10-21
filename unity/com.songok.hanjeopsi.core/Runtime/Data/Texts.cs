using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>texts.json. 계산 결과로 고르는 짧은 문구</summary>
    public sealed class Texts
    {
        /// <summary>신선도 단계 이름. 키는 live, fresh, aged, spoiled</summary>
        public Dictionary<string, string> StageNames = new Dictionary<string, string>();
        public MarketTexts Market = new MarketTexts();
    }

    public sealed class MarketTexts
    {
        public string FirstDay;
        public string Calm;
        /// <summary>{0} 어종 이름, {1} 오른 퍼센트</summary>
        public string Up;
        /// <summary>{0} 어종 이름, {1} 내린 퍼센트</summary>
        public string Down;
    }
}

using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>texts.json. 계산 결과로 고르는 짧은 문구</summary>
    public sealed class Texts
    {
        /// <summary>신선도 단계 이름. 키는 live, fresh, aged, spoiled</summary>
        public Dictionary<string, string> StageNames = new Dictionary<string, string>();
        public MarketTexts Market = new MarketTexts();
        /// <summary>시간대 이름 [새벽, 아침, 낮, 저녁]</summary>
        public List<string> Slots = new List<string>();
        /// <summary>저녁 다음 (날이 바뀌기 직전)</summary>
        public string SlotAfter;
        /// <summary>{0}월 {1}일</summary>
        public string Date;
        public RuleTexts Rules = new RuleTexts();
        public ReviewTexts Review = new ReviewTexts();
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

    public sealed class RuleTexts
    {
        /// <summary>{0} 금지체장(cm)</summary>
        public string Undersize;
        /// <summary>{0}/{1}~{2}/{3} 금어기</summary>
        public string Closed;
    }

    public sealed class ReviewTexts
    {
        /// <summary>아직 모르는 맛</summary>
        public string Unknown;
        public string WellAged;
        public string VeryFresh;
        /// <summary>[실망, 보통, 만족]</summary>
        public List<string> Mood = new List<string>();
    }
}

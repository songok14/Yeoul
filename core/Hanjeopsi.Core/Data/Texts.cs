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
        /// <summary>거리 구간 이름 [근거리, 중거리, 원거리]</summary>
        public List<string> Zones = new List<string>();
        /// <summary>낚시 화면 안내 문구. {0} 자리는 화면이 채운다</summary>
        public Dictionary<string, string> Fishing = new Dictionary<string, string>();
        /// <summary>위판장 화면 (경매, 납품 의뢰)</summary>
        public Dictionary<string, string> Auction = new Dictionary<string, string>();
        /// <summary>노씨 미끼가게 화면</summary>
        public Dictionary<string, string> Shop = new Dictionary<string, string>();
        /// <summary>수첩 화면 (쿨러, 도감, 미각, 조황일지)</summary>
        public Dictionary<string, string> Notebook = new Dictionary<string, string>();
        /// <summary>여울호 푸드트럭 화면</summary>
        public Dictionary<string, string> Truck = new Dictionary<string, string>();
        /// <summary>타이틀 화면 제목과 챕터 부제</summary>
        public TitleTexts Title = new TitleTexts();
    }

    public sealed class TitleTexts
    {
        /// <summary>게임 제목</summary>
        public string Name;
        /// <summary>챕터 순서대로 (1장부터)</summary>
        public List<ChapterTitle> Chapters = new List<ChapterTitle>();
        /// <summary>트루 엔딩을 본 뒤의 부제</summary>
        public string AfterEnding;
        /// <summary>타이틀 아래 작은 글씨. {0} 챕터 번호, {1} 지역</summary>
        public string Foot;
    }

    public sealed class ChapterTitle
    {
        public string Place;
        public string Subtitle;
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

namespace Hanjeopsi.Core.Data
{
    /// <summary>balance.json. 게임 밸런스 수치 (웹 config.ts 와 systems/*.ts 의 상수)</summary>
    public sealed class Balance
    {
        public Chapter1Balance Chapter1 = new Chapter1Balance();
        public MarketBalance Market = new MarketBalance();
        public FreshBalance Fresh = new FreshBalance();
        public TimeBalance Time = new TimeBalance();
        public RequestBalance Requests = new RequestBalance();
        public CatBalance Cat = new CatBalance();
        public FishingBalance Fishing = new FishingBalance();
        public SeasonBalance Season = new SeasonBalance();
        public TruckBalance Truck = new TruckBalance();
    }

    public sealed class Chapter1Balance
    {
        public int DeadlineDays;
        public int Debt;
        public int StartMoney;
        public int StartBait;
        public int CastsPerTrip;
        public int CustomersPerShift;
        /// <summary>1일차 날짜 (2026년 4월 16일)</summary>
        public int StartYear;
        public int StartMonth;
        public int StartDate;
        /// <summary>여울식당 "10만 원 갚기" 버튼 금액</summary>
        public int RepayStep;
    }

    /// <summary>시세 랜덤 워크</summary>
    public sealed class MarketBalance
    {
        /// <summary>하루 변동폭. (난수 - 0.5) × 이 값 만큼 움직인다</summary>
        public double DailySwing;
        public double MinMul;
        public double MaxMul;
        /// <summary>충격 난수가 이 값보다 작으면 풍어</summary>
        public double GlutBelow;
        public double GlutMul;
        /// <summary>충격 난수가 이 값보다 크면 품귀</summary>
        public double ShortageAbove;
        public double ShortageMul;
        /// <summary>값 = 기준가 × (크기 / 평균)^이 값</summary>
        public double SizeExponent;
    }

    /// <summary>신선도 단계 경계와 경매 배율</summary>
    public sealed class FreshBalance
    {
        public int Start;
        public int LiveAbove;
        public int FreshAbove;
        public int AgedAbove;
        public double LiveFactor;
        public double FreshFactor;
        public double AgedFactor;
        /// <summary>숙성 어종(노래미, 감성돔)이 숙성 단계일 때 배율</summary>
        public double AgedFactorAging;
    }

    /// <summary>하루 시간 칸</summary>
    public sealed class TimeBalance
    {
        /// <summary>새벽 · 아침 · 낮 · 저녁</summary>
        public int SlotsPerDay;
        /// <summary>경매는 이 칸까지 (아침)</summary>
        public int MarketLastSlot;
        /// <summary>트럭은 이 칸부터 (아침)</summary>
        public int TruckFirstSlot;
    }

    /// <summary>식당 납품 의뢰</summary>
    public sealed class RequestBalance
    {
        public int Max;
        /// <summary>날짜별 의뢰 난수 시드에 섞는 값</summary>
        public int SeedSalt;
        public int Guard;
        public double RewardMul;
        public int DeadlineMin;
        public int DeadlineRange;
        /// <summary>귀한 고기(감성돔) 의뢰는 이날부터</summary>
        public int RareFromDay;
    }

    /// <summary>고양이 고등어 호감과 입질 힌트</summary>
    public sealed class CatBalance
    {
        public int MaxAffinity;
        public double HintBase;
        public double HintPerAffinity;
        public double HintMax;
    }

    /// <summary>거리 구간과 어종·크기 뽑기</summary>
    public sealed class FishingBalance
    {
        public double NearBelow;
        public double MidBelow;
        /// <summary>물 좋은 곳에서 귀한 고기 가중치</summary>
        public double RareHotMul;
        public double RareColdMul;
        public double SizePow;
        public double HotSizePow;
    }

    /// <summary>간 맞추기 판정</summary>
    public sealed class SeasonBalance
    {
        /// <summary>정답 구간 안: 1 - 이 값 × (거리 / 반폭)</summary>
        public double InZoneDrop;
        /// <summary>정답 구간 밖 최고 정확도</summary>
        public double OutMax;
        /// <summary>정답 구간 밖에서 0이 되는 거리 (반폭의 배수)</summary>
        public double OutWidth;
    }

    /// <summary>푸드트럭</summary>
    public sealed class TruckBalance
    {
        public double ZoneHalf;
        public double TargetJitter;
        public double TargetMin;
        public double TargetMax;
        public double GaugeSpeedMin;
        public double GaugeSpeedRange;
        /// <summary>요리값 = 가격 × 신선도 × (PayBase + PayQuality × 정확도)</summary>
        public double PayBase;
        public double PayQuality;
        public double MoodGood;
        public double MoodOk;
        /// <summary>이 정확도 이상이면 방향이 틀려도 칭찬</summary>
        public double ReviewGoodQuality;
        /// <summary>이 미각 단계부터 리뷰 전문이 보인다</summary>
        public int FullReviewTaste;
        public DishFresh Raw = new DishFresh();
        public DishFresh Cooked = new DishFresh();
    }

    /// <summary>요리 신선도 배율</summary>
    public sealed class DishFresh
    {
        public double Live;
        public double Fresh;
        public double Aged;
        /// <summary>숙성 어종이 숙성 단계일 때</summary>
        public double AgedAging;
    }
}

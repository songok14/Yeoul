namespace Hanjeopsi.Core.Data
{
    /// <summary>balance.json. 게임 밸런스 수치 (웹 config.ts 와 systems/*.ts 의 상수)</summary>
    public sealed class Balance
    {
        public Chapter1Balance Chapter1 = new Chapter1Balance();
        public MarketBalance Market = new MarketBalance();
        public FreshBalance Fresh = new FreshBalance();
    }

    public sealed class Chapter1Balance
    {
        public int DeadlineDays;
        public int Debt;
        public int StartMoney;
        public int StartBait;
        public int CastsPerTrip;
        public int CustomersPerShift;
        /// <summary>1일차 날짜 (4월 16일)</summary>
        public int StartMonth;
        public int StartDate;
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
}

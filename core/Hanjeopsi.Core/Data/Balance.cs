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
        public ReelBalance Reel = new ReelBalance();
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
        /// <summary>장사 한 번에 받는 손님 수</summary>
        public int CustomersPerShift;
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

    /// <summary>낚시 미니게임: 파워 게이지, 입질 대기, 챔질, 릴링 물리 (웹 systems/reel.ts REEL_TUNING)</summary>
    public sealed class ReelBalance
    {
        /// <summary>파워 게이지가 1초에 움직이는 양 (0~1 사이를 왕복)</summary>
        public double GaugeSpeed;
        /// <summary>가장 가까이 던져도 이 거리(0~1)는 간다</summary>
        public double MinCast;

        /// <summary>입질까지 기다리는 시간(초). 물 좋은 곳이면 HotWaitMul 배</summary>
        public double WaitMin;
        public double WaitMax;
        public double WaitTutorial;
        public double HotWaitMul;
        /// <summary>찌를 톡톡 건드리는 속임 입질 간격(초)</summary>
        public double NibbleFirstMin;
        public double NibbleFirstMax;
        public double NibbleMin;
        public double NibbleMax;

        /// <summary>챔질 허용 시간(초) = BiteWindow + 낚싯대 HookBonus(ms)/1000, 귀한 고기는 짧다</summary>
        public double BiteWindow;
        public double BiteTutorial;
        public double RareBitePenalty;
        /// <summary>이 시간 안에 누르면 완벽한 챔질</summary>
        public double PerfectBelow;
        /// <summary>프롤로그에서 챔질을 놓치면 다시 기다리는 시간</summary>
        public double MissWaitTutorial;

        /// <summary>걸었을 때 거리(m) = Base + 던진 거리 × PerDist. 완벽한 챔질이면 PerfectBonus 만큼 가깝다</summary>
        public double StartDistanceBase;
        public double StartDistancePerDist;
        public double PerfectBonus;
        public double StartTension;
        /// <summary>첫 꾹꾹 저항까지 시간</summary>
        public double SurgeFirstMin;
        public double SurgeFirstMax;
        public double SurgeTutorial;

        /// <summary>텐션 구간: 느슨함 &lt; Slack, Danger 이상은 빨간색, Snap 이상이면 줄이 터진다</summary>
        public double Slack;
        public double Danger;
        public double Snap;
        public double MaxTension;
        /// <summary>처음 거리보다 이만큼 더 멀어질 수는 없다</summary>
        public double Overrun;

        /// <summary>감을 때 텐션 상승 = (RiseBase + 힘 × RisePerStr) × 릴 배율. 드랙을 넘으면 OverDragMul 배</summary>
        public double RiseBase;
        public double RisePerStr;
        public double OverDragMul;
        /// <summary>줄이 느슨할 때 감는 속도 배율</summary>
        public double SlackReelMul;
        /// <summary>손을 떼면 텐션이 초당 이만큼 내려가고 물고기는 힘 × PullPerStr 만큼 멀어진다</summary>
        public double RelaxRate;
        public double PullPerStr;

        /// <summary>꾹꾹 저항 = (KickBase + 힘 × KickPerStr) × (1 - 낚싯대 흡수율)</summary>
        public double SurgeKickBase;
        public double SurgeKickPerStr;
        public double SurgeDistance;
        /// <summary>저항 뒤 그림자가 크게 흔들리는 시간</summary>
        public double SurgeKickTime;
        /// <summary>다음 저항까지 = 난수(GapMin~GapMax) / (GapBase + 힘 × GapPerStr)</summary>
        public double SurgeGapMin;
        public double SurgeGapMax;
        public double SurgeGapBase;
        public double SurgeGapPerStr;

        /// <summary>느슨한 시간이 이만큼 쌓이면 바늘이 빠진다. 텐션이 돌아오면 SlackRecover 배로 줄어든다</summary>
        public double SlackLimit;
        public double SlackLimitFast;
        public double SlackLimitTutorial;
        public double SlackRecover;
        /// <summary>입이 약해 바늘이 잘 빠지는 어종</summary>
        public string[] FastSpecies;

        /// <summary>지침: TiredTime 초 동안 힘이 TiredMax 만큼 줄어든다</summary>
        public double TiredMax;
        public double TiredTime;
        /// <summary>힘에 곱하는 크기 비율(크기 / 평균) 범위</summary>
        public double SizeMin;
        public double SizeMax;
        public double StrengthTutorial;
        /// <summary>프롤로그에서 줄이 터질 뻔하면 텐션을 여기로</summary>
        public double RescueTension;
    }
}

using System.Collections.Generic;

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
        public SpotBalance Spot = new SpotBalance();
        public TugBalance Tug = new TugBalance();
        public TownBalance Town = new TownBalance();
    }

    public sealed class Chapter1Balance
    {
        public int DeadlineDays;
        public int Debt;
        public int StartMoney;
        public int StartBait;
        public int CastsPerTrip;
        public int CustomersPerShift;
        /// <summary>1장 바다 (species.json seas)</summary>
        public string Sea;
        /// <summary>1장 낚시터 (species.json spots). 방파제</summary>
        public string Spot;
        /// <summary>1일차 날짜 (2026년 4월 16일)</summary>
        public int StartYear;
        public int StartMonth;
        public int StartDate;
        /// <summary>할머니댁 "10만 원 갚기" 버튼 금액</summary>
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
        /// <summary>하루 칸 수. 고닷은 아침 · 점심 · 저녁 · 밤 (2026-10-28, 데이브 더 다이브처럼), 웹은 새벽 · 아침 · 낮 · 저녁</summary>
        public int SlotsPerDay;
        /// <summary>경매는 이 칸까지</summary>
        public int MarketLastSlot;
        /// <summary>트럭은 이 칸부터 (아침)</summary>
        public int TruckFirstSlot;
        /// <summary>미끼가게는 이 칸까지 연다 (저녁). 없으면 언제나</summary>
        public int ShopLastSlot = 99;
        /// <summary>시간대마다 마을 배경 그림 이름 (godot/art/backgrounds)</summary>
        public List<string> TownArt = new List<string>();
        /// <summary>낚시하러 나갈 수 있는 칸 (고닷: 새벽 · 아침 · 낮 · 밤). 비어 있으면 언제나</summary>
        public List<int> FishSlots = new List<int>();
        /// <summary>트럭을 여는 칸 (고닷: 저녁만). 비어 있으면 TruckFirstSlot 부터</summary>
        public List<int> TruckSlots = new List<int>();
        /// <summary>밤낚시를 여는 이야기 장면. 이 장면을 보기 전에는 밤에 방파제에 못 간다. 비어 있으면 처음부터</summary>
        public string NightFishBeat;
        /// <summary>
        /// 하루 칸마다 쓰는 어종 시간대 가중치 칸 (species.json slot 은 새벽 · 아침 · 점심 · 저녁 · 밤 다섯 칸).
        /// 여러 칸이면 평균. 비어 있으면 같은 번호 칸
        /// </summary>
        public List<List<int>> SpeciesSlots = new List<List<int>>();
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

    /// <summary>마을 마당을 지나가는 사람 수</summary>
    public sealed class TownBalance
    {
        /// <summary>시간대마다 마당에 있는 사람 수 (새벽 · 아침 · 낮 · 저녁 · 밤)</summary>
        public List<int> Walkers = new List<int>();
        /// <summary>궂은 날씨면 이만큼 줄어든다 (날씨 id → 줄어드는 수)</summary>
        public Dictionary<string, int> WeatherLess = new Dictionary<string, int>();
        /// <summary>한 사람이 나간 뒤 다음 사람이 들어오기까지 (초)</summary>
        public double SpawnMin;
        public double SpawnMax;
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

    /// <summary>
    /// 고닷 낚시: 부채꼴 방향 고르기, 고등어가 가리키는 지점, 큰 놈 입질, 고등어 도둑.
    /// 지점에 가까이 던질수록 귀한 고기와 큰 고기가 잘 나온다. 입질까지 기다리는 시간은 어디든 같다
    /// </summary>
    public sealed class SpotBalance
    {
        /// <summary>부채꼴 바늘이 1초에 움직이는 양 (방향 -1~1 사이를 왕복)</summary>
        public double AimSpeed;
        /// <summary>물 좋은 지점의 방향 범위 (±)</summary>
        public double HotAngleMax;
        /// <summary>물 좋은 지점의 거리 범위 (낚싯대가 닿는 거리에 곱한다)</summary>
        public double HotDistMin;
        public double HotDistMax;
        /// <summary>지점에서 이만큼 벗어나면 효과가 없다 (방향, 거리)</summary>
        public double AngleRange;
        public double DistRange;
        /// <summary>큰 놈 입질 확률 = BigBase + BigPerClose × 가까운 정도</summary>
        public double BigBase;
        public double BigPerClose;
        /// <summary>큰 놈은 크기 범위의 이 지점부터 위에서 나온다 (0~1)</summary>
        public double BigSizeFrom;
        /// <summary>쿨러에 고기가 있으면 던질 때마다 고등어가 노릴 확률</summary>
        public double ThiefChance;
        /// <summary>찌가 떨어지고 고등어가 움직이기까지 (초)</summary>
        public double ThiefDelayMin;
        public double ThiefDelayMax;
        /// <summary>이 시간 안에 고등어를 누르면 막는다 (초)</summary>
        public double ThiefWindow;
    }

    /// <summary>고닷 힘싸움 (TugFight). 끊김·느슨 기준과 감기 속도는 ReelBalance 를 같이 쓴다</summary>
    public sealed class TugBalance
    {
        /// <summary>첫 질주까지 (초)</summary>
        public double FirstRunMin;
        public double FirstRunMax;
        public double FirstRunTutorial;
        /// <summary>질주 예고 시간 (초)</summary>
        public double WarnTime;
        public double WarnTutorial;
        /// <summary>질주 길이 (초). 지칠수록 RunTiredMul 쪽으로 짧아진다</summary>
        public double RunMin;
        public double RunMax;
        public double RunTiredMul;
        /// <summary>다음 질주까지 = 간격 / (GapBase + 힘 × GapPerStr)</summary>
        public double GapMin;
        public double GapMax;
        public double GapBase;
        public double GapPerStr;
        /// <summary>질주 시간의 이만큼 반대쪽을 누르면 버텨 낸 것</summary>
        public double HeldRatio;
        /// <summary>질주 때 줄이 풀려 나가는 속도 (힘 1 당 m/초)</summary>
        public double RunPull;
        /// <summary>버틸 때도 이만큼은 풀린다 (RunPull 비율)</summary>
        public double BraceLeak;
        /// <summary>버틸 때 텐션 오름 (초당)</summary>
        public double BraceRise;
        public double BraceRisePerStr;
        /// <summary>버틸 때 물고기 힘 빠짐 (초당) = (BraceDrain + 텐션/100 × BraceDrainPerTension) / max(1, 힘 × StaminaPerStr)</summary>
        public double BraceDrain;
        public double BraceDrainPerTension;
        public double StaminaPerStr;
        /// <summary>질주 쪽으로 당기면 텐션이 치솟는다 (초당)</summary>
        public double WrongRise;
        public double WrongRisePerStr;
        /// <summary>질주 중 손을 떼도 텐션은 이 아래로 안 내려간다</summary>
        public double RunFloor;
        /// <summary>잠잠할 때 알맞게 팽팽하면 힘이 빠진다 (초당, 압력 1일 때)</summary>
        public double CalmDrain;
        /// <summary>손을 떼고 있으면 힘이 돌아온다 (초당)</summary>
        public double Recover;
        /// <summary>다 지쳐도 남는 힘 비율</summary>
        public double TiredFloor;
        /// <summary>완벽한 챔질이면 처음부터 힘이 이만큼 빠져 있다</summary>
        public double PerfectStamina;
        /// <summary>줄이 풀려 나가도 시작 거리 + 이만큼까지</summary>
        public double MaxOverrun;
        /// <summary>힘이 가득한 물고기는 감는 속도의 이만큼만 끌려온다 (다 지치면 1)</summary>
        public double FreshReelMul;
        /// <summary>다 지친 물고기를 감을 때 텐션 상승 배율 (거의 안 오른다)</summary>
        public double TiredRiseMul = 1;
        /// <summary>다 지친 물고기를 감을 때 감는 속도 배율 (쭉 끌려온다)</summary>
        public double TiredReelMul = 1;
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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hanjeopsi.Core.State
{
    /// <summary>
    /// 세이브 데이터 (웹 state/save.ts SaveData). 필드 이름과 뜻을 웹과 똑같이 둬서
    /// 웹 세이브 JSON을 그대로 읽을 수 있다. 필드를 추가하면 Create 에 기본값을 넣는다.
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Seed;
        public int Day;
        /// <summary>0 새벽, 1 아침, 2 점심, 3 저녁, 4 밤 (웹은 네 칸)</summary>
        public int Slot;
        public int Money;
        public int Debt;
        public int Bait;
        public int Rod;
        public int Reel;
        public int Tackle;
        public List<FishItem> Cooler = new List<FishItem>();
        public int NextUid;
        public Dictionary<string, DexEntry> Dex = new Dictionary<string, DexEntry>();
        public List<string> RecipesCooked = new List<string>();
        public int DishesCooked;
        public int CatAffinity;
        public List<DeliveryRequest> Requests = new List<DeliveryRequest>();
        public int NextRequestId;
        public int EarnedToday;
        public int TotalEarned;
        public int JournalPages;
        public DaySummary PendingSummary;
        public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        /// <summary>여울호 평판·등급·손님 친밀도 (고닷만). 웹 세이브에는 없어서 비어 있으면 쓰지 않는다</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DinerState Diner;

        public bool Flag(string name)
        {
            bool v;
            return Flags.TryGetValue(name, out v) && v;
        }
    }

    /// <summary>여울호 평판과 손님 친밀도</summary>
    public sealed class DinerState
    {
        public int Rep;
        /// <summary>마지막으로 알려 준 등급 (등급이 오른 걸 한 번만 알린다)</summary>
        public int Rank;
        /// <summary>손님 id → 친밀도</summary>
        public Dictionary<string, int> Affinity = new Dictionary<string, int>();
    }

    /// <summary>도감 한 칸</summary>
    public sealed class DexEntry
    {
        public int Count;
        public double Best;
        public int Released;
    }

    /// <summary>받아 둔 납품 의뢰</summary>
    public sealed class DeliveryRequest
    {
        public int Id;
        /// <summary>requests.json 안 순서</summary>
        public int Template;
        public int Reward;
        /// <summary>이날까지 납품해야 한다</summary>
        public int Deadline;
    }

    /// <summary>날이 바뀔 때 보여 줄 어제 요약</summary>
    public sealed class DaySummary
    {
        public int Day;
        public int Earned;
        public int Spoiled;
        public int ExpiredRequests;
    }
}

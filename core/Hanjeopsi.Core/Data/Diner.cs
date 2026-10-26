using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// diner.json. 여울호 등급과 손님 (고닷만, 웹에는 없다).
    /// 평판(손님 반응과 납품 의뢰)이 쌓이면 등급이 올라 메뉴 칸과 기본 손님이 는다.
    /// 손님마다 오는 시간대와 좋아하는 요리가 있고, 친밀도가 쌓여 단골이 되면 그 시간대에 꼭 온다.
    /// </summary>
    public sealed class DinerTable
    {
        /// <summary>등급 (평판이 낮은 순)</summary>
        public List<DinerRank> Ranks = new List<DinerRank>();
        /// <summary>시간대마다 기본 손님에 더하는 수 (새벽 · 아침 · 점심 · 저녁 · 밤)</summary>
        public List<int> SlotBonus = new List<int>();
        /// <summary>한 번 장사에 오는 손님 최대</summary>
        public int MaxGuests;
        /// <summary>평판: 손님 기분 0 실망 · 1 보통 · 2 만족마다</summary>
        public List<int> RepByMood = new List<int>();
        /// <summary>평판: 납품 의뢰를 마치면</summary>
        public int RepRequest;
        /// <summary>친밀도: 손님 기분마다</summary>
        public List<int> AffinityByMood = new List<int>();
        /// <summary>친밀도: 좋아하는 요리를 보통 이상으로 내면 더</summary>
        public int AffinityFavorite;
        public int AffinityMax;
        /// <summary>이만큼 친해지면 단골. 단골은 자기 시간대에 꼭 온다</summary>
        public int RegularAt;
        /// <summary>메뉴에 좋아하는 요리가 있으면 그걸 시킬 확률</summary>
        public double FavoriteChance;
        public List<DinerGuest> Guests = new List<DinerGuest>();

        public DinerGuest GuestById(string id)
        {
            foreach (var g in Guests)
                if (g.Id == id) return g;
            throw new KeyNotFoundException("없는 손님 id: " + id);
        }
    }

    public sealed class DinerRank
    {
        public string Name;
        /// <summary>이 등급이 되는 평판</summary>
        public int Rep;
        /// <summary>오늘의 메뉴 칸</summary>
        public int Menu;
        /// <summary>장사 한 번 기본 손님 (단골과 시간대 보정은 따로)</summary>
        public int Guests;
    }

    public sealed class DinerGuest
    {
        /// <summary>dialogue.json customers 의 id 와 같다 (SD 그림과 주문 말투)</summary>
        public string Id;
        public string Name;
        /// <summary>오는 시간대 (0 새벽 ~ 4 밤)</summary>
        public List<int> Slots = new List<int>();
        /// <summary>좋아하는 요리 id</summary>
        public List<string> Likes = new List<string>();
        /// <summary>단골이 되면 하는 주문. {0} 은 요리 이름</summary>
        public string Regular;
    }
}

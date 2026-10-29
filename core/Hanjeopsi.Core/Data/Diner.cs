using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// diner.json. 여울호 등급과 손님 (고닷만, 웹에는 없다).
    /// 평판(명성)이 손님 수를 정하고 친밀도가 손님 등급을 정한다 (2026-10-28, 데이브 더 다이브 방식).
    /// 손님 등급: 일반 → 단골(친밀도, 좋아하는 요리와 팁) → VIP(예약, 정해진 요리와 신선도) → 특별 손님(이야기 인물).
    /// </summary>
    public sealed class DinerTable
    {
        /// <summary>등급 (평판이 낮은 순)</summary>
        public List<DinerRank> Ranks = new List<DinerRank>();
        /// <summary>시간대마다 기본 손님에 더하는 수 (새벽 · 아침 · 낮 · 저녁 · 밤)</summary>
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
        /// <summary>단골이 보통 이상으로 먹으면 요리값의 이만큼 팁</summary>
        public double RegularTip;
        /// <summary>등급마다 VIP 예약 확률 [첫째, 둘째]</summary>
        public List<List<double>> VipChance = new List<List<double>>();
        /// <summary>날짜별 VIP 예약 난수 시드에 섞는 값</summary>
        public int VipSeedSalt;
        public List<DinerGuest> Guests = new List<DinerGuest>();
        public List<DinerVip> Vips = new List<DinerVip>();
        public List<DinerSpecial> Specials = new List<DinerSpecial>();

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
        /// <summary>이 등급이 될 때 저녁 손님. 다음 등급까지는 평판에 따라 한 명씩 는다</summary>
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

    /// <summary>VIP 손님. 그날 아침에 예약이 들어오고 정해진 요리를 정해진 신선도로 바란다</summary>
    public sealed class DinerVip
    {
        public string Id;
        public string Name;
        /// <summary>SD 그림 (godot/art/customers/&lt;Art&gt;-stand 등). 아직 VIP 그림이 없어 손님 그림을 빌려 쓴다</summary>
        public string Art;
        /// <summary>바라는 요리 후보. 예약할 때 하나를 고른다</summary>
        public List<string> Wants = new List<string>();
        /// <summary>받아 주는 신선도 단계 (0 활어, 1 선어, 2 숙성)</summary>
        public int Fresh;
        /// <summary>주문 말. {0} 요리 이름</summary>
        public string Order;
        public string Thanks;
        /// <summary>요리를 못 내거나 실망했을 때. {0} 요리 이름</summary>
        public string Miss;
        /// <summary>팁 = 요리값 × TipMul</summary>
        public double TipMul;
        public int Rep;
        public int MissRep;
    }

    /// <summary>특별 손님. 이야기 인물이 정해진 날 저녁에 한 번 온다</summary>
    public sealed class DinerSpecial
    {
        public string Id;
        public string Name;
        /// <summary>dialogue.json speakers id (대화 일러스트)</summary>
        public string Speaker;
        /// <summary>이날부터 Until 까지 저녁에 온다</summary>
        public int Day;
        public int Until;
        /// <summary>이 이야기 장면을 본 뒤에만 온다 (비어 있으면 언제나)</summary>
        public string After;
        /// <summary>바라는 요리 (메뉴에 있으면 그것, 없으면 메뉴에서 제일 비싼 요리)</summary>
        public List<string> Wants = new List<string>();
        public string Order;
        /// <summary>주문할 때 표정</summary>
        public string Face;
        /// <summary>실망 · 보통 · 만족 한마디와 표정</summary>
        public List<string> Lines = new List<string>();
        public List<string> Faces = new List<string>();
        public List<int> Rep = new List<int>();
    }
}

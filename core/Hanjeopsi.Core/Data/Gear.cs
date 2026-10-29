using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>gear.json (웹 src/data/gear.ts). 각 목록의 0번이 시작 장비</summary>
    public sealed class GearTable
    {
        public List<Rod> Rods = new List<Rod>();
        public List<Reel> Reels = new List<Reel>();
        public List<Tackle> Tackles = new List<Tackle>();
        public Bait Bait = new Bait();
        /// <summary>고닷 장비 (낚싯줄, 바늘, 찌, 미끼통, 뜰채, 헤드랜턴, 의자, 장화, 통발). 웹에는 없다</summary>
        public List<KitKind> Kits = new List<KitKind>();
        /// <summary>고닷 미끼 열 가지. krill 은 웹의 크릴(Bait)과 같다</summary>
        public List<BaitKind> Baits = new List<BaitKind>();
        /// <summary>좋아하는 미끼를 쓰면 그 어종 가중치에 곱한다</summary>
        public double BaitLikeMul = 1;
        /// <summary>통발에 드는 어종</summary>
        public List<string> TrapPool = new List<string>();
        /// <summary>보상 조건을 채워 가게에 장비가 들어올 때 바로 주는 미끼</summary>
        public StockGift StockGift = new StockGift();
        /// <summary>여울호 활어 수조 단계 (고닷만). 0번이 처음 수조. 비어 있으면 수조가 없다</summary>
        public List<TankLevel> Tanks = new List<TankLevel>();
    }

    /// <summary>활어 수조 한 단계. 넣은 고기는 활어 그대로 산다</summary>
    public sealed class TankLevel
    {
        public string Name;
        public string Desc;
        public int Price;
        public int Capacity;
    }

    public sealed class StockGift
    {
        /// <summary>미끼 id (krill 이면 크릴)</summary>
        public string Bait;
        /// <summary>몇 봉지</summary>
        public int Packs;
    }

    /// <summary>
    /// 장비를 얻는 법: start 시작 장비, shop 상점, later 다음 지역,
    /// 그 밖은 보상(rank:N 여울호 등급, regulars:N 단골 수, requests:N 마친 납품 의뢰, favor 노씨 의뢰, clear 1장 클리어).
    /// favor 는 노씨가 그냥 준다. 나머지는 조건을 채우면 가게에 들어오고 값을 내고 산다(2026-10-29).
    /// 비어 있으면 상점 (웹 데이터)
    /// </summary>
    public static class Unlock
    {
        public static bool IsShop(string u)
        {
            return string.IsNullOrEmpty(u) || u == "shop";
        }

        public static bool IsReward(string u)
        {
            return !IsShop(u) && u != "start" && u != "later";
        }
    }

    /// <summary>장비 한 종류. 단계마다 값(value)이 하나라 효과 글(effect)에 넣어 보여 준다</summary>
    public sealed class KitKind
    {
        public string Id;
        public string Name;
        /// <summary>{0} 은 단계 값</summary>
        public string Effect;
        public List<KitLevel> Levels = new List<KitLevel>();
    }

    public sealed class KitLevel
    {
        public string Name;
        public int Price;
        public double Value;
        public string Unlock;
    }

    public sealed class BaitKind
    {
        public string Id;
        public string Name;
        public int Price;
        public int Amount;
        /// <summary>입질 대기 배율 (집어 크릴은 빨리 모인다)</summary>
        public double WaitMul = 1;
        /// <summary>이 미끼를 좋아하는 어종</summary>
        public List<string> Likes = new List<string>();
    }

    public sealed class Rod
    {
        public string Name;
        public string Desc;
        public int Price;
        /// <summary>얻는 법 (Unlock 참고)</summary>
        public string Unlock;
        /// <summary>캐스팅 파워가 실제 거리로 이어지는 비율</summary>
        public double Reach;
        /// <summary>물고기 저항(꾹꾹 당김) 흡수율</summary>
        public double Absorb;
        /// <summary>챔질 허용 시간 추가(ms)</summary>
        public int HookBonus;
        /// <summary>낚시 화면에 그리는 대 색 (상점 그림 rod-단계.png 에서 땄다). 없으면 기본 갈색</summary>
        public RodLook Look;
    }

    /// <summary>낚싯대 색: 몸통, 손잡이, 고리와 띠 (#rrggbb)</summary>
    public sealed class RodLook
    {
        public string Blank;
        public string Handle;
        public string Trim;
    }

    /// <summary>릴 색: 몸통, 장식(핸들 노브·스풀 테) (#rrggbb)</summary>
    public sealed class ReelLook
    {
        public string Body;
        public string Accent;
    }

    public sealed class Reel
    {
        public string Name;
        public string Desc;
        public int Price;
        /// <summary>얻는 법 (Unlock 참고)</summary>
        public string Unlock;
        /// <summary>초당 감는 거리(m)</summary>
        public double Speed;
        /// <summary>감을 때 텐션 상승 배율</summary>
        public double TensionMul;
        /// <summary>이 텐션을 넘으면 드랙이 풀리며 상승이 느려진다</summary>
        public double Drag;
        /// <summary>고닷 힘싸움: 이 텐션 아래면 느슨하다 (0 이면 balance reel.slack)</summary>
        public double Slack;
        /// <summary>고닷 힘싸움: 이 텐션부터 위험 (0 이면 balance reel.danger). 좋은 릴일수록 초록이 넓다</summary>
        public double Danger;
        /// <summary>낚시 화면에 그리는 릴 색 (상점 그림 reel-단계.png 에서 땄다). 없으면 기본 은색</summary>
        public ReelLook Look;
    }

    /// <summary>소품(쿨러). 담을 수 있는 마릿수와 하루 신선도 감소</summary>
    public sealed class Tackle
    {
        public string Name;
        public string Desc;
        public int Price;
        /// <summary>얻는 법 (Unlock 참고)</summary>
        public string Unlock;
        public int Capacity;
        public int Decay;
    }

    public sealed class Bait
    {
        public string Name;
        public string Desc;
        public int Price;
        public int Amount;
    }
}

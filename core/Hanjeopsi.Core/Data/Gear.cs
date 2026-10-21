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
    }

    public sealed class Rod
    {
        public string Name;
        public string Desc;
        public int Price;
        /// <summary>캐스팅 파워가 실제 거리로 이어지는 비율</summary>
        public double Reach;
        /// <summary>물고기 저항(꾹꾹 당김) 흡수율</summary>
        public double Absorb;
        /// <summary>챔질 허용 시간 추가(ms)</summary>
        public int HookBonus;
    }

    public sealed class Reel
    {
        public string Name;
        public string Desc;
        public int Price;
        /// <summary>초당 감는 거리(m)</summary>
        public double Speed;
        /// <summary>감을 때 텐션 상승 배율</summary>
        public double TensionMul;
        /// <summary>이 텐션을 넘으면 드랙이 풀리며 상승이 느려진다</summary>
        public double Drag;
    }

    /// <summary>소품(쿨러). 담을 수 있는 마릿수와 하루 신선도 감소</summary>
    public sealed class Tackle
    {
        public string Name;
        public string Desc;
        public int Price;
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

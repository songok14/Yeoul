using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// townmap.json. 걷는 언덕 마을 (2026-10-28). 좌표는 지도 전체를 웹 논리 좌표 450×800 으로 본 값이다.
    /// 길은 점(nodes)과 이음(edges)으로 된 그물이고, 여울은 누른 곳에서 가장 가까운 길 위 점까지 이 그물을 따라 걷는다.
    /// </summary>
    public sealed class TownMap
    {
        /// <summary>시간대(아침 · 점심 · 저녁 · 밤)마다 지도 그림 이름 (godot/art/backgrounds)</summary>
        public List<string> Maps = new List<string>();
        /// <summary>화면 확대 (1이면 지도 가로가 화면 가로에 꽉 찬다)</summary>
        public float Zoom = 1;
        /// <summary>걷는 속도 (논리 좌표/초, 가장 앞쪽 크기 기준)</summary>
        public float WalkSpeed;
        /// <summary>앞쪽(광장)에서 SD 키 (논리 좌표)</summary>
        public float YeoulHeight;
        public float CatHeight;
        public float VillagerHeight;
        public TownScale Scale = new TownScale();
        /// <summary>마을에 들어오면 여울이 서는 점</summary>
        public string Start;
        public List<TownNode> Nodes = new List<TownNode>();
        public List<List<string>> Edges = new List<List<string>>();
        public List<TownPlace> Places = new List<TownPlace>();
        /// <summary>의뢰인이 서는 자리</summary>
        public List<TownPoint> ClientSpots = new List<TownPoint>();

        public TownNode NodeById(string id)
        {
            foreach (var n in Nodes)
                if (n.Id == id) return n;
            throw new KeyNotFoundException("없는 마을 길 점: " + id);
        }

        public TownPlace PlaceById(string id)
        {
            foreach (var p in Places)
                if (p.Id == id) return p;
            throw new KeyNotFoundException("없는 마을 장소: " + id);
        }
    }

    /// <summary>원근: 지도 위쪽(먼 곳)일수록 SD 를 작게. topY 에서 top 배, bottomY 에서 bottom 배, 그 아래로는 max 까지</summary>
    public sealed class TownScale
    {
        public float TopY;
        public float BottomY;
        public float Top = 1;
        public float Bottom = 1;
        public float Max = 1;
    }

    public sealed class TownNode
    {
        public string Id;
        public float X;
        public float Y;
    }

    public sealed class TownPoint
    {
        public float X;
        public float Y;
    }

    /// <summary>들어갈 수 있는 곳. 간판을 누르거나 문 앞(node)에 닿으면 들어간다</summary>
    public sealed class TownPlace
    {
        public string Id;
        /// <summary>간판 글</summary>
        public string Name;
        /// <summary>문 앞 길 점</summary>
        public string Node;
        public float SignX;
        public float SignY;
        /// <summary>들어갈 곳이 없으면 도착해서 여울이 하는 말</summary>
        public string Line;
    }
}

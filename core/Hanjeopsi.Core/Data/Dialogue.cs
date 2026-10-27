using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// dialogue.json. 화면에 흩어진 짧은 대사와 대화 장면.
    /// 대화는 말하는 사람의 감정 일러스트와 함께 대화창으로 나온다 (인게임 캐릭터는 SD, 대화와 감정은 일러스트).
    /// </summary>
    public sealed class Dialogue
    {
        /// <summary>할머니댁 막순 할머니 (웹 TownScene GRANDMA_LINES)</summary>
        public List<string> Grandma = new List<string>();
        /// <summary>고등어를 눌렀을 때 SD 말풍선 (상한 생선이 없을 때)</summary>
        public List<string> CatIdle = new List<string>();
        /// <summary>마을에서 SD 여울을 눌렀을 때 말풍선</summary>
        public List<string> YeoulIdle = new List<string>();
        /// <summary>노씨 미끼가게에 들어가거나 노씨를 눌렀을 때 (웹 ShopScene NOSSI_LINES)</summary>
        public List<string> Nossi = new List<string>();
        /// <summary>노씨가 상황에 따라 하는 말 (favor 노래미 의뢰 뒤, rod 감성돔대, gear 장비, bamboo 대나무대)</summary>
        public Dictionary<string, string> NossiSays = new Dictionary<string, string>();
        /// <summary>트럭 손님 (SD 그림 godot/art/customers/&lt;id&gt;-stand|walk|happy|sad.png 와 주문 말투)</summary>
        public List<TruckCustomer> Customers = new List<TruckCustomer>();
        /// <summary>마을 마당을 오가는 사람들 (SD 그림 godot/art/villagers/&lt;id&gt;-walk-1|walk-2|stand.png)</summary>
        public List<Villager> Villagers = new List<Villager>();
        /// <summary>대화에 나오는 사람과 감정별 일러스트</summary>
        public List<Speaker> Speakers = new List<Speaker>();
        /// <summary>이름 붙은 대화 장면 (welcome, chapterClear, chapterFail ...)</summary>
        public Dictionary<string, List<DialogueLine>> Scenes = new Dictionary<string, List<DialogueLine>>();
        /// <summary>날이 되면 마을에 들어올 때 한 번 나오는 이야기 장면 (고닷 1장)</summary>
        public List<StoryBeat> Beats = new List<StoryBeat>();

        public Speaker SpeakerById(string id)
        {
            foreach (var s in Speakers)
                if (s.Id == id) return s;
            throw new KeyNotFoundException("없는 화자 id: " + id);
        }
    }

    public sealed class StoryBeat
    {
        /// <summary>scenes 의 장면 이름</summary>
        public string Scene;
        /// <summary>이 날부터</summary>
        public int Day;
        /// <summary>이 날까지만 (지나면 건너뛴다). 0 이면 끝까지</summary>
        public int Until;
    }

    public sealed class Villager
    {
        public string Id;
        /// <summary>나오는 시간대 (0 새벽 ~ 4 밤)</summary>
        public List<int> Slots = new List<int>();
        /// <summary>SD 키 비율</summary>
        public float Height = 1;
        /// <summary>눌렀을 때 한 줄 말풍선</summary>
        public List<string> Lines = new List<string>();
    }

    public sealed class TruckCustomer
    {
        public string Id;
        /// <summary>주문 말풍선. {0} 은 요리 이름</summary>
        public string Order;
        /// <summary>SD 키 비율 (어른 1, 꼬마는 더 작게)</summary>
        public float Height = 1;
    }

    public sealed class Speaker
    {
        public string Id;
        /// <summary>대화창 이름표</summary>
        public string Name;
        /// <summary>이름표 색 (#rrggbb)</summary>
        public string Color;
        /// <summary>일러스트가 서는 쪽: left, right</summary>
        public string Side;
        /// <summary>감정 이름 → godot/art/portraits/ 파일 이름. default 는 꼭 있어야 한다</summary>
        public Dictionary<string, string> Faces = new Dictionary<string, string>();

        /// <summary>감정에 맞는 그림. 없으면 default</summary>
        public string Face(string face)
        {
            string file;
            if (!string.IsNullOrEmpty(face) && Faces.TryGetValue(face, out file)) return file;
            return Faces["default"];
        }
    }

    /// <summary>대사 한 줄. speaker 가 비어 있으면 설명 글(이름표와 일러스트 없이)</summary>
    public sealed class DialogueLine
    {
        public string Speaker;
        public string Face;
        public string Text;
        /// <summary>speakers 에 없는 사람(마을 의뢰인)의 이름표. Speaker 가 비었을 때만</summary>
        public string Name;
        /// <summary>그 사람의 SD 그림 (godot/art/ 아래 경로, 확장자 없이). 일러스트 대신 작게 선다</summary>
        public string Art;
    }
}

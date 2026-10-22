using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// dialogue.json. 화면에 흩어진 짧은 대사와 대화 장면.
    /// 대화는 말하는 사람의 감정 일러스트와 함께 대화창으로 나온다 (인게임 캐릭터는 SD, 대화와 감정은 일러스트).
    /// </summary>
    public sealed class Dialogue
    {
        /// <summary>여울식당 막순 할머니 (웹 TownScene GRANDMA_LINES)</summary>
        public List<string> Grandma = new List<string>();
        /// <summary>고등어를 눌렀을 때 SD 말풍선 (상한 생선이 없을 때)</summary>
        public List<string> CatIdle = new List<string>();
        /// <summary>마을에서 SD 여울을 눌렀을 때 말풍선</summary>
        public List<string> YeoulIdle = new List<string>();
        /// <summary>대화에 나오는 사람과 감정별 일러스트</summary>
        public List<Speaker> Speakers = new List<Speaker>();
        /// <summary>이름 붙은 대화 장면 (welcome, chapterClear, chapterFail ...)</summary>
        public Dictionary<string, List<DialogueLine>> Scenes = new Dictionary<string, List<DialogueLine>>();

        public Speaker SpeakerById(string id)
        {
            foreach (var s in Speakers)
                if (s.Id == id) return s;
            throw new KeyNotFoundException("없는 화자 id: " + id);
        }
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
    }
}

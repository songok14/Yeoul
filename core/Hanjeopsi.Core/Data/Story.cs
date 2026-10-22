using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// story.json. 프롤로그(p1, p2)와 1장 결말(clear, fail)의 이야기 장면 (웹 data/story.ts).
    /// 한 장면은 9:16 그림 한 장(배경 + 감정 일러스트)과 아래 글 상자 한 칸이다
    /// </summary>
    public sealed class Story
    {
        /// <summary>이야기 화면의 단추와 통지서 같은 그림 속 글</summary>
        public Dictionary<string, string> Ui = new Dictionary<string, string>();
        /// <summary>p1, p2, clear, fail</summary>
        public Dictionary<string, List<StoryPage>> Parts = new Dictionary<string, List<StoryPage>>();
    }

    public sealed class StoryPage
    {
        /// <summary>배경: kitchen, ash, phone, harbor, room, notice, storage, memory, breakwater, grill, night, dawn, seize</summary>
        public string Bg;
        /// <summary>올릴 일러스트 (godot/art/portraits 파일 이름). 없으면 배경만</summary>
        public string Pic;
        public string Text;
        /// <summary>글을 다 읽고 누르면 할 일: salt(소금 간 맞추기), cat(고등어가 볼락을 물고 달아난다)</summary>
        public string Event;
    }
}

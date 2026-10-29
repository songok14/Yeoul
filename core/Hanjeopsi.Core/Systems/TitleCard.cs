using System;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>
    /// 타이틀 화면의 챕터 부제 (docs/STORY.md 4절). 새 게임은 1장 부제,
    /// 이어하기는 세이브의 챕터 부제, 트루 엔딩을 본 뒤에는 따로 정한 부제.
    /// </summary>
    public static class TitleCard
    {
        /// <summary>트루 엔딩(놓아주기)을 봤다</summary>
        public const string TrueEndingFlag = "endingTrue";

        /// <summary>
        /// 세이브가 있는 챕터. 지금은 1장뿐이라 늘 1이다.
        /// 2장이 생기면 세이브에 챕터를 두고 여기서 읽는다.
        /// </summary>
        public static int Chapter(SaveData save)
        {
            return 1;
        }

        /// <summary>chapter 의 부제. 범위 밖이면 가까운 챕터로 맞춘다</summary>
        public static string Subtitle(TitleTexts t, int chapter, bool afterEnding)
        {
            if (afterEnding && !string.IsNullOrEmpty(t.AfterEnding)) return t.AfterEnding;
            return Entry(t, chapter).Subtitle;
        }

        /// <summary>세이브로 고른 부제. 세이브가 없으면 1장</summary>
        public static string Subtitle(TitleTexts t, SaveData save)
        {
            if (save == null) return Subtitle(t, 1, false);
            return Subtitle(t, Chapter(save), save.Flag(TrueEndingFlag));
        }

        /// <summary>"1장 물비늘포"</summary>
        public static string Foot(TitleTexts t, int chapter)
        {
            int n = Math.Max(1, Math.Min(chapter, t.Chapters.Count));
            return string.Format(t.Foot, n, Entry(t, chapter).Place);
        }

        private static ChapterTitle Entry(TitleTexts t, int chapter)
        {
            int i = Math.Max(0, Math.Min(chapter - 1, t.Chapters.Count - 1));
            return t.Chapters[i];
        }
    }
}

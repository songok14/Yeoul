using System.Linq;

namespace Hanjeopsi.Core
{
    /// <summary>1장 이야기 장면 (dialogue.json beats). 날이 되면 마을에 들어올 때 하나씩 나온다</summary>
    public sealed partial class Game
    {
        /// <summary>지금 볼 차례인 장면. 없으면 null. 본 장면은 MarkBeat 로 표시한다</summary>
        public string NextBeat()
        {
            int day = Save.Day;
            var beat = Data.Dialogue.Beats.FirstOrDefault(b =>
                day >= b.Day && (b.Until <= 0 || day <= b.Until) && !Save.Flag(BeatFlag(b.Scene)));
            return beat?.Scene;
        }

        public void MarkBeat(string scene)
        {
            Save.Flags[BeatFlag(scene)] = true;
            Persist();
        }

        private static string BeatFlag(string scene)
        {
            return "beat:" + scene;
        }
    }
}

namespace Hanjeopsi.Core.State
{
    /// <summary>쿨러 안 물고기 한 마리 (웹 state/save.ts FishItem). 필드 이름은 웹 세이브와 같다</summary>
    public sealed class FishItem
    {
        public int Uid;
        public string Species;
        public double Size;
        public int CaughtDay;
        /// <summary>신선도 0~100</summary>
        public int Fresh;

        public FishItem() { }

        public FishItem(int uid, string species, double size, int caughtDay, int fresh)
        {
            Uid = uid;
            Species = species;
            Size = size;
            CaughtDay = caughtDay;
            Fresh = fresh;
        }
    }
}

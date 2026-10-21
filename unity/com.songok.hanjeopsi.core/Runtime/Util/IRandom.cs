namespace Hanjeopsi.Core.Util
{
    /// <summary>0 이상 1 미만 난수. 테스트에서는 고정 시드, 게임에서는 아무 구현이나 넣는다</summary>
    public interface IRandom
    {
        double Next();
    }

    public static class RandomExtensions
    {
        /// <summary>Phaser.Math.FloatBetween과 같은 [min, max) 실수</summary>
        public static double Range(this IRandom r, double min, double max)
        {
            return min + (max - min) * r.Next();
        }

        /// <summary>Phaser.Math.Between과 같은 [min, max] 정수</summary>
        public static int RangeInt(this IRandom r, int min, int max)
        {
            return min + (int)System.Math.Floor(r.Next() * (max - min + 1));
        }
    }
}

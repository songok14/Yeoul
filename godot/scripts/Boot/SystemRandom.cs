using Hanjeopsi.Core.Util;

namespace Hanjeopsi.App.Boot
{
    /// <summary>게임 중 난수 (웹 Math.random 자리). 코어 규칙에는 IRandom 으로 넘긴다</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random _r = new System.Random();

        public double Next()
        {
            return _r.NextDouble();
        }
    }
}

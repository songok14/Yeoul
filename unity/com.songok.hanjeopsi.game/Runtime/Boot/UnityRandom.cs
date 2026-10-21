using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Game.Boot
{
    /// <summary>게임 중 난수 (웹 Math.random 자리). 코어 규칙에는 IRandom 으로 넘긴다</summary>
    public sealed class UnityRandom : IRandom
    {
        public double Next()
        {
            // Random.value 는 1 을 포함할 수 있어 [0, 1) 로 맞춘다
            float v = UnityEngine.Random.value;
            return v >= 1f ? 0.9999999 : v;
        }
    }
}

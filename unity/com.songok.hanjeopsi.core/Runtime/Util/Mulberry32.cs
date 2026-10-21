namespace Hanjeopsi.Core.Util
{
    /// <summary>
    /// 시드 고정 난수 mulberry32. utils.ts 와 비트 단위로 같은 값을 낸다.
    /// JS의 Math.imul과 &gt;&gt;&gt; 연산은 uint 곱셈과 시프트로 그대로 옮겨진다.
    /// </summary>
    public sealed class Mulberry32 : IRandom
    {
        private uint _a;

        public Mulberry32(uint seed)
        {
            _a = seed;
        }

        public double Next()
        {
            unchecked
            {
                _a += 0x6d2b79f5u;
                uint t = _a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>FNV 계열 해시 (utils.ts hash). 날짜·어종별 시드를 만든다</summary>
        public static uint Hash(params int[] nums)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (int n in nums)
                {
                    h ^= (uint)n;
                    h *= 16777619u;
                }
                return h;
            }
        }

        /// <summary>mulberry32(hash(...))() 를 한 번에</summary>
        public static double At(params int[] nums)
        {
            return new Mulberry32(Hash(nums)).Next();
        }
    }
}

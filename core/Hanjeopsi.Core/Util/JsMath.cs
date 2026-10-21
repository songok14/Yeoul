using System;
using System.Globalization;

namespace Hanjeopsi.Core.Util
{
    /// <summary>
    /// 웹 버전(JavaScript)과 똑같은 결과를 내기 위한 수학 함수.
    /// C#의 Math.Round는 은행가 반올림이라 JS Math.round와 다르다.
    /// </summary>
    public static class JsMath
    {
        /// <summary>JS Math.round. .5는 항상 +무한대 쪽으로 올린다 (-2.5 → -2)</summary>
        public static double Round(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return x;
            double r = Math.Floor(x);
            return x - r >= 0.5 ? r + 1 : r;
        }

        public static int RoundToInt(double x)
        {
            return (int)Round(x);
        }

        /// <summary>100원 단위로 반올림 (utils.ts round100)</summary>
        public static int Round100(double n)
        {
            return (int)(Round(n / 100) * 100);
        }

        public static double Clamp(double v, double lo, double hi)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        /// <summary>돈 표시. 1234567 → "1,234,567원" (utils.ts won)</summary>
        public static string Won(double n)
        {
            return Round(n).ToString("#,0", CultureInfo.InvariantCulture) + "원";
        }
    }
}

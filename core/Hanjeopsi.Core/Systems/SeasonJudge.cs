using System;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>간 맞추기 한 번의 결과</summary>
    public sealed class SeasonResult
    {
        /// <summary>정확도 0~1</summary>
        public double Quality;
        /// <summary>-1 모자람, 0 정답 구간, 1 지나침</summary>
        public int Side;

        public SeasonResult() { }

        public SeasonResult(double quality, int side)
        {
            Quality = quality;
            Side = side;
        }
    }

    /// <summary>간 맞추기 판정 (웹 systems/season.ts)</summary>
    public static class SeasonJudge
    {
        /// <summary>게이지를 멈춘 위치(pos)와 정답 중심(target), 정답 구간 반폭(half)으로 판정</summary>
        public static SeasonResult Judge(SeasonBalance b, double pos, double target, double half)
        {
            double d = Math.Abs(pos - target);
            double quality = d <= half ? 1 - b.InZoneDrop * (d / half) : JsMath.Clamp(b.OutMax - (d - half) / (half * b.OutWidth), 0, b.OutMax);
            int side = d <= half ? 0 : pos < target ? -1 : 1;
            return new SeasonResult(quality, side);
        }
    }
}

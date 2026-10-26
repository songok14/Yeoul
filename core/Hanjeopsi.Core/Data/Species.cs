namespace Hanjeopsi.Core.Data
{
    /// <summary>어종 한 종. species.json 한 항목 (웹 src/data/fish.ts)</summary>
    public sealed class Species
    {
        /// <summary>로마자 id. FishingPlan 도감 그림 이름(fish_&lt;id&gt;)과 같다</summary>
        public string Id;
        public string Name;
        public double MinSize;
        public double MaxSize;
        public double AvgSize;
        /// <summary>평균 크기 활어 한 마리 기준 경매가</summary>
        public int BasePrice;
        /// <summary>힘 1~5. 텐션이 오르는 속도와 저항 빈도에 영향</summary>
        public int Strength;
        public bool Rare;
        /// <summary>숙성하면 값이 오르는 어종</summary>
        public bool Aging;
        /// <summary>금지체장(cm). 이 길이 이하는 놓아줘야 한다. 없으면 null</summary>
        public double? LimitCm;
        /// <summary>금어기 [시작 월, 일, 끝 월, 일]. 없으면 null</summary>
        public int[] Closed;
        /// <summary>거리별 입질 가중치 [근거리, 중거리, 원거리]</summary>
        public double[] Zone;
        /// <summary>시간대별 가중치 [새벽, 아침, 낮, 저녁]</summary>
        public double[] Slot;
        /// <summary>귀한 정도 1(흔함)~3(귀함). FishingPlan 도감과 같다</summary>
        public int Stars = 1;
        /// <summary>달별 가중치 [1월..12월]. 제철 1, 걸쳐서 0.5, 철 아님 0.06 (고닷 낚시)</summary>
        public double[] Months;
        /// <summary>사는 바다: south(남해), east(동해), west(서해), jeju(제주). 지금 장의 바다에 있어야 낚인다</summary>
        public string[] Seas;
        /// <summary>낚이는 곳: breakwater(방파제), rock(갯바위), boat(배낚시). 지금 장의 낚시터에 있어야 낚인다 (없으면 어디든)</summary>
        public string[] Spots;
        /// <summary>도감 한 줄 힌트</summary>
        public string Hint;
        public string Desc;
    }
}

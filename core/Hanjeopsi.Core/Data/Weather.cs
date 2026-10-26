using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// weather.json. 하루 날씨와 효과 (고닷만, 웹에는 없다).
    /// 날씨는 세이브 시드와 날짜로 정해서 같은 판 같은 날은 늘 같다.
    /// </summary>
    public sealed class WeatherTable
    {
        /// <summary>날씨 종류. 첫 번째가 맑음</summary>
        public List<WeatherKind> Kinds = new List<WeatherKind>();
        /// <summary>1~12월마다 종류별 가중치 (Kinds 순서)</summary>
        public List<List<double>> Months = new List<List<double>>();
        public int SeedSalt;

        public WeatherKind ById(string id)
        {
            foreach (var k in Kinds)
                if (k.Id == id) return k;
            throw new KeyNotFoundException("없는 날씨 id: " + id);
        }
    }

    public sealed class WeatherKind
    {
        public string Id;
        public string Name;
        /// <summary>마을 소식에 붙는 한 줄</summary>
        public string Note;
        /// <summary>입질 대기 배율</summary>
        public double WaitMul = 1;
        /// <summary>장화가 있으면 입질 대기 불리함이 없어진다</summary>
        public bool BootsCancel;
        /// <summary>장사 손님 더하기 (빼기)</summary>
        public int GuestAdd;
        /// <summary>큰 놈 입질 확률 더하기</summary>
        public double BigAdd;
        /// <summary>고등어 힌트 정확도 빼기</summary>
        public double HintPenalty;
        /// <summary>방파제 출입 금지</summary>
        public bool NoFishing;
    }
}

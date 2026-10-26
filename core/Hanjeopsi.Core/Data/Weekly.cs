using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>
    /// weekly.json. 1장 주간 상환 (고닷만, 웹은 30일 한 번).
    /// 주마다 남은 원금의 이자가 붙고, 상환일(그 주 마지막 날)까지 남은 빚을 그 주 목표 아래로 낮춰야 한다.
    /// 못 맞추면 모자란 돈에 연체료가 붙어 다음 주로 넘어가고, MaxLate 번을 넘기면 차압
    /// </summary>
    public sealed class WeeklyTable
    {
        public int DaysPerWeek = 7;
        /// <summary>주 시작에 남은 빚에 붙는 이자</summary>
        public double InterestRate;
        /// <summary>상환일에 모자란 돈에 붙는 연체료</summary>
        public double LateRate;
        /// <summary>이만큼 연체는 봐준다. 넘으면 차압</summary>
        public int MaxLate = 1;
        public List<WeekDef> Weeks = new List<WeekDef>();
        public List<WeekEvent> Events = new List<WeekEvent>();
    }

    public sealed class WeekDef
    {
        /// <summary>이 주에 갚을 원금</summary>
        public int Principal;
        public List<WeekGoal> Goals = new List<WeekGoal>();
    }

    /// <summary>
    /// 주간 목표. type: catch(이번 주 N마리), happy(만족한 손님 N명), request(납품 N번), earn(이번 주 N원 벌기),
    /// species(이번 주 그 어종 낚기), dex(도감 N종), regulars(단골 N명)
    /// </summary>
    public sealed class WeekGoal
    {
        public string Type;
        public int N;
        public string Id;
        public string Text;
        public int Reward;
    }

    /// <summary>주말 이벤트 (그날 하루)</summary>
    public sealed class WeekEvent
    {
        public int Day;
        public string Id;
        public string Name;
        public string Note;
        public int GuestAdd;
        public double BigAdd;
        /// <summary>대결: 그날 이 길이(cm) 넘는 고기를 낚으면 상금</summary>
        public double RivalSize;
        public int RivalPrize;
    }
}

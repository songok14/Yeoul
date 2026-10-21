using System;
using System.Collections.Generic;
using System.Globalization;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>
    /// 1장 달력과 낚시 규정 (웹 systems/calendar.ts).
    /// 규정 수치는 FishingPlan 앱 기본표(core/catches/Limits.kt)를 따른다.
    /// </summary>
    public sealed class Rules
    {
        private readonly GameData _data;
        private readonly DateTime _start;

        public Rules(GameData data)
        {
            _data = data;
            var c = data.Balance.Chapter1;
            _start = new DateTime(c.StartYear, c.StartMonth, c.StartDate);
        }

        /// <summary>n일차의 날짜. 1장 1일차는 4월 16일</summary>
        public DateTime DateOf(int day)
        {
            return _start.AddDays(day - 1);
        }

        public string DateLabel(int day)
        {
            var d = DateOf(day);
            return string.Format(_data.Texts.Date, d.Month, d.Day);
        }

        /// <summary>
        /// 금어기인지. 12/1~1/31 처럼 해를 넘는 기간도 FishingPlan 과 같게 처리한다
        /// (웹 버전에는 아직 해를 넘는 금어기가 없다)
        /// </summary>
        public bool IsClosedSeason(string speciesId, int day)
        {
            var c = _data.SpeciesById(speciesId).Closed;
            if (c == null) return false;
            var d = DateOf(day);
            int v = d.Month * 100 + d.Day;
            int a = c[0] * 100 + c[1];
            int b = c[2] * 100 + c[3];
            return a <= b ? v >= a && v <= b : v >= a || v <= b;
        }

        /// <summary>금지체장 이하인지 (FishingPlan 기본표처럼 "이하")</summary>
        public bool IsUndersize(string speciesId, double size)
        {
            var limit = _data.SpeciesById(speciesId).LimitCm;
            return limit.HasValue && size <= limit.Value;
        }

        /// <summary>챙길 수 없는 이유. 문제없으면 null</summary>
        public string ReleaseReason(string speciesId, double size, int day)
        {
            var sp = _data.SpeciesById(speciesId);
            var t = _data.Texts.Rules;
            if (IsUndersize(speciesId, size)) return string.Format(CultureInfo.InvariantCulture, t.Undersize, sp.LimitCm.Value);
            if (IsClosedSeason(speciesId, day))
            {
                var c = sp.Closed;
                return string.Format(t.Closed, c[0], c[1], c[2], c[3]);
            }
            return null;
        }

        /// <summary>오늘 금어기인 어종 이름</summary>
        public List<string> ClosedToday(int day)
        {
            var list = new List<string>();
            foreach (var sp in _data.Species)
                if (IsClosedSeason(sp.Id, day)) list.Add(sp.Name);
            return list;
        }
    }
}

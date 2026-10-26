using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core
{
    /// <summary>
    /// 1장 주간 상환 (고닷 weekly.json). 주 시작에 남은 빚의 이자가 붙고, 상환일(7·14·21·28일)이 끝날 때
    /// 남은 빚이 그 주 목표보다 많으면 연체(모자란 돈의 연체료). 연체가 MaxLate 번을 넘거나 마지막 주에 빚이 남으면 차압.
    /// 주마다 목표 셋(보상)과 주말 이벤트. 웹 데이터면 Weekly 가 없어 웹과 같은 30일 규칙이다
    /// </summary>
    public sealed partial class Game
    {
        public bool IsWeekly
        {
            get { return Data.Weekly != null && Save.Week != null; }
        }

        private WeeklyTable W
        {
            get { return Data.Weekly; }
        }

        /// <summary>새 게임: 첫 주 시작. 주간 상환 전에 만든 세이브는 지금 날짜의 주부터 시작한다</summary>
        private void BeginWeeks()
        {
            if (Data.Weekly == null || Save.Week != null || Save.Flag("ch1Clear")) return;
            int no = Math.Min(Data.Weekly.Weeks.Count, Math.Max(1, (Save.Day - 1) / Data.Weekly.DaysPerWeek + 1));
            Save.Week = new WeekState { No = no };
            StartWeek();
        }

        /// <summary>주 시작: 이자를 붙이고 이번 주 목표 빚을 정한다</summary>
        private void StartWeek()
        {
            var wk = Save.Week;
            int interest = JsMath.Round100(Save.Debt * W.InterestRate);
            Save.Debt += interest;
            wk.Interest = interest;
            // 이번 주 원금 몫을 갚고 나서 남아야 하는 빚 (지난주 못 낸 돈은 빚에 그대로 남아 이번 주에 같이 낸다)
            int principalLeft = W.Weeks.Skip(wk.No).Sum(w => w.Principal);
            wk.Target = principalLeft;
            wk.Count.Clear();
            wk.Done.Clear();
            wk.RivalBest = 0;
        }

        public int Weeks
        {
            get { return W != null ? W.Weeks.Count : 0; }
        }

        /// <summary>상환일 (그 주 마지막 날)</summary>
        public int DueDay()
        {
            return IsWeekly ? Save.Week.No * W.DaysPerWeek : Data.Balance.Chapter1.DeadlineDays;
        }

        /// <summary>상환일까지 남은 날 (오늘이 상환일이면 0)</summary>
        public int DaysToDue()
        {
            return DueDay() - Save.Day;
        }

        /// <summary>이번 주에 더 갚아야 하는 돈</summary>
        public int DueNow()
        {
            if (!IsWeekly) return Save.Debt;
            return Math.Max(0, Save.Debt - Save.Week.Target);
        }

        /// <summary>오늘의 주말 이벤트 (없으면 null)</summary>
        public WeekEvent EventToday()
        {
            if (!IsWeekly) return null;
            return W.Events.FirstOrDefault(e => e.Day == Save.Day);
        }

        private int EventGuests()
        {
            var e = EventToday();
            return e != null ? e.GuestAdd : 0;
        }

        private double EventBig()
        {
            var e = EventToday();
            return e != null ? e.BigAdd : 0;
        }

        // ───────── 기록과 목표 ─────────

        private void Tally(string key, int n = 1)
        {
            if (!IsWeekly) return;
            int v;
            Save.Week.Count.TryGetValue(key, out v);
            Save.Week.Count[key] = v + n;
        }

        private int Tallied(string key)
        {
            int v;
            return IsWeekly && Save.Week.Count.TryGetValue(key, out v) ? v : 0;
        }

        /// <summary>낚은 고기 기록 (놓아줘도 낚은 것). 대결 날이면 가장 큰 길이를 남긴다</summary>
        private void TallyCatch(string species, double size)
        {
            if (!IsWeekly) return;
            Tally("catch");
            Tally("sp:" + species);
            var e = EventToday();
            if (e != null && e.RivalSize > 0) Save.Week.RivalBest = Math.Max(Save.Week.RivalBest, size);
        }

        public IList<WeekGoal> WeekGoals()
        {
            return IsWeekly ? W.Weeks[Save.Week.No - 1].Goals : (IList<WeekGoal>)new WeekGoal[0];
        }

        /// <summary>목표 진행 (지금 값, 목표 값)</summary>
        public (int Now, int Need) GoalProgress(WeekGoal g)
        {
            switch (g.Type)
            {
                case "catch": return (Tallied("catch"), g.N);
                case "happy": return (Tallied("happy"), g.N);
                case "request": return (Tallied("request"), g.N);
                case "earn": return (Tallied("earn"), g.N);
                case "species": return (Math.Min(1, Tallied("sp:" + g.Id)), 1);
                case "dex": return (Save.Dex.Count(d => d.Value.Count > 0), g.N);
                case "regulars": return (Diner == null ? 0 : Data.Diner.Guests.Count(x => Diner.IsRegular(Save, x.Id)), g.N);
                default: return (0, g.N);
            }
        }

        public bool GoalDone(int i)
        {
            return IsWeekly && Save.Week.Done.Contains(i);
        }

        /// <summary>새로 마친 목표에 보상을 준다. 마친 목표 목록</summary>
        public List<WeekGoal> CheckGoals()
        {
            var done = new List<WeekGoal>();
            if (!IsWeekly) return done;
            var goals = WeekGoals();
            for (int i = 0; i < goals.Count; i++)
            {
                if (Save.Week.Done.Contains(i)) continue;
                var p = GoalProgress(goals[i]);
                if (p.Now < p.Need) continue;
                Save.Week.Done.Add(i);
                Save.Money += goals[i].Reward;
                done.Add(goals[i]);
            }
            if (done.Count > 0) Persist();
            return done;
        }

        // ───────── 상환일 ─────────

        /// <summary>날이 끝날 때: 대결 결과와 상환일 정산</summary>
        private void WeekEndOfDay(DaySummary summary, int day)
        {
            if (!IsWeekly) return;
            var e = W.Events.FirstOrDefault(x => x.Day == day);
            if (e != null && e.RivalSize > 0)
            {
                bool win = Save.Week.RivalBest >= e.RivalSize;
                summary.Rival = win ? Save.Week.RivalBest : 0;
                if (win)
                {
                    Save.Money += e.RivalPrize;
                    Save.Flags["rivalWin1"] = true;
                }
            }
            var wk = Save.Week;
            if (day != wk.No * W.DaysPerWeek) return;
            var res = new WeekResult { No = wk.No };
            int shortBy = Math.Max(0, Save.Debt - wk.Target);
            res.Paid = shortBy == 0;
            res.Short = shortBy;
            if (!res.Paid)
            {
                wk.Late += 1;
                res.LateFee = JsMath.Round100(shortBy * W.LateRate);
                Save.Debt += res.LateFee;
                if (wk.Late > W.MaxLate || wk.No >= W.Weeks.Count) res.Seized = true;
            }
            if (res.Seized) Save.Flags["ch1Seized"] = true;
            else if (wk.No < W.Weeks.Count && Save.Debt > 0)
            {
                wk.No += 1;
                StartWeek();
                res.NextInterest = wk.Interest;
                res.NextDue = DueNow();
            }
            summary.Week = res;
        }
    }
}

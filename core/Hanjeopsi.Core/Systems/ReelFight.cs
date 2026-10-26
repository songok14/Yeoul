using System;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>
    /// 낚시 미니게임 판정 (웹 systems/reel.ts): 고등어 힌트 방향, 파워 게이지, 던진 거리, 챔질 허용 시간과 판정.
    /// 입질 대기는 BiteWait, 릴링은 ReelFight. 화면 코드는 프레임마다 Step 을 부르고 결과를 그리기만 한다.
    /// </summary>
    public sealed class ReelRules
    {
        private readonly GameData _data;
        private ReelBalance B { get { return _data.Balance.Reel; } }

        public ReelRules(GameData data)
        {
            _data = data;
        }

        /// <summary>입질이 몰리는 방향(-1 왼쪽, 0 앞, 1 오른쪽)과 고등어가 가리키는 방향. 호감이 높을수록 잘 맞힌다</summary>
        public (int Hot, int Hint) RollLanes(int affinity, IRandom rng)
        {
            int hot = rng.RangeInt(-1, 1);
            var c = _data.Balance.Cat;
            double accuracy = Math.Min(c.HintMax, c.HintBase + affinity * c.HintPerAffinity);
            int hint = rng.Next() < accuracy ? hot : rng.RangeInt(-1, 1);
            return (hot, hint);
        }

        /// <summary>파워 게이지 한 프레임. 끝에 닿으면 방향을 바꾼다</summary>
        public (double Pos, int Dir) StepPower(double pos, int dir, double dt)
        {
            pos += dir * B.GaugeSpeed * dt;
            if (pos >= 1) return (1, -1);
            if (pos <= 0) return (0, 1);
            return (pos, dir);
        }

        /// <summary>실제 던진 거리(0~1) = 파워 × 낚싯대 비거리</summary>
        public double CastDistance(double power, double reach)
        {
            return JsMath.Clamp(power * reach, B.MinCast, 1);
        }

        /// <summary>챔질 허용 시간(초). 좋은 낚싯대는 길고 귀한 고기는 짧다</summary>
        public double BiteWindow(Rod rod, Species sp, bool tutorial)
        {
            if (tutorial) return B.BiteTutorial;
            return B.BiteWindow + rod.HookBonus / 1000.0 + (sp.Rare ? -B.RareBitePenalty : 0);
        }

        /// <summary>찌가 들어가고 이 시간 안에 누르면 완벽한 챔질 (가까이에서 시작한다)</summary>
        public bool IsPerfectStrike(double elapsed)
        {
            return elapsed < B.PerfectBelow;
        }

        /// <summary>허용 시간이 지나면 미끼만 뺏긴다</summary>
        public bool BiteMissed(double elapsed, double window)
        {
            return elapsed > window;
        }

        public BiteWait StartWait(bool hot, bool tutorial, IRandom rng)
        {
            return new BiteWait(B, hot, tutorial, rng);
        }

        public ReelFight StartFight(FightSetup setup, IRandom rng)
        {
            return new ReelFight(B, setup, rng);
        }
    }

    /// <summary>입질 대기 한 프레임의 결과</summary>
    public struct WaitStep
    {
        /// <summary>찌를 톡 건드렸다 (속임 입질)</summary>
        public bool Nibble;
        /// <summary>찌가 쑥 들어갔다 (진짜 입질)</summary>
        public bool Bite;
    }

    /// <summary>찌가 들어가기 전. 속임 입질(톡)은 진짜 입질 전까지 여러 번 온다</summary>
    public sealed class BiteWait
    {
        private readonly ReelBalance _b;
        public double Wait { get; private set; }
        public double Nibble { get; private set; }

        internal BiteWait(ReelBalance b, bool hot, bool tutorial, IRandom rng)
        {
            _b = b;
            double baseWait = tutorial ? b.WaitTutorial : rng.Range(b.WaitMin, b.WaitMax);
            Wait = baseWait * (hot ? b.HotWaitMul : 1);
            Nibble = rng.Range(b.NibbleFirstMin, b.NibbleFirstMax);
        }

        public WaitStep Step(double dt, IRandom rng)
        {
            var r = new WaitStep();
            Nibble -= dt;
            if (Nibble <= 0)
            {
                Nibble = rng.Range(_b.NibbleMin, _b.NibbleMax);
                r.Nibble = true;
            }
            Wait -= dt;
            r.Bite = Wait <= 0;
            return r;
        }

        /// <summary>날씨·장비로 남은 대기 시간을 늘리거나 줄인다</summary>
        public void Scale(double mul)
        {
            Wait *= mul;
        }

        /// <summary>프롤로그에서 챔질을 놓쳤을 때 다시 기다리기 (속임 입질 타이머는 이어서)</summary>
        public void Retry()
        {
            Wait = _b.MissWaitTutorial;
        }
    }

    /// <summary>릴링이 끝난 까닭</summary>
    public enum FightEnd
    {
        None,
        /// <summary>텐션이 한계를 넘어 줄이 터졌다</summary>
        Snap,
        /// <summary>너무 오래 느슨해서 바늘이 빠졌다</summary>
        Slack,
        /// <summary>끌어올렸다</summary>
        Landed,
    }

    /// <summary>릴링 한 프레임의 결과</summary>
    public struct FightStep
    {
        /// <summary>꾹꾹 저항이 왔다 (화면 흔들기, 토스트)</summary>
        public bool Surge;
        /// <summary>프롤로그 구제: 줄이 끊어질 뻔했다 / 너무 느슨했다</summary>
        public bool RescueSnap;
        public bool RescueSlack;
        public FightEnd End;
    }

    public sealed class FightSetup
    {
        public Species Species;
        public double Size;
        /// <summary>던진 거리 0~1</summary>
        public double Dist01;
        public bool Perfect;
        public Rod Rod;
        public Reel Reel;
        public bool Tutorial;
    }

    /// <summary>물고기를 건 뒤 끌어올릴 때까지. 꾹 누르면 감고(텐션 상승), 떼면 텐션이 풀리며 물고기가 멀어진다</summary>
    public sealed class ReelFight
    {
        private readonly ReelBalance _b;

        public FightSetup Setup { get; }
        public double StartDistance { get; }
        public double Tension { get; private set; }
        public double Distance { get; private set; }
        public double SlackTime { get; private set; }
        public double SurgeTimer { get; private set; }
        /// <summary>저항 직후 남은 흔들림 시간 (그림자 연출용)</summary>
        public double SurgeKick { get; private set; }
        public double FightTime { get; private set; }
        public FightEnd Ended { get; private set; }

        internal ReelFight(ReelBalance b, FightSetup setup, IRandom rng)
        {
            _b = b;
            Setup = setup;
            StartDistance = b.StartDistanceBase + setup.Dist01 * b.StartDistancePerDist;
            Distance = StartDistance - (setup.Perfect ? b.PerfectBonus : 0);
            Tension = b.StartTension;
            SurgeTimer = setup.Tutorial ? b.SurgeTutorial : rng.Range(b.SurgeFirstMin, b.SurgeFirstMax);
        }

        /// <summary>물고기 힘. 오래 버틸수록 지쳐서 18초 뒤에는 절반 가까이 약해진다</summary>
        public double Strength()
        {
            var s = Setup;
            if (s.Tutorial) return _b.StrengthTutorial;
            double tired = 1 - _b.TiredMax * JsMath.Clamp(FightTime / _b.TiredTime, 0, 1);
            return s.Species.Strength * JsMath.Clamp(s.Size / s.Species.AvgSize, _b.SizeMin, _b.SizeMax) * tired;
        }

        /// <summary>바늘이 빠지기까지 느슨해도 되는 시간</summary>
        public double SlackLimit()
        {
            var s = Setup;
            if (s.Tutorial) return _b.SlackLimitTutorial;
            return _b.FastSpecies.Contains(s.Species.Id) ? _b.SlackLimitFast : _b.SlackLimit;
        }

        /// <summary>끌어온 정도 0~1</summary>
        public double Progress()
        {
            return JsMath.Clamp(1 - Distance / StartDistance, 0, 1);
        }

        /// <summary>dt 초만큼 진행. holding: 화면을 누르고 있는지</summary>
        public FightStep Step(bool holding, double dt, IRandom rng)
        {
            var r = new FightStep();
            if (Ended != FightEnd.None)
            {
                r.End = Ended;
                return r;
            }
            var reel = Setup.Reel;
            FightTime += dt;
            double str = Strength();

            if (holding)
            {
                double rise = (_b.RiseBase + str * _b.RisePerStr) * reel.TensionMul;
                if (Tension > reel.Drag) rise *= _b.OverDragMul;
                Tension += rise * dt;
                Distance -= reel.Speed * (Tension > _b.Slack ? 1 : _b.SlackReelMul) * dt;
            }
            else
            {
                Tension -= _b.RelaxRate * dt;
                Distance += str * _b.PullPerStr * dt;
            }

            SurgeTimer -= dt;
            if (SurgeTimer <= 0)
            {
                Tension += (_b.SurgeKickBase + str * _b.SurgeKickPerStr) * (1 - Setup.Rod.Absorb);
                Distance += _b.SurgeDistance;
                SurgeKick = _b.SurgeKickTime;
                SurgeTimer = rng.Range(_b.SurgeGapMin, _b.SurgeGapMax) / (_b.SurgeGapBase + str * _b.SurgeGapPerStr);
                r.Surge = true;
            }
            SurgeKick = Math.Max(0, SurgeKick - dt);

            Tension = JsMath.Clamp(Tension, 0, _b.MaxTension);
            Distance = Math.Min(Distance, StartDistance + _b.Overrun);

            if (Tension < _b.Slack) SlackTime += dt;
            else SlackTime = Math.Max(0, SlackTime - dt * _b.SlackRecover);

            if (Tension >= _b.Snap)
            {
                if (Setup.Tutorial)
                {
                    Tension = _b.RescueTension;
                    r.RescueSnap = true;
                }
                else
                {
                    r.End = Ended = FightEnd.Snap;
                    return r;
                }
            }
            if (SlackTime > SlackLimit())
            {
                if (Setup.Tutorial)
                {
                    SlackTime = 0;
                    r.RescueSlack = true;
                }
                else
                {
                    r.End = Ended = FightEnd.Slack;
                    return r;
                }
            }
            if (Distance <= 0) r.End = Ended = FightEnd.Landed;
            return r;
        }
    }
}

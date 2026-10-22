using System;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>힘싸움 단계</summary>
    public enum TugPhase
    {
        /// <summary>잠잠하다: 감으면 끌려오고 텐션을 지키면 지친다</summary>
        Calm,
        /// <summary>곧 한쪽으로 차고 나간다 (물보라와 화살표로 알린다)</summary>
        Warn,
        /// <summary>한쪽으로 질주 중: 반대쪽을 눌러 버텨야 한다</summary>
        Run,
    }

    /// <summary>힘싸움 한 프레임의 결과</summary>
    public struct TugStep
    {
        /// <summary>이번 프레임에 질주 예고가 시작됐다</summary>
        public bool WarnStart;
        /// <summary>이번 프레임에 질주가 시작됐다</summary>
        public bool RunStart;
        /// <summary>질주를 버텨 냈다 (끝까지 반대쪽을 눌렀다)</summary>
        public bool RunHeld;
        /// <summary>지금 반대쪽을 눌러 버티는 중</summary>
        public bool Bracing;
        /// <summary>질주 중에 같은 쪽을 눌러 텐션이 치솟는 중</summary>
        public bool WrongSide;
        /// <summary>물고기가 이번 프레임에 다 지쳤다</summary>
        public bool Exhausted;
        /// <summary>프롤로그 구제</summary>
        public bool RescueSnap;
        public bool RescueSlack;
        public FightEnd End;
    }

    /// <summary>
    /// 고닷 힘싸움 (웹 ReelFight 를 대신한다). 꾹 누르면 감고 떼면 풀리는 건 같고,
    /// 물고기가 가끔 왼쪽이나 오른쪽으로 질주한다. 그때 화면 반대쪽을 눌러 버티면 물고기 힘(Stamina)이 크게 빠진다.
    /// 다 지치면 질주하지 않고 힘도 약해져 쉽게 끌려온다. 수치는 balance.json tug (끊김·느슨 기준은 reel).
    /// </summary>
    public sealed class TugFight
    {
        private readonly ReelBalance _r;
        private readonly TugBalance _b;
        private readonly IRandom _rng;

        public FightSetup Setup { get; }
        public double StartDistance { get; }
        public double Tension { get; private set; }
        public double Distance { get; private set; }
        /// <summary>물고기 힘 1(팔팔) → 0(지침)</summary>
        public double Stamina { get; private set; } = 1;
        public TugPhase Phase { get; private set; } = TugPhase.Calm;
        /// <summary>질주 방향 -1 왼쪽, 1 오른쪽 (Warn, Run 일 때)</summary>
        public int RunDir { get; private set; }
        /// <summary>지금 단계가 끝나기까지 남은 시간 (Warn, Run) / 다음 질주까지 (Calm)</summary>
        public double PhaseLeft { get; private set; }
        public double SlackTime { get; private set; }
        public double FightTime { get; private set; }
        public int Runs { get; private set; }
        public int RunsHeld { get; private set; }
        public FightEnd Ended { get; private set; }
        /// <summary>이번 질주 동안 반대쪽을 누른 비율 (버텨 냈는지 판정)</summary>
        private double _braceTime, _runTime;

        internal TugFight(ReelBalance r, TugBalance b, FightSetup setup, IRandom rng)
        {
            _r = r;
            _b = b;
            _rng = rng;
            Setup = setup;
            StartDistance = r.StartDistanceBase + setup.Dist01 * r.StartDistancePerDist;
            Distance = StartDistance - (setup.Perfect ? r.PerfectBonus : 0);
            Tension = r.StartTension;
            if (setup.Perfect) Stamina -= b.PerfectStamina;
            PhaseLeft = setup.Tutorial ? b.FirstRunTutorial : rng.Range(b.FirstRunMin, b.FirstRunMax);
        }

        public bool Tired
        {
            get { return Stamina <= 0; }
        }

        /// <summary>물고기 힘: 어종 힘 × 크기 × 지친 정도</summary>
        public double Strength()
        {
            var s = Setup;
            if (s.Tutorial) return _r.StrengthTutorial * (0.5 + 0.5 * Stamina);
            double size = JsMath.Clamp(s.Size / s.Species.AvgSize, _r.SizeMin, _r.SizeMax);
            return s.Species.Strength * size * (_b.TiredFloor + (1 - _b.TiredFloor) * Stamina);
        }

        /// <summary>끌어온 정도 0~1</summary>
        public double Progress()
        {
            return JsMath.Clamp(1 - Distance / StartDistance, 0, 1);
        }

        private double SlackLimit()
        {
            var s = Setup;
            if (s.Tutorial) return _r.SlackLimitTutorial;
            return Array.IndexOf(_r.FastSpecies, s.Species.Id) >= 0 ? _r.SlackLimitFast : _r.SlackLimit;
        }

        /// <param name="holding">화면을 누르고 있는지</param>
        /// <param name="side">누른 자리: -1 화면 왼쪽 절반, 1 오른쪽 절반</param>
        public TugStep Step(bool holding, int side, double dt)
        {
            var res = new TugStep();
            if (Ended != FightEnd.None)
            {
                res.End = Ended;
                return res;
            }
            var reel = Setup.Reel;
            FightTime += dt;
            double str = Strength();
            double staminaWas = Stamina;

            // ── 단계 넘기기 ──
            PhaseLeft -= dt;
            if (PhaseLeft <= 0)
            {
                switch (Phase)
                {
                    case TugPhase.Calm:
                        if (!Tired)
                        {
                            Phase = TugPhase.Warn;
                            RunDir = _rng.Next() < 0.5 ? -1 : 1;
                            PhaseLeft = Setup.Tutorial ? _b.WarnTutorial : _b.WarnTime;
                            res.WarnStart = true;
                        }
                        else PhaseLeft = 1;
                        break;
                    case TugPhase.Warn:
                        Phase = TugPhase.Run;
                        PhaseLeft = _rng.Range(_b.RunMin, _b.RunMax) * (_b.RunTiredMul + (1 - _b.RunTiredMul) * Stamina);
                        _runTime = 0;
                        _braceTime = 0;
                        Runs += 1;
                        res.RunStart = true;
                        break;
                    case TugPhase.Run:
                        if (_runTime > 0 && _braceTime / _runTime >= _b.HeldRatio)
                        {
                            RunsHeld += 1;
                            res.RunHeld = true;
                        }
                        Phase = TugPhase.Calm;
                        PhaseLeft = _rng.Range(_b.GapMin, _b.GapMax) / (_b.GapBase + str * _b.GapPerStr);
                        break;
                }
            }

            if (Phase == TugPhase.Run)
            {
                _runTime += dt;
                bool brace = holding && side == -RunDir;
                bool wrong = holding && !brace;
                if (brace)
                {
                    _braceTime += dt;
                    res.Bracing = true;
                    // 버티면 텐션은 조금씩 오르고 줄은 거의 안 나가며 물고기가 크게 지친다
                    Tension += (_b.BraceRise + str * _b.BraceRisePerStr) * reel.TensionMul * dt;
                    Distance += str * _b.RunPull * _b.BraceLeak * dt;
                    Stamina -= (_b.BraceDrain + _b.BraceDrainPerTension * Tension / 100) / Math.Max(1, str * _b.StaminaPerStr) * dt;
                }
                else if (wrong)
                {
                    // 같은 쪽으로 당기면 줄이 비명을 지른다
                    res.WrongSide = true;
                    Tension += (_b.WrongRise + str * _b.WrongRisePerStr) * reel.TensionMul * dt;
                    Distance += str * _b.RunPull * 0.5 * dt;
                }
                else
                {
                    // 손을 떼면 줄이 풀려 나간다. 줄은 물고기가 당겨서 느슨해지지 않는다
                    Tension = Math.Max(_b.RunFloor, Tension - _r.RelaxRate * dt);
                    Distance += str * _b.RunPull * dt;
                }
            }
            else
            {
                if (holding)
                {
                    double rise = (_r.RiseBase + str * _r.RisePerStr) * reel.TensionMul;
                    if (Tension > reel.Drag) rise *= _r.OverDragMul;
                    Tension += rise * dt;
                    // 팔팔한 물고기는 잘 안 끌려온다. 지칠수록 감는 만큼 다가온다
                    double give = _b.FreshReelMul + (1 - _b.FreshReelMul) * (1 - Stamina);
                    Distance -= reel.Speed * give * (Tension > _r.Slack ? 1 : _r.SlackReelMul) * dt;
                    // 알맞게 팽팽하면 물고기가 지친다
                    double pressure = JsMath.Clamp((Tension - _r.Slack) / (_r.Danger - _r.Slack), 0, 1);
                    Stamina -= _b.CalmDrain * pressure / Math.Max(1, str * _b.StaminaPerStr) * dt;
                }
                else
                {
                    Tension -= _r.RelaxRate * dt;
                    Distance += str * _r.PullPerStr * dt;
                    Stamina += _b.Recover * dt;
                }
            }

            Stamina = JsMath.Clamp(Stamina, 0, 1);
            if (staminaWas > 0 && Stamina <= 0)
            {
                res.Exhausted = true;
                if (Phase != TugPhase.Calm)
                {
                    Phase = TugPhase.Calm;
                    PhaseLeft = 1;
                }
            }
            Tension = JsMath.Clamp(Tension, 0, _r.MaxTension);
            Distance = Math.Min(Distance, StartDistance + _b.MaxOverrun);

            if (Phase != TugPhase.Run && Tension < _r.Slack) SlackTime += dt;
            else SlackTime = Math.Max(0, SlackTime - dt * _r.SlackRecover);

            if (Tension >= _r.Snap)
            {
                if (Setup.Tutorial)
                {
                    Tension = _r.RescueTension;
                    res.RescueSnap = true;
                }
                else
                {
                    res.End = Ended = FightEnd.Snap;
                    return res;
                }
            }
            if (SlackTime > SlackLimit())
            {
                if (Setup.Tutorial)
                {
                    SlackTime = 0;
                    res.RescueSlack = true;
                }
                else
                {
                    res.End = Ended = FightEnd.Slack;
                    return res;
                }
            }
            if (Distance <= 0) res.End = Ended = FightEnd.Landed;
            return res;
        }
    }
}

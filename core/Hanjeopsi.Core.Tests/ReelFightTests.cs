using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>
    /// 낚시 미니게임이 웹 systems/reel.ts 와 같은 입력이면 같은 결과를 내는지 (core/golden/reel.json).
    /// 고닷 수치(godot/data/balance.json reel)는 낚시 시간을 줄이려고 바꿨으니 계산은 웹 수치로 비교한다
    /// </summary>
    public class ReelFightTests
    {
        private static readonly GameData D = TestData.WebReel;
        private readonly ReelRules _r = new ReelRules(D);
        private static JObject G { get { return TestData.ReelGolden; } }

        [Fact]
        public void 고닷_수치가_웹과_같은_키를_가진다()
        {
            var web = (JObject)G["tuning"];
            var mine = JObject.FromObject(TestData.Full.Balance.Reel, Newtonsoft.Json.JsonSerializer.Create(GameData.JsonSettings));
            Assert.Equal(web.Properties().Select(p => p.Name).OrderBy(n => n), mine.Properties().Select(p => p.Name).OrderBy(n => n));
        }

        [Fact]
        public void 고등어_힌트_방향이_웹과_같다()
        {
            var rngs = new Dictionary<int, Mulberry32>();
            foreach (JObject c in G["lanes"])
            {
                int seed = (int)c["seed"];
                if (!rngs.ContainsKey(seed)) rngs[seed] = new Mulberry32((uint)seed);
                var (hot, hint) = _r.RollLanes((int)c["affinity"], rngs[seed]);
                Assert.Equal((int)c["hot"], hot);
                Assert.Equal((int)c["hint"], hint);
            }
        }

        [Fact]
        public void 파워_게이지와_던진_거리가_웹과_같다()
        {
            double pos = 0;
            int dir = 1;
            foreach (JObject c in G["power"])
            {
                (pos, dir) = _r.StepPower(pos, dir, (double)c["dt"]);
                Assert.Equal((double)c["pos"], pos);
                Assert.Equal((int)c["dir"], dir);
            }
            foreach (JObject c in G["cast"])
                Assert.Equal((double)c["dist01"], _r.CastDistance((double)c["power"], (double)c["reach"]));
        }

        [Fact]
        public void 입질_대기가_웹과_같다()
        {
            foreach (JObject c in G["waits"])
            {
                int seed = (int)c["seed"];
                var rng = new Mulberry32((uint)seed);
                var dr = new Mulberry32((uint)(seed ^ 0x77));
                bool varDt = (bool)c["varDt"];
                var w = _r.StartWait((bool)c["hot"], (bool)c["tutorial"], rng);
                Assert.Equal((double)c["init"]["wait"], w.Wait);
                Assert.Equal((double)c["init"]["nibble"], w.Nibble);
                var nibbles = new List<int>();
                int bite = -1;
                for (int step = 0; step < 2000; step++)
                {
                    double dt = varDt ? Math.Min(0.05, 0.004 + dr.Next() * 0.06) : 1.0 / 60;
                    var res = w.Step(dt, rng);
                    if (res.Nibble) nibbles.Add(step);
                    if (res.Bite)
                    {
                        bite = step;
                        break;
                    }
                }
                Assert.Equal(c["nibbles"].ToObject<int[]>(), nibbles.ToArray());
                Assert.Equal((int)c["bite"], bite);
                Assert.Equal((double)c["end"]["wait"], w.Wait);
                Assert.Equal((double)c["end"]["nibble"], w.Nibble);
                w.Retry();
                Assert.Equal((double)c["retry"], w.Wait);
            }
        }

        [Fact]
        public void 챔질_허용_시간과_판정이_웹과_같다()
        {
            foreach (JObject c in G["windows"])
            {
                var rod = D.Gear.Rods[(int)c["rod"]];
                var sp = D.SpeciesById((string)c["species"]);
                Assert.Equal((double)c["window"], _r.BiteWindow(rod, sp, (bool)c["tutorial"]));
            }
            foreach (JObject c in G["strikes"])
            {
                double e = (double)c["elapsed"];
                Assert.Equal((bool)c["perfect"], _r.IsPerfectStrike(e));
                Assert.Equal((bool)c["missedAt062"], _r.BiteMissed(e, 0.62));
                Assert.Equal((bool)c["missedAt078"], _r.BiteMissed(e, 0.78));
            }
        }

        /// <summary>scripts/golden/reel.ts makeInput 과 같은 입력 규칙</summary>
        private static Func<double, (bool Holding, double Dt)> MakeInput(string policy, bool varDt, double threshold, IRandom r)
        {
            bool holding = policy != "never";
            return tension =>
            {
                double dt = varDt ? Math.Min(0.05, 0.004 + r.Next() * 0.06) : 1.0 / 60;
                if (policy == "smart") holding = tension < threshold;
                else if (policy == "random")
                {
                    if (r.Next() < 0.08) holding = !holding;
                }
                return (holding, dt);
            };
        }

        private static string EndName(FightEnd e)
        {
            switch (e)
            {
                case FightEnd.Snap: return "snap";
                case FightEnd.Slack: return "slack";
                case FightEnd.Landed: return "landed";
                default: return null;
            }
        }

        [Fact]
        public void 릴링이_웹과_프레임마다_같다()
        {
            int maxSteps = (int)G["maxSteps"];
            var fights = (JArray)G["fights"];
            Assert.True(fights.Count >= 200);
            foreach (JObject c in fights)
            {
                int seed = (int)c["seed"];
                var rng = new Mulberry32((uint)seed);
                var input = MakeInput((string)c["policy"], (bool)c["varDt"], (double)c["threshold"], new Mulberry32((uint)(seed ^ 0x51ed)));
                var setup = new FightSetup
                {
                    Species = D.SpeciesById((string)c["species"]),
                    Size = (double)c["size"],
                    Dist01 = (double)c["dist01"],
                    Perfect = (bool)c["perfect"],
                    Rod = D.Gear.Rods[(int)c["rod"]],
                    Reel = D.Gear.Reels[(int)c["reel"]],
                    Tutorial = (bool)c["tutorial"],
                };
                var f = _r.StartFight(setup, rng);
                string at = "싸움 " + seed;
                Assert.True((double)c["start"]["startDistance"] == f.StartDistance, at);
                Assert.True((double)c["start"]["distance"] == f.Distance, at);
                Assert.True((double)c["start"]["tension"] == f.Tension, at);
                Assert.True((double)c["start"]["surgeTimer"] == f.SurgeTimer, at);

                var trace = new Dictionary<int, JArray>();
                foreach (JArray t in c["trace"]) trace[(int)t[0]] = t;
                var events = new List<string>();
                int step = 0;
                for (; step < maxSteps; step++)
                {
                    var (holding, dt) = input(f.Tension);
                    var res = f.Step(holding, dt, rng);
                    if (res.Surge) events.Add(step + ":surge");
                    if (res.RescueSnap) events.Add(step + ":rescueSnap");
                    if (res.RescueSlack) events.Add(step + ":rescueSlack");
                    JArray t;
                    if (trace.TryGetValue(step, out t))
                    {
                        double[] mine = { f.Tension, f.Distance, f.SlackTime, f.SurgeTimer, f.SurgeKick, f.FightTime, f.Strength(), f.Progress() };
                        for (int k = 0; k < mine.Length; k++)
                            Assert.True((double)t[k + 1] == mine[k], at + " 프레임 " + step + " 값 " + k + ": 웹 " + (double)t[k + 1] + " / C# " + mine[k]);
                    }
                    if (res.End != FightEnd.None)
                    {
                        events.Add(step + ":" + EndName(res.End));
                        break;
                    }
                }
                Assert.Equal(c["events"].Select(e => (int)e[0] + ":" + (string)e[1]).ToArray(), events.ToArray());
                Assert.Equal((string)c["end"], EndName(f.Ended));
                Assert.Equal((int)c["steps"], step);
                var fin = c["final"];
                Assert.True((double)fin["tension"] == f.Tension, at);
                Assert.True((double)fin["distance"] == f.Distance, at);
                Assert.True((double)fin["slackTime"] == f.SlackTime, at);
                Assert.True((double)fin["fightTime"] == f.FightTime, at);
                Assert.True((double)fin["slackLimit"] == f.SlackLimit(), at);
            }
        }

        [Fact]
        public void 끝난_싸움은_더_움직이지_않는다()
        {
            var setup = new FightSetup
            {
                Species = D.SpeciesById("bollak"),
                Size = 20,
                Dist01 = 0.05,
                Perfect = true,
                Rod = D.Gear.Rods[2],
                Reel = D.Gear.Reels[2],
            };
            var rng = new Mulberry32(1);
            var f = _r.StartFight(setup, rng);
            FightStep res = default;
            for (int i = 0; i < 2000 && res.End == FightEnd.None; i++) res = f.Step(f.Tension < 70, 1.0 / 60, rng);
            Assert.Equal(FightEnd.Landed, res.End);
            double d = f.Distance;
            Assert.Equal(FightEnd.Landed, f.Step(true, 0.05, rng).End);
            Assert.Equal(d, f.Distance);
        }
    }
}

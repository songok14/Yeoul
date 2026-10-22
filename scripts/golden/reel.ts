// C# 코어 테스트용 기준값: 낚시 미니게임 (고등어 힌트, 파워 게이지, 입질 대기, 챔질, 릴링 물리)
// 입력(누르고 있는지, 프레임 시간)은 아래 drive() 규칙으로 만든다. C# 테스트도 같은 규칙으로 같은 입력을 만든다

import { SPECIES } from '../../src/data/fish';
import { REELS, RODS } from '../../src/data/gear';
import { biteMissed, biteWindow, BiteWait, castDistance, isPerfectStrike, REEL_TUNING, ReelFight, rollLanes, stepPower, type FightSetup } from '../../src/systems/reel';
import { mulberry32 } from '../../src/utils';

const POLICIES = ['smart', 'random', 'always', 'never'] as const;
type Policy = (typeof POLICIES)[number];

/** 입력 만들기: 정책에 따라 누름 여부, varDt 면 프레임 시간이 들쭉날쭉 (씬처럼 0.05초로 자른다) */
function makeInput(policy: Policy, varDt: boolean, threshold: number, r: () => number) {
  let holding = policy !== 'never';
  return (tension: number) => {
    const dt = varDt ? Math.min(0.05, 0.004 + r() * 0.06) : 1 / 60;
    if (policy === 'smart') holding = tension < threshold;
    else if (policy === 'random') {
      if (r() < 0.08) holding = !holding;
    }
    return { holding, dt };
  };
}

const MAX_STEPS = 6000;

function fight(setup: FightSetup, policy: Policy, varDt: boolean, threshold: number, seed: number, full: boolean) {
  const rnd = mulberry32(seed);
  const input = makeInput(policy, varDt, threshold, mulberry32(seed ^ 0x51ed));
  const f = new ReelFight(setup, rnd);
  const start = { startDistance: f.startDistance, distance: f.distance, tension: f.tension, surgeTimer: f.surgeTimer };
  const events: [number, string][] = [];
  const trace: number[][] = [];
  let step = 0;
  for (; step < MAX_STEPS; step++) {
    const { holding, dt } = input(f.tension);
    const res = f.step(holding, dt, rnd);
    if (res.surge) events.push([step, 'surge']);
    if (res.rescueSnap) events.push([step, 'rescueSnap']);
    if (res.rescueSlack) events.push([step, 'rescueSlack']);
    if (full || step % 25 === 0) trace.push([step, f.tension, f.distance, f.slackTime, f.surgeTimer, f.surgeKick, f.fightTime, f.strength(), f.progress()]);
    if (res.end) {
      events.push([step, res.end]);
      break;
    }
  }
  return {
    seed,
    species: setup.species.id,
    size: setup.size,
    dist01: setup.dist01,
    perfect: setup.perfect,
    rod: RODS.indexOf(setup.rod),
    reel: REELS.indexOf(setup.reel),
    tutorial: setup.tutorial,
    policy,
    varDt,
    threshold,
    start,
    events,
    end: f.ended,
    steps: step,
    final: { tension: f.tension, distance: f.distance, slackTime: f.slackTime, surgeTimer: f.surgeTimer, surgeKick: f.surgeKick, fightTime: f.fightTime, slackLimit: f.slackLimit() },
    trace,
  };
}

export function build(): unknown {
  // 고등어 힌트 방향
  const lanes = [];
  for (const seed of [3, 41, 2026]) {
    const r = mulberry32(seed);
    for (let affinity = 0; affinity <= 10; affinity++) {
      for (let k = 0; k < 30; k++) lanes.push({ seed, affinity, k, ...rollLanes(affinity, r) });
    }
  }

  // 파워 게이지 왕복과 던진 거리
  const power = [];
  {
    const r = mulberry32(17);
    let g = { pos: 0, dir: 1 };
    for (let i = 0; i < 400; i++) {
      const dt = Math.min(0.05, r() * 0.06);
      g = stepPower(g.pos, g.dir, dt);
      power.push({ dt, pos: g.pos, dir: g.dir });
    }
  }
  const cast = [];
  for (const rod of RODS) {
    for (const p of [0, 0.01, 0.0625, 0.2, 0.4375, 0.5, 0.75, 0.875, 0.9, 1]) cast.push({ power: p, reach: rod.reach, dist01: castDistance(p, rod.reach) });
  }

  // 입질 대기: 속임 입질이 오는 프레임과 진짜 입질 프레임
  const waits = [];
  for (let i = 0; i < 120; i++) {
    const seed = 500 + i;
    const r = mulberry32(seed);
    const hot = i % 3 === 0;
    const tutorial = i % 10 === 9;
    const varDt = i % 2 === 1;
    const w = new BiteWait(hot, tutorial, r);
    const init = { wait: w.wait, nibble: w.nibble };
    const nibbles: number[] = [];
    let bite = -1;
    const dr = mulberry32(seed ^ 0x77);
    for (let step = 0; step < 2000; step++) {
      const dt = varDt ? Math.min(0.05, 0.004 + dr() * 0.06) : 1 / 60;
      const res = w.step(dt, r);
      if (res.nibble) nibbles.push(step);
      if (res.bite) {
        bite = step;
        break;
      }
    }
    const end = { wait: w.wait, nibble: w.nibble };
    w.retry();
    waits.push({ seed, hot, tutorial, varDt, init, nibbles, bite, end, retry: w.wait });
  }

  // 챔질 허용 시간과 판정
  const windows = [];
  for (let rod = 0; rod < RODS.length; rod++) {
    for (const sp of SPECIES) {
      for (const tutorial of [false, true]) windows.push({ rod, species: sp.id, tutorial, window: biteWindow(RODS[rod], sp, tutorial) });
    }
  }
  const strikes = [0, 0.1, 0.2799, 0.28, 0.2801, 0.5, 0.62, 0.6201, 0.7, 0.78, 1.6, 1.61].map((e) => ({
    elapsed: e,
    perfect: isPerfectStrike(e),
    missedAt062: biteMissed(e, 0.62),
    missedAt078: biteMissed(e, 0.78),
  }));

  // 릴링: 어종 · 장비 · 크기 · 거리 · 입력 정책을 섞어 끝까지
  const fights = [];
  const pr = mulberry32(4242);
  for (let i = 0; i < 260; i++) {
    const tutorial = i % 13 === 12;
    const sp = tutorial ? SPECIES[0] : SPECIES[i % SPECIES.length];
    const size = tutorial ? 19.4 : Math.round((sp.minSize + pr() * (sp.maxSize - sp.minSize)) * 10) / 10;
    const rod = tutorial ? 1 : Math.floor(pr() * RODS.length);
    const reel = tutorial ? 1 : Math.floor(pr() * REELS.length);
    const dist01 = Math.max(0.05, Math.round(pr() * 1000) / 1000);
    const perfect = pr() < 0.3;
    const policy = POLICIES[Math.floor(i / 5) % POLICIES.length];
    const varDt = pr() < 0.5;
    const threshold = 50 + Math.floor(pr() * 40);
    const setup: FightSetup = { species: sp, size, dist01, perfect, rod: RODS[rod], reel: REELS[reel], tutorial };
    fights.push(fight(setup, policy, varDt, threshold, 9000 + i, i < 8));
  }

  return { source: 'src/systems/reel.ts', tuning: REEL_TUNING, maxSteps: MAX_STEPS, lanes, power, cast, waits, windows, strikes, fights };
}

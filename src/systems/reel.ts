// 낚시 미니게임 계산: 고등어 힌트 방향, 파워 게이지, 입질 대기와 속임 입질, 챔질 판정, 릴링 물리
// FishingScene 에서 동작 그대로 뺐다. 씬은 그리기와 입력만 맡는다.
// 고닷 C# core/Hanjeopsi.Core/Systems/ReelFight.cs 가 같은 입력이면 같은 결과를 내야 한다 (core/golden/reel.json)

import type { Species } from '../data/fish';
import type { Reel, Rod } from '../data/gear';
import { clamp } from '../utils';

export type Rand = () => number;

/** 낚시 미니게임 수치. 고닷 balance.json 의 reel 과 같다 */
export const REEL_TUNING = {
  /** 파워 게이지가 1초에 움직이는 양 (0~1 사이를 왕복) */
  gaugeSpeed: 1.25,
  /** 가장 가까이 던져도 이 거리(0~1)는 간다 */
  minCast: 0.05,

  /** 입질까지 기다리는 시간(초). 물 좋은 곳이면 짧아진다 */
  waitMin: 2.2,
  waitMax: 5.2,
  waitTutorial: 2.2,
  hotWaitMul: 0.55,
  /** 찌를 톡톡 건드리는 속임 입질 간격(초) */
  nibbleFirstMin: 0.6,
  nibbleFirstMax: 1.4,
  nibbleMin: 0.7,
  nibbleMax: 1.6,

  /** 챔질 허용 시간(초) = biteWindow + 낚싯대 hookBonus(ms)/1000, 귀한 고기는 짧다 */
  biteWindow: 0.62,
  biteTutorial: 1.6,
  rareBitePenalty: 0.08,
  /** 이 시간 안에 누르면 완벽한 챔질 */
  perfectBelow: 0.28,
  /** 프롤로그에서 챔질을 놓치면 다시 기다리는 시간 */
  missWaitTutorial: 2,

  /** 걸었을 때 거리(m) = base + 던진 거리 × perDist. 완벽한 챔질이면 bonus 만큼 가깝다 */
  startDistanceBase: 8,
  startDistancePerDist: 26,
  perfectBonus: 3,
  startTension: 35,
  /** 첫 꾹꾹 저항까지 시간 */
  surgeFirstMin: 0.8,
  surgeFirstMax: 1.8,
  surgeTutorial: 2.5,

  /** 텐션 구간: 느슨함 < slack, danger 이상은 빨간색, snap 이상이면 줄이 터진다 */
  slack: 12,
  danger: 85,
  snap: 100,
  maxTension: 110,
  /** 처음 거리보다 이만큼 더 멀어질 수는 없다 */
  overrun: 4,

  /** 감을 때 텐션 상승 = (riseBase + 힘 × risePerStr) × 릴 배율. 드랙을 넘으면 overDragMul 배 */
  riseBase: 26,
  risePerStr: 9,
  overDragMul: 0.3,
  /** 줄이 느슨할 때는 감는 속도가 이 배율 */
  slackReelMul: 0.6,
  /** 손을 떼면 텐션이 초당 이만큼 내려가고 물고기는 힘 × pullPerStr 만큼 멀어진다 */
  relaxRate: 48,
  pullPerStr: 0.55,

  /** 꾹꾹 저항 = (kickBase + 힘 × kickPerStr) × (1 - 낚싯대 흡수율) */
  surgeKickBase: 9,
  surgeKickPerStr: 5,
  surgeDistance: 1.2,
  /** 저항 뒤 그림자가 크게 흔들리는 시간 */
  surgeKickTime: 0.35,
  /** 다음 저항까지 = 난수(gapMin~gapMax) / (gapBase + 힘 × gapPerStr) */
  surgeGapMin: 1.1,
  surgeGapMax: 2.6,
  surgeGapBase: 0.7,
  surgeGapPerStr: 0.12,

  /** 느슨한 시간이 이만큼 쌓이면 바늘이 빠진다. 텐션이 돌아오면 두 배 빠르게 줄어든다 */
  slackLimit: 1.6,
  slackLimitFast: 1.1,
  slackLimitTutorial: 3.2,
  slackRecover: 2,
  /** 입이 약해 바늘이 잘 빠지는 어종 */
  fastSpecies: ['hakgongchi'] as string[],

  /** 지침: tiredTime 초 동안 힘이 tiredMax 만큼 줄어든다 */
  tiredMax: 0.55,
  tiredTime: 18,
  /** 힘에 곱하는 크기 비율(크기 / 평균) 범위 */
  sizeMin: 0.8,
  sizeMax: 1.5,
  strengthTutorial: 0.8,
  /** 프롤로그에서 줄이 터질 뻔하면 텐션을 여기로 */
  rescueTension: 70,
};

const R = REEL_TUNING;

/** Phaser.Math.FloatBetween 과 같은 [min, max) */
function range(rnd: Rand, min: number, max: number): number {
  return min + (max - min) * rnd();
}

/** Phaser.Math.Between 과 같은 [min, max] 정수 */
function rangeInt(rnd: Rand, min: number, max: number): number {
  return min + Math.floor(rnd() * (max - min + 1));
}

// ───────── 고등어 힌트와 캐스팅 ─────────

/** 입질이 몰리는 방향(-1 왼쪽, 0 앞, 1 오른쪽)과 고등어가 가리키는 방향. 호감이 높을수록 잘 맞힌다 */
export function rollLanes(affinity: number, rnd: Rand = Math.random): { hot: number; hint: number } {
  const hot = rangeInt(rnd, -1, 1);
  const accuracy = Math.min(0.95, 0.6 + affinity * 0.035);
  const hint = rnd() < accuracy ? hot : rangeInt(rnd, -1, 1);
  return { hot, hint };
}

/** 파워 게이지 한 프레임. 끝에 닿으면 방향을 바꾼다 */
export function stepPower(pos: number, dir: number, dt: number): { pos: number; dir: number } {
  pos += dir * R.gaugeSpeed * dt;
  if (pos >= 1) return { pos: 1, dir: -1 };
  if (pos <= 0) return { pos: 0, dir: 1 };
  return { pos, dir };
}

/** 실제 던진 거리(0~1) = 파워 × 낚싯대 비거리 */
export function castDistance(power: number, reach: number): number {
  return clamp(power * reach, R.minCast, 1);
}

// ───────── 입질 대기와 챔질 ─────────

/** 찌가 들어가기 전. 속임 입질(톡)은 진짜 입질 전까지 여러 번 온다 */
export class BiteWait {
  wait: number;
  nibble: number;

  constructor(hot: boolean, tutorial: boolean, rnd: Rand = Math.random) {
    const base = tutorial ? R.waitTutorial : range(rnd, R.waitMin, R.waitMax);
    this.wait = base * (hot ? R.hotWaitMul : 1);
    this.nibble = range(rnd, R.nibbleFirstMin, R.nibbleFirstMax);
  }

  /** 한 프레임. 속임 입질이 왔는지, 진짜 입질이 왔는지 */
  step(dt: number, rnd: Rand = Math.random): { nibble: boolean; bite: boolean } {
    let nibble = false;
    this.nibble -= dt;
    if (this.nibble <= 0) {
      this.nibble = range(rnd, R.nibbleMin, R.nibbleMax);
      nibble = true;
    }
    this.wait -= dt;
    return { nibble, bite: this.wait <= 0 };
  }

  /** 프롤로그에서 챔질을 놓쳤을 때 다시 기다리기 (속임 입질 타이머는 이어서) */
  retry(): void {
    this.wait = R.missWaitTutorial;
  }
}

/** 챔질 허용 시간(초). 좋은 낚싯대는 길고 귀한 고기는 짧다 */
export function biteWindow(rod: Rod, sp: Species, tutorial: boolean): number {
  if (tutorial) return R.biteTutorial;
  return R.biteWindow + rod.hookBonus / 1000 + (sp.rare ? -R.rareBitePenalty : 0);
}

/** 찌가 들어가고 이 시간 안에 누르면 완벽한 챔질 (가까이에서 시작한다) */
export function isPerfectStrike(elapsed: number): boolean {
  return elapsed < R.perfectBelow;
}

/** 허용 시간이 지나면 미끼만 뺏긴다 */
export function biteMissed(elapsed: number, window: number): boolean {
  return elapsed > window;
}

// ───────── 릴링 ─────────

export interface FightStep {
  /** 꾹꾹 저항이 왔다 */
  surge: boolean;
  /** 프롤로그 구제: 줄이 끊어질 뻔했다 / 너무 느슨했다 (텐션이나 느슨한 시간을 되돌린다) */
  rescueSnap: boolean;
  rescueSlack: boolean;
  /** 끝: 줄이 터짐 / 바늘이 빠짐 / 끌어올림 */
  end: 'snap' | 'slack' | 'landed' | null;
}

export interface FightSetup {
  species: Species;
  size: number;
  /** 던진 거리 0~1 */
  dist01: number;
  perfect: boolean;
  rod: Rod;
  reel: Reel;
  tutorial: boolean;
}

/** 물고기를 건 뒤 끌어올릴 때까지. 꾹 누르면 감고(텐션 상승), 떼면 텐션이 풀리며 물고기가 멀어진다 */
export class ReelFight {
  readonly setup: FightSetup;
  readonly startDistance: number;
  tension: number;
  distance: number;
  slackTime = 0;
  surgeTimer: number;
  /** 저항 직후 남은 흔들림 시간 (그림자 연출용) */
  surgeKick = 0;
  fightTime = 0;
  ended: FightStep['end'] = null;

  constructor(setup: FightSetup, rnd: Rand = Math.random) {
    this.setup = setup;
    this.startDistance = R.startDistanceBase + setup.dist01 * R.startDistancePerDist;
    this.distance = this.startDistance - (setup.perfect ? R.perfectBonus : 0);
    this.tension = R.startTension;
    this.surgeTimer = setup.tutorial ? R.surgeTutorial : range(rnd, R.surgeFirstMin, R.surgeFirstMax);
  }

  /** 물고기 힘. 오래 버틸수록 지쳐서 18초 뒤에는 절반 가까이 약해진다 */
  strength(): number {
    const s = this.setup;
    if (s.tutorial) return R.strengthTutorial;
    const tired = 1 - R.tiredMax * clamp(this.fightTime / R.tiredTime, 0, 1);
    return s.species.strength * clamp(s.size / s.species.avgSize, R.sizeMin, R.sizeMax) * tired;
  }

  /** 바늘이 빠지기까지 느슨해도 되는 시간 */
  slackLimit(): number {
    const s = this.setup;
    if (s.tutorial) return R.slackLimitTutorial;
    return R.fastSpecies.includes(s.species.id) ? R.slackLimitFast : R.slackLimit;
  }

  /** 끌어온 정도 0~1 */
  progress(): number {
    return clamp(1 - this.distance / this.startDistance, 0, 1);
  }

  /** dt 초만큼 진행. holding: 화면을 누르고 있는지 */
  step(holding: boolean, dt: number, rnd: Rand = Math.random): FightStep {
    const out: FightStep = { surge: false, rescueSnap: false, rescueSlack: false, end: null };
    if (this.ended) {
      out.end = this.ended;
      return out;
    }
    const { reel, rod, tutorial } = this.setup;
    this.fightTime += dt;
    const str = this.strength();

    if (holding) {
      let rise = (R.riseBase + str * R.risePerStr) * reel.tensionMul;
      if (this.tension > reel.drag) rise *= R.overDragMul;
      this.tension += rise * dt;
      this.distance -= reel.speed * (this.tension > R.slack ? 1 : R.slackReelMul) * dt;
    } else {
      this.tension -= R.relaxRate * dt;
      this.distance += str * R.pullPerStr * dt;
    }

    this.surgeTimer -= dt;
    if (this.surgeTimer <= 0) {
      this.tension += (R.surgeKickBase + str * R.surgeKickPerStr) * (1 - rod.absorb);
      this.distance += R.surgeDistance;
      this.surgeKick = R.surgeKickTime;
      this.surgeTimer = range(rnd, R.surgeGapMin, R.surgeGapMax) / (R.surgeGapBase + str * R.surgeGapPerStr);
      out.surge = true;
    }
    this.surgeKick = Math.max(0, this.surgeKick - dt);

    this.tension = clamp(this.tension, 0, R.maxTension);
    this.distance = Math.min(this.distance, this.startDistance + R.overrun);

    if (this.tension < R.slack) this.slackTime += dt;
    else this.slackTime = Math.max(0, this.slackTime - dt * R.slackRecover);

    if (this.tension >= R.snap) {
      if (tutorial) {
        this.tension = R.rescueTension;
        out.rescueSnap = true;
      } else {
        out.end = this.ended = 'snap';
        return out;
      }
    }
    if (this.slackTime > this.slackLimit()) {
      if (tutorial) {
        this.slackTime = 0;
        out.rescueSlack = true;
      } else {
        out.end = this.ended = 'slack';
        return out;
      }
    }
    if (this.distance <= 0) out.end = this.ended = 'landed';
    return out;
  }
}

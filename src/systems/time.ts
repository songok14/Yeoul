// 하루 시간 슬롯과 날짜 넘김

import { CH1_DEADLINE } from '../config';
import { REQUEST_TEMPLATES } from '../data/requests';
import { SPECIES_BY_ID } from '../data/fish';
import { S, persist, type DaySummary } from '../state/save';
import { hash, mulberry32, round100 } from '../utils';
import { dailyDecay, stageOf, todayBasePrice } from './economy';

export const SLOT_NAME = ['새벽', '아침', '낮', '저녁'] as const;

export function slotName(slot = S.slot): string {
  return SLOT_NAME[slot] ?? '밤';
}

export function daysLeft(): number {
  return CH1_DEADLINE - S.day;
}

export function isMarketOpen(): boolean {
  return S.slot <= 1;
}

export function isTruckOpen(): boolean {
  return S.slot >= 1;
}

/** 행동 하나로 시간 한 칸을 쓴다. 저녁이 지나면 날이 바뀐다 */
export function useSlot(): void {
  S.slot += 1;
  if (S.slot > 3) endDay();
  else persist();
}

export function endDay(): void {
  const summary: DaySummary = { day: S.day, earned: S.earnedToday, spoiled: 0, expiredRequests: 0 };

  S.day += 1;
  S.slot = 0;
  S.earnedToday = 0;

  const decay = dailyDecay();
  for (const f of S.cooler) {
    const wasOk = stageOf(f.fresh) !== 'spoiled';
    f.fresh = Math.max(0, f.fresh - decay);
    if (wasOk && stageOf(f.fresh) === 'spoiled') summary.spoiled += 1;
  }

  const before = S.requests.length;
  S.requests = S.requests.filter((r) => r.deadline >= S.day);
  summary.expiredRequests = before - S.requests.length;
  refreshRequests();

  S.pendingSummary = summary;
  persist();
}

/** 의뢰는 최대 2개. 날마다 빈자리가 있으면 새 의뢰가 들어온다 */
export function refreshRequests(): void {
  const rng = mulberry32(hash(S.seed, S.day, 99));
  let guard = 0;
  while (S.requests.length < 2 && guard++ < 10) {
    const template = Math.floor(rng() * REQUEST_TEMPLATES.length);
    if (S.requests.some((r) => r.template === template)) continue;
    const t = REQUEST_TEMPLATES[template];
    // 감성돔 의뢰는 첫 주에는 나오지 않는다
    if (SPECIES_BY_ID[t.species].rare && S.day < 5) continue;
    S.requests.push({
      id: S.nextRequestId++,
      template,
      reward: round100(todayBasePrice(t.species) * t.count * t.mul * 1.25),
      deadline: S.day + 3 + Math.floor(rng() * 3),
    });
  }
}

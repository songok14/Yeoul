// 상태를 바꾸는 행동 모음. 바꾼 뒤에는 항상 저장한다

import { SPECIES_BY_ID, type SpeciesId } from '../data/fish';
import { BAIT, REELS, RODS, TACKLES } from '../data/gear';
import { REQUEST_TEMPLATES } from '../data/requests';
import { coolerCapacity, fishValue, stageOf, type FreshStage } from '../systems/economy';
import { refreshRequests } from '../systems/time';
import { averageQuality, bestFishFor, dishPay, type SeasonResult } from '../systems/truck';
import type { Recipe } from '../data/recipes';
import { createSave, persist, S, setState, type FishItem } from './save';

export function newGame(): void {
  setState(createSave());
  refreshRequests();
  persist();
}

export function earn(amount: number): void {
  S.money += amount;
  S.earnedToday += amount;
  S.totalEarned += amount;
}

/** 도감 기록. 처음 낚은 어종이면 true */
export function recordCatch(species: SpeciesId, size: number, released: boolean): boolean {
  const prev = S.dex[species];
  const isNew = !prev || prev.count === 0;
  const e = prev ?? { count: 0, best: 0, released: 0 };
  e.count += 1;
  e.best = Math.max(e.best, size);
  if (released) e.released += 1;
  S.dex[species] = e;
  persist();
  return isNew;
}

export function coolerFull(): boolean {
  return S.cooler.length >= coolerCapacity();
}

export function addToCooler(species: SpeciesId, size: number): FishItem | null {
  if (coolerFull()) return null;
  const item: FishItem = { uid: S.nextUid++, species, size, caughtDay: S.day, fresh: 100 };
  S.cooler.push(item);
  persist();
  return item;
}

export function removeFish(uids: number[]): void {
  const set = new Set(uids);
  S.cooler = S.cooler.filter((f) => !set.has(f.uid));
}

export function sellFish(uids: number[], mul = 1): number {
  let total = 0;
  for (const f of S.cooler) if (uids.includes(f.uid)) total += fishValue(f);
  total = Math.round((total * mul) / 100) * 100;
  removeFish(uids);
  earn(total);
  persist();
  return total;
}

export function buyBait(): boolean {
  if (S.money < BAIT.price) return false;
  S.money -= BAIT.price;
  S.bait += BAIT.amount;
  persist();
  return true;
}

export type GearSlot = 'rod' | 'reel' | 'tackle';

export function gearList(slot: GearSlot) {
  return slot === 'rod' ? RODS : slot === 'reel' ? REELS : TACKLES;
}

export function upgradeGear(slot: GearSlot): boolean {
  const list = gearList(slot);
  const next = S[slot] + 1;
  if (next >= list.length) return false;
  const price = list[next].price;
  if (S.money < price) return false;
  S.money -= price;
  S[slot] = next;
  persist();
  return true;
}

export function repay(amount: number): number {
  const pay = Math.min(amount, S.money, S.debt);
  if (pay <= 0) return 0;
  S.money -= pay;
  S.debt -= pay;
  persist();
  return pay;
}

/** 의뢰에 맞는 물고기 (상하지도 숙성되지도 않은 것 중 큰 순서) */
export function fishForRequest(reqId: number): FishItem[] | null {
  const req = S.requests.find((r) => r.id === reqId);
  if (!req) return null;
  const t = REQUEST_TEMPLATES[req.template];
  const ok = S.cooler
    .filter((f) => f.species === t.species && (stageOf(f.fresh) === 'live' || stageOf(f.fresh) === 'fresh'))
    .sort((a, b) => b.size - a.size);
  return ok.length >= t.count ? ok.slice(0, t.count) : null;
}

export function deliverRequest(reqId: number): number {
  const fish = fishForRequest(reqId);
  const req = S.requests.find((r) => r.id === reqId);
  if (!fish || !req) return 0;
  removeFish(fish.map((f) => f.uid));
  S.requests = S.requests.filter((r) => r.id !== reqId);
  earn(req.reward);
  if (REQUEST_TEMPLATES[req.template].client === '노씨') S.flags.nossiFavor = true;
  persist();
  return req.reward;
}

export function spoiledFish(): FishItem[] {
  return S.cooler.filter((f) => stageOf(f.fresh) === 'spoiled');
}

/** 상한 생선을 고등어에게 준다. 호감이 오르면 입질 힌트가 정확해진다 */
export function feedCat(): number {
  const bad = spoiledFish();
  if (bad.length === 0) return 0;
  removeFish(bad.map((f) => f.uid));
  S.catAffinity = Math.min(10, S.catAffinity + bad.length);
  persist();
  return bad.length;
}

export function speciesName(id: SpeciesId): string {
  return SPECIES_BY_ID[id].name;
}

/** 트럭에서 요리 한 접시를 낸다. 가장 좋은 재료를 쓰고 값을 받는다. 재료가 없으면 null */
export function serveDish(recipe: Recipe, results: SeasonResult[]): { stage: FreshStage; avg: number; pay: number; firstTime: boolean } | null {
  const fish = bestFishFor(recipe);
  if (!fish) return null;
  const stage = stageOf(fish.fresh);
  removeFish([fish.uid]);
  const avg = averageQuality(results);
  const pay = dishPay(recipe, stage, avg);
  earn(pay);
  S.dishesCooked += 1;
  const firstTime = !S.recipesCooked.includes(recipe.id);
  if (firstTime) S.recipesCooked.push(recipe.id);
  persist();
  return { stage, avg, pay, firstTime };
}

/** 프롤로그를 마쳤다. 볼락구이로 짠맛이 돌아온다 */
export function completePrologue(): void {
  S.flags.prologueDone = true;
  S.flags.firstTaste = true;
  S.flags.taste1 = true;
  S.dishesCooked = Math.max(1, S.dishesCooked);
  persist();
}

/** 캐스팅 한 번에 미끼 하나 */
export function useBait(): void {
  S.bait -= 1;
  persist();
}

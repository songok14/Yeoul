// 유니티 코어 테스트용 기준값: 새 게임부터 행동을 이어 가며 세이브 상태를 통째로 기록한다
// 행동은 시드 고정 난수로 고르고, 고른 인자를 함께 남겨서 C# 이 똑같이 따라 하게 한다

import { SPECIES } from '../../src/data/fish';
import { RECIPES } from '../../src/data/recipes';
import { REELS, RODS, TACKLES } from '../../src/data/gear';
import {
  addToCooler, buyBait, coolerFull, completePrologue, deliverRequest, earn, feedCat, fishForRequest, recordCatch,
  repay, sellFish, serveDish, spoiledFish, upgradeGear, useBait, type GearSlot,
} from '../../src/state/actions';
import { createSave, S, setState } from '../../src/state/save';
import { closedToday, dateLabel, releaseReason } from '../../src/systems/calendar';
import { chapterOutcome } from '../../src/systems/chapter';
import { stageOf } from '../../src/systems/economy';
import { judgeSeason } from '../../src/systems/season';
import { checkTasteUnlock, speciesCaught, tasteLevel } from '../../src/systems/taste';
import { daysLeft, endDay, isMarketOpen, isTruckOpen, refreshRequests, slotName, useSlot } from '../../src/systems/time';
import { cookable } from '../../src/systems/truck';
import { mulberry32 } from '../../src/utils';

const snap = () => JSON.parse(JSON.stringify(S));
const view = () => ({
  tasteLevel: tasteLevel(),
  speciesCaught: speciesCaught(),
  daysLeft: daysLeft(),
  slotName: slotName(),
  marketOpen: isMarketOpen(),
  truckOpen: isTruckOpen(),
  dateLabel: dateLabel(),
  closedToday: closedToday(),
  coolerFull: coolerFull(),
  spoiled: spoiledFish().length,
  cookable: cookable().map((r) => r.id),
});

function run(seed: number, steps: number) {
  setState(createSave(seed));
  refreshRequests();
  const log: unknown[] = [{ op: 'new', args: { seed }, result: null, state: snap(), view: view() }];
  const r = mulberry32(seed ^ 0x5eed);
  const push = (op: string, args: unknown, result: unknown) => log.push({ op, args, result, state: snap(), view: view() });

  for (let i = 0; i < steps; i++) {
    if (i === 3) push('completePrologue', {}, completePrologue());
    const x = r();
    if (x < 0.32) {
      // 한 마리 낚기: 규정 확인 → 도감 기록 → 쿨러
      if (S.bait <= 0) {
        push('buyBait', {}, buyBait());
        continue;
      }
      push('useBait', {}, useBait());
      const sp = SPECIES[Math.floor(r() * SPECIES.length)];
      const size = Math.round((sp.minSize + r() * (sp.maxSize - sp.minSize)) * 10) / 10;
      const rule = releaseReason(sp.id, size);
      push('releaseReason', { species: sp.id, size }, rule);
      const released = rule !== null || coolerFull() || r() < 0.08;
      push('recordCatch', { species: sp.id, size, released }, recordCatch(sp.id, size, released));
      if (!released) push('addToCooler', { species: sp.id, size }, addToCooler(sp.id, size)?.uid ?? null);
      push('checkTasteUnlock', {}, checkTasteUnlock()?.name ?? null);
    } else if (x < 0.42) {
      if (!isMarketOpen()) continue;
      const ok = S.cooler.filter((f) => stageOf(f.fresh) !== 'spoiled' && r() < 0.6).map((f) => f.uid);
      if (ok.length === 0) continue;
      const mul = 0.92 + r() * 0.23;
      push('sellFish', { uids: ok, mul }, sellFish(ok, mul));
    } else if (x < 0.56) {
      if (!isTruckOpen()) continue;
      const menu = cookable();
      if (menu.length === 0) continue;
      const recipe = menu[Math.floor(r() * menu.length)];
      const results = recipe.steps.map((s) => judgeSeason(r(), s.target, 0.085));
      push('serveDish', { recipe: recipe.id, results }, serveDish(recipe, results));
    } else if (x < 0.62) {
      const req = S.requests.find((q) => fishForRequest(q.id) !== null);
      if (!req) continue;
      push('deliverRequest', { id: req.id }, deliverRequest(req.id));
    } else if (x < 0.66) {
      push('feedCat', {}, feedCat());
    } else if (x < 0.7) {
      const slot = (['rod', 'reel', 'tackle'] as GearSlot[])[Math.floor(r() * 3)];
      push('upgradeGear', { slot }, upgradeGear(slot));
    } else if (x < 0.74) {
      const amount = r() < 0.5 ? 100_000 : Math.min(S.money, S.debt);
      push('repay', { amount }, repay(amount));
    } else if (x < 0.77) {
      const amount = Math.round(r() * 300) * 1000;
      push('earn', { amount }, earn(amount));
    } else if (x < 0.93) {
      push('useSlot', {}, useSlot());
    } else {
      push('endDay', {}, endDay());
    }
    if (i % 7 === 0) push('chapterOutcome', {}, chapterOutcome());
  }
  return { seed, steps: log };
}

export function build(): unknown {
  // 장비 목록 크기는 C# 쪽 확인용
  return { source: 'src/state/actions.ts, src/systems/time.ts, chapter.ts', gear: [RODS.length, REELS.length, TACKLES.length], recipes: RECIPES.length, runs: [run(12345, 400), run(42, 400), run(2026, 600)] };
}

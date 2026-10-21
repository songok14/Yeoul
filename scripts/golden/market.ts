// 유니티 코어 테스트용 기준값: 시세와 신선도
// 웹 버전 코드를 그대로 불러 계산한 값을 unity/golden/market.json 으로 남긴다

import { SPECIES } from '../../src/data/fish';
import { BAIT, REELS, RODS, TACKLES } from '../../src/data/gear';
import { createSave, S, setState, type FishItem } from '../../src/state/save';
import { changePercent, fishValue, freshFactor, marketHeadline, priceMul, sizeFactor, stageOf, todayBasePrice, type FreshStage } from '../../src/systems/economy';
import { endDay } from '../../src/systems/time';
import { hash, mulberry32, round100, won } from '../../src/utils';

const SEEDS = [12345, 987654321, 42, 777777];
const DAYS = 45;
const STAGES: FreshStage[] = ['live', 'fresh', 'aged', 'spoiled'];

export function build(): unknown {
  const rng = SEEDS.map((seed) => {
    const r = mulberry32(seed);
    return { seed, values: Array.from({ length: 12 }, () => r()) };
  });
  const hashCases = [[0], [1, 2, 3], [12345, 1, 0], [987654321, 30, 4, 7], [42, 99], [-5, 2147483647], [999999999, 45, 2, 7]].map((args) => ({
    args,
    value: hash(...args),
  }));
  const roundInputs = [0.5, 1.5, 2.5, -0.5, -1.5, -2.5, 0.49999999999999994, 1234.5, 1234.4999, -1234.5, 99.99, 150, 250, 349.99999];
  const round = roundInputs.map((x) => ({ x, round: Math.round(x), round100: round100(x) }));
  const wonCases = [0, 100, 4500, 13000, 3_000_000, 1234567].map((n) => ({ n, text: won(n) }));

  const species = SPECIES.map(({ look: _look, ...rest }) => rest);
  const gear = { rods: RODS, reels: REELS, tackles: TACKLES, bait: BAIT };

  const stage = Array.from({ length: 101 }, (_, fresh) => ({ fresh, stage: stageOf(fresh) }));
  const factor = SPECIES.flatMap((sp) => STAGES.map((st) => ({ species: sp.id, stage: st, value: freshFactor(sp.id, st) })));
  const size = SPECIES.flatMap((sp) => [sp.minSize, sp.avgSize, sp.maxSize, 19.4, 33.3].map((cm) => ({ species: sp.id, size: cm, value: sizeFactor(sp.id, cm) })));

  const market = SEEDS.map((seed) => {
    setState(createSave(seed));
    const days = [];
    for (let day = 1; day <= DAYS; day++) {
      days.push({
        day,
        priceMul: SPECIES.map((sp) => priceMul(sp.id, day)),
        basePrice: SPECIES.map((sp) => todayBasePrice(sp.id, day)),
        change: SPECIES.map((sp) => changePercent(sp.id, day)),
        headline: marketHeadline(day),
      });
    }
    return { seed, days };
  });

  // 크기, 신선도, 날짜를 섞은 물고기 값
  const pickR = mulberry32(2026);
  const fish = [];
  for (let i = 0; i < 300; i++) {
    const seed = SEEDS[i % SEEDS.length];
    setState(createSave(seed));
    const sp = SPECIES[Math.floor(pickR() * SPECIES.length)];
    const sizeCm = Math.round((sp.minSize + (sp.maxSize - sp.minSize) * pickR()) * 10) / 10;
    const fresh = Math.floor(pickR() * 101);
    const day = 1 + Math.floor(pickR() * DAYS);
    const item: FishItem = { uid: i, species: sp.id, size: sizeCm, caughtDay: 1, fresh };
    fish.push({ seed, day, species: sp.id, size: sizeCm, fresh, value: fishValue(item, day) });
  }

  // 위판장 낙찰가: 묶음 값을 더하고 낙찰 배율을 곱해 100원 단위로
  const auction = [];
  for (let i = 0; i < 20; i++) {
    const seed = SEEDS[i % SEEDS.length];
    setState(createSave(seed));
    S.day = 1 + Math.floor(pickR() * DAYS);
    const items = fish.slice(i * 5, i * 5 + 5).map((f, k) => ({ uid: k, species: f.species, size: f.size, caughtDay: 1, fresh: f.fresh }) as FishItem);
    const mul = 0.92 + pickR() * 0.23;
    let total = 0;
    for (const f of items) total += fishValue(f);
    auction.push({ seed, day: S.day, mul, items, total: Math.round((total * mul) / 100) * 100 });
  }

  // 날이 바뀔 때 쿨러 신선도 감소 (실제 endDay를 돌린다)
  const decay = [0, 1, 2].map((tackle) => {
    setState(createSave(12345));
    S.tackle = tackle;
    const start = [100, 90, 76, 75, 60, 41, 40, 26, 11, 10, 3, 0];
    S.cooler = start.map((fresh, k) => ({ uid: k, species: 'bollak', size: 20, caughtDay: 1, fresh }));
    const steps = [];
    for (let d = 0; d < 4; d++) {
      endDay();
      steps.push({ fresh: S.cooler.map((f) => f.fresh), spoiled: S.pendingSummary?.spoiled ?? 0 });
    }
    return { tackle, start, steps };
  });

  return { source: 'src/systems/economy.ts, src/systems/time.ts (endDay)', rng, hash: hashCases, round, won: wonCases, species, gear, stage, factor, size, market, fish, auction, decay };
}

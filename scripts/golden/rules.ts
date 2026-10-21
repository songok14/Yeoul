// 유니티 코어 테스트용 기준값: 달력·규정, 미각, 어종 뽑기, 요리, 간 맞추기 판정
// Math.random 을 쓰는 함수는 mulberry32 로 바꿔 끼워 C# 과 같은 난수 순서로 비교한다

import { SPECIES } from '../../src/data/fish';
import { FLAVOR_NAME, FLAVOR_REVIEW, FLAVOR_TOOL, RECIPES } from '../../src/data/recipes';
import { REQUEST_TEMPLATES } from '../../src/data/requests';
import { createSave, S, setState } from '../../src/state/save';
import { closedToday, dateLabel, dateOf, isClosedSeason, releaseReason } from '../../src/systems/calendar';
import { stageOf, type FreshStage } from '../../src/systems/economy';
import { rollSize, rollSpecies, zoneOf, type Zone } from '../../src/systems/fishing';
import { judgeSeason } from '../../src/systems/season';
import { canTaste, checkTasteUnlock, speciesCaught, tasteLevel, TASTE_STAGES } from '../../src/systems/taste';
import { bestFishFor, cookable, dishFresh, dishPay, moodOf, reviewLines, type SeasonResult } from '../../src/systems/truck';
import { mulberry32 } from '../../src/utils';

const STAGES: FreshStage[] = ['live', 'fresh', 'aged', 'spoiled'];

export function build(): unknown {
  const realRandom = Math.random;

  // 데이터 원본
  const data = {
    recipes: RECIPES,
    flavors: Object.keys(FLAVOR_NAME).map((k) => ({ id: k, name: FLAVOR_NAME[k as keyof typeof FLAVOR_NAME], tool: FLAVOR_TOOL[k as keyof typeof FLAVOR_TOOL], review: FLAVOR_REVIEW[k as keyof typeof FLAVOR_REVIEW] })),
    requests: REQUEST_TEMPLATES,
    taste: TASTE_STAGES,
  };

  // 달력과 규정
  setState(createSave(1));
  const calendar = [];
  for (let day = 1; day <= 60; day++) {
    calendar.push({
      day,
      date: dateOf(day),
      label: dateLabel(day),
      closed: SPECIES.map((sp) => isClosedSeason(sp.id, day)),
      closedToday: closedToday(day),
    });
  }
  const release = [];
  for (const sp of SPECIES) {
    for (const size of [sp.minSize, 14.9, 15, 15.1, 24.9, 25, 25.1, 30, sp.maxSize]) {
      for (const day of [1, 15, 16, 30, 46, 47]) release.push({ species: sp.id, size, day, reason: releaseReason(sp.id, size, day) });
    }
  }

  // 거리 구간
  const zones = [0, 0.05, 0.2, 0.3499, 0.35, 0.5, 0.6999, 0.7, 0.9, 1].map((d) => ({ d, zone: zoneOf(d) }));

  // 어종과 크기 뽑기 (Math.random 을 mulberry32(seed) 로)
  const rolls = [];
  for (const seed of [7, 99, 2026]) {
    Math.random = mulberry32(seed);
    const list = [];
    for (let i = 0; i < 200; i++) {
      const zone = (i % 3) as Zone;
      const slot = Math.floor(i / 3) % 4;
      const hot = i % 5 === 0;
      const sp = rollSpecies(zone, slot, hot);
      const size = rollSize(sp, hot);
      list.push({ zone, slot, hot, species: sp.id, size });
    }
    rolls.push({ seed, list });
  }
  Math.random = realRandom;

  // 간 맞추기 판정
  const judge = [];
  const jr = mulberry32(31);
  for (let i = 0; i < 200; i++) {
    const pos = jr();
    const target = 0.12 + jr() * 0.76;
    const half = i % 2 ? 0.085 : 0.1;
    judge.push({ pos, target, half, ...judgeSeason(pos, target, half) });
  }

  // 요리: 신선도 배율, 요리값, 손님 기분, 한마디
  const dish = RECIPES.flatMap((r) => STAGES.map((st) => ({ recipe: r.id, stage: st, value: dishFresh(r, st) })));
  const pay = [];
  const pr = mulberry32(57);
  for (let i = 0; i < 200; i++) {
    const r = RECIPES[Math.floor(pr() * RECIPES.length)];
    const st = STAGES[Math.floor(pr() * 3)];
    const avg = pr();
    pay.push({ recipe: r.id, stage: st, avg, pay: dishPay(r, st, avg), mood: moodOf(avg) });
  }
  const reviews = [];
  for (const flags of [{}, { firstTaste: true }]) {
    for (const caught of [0, 3, 5]) {
      setState(createSave(1));
      S.flags = { ...flags };
      SPECIES.slice(0, caught).forEach((sp) => (S.dex[sp.id] = { count: 1, best: 20, released: 0 }));
      for (const r of RECIPES) {
        for (const st of STAGES.slice(0, 3)) {
          const results: SeasonResult[] = r.steps.map((_, k) => {
            const pos = pr();
            return judgeSeason(pos, 0.5, 0.085 + k * 0.01);
          });
          const avg = results.reduce((a, x) => a + x.quality, 0) / results.length;
          const mood = moodOf(avg);
          reviews.push({ flags, caught, recipe: r.id, stage: st, results, mood, lines: reviewLines(r, results, st, mood), tasteLevel: tasteLevel(), canTaste: ['salty', 'sweet', 'umami', 'sour', 'bitter'].map((f) => canTaste(f as never)) });
        }
      }
    }
  }

  // 재료 고르기
  const best = [];
  for (let i = 0; i < 40; i++) {
    setState(createSave(1));
    const n = 1 + Math.floor(pr() * 8);
    for (let k = 0; k < n; k++) {
      const sp = SPECIES[Math.floor(pr() * SPECIES.length)];
      S.cooler.push({ uid: k + 1, species: sp.id, size: Math.round((sp.minSize + pr() * (sp.maxSize - sp.minSize)) * 10) / 10, caughtDay: 1, fresh: Math.floor(pr() * 101) });
    }
    best.push({ cooler: S.cooler, best: RECIPES.map((r) => bestFishFor(r)?.uid ?? null), cookable: cookable().map((r) => r.id) });
  }

  // 미각 단계와 해금
  const taste = [];
  for (const first of [false, true]) {
    setState(createSave(1));
    if (first) S.flags.firstTaste = true;
    for (const sp of [null, ...SPECIES]) {
      if (sp) S.dex[sp.id] = { count: 1, best: 20, released: 0 };
      const unlocked = checkTasteUnlock();
      taste.push({ first, caught: speciesCaught(), level: tasteLevel(), unlocked: unlocked?.name ?? null, flags: { ...S.flags } });
    }
  }

  return { source: 'src/systems/calendar.ts, fishing.ts, season.ts, taste.ts, truck.ts', data, calendar, release, zones, rolls, judge, dish, pay, reviews, best, taste, stageCheck: stageOf(50) };
}

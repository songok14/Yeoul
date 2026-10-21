import Phaser from 'phaser';
import { C, CUSTOMERS_PER_SHIFT, GAME_W, T } from '../config';
import { SPECIES_BY_ID } from '../data/fish';
import { FLAVOR_NAME, FLAVOR_REVIEW, FLAVOR_TOOL, RECIPES, type Recipe } from '../data/recipes';
import { earn, removeFish } from '../state/actions';
import { persist, S, type FishItem } from '../state/save';
import { stageOf, type FreshStage } from '../systems/economy';
import { canTaste, tasteLevel, TASTE_STAGES } from '../systems/taste';
import { useSlot } from '../systems/time';
import { bands, CUSTOMER_COUNT, drawFace, drawTruck, SKY } from '../ui/art';
import { SeasonGauge } from '../ui/gauge';
import { Hud, HUD_H } from '../ui/hud';
import { bubble, Button, drawPanel, fadeIn, floatText, goTo, modal, txt } from '../ui/ui';
import { clamp, round100, won } from '../utils';

/** 요리 신선도 배율. 회는 신선도에 민감하고 숙성 어종은 숙성일 때 가장 좋다 */
function dishFresh(recipe: Recipe, stage: FreshStage): number {
  const aging = SPECIES_BY_ID[recipe.species].aging;
  if (recipe.raw) return { live: 1, fresh: 0.8, aged: aging ? 1.25 : 0.5, spoiled: 0 }[stage];
  return { live: 1, fresh: 0.95, aged: aging ? 1.1 : 0.75, spoiled: 0 }[stage];
}

function bestFishFor(recipe: Recipe): FishItem | null {
  const list = S.cooler.filter((f) => f.species === recipe.species && stageOf(f.fresh) !== 'spoiled');
  if (list.length === 0) return null;
  return list.sort((a, b) => dishFresh(recipe, stageOf(b.fresh)) - dishFresh(recipe, stageOf(a.fresh)) || b.size - a.size)[0];
}

function cookable(): Recipe[] {
  return RECIPES.filter((r) => bestFishFor(r) !== null);
}

/** 여울호 푸드트럭 장사: 손님 주문 → 간 맞추기 → 반응과 계산 */
export class TruckScene extends Phaser.Scene {
  private hud!: Hud;
  private stage!: Phaser.GameObjects.Container;
  private served = 0;
  private sales = 0;
  private gauge?: SeasonGauge;
  private customer?: Phaser.GameObjects.Image;
  private order?: Phaser.GameObjects.Container;

  constructor() {
    super('Truck');
  }

  init(): void {
    this.served = 0;
    this.sales = 0;
    this.gauge = undefined;
    this.customer = undefined;
  }

  create(): void {
    fadeIn(this);
    const sky = SKY[S.slot] ?? SKY[1];
    const g = this.add.graphics();
    bands(g, 0, HUD_H, GAME_W, 240, sky.top, sky.bot, 8);
    bands(g, 0, HUD_H + 240, GAME_W, 800 - HUD_H - 240, 0xcfc3a4, 0xa89a7a, 10);
    // 항구 광장 바닥
    g.fillStyle(0xbfb293, 1);
    for (let i = 0; i < 10; i++) g.fillRect(i * 48, 330, 40, 4);
    g.fillStyle(C.concreteDark, 1);
    g.fillRect(0, 290, GAME_W, 10);

    this.hud = new Hud(this);
    txt(this, 16, HUD_H + 10, '여울호 푸드트럭', { size: 'h1', color: sky.dark ? T.paper : T.ink });
    const lv = tasteLevel();
    txt(this, GAME_W - 16, HUD_H + 12, `미각 ${TASTE_STAGES[lv].name}`, { size: 'small', color: sky.dark ? T.gold : '#9e3330', origin: [1, 0] });

    drawTruck(this, 12, 160, 1.6);
    this.stage = this.add.container(0, 0);
    this.showMenu();
  }

  update(_t: number, delta: number): void {
    this.gauge?.tick(delta / 1000);
  }

  private showMenu(): void {
    this.stage.removeAll(true);
    const L = this.stage;
    const g = this.add.graphics();
    L.add(g);
    drawPanel(g, 20, 360, GAME_W - 40, 330, 0x2b2b2b, C.wood);
    L.add(txt(this, GAME_W / 2, 380, '오늘의 메뉴', { size: 'h1', color: T.paper, origin: [0.5, 0] }));
    RECIPES.forEach((r, i) => {
      const y = 420 + i * 50;
      const n = S.cooler.filter((f) => f.species === r.species && stageOf(f.fresh) !== 'spoiled').length;
      const known = S.dex[r.species];
      L.add(this.add.image(58, y + 16, `fish-${r.species}`).setScale(1.5).setAlpha(n > 0 ? 1 : 0.35));
      L.add(txt(this, 92, y, known ? r.name : '???', { size: 'body', color: n > 0 ? T.paper : T.gray }));
      const flavors = r.steps.map((s) => (canTaste(s.flavor) ? FLAVOR_NAME[s.flavor] : '???')).join(' · ');
      L.add(txt(this, 92, y + 26, flavors, { size: 'small', color: T.gray }));
      L.add(txt(this, GAME_W - 40, y + 4, n > 0 ? `재료 ${n}` : '재료 없음', { size: 'small', color: n > 0 ? T.green : T.gray, origin: [1, 0] }));
    });

    const can = cookable().length > 0;
    L.add(new Button(this, 120, 740, '마을로', () => goTo(this, 'Town'), { w: 180, h: 54, color: C.blueDark }));
    L.add(new Button(this, 330, 740, '장사 시작', () => this.nextCustomer(), { w: 200, h: 54, color: C.red, disabled: !can, sub: can ? '시간 1칸' : '쿨러가 비었다' }));
  }

  private nextCustomer(): void {
    this.stage.removeAll(true);
    this.customer?.destroy();
    const menu = cookable();
    if (this.served >= CUSTOMERS_PER_SHIFT || menu.length === 0) {
      this.endShift(menu.length === 0 && this.served < CUSTOMERS_PER_SHIFT);
      return;
    }
    const recipe = menu[Math.floor(Math.random() * menu.length)];
    const cust = this.add.image(GAME_W + 40, 330, `cust${Math.floor(Math.random() * CUSTOMER_COUNT)}`).setOrigin(0.5, 1).setScale(3);
    this.customer = cust;
    this.tweens.add({
      targets: cust,
      x: 340,
      duration: 600,
      ease: 'Quad.Out',
      onComplete: () => this.takeOrder(recipe),
    });
    this.stage.add(txt(this, GAME_W - 16, 350, `손님 ${this.served + 1}/${CUSTOMERS_PER_SHIFT}`, { size: 'small', color: T.ink, origin: [1, 1] }));
  }

  private takeOrder(recipe: Recipe): void {
    const known = !!S.dex[recipe.species];
    const order = (this.order = bubble(this, 340, 226, `${known ? recipe.name : SPECIES_BY_ID[recipe.species].name + ' 요리'}\n하나 주세요!`, { wrap: 180 }));
    this.stage.add(order);
    this.cook(recipe, 0, []);
  }

  private cook(recipe: Recipe, stepIndex: number, results: { quality: number; side: -1 | 0 | 1 }[]): void {
    if (stepIndex >= recipe.steps.length) {
      this.serve(recipe, results);
      return;
    }
    const step = recipe.steps[stepIndex];
    const visible = canTaste(step.flavor);
    const panelG = this.add.graphics();
    drawPanel(panelG, 16, 360, GAME_W - 32, 330, 0x1f2b3d, C.paper);
    this.stage.add(panelG);
    this.stage.add(txt(this, GAME_W / 2, 378, `${recipe.name}  ${stepIndex + 1}/${recipe.steps.length}`, { size: 'h1', color: T.gold, origin: [0.5, 0] }));
    this.stage.add(txt(this, GAME_W / 2, 412, visible ? '초록 구간에서 멈추세요' : '이 맛은 아직 모르겠다. 감으로...', { size: 'small', color: T.gray, origin: [0.5, 0] }));

    const target = clamp(step.target + Phaser.Math.FloatBetween(-0.04, 0.04), 0.12, 0.88);
    const gauge = new SeasonGauge(this, GAME_W / 2, 520, 360, {
      target,
      zoneHalf: 0.085,
      visible,
      speed: 0.8 + Math.random() * 0.25,
      label: `${FLAVOR_TOOL[step.flavor]} 넣기 (${visible ? FLAVOR_NAME[step.flavor] : '???'})`,
    });
    this.gauge = gauge;
    this.stage.add(gauge);

    const btn = new Button(this, GAME_W / 2, 630, '넣기!', () => {
      if (!gauge.isRunning) return;
      const r = gauge.stop();
      btn.setEnabled(false);
      this.time.delayedCall(450, () => {
        this.gauge = undefined;
        this.stage.remove([panelG, gauge, btn], true);
        this.cook(recipe, stepIndex + 1, [...results, r]);
      });
    }, { w: 220, h: 58, color: C.red });
    this.stage.add(btn);
  }

  private serve(recipe: Recipe, results: { quality: number; side: -1 | 0 | 1 }[]): void {
    const fish = bestFishFor(recipe);
    if (!fish) {
      this.nextCustomer();
      return;
    }
    this.order?.destroy();
    const stage = stageOf(fish.fresh);
    removeFish([fish.uid]);
    const avg = results.reduce((a, r) => a + r.quality, 0) / results.length;
    const pay = round100(recipe.price * dishFresh(recipe, stage) * (0.4 + 0.8 * avg));
    earn(pay);
    this.sales += pay;
    this.served += 1;
    S.dishesCooked += 1;
    const firstTime = !S.recipesCooked.includes(recipe.id);
    if (firstTime) S.recipesCooked.push(recipe.id);
    persist();
    this.hud.refresh();

    const mood: 0 | 1 | 2 = avg >= 0.75 ? 2 : avg >= 0.45 ? 1 : 0;
    const lines = recipe.steps.map((s, i) => {
      if (!canTaste(s.flavor)) return '???';
      const r = results[i];
      const idx = r.side === 0 || r.quality >= 0.8 ? 0 : r.side < 0 ? 1 : 2;
      return FLAVOR_REVIEW[s.flavor][idx];
    });
    if (tasteLevel() >= 3) {
      if (recipe.raw && stage === 'aged' && SPECIES_BY_ID[recipe.species].aging) lines.push('숙성이 잘 됐네요. 쫀득해요');
      else if (stage === 'live') lines.push('생선이 아주 싱싱해요');
      lines.push(mood === 2 ? '또 올게요!' : mood === 1 ? '먹을 만했어요' : '다음엔 더 잘해 줘요');
    }

    const g = this.add.graphics();
    drawPanel(g, 16, 360, GAME_W - 32, 330, C.paper, C.ink);
    this.stage.add(g);
    this.stage.add(this.add.image(GAME_W / 2, 420, `fish-${recipe.species}`).setScale(4));
    this.stage.add(txt(this, GAME_W / 2, 462, `${recipe.name} 완성${firstTime ? ' (레시피 기록!)' : ''}`, { size: 'h1', color: T.ink, origin: [0.5, 0] }));
    this.stage.add(drawFace(this, 70, 560, mood));
    this.stage.add(txt(this, 110, 510, lines.map((l) => `"${l}"`).join('\n'), { size: 'small', color: T.inkSoft, wrap: 300, lineSpacing: 8 }));
    this.stage.add(txt(this, GAME_W / 2, 650, `받은 돈 ${won(pay)}`, { size: 'h1', color: '#9e3330', origin: [0.5, 0.5] }));
    floatText(this, 340, 250, `+${won(pay)}`);

    const next = new Button(this, GAME_W / 2, 740, this.served >= CUSTOMERS_PER_SHIFT ? '장사 마치기' : '다음 손님', () => {
      if (this.customer) {
        this.tweens.add({ targets: this.customer, x: GAME_W + 60, duration: 400, onComplete: () => this.nextCustomer() });
      } else this.nextCustomer();
    }, { w: 240, h: 54, color: C.green });
    this.stage.add(next);
  }

  private endShift(ranOut: boolean): void {
    this.customer?.destroy();
    useSlot();
    this.hud.refresh();
    modal(this, {
      title: ranOut ? '재료가 다 떨어졌다' : '오늘 장사 끝!',
      body: `손님 ${this.served}명\n매출 ${won(this.sales)}`,
      buttons: [{ label: '마을로', color: C.blue, onClick: () => goTo(this, 'Town') }],
    });
  }
}

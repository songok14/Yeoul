import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { SPECIES, SPECIES_BY_ID, type SpeciesId } from '../data/fish';
import { REQUEST_TEMPLATES } from '../data/requests';
import { deliverRequest, fishForRequest, sellFish } from '../state/actions';
import { S, type FishItem } from '../state/save';
import { changePercent, coolerCapacity, fishValue, STAGE_INK, STAGE_NAME, stageOf, todayBasePrice, type FreshStage } from '../systems/economy';
import { isMarketOpen } from '../systems/time';
import { Hud, HUD_H } from '../ui/hud';
import { backdrop, Button, drawPanel, fadeIn, floatText, goTo, modal, toast, txt } from '../ui/ui';
import { won } from '../utils';

interface Group {
  species: SpeciesId;
  stage: FreshStage;
  items: FishItem[];
  value: number;
}

const PAGE_SIZE = 5;

/** 물비늘포 위판장: 경매 판매와 납품 의뢰 */
export class MarketScene extends Phaser.Scene {
  private hud!: Hud;
  private tab: 'auction' | 'request' = 'auction';
  private page = 0;
  private layer!: Phaser.GameObjects.Container;

  constructor() {
    super('Market');
  }

  init(data: { tab?: string }): void {
    this.tab = data.tab === 'request' ? 'request' : 'auction';
    this.page = 0;
  }

  create(): void {
    fadeIn(this);
    backdrop(this, 'bg-market');
    this.add.rectangle(0, 0, GAME_W, 800, 0x12202e, 0.35).setOrigin(0, 0);
    this.hud = new Hud(this);

    txt(this, 16, HUD_H + 12, '물비늘포 위판장', { size: 'h1', color: T.paper, stroke: '#1b2a41' });
    txt(this, GAME_W - 16, HUD_H + 14, isMarketOpen() ? '경매 진행 중' : '경매 마감', { size: 'small', color: isMarketOpen() ? T.green : T.red, origin: [1, 0] });

    new Button(this, 118, HUD_H + 66, '경매', () => this.switchTab('auction'), { w: 200, h: 44, color: this.tab === 'auction' ? C.gold : C.grayDark, textColor: this.tab === 'auction' ? T.ink : T.white });
    new Button(this, 332, HUD_H + 66, `납품 의뢰 ${S.requests.length}`, () => this.switchTab('request'), { w: 200, h: 44, color: this.tab === 'request' ? C.gold : C.grayDark, textColor: this.tab === 'request' ? T.ink : T.white });

    this.layer = this.add.container(0, 0);
    new Button(this, GAME_W / 2, 762, '마을로', () => goTo(this, 'Town'), { w: 200, h: 50, color: C.blueDark });
    this.render();
  }

  private switchTab(tab: 'auction' | 'request'): void {
    if (this.tab === tab) return;
    this.scene.restart({ tab });
  }

  private render(): void {
    this.layer.removeAll(true);
    if (this.tab === 'auction') this.renderAuction();
    else this.renderRequests();
    this.hud.refresh();
  }

  private renderAuction(): void {
    const L = this.layer;
    const top = HUD_H + 100;
    const g = this.add.graphics();
    L.add(g);

    // 오늘 시세판
    drawPanel(g, 14, top, GAME_W - 28, 196, 0x1b2a41, C.paper);
    L.add(txt(this, 30, top + 12, '오늘 시세 (평균 크기 활어 한 마리)', { size: 'small', color: T.gold }));
    SPECIES.forEach((sp, i) => {
      const y = top + 46 + i * 29;
      L.add(this.add.image(46, y + 9, `fish-${sp.id}`).setScale(1.4));
      L.add(txt(this, 80, y, sp.name, { size: 'small', color: S.dex[sp.id] ? T.paper : T.gray }));
      L.add(txt(this, 300, y, won(todayBasePrice(sp.id)), { size: 'small', color: T.paper, origin: [1, 0] }));
      const c = changePercent(sp.id);
      const arrow = c > 0 ? `▲${c}%` : c < 0 ? `▼${-c}%` : '-';
      L.add(txt(this, 420, y, arrow, { size: 'small', color: c > 0 ? T.red : c < 0 ? T.blue : T.gray, origin: [1, 0] }));
    });

    // 내 쿨러
    const groups = this.groups();
    const ctop = top + 208;
    drawPanel(g, 14, ctop, GAME_W - 28, 318, C.paper, C.ink);
    L.add(txt(this, 30, ctop + 12, `내 쿨러 ${S.cooler.length}/${coolerCapacity()}`, { size: 'h1', color: T.ink }));
    if (groups.length === 0) {
      L.add(txt(this, GAME_W / 2, ctop + 150, '쿨러가 비었다.\n방파제에 나가 보자.', { size: 'small', color: T.inkSoft, origin: [0.5, 0.5], align: 'center' }));
      return;
    }
    const pages = Math.ceil(groups.length / PAGE_SIZE);
    this.page = Math.min(this.page, pages - 1);
    const open = isMarketOpen();
    groups.slice(this.page * PAGE_SIZE, this.page * PAGE_SIZE + PAGE_SIZE).forEach((grp, i) => {
      const y = ctop + 50 + i * 48;
      g.fillStyle(i % 2 === 0 ? C.paperDark : C.paper, 0.6);
      g.fillRect(20, y - 4, GAME_W - 40, 44);
      L.add(this.add.image(48, y + 18, `fish-${grp.species}`).setScale(1.6));
      const name = txt(this, 82, y + 8, `${SPECIES_BY_ID[grp.species].name} ×${grp.items.length}`, { size: 'small', color: T.ink });
      L.add(name);
      L.add(txt(this, 82 + name.width + 8, y + 8, STAGE_NAME[grp.stage], { size: 'small', color: STAGE_INK[grp.stage] }));
      L.add(txt(this, 300, y + 10, won(grp.value), { size: 'small', color: T.ink, origin: [1, 0] }));
      const btn = new Button(this, 372, y + 18, '팔기', () => this.auction(grp.items), {
        w: 80, h: 38, size: 'small', color: C.red, disabled: !open || grp.stage === 'spoiled',
      });
      L.add(btn);
    });
    if (pages > 1) {
      L.add(new Button(this, 60, ctop + 296, '◀', () => { this.page = (this.page + pages - 1) % pages; this.render(); }, { w: 56, h: 32, size: 'small', color: C.grayDark }));
      L.add(txt(this, GAME_W / 2, ctop + 296, `${this.page + 1}/${pages}`, { size: 'small', color: T.ink, origin: [0.5, 0.5] }));
      L.add(new Button(this, GAME_W - 60, ctop + 296, '▶', () => { this.page = (this.page + 1) % pages; this.render(); }, { w: 56, h: 32, size: 'small', color: C.grayDark }));
    } else if (open) {
      const sellable = S.cooler.filter((f) => stageOf(f.fresh) !== 'spoiled');
      if (sellable.length > 0) {
        L.add(new Button(this, GAME_W / 2, ctop + 296, `전부 팔기 (${won(sellable.reduce((a, f) => a + fishValue(f), 0))})`, () => this.auction(sellable), { w: 300, h: 36, size: 'small', color: C.red }));
      }
    }
    if (!open) L.add(txt(this, GAME_W / 2, ctop + 268, '경매는 새벽과 아침에만 열린다', { size: 'small', color: T.red, origin: [0.5, 0.5] }));
  }

  private groups(): Group[] {
    const map = new Map<string, Group>();
    for (const f of S.cooler) {
      const stage = stageOf(f.fresh);
      const key = `${f.species}-${stage}`;
      const grp = map.get(key) ?? { species: f.species, stage, items: [], value: 0 };
      grp.items.push(f);
      grp.value += fishValue(f);
      map.set(key, grp);
    }
    const order: FreshStage[] = ['live', 'fresh', 'aged', 'spoiled'];
    return [...map.values()].sort((a, b) => SPECIES.findIndex((s) => s.id === a.species) - SPECIES.findIndex((s) => s.id === b.species) || order.indexOf(a.stage) - order.indexOf(b.stage));
  }

  /** 중매인들이 값을 부르는 짧은 경매 연출 */
  private auction(items: FishItem[]): void {
    const base = items.reduce((a, f) => a + fishValue(f), 0);
    if (base <= 0) return;
    const finalMul = Phaser.Math.FloatBetween(0.92, 1.15);
    const c = this.add.container(0, 0).setDepth(1000);
    const dim = this.add.rectangle(0, 0, GAME_W, 800, C.black, 0.6).setOrigin(0, 0).setInteractive();
    const g = this.add.graphics();
    drawPanel(g, 50, 300, 350, 180, C.paper, C.ink);
    const title = txt(this, GAME_W / 2, 326, '중매인들이 손가락으로 값을 부른다', { size: 'small', color: T.inkSoft, origin: [0.5, 0] });
    const price = txt(this, GAME_W / 2, 400, won(base * 0.8), { size: 'title', color: T.ink, origin: [0.5, 0.5] });
    c.add([dim, g, title, price]);
    const tw = { v: 0.8 };
    this.tweens.add({
      targets: tw,
      v: finalMul,
      duration: 1100,
      ease: 'Stepped',
      easeParams: [7],
      onUpdate: () => price.setText(won(Math.round((base * tw.v) / 100) * 100)),
      onComplete: () => {
        const total = sellFish(items.map((f) => f.uid), finalMul);
        price.setText(won(total));
        title.setText('낙찰!');
        title.setColor(T.red);
        this.time.delayedCall(650, () => {
          c.destroy();
          floatText(this, GAME_W / 2, 200, `+${won(total)}`);
          this.render();
        });
      },
    });
  }

  private renderRequests(): void {
    const L = this.layer;
    const top = HUD_H + 100;
    if (S.requests.length === 0) {
      L.add(txt(this, GAME_W / 2, top + 160, '지금은 들어온 의뢰가 없다.\n내일 다시 와 보자.', { size: 'small', color: T.paper, origin: [0.5, 0.5], align: 'center' }));
      return;
    }
    S.requests.forEach((req, i) => {
      const t = REQUEST_TEMPLATES[req.template];
      const y = top + i * 270;
      const g = this.add.graphics();
      L.add(g);
      drawPanel(g, 14, y, GAME_W - 28, 256, C.paper, C.ink);
      L.add(txt(this, 32, y + 16, t.client, { size: 'h1', color: T.ink }));
      const left = req.deadline - S.day;
      L.add(txt(this, GAME_W - 32, y + 18, left <= 0 ? '오늘까지' : `D-${left}`, { size: 'small', color: left <= 1 ? T.red : T.inkSoft, origin: [1, 0] }));
      L.add(txt(this, 32, y + 54, `"${t.text}"`, { size: 'small', color: T.inkSoft, wrap: GAME_W - 70 }));
      L.add(this.add.image(60, y + 140, `fish-${t.species}`).setScale(2.4));
      const have = S.cooler.filter((f) => f.species === t.species && ['live', 'fresh'].includes(stageOf(f.fresh))).length;
      L.add(txt(this, 110, y + 120, `${SPECIES_BY_ID[t.species].name} ${t.count}마리 (선어 이상)`, { size: 'small', color: T.ink }));
      L.add(txt(this, 110, y + 146, `가진 것 ${have}마리`, { size: 'small', color: have >= t.count ? '#3f8a4a' : T.red }));
      L.add(txt(this, 32, y + 186, `보상 ${won(req.reward)}`, { size: 'h1', color: '#9e3330' }));
      const ok = fishForRequest(req.id) !== null;
      L.add(new Button(this, GAME_W - 90, y + 202, '납품하기', () => {
        modal(this, {
          title: `${t.client}에게 납품할까요?`,
          body: `가장 큰 ${SPECIES_BY_ID[t.species].name} ${t.count}마리를 보냅니다.`,
          buttons: [
            { label: '취소', color: C.grayDark },
            {
              label: '납품',
              color: C.green,
              onClick: () => {
                const reward = deliverRequest(req.id);
                floatText(this, GAME_W / 2, 200, `+${won(reward)}`);
                toast(this, `${t.client}: 고맙소!`, T.paper, 260);
                this.render();
              },
            },
          ],
        });
      }, { w: 130, h: 46, size: 'small', color: C.green, disabled: !ok }));
    });
  }
}

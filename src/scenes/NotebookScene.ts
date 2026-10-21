import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { SPECIES, SPECIES_BY_ID } from '../data/fish';
import { RECIPE_BY_SPECIES } from '../data/recipes';
import { S } from '../state/save';
import { coolerCapacity, fishValue, STAGE_COLOR, STAGE_INK, STAGE_NAME, stageOf } from '../systems/economy';
import { speciesCaught, tasteLevel, TASTE_STAGES } from '../systems/taste';
import { Hud, HUD_H } from '../ui/hud';
import { Button, drawPanel, fadeIn, goTo, modal, txt } from '../ui/ui';
import { won } from '../utils';

type Tab = 'cooler' | 'dex' | 'taste' | 'journal';
const TABS: [Tab, string][] = [
  ['cooler', '쿨러'],
  ['dex', '도감'],
  ['taste', '미각'],
  ['journal', '일지'],
];

const JOURNAL_1 = '물비늘포 볼락은 해 질 녘 테트라포드 그늘에 있다.\n여울이는 소금을 아끼면 안 된다. 짜게 먹는 애다.';

/** 수첩: 쿨러, 어종 도감, 미각 회복, 조황일지 */
export class NotebookScene extends Phaser.Scene {
  private tab: Tab = 'dex';

  constructor() {
    super('Notebook');
  }

  init(data: { tab?: string }): void {
    this.tab = (TABS.find(([t]) => t === data.tab)?.[0] ?? 'cooler') as Tab;
  }

  create(): void {
    fadeIn(this);
    this.cameras.main.setBackgroundColor('#2b2420');
    new Hud(this);
    const g = this.add.graphics();
    drawPanel(g, 10, HUD_H + 64, GAME_W - 20, 600, C.paper, C.ink);
    // 수첩 줄
    g.fillStyle(C.paperDark, 0.5);
    for (let y = HUD_H + 100; y < HUD_H + 650; y += 32) g.fillRect(20, y, GAME_W - 40, 1);

    TABS.forEach(([tab, label], i) => {
      const active = tab === this.tab;
      new Button(this, 62 + i * 109, HUD_H + 32, label, () => {
        if (!active) this.scene.restart({ tab });
      }, { w: 100, h: 44, color: active ? C.gold : C.grayDark, textColor: active ? T.ink : T.white });
    });

    const top = HUD_H + 84;
    if (this.tab === 'cooler') this.cooler(top);
    else if (this.tab === 'dex') this.dex(top);
    else if (this.tab === 'taste') this.taste(top);
    else this.journal(top);

    new Button(this, GAME_W / 2, 762, '마을로', () => goTo(this, 'Town'), { w: 200, h: 50, color: C.blueDark });
  }

  private cooler(top: number): void {
    txt(this, 30, top, `쿨러 ${S.cooler.length}/${coolerCapacity()}`, { size: 'h1', color: T.ink });
    txt(this, GAME_W - 30, top + 4, '하루마다 신선도가 떨어진다', { size: 'small', color: T.inkSoft, origin: [1, 0] });
    if (S.cooler.length === 0) {
      txt(this, GAME_W / 2, top + 240, '비어 있다', { size: 'body', color: T.inkSoft, origin: [0.5, 0.5] });
      return;
    }
    const list = [...S.cooler].sort((a, b) => b.fresh - a.fresh).slice(0, 13);
    list.forEach((f, i) => {
      const y = top + 40 + i * 40;
      const stage = stageOf(f.fresh);
      this.add.image(44, y + 12, `fish-${f.species}`).setScale(1.4);
      txt(this, 76, y, `${SPECIES_BY_ID[f.species].name} ${f.size.toFixed(1)}cm`, { size: 'small', color: T.ink });
      const g = this.add.graphics();
      g.fillStyle(C.ink, 1);
      g.fillRect(222, y + 6, 64, 12);
      g.fillStyle(Phaser.Display.Color.HexStringToColor(STAGE_COLOR[stage]).color, 1);
      g.fillRect(224, y + 8, Math.max(0, (f.fresh / 100) * 60), 8);
      txt(this, 294, y, STAGE_NAME[stage], { size: 'small', color: STAGE_INK[stage] });
      txt(this, GAME_W - 28, y, won(fishValue(f)), { size: 'small', color: T.ink, origin: [1, 0] });
    });
    if (S.cooler.length > list.length) txt(this, GAME_W / 2, top + 560, `외 ${S.cooler.length - list.length}마리`, { size: 'small', color: T.inkSoft, origin: [0.5, 0] });
  }

  private dex(top: number): void {
    txt(this, 30, top, `어종 도감 ${speciesCaught()}/${SPECIES.length}`, { size: 'h1', color: T.ink });
    txt(this, GAME_W - 30, top + 4, '눌러서 자세히', { size: 'small', color: T.inkSoft, origin: [1, 0] });
    SPECIES.forEach((sp, i) => {
      const y = top + 44 + i * 100;
      const e = S.dex[sp.id];
      const img = this.add.image(62, y + 30, `fish-${sp.id}`).setScale(sp.look.len > 20 ? 2.4 : 3);
      if (!e) {
        img.setTintFill(0x5b5148);
        txt(this, 120, y + 4, '???', { size: 'h1', color: T.inkSoft });
        txt(this, 120, y + 32, sp.rare ? '먼 물 새벽에 무언가 있다는데...' : '아직 낚지 못했다', { size: 'small', color: T.inkSoft });
        return;
      }
      txt(this, 120, y, `${sp.name}${sp.rare ? ' ★' : ''}`, { size: 'h1', color: T.ink });
      txt(this, GAME_W - 30, y + 2, `${e.count}마리`, { size: 'small', color: T.inkSoft, origin: [1, 0] });
      txt(this, 120, y + 30, sp.hint, { size: 'small', color: T.inkSoft });
      const recipe = RECIPE_BY_SPECIES[sp.id];
      const cooked = S.recipesCooked.includes(recipe.id);
      txt(this, 120, y + 54, `최대 ${e.best.toFixed(1)}cm${cooked ? ` · 레시피 ${recipe.name}` : ''}`, { size: 'small', color: cooked ? '#9e3330' : T.inkSoft });
      const z = this.add.zone(GAME_W / 2, y + 36, GAME_W - 40, 92).setInteractive({ useHandCursor: true });
      z.on('pointerup', () => {
        modal(this, {
          title: sp.name,
          body: `${sp.desc}\n\n${sp.minSize}~${sp.maxSize}cm · 힘 ${'●'.repeat(sp.strength)}${sp.aging ? '\n숙성하면 값이 오른다' : ''}`,
          extra: (c, t) => {
            c.add(this.add.image(0, t + 30, `fish-${sp.id}`).setScale(5));
            return 70;
          },
          buttons: [{ label: '닫기', color: C.grayDark }],
        });
      });
    });
  }

  private taste(top: number): void {
    const lv = tasteLevel();
    txt(this, 30, top, '미각 회복', { size: 'h1', color: T.ink });
    txt(this, GAME_W - 30, top + 4, `낚은 어종 ${speciesCaught()}종`, { size: 'small', color: T.inkSoft, origin: [1, 0] });
    const g = this.add.graphics();
    TASTE_STAGES.forEach((st, i) => {
      const y = top + 54 + i * 92;
      const done = i <= lv;
      // 단계 사이 선
      if (i < TASTE_STAGES.length - 1) {
        g.fillStyle(i < lv ? C.gold : C.paperDark, 1);
        g.fillRect(54, y + 20, 6, 92);
      }
      g.fillStyle(C.ink, 1);
      g.fillCircle(57, y + 16, 20);
      g.fillStyle(done ? C.gold : i === lv + 1 ? C.paperDark : C.grayDark, 1);
      g.fillCircle(57, y + 16, 16);
      txt(this, 57, y + 16, `${i}`, { size: 'h1', color: T.ink, origin: [0.5, 0.5] });
      const known = done || i === lv + 1;
      txt(this, 96, y, known ? st.name : '???', { size: 'h1', color: done ? T.ink : T.inkSoft });
      const cond = st.needSpecies && !done ? `${st.cond} (${Math.min(speciesCaught(), st.needSpecies)}/${st.needSpecies})` : st.cond;
      txt(this, GAME_W - 30, y + 2, cond, { size: 'small', color: done ? '#3f8a4a' : st.needSpecies ? '#9e3330' : T.inkSoft, origin: [1, 0] });
      txt(this, 96, y + 32, known ? st.effect : '???', { size: 'small', color: T.inkSoft });
    });
  }

  private journal(top: number): void {
    txt(this, 30, top, `조황일지 ${S.journalPages}/12`, { size: 'h1', color: T.ink });
    const g = this.add.graphics();
    for (let i = 0; i < 12; i++) {
      const x = 30 + (i % 4) * 100;
      const y = top + 44 + Math.floor(i / 4) * 92;
      const have = i < S.journalPages;
      g.fillStyle(C.ink, 1);
      g.fillRect(x, y, 86, 78);
      g.fillStyle(have ? 0xf8f1de : C.paperDark, 1);
      g.fillRect(x + 3, y + 3, 80, 72);
      txt(this, x + 43, y + 39, have ? `${i + 1}장` : '?', { size: 'h1', color: have ? T.ink : T.inkSoft, origin: [0.5, 0.5] });
    }
    const text = S.journalPages > 0 ? `[1장 · 물비늘포]\n${JOURNAL_1}` : '페이지 절반이 뜯겨 나갔다.\n남은 페이지는 어디에 있을까.';
    txt(this, 30, top + 340, text, { size: 'small', color: T.inkSoft, wrap: GAME_W - 60, lineSpacing: 6 });
  }
}

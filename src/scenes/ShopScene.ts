import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { BAIT } from '../data/gear';
import { buyBait, gearList, upgradeGear, type GearSlot } from '../state/actions';
import { S } from '../state/save';
import { Hud, HUD_H } from '../ui/hud';
import { backdrop, bubble, Button, drawPanel, fadeIn, floatText, goTo, sprite, toast, txt } from '../ui/ui';
import { pick, won } from '../utils';

const NOSSI_LINES = [
  '...',
  '볼락은 해 질 녘이다.',
  '먼 물엔 감성돔이 있다. 대가 약하면 못 버틴다.',
  '고등어가 우는 쪽을 봐라.',
  '줄이 울면 손을 놔라.',
  '...느그 아부지도 크릴만 썼다.',
];

const SLOT_LABEL: Record<GearSlot, string> = { rod: '낚싯대', reel: '릴', tackle: '소품' };

/** 노씨 미끼가게: 미끼와 장비 */
export class ShopScene extends Phaser.Scene {
  private hud!: Hud;
  private layer!: Phaser.GameObjects.Container;
  private talk?: Phaser.GameObjects.Container;

  constructor() {
    super('Shop');
  }

  create(): void {
    fadeIn(this);
    backdrop(this, 'bg-baitshop');
    this.hud = new Hud(this);
    const nossi = sprite(this, 'ch-nossi', 62, HUD_H + 182, 172);
    nossi.setInteractive({ useHandCursor: true }).on('pointerup', () => this.say(pick(NOSSI_LINES)));
    txt(this, 120, HUD_H + 140, '노씨 미끼가게', { size: 'h1', color: T.paper, stroke: '#1b2a41' });
    this.say(S.flags.nossiFavor ? '...노래미 고맙다.' : pick(NOSSI_LINES));

    this.layer = this.add.container(0, 0);
    new Button(this, GAME_W / 2, 762, '마을로', () => goTo(this, 'Town'), { w: 200, h: 50, color: C.blueDark });
    this.render();
  }

  private say(line: string): void {
    this.talk?.destroy();
    this.talk = bubble(this, 270, HUD_H + 96, line, { wrap: 260, tail: 'left' });
  }

  private render(): void {
    this.layer.removeAll(true);
    const L = this.layer;
    const top = HUD_H + 184;
    const g = this.add.graphics();
    L.add(g);
    drawPanel(g, 14, top, GAME_W - 28, 490, C.paper, C.ink);

    // 미끼
    let y = top + 16;
    L.add(txt(this, 30, y, BAIT.name, { size: 'h1', color: T.ink }));
    L.add(txt(this, 30, y + 30, `${BAIT.desc} · 가진 것 ${S.bait}`, { size: 'small', color: T.inkSoft }));
    L.add(new Button(this, 370, y + 26, won(BAIT.price), () => {
      if (buyBait()) {
        floatText(this, 362, y, `미끼 +${BAIT.amount}`, T.green);
        this.refresh();
      } else toast(this, '돈이 모자라다', T.red);
    }, { w: 104, h: 44, size: 'small', color: C.green, disabled: S.money < BAIT.price }));

    // 장비
    (['rod', 'reel', 'tackle'] as GearSlot[]).forEach((slot) => {
      y += 90;
      g.fillStyle(C.paperDark, 1);
      g.fillRect(24, y - 12, GAME_W - 48, 2);
      const list = gearList(slot);
      const cur = list[S[slot]];
      const next = list[S[slot] + 1];
      L.add(txt(this, 30, y, `${SLOT_LABEL[slot]} · ${cur.name}`, { size: 'small', color: T.inkSoft }));
      if (next) {
        L.add(txt(this, 30, y + 22, `→ ${next.name}`, { size: 'h1', color: T.ink }));
        L.add(txt(this, 30, y + 50, next.desc, { size: 'small', color: T.inkSoft }));
        L.add(new Button(this, 370, y + 34, won(next.price), () => {
          if (upgradeGear(slot)) {
            toast(this, `${next.name} 장만!`, T.gold, 300);
            this.say(slot === 'rod' && S.rod === 2 ? '...잘 써라. 아끼던 거다.' : '...괜찮은 물건이다.');
            this.refresh();
          } else toast(this, '돈이 모자라다', T.red);
        }, { w: 104, h: 44, size: 'small', color: C.blue, disabled: S.money < next.price }));
      } else {
        L.add(txt(this, 30, y + 22, '가장 좋은 장비', { size: 'h1', color: '#3f8a4a' }));
      }
    });

    // 채비와 숨은 퀘스트
    y += 90;
    g.fillStyle(C.paperDark, 1);
    g.fillRect(24, y - 12, GAME_W - 48, 2);
    L.add(txt(this, 30, y, '채비 · 기본 찌채비', { size: 'small', color: T.inkSoft }));
    L.add(txt(this, 30, y + 24, '원투·에깅은 다음 지역에서', { size: 'small', color: T.gray }));
    L.add(txt(this, 30, y + 54, '아버지의 대나무대 (부러짐)', { size: 'small', color: '#8a5a3b' }));
    L.add(new Button(this, 370, y + 40, '보여 주기', () => this.say('...그 대나무대. 버리지 마라.'), { w: 104, h: 40, size: 'small', color: C.wood }));
  }

  private refresh(): void {
    this.hud.refresh();
    this.render();
  }
}

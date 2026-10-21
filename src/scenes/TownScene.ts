import Phaser from 'phaser';
import { C, CH1_DEADLINE, GAME_W, T } from '../config';
import { feedCat, repay, spoiledFish } from '../state/actions';
import { deleteSave, persist, S } from '../state/save';
import { marketHeadline } from '../systems/economy';
import { daysLeft, endDay, isMarketOpen, isTruckOpen, slotName } from '../systems/time';
import { addSparkles, bands, drawLighthouse, drawSeascape, drawTruck } from '../ui/art';
import { Hud, HUD_H } from '../ui/hud';
import { bubble, Button, drawPanel, fadeIn, floatText, goTo, modal, toast, txt } from '../ui/ui';
import { pick, won } from '../utils';

const GRANDMA_LINES = [
  '밥은 먹고 다니나. 맛 몰라도 배는 고프다.',
  '빚 걱정은 내가 할게. 니는 바다 걱정이나 해라.',
  '느그 아부지도 저 방파제에서 살았다.',
  '고등어 그놈, 니 트럭을 지 집인 줄 안다.',
  '생선은 손으로 만져 보면 안다. 맛 몰라도 된다.',
];

/** 물비늘포 마을 허브 */
export class TownScene extends Phaser.Scene {
  private hud!: Hud;

  constructor() {
    super('Town');
  }

  create(): void {
    fadeIn(this);

    // 1장 결말 확인
    if (S.debt <= 0 && !S.flags.ch1Clear) {
      S.flags.ch1Clear = true;
      S.journalPages = Math.max(1, S.journalPages);
      persist();
      this.scene.start('Prologue', { mode: 'clear' });
      return;
    }
    if (S.day > CH1_DEADLINE && S.debt > 0 && !S.flags.ch1Clear) {
      deleteSave();
      this.scene.start('Prologue', { mode: 'fail' });
      return;
    }

    this.drawTown();
    this.hud = new Hud(this);

    // 오늘 소식 띠
    const news = this.add.graphics();
    news.fillStyle(C.ink, 0.6);
    news.fillRect(0, HUD_H, GAME_W, 28);
    txt(this, 12, HUD_H + 4, `소식 · ${marketHeadline()}`, { size: 'small', color: T.paper });

    this.makeMenu();

    if (S.pendingSummary) {
      const sum = S.pendingSummary;
      S.pendingSummary = null;
      persist();
      const lines = [`어제 번 돈 ${won(sum.earned)}`];
      if (sum.spoiled > 0) lines.push(`쿨러에서 ${sum.spoiled}마리가 상했다`);
      if (sum.expiredRequests > 0) lines.push(`의뢰 ${sum.expiredRequests}건이 기한을 넘겼다`);
      lines.push(`위판장 시세가 바뀌었다`);
      lines.push(S.debt > 0 ? `상환까지 ${daysLeft() > 0 ? `${daysLeft()}일` : '오늘이 마지막 날'}` : '1차 상환은 끝났다');
      modal(this, {
        title: `${S.day}일차 ${slotName()}`,
        body: lines.join('\n'),
        buttons: [{ label: '좋아', color: C.green }],
      });
    }
  }

  private drawTown(): void {
    drawSeascape(this, S.slot, HUD_H, 250, 400);
    addSparkles(this, 0, 260, GAME_W, 130, 14);
    const g = this.add.graphics();

    // 방파제와 등대
    g.fillStyle(C.concreteDark, 1);
    g.fillRect(250, 334, 200, 18);
    g.fillStyle(C.concrete, 1);
    g.fillRect(250, 328, 200, 8);
    drawLighthouse(g, 420, 330, 0.9);

    // 땅과 길
    bands(g, 0, 400, GAME_W, 250, 0xdcc89a, 0xc2ab7a, 10);
    g.fillStyle(0x9a9286, 1);
    g.fillRect(0, 560, GAME_W, 56);
    g.fillStyle(0xe8e2d4, 1);
    for (let i = 0; i < 9; i++) g.fillRect(10 + i * 52, 586, 28, 4);

    // 위판장
    this.building(g, 12, 410, 146, 120, 0xdfe8ee, 0x2f5689);
    g.fillStyle(0x5e6a74, 1);
    g.fillRect(28, 460, 114, 70);
    g.fillStyle(0x3d6e8c, 1);
    for (let i = 0; i < 4; i++) g.fillRect(34 + i * 27, 506, 22, 18);
    // 미끼가게
    this.building(g, 166, 440, 88, 90, 0xb88a5a, 0x5e3b26);
    for (let i = 0; i < 8; i++) {
      g.fillStyle(i % 2 === 0 ? C.green : C.white, 1);
      g.fillRect(166 + i * 11, 470, 11, 12);
    }
    g.fillStyle(0x3b2a20, 1);
    g.fillRect(196, 494, 28, 36);
    // 여울식당
    this.building(g, 262, 420, 176, 110, 0xf1e6cc, 0x3c4148);
    g.fillStyle(0x6a4a30, 1);
    g.fillRect(330, 478, 40, 52);
    g.fillStyle(C.paper, 1);
    g.fillRect(338, 490, 24, 16);
    g.fillStyle(0x8fc1d4, 1);
    g.fillRect(282, 470, 34, 26);
    g.fillRect(386, 470, 34, 26);

    this.sign(85, 430, '위판장', () => goTo(this, 'Market'));
    this.sign(210, 456, '미끼', () => goTo(this, 'Shop'));
    this.sign(350, 440, '여울식당', () => this.restaurant());

    const truck = drawTruck(this, 150, 540, 1);
    truck.setSize(150, 70);
    const tz = this.add.zone(225, 570, 150, 70).setInteractive({ useHandCursor: true });
    tz.on('pointerup', () => this.openTruck());

    const yeoul = this.add.image(110, 610, 'yeoul').setOrigin(0.5, 1).setScale(2.5);
    this.tweens.add({ targets: yeoul, y: 608, duration: 700, yoyo: true, repeat: -1 });

    const cat = this.add.image(330, 618, 'cat').setOrigin(0.5, 1).setScale(2.5).setInteractive({ useHandCursor: true });
    cat.on('pointerup', () => this.tapCat(cat));

    // 방파제 가는 길 표지
    this.sign(380, 380, '방파제 →', () => this.goFishing());
  }

  private building(g: Phaser.GameObjects.Graphics, x: number, y: number, w: number, h: number, wall: number, roof: number): void {
    g.fillStyle(C.black, 0.2);
    g.fillRect(x + 6, y + h - 4, w, 8);
    g.fillStyle(C.ink, 1);
    g.fillRect(x - 2, y - 2, w + 4, h + 4);
    g.fillStyle(wall, 1);
    g.fillRect(x, y, w, h);
    g.fillStyle(C.ink, 1);
    g.fillRect(x - 10, y - 22, w + 20, 26);
    g.fillStyle(roof, 1);
    g.fillRect(x - 8, y - 20, w + 16, 22);
    g.fillStyle(C.white, 0.15);
    g.fillRect(x - 8, y - 20, w + 16, 4);
  }

  private sign(x: number, y: number, label: string, onTap: () => void): void {
    const t = txt(this, x, y, label, { size: 'small', color: T.ink, origin: [0.5, 0.5] });
    const w = t.width + 16;
    const g = this.add.graphics();
    g.fillStyle(C.ink, 1);
    g.fillRect(x - w / 2 - 2, y - 14, w + 4, 28);
    g.fillStyle(C.paper, 1);
    g.fillRect(x - w / 2, y - 12, w, 24);
    t.setDepth(1);
    g.setDepth(0.5);
    const z = this.add.zone(x, y, w + 10, 36).setInteractive({ useHandCursor: true });
    z.on('pointerup', onTap);
  }

  private makeMenu(): void {
    const g = this.add.graphics();
    drawPanel(g, 0, 640, GAME_W, 160, 0x1f2b3d, C.ink, false);
    const col = [78, 225, 372];
    const row = [686, 758];
    const w = 138;
    const h = 62;
    new Button(this, col[0], row[0], '방파제', () => this.goFishing(), { w, h, color: C.blue, sub: '낚시 · 1칸' });
    new Button(this, col[1], row[0], '위판장', () => goTo(this, 'Market'), { w, h, color: 0x3f7a8a, sub: isMarketOpen() ? '경매 중' : '경매 마감' });
    new Button(this, col[2], row[0], '여울호', () => this.openTruck(), {
      w, h, color: C.red, sub: isTruckOpen() ? '장사 · 1칸' : '새벽엔 쉼', disabled: !isTruckOpen(),
    });
    new Button(this, col[0], row[1], '미끼가게', () => goTo(this, 'Shop'), { w, h, color: C.greenDark, sub: '장비·미끼' });
    new Button(this, col[1], row[1], '여울식당', () => this.restaurant(), { w, h, color: C.wood, sub: '상환·휴식' });
    new Button(this, col[2], row[1], '수첩', () => goTo(this, 'Notebook'), { w, h, color: 0x6a5a8a, sub: '도감·미각' });
  }

  private goFishing(): void {
    if (S.bait <= 0) {
      modal(this, {
        title: '미끼가 없다',
        body: '노씨 가게에서 크릴을 사 오자.',
        buttons: [
          { label: '닫기', color: C.grayDark },
          { label: '미끼가게', color: C.greenDark, onClick: () => goTo(this, 'Shop') },
        ],
      });
      return;
    }
    goTo(this, 'Fishing');
  }

  private openTruck(): void {
    if (!isTruckOpen()) {
      toast(this, '새벽엔 손님이 없다');
      return;
    }
    goTo(this, 'Truck');
  }

  private tapCat(cat: Phaser.GameObjects.Image): void {
    const bad = spoiledFish().length;
    if (bad > 0) {
      modal(this, {
        title: '고등어가 쿨러를 쳐다본다',
        body: `상한 생선 ${bad}마리를 고등어에게 줄까?\n친해지면 입질 방향을 더 정확히 알려 준다.`,
        buttons: [
          { label: '아니', color: C.grayDark },
          {
            label: '주기',
            color: C.green,
            onClick: () => {
              feedCat();
              toast(this, `고등어가 골골거린다 (호감 ${S.catAffinity}/10)`);
            },
          },
        ],
      });
      return;
    }
    const b = bubble(this, cat.x, cat.y - 64, pick(['냐옹', '냐아...', '(하품)', '(꼬리를 흔든다)']), { wrap: 160 });
    this.tweens.add({ targets: cat, y: cat.y - 6, duration: 100, yoyo: true });
    this.time.delayedCall(1200, () => b.destroy());
  }

  private restaurant(): void {
    const canPay = Math.min(S.money, S.debt);
    modal(this, {
      title: '여울식당',
      body: `막순 할머니: "${pick(GRANDMA_LINES)}"\n\n남은 빚 ${won(S.debt)}\n소지금 ${won(S.money)}`,
      extra: (c, top) => {
        c.add(this.add.image(0, top + 34, 'grandma').setScale(3));
        return 76;
      },
      buttons: [
        { label: '10만 원 갚기', color: C.gold, disabled: S.debt <= 0 || S.money < 100_000, onClick: () => this.pay(100_000) },
        { label: `있는 돈 전부 갚기 (${won(canPay)})`, color: C.red, disabled: canPay <= 0, onClick: () => this.pay(canPay) },
        { label: '하루 마치기 (잠자기)', color: C.blueDark, onClick: () => this.sleep() },
        { label: '닫기', color: C.grayDark },
      ],
    });
  }

  private pay(amount: number): void {
    const paid = repay(amount);
    if (paid <= 0) return;
    this.hud.refresh();
    floatText(this, GAME_W / 2, 300, `-${won(paid)}`, T.red);
    if (S.debt <= 0) this.time.delayedCall(700, () => this.scene.restart());
    else toast(this, '할머니가 말없이 손을 잡아 주셨다', T.paper, 380);
  }

  private sleep(): void {
    modal(this, {
      title: '오늘을 마칠까요?',
      body: `${slotName()} 이후의 시간이 지나가고\n다음 날 새벽이 됩니다.\n쿨러의 생선 신선도가 떨어져요.`,
      buttons: [
        { label: '아니', color: C.grayDark },
        {
          label: '잠자기',
          color: C.blueDark,
          onClick: () => {
            endDay();
            goTo(this, 'Town');
          },
        },
      ],
    });
  }
}

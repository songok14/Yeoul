import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { feedCat, repay, spoiledFish } from '../state/actions';
import { deleteSave, persist, S } from '../state/save';
import { closedToday, dateLabel } from '../systems/calendar';
import { chapterOutcome } from '../systems/chapter';
import { marketHeadline } from '../systems/economy';
import { daysLeft, endDay, isMarketOpen, isTruckOpen, slotName } from '../systems/time';
import { Hud, HUD_H } from '../ui/hud';
import { backdrop, bubble, Button, drawPanel, fadeIn, floatText, goTo, modal, sprite, toast, txt } from '../ui/ui';
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
    const outcome = chapterOutcome();
    if (outcome === 'clear') {
      persist();
      this.scene.start('Prologue', { mode: 'clear' });
      return;
    }
    if (outcome === 'fail') {
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
    const closed = closedToday();
    const headline = closed.length ? `${closed.join('·')} 금어기` : marketHeadline();
    txt(this, 12, HUD_H + 4, `${dateLabel()} · ${headline}`, { size: 'small', color: T.paper });

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
    backdrop(this, `bg-village-${S.slot}`);

    // 그림 속 건물 위에 이름표를 단다
    this.sign(70, 300, '위판장', () => goTo(this, 'Market'));
    this.sign(118, 448, '미끼가게', () => goTo(this, 'Shop'));
    this.sign(372, 352, '여울식당', () => this.restaurant());
    this.sign(365, 262, '방파제 →', () => this.goFishing());

    const truck = sprite(this, 'truck', 300, 600, 78);
    truck.setInteractive({ useHandCursor: true }).on('pointerup', () => this.openTruck());

    const yeoul = sprite(this, 'ch-yeoul', 200, 636, 150);
    this.tweens.add({ targets: yeoul, scaleY: yeoul.scaleY * 1.012, duration: 1400, yoyo: true, repeat: -1, ease: 'Sine.InOut' });

    const cat = sprite(this, 'ch-cat', 120, 636, 62).setInteractive({ useHandCursor: true });
    cat.on('pointerup', () => this.tapCat(cat));
  }

  private sign(x: number, y: number, label: string, onTap: () => void): void {
    const t = txt(this, x, y, label, { size: 'small', color: T.ink, origin: [0.5, 0.5] }).setDepth(2);
    const w = t.width + 22;
    const g = this.add.graphics().setDepth(1);
    g.fillStyle(C.black, 0.25);
    g.fillRoundedRect(x - w / 2 + 2, y - 13, w, 28, 14);
    g.fillStyle(0xfbf6ea, 0.95);
    g.fillRoundedRect(x - w / 2, y - 15, w, 28, 14);
    const z = this.add.zone(x, y, w + 10, 40).setInteractive({ useHandCursor: true });
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
    const b = bubble(this, cat.x, cat.y - 84, pick(['냐옹', '냐아...', '(하품)', '(꼬리를 흔든다)']), { wrap: 160 });
    this.tweens.add({ targets: cat, y: cat.y - 6, duration: 100, yoyo: true });
    this.time.delayedCall(1200, () => b.destroy());
  }

  private restaurant(): void {
    const canPay = Math.min(S.money, S.debt);
    modal(this, {
      title: '여울식당',
      body: `막순 할머니: "${pick(GRANDMA_LINES)}"\n\n남은 빚 ${won(S.debt)}\n소지금 ${won(S.money)}`,
      extra: (c, top) => {
        c.add(sprite(this, 'ch-grandma', 0, top + 150, 150));
        return 158;
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

import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { CLEAR_PAGES, FAIL_PAGES, PROLOGUE_1, PROLOGUE_2, type StoryBg, type StoryPage } from '../data/story';
import { S, persist } from '../state/save';
import { SeasonGauge } from '../ui/gauge';
import { backdrop, Button, drawPanel, fadeIn, fishIcon, goTo, modal, sprite, txt } from '../ui/ui';

type Mode = 'p1' | 'p2' | 'clear' | 'fail';

interface PrologueData {
  part?: number | string;
  /** 확인용: 이 페이지부터 보여 준다 */
  page?: number | string;
  mode?: Mode;
}

/** 타자기 글자로 넘기는 이야기 장면. 프롤로그와 1장 결말에 함께 쓴다 */
export class PrologueScene extends Phaser.Scene {
  private mode: Mode = 'p1';
  private pages: StoryPage[] = [];
  private index = 0;
  private full = '';
  private shown = 0;
  private typing = false;
  private acc = 0;
  private blocked = false;
  private bgLayer!: Phaser.GameObjects.Container;
  private bodyText!: Phaser.GameObjects.Text;
  private nextMark!: Phaser.GameObjects.Text;
  private currentBg: string | null = null;
  private gauge?: SeasonGauge;

  constructor() {
    super('Prologue');
  }

  init(data: PrologueData): void {
    const part = Number(data.part ?? 1);
    this.mode = data.mode ?? (part === 2 ? 'p2' : 'p1');
    this.pages = { p1: PROLOGUE_1, p2: PROLOGUE_2, clear: CLEAR_PAGES, fail: FAIL_PAGES }[this.mode];
    this.index = Math.min(Number(data.page ?? 0), this.pages.length - 1);
    this.blocked = false;
    this.currentBg = null;
    this.gauge = undefined;
  }

  create(): void {
    fadeIn(this);
    this.bgLayer = this.add.container(0, 0);

    const g = this.add.graphics();
    drawPanel(g, 16, 548, GAME_W - 32, 236, 0x1f2b3d, C.paper);
    this.bodyText = txt(this, 36, 570, '', { size: 'body', color: T.paper, wrap: GAME_W - 72, lineSpacing: 10 });
    this.nextMark = txt(this, GAME_W - 40, 760, '▼', { size: 'small', color: T.gold, origin: [0.5, 0.5] });
    this.tweens.add({ targets: this.nextMark, y: 766, duration: 400, yoyo: true, repeat: -1 });

    if (this.mode === 'p1' || this.mode === 'p2') {
      new Button(this, GAME_W - 60, 26, '건너뛰기', () => this.finish(), { w: 104, h: 36, size: 'small', color: C.grayDark });
    }

    this.input.on('pointerdown', (_p: Phaser.Input.Pointer, over: Phaser.GameObjects.GameObject[]) => {
      if (over.length > 0 || this.blocked) return;
      this.advance();
    });

    this.showPage();
  }

  update(_t: number, delta: number): void {
    const dt = delta / 1000;
    this.gauge?.tick(dt);
    if (!this.typing) return;
    this.acc += delta;
    while (this.acc > 38 && this.shown < this.full.length) {
      this.acc -= 38;
      this.shown += 1;
    }
    this.bodyText.setText(this.full.slice(0, this.shown));
    if (this.shown >= this.full.length) this.typing = false;
    this.nextMark.setVisible(!this.typing);
  }

  private showPage(): void {
    const page = this.pages[this.index];
    const look = `${page.bg}/${page.pic ?? ''}`;
    if (look !== this.currentBg) {
      this.drawBg(page.bg);
      if (page.pic) this.drawPic(page.pic);
      this.currentBg = look;
    }
    // 글자가 늘어나며 줄이 바뀌지 않도록 미리 줄바꿈을 계산해 둔다
    this.full = this.bodyText.getWrappedText(page.text).join('\n');
    this.shown = 0;
    this.acc = 0;
    this.typing = true;
    this.bodyText.setText('');
    this.nextMark.setVisible(false);
  }

  private advance(): void {
    if (this.typing) {
      this.shown = this.full.length;
      return;
    }
    const page = this.pages[this.index];
    if (page.event === 'salt' && !this.gauge) {
      this.saltEvent();
      return;
    }
    if (page.event === 'cat') {
      this.catEvent();
    }
    this.index += 1;
    if (this.index >= this.pages.length) this.finish();
    else this.showPage();
  }

  /** 미각 0단계의 간 맞추기. 정답 구간이 보이지 않는다 */
  private saltEvent(): void {
    this.blocked = true;
    this.bodyText.setText('');
    this.nextMark.setVisible(false);
    const gauge = new SeasonGauge(this, GAME_W / 2, 650, 340, { target: 0.58, zoneHalf: 0.1, visible: false, speed: 0.9, label: '소금 (???)' });
    this.gauge = gauge;
    const btn = new Button(this, GAME_W / 2, 735, '소금 치기', () => {
      gauge.stop();
      btn.destroy();
      this.time.delayedCall(500, () => {
        this.cameras.main.flash(300, 255, 255, 255);
        gauge.reveal();
        this.time.delayedCall(900, () => {
          gauge.destroy();
          this.gauge = undefined;
          this.blocked = false;
          this.index += 1;
          this.showPage();
        });
      });
    }, { w: 200, h: 50, color: C.red });
  }

  private catEvent(): void {
    const cat = sprite(this, 'ch-cat-walk', -40, 520, 90).setFlipX(true);
    this.bgLayer.add(cat);
    this.tweens.add({
      targets: cat,
      x: 250,
      duration: 500,
      ease: 'Quad.Out',
      onComplete: () => {
        this.tweens.add({ targets: cat, x: GAME_W + 60, y: 440, duration: 600, delay: 300, ease: 'Quad.In' });
      },
    });
  }

  private finish(): void {
    if (this.mode === 'p1') {
      goTo(this, 'Fishing', { tutorial: true });
      return;
    }
    if (this.mode === 'p2') {
      S.flags.prologueDone = true;
      S.flags.firstTaste = true;
      S.flags.taste1 = true;
      S.dishesCooked = Math.max(1, S.dishesCooked);
      persist();
      this.blocked = true;
      modal(this, {
        title: '미각 회복 1단계: 짠맛',
        body: '간 맞추기에서 짠맛의 정답 구간이 보입니다.\n\n30일 안에 300만 원을 모아 여울식당을 지키세요.\n어종을 많이 낚을수록 맛이 돌아옵니다.',
        buttons: [{ label: '물비늘포로', color: C.green, onClick: () => goTo(this, 'Town') }],
      });
      return;
    }
    if (this.mode === 'clear') {
      goTo(this, 'Town');
      return;
    }
    goTo(this, 'Title');
  }

  /** 감정 그림: 배경 위에 밝은 종이를 깔고 (흰 테두리가 안 보이게) 가운데 크게 올린다 */
  private drawPic(key: string): void {
    this.bgLayer.add(this.add.rectangle(0, 0, GAME_W, 548, 0xf4ecd8, 0.88).setOrigin(0, 0));
    const pic = sprite(this, key, GAME_W / 2, 556, 470);
    pic.setAlpha(0);
    this.tweens.add({ targets: pic, alpha: 1, duration: 400 });
    this.bgLayer.add(pic);
  }

  private drawBg(bg: StoryBg): void {
    this.bgLayer.removeAll(true);
    const add = <T extends Phaser.GameObjects.GameObject>(o: T): T => {
      this.bgLayer.add(o);
      return o;
    };
    const g = add(this.add.graphics());
    const W = GAME_W;
    switch (bg) {
      case 'kitchen':
      case 'ash': {
        g.fillStyle(bg === 'kitchen' ? 0x2a1410 : 0x1a1a1e, 1);
        g.fillRect(0, 0, W, 548);
        // 화구와 팬
        g.fillStyle(0x3a3a3a, 1);
        g.fillRect(40, 380, 370, 30);
        g.fillStyle(0x555555, 1);
        g.fillRect(90, 350, 90, 12);
        g.fillRect(260, 350, 90, 12);
        if (bg === 'kitchen') {
          for (let i = 0; i < 14; i++) {
            const f = add(this.add.rectangle(60 + i * 26, 340, 18, 40 + (i % 3) * 30, i % 2 ? 0xf08a3a : 0xd9534f).setOrigin(0.5, 1));
            this.tweens.add({ targets: f, scaleY: 1.4, duration: 180 + (i % 4) * 60, yoyo: true, repeat: -1 });
          }
        } else {
          g.fillStyle(0x444448, 0.6);
          for (let i = 0; i < 8; i++) g.fillCircle(80 + i * 45, 200 + (i % 3) * 40, 30);
          add(txt(this, W / 2, 470, '...', { size: 'title', color: '#6a6a70', origin: [0.5, 0.5] }));
        }
        break;
      }
      case 'phone': {
        g.fillStyle(0x0f1a2b, 1);
        g.fillRect(0, 0, W, 548);
        for (let i = 0; i < 9; i++) {
          const bh = 120 + ((i * 37) % 160);
          g.fillStyle(0x1b2a41, 1);
          g.fillRect(i * 52, 548 - bh, 46, bh);
          g.fillStyle(C.gold, 0.7);
          for (let j = 0; j < 6; j++) if ((i + j) % 3) g.fillRect(i * 52 + 8 + (j % 2) * 18, 548 - bh + 14 + Math.floor(j / 2) * 26, 8, 8);
        }
        g.fillStyle(C.ink, 1);
        g.fillRect(185, 150, 80, 140);
        g.fillStyle(0x8cc8ff, 1);
        g.fillRect(191, 160, 68, 112);
        add(txt(this, 225, 215, '할머니\n위독', { size: 'small', color: T.ink, origin: [0.5, 0.5], align: 'center' }));
        break;
      }
      case 'harbor':
      case 'breakwater':
      case 'memory':
      case 'night':
      case 'dawn': {
        const key = { harbor: 'bg-village-1', breakwater: 'bg-breakwater', memory: 'bg-village-3', night: 'bg-village-0', dawn: 'bg-village-1' }[bg];
        const tint = { harbor: 0xffffff, breakwater: 0xffd8b8, memory: 0xe8c89a, night: 0x6a70a8, dawn: 0xfff0dc }[bg];
        add(backdrop(this, key, tint));
        if (bg === 'memory') {
          // 오래된 기억: 빛바랜 세피아
          add(this.add.rectangle(0, 0, W, 800, 0x8a5a3b, 0.28).setOrigin(0, 0));
        } else if (bg === 'breakwater' || bg === 'night') {
          add(sprite(this, 'ch-yeoul-back', 200, 548, 200));
        } else if (bg === 'harbor') {
          add(sprite(this, 'truck', 150, 540, 90));
        }
        break;
      }
      case 'notice': {
        g.fillStyle(0x2b2420, 1);
        g.fillRect(0, 0, W, 548);
        g.fillStyle(C.paper, 1);
        g.fillRect(85, 80, 280, 380);
        g.fillStyle(C.paperDark, 1);
        for (let i = 0; i < 9; i++) g.fillRect(110, 180 + i * 26, 230 - (i % 3) * 40, 6);
        add(txt(this, 225, 120, '차압 예고 통지서', { size: 'h1', color: T.ink, origin: [0.5, 0.5] }));
        g.lineStyle(4, C.red, 1);
        g.strokeCircle(300, 400, 36);
        add(txt(this, 300, 400, '차압', { size: 'h1', color: T.red, origin: [0.5, 0.5] }));
        break;
      }
      case 'storage': {
        g.fillStyle(0x3b2a20, 1);
        g.fillRect(0, 0, W, 548);
        g.fillStyle(0x4a3528, 1);
        for (let i = 0; i < 8; i++) g.fillRect(0, i * 60, W, 4);
        add(sprite(this, 'truck', 225, 400, 160));
        // 부러진 대나무대
        g.lineStyle(5, 0xc8a85a, 1);
        g.lineBetween(30, 520, 120, 300);
        g.lineBetween(140, 520, 110, 420);
        g.fillStyle(C.paper, 1);
        g.fillRect(330, 470, 70, 50);
        g.fillStyle(C.paperDark, 1);
        g.fillRect(366, 470, 34, 50);
        break;
      }
      case 'grill': {
        g.fillStyle(0x141c2a, 1);
        g.fillRect(0, 0, W, 548);
        g.fillStyle(0x2a2f3a, 1);
        g.fillRect(0, 430, W, 118);
        g.fillStyle(0x555555, 1);
        g.fillRect(120, 400, 210, 14);
        for (let i = 0; i < 8; i++) {
          const f = add(this.add.rectangle(140 + i * 24, 430, 16, 30, i % 2 ? 0xf08a3a : C.gold).setOrigin(0.5, 1));
          this.tweens.add({ targets: f, scaleY: 1.5, duration: 160 + (i % 3) * 70, yoyo: true, repeat: -1 });
        }
        add(fishIcon(this, 'bollak', 225, 380, 140));
        add(sprite(this, 'ch-yeoul', 84, 548, 230));
        break;
      }
      case 'seize': {
        g.fillStyle(0x2b2420, 1);
        g.fillRect(0, 0, W, 548);
        g.fillStyle(0x6a4a30, 1);
        g.fillRect(110, 120, 230, 380);
        g.fillStyle(C.paper, 1);
        g.fillRect(150, 160, 150, 60);
        add(txt(this, 225, 190, '여울식당', { size: 'h1', color: T.ink, origin: [0.5, 0.5] }));
        g.fillStyle(C.red, 1);
        g.fillRect(170, 300, 110, 70);
        add(txt(this, 225, 335, '압류', { size: 'h1', color: T.white, origin: [0.5, 0.5] }));
        break;
      }
    }
  }
}

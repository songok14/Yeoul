import Phaser from 'phaser';
import { C, CASTS_PER_TRIP, GAME_W, T } from '../config';
import { SPECIES_BY_ID, type Species } from '../data/fish';
import { REELS, RODS } from '../data/gear';
import { addToCooler, coolerFull, recordCatch } from '../state/actions';
import { S, persist } from '../state/save';
import { coolerCapacity, fishValue } from '../systems/economy';
import { rollSize, rollSpecies, zoneOf, ZONE_NAME } from '../systems/fishing';
import { checkTasteUnlock } from '../systems/taste';
import { useSlot } from '../systems/time';
import { Hud } from '../ui/hud';
import { backdrop, bubble, Button, drawPanel, fadeIn, goTo, modal, sprite, toast, txt } from '../ui/ui';
import { clamp, lerp, won } from '../utils';

type Phase = 'aim' | 'power' | 'flying' | 'wait' | 'bite' | 'reel' | 'result' | 'busy';

// 배경 그림(방파제) 기준 좌표
const HORIZON = 290;
const SHORE_Y = 560;
const TIP_IDLE = { x: 238, y: 528 };
const GAUGE_X = 45;
const GAUGE_W = 360;
const GAUGE_Y = 768;
const SLACK = 12;

/** 방파제 찌낚시: 캐스팅 → 입질 대기 → 챔질 → 릴링 */
export class FishingScene extends Phaser.Scene {
  private tutorial = false;
  private phase: Phase = 'aim';
  private hud?: Hud;
  private castsLeft = CASTS_PER_TRIP;
  private castsDone = 0;
  private caught = 0;

  private rodG!: Phaser.GameObjects.Graphics;
  private uiG!: Phaser.GameObjects.Graphics;
  private laneG!: Phaser.GameObjects.Graphics;
  private bobber!: Phaser.GameObjects.Container;
  private shadow!: Phaser.GameObjects.Ellipse;
  private msg!: Phaser.GameObjects.Text;
  private info!: Phaser.GameObjects.Text;
  private sub!: Phaser.GameObjects.Text;
  private cat?: Phaser.GameObjects.Image;
  private catBubble?: Phaser.GameObjects.Container;
  private ripple?: Phaser.GameObjects.Ellipse;
  private exitBtn?: Button;
  private zoneLabels: Phaser.GameObjects.Text[] = [];

  private lane = 0;
  private hotLane = 0;
  private hintLane = 0;
  private gaugePos = 0;
  private gaugeDir = 1;
  private dist01 = 0;
  private target = { x: 0, y: 0 };
  private bobT = 0;
  private waitTimer = 0;
  private nibbleTimer = 0;
  private biteTimer = 0;
  private biteElapsed = 0;
  private species!: Species;
  private size = 0;
  private hot = false;
  private tension = 0;
  private distance = 0;
  private startDistance = 0;
  private slackTime = 0;
  private surgeTimer = 0;
  private surgeKick = 0;
  private holding = false;
  private fishWobble = 0;
  private fightTime = 0;

  constructor() {
    super('Fishing');
  }

  init(data: { tutorial?: boolean | string }): void {
    this.tutorial = data.tutorial === true || data.tutorial === 'true' || data.tutorial === '1';
    this.phase = 'aim';
    this.castsLeft = CASTS_PER_TRIP;
    this.castsDone = 0;
    this.caught = 0;
    this.holding = false;
    this.cat = undefined;
    this.catBubble = undefined;
    this.ripple = undefined;
    this.hud = undefined;
  }

  create(): void {
    fadeIn(this);
    // 시간대에 맞춰 배경 색을 살짝 바꾼다
    const slot = this.tutorial ? 3 : S.slot;
    backdrop(this, 'bg-breakwater', [0xb8b4d8, 0xfff4e6, 0xffffff, 0xffc8a8][slot]);

    this.laneG = this.add.graphics();
    this.shadow = this.add.ellipse(0, 0, 40, 14, 0x0b1a2a, 0.55).setVisible(false);
    this.bobber = this.makeBobber();
    this.bobber.setVisible(false);

    sprite(this, 'ch-yeoul-back', 200, 702, 180);
    this.rodG = this.add.graphics();
    if (!this.tutorial) {
      this.cat = sprite(this, 'ch-cat', 352, 698, 70);
      this.cat.setInteractive({ useHandCursor: true }).on('pointerdown', () => this.showCatHint());
    }

    // 아래쪽 안내판
    const panel = this.add.graphics();
    drawPanel(panel, 0, 700, GAME_W, 100, 0x1f2b3d, C.ink, false);
    this.uiG = this.add.graphics();
    this.msg = txt(this, GAME_W / 2, 708, '', { size: 'body', color: T.paper, origin: [0.5, 0], align: 'center', wrap: 420 });
    this.sub = txt(this, GAME_W / 2, 742, '', { size: 'small', color: T.gray, origin: [0.5, 0], align: 'center', wrap: 420 });

    if (this.tutorial) {
      txt(this, 12, 12, '프롤로그 · 방파제', { size: 'h1', color: T.paper });
    } else {
      this.hud = new Hud(this);
      this.exitBtn = new Button(this, GAME_W - 52, 86, '철수', () => this.confirmExit(), { w: 84, h: 38, size: 'small', color: C.grayDark });
    }
    // 파워 게이지 위 거리 구간 이름
    const reach = this.tutorial ? 1 : RODS[S.rod].reach;
    const b1 = Math.min(1, 0.35 / reach);
    const b2 = Math.min(1, 0.7 / reach);
    this.zoneLabels = [
      [0, b1],
      [b1, b2],
      [b2, 1],
    ].map(([a, b], i) =>
      txt(this, GAUGE_X + GAUGE_W * ((a + b) / 2), GAUGE_Y + 12, ZONE_NAME[i], { size: 'small', color: T.white, origin: [0.5, 0.5], stroke: '#1b2a41' })
        .setDepth(5)
        .setVisible(false),
    );
    this.info = txt(this, 12, this.tutorial ? 44 : 70, '', { size: 'small', color: T.paper, stroke: '#1b2a41' });

    this.input.on('pointerdown', (p: Phaser.Input.Pointer, over: Phaser.GameObjects.GameObject[]) => {
      if (over.length > 0) return;
      this.onTap(p);
    });
    this.input.on('pointerup', () => {
      this.holding = false;
    });

    this.drawRod(TIP_IDLE.x, TIP_IDLE.y, false);
    this.startAim();
  }

  // ───────── 그리기 ─────────

  private makeBobber(): Phaser.GameObjects.Container {
    const c = this.add.container(0, 0);
    const g = this.add.graphics();
    g.fillStyle(C.ink, 1);
    g.fillRect(-4, -18, 8, 20);
    g.fillStyle(C.red, 1);
    g.fillRect(-2, -16, 4, 9);
    g.fillStyle(C.gold, 1);
    g.fillRect(-2, -7, 4, 3);
    g.fillStyle(C.white, 1);
    g.fillRect(-2, -4, 4, 4);
    g.fillStyle(C.foam, 0.7);
    g.fillRect(-12, 2, 24, 2);
    c.add(g);
    return c;
  }

  /** 낚싯대는 그림에 있으니 초릿대 끝의 휨과 낚싯줄만 그린다 */
  private drawRod(tx: number, ty: number, withLine: boolean, lx = 0, ly = 0): void {
    const g = this.rodG;
    g.clear();
    if (tx !== TIP_IDLE.x || ty !== TIP_IDLE.y) {
      g.lineStyle(3, 0x2b2b2b, 1);
      g.lineBetween(TIP_IDLE.x - 4, TIP_IDLE.y + 10, tx, ty);
    }
    if (withLine) {
      g.lineStyle(1, C.white, 0.8);
      g.lineBetween(tx, ty, lx, ly);
    }
  }

  private posFor(lane: number, d: number): { x: number; y: number; s: number } {
    return {
      x: GAME_W / 2 + lane * (44 + 74 * (1 - d)),
      y: SHORE_Y - 14 - d * (SHORE_Y - HORIZON - 40),
      s: 1.6 - d * 0.9,
    };
  }

  private setInfo(): void {
    if (this.tutorial) {
      this.info.setText('처음 한 마리를 낚아 보자');
      return;
    }
    this.hud?.refresh();
    this.info.setText(`캐스팅 ${this.castsLeft}/${CASTS_PER_TRIP}  미끼 ${S.bait}  쿨러 ${S.cooler.length}/${coolerCapacity()}`);
  }

  private say(main: string, sub = ''): void {
    this.msg.setText(main);
    this.sub.setText(sub);
    this.sub.setY(this.msg.y + this.msg.height + 2);
  }

  // ───────── 단계 ─────────

  private startAim(): void {
    this.phase = 'aim';
    this.uiG.clear();
    this.bobber.setVisible(false);
    this.shadow.setVisible(false);
    this.drawRod(TIP_IDLE.x, TIP_IDLE.y, false);
    this.exitBtn?.setEnabled(true);
    this.setInfo();

    this.hotLane = Phaser.Math.Between(-1, 1);
    const accuracy = Math.min(0.95, 0.6 + S.catAffinity * 0.035);
    this.hintLane = Math.random() < accuracy ? this.hotLane : Phaser.Math.Between(-1, 1);

    this.drawLanes();
    if (this.tutorial) {
      this.say('바다를 눌러 던질 곳을 고르세요', '왼쪽 · 가운데 · 오른쪽');
    } else {
      this.say('바다를 눌러 던질 곳을 고르세요', '고등어가 우는 쪽에 입질이 많다');
      this.showCatHint();
    }
  }

  private drawLanes(): void {
    const g = this.laneG;
    g.clear();
    for (const lane of [-1, 0, 1]) {
      const near = this.posFor(lane, 0.05);
      const far = this.posFor(lane, 0.95);
      g.fillStyle(C.white, this.phase === 'power' && lane === this.lane ? 0.22 : 0.07);
      g.fillTriangle(near.x - 40, near.y, near.x + 40, near.y, far.x, far.y);
    }
  }

  private showCatHint(): void {
    if (!this.cat || this.phase !== 'aim') return;
    this.catBubble?.destroy();
    const dirText = this.hintLane === -1 ? '왼쪽' : this.hintLane === 1 ? '오른쪽' : '앞바다';
    this.catBubble = bubble(this, 352, 594, `냐아옹\n(${dirText}을 본다)`, { wrap: 160 });
    this.cat.setFlipX(this.hintLane === 1);
    this.tweens.add({ targets: this.cat, y: this.cat.y - 6, duration: 120, yoyo: true });
    this.ripple?.destroy();
    const p = this.posFor(this.hintLane, 0.55);
    this.ripple = this.add.ellipse(p.x, p.y, 30, 10).setStrokeStyle(2, C.foam, 0.9);
    this.tweens.add({ targets: this.ripple, scaleX: 2.2, scaleY: 2.2, alpha: 0, duration: 1100, repeat: -1 });
  }

  private onTap(p: Phaser.Input.Pointer): void {
    switch (this.phase) {
      case 'aim':
        if (p.worldY > SHORE_Y + 10 || p.worldY < HORIZON) return;
        this.lane = p.worldX < 150 ? -1 : p.worldX > 300 ? 1 : 0;
        this.startPower();
        break;
      case 'power':
        this.cast();
        break;
      case 'wait':
        this.earlyStrike();
        break;
      case 'bite':
        this.strike();
        break;
      case 'reel':
        this.holding = true;
        break;
      default:
        break;
    }
  }

  private startPower(): void {
    if (!this.tutorial && S.bait <= 0) {
      this.say('미끼가 떨어졌다', '노씨 미끼가게에서 사 오자');
      this.time.delayedCall(1400, () => this.endTrip());
      this.phase = 'busy';
      return;
    }
    this.phase = 'power';
    this.exitBtn?.setEnabled(false);
    this.catBubble?.destroy();
    this.drawLanes();
    this.gaugePos = 0;
    this.gaugeDir = 1;
    this.say('한 번 더 눌러서 던지기!');
    this.zoneLabels.forEach((t) => t.setVisible(true));
  }

  private drawPowerGauge(): void {
    const g = this.uiG;
    g.clear();
    const reach = this.tutorial ? 1 : RODS[S.rod].reach;
    const y = GAUGE_Y;
    g.fillStyle(C.ink, 1);
    g.fillRect(GAUGE_X - 3, y - 3, GAUGE_W + 6, 30);
    // 거리 구간: 실제 거리 = 파워 × 낚싯대 비거리
    const b1 = Math.min(1, 0.35 / reach);
    const b2 = Math.min(1, 0.7 / reach);
    g.fillStyle(0x4ea3c7, 1);
    g.fillRect(GAUGE_X, y, GAUGE_W * b1, 24);
    g.fillStyle(0x2e6f95, 1);
    g.fillRect(GAUGE_X + GAUGE_W * b1, y, GAUGE_W * (b2 - b1), 24);
    if (b2 < 1) {
      g.fillStyle(0x1b4466, 1);
      g.fillRect(GAUGE_X + GAUGE_W * b2, y, GAUGE_W * (1 - b2), 24);
    }
    const nx = GAUGE_X + this.gaugePos * GAUGE_W;
    g.fillStyle(C.gold, 1);
    g.fillRect(nx - 3, y - 6, 6, 36);
    g.fillStyle(C.ink, 1);
    g.fillRect(nx - 1, y - 6, 2, 36);
  }

  private cast(): void {
    this.zoneLabels.forEach((t) => t.setVisible(false));
    const reach = this.tutorial ? 1 : RODS[S.rod].reach;
    this.dist01 = clamp(this.gaugePos * reach, 0.05, 1);
    if (!this.tutorial) {
      S.bait -= 1;
      this.castsLeft -= 1;
      persist();
    }
    this.castsDone += 1;
    this.setInfo();
    this.uiG.clear();
    this.laneG.clear();
    this.ripple?.destroy();
    this.phase = 'flying';
    this.hot = !this.tutorial && this.lane === this.hotLane;

    const to = this.posFor(this.lane, this.dist01);
    this.target = { x: to.x, y: to.y };
    this.say(`${ZONE_NAME[zoneOf(this.dist01)]}에 던졌다`);
    this.bobber.setVisible(true).setScale(to.s);
    const from = { x: TIP_IDLE.x, y: TIP_IDLE.y };
    const peak = Math.min(from.y, to.y) - 120;
    const tw = { t: 0 };
    this.tweens.add({
      targets: tw,
      t: 1,
      duration: 650,
      ease: 'Sine.Out',
      onUpdate: () => {
        const x = lerp(from.x, to.x, tw.t);
        const y = (1 - tw.t) * (1 - tw.t) * from.y + 2 * (1 - tw.t) * tw.t * peak + tw.t * tw.t * to.y;
        this.bobber.setPosition(x, y);
        this.drawRod(TIP_IDLE.x, TIP_IDLE.y, true, x, y);
      },
      onComplete: () => this.landed(),
    });
  }

  private splash(x: number, y: number, s = 1): void {
    const r = this.add.ellipse(x, y, 20 * s, 6 * s).setStrokeStyle(2, C.foam, 1);
    this.tweens.add({ targets: r, scaleX: 3, scaleY: 3, alpha: 0, duration: 600, onComplete: () => r.destroy() });
  }

  private landed(): void {
    this.splash(this.target.x, this.target.y, this.bobber.scale);
    this.phase = 'wait';
    this.bobT = 0;
    const base = this.tutorial ? 2.2 : Phaser.Math.FloatBetween(2.2, 5.2);
    this.waitTimer = base * (this.hot ? 0.55 : 1);
    this.nibbleTimer = Phaser.Math.FloatBetween(0.6, 1.4);
    this.say('찌를 지켜보세요', this.tutorial ? '톡톡 건드리는 건 속임수. 쑥 들어갈 때 누르기' : this.hot ? '물살이 좋다. 금방 올 것 같다' : '');
  }

  private earlyStrike(): void {
    if (this.tutorial) {
      this.say('아직이에요', '찌가 물속으로 쑥 들어갈 때 누르세요');
      return;
    }
    this.phase = 'busy';
    this.say('헛챔질!', '고기가 놀라서 흩어졌다');
    this.finishCast();
  }

  private startBite(): void {
    this.phase = 'bite';
    this.biteElapsed = 0;
    const zone = zoneOf(this.dist01);
    this.species = this.tutorial ? SPECIES_BY_ID.bollak : rollSpecies(zone, S.slot, this.hot);
    this.size = this.tutorial ? 19.4 : rollSize(this.species, this.hot);
    this.biteTimer = this.tutorial ? 1.6 : 0.62 + RODS[S.rod].hookBonus / 1000 + (this.species.rare ? -0.08 : 0);
    this.bobber.setY(this.target.y + 10 * this.bobber.scale);
    this.bobber.setAlpha(0.35);
    this.splash(this.target.x, this.target.y, this.bobber.scale * 1.4);
    this.cameras.main.shake(120, 0.004);
    this.say('지금! 눌러서 챔질!', '찌가 쑥 들어갔다');
    this.msg.setColor(T.gold);
  }

  private strike(): void {
    const perfect = this.biteElapsed < 0.28;
    this.msg.setColor(T.paper);
    this.bobber.setAlpha(1);
    this.startReel(perfect);
  }

  private startReel(perfect: boolean): void {
    this.phase = 'reel';
    this.holding = this.input.activePointer.isDown;
    this.bobber.setVisible(false);
    this.shadow.setVisible(true);
    this.startDistance = 8 + this.dist01 * 26;
    this.distance = this.startDistance - (perfect ? 3 : 0);
    this.tension = 35;
    this.slackTime = 0;
    this.surgeTimer = this.tutorial ? 2.5 : Phaser.Math.FloatBetween(0.8, 1.8);
    this.surgeKick = 0;
    this.fishWobble = Math.random() * 10;
    this.fightTime = 0;
    const s = clamp(this.size / this.species.avgSize, 0.7, 1.6);
    this.shadow.setSize(34 * s, 12 * s);
    toast(this, perfect ? '완벽한 챔질!' : '걸었다!', perfect ? T.gold : T.paper, 200);
    this.say('꾹 누르면 릴을 감아요', '빨간색이면 손을 떼고 파란색이면 감기');
  }

  /** 물고기 힘. 오래 버틸수록 지쳐서 18초 뒤에는 절반 가까이 약해진다 */
  private fishStrength(): number {
    if (this.tutorial) return 0.8;
    const tired = 1 - 0.55 * clamp(this.fightTime / 18, 0, 1);
    return this.species.strength * clamp(this.size / this.species.avgSize, 0.8, 1.5) * tired;
  }

  private updateReel(dt: number): void {
    const reel = this.tutorial ? REELS[1] : REELS[S.reel];
    const rod = this.tutorial ? RODS[1] : RODS[S.rod];
    this.fightTime += dt;
    const str = this.fishStrength();

    if (this.holding) {
      let rise = (26 + str * 9) * reel.tensionMul;
      if (this.tension > reel.drag) rise *= 0.3;
      this.tension += rise * dt;
      this.distance -= reel.speed * (this.tension > SLACK ? 1 : 0.6) * dt;
    } else {
      this.tension -= 48 * dt;
      this.distance += str * 0.55 * dt;
    }

    this.surgeTimer -= dt;
    if (this.surgeTimer <= 0) {
      const kick = (9 + str * 5) * (1 - rod.absorb);
      this.tension += kick;
      this.distance += 1.2;
      this.surgeKick = 0.35;
      this.cameras.main.shake(90, 0.003);
      this.surgeTimer = Phaser.Math.FloatBetween(1.1, 2.6) / (0.7 + str * 0.12);
      if (!this.tutorial) toast(this, '꾹꾹!', T.red, 260);
    }
    this.surgeKick = Math.max(0, this.surgeKick - dt);

    this.tension = clamp(this.tension, 0, 110);
    this.distance = Math.min(this.distance, this.startDistance + 4);

    if (this.tension < SLACK) this.slackTime += dt;
    else this.slackTime = Math.max(0, this.slackTime - dt * 2);

    const slackLimit = this.tutorial ? 3.2 : this.species.id === 'hakgongchi' ? 1.1 : 1.6;
    if (this.tension >= 100) {
      if (this.tutorial) {
        this.tension = 70;
        toast(this, '줄이 끊어질 뻔했다! 손을 떼요', T.red, 260);
      } else return this.lose('팅! 줄이 터졌다', '텐션이 빨간색이면 손을 떼야 한다');
    }
    if (this.slackTime > slackLimit) {
      if (this.tutorial) {
        this.slackTime = 0;
        toast(this, '줄이 너무 느슨해요! 감아요', T.blue, 260);
      } else return this.lose('바늘이 빠졌다', '줄이 너무 오래 느슨했다');
    }
    if (this.distance <= 0) return this.landFish();

    // 물고기 그림자와 낚싯줄
    const p = clamp(1 - this.distance / this.startDistance, 0, 1);
    this.fishWobble += dt * (2 + str * 0.6 + this.surgeKick * 20);
    const from = this.posFor(this.lane, this.dist01);
    const fx = lerp(from.x, GAME_W / 2 + 10, p) + Math.sin(this.fishWobble) * (18 + this.surgeKick * 40) * (1 - p * 0.6);
    const fy = lerp(from.y, SHORE_Y - 16, p);
    this.shadow.setPosition(fx, fy);
    this.shadow.setScale(lerp(0.6, 1.3, p));
    const bend = this.tension / 100;
    const tipX = lerp(TIP_IDLE.x, fx, bend * 0.25);
    const tipY = lerp(TIP_IDLE.y, fy, bend * 0.2) + bend * 18;
    this.drawRod(tipX, tipY, true, fx, fy);
    this.drawTension(reel.drag);
    this.sub.setText(`남은 거리 ${Math.max(0, this.distance).toFixed(1)}m`);
  }

  private drawTension(drag: number): void {
    const g = this.uiG;
    g.clear();
    const y = GAUGE_Y;
    g.fillStyle(C.ink, 1);
    g.fillRect(GAUGE_X - 3, y - 3, GAUGE_W + 6, 30);
    g.fillStyle(C.blue, 1);
    g.fillRect(GAUGE_X, y, GAUGE_W * (SLACK / 100), 24);
    g.fillStyle(C.greenDark, 1);
    g.fillRect(GAUGE_X + GAUGE_W * (SLACK / 100), y, GAUGE_W * ((85 - SLACK) / 100), 24);
    g.fillStyle(C.redDark, 1);
    g.fillRect(GAUGE_X + GAUGE_W * 0.85, y, GAUGE_W * 0.15, 24);
    if (drag <= 100) {
      g.fillStyle(C.gold, 0.8);
      g.fillRect(GAUGE_X + GAUGE_W * (drag / 100) - 1, y, 2, 24);
    }
    const t = clamp(this.tension / 100, 0, 1);
    const col = this.tension >= 85 ? C.red : this.tension < SLACK ? 0x8cc8ff : C.green;
    g.fillStyle(col, 1);
    g.fillRect(GAUGE_X, y + 6, GAUGE_W * t, 12);
    g.fillStyle(C.white, 1);
    g.fillRect(GAUGE_X + GAUGE_W * t - 2, y - 5, 4, 34);
  }

  private lose(main: string, sub: string): void {
    this.phase = 'busy';
    this.uiG.clear();
    this.shadow.setVisible(false);
    this.splash(this.shadow.x, this.shadow.y, 1.5);
    this.drawRod(TIP_IDLE.x, TIP_IDLE.y, false);
    this.say(main, sub);
    if (this.tutorial) {
      this.time.delayedCall(1500, () => this.startAim());
      return;
    }
    this.finishCast();
  }

  private landFish(): void {
    this.phase = 'result';
    this.uiG.clear();
    this.shadow.setVisible(false);
    this.drawRod(TIP_IDLE.x, TIP_IDLE.y, false);
    this.say('');
    this.showResult();
  }

  private showResult(): void {
    const sp = this.species;
    const size = this.size;
    const isNew = !S.dex[sp.id] || (S.dex[sp.id]?.count ?? 0) === 0;
    const value = fishValue({ uid: 0, species: sp.id, size, caughtDay: S.day, fresh: 100 });
    const full = !this.tutorial && coolerFull();
    // 금지체장 미달이면 챙길 수 없다
    const undersize = sp.limitCm !== undefined && size <= sp.limitCm;

    const finish = (released: boolean) => {
      recordCatch(sp.id, size, released);
      this.caught += 1;
      if (!released && !this.tutorial) addToCooler(sp.id, size);
      if (this.tutorial) {
        S.flags.tutorialFish = true;
        persist();
        goTo(this, 'Prologue', { part: 2 });
        return;
      }
      if (released) {
        S.flags.released = true;
        persist();
      }
      this.setInfo();
      const unlocked = checkTasteUnlock();
      persist();
      if (unlocked) {
        modal(this, {
          title: `미각이 돌아온다: ${unlocked.name}`,
          body: `${unlocked.effect}\n\n낚은 어종이 늘어날수록 맛이 선명해진다.`,
          buttons: [{ label: '좋아', color: C.green, onClick: () => this.finishCast() }],
        });
      } else this.finishCast();
    };

    modal(this, {
      title: `${sp.name}${isNew ? ' (새 어종!)' : ''}`,
      body: `${size.toFixed(1)}cm${sp.rare ? ' · 귀한 고기' : ''}\n${this.tutorial ? '손바닥만 한 볼락이다' : `오늘 위판장 시세로 약 ${won(value)}`}${undersize ? `\n금지체장 ${sp.limitCm}cm 이하라 놓아줘야 한다` : full ? '\n쿨러가 가득 찼다' : ''}`,
      extra: (c, top) => {
        const img = this.add.image(0, top + 40, `fish-${sp.id}`).setScale(sp.look.len > 20 ? 5 : 6);
        c.add(img);
        this.tweens.add({ targets: img, angle: { from: -4, to: 4 }, duration: 300, yoyo: true, repeat: -1 });
        return 96;
      },
      buttons: this.tutorial
        ? [{ label: '챙기기', color: C.green, onClick: () => finish(false) }]
        : [
            { label: '놓아주기', color: C.blueDark, onClick: () => finish(true) },
            { label: undersize ? '금지체장' : full ? '쿨러 가득' : '챙기기', color: C.green, disabled: full || undersize, onClick: () => finish(false) },
          ],
    });
    if (isNew && !this.tutorial) toast(this, '도감에 새로 등록!', T.gold, 150);
  }

  private finishCast(): void {
    this.phase = 'busy';
    this.time.delayedCall(1200, () => {
      if (this.castsLeft <= 0 || S.bait <= 0) this.endTrip();
      else this.startAim();
    });
  }

  private confirmExit(): void {
    if (this.phase !== 'aim') return;
    modal(this, {
      title: this.castsDone > 0 ? '철수할까요?' : '돌아갈까요?',
      body: this.castsDone > 0 ? '시간이 한 칸 지나갑니다.' : '아직 던지지 않아서 시간은 그대로예요.',
      buttons: [
        { label: '계속 낚시', color: C.grayDark },
        { label: '마을로', color: C.blue, onClick: () => this.leave() },
      ],
    });
  }

  private endTrip(): void {
    this.phase = 'busy';
    modal(this, {
      title: this.castsLeft <= 0 ? '물때가 끝났다' : '오늘은 여기까지',
      body: `오늘 조과 ${this.caught}마리\n쿨러 ${S.cooler.length}/${coolerCapacity()}`,
      buttons: [{ label: '마을로', color: C.blue, onClick: () => this.leave() }],
    });
  }

  private leave(): void {
    if (this.castsDone > 0) useSlot();
    goTo(this, 'Town');
  }

  update(_t: number, delta: number): void {
    const dt = Math.min(0.05, delta / 1000);
    switch (this.phase) {
      case 'power': {
        this.gaugePos += this.gaugeDir * 1.25 * dt;
        if (this.gaugePos >= 1) {
          this.gaugePos = 1;
          this.gaugeDir = -1;
        } else if (this.gaugePos <= 0) {
          this.gaugePos = 0;
          this.gaugeDir = 1;
        }
        this.drawPowerGauge();
        break;
      }
      case 'wait': {
        this.bobT += dt;
        this.bobber.setY(this.target.y + Math.sin(this.bobT * 3) * 2);
        this.nibbleTimer -= dt;
        if (this.nibbleTimer <= 0) {
          this.nibbleTimer = Phaser.Math.FloatBetween(0.7, 1.6);
          this.bobber.setY(this.target.y + 5 * this.bobber.scale);
          const t = txt(this, this.target.x + 18, this.target.y - 24, '톡', { size: 'small', color: T.paper, stroke: '#1b2a41' });
          this.tweens.add({ targets: t, alpha: 0, y: t.y - 14, duration: 500, onComplete: () => t.destroy() });
        }
        this.waitTimer -= dt;
        if (this.waitTimer <= 0) this.startBite();
        break;
      }
      case 'bite': {
        this.biteElapsed += dt;
        this.bobber.setY(this.target.y + 10 * this.bobber.scale + Math.sin(this.biteElapsed * 30) * 2);
        if (this.biteElapsed > this.biteTimer) {
          this.msg.setColor(T.paper);
          this.bobber.setAlpha(1);
          if (this.tutorial) {
            this.say('놓쳤다... 다시 기다려 보자', '찌가 들어가면 바로 누르기');
            this.phase = 'wait';
            this.bobber.setY(this.target.y);
            this.waitTimer = 2;
          } else {
            this.phase = 'busy';
            this.bobber.setVisible(false);
            this.say('미끼만 뺏겼다', '챔질이 늦었다');
            this.finishCast();
          }
        }
        break;
      }
      case 'reel':
        this.holding = this.holding && this.input.activePointer.isDown;
        this.updateReel(dt);
        break;
      default:
        break;
    }
  }
}

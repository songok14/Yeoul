// 간 맞추기 게이지. 바늘이 왕복하고 눌러서 멈춘다

import Phaser from 'phaser';
import { C, T } from '../config';
import { clamp } from '../utils';
import { txt } from './ui';

export interface SeasonGaugeOpts {
  target: number;
  zoneHalf: number;
  /** 미각이 돌아온 맛이면 정답 구간이 보인다 */
  visible: boolean;
  speed: number;
  label: string;
}

export class SeasonGauge extends Phaser.GameObjects.Container {
  private needle: Phaser.GameObjects.Graphics;
  private pos = 0;
  private dir = 1;
  private running = true;
  private zoneG: Phaser.GameObjects.Graphics;
  private hidden?: Phaser.GameObjects.Text;

  constructor(scene: Phaser.Scene, x: number, y: number, private gw: number, private o: SeasonGaugeOpts) {
    super(scene, x, y);
    const w = gw;
    const h = 30;
    const g = scene.add.graphics();
    g.fillStyle(C.ink, 1);
    g.fillRect(-w / 2 - 3, -h / 2 - 3, w + 6, h + 6);
    g.fillStyle(0x3a3f47, 1);
    g.fillRect(-w / 2, -h / 2, w, h);
    // 눈금
    g.fillStyle(0x5c6269, 1);
    for (let i = 1; i < 10; i++) g.fillRect(-w / 2 + (w * i) / 10 - 1, -h / 2, 2, 6);
    this.add(g);

    this.zoneG = scene.add.graphics();
    this.add(this.zoneG);
    if (o.visible) {
      const zx = -w / 2 + (o.target - o.zoneHalf) * w;
      this.zoneG.fillStyle(C.green, 0.9);
      this.zoneG.fillRect(zx, -h / 2, o.zoneHalf * 2 * w, h);
      this.zoneG.fillStyle(C.white, 0.35);
      this.zoneG.fillRect(zx, -h / 2, o.zoneHalf * 2 * w, 4);
    } else {
      this.hidden = txt(scene, 0, 0, '? ? ? ? ? ? ?', { size: 'h1', color: '#8d939b', origin: [0.5, 0.5] });
      this.add(this.hidden);
    }

    const label = txt(scene, -w / 2, -h / 2 - 40, o.label, { size: 'h1', color: T.paper });
    this.add(label);
    const left = txt(scene, -w / 2, h / 2 + 6, '적게', { size: 'small', color: T.gray });
    const right = txt(scene, w / 2, h / 2 + 6, '많이', { size: 'small', color: T.gray, origin: [1, 0] });
    this.add([left, right]);

    this.needle = scene.add.graphics();
    this.add(this.needle);
    this.drawNeedle();
    scene.add.existing(this);
  }

  private drawNeedle(): void {
    const g = this.needle;
    const x = -this.gw / 2 + this.pos * this.gw;
    g.clear();
    g.fillStyle(C.ink, 1);
    g.fillRect(x - 4, -22, 8, 44);
    g.fillStyle(C.gold, 1);
    g.fillRect(x - 2, -20, 4, 40);
    g.fillTriangle(x - 8, -26, x + 8, -26, x, -16);
  }

  tick(dt: number): void {
    if (!this.running) return;
    this.pos += this.dir * this.o.speed * dt;
    if (this.pos >= 1) {
      this.pos = 1;
      this.dir = -1;
    } else if (this.pos <= 0) {
      this.pos = 0;
      this.dir = 1;
    }
    this.drawNeedle();
  }

  /** 멈추고 정확도(0~1)와 방향(-1 모자람, 1 지나침)을 돌려준다 */
  stop(): { quality: number; side: -1 | 0 | 1; pos: number } {
    this.running = false;
    const d = Math.abs(this.pos - this.o.target);
    const half = this.o.zoneHalf;
    const quality = d <= half ? 1 - 0.2 * (d / half) : clamp(0.8 - (d - half) / (half * 2.5), 0, 0.8);
    const side: -1 | 0 | 1 = d <= half ? 0 : this.pos < this.o.target ? -1 : 1;
    return { quality, side, pos: this.pos };
  }

  /** 숨겨져 있던 정답 구간을 드러낸다 (프롤로그 연출용) */
  reveal(): void {
    const half = this.o.zoneHalf;
    const zx = -this.gw / 2 + (this.o.target - half) * this.gw;
    this.zoneG.fillStyle(C.green, 0.9);
    this.zoneG.fillRect(zx, -15, half * 2 * this.gw, 30);
    this.hidden?.setVisible(false);
  }

  get isRunning(): boolean {
    return this.running;
  }
}

// 화면 위 상태 막대: 날짜, 시간대, 소지금, 남은 빚

import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { S } from '../state/save';
import { daysLeft, SLOT_NAME } from '../systems/time';
import { won } from '../utils';
import { txt } from './ui';

export const HUD_H = 58;

export class Hud {
  private g: Phaser.GameObjects.Graphics;
  private dayText: Phaser.GameObjects.Text;
  private dText: Phaser.GameObjects.Text;
  private moneyText: Phaser.GameObjects.Text;
  private debtText: Phaser.GameObjects.Text;
  private pips: Phaser.GameObjects.Text[] = [];

  constructor(private scene: Phaser.Scene) {
    this.g = scene.add.graphics().setDepth(900);
    this.g.fillStyle(C.ink, 0.92);
    this.g.fillRect(0, 0, GAME_W, HUD_H);
    this.g.fillStyle(C.gold, 1);
    this.g.fillRect(0, HUD_H - 3, GAME_W, 3);

    this.dayText = txt(scene, 12, 6, '', { size: 'h1', color: T.paper }).setDepth(901);
    this.dText = txt(scene, GAME_W - 12, 6, '', { size: 'h1', color: T.red, origin: [1, 0] }).setDepth(901);
    this.moneyText = txt(scene, 12, 32, '', { size: 'small', color: T.gold }).setDepth(901);
    this.debtText = txt(scene, GAME_W - 12, 32, '', { size: 'small', color: T.gray, origin: [1, 0] }).setDepth(901);
    let px = 126;
    SLOT_NAME.forEach((name) => {
      const p = txt(scene, px, 9, name, { size: 'small', color: T.gray }).setDepth(901);
      this.pips.push(p);
      px += p.width + 12;
    });
    this.refresh();
  }

  refresh(): void {
    this.dayText.setText(`${S.day}일차`);
    this.pips.forEach((p, i) => {
      p.setColor(i === S.slot ? T.gold : i < S.slot ? '#5c6269' : T.gray);
    });
    const left = daysLeft();
    this.dText.setText(S.debt <= 0 ? '상환 완료' : left > 0 ? `D-${left}` : left === 0 ? 'D-DAY' : '기한 지남');
    this.dText.setColor(S.debt <= 0 ? T.green : T.red);
    this.moneyText.setText(`소지금 ${won(S.money)}`);
    this.debtText.setText(S.debt > 0 ? `남은 빚 ${won(S.debt)}` : '1차 상환 끝');
  }

  destroy(): void {
    this.g.destroy();
    this.scene.children.remove(this.g);
  }
}

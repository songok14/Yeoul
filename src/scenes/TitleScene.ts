import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { newGame } from '../state/actions';
import { hasSave, loadSave } from '../state/save';
import { addSparkles, drawSeascape } from '../ui/art';
import { Button, fadeIn, goTo, modal, txt } from '../ui/ui';

export class TitleScene extends Phaser.Scene {
  constructor() {
    super('Title');
  }

  create(): void {
    fadeIn(this);
    drawSeascape(this, 0, 0, 430, 800);
    addSparkles(this, 0, 440, GAME_W, 340, 30);

    // 찌 하나가 둥둥
    const bob = this.add.container(225, 560);
    const g = this.add.graphics();
    g.fillStyle(C.ink, 1);
    g.fillRect(-5, -22, 10, 26);
    g.fillStyle(C.red, 1);
    g.fillRect(-3, -20, 6, 12);
    g.fillStyle(C.white, 1);
    g.fillRect(-3, -8, 6, 10);
    g.fillStyle(C.foam, 0.6);
    g.fillRect(-16, 4, 32, 3);
    bob.add(g);
    this.tweens.add({ targets: bob, y: 566, duration: 1100, yoyo: true, repeat: -1, ease: 'Sine.InOut' });

    txt(this, GAME_W / 2, 150, '한 접시의 바다', { size: 'title', color: T.paper, origin: [0.5, 0.5], stroke: '#1b2a41' });
    txt(this, GAME_W / 2, 210, '나는 맛을 잃었다.\n다시 맛을 느낄 수 있는 건\n내 손으로 낚은 물고기뿐이다.', {
      size: 'small', color: '#e8dcc0', origin: [0.5, 0], align: 'center', lineSpacing: 8,
    });

    const saved = hasSave();
    new Button(this, GAME_W / 2, 650, '새로 시작', () => {
      if (saved) {
        modal(this, {
          title: '새로 시작할까요?',
          body: '지금까지의 기록이 지워져요.',
          buttons: [
            { label: '취소', color: C.grayDark },
            { label: '새로 시작', color: C.red, onClick: () => this.startNew() },
          ],
        });
      } else this.startNew();
    }, { w: 240, h: 56, color: C.red });

    new Button(this, GAME_W / 2, 722, '이어하기', () => {
      if (loadSave()) goTo(this, 'Town');
    }, { w: 240, h: 56, color: C.blue, disabled: !saved });

    txt(this, GAME_W / 2, 782, 'v0.1 MVP · 1장 물비늘포', { size: 'small', color: '#8d939b', origin: [0.5, 0.5] });
  }

  private startNew(): void {
    newGame();
    goTo(this, 'Prologue', { part: 1 });
  }
}

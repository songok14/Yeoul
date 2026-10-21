// 스타일 시안 페이지: lab.html?style=1|2|3
import Phaser from 'phaser';
import { loadFonts } from '../fonts';
import { CasualScene, loadCasualFonts } from './style1';
import { PixelScene } from './style2';
import { WatercolorScene, loadWatercolorFonts } from './style3';

async function start(): Promise<void> {
  const style = new URLSearchParams(location.search).get('style') ?? '1';
  const pixel = style === '2';
  if (style === '1') await loadCasualFonts();
  else if (style === '3') await loadWatercolorFonts();
  else await loadFonts();

  const game = new Phaser.Game({
    type: Phaser.AUTO,
    parent: 'game',
    // 1·3번은 두 배 해상도로 그리고, 2번은 225×400 도트를 두 배로 키운다
    width: pixel ? 450 : 900,
    height: pixel ? 800 : 1600,
    backgroundColor: '#0f1a2b',
    pixelArt: pixel,
    antialias: !pixel,
    scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH },
    scene: [style === '2' ? PixelScene : style === '3' ? WatercolorScene : CasualScene],
  });
  (window as unknown as { __game: Phaser.Game }).__game = game;
}

void start();

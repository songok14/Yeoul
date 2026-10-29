import '@fontsource/jua/400.css';
import './fonts.css';
import { FONT, FONT_BOLD } from './config';

const SAMPLE = '여울호 짠맛이 돌아온 봄 물비늘포 할머니댁 위판장 0123456789,원';

/** Phaser 텍스트가 대체 글꼴로 그려지지 않도록 게임 시작 전에 글꼴을 불러온다 */
export async function loadFonts(): Promise<void> {
  try {
    await Promise.all([document.fonts.load(`400 22px '${FONT}'`, SAMPLE), document.fonts.load(`400 22px '${FONT_BOLD}'`, SAMPLE)]);
  } catch (err) {
    console.warn('글꼴을 불러오지 못했습니다', err);
  }
}

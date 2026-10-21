// 1장 결말 판정: 빚을 다 갚았는지, 기한이 지났는지

import { CH1_DEADLINE } from '../config';
import { S } from '../state/save';

/** 마을에 들어올 때 확인한다. 클리어면 조황일지 1장을 준다 */
export function chapterOutcome(): 'clear' | 'fail' | null {
  if (S.debt <= 0 && !S.flags.ch1Clear) {
    S.flags.ch1Clear = true;
    S.journalPages = Math.max(1, S.journalPages);
    return 'clear';
  }
  if (S.day > CH1_DEADLINE && S.debt > 0 && !S.flags.ch1Clear) return 'fail';
  return null;
}

// 장비 단계 데이터. 0단계는 시작 장비

export interface Rod {
  name: string;
  desc: string;
  price: number;
  /** 캐스팅 파워가 실제 거리로 이어지는 비율 */
  reach: number;
  /** 물고기 저항(꾹꾹 당김) 흡수율 */
  absorb: number;
  /** 챔질 허용 시간 추가(ms) */
  hookBonus: number;
}

export interface Reel {
  name: string;
  desc: string;
  price: number;
  /** 초당 감는 거리(m) */
  speed: number;
  /** 감을 때 텐션 상승 배율 */
  tensionMul: number;
  /** 이 텐션을 넘으면 드랙이 풀리며 상승이 느려진다 */
  drag: number;
}

export interface Tackle {
  name: string;
  desc: string;
  price: number;
  capacity: number;
  /** 하루에 줄어드는 신선도 */
  decay: number;
}

export const RODS: Rod[] = [
  { name: '다이소 낚싯대', desc: '할머니가 쓰던 이천 원짜리', price: 0, reach: 0.8, absorb: 0, hookBonus: 0 },
  { name: '갯바위 찌낚싯대 1호', desc: '먼 물까지 닿는다. 저항 흡수', price: 80_000, reach: 1.0, absorb: 0.25, hookBonus: 80 },
  { name: '노씨의 감성돔대', desc: '노씨 애장품. 저항 크게 흡수', price: 300_000, reach: 1.0, absorb: 0.45, hookBonus: 160 },
];

export const REELS: Reel[] = [
  { name: '녹슨 스피닝릴', desc: '감을 때마다 끼익 소리가 난다', price: 0, speed: 5, tensionMul: 1.0, drag: 101 },
  { name: '2500번 스피닝릴', desc: '빨리 감긴다. 드랙 텐션 90', price: 60_000, speed: 7, tensionMul: 0.9, drag: 90 },
  { name: '레버브레이크릴', desc: '더 빨리 감긴다. 드랙 85', price: 250_000, speed: 9, tensionMul: 0.8, drag: 85 },
];

export const TACKLES: Tackle[] = [
  { name: '양동이', desc: '8마리. 금방 상한다', price: 0, capacity: 8, decay: 40 },
  { name: '아이스 쿨러', desc: '15마리. 천천히 상한다', price: 50_000, capacity: 15, decay: 25 },
  { name: '대형 아이스박스', desc: '25마리. 얼음 가득', price: 180_000, capacity: 25, decay: 15 },
];

export const BAIT = { name: '크릴 미끼', desc: '한 봉지에 10번', price: 3_000, amount: 10 };

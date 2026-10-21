// 수채화 캐릭터: 한여울, 고양이 고등어, 1톤 트럭 여울호
// 크게 그려 두고 마을에서는 줄여서 쓴다

import { text, type Ctx } from './paint';
import { blob, ink, INK, rand, SANS, seed, SERIF, wash, wrect } from './watercolor';

export const YEOUL_SIZE = { w: 120, h: 180 };
export const CAT_SIZE = { w: 100, h: 90 };
export const TRUCK_SIZE = { w: 320, h: 180 };

/** 임의의 점들로 닫힌 경로 */
function poly(pts: [number, number][]): Path2D {
  const p = new Path2D();
  p.moveTo(pts[0][0], pts[0][1]);
  for (const [x, y] of pts.slice(1)) p.lineTo(x, y);
  p.closePath();
  return p;
}

function strokePath(ctx: Ctx, p: Path2D, w = 1.4, alpha = 0.85, color = INK): void {
  ctx.save();
  ctx.strokeStyle = color;
  ctx.globalAlpha = alpha;
  ctx.lineWidth = w;
  ctx.lineJoin = 'round';
  ctx.lineCap = 'round';
  ctx.stroke(p);
  ctx.restore();
}

function softShadow(ctx: Ctx, cx: number, cy: number, rx: number, ry: number, alpha = 0.28): void {
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.filter = 'blur(3px)';
  ctx.fillStyle = '#6A5444';
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function dot(ctx: Ctx, x: number, y: number, rad: number, color: string, alpha = 1): void {
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.arc(x, y, rad, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

// ───────── 한여울 ─────────
// 노란 비옷 아래로 셰프복 깃이 보인다. 한 손엔 다이소 낚싯대, 한 손엔 볼락이 든 양동이.
// 볼에는 주방 화재 때 생긴 작은 밴드.
export function drawYeoulWC(ctx: Ctx): void {
  seed(101);
  softShadow(ctx, 60, 172, 32, 5);

  // 낚싯대와 줄, 찌
  ink(ctx, [[34, 120], [8, 12]], 2, '#3A3633', 0.9);
  ink(ctx, [[8, 12], [12, 46]], 0.6, '#6A6A6A', 0.6);
  wash(ctx, wrect(9, 44, 6, 10, 0.3), '#C8434A', 0.85, 0.5, false, true);
  dot(ctx, 12, 52, 2.2, '#FBF6EA');
  dot(ctx, 33, 112, 3, '#2E2B28', 0.9);

  // 비옷 후드 (목 뒤)
  const hood = blob(60, 66, 26, 9, 0.08, 12);
  wash(ctx, hood, '#C99A2E', 0.75, 0.8, false, true);

  // 다리와 장화
  wash(ctx, wrect(44, 128, 14, 24, 0.5), '#3F5E8C', 0.8, 0.8, true, true);
  wash(ctx, wrect(62, 128, 14, 24, 0.5), '#3F5E8C', 0.8, 0.8, true, true);
  const boot = (x: number) => {
    const p = new Path2D();
    p.moveTo(x, 148);
    p.lineTo(x + 15, 148);
    p.lineTo(x + 16, 166);
    p.quadraticCurveTo(x + 16, 172, x + 8, 172);
    p.lineTo(x - 4, 172);
    p.quadraticCurveTo(x - 6, 166, x, 164);
    p.closePath();
    wash(ctx, p, '#4E6B5A', 0.85, 0.8, true, true);
    strokePath(ctx, p, 1.3);
    ink(ctx, [[x + 2, 152], [x + 2, 162]], 1, '#FBF6EA', 0.6);
  };
  boot(43);
  boot(62);

  // 비옷 몸통 (앞이 열려 셰프복이 보인다)
  const coatL = poly([[38, 68], [56, 66], [54, 138], [28, 134]]);
  const coatR = poly([[64, 66], [82, 68], [92, 134], [66, 138]]);
  const jacket = poly([[52, 64], [68, 64], [68, 132], [52, 132]]);
  wash(ctx, jacket, '#E9E4DA', 0.5, 0.6, true, true);
  wash(ctx, wrect(52, 64, 7, 68, 0.3), '#CFC8BA', 0.35, 1, false);
  for (let y = 80; y <= 118; y += 12) {
    dot(ctx, 56, y, 1.5, '#9A8E7C');
    dot(ctx, 64, y, 1.5, '#9A8E7C');
  }
  strokePath(ctx, jacket, 1);
  for (const c of [coatL, coatR]) {
    wash(ctx, c, '#E3B23C', 0.82, 0.9, true, true);
    strokePath(ctx, c, 1.4);
  }
  // 비옷 그림자와 주름
  wash(ctx, poly([[70, 70], [82, 68], [92, 134], [80, 136]]), '#B8862A', 0.4, 1.5, false);
  ink(ctx, [[44, 96], [42, 126]], 0.8, '#8A6420', 0.6);
  ink(ctx, [[76, 98], [80, 128]], 0.8, '#8A6420', 0.6);
  // 단추 대신 토글
  for (const y of [86, 104]) {
    wash(ctx, wrect(48, y, 6, 3, 0.2), '#7A5A3A', 0.9, 0.3, false);
    wash(ctx, wrect(66, y, 6, 3, 0.2), '#7A5A3A', 0.9, 0.3, false);
  }

  // 소매와 손
  const sleeveL = poly([[38, 68], [46, 74], [38, 114], [27, 112]]);
  const sleeveR = poly([[82, 68], [74, 74], [84, 110], [94, 106]]);
  for (const s of [sleeveL, sleeveR]) {
    wash(ctx, s, '#E3B23C', 0.82, 0.9, true, true);
    strokePath(ctx, s, 1.3);
  }
  wash(ctx, wrect(26, 108, 13, 5, 0.3), '#C99A2E', 0.9, 0.5, false);
  wash(ctx, wrect(83, 103, 12, 5, 0.3), '#C99A2E', 0.9, 0.5, false);
  wash(ctx, blob(33, 118, 6, 6, 0.08, 10), '#F3CDB0', 0.85, 0.6, true, true);
  wash(ctx, blob(89, 113, 6, 6, 0.08, 10), '#F3CDB0', 0.85, 0.6, true, true);

  // 양동이와 볼락 꼬리
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 1.1;
  ctx.beginPath();
  ctx.arc(98, 128, 11, Math.PI * 1.1, Math.PI * 1.95);
  ctx.stroke();
  ctx.restore();
  const bucket = poly([[86, 126], [110, 126], [107, 150], [89, 150]]);
  wash(ctx, bucket, '#7C9CB4', 0.75, 0.8, true, true);
  wash(ctx, wrect(100, 127, 8, 22, 0.3), '#55738A', 0.35, 1, false);
  strokePath(ctx, bucket, 1.2);
  const fish = new Path2D();
  fish.ellipse(98, 122, 9, 5.5, -0.5, 0, Math.PI * 2);
  wash(ctx, fish, '#6E5A4F', 0.9, 0.5, false, true);
  wash(ctx, blob(96, 124, 5, 2.5, 0.1, 8), '#B9A48C', 0.7, 0.4, false);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 0.9;
  ctx.stroke(fish);
  ctx.restore();
  dot(ctx, 103, 118, 2.4, '#FBF6EA');
  dot(ctx, 103.4, 118.3, 1.2, '#1E1A18');
  ink(ctx, [[91, 119], [94, 113], [97, 116], [100, 112]], 0.9, INK, 0.7);
  wash(ctx, wrect(86, 125, 24, 3, 0.3), '#55738A', 0.6, 0.4, false);

  // 목
  wash(ctx, wrect(54, 54, 12, 12, 0.3), '#E8BFA0', 0.85, 0.5, false, true);
  // 머리: 뒤로 묶은 쪽머리
  wash(ctx, blob(78, 20, 9, 9, 0.1, 10), '#3E2C24', 0.9, 0.6, true, true);
  // 얼굴
  const face = new Path2D();
  face.ellipse(60, 40, 18, 21, 0, 0, Math.PI * 2);
  wash(ctx, face, '#F3CDB0', 0.8, 0.6, true, true);
  // 머리카락: 앞머리는 옆으로 넘겼다
  const hair = new Path2D();
  hair.moveTo(40, 50);
  hair.quadraticCurveTo(36, 24, 52, 16);
  hair.quadraticCurveTo(66, 10, 76, 20);
  hair.quadraticCurveTo(82, 30, 80, 50);
  hair.lineTo(76, 48);
  hair.quadraticCurveTo(76, 34, 70, 28);
  hair.quadraticCurveTo(58, 34, 46, 30);
  hair.quadraticCurveTo(44, 40, 45, 50);
  hair.closePath();
  wash(ctx, hair, '#3E2C24', 0.92, 0.7, true, true);
  ink(ctx, [[52, 20], [60, 17], [68, 19]], 1.2, '#7A6050', 0.5);
  strokePath(ctx, face, 1.1, 0.7);
  // 눈썹, 눈, 코, 입: 조금 지친 얼굴
  ink(ctx, [[48, 35], [55, 34]], 1, INK, 0.8);
  ink(ctx, [[65, 34], [72, 35]], 1, INK, 0.8);
  for (const x of [52, 68]) {
    ctx.save();
    ctx.fillStyle = '#2E2420';
    ctx.beginPath();
    ctx.ellipse(x, 42, 2.1, 2.7, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
    ink(ctx, [[x - 3.5, 39.5], [x + 3.5, 39.5]], 1.1, INK, 0.75);
    dot(ctx, x + 0.7, 41, 0.7, '#FFFFFF');
  }
  ink(ctx, [[60, 45], [59, 49], [61, 49]], 0.8, '#B07A60', 0.8);
  ink(ctx, [[57, 54], [60, 55], [63, 54]], 0.9, '#9A5A4A', 0.85);
  wash(ctx, blob(47, 50, 4, 2.5, 0.1, 8), '#E58A80', 0.4, 1.2, false);
  wash(ctx, blob(73, 50, 4, 2.5, 0.1, 8), '#E58A80', 0.4, 1.2, false);
  // 볼의 밴드
  ctx.save();
  ctx.translate(74, 45);
  ctx.rotate(-0.5);
  ctx.globalAlpha = 0.9;
  ctx.fillStyle = '#F2DDBE';
  ctx.fillRect(-5, -2, 10, 4);
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.5;
  ctx.lineWidth = 0.6;
  ctx.strokeRect(-5, -2, 10, 4);
  ctx.restore();
  // 셰프복 깃
  wash(ctx, poly([[50, 64], [60, 70], [70, 64], [70, 70], [60, 74], [50, 70]]), '#FBF8F0', 0.6, 0.5, false, true);
  ink(ctx, [[50, 65], [60, 71], [70, 65]], 1, INK, 0.75);
}

// ───────── 고등어 ─────────
// 고등어무늬 길고양이. 왼쪽 귀 끝이 살짝 찢겼고, 목에 'D.Y.'가 새겨진 낡은 찌를 달았다.
export function drawCatWC(ctx: Ctx, withTag = false): void {
  seed(202);
  softShadow(ctx, 54, 84, 32, 4);

  // 꼬리가 앞발을 감싼다
  // 몸통
  const body = new Path2D();
  body.moveTo(36, 44);
  body.quadraticCurveTo(26, 70, 32, 82);
  body.lineTo(76, 82);
  body.quadraticCurveTo(84, 60, 66, 42);
  body.closePath();
  wash(ctx, body, '#9A938B', 0.8, 0.8, true, true);
  wash(ctx, poly([[64, 46], [78, 64], [76, 82], [66, 82]]), '#6E6862', 0.35, 1.5, false);
  ctx.save();
  ctx.clip(body);
  for (let i = 0; i < 6; i++) {
    const x = 52 + i * 5;
    ink(ctx, [[x, 48 + i], [x + 3, 62], [x + 1, 78]], 2.2, '#4E4A45', 0.55);
  }
  ctx.restore();
  strokePath(ctx, body, 1.3);
  // 꼬리가 앞발을 감싼다
  const tail = new Path2D();
  tail.moveTo(74, 80);
  tail.quadraticCurveTo(80, 88, 58, 87);
  tail.quadraticCurveTo(38, 87, 28, 79);
  ctx.save();
  ctx.lineCap = 'round';
  ctx.strokeStyle = '#2E2A28';
  ctx.globalAlpha = 0.85;
  ctx.lineWidth = 8.6;
  ctx.stroke(tail);
  ctx.strokeStyle = '#9A938B';
  ctx.globalAlpha = 1;
  ctx.lineWidth = 6.4;
  ctx.stroke(tail);
  ctx.strokeStyle = '#4E4A45';
  ctx.setLineDash([2.5, 4.5]);
  ctx.globalAlpha = 0.65;
  ctx.stroke(tail);
  ctx.restore();
  // 흰 가슴털과 앞발
  wash(ctx, blob(46, 62, 10, 16, 0.12, 12), '#FBF8F2', 0.9, 0.6, false, true);
  for (const x of [40, 53]) {
    const paw = new Path2D();
    paw.ellipse(x, 81, 6, 4, 0, 0, Math.PI * 2);
    wash(ctx, paw, '#FBF8F2', 0.9, 0.4, false, true);
    strokePath(ctx, paw, 1);
  }

  // 머리와 귀
  const earL = poly([[30, 26], [28, 6], [36, 10], [42, 18]]);
  const earR = poly([[52, 18], [60, 6], [64, 26]]);
  for (const e of [earL, earR]) {
    wash(ctx, e, '#9A938B', 0.85, 0.5, true, true);
    strokePath(ctx, e, 1.2);
  }
  // 찢긴 귀 끝
  wash(ctx, poly([[30, 8], [34, 9], [31, 12]]), '#F7F0E2', 1, 0.2, false, true);
  wash(ctx, poly([[32, 22], [31, 12], [38, 18]]), '#D9A0A0', 0.6, 0.5, false);
  wash(ctx, poly([[55, 20], [59, 11], [61, 22]]), '#D9A0A0', 0.6, 0.5, false);
  const head = new Path2D();
  head.ellipse(46, 31, 19, 16, 0, 0, Math.PI * 2);
  wash(ctx, head, '#9A938B', 0.85, 0.6, true, true);
  wash(ctx, blob(46, 37, 9, 6, 0.1, 10), '#FBF8F2', 0.8, 0.6, false, true);
  // 이마의 M 무늬
  ink(ctx, [[40, 20], [42, 25], [46, 21], [50, 25], [52, 20]], 1.3, '#4E4A45', 0.7);
  ink(ctx, [[30, 32], [35, 31]], 1.4, '#4E4A45', 0.6);
  ink(ctx, [[57, 31], [62, 32]], 1.4, '#4E4A45', 0.6);
  strokePath(ctx, head, 1.3);
  // 반쯤 감은 눈 (거만한 표정)
  for (const x of [39, 53]) {
    const eye = new Path2D();
    eye.ellipse(x, 30, 4, 2.4, 0, 0, Math.PI * 2);
    wash(ctx, eye, '#C9C060', 0.9, 0.3, false, true);
    ctx.save();
    ctx.fillStyle = '#2E2420';
    ctx.fillRect(x - 0.6, 28, 1.2, 4);
    ctx.restore();
    ink(ctx, [[x - 5, 28.6], [x + 5, 28.6]], 1.6, INK, 0.9);
  }
  wash(ctx, poly([[44, 36], [48, 36], [46, 39]]), '#E58A8A', 0.95, 0.3, false, true);
  ink(ctx, [[46, 39], [44, 41], [42, 40]], 0.8, INK, 0.8);
  ink(ctx, [[46, 39], [48, 41], [50, 40]], 0.8, INK, 0.8);
  for (const [a, b] of [[[34, 37], [20, 34]], [[34, 39], [20, 40]], [[58, 37], [72, 34]], [[58, 39], [72, 40]]] as [number, number][][]) {
    ink(ctx, [a, b], 0.6, INK, 0.5);
  }

  // 목줄과 낡은 찌
  ink(ctx, [[33, 44], [46, 48], [60, 44]], 1.4, '#A8343A', 0.9);
  ink(ctx, [[46, 48], [46, 51]], 0.8, INK, 0.8);
  const floatTop = new Path2D();
  floatTop.ellipse(46, 54, 3.2, 3.6, 0, Math.PI, 0);
  floatTop.closePath();
  wash(ctx, floatTop, '#C8434A', 0.95, 0.3, false, true);
  const floatBot = new Path2D();
  floatBot.ellipse(46, 54, 3.2, 4.4, 0, 0, Math.PI);
  floatBot.closePath();
  wash(ctx, floatBot, '#FBF6EA', 0.95, 0.3, false, true);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 0.8;
  ctx.beginPath();
  ctx.ellipse(46, 54.5, 3.2, 4, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
  if (withTag) text(ctx, 'D.Y.', 46, 57, `700 3px ${SERIF}`, '#8A2A30', 'center');
}

// ───────── 여울호 ─────────
// 할아버지의 낡은 1톤 트럭. 짐칸을 주방으로 고쳤고 지붕엔 낚싯대를 싣고 다닌다.
export function drawTruckWC(ctx: Ctx, menu = true): void {
  seed(303);
  softShadow(ctx, 165, 167, 152, 7, 0.32);

  // 지붕 위 낚싯대와 아이스박스
  ink(ctx, [[112, 26], [300, 18]], 1.6, '#3A3633', 0.9);
  ink(ctx, [[116, 28], [292, 24]], 1.4, '#7A5A3A', 0.85);
  wash(ctx, wrect(250, 8, 34, 18, 0.4), '#5E8FB5', 0.85, 0.6, true, true);
  wash(ctx, wrect(250, 8, 34, 5, 0.3), '#FBF6EA', 0.9, 0.3, false, true);
  ink(ctx, [[250, 8], [284, 8], [284, 26], [250, 26]], 1, INK, 0.8, true);

  // 아래 차대
  wash(ctx, wrect(22, 132, 280, 14, 0.6), '#4A4440', 0.85, 0.8, false, true);

  // 짐칸 주방
  const box = wrect(104, 30, 202, 110, 0.8);
  wash(ctx, box, '#EFE3C8', 0.6, 0.8, true, true);
  wash(ctx, wrect(280, 30, 26, 110, 0.6), '#C9B49A', 0.4, 2, false);
  wash(ctx, wrect(104, 128, 202, 12, 0.5), '#A0704C', 0.75, 0.6, false, true);
  strokePath(ctx, box, 1.5);
  // 주방 창: 안쪽은 따뜻한 불빛
  const win = wrect(118, 60, 124, 54, 0.6);
  wash(ctx, win, '#7A5440', 0.85, 0.8, false, true);
  wash(ctx, blob(180, 76, 52, 16, 0.2), '#E8B870', 0.45, 6, false);
  strokePath(ctx, win, 1.3);
  // 전구 줄
  for (let i = 0; i < 9; i++) {
    const x = 124 + i * 14;
    const y = 64 + Math.sin((i / 8) * Math.PI) * 4;
    dot(ctx, x, y, 2.2, '#FBE2A6');
    dot(ctx, x, y, 5, '#FBE2A6', 0.25);
  }
  ink(ctx, [[120, 63], [180, 68], [240, 63]], 0.6, INK, 0.6);
  // 냄비와 김
  wash(ctx, wrect(134, 90, 30, 16, 0.4), '#9FA6AE', 0.9, 0.5, true, true);
  ink(ctx, [[130, 90], [168, 90]], 1.2, INK, 0.8);
  wash(ctx, wrect(178, 94, 22, 12, 0.4), '#C8664A', 0.85, 0.5, true, true);
  ink(ctx, [[176, 94], [202, 94]], 1.1, INK, 0.8);
  ctx.save();
  ctx.strokeStyle = '#FBF6EA';
  ctx.lineCap = 'round';
  ctx.lineWidth = 1.6;
  ctx.filter = 'blur(0.6px)';
  for (const x of [143, 153, 189]) {
    ctx.globalAlpha = 0.55;
    ctx.beginPath();
    ctx.moveTo(x, 86);
    ctx.bezierCurveTo(x - 5, 81, x + 4, 77, x - 1, 70);
    ctx.stroke();
  }
  ctx.restore();
  // 국자와 뜰채
  ink(ctx, [[220, 68], [220, 92]], 1, INK, 0.8);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.arc(220, 95, 4, 0, Math.PI);
  ctx.stroke();
  ctx.restore();
  // 판매대
  wash(ctx, wrect(112, 112, 136, 7, 0.4), '#A0704C', 0.9, 0.5, true, true);
  ink(ctx, [[112, 112], [248, 112]], 1.2);

  // 차양 (줄무늬 천, 아래는 물결 모양)
  for (let i = 0; i < 12; i++) {
    const x = 106 + i * 16.5;
    wash(ctx, wrect(x, 38, 16.5, 16, 0.4), i % 2 ? '#F6EEDD' : '#C8664A', 0.85, 0.5, false, true);
    const sc = new Path2D();
    sc.arc(x + 8.25, 54, 8.25, 0, Math.PI);
    sc.closePath();
    wash(ctx, sc, i % 2 ? '#F6EEDD' : '#C8664A', 0.85, 0.4, false, true);
    ctx.save();
    ctx.strokeStyle = INK;
    ctx.globalAlpha = 0.7;
    ctx.lineWidth = 0.9;
    ctx.beginPath();
    ctx.arc(x + 8.25, 54, 8.25, 0, Math.PI);
    ctx.stroke();
    ctx.restore();
  }
  ink(ctx, [[104, 38], [306, 38]], 1.3);
  // 붉은 초롱
  ink(ctx, [[112, 56], [112, 64]], 0.8);
  wash(ctx, blob(112, 70, 5, 6.5, 0.08, 10), '#C8434A', 0.95, 0.4, true, true);
  dot(ctx, 112, 70, 9, '#F4A070', 0.2);

  // 칠판 메뉴
  wash(ctx, wrect(252, 62, 44, 52, 0.4), '#3E4A48', 0.9, 0.6, true, true);
  ink(ctx, [[252, 62], [296, 62], [296, 114], [252, 114]], 1.2, '#7A5A3A', 0.9, true);
  if (menu) {
    text(ctx, '오늘', 274, 71, `400 8px ${SANS}`, '#F4F0E6', 'center');
    text(ctx, '볼락구이', 274, 84, `400 8px ${SANS}`, '#F4F0E6', 'center');
    text(ctx, '고등어조림', 274, 96, `400 7px ${SANS}`, '#F4F0E6', 'center');
    text(ctx, '매운탕', 274, 107, `400 8px ${SANS}`, '#F4D48A', 'center');
  }

  // 손으로 쓴 이름과 닻
  text(ctx, '여울호', 172, 124, `700 15px ${SERIF}`, '#2F4A7A', 'center');
  ctx.save();
  ctx.strokeStyle = '#2F4A7A';
  ctx.globalAlpha = 0.85;
  ctx.lineWidth = 1.2;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(140, 117);
  ctx.lineTo(140, 129);
  ctx.moveTo(136, 119);
  ctx.lineTo(144, 119);
  ctx.moveTo(134, 125);
  ctx.quadraticCurveTo(140, 132, 146, 125);
  ctx.stroke();
  ctx.beginPath();
  for (let x = 196; x < 230; x += 2) ctx.lineTo(x, 125 + Math.sin(x / 3) * 1.5);
  ctx.stroke();
  ctx.restore();

  // 운전석 (1톤 트럭 특유의 납작한 앞모습)
  const cab = poly([[14, 140], [14, 70], [24, 44], [104, 42], [104, 140]]);
  wash(ctx, cab, '#86B4C4', 0.75, 0.8, true, true);
  wash(ctx, poly([[14, 112], [104, 112], [104, 140], [14, 140]]), '#5E8EA2', 0.4, 1.5, false);
  wash(ctx, poly([[24, 46], [104, 44], [104, 50], [22, 52]]), '#FBF6EA', 0.4, 1.5, false);
  strokePath(ctx, cab, 1.5);
  // 창문
  const glass = poly([[30, 52], [94, 52], [94, 86], [24, 86]]);
  wash(ctx, glass, '#D8E8EC', 0.8, 0.6, true, true);
  ink(ctx, [[46, 56], [36, 82]], 2, '#FFFFFF', 0.7);
  ink(ctx, [[56, 56], [50, 70]], 1.2, '#FFFFFF', 0.6);
  strokePath(ctx, glass, 1.2);
  // 문, 손잡이, 거울
  ink(ctx, [[42, 52], [42, 128], [98, 128], [98, 52]], 1, INK, 0.6);
  wash(ctx, wrect(84, 94, 10, 3, 0.2), '#4A4440', 0.9, 0.3, false);
  ink(ctx, [[24, 62], [10, 56]], 1.4);
  wash(ctx, wrect(4, 48, 8, 14, 0.3), '#6A6A6A', 0.85, 0.5, true, true);
  // 전조등과 범퍼
  dot(ctx, 22, 112, 6.5, '#FBF0C8');
  dot(ctx, 22, 112, 11, '#FBE2A6', 0.25);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 1.1;
  ctx.beginPath();
  ctx.arc(22, 112, 6.5, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
  wash(ctx, wrect(16, 98, 6, 4, 0.2), '#E8963A', 0.9, 0.3, false);
  wash(ctx, wrect(6, 128, 40, 12, 0.4), '#A8A39A', 0.85, 0.5, true, true);
  wash(ctx, wrect(12, 131, 22, 6, 0.2), '#FBF6EA', 0.95, 0.2, false, true);
  ink(ctx, [[6, 128], [46, 128], [46, 140], [6, 140]], 1.1, INK, 0.8, true);
  // 녹슨 자국
  for (const [x, y, s] of [[34, 132], [92, 124], [118, 136], [296, 134]].map(([a, b]) => [a, b, 3 + rand() * 3])) {
    wash(ctx, blob(x, y, s, s * 0.7, 0.3, 8), '#B0703A', 0.45, 1, false);
  }

  // 바퀴
  for (const x of [58, 250]) {
    wash(ctx, blob(x, 146, 22, 22, 0.03, 16), '#4A4440', 0.95, 0.5, false, true);
    wash(ctx, blob(x, 146, 18, 18, 0.03, 16), '#33302E', 0.9, 0.6, false);
    wash(ctx, blob(x, 146, 8, 8, 0.03, 12), '#C9C2B4', 0.95, 0.3, false, true);
    ctx.save();
    ctx.strokeStyle = INK;
    ctx.globalAlpha = 0.8;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.arc(x, 146, 8, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
    dot(ctx, x, 146, 2, '#6A6460');
  }
}

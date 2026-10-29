# 🎣 여울호

> 나는 맛을 잃었다. 다시 맛을 느낄 수 있는 건 내 손으로 낚은 물고기뿐이다.

미각을 잃은 셰프 한여울이 고향 남해 물비늘포로 돌아와 직접 낚은 물고기로 맛과 할머니의 식당을 되찾는 **모바일 웹 낚시 경영 게임**입니다.
지금은 1장 물비늘포 MVP(v0.1)입니다. 그래픽은 재미 확인용 임시 도트입니다.

## 플레이 방법

- 폰 브라우저 세로 화면 기준. 손가락 하나로 누르기와 꾹 누르기만 씁니다.
- 목표: **30일 안에 1차 상환금 300만 원**을 갚아 여울식당을 지키기
- 하루는 새벽 · 아침 · 낮 · 저녁 네 칸. 출조와 트럭 장사가 한 칸씩 씁니다.
- 낚시: 바다를 눌러 방향 → 게이지를 멈춰 던지기 → 찌가 쑥 들어가면 챔질 → 누르고 있으면 감기, 텐션이 빨개지면 떼기
- 고양이 고등어가 바라보는 쪽에 입질이 많습니다.
- 낚은 어종이 늘수록 미각이 돌아와 요리의 정답 구간이 보입니다.

## 개발

Node.js 20 이상이 필요합니다.

```bash
npm install
npm run dev        # 개발 서버 (같은 와이파이의 폰에서 http://<PC IP>:5173 접속 가능)
npm run build      # 타입 검사 + 프로덕션 빌드 → dist/
npm run preview    # 빌드 결과 미리보기
npm run shot       # 빌드 후 실행. 주요 화면 17장을 screenshots/에 저장
node scripts/playtest.mjs   # 빌드 후 실행. 새 게임부터 하루를 자동으로 플레이
```

주소 뒤에 `?scene=Town&demo=1` 처럼 붙이면 체험 세이브로 원하는 화면에 바로 갈 수 있습니다.
(`Title`, `Prologue&part=2`, `Town`, `Fishing`, `Fishing&tutorial=1`, `Market&tab=request`, `Truck`, `Shop`, `Notebook&tab=taste` 등)

## 기술

| 항목 | 선택 |
|---|---|
| 엔진 | Phaser 3.90.0 |
| 언어 · 빌드 | TypeScript, Vite |
| 글꼴 | [Galmuri](https://github.com/quiple/galmuri) 2.40.3 (Galmuri11 · Galmuri9, SIL OFL 1.1) |
| 화면 | 450×800 세로, Scale.FIT, 픽셀아트 확대 |
| 세이브 | localStorage (`hanjeopsi-save-v1`) |
| 점검 | Playwright + Chromium 스크린샷 |

## 폴더

```
src/
  main.ts          게임 설정과 씬 등록
  config.ts        화면 크기, 색, 1장 목표 수치
  fonts.ts         Galmuri 글꼴 미리 불러오기
  data/            어종, 장비, 요리, 의뢰, 대사
  state/           세이브 구조(save.ts)와 상태를 바꾸는 행동(actions.ts)
  systems/         시세·신선도, 미각, 시간, 입질 계산
  ui/              버튼·모달, 상단 HUD, 도트 그림, 간 맞추기 게이지
  scenes/          화면별 씬
docs/
  STORY.md         전체 스토리
  GAME_DESIGN.md   시스템과 수치
  ROADMAP.md       단계별 계획
scripts/           스크린샷, 자동 플레이 점검
```

## 무료 배포

`npm run build`로 만든 `dist/` 폴더만 올리면 되는 정적 사이트입니다. Vercel 또는 Cloudflare Pages 무료 플랜에서 GitHub 저장소를 연결하면 됩니다.

- 빌드 명령: `npm run build`
- 출력 폴더: `dist`

## 크레딧

- 글꼴: Galmuri © Lee Minseo (quiple), SIL Open Font License 1.1
- 게임 엔진: Phaser © Phaser Studio, MIT License

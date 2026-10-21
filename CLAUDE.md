# CLAUDE.md · 프로젝트 규칙

모바일 웹 낚시 경영 게임 **한 접시의 바다**. 처음 보는 사람도 이 파일과 `docs/`만 읽고 작업을 이어갈 수 있게 유지한다.

## 언어

- 답변, 문서, 커밋 메시지, 코드 주석, 게임 안 글은 모두 **한국어**로 쓴다.
- 게임 안 대사는 짧고 자연스럽게. 쉼표를 남발하거나 줄표(ㅡ, —)로 꾸민 AI 같은 문장은 쓰지 않는다.
- 식별자(변수, 함수, 파일 이름)는 영어. 어종 id는 로마자 표기(`bollak`, `godeungeo` ...).

## 기술 결정 (확정)

- Phaser **3.90.0** + TypeScript + Vite. 버전은 고정해 둔다.
- 그림체는 **일러스트(애니메이션 채색 + 수채화 배경)**로 확정. 기준 그림은 `docs/ART_PROMPTS.md`.
- 글꼴은 고운돋움(본문, `@fontsource/gowun-dodum`)과 고운바탕 굵게(제목, `@fontsource/gowun-batang`). 모두 OFL. 글자는 `ui/ui.ts`의 `txt()`로만 만든다(해상도 2배로 그린다).
- 화면은 논리 좌표 세로 450×800. 캔버스는 두 배(900×1600)로 만들고 `fadeIn()`에서 카메라를 2배 확대한다. 그래서 씬 좌표는 계속 450×800 기준으로 쓰고, 포인터는 `p.x` 대신 `p.worldX`/`p.worldY`를 쓴다.
- 물고기 그림은 FishingPlan 레포(`songok14/FishingPlan`)의 도감 그림을 가져와 쓰고, 규정도 그 앱 기본표를 따른다. 그 레포에는 쓰지 않는다.
- 아직 그림이 없는 것(손님, 요리)은 `ui/art.ts`의 임시 도트로 그린다(NEAREST 필터).
- 세이브는 localStorage 하나(`SAVE_KEY`). 필드를 추가하면 `createSave()`에 기본값을 넣는다(옛 세이브는 기본값 위에 덮어 불러온다). 구조를 깨는 변경이면 `version`을 올린다.
- **돈이 드는 것은 쓰지 않는다.** 유료 API, 유료 에셋, 유료 호스팅 금지. 배포는 Vercel 또는 Cloudflare Pages 무료 플랜.
- 일러스트는 사용자가 이미지 AI로 뽑아 준다. 원본은 레포에 넣지 않고 `python3 scripts/art.py <원본 폴더>`로 배경 제거·자르기·WebP 변환을 해서 `public/assets/`에 넣는다. 새 그림은 `scripts/art.py`의 목록과 `BootScene.preload()`에 같이 추가한다.
- 새 그림이 필요하면 `docs/ART_PROMPTS.md` 형식(복붙용 코드 블록 하나, 시트형)으로 프롬프트를 준다.

## 구조 규칙

- 수치와 대사는 `src/data/`, 계산은 `src/systems/`, 상태 변경은 `src/state/actions.ts`에 모은다. 씬은 그리기와 입력만 맡는다.
- 상태는 `state/save.ts`의 `S` 하나. 바꾼 뒤에는 `persist()`를 부른다.
- 씬 이동은 `goTo(scene, key, data)`를 쓴다. Phaser는 데이터를 안 넘기면 **이전 실행 데이터를 재사용**하므로 `scene.start`를 직접 쓸 때도 항상 객체를 넘긴다.
- 씬 인스턴스는 재사용된다. 필드 초기값에 기대지 말고 `init()`에서 다시 설정한다.
- 수치를 바꾸면 `docs/GAME_DESIGN.md` 표도 함께 고친다. 할 일이 끝나면 `docs/ROADMAP.md` 체크박스를 갱신한다.

## 작업 후 확인 (필수)

```bash
npm run build                 # 타입 검사 + 빌드가 통과해야 한다
npm run shot                  # screenshots/*.png 를 직접 열어 화면을 눈으로 확인
node scripts/playtest.mjs     # 새 게임부터 하루 자동 플레이. 실패나 페이지 오류가 없어야 한다
```

- 스크린샷은 Playwright와 미리 설치된 Chromium으로 찍는다(`playwright install` 금지).
- 글자 겹침, 화면 밖으로 나간 글자, 버튼 겹침을 특히 본다.
- 새 화면을 만들면 `scripts/screenshot.mjs`에 한 장 추가한다. `?scene=이름&demo=1`로 바로 갈 수 있다.
- `screenshots/`와 `dist/`는 커밋하지 않는다.

## 스토리 기준

- 전체 줄거리는 `docs/STORY.md`. 설정과 다르게 쓰지 않는다.
- 아버지의 말 "고기는 먼저 낚은 사람 게 아니야. 제일 맛있게 먹어주는 사람 거지."는 문장 그대로 쓴다.
- 1장은 "90일 뒤 차압 예정, 30일 안에 1차 상환금 300만 원"으로 정리했다.

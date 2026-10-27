# CLAUDE.md · 프로젝트 규칙

모바일 낚시 경영 게임 **한 접시의 바다**. 처음 보는 사람도 이 파일과 `docs/`만 읽고 작업을 이어갈 수 있게 유지한다.

> **2026-10부터 고닷 4(C#)로 옮기는 중이다.** 계획은 `docs/GODOT_PLAN.md`. 규칙은 `core/`, 고닷 프로젝트는 `godot/`.
> 웹 버전(`src/`, Phaser)은 지우지 않고 기획과 동작의 기준으로 남긴다. 아래 "기술 결정"과 "작업 후 확인"은 웹 버전 규칙이고, 고닷 규칙은 "고닷" 절에 있다.

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

## 고닷 (2026-10~)

- Godot 4.7.2 .NET(C#), Compatibility 렌더러. 안드로이드 먼저(arm64), 세로 1080×1920 기준. 유니티는 라이선스 문제로 접었다(`docs/GODOT_PLAN.md` 머리말).
- 게임 규칙은 `core/Hanjeopsi.Core` (고닷을 모르는 순수 C#, net8.0). 고닷 쪽 코드는 `godot/scripts`. 화면 코드에 계산식을 넣지 않고 코어로 뺀다.
- 데이터는 `godot/data/*.json`. 수치와 글은 코드에 쓰지 않는다.
- 웹과 같은 결과인지 골든 테스트로 확인한다. 웹 수치나 계산을 바꾸면 `npm run golden`으로 `core/golden/`을 다시 만든다.
- 이 환경에서는 고닷을 실행하지 못한다. 고닷 쪽 C#은 `dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true`로 진짜 GodotSharp에 대고 컴파일한다(프로젝트는 안드로이드 템플릿 때문에 net9.0, 여기엔 .NET 8만 있다).
- **APK와 화면 사진은 GitHub Actions(`.github/workflows/android.yml`)가 만든다.** 비밀값은 필요 없다. `core/`나 `godot/`를 푸시하면 돌고, 사용자는 릴리스 `test-latest`에서 받아 폰에서 테스트한다.
- 푸시한 뒤에는 GitHub MCP 도구로 실행 결과를 확인한다. 성공하면 `git fetch origin ci-build`로 `lab.png` 같은 화면 사진과 APK(`cat hanjeopsi.apk.part-* > hanjeopsi.apk`로 합친다)를 받아 **직접 열어 본다**(글자 겹침, 화면 밖 글자, 버튼 겹침). 실패하면 로그를 보고 고쳐서 다시 푸시한다.
- `godot/Hanjeopsi.sln`은 지우지 않는다. 없으면 C#이 빠진 APK가 나와 켜자마자 꺼진다(워크플로가 검사한다).
- 화면은 `Ui/Screen`을 물려받아 `Build()`에서 코드로 만든다. `.tscn`은 뿌리 노드에 스크립트만 붙인다. 색은 `Palette`, 글자는 `UiKit.Text`(크기는 `TextSize` 단계), 버튼은 `GameButton`, 알림은 `Toast`, 확인은 `Modal`. 이동은 `GameState.I.GoTo("town")`.
- 그림 위에 놓는 것(간판, 캐릭터)은 `ArtStage`에 웹 논리 좌표(450×800)로 놓는다. 화면 비율이 달라도 그림에 붙어 있다.
- 새 화면을 만들면 워크플로 `shots=( ... )` 목록에 `이름:--screen=<씬> [--demo=…] [--shot-action=…]`을 한 줄 추가한다. 사진 모드는 체험 판(`GameState.SetupDemo`)으로 시작하고 3초 뒤 찍는다. `town-tall`은 1080×2400 긴 화면이다.
- 그림 역할: **화면 위에서 움직이는 캐릭터는 SD(2~2.5등신)**, **대화와 감정은 일러스트**. 마을·낚시·트럭에서 걷고 반응하는 건 SD와 한 줄 말풍선(`Screen.Bubble`). 누군가와 이야기하는 장면은 대화창(`Screen.Talk`/`Say`/`TalkScene`)에 감정 일러스트(`godot/art/portraits/`)가 들어온다. 감정이 터지는 장면은 한 장짜리 컷신 일러스트. 배경은 수채화 그대로. 새 시트 프롬프트는 배경 #EDEDED, 그림 한 장당 코드 블록 하나(`docs/ART_PROMPTS.md`).
- 소리는 모두 `python3 scripts/sound.py [이름...]`로 합성해 `godot/audio/sfx`, `godot/audio/bgm`에 OGG로 둔다(외부 소리 파일 없음). 효과음은 `Sound.Play("plop")`, 반복은 `Sound.Loop`, 배경음은 화면에서 `Music`을 재정의한다. 진동은 `Haptics.Pulse`/`Pattern`. 켜고 끄기는 `Settings`(user://settings.cfg, 수첩 설정 탭).
- 고닷 낚시는 웹과 다르다(부채꼴 조준, 고등어 지점, 큰 놈, 고등어 도둑, 짧은 수치). 규칙은 코어 `SpotRules`, 수치는 `balance.json` spot·reel. 골든 테스트는 웹 수치(`TestData.WebReel`)로 계산만 비교한다. `docs/GAME_DESIGN.md` 3절.
- 대화 내용은 `godot/data/dialogue.json`: `speakers`(이름, 이름표 색, 서는 쪽, 감정별 그림)와 `scenes`(이름 붙은 대화). 새 감정 그림은 `faces`에 이름을 붙여 넣는다.
- 배경을 지운 일러스트는 `python3 scripts/clean_halo.py <그림들>`로 머리카락 사이 흰 테두리를 정리한다(제자리에서 고친다, 흰 옷은 남긴다). 머리색이 비친 크림빛 테두리가 남으면 머리 둘레에 흰 옷이 없는 그림에만 `--strong=0.45`를 준다(요리복 두 장에 썼다. 후드 그림에 쓰면 후드가 먹힌다). 갈색 머리 둘레에 가는 가닥과 분홍빛 바탕이 남으면 `--fringe`를 준다(흰머리·회색 머리 그림에는 쓰지 않는다). 정리한 뒤 어두운 바탕 위에 올려 눈으로 확인한다.
- **SD 걷기 그림은 모두 오른쪽을 본다. 왼쪽으로 갈 때만 `FlipH = true`.** 시트를 자를 때 `sd_sheet.py ... --face=right`를 주면 얼굴 쪽을 재서 맞춰 준다(이미 자른 그림은 `python3 scripts/sd_sheet.py --face=right <그림들>`). 서기·앉기 같은 다른 포즈는 그림마다 다르니 쓰는 곳에서 확인한다.
- 그림은 PNG로 `godot/art/`에 넣는다(웹은 WebP). SD 시트는 `python3 scripts/sd_sheet.py <시트> <행> <열> <폴더> <이름들>`로 자른다. 원본 시트는 커밋하지 않는다. 배경이 이미 투명한 SD 포즈 한 장(낚싯대 든 여울 등)은 `python3 scripts/sd_pose.py <원본> <이름> --body=<몸 오른쪽 끝 x> [--erase=x,y]`로 줄을 지우고 줄여서 `godot/art/yeoul/`에 넣는다. 발과 낚싯대 끝 위치는 `godot/data/poses.json`에 기록되고 화면은 `SdPoses`로 읽는다(키를 기준으로 크기를 맞춘다). 요리 시트(위 3, 아래 2처럼 줄마다 수가 달라도 된다)는 `python3 scripts/dish_sheet.py <시트> <요리 id들>`로 잘라 `godot/art/dishes/`에 넣는다. 장비 시트(2줄 × 5칸, 10단계)는 `python3 scripts/item_sheet.py <시트> <종류> 192`로 `godot/art/gear/<종류>-<단계>.png`에 넣는다(배경이 투명한 시트도 된다).

```bash
dotnet test core/Hanjeopsi.sln              # 코어 빌드 + 테스트. 모두 통과해야 한다
dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true   # 고닷 쪽 C# 컴파일 (여기엔 .NET 8만 있다)
npm run golden                               # 웹 코드를 고쳤으면 기준값 다시 만들기
```
- 웹 코드를 고치면(계산을 systems로 빼는 리팩터링 포함) 웹 "작업 후 확인"도 함께 돌린다.

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
- 소프트웨어 렌더링이라 작업 환경에서는 초당 7프레임 안팎이다. 두 스크립트 모두 Phaser의 `smoothStep`을 꺼서 게임 시간이 실제 시간을 따라가게 하고, 시간 대신 게임 단계(`phase`)를 보고 누른다. 자동 플레이는 10분쯤 걸린다.
- 글자 겹침, 화면 밖으로 나간 글자, 버튼 겹침을 특히 본다.
- 새 화면을 만들면 `scripts/screenshot.mjs`에 한 장 추가한다. `?scene=이름&demo=1`로 바로 갈 수 있다.
- `screenshots/`와 `dist/`는 커밋하지 않는다.

## 스토리 기준

- 전체 줄거리는 `docs/STORY.md`. 설정과 다르게 쓰지 않는다.
- 아버지의 말 "고기는 먼저 낚은 사람 게 아니야. 제일 맛있게 먹어주는 사람 거지."는 문장 그대로 쓴다.
- 1장은 "90일 뒤 차압 예정, 1차 상환금 300만 원을 4주 동안 매주 나눠 낸다"(고닷, `godot/data/weekly.json`). 웹 버전은 "30일 안에 300만 원" 그대로.

## 세션 마무리

- 세션을 끝낼 때는 `.claude/skills/session-handoff` 스킬로 다음 세션 프롬프트를 `docs/HANDOFF.md`에 남긴다.

# 고닷 이전 계획

웹 버전(Phaser 3.90 + TypeScript, `src/`)은 1장 물비늘포 MVP로 재미 확인을 마쳤다. 이제 모바일 게임으로 제대로 만들기 위해 **고닷 4(C#)**로 옮긴다.
웹 버전은 지우지 않고 **기획과 동작의 기준**으로 남긴다. 고닷에서 같은 상황이면 같은 결과가 나와야 한다.

> 처음에는 유니티로 시작했다(2026-10). 유니티는 무료 플랜도 빌드마다 계정 라이선스가 필요해서 Claude가 혼자 APK를 만들 수 없었다.
> 이 게임은 취미 프로젝트라 수익 기능이 필요 없고, "Claude가 빌드하고 사용자는 폰에서 테스트"하는 흐름이 중요해서 라이선스가 없는 고닷으로 바꿨다.
> 코어 규칙 C#과 골든 테스트는 그대로 가져왔다. 유니티 쪽 코드는 git 기록에만 남아 있다.

## 1. 결정 사항

| 항목 | 결정 |
|---|---|
| 엔진 | **Godot 4.7.2 .NET(C#)**. MIT 라이선스, 계정·비용 없음 |
| 렌더러 | Compatibility(OpenGL ES 3). 2D 게임이라 충분하고 오래된 폰까지 돈다 |
| 목표 기기 | 안드로이드 먼저(arm64). 세로 1080×1920 기준, 화면이 길면 세로로 늘어난다(stretch `canvas_items` + `expand`) |
| 언어 | C#. 코어는 .NET 8, 고닷 프로젝트는 .NET 9 (안드로이드 C# 템플릿 조건) |
| 규칙 코드 | 고닷을 모르는 순수 C# 코어(`core/`). dotnet으로 빌드하고 xUnit으로 테스트 |
| 데이터 | JSON (`godot/data/*.json`). 파싱은 Newtonsoft Json |
| 웹과 같은지 확인 | 웹 코드로 기준값(골든)을 뽑아 C# 테스트가 비교한다 |
| 빌드 | **GitHub Actions**가 공식 고닷 배포판으로 APK를 만든다. 비밀값이 필요 없다. 비공개 레포는 한 달 2,000분 무료. 결과는 릴리스와 `ci-build` 브랜치로만 남긴다(아티팩트 저장 한도 때문) |
| 화면 확인 | CI가 가상 화면(Xvfb)에서 사진을 찍어 `ci-build` 브랜치에 올린다. Claude가 받아서 눈으로 본다 |
| 그림 | 배경은 지금 수채화 일러스트. 인게임 캐릭터·물체는 SD(2~2.5등신). 일러스트는 컷신 전용 |
| 돈 | 유료 에셋, 유료 서비스, 유료 플러그인 금지 |

질을 높이는 데 쓰는 고닷 기본 기능: `Tween`(움직임), `AnimationPlayer`(연출), `Skeleton2D`(SD 부품 뼈대 애니메이션), `GPUParticles2D`/`CPUParticles2D`(물보라, 김), 셰이더(물결, 빛), `AudioStreamPlayer`(효과음). 모두 엔진에 들어 있다.

## 2. 폴더 구조

```
Yeoul/
├─ src/, public/, docs/ ...           웹 버전 (기준. 지우지 않는다)
├─ scripts/
│  ├─ export-golden.mjs               웹 코드로 기준값을 뽑는다 (npm run golden)
│  ├─ golden/*.ts                     기준값 만드는 코드 (market, rules, scenario, reel)
│  └─ sd_sheet.py                     SD 시트(#EDEDED)를 칸마다 잘라 PNG로
├─ core/                              ① 순수 C# 코어
│  ├─ Hanjeopsi.sln
│  ├─ Hanjeopsi.Core/                 규칙 (net8.0)
│  │  ├─ Util/                        JsMath, Mulberry32, IRandom
│  │  ├─ Data/                        Species, Gear, Balance, Texts, Recipe, RequestTemplate, TasteStage, GameData(로더)
│  │  ├─ State/                       FishItem, SaveData, SaveJson
│  │  ├─ Systems/                     Freshness, Market, Rules, Taste, FishingRoll, SeasonJudge, Truck, ReelFight
│  │  └─ Game.cs                      한 판: 세이브 + 규칙 + 행동 (웹 actions.ts, time.ts)
│  ├─ Hanjeopsi.Core.Tests/           xUnit 테스트, 자동 플레이(AutoPlayer)
│  └─ golden/                         웹에서 뽑은 기준값 JSON
├─ godot/                             ② 고닷 프로젝트
│  ├─ project.godot, Hanjeopsi.csproj, export_presets.cfg, icon.png
│  ├─ data/                           species, gear, balance, texts, recipes, requests, taste, dialogue .json
│  ├─ art/                            backgrounds/, yeoul/, cat/ (SD), props/, portraits/ (대화 일러스트) (PNG)
│  ├─ fonts/                          고운돋움(본문), 고운바탕 굵게(제목) (OFL)
│  ├─ scenes/                         boot(첫 장면), title, town, core_lab .tscn (뿌리 노드에 스크립트만 붙인다)
│  ├─ scripts/
│  │  ├─ App/                         GameState(자동 로드: 데이터·판·전환·사진 모드), BootLoader
│  │  ├─ Ui/                          Palette·Fonts·TextSize, UiKit, GameButton, Screen(모달·토스트·말풍선·대화), DialogueBox,
│  │  │                               Hud, ArtStage(배경 좌표계), SignButton
│  │  ├─ Screens/                     TitleScreen, TownScreen
│  │  ├─ Boot/                        GameDataLoader, SaveStore, SystemRandom
│  │  └─ Lab/                         CoreLab (점검 화면, 타이틀 아래 '점검')
│  └─ keystore/ci-test.keystore       테스트 APK 서명 키 (덮어 설치용, 공개 값)
└─ .github/workflows/android.yml      코어 테스트 → 고닷 받기 → 사진 → APK → 릴리스 test-latest, ci-build
```

- 코어는 고닷을 모른다. 고닷 쪽(`godot/scripts`)은 `ProjectReference`로 코어를 쓴다. 계산식이 화면 코드에 들어가면 테스트할 수 없으니 코어로 뺀다.
- 고닷 쪽 C#은 여기서도 `dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true`로 진짜 GodotSharp API에 대고 컴파일된다. 프로젝트 기본은 net9.0이다(고닷 4.7 안드로이드 C# 템플릿 조건).
- `.godot/`, `build/`, `bin/`, `obj/`는 커밋하지 않는다. 에디터가 만드는 `*.import`, `*.uid` 파일은 PC에서 에디터를 열었을 때 커밋해도 된다.

## 3. 웹 구조 규칙을 그대로

- 수치와 대사는 JSON(`src/data` 자리), 계산은 코어 `Systems`(`src/systems` 자리), 상태 변경은 코어 `Game`(`state/actions.ts` 자리).
- 화면 코드는 그리기와 입력만 맡는다.
- 상태는 `Game.Save` 하나. 행동이 끝나면 `Game.Saved` 이벤트로 `SaveStore`가 `user://save.json`에 쓴다.

## 4. 데이터 (JSON)

| 파일 | 웹 원본 | 상태 |
|---|---|---|
| `species.json` | `src/data/fish.ts` (도트용 `look`은 뺐다) | ✅ |
| `gear.json` | `src/data/gear.ts` | ✅ |
| `balance.json` | `config.ts` 상수와 시스템 상수 (시세, 신선도, 시간, 의뢰, 고양이, 입질, 판정, 트럭) | ✅ |
| `texts.json` | 신선도 단계, 위판장 소식, 시간대, 날짜, 규정 문구, 손님 리뷰 | ✅ |
| `recipes.json` | `src/data/recipes.ts` (맛 이름, 도구, 리뷰 포함) | ✅ |
| `requests.json` | `src/data/requests.ts` (노씨 의뢰는 `flag: nossiFavor`) | ✅ |
| `taste.json` | `systems/taste.ts` 미각 단계표 | ✅ |
| `story.json` | `src/data/story.ts` (페이지, 배경 키, 컷신 키, 이벤트) | 다음 |
| `dialogue.json` | 할머니 대사, 노씨 대사, 고등어 말풍선, 토스트 문구 등 씬에 흩어진 글 | 다음 |

- 필드 이름은 웹과 같은 camelCase. 모르는 필드가 있으면 바로 오류를 낸다(오타 방지).
- `species.json`의 순서는 시세 시드에 들어간다. 새 어종은 뒤에 붙인다.
- 어종 id는 FishingPlan 도감 그림 이름(`fish_<id>`)과 같다. 금지체장·금어기는 FishingPlan `core/catches/Limits.kt` 기본표를 따른다.

## 5. 웹과 같은 결과 확인 (골든 테스트)

```
웹 코드 (src/systems/*.ts, state/actions.ts)
   │  npm run golden  (esbuild로 묶어서 node에서 실행)
   ▼
core/golden/market.json   ← 시드 4개 × 45일 × 5종 시세, 물고기 값 300건, 낙찰가, endDay 신선도 감소
core/golden/rules.json    ← 달력·금어기 60일, 놓아주기 이유, 어종·크기 뽑기 600번, 판정 200번, 요리값, 손님 한마디
core/golden/scenario.json ← 새 게임부터 행동 2,800여 단계. 매 단계 세이브 전체와 화면 값
core/golden/reel.json     ← 고등어 힌트 330번, 파워 게이지, 입질 대기 120번, 챔질 판정, 릴링 260판(프레임마다)
   │  dotnet test core/Hanjeopsi.sln
   ▼
C# 코어가 같은 입력으로 계산해서 비교 (테스트 72개, 자동 플레이 포함)
```

- 난수는 웹과 같은 mulberry32와 hash를 C#으로 옮겼다(uint 연산). 시드가 같으면 시세가 소수점 끝자리까지 같다.
- JS `Math.round`(.5는 위로)와 C# `Math.Round`(은행가 반올림)가 달라서 `JsMath.Round`를 쓴다.
- 웹이 `Math.random()`을 쓰는 곳은 C#에서 `IRandom`을 받는다. 골든 스크립트에서 `Math.random`을 mulberry32로 바꿔 끼워 같은 난수 순서로 비교한다.
- 웹에서 씬 안에 있던 계산은 먼저 웹 쪽을 systems로 빼서(동작은 그대로) 골든을 뽑는다. U2에서 `truck.ts`, `season.ts`, `chapter.ts`, `actions.ts`의 `serveDish`·`completePrologue`·`useBait`를 뺐다. U3에서 낚시 미니게임 계산을 `reel.ts`로 뺐다.
- 릴링 골든은 입력(누르고 있는지, 프레임 시간)을 정해진 규칙(똑똑하게 · 아무렇게나 · 계속 누름 · 안 누름, 고정 1/60초 · 들쭉날쭉)으로 만든다. C# 테스트가 같은 규칙으로 같은 입력을 만들어 프레임마다 값을 비교한다. 곱셈 순서 하나만 바뀌어도 테스트가 잡는다.
- 시나리오 골든은 웹이 행동을 고르고 인자까지 남긴다. C#은 같은 인자로 같은 행동을 하고 세이브 JSON을 키 하나하나 비교한다.

## 6. 화면 목록

| 고닷 씬 | 웹 씬 | 내용 |
|---|---|---|
| `boot.tscn` | `BootScene` | 데이터·세이브 로드, 첫 화면으로 (autoload `GameState`) |
| `title.tscn` | `TitleScene` | 새로 시작 / 이어하기 |
| `story.tscn` | `PrologueScene` | 컷신 일러스트 + 타자기 글. 프롤로그 1·2부, 1장 클리어, 차압 결말, 소금 간 맞추기 |
| `town.tscn` | `TownScene` | 마을 허브. 시간대별 배경, SD 여울·고등어·트럭, 6개 메뉴, 식당 |
| `fishing.tscn` | `FishingScene` | 방향 → 파워 → 입질 → 챔질 → 릴링, 결과 |
| `market.tscn` | `MarketScene` | 시세판, 경매 연출, 납품 의뢰 |
| `truck.tscn` | `TruckScene` | SD 손님 입장, 주문, 간 맞추기 게이지, 반응 |
| `shop.tscn` | `ShopScene` | 미끼, 장비 |
| `notebook.tscn` | `NotebookScene` | 쿨러, 도감, 미각, 조황일지 |
| `core_lab.tscn` | (없음) | 점검 화면. 타이틀 아래 작은 '점검'으로 들어간다. 씬을 다 옮기면 뺀다 |

- 공통 UI(HUD, 버튼, 모달, 토스트, 말풍선, 떠오르는 숫자)는 씬으로 만들어 재사용하고, 색과 글꼴은 `Theme` 하나로 모은다.
- 화면 좌표: 웹 논리 450×800 → 고닷 1080×1920 (2.4배).
- 노치·제스처 바는 `DisplayServer.GetDisplaySafeArea()`로 피한다.

## 7. 웹 기능과 고닷 구현 대응표

| 웹 | 고닷 | 어디에 | 상태 |
|---|---|---|---|
| `utils.ts` mulberry32, hash, clamp, round100, won | `Util/Mulberry32`, `Util/JsMath` | core | ✅ |
| `systems/economy.ts` 신선도, 시세, 물고기 값, 등락, 소식 | `Systems/Freshness`, `Systems/Market` | core | ✅ |
| `time.ts` 시간 칸, 날 넘김, 의뢰 갱신 | `Game.UseSlot`, `EndDay`, `RefreshRequests` | core | ✅ |
| `calendar.ts` 날짜, 금지체장, 금어기 | `Systems/Rules` (해를 넘는 금어기도 FishingPlan과 같게) | core | ✅ |
| `chapter.ts` 1장 클리어·차압 | `Game.CheckChapter` | core | ✅ |
| `taste.ts` 미각 단계, 해금 | `Systems/Taste` | core | ✅ |
| `fishing.ts` 거리 구간, 어종·크기, 고등어 힌트 정확도 | `Systems/FishingRoll` | core | ✅ |
| `systems/truck.ts` 재료 고르기, 요리값, 손님 기분과 한마디 | `Systems/Truck` | core | ✅ |
| `systems/season.ts` 간 맞추기 정확도 | `Systems/SeasonJudge` | core | ✅ |
| `state/save.ts`, `state/actions.ts` | `State/SaveData`, `SaveJson`, `Game` (웹 세이브 JSON을 그대로 읽는다) | core | ✅ |
| `systems/reel.ts` 릴링 텐션·거리·저항·지침·드랙, 챔질, 입질 대기·속임 입질, 고등어 힌트, 파워 게이지 | `Systems/ReelFight` (`ReelRules`, `BiteWait`, `ReelFight.Step(holding, dt)`) | core | ✅ |
| localStorage | `SaveStore` → `user://save.json` (임시 파일에 쓰고 바꿔 끼움) | godot | ✅ |
| `ui/ui.ts` txt, Button, modal, toast, bubble, floatText, `ui/hud.ts` | `Ui/UiKit`, `GameButton`, `Screen.Modal/Toast/Bubble/FloatText`, `Hud` (코드로 만든다) | godot | ✅ |
| 고운돋움·고운바탕 | `Fonts.Body`, `Fonts.Title` (TTF 그대로, 동적 글리프) | godot | ✅ |
| `goTo`, `fadeIn` | `GameState.GoTo("town")` 어둡게 덮었다 걷기 | godot | ✅ |
| tween, 카메라 흔들기 | `Tween`, `Camera2D` 오프셋 | godot | U5~ (Tween ✅) |
| `scripts/art.py` | SD 시트는 `scripts/sd_sheet.py`(PNG). 배경은 WebP를 PNG로 | scripts | ✅ |
| `scripts/screenshot.mjs` | CI 사진 모드 (`-- --shot=<경로>`), `ci-build` 브랜치 | godot / CI | ✅ |
| `scripts/playtest.mjs` | 코어 자동 플레이 테스트 `AutoPlayTests` (30일, 시드 20개 × 두 실력) | core | ✅ |

## 8. 그림 적용 방향

- **배경**: `public/assets/bg-*.webp`를 PNG로 바꿔 `godot/art/backgrounds`에 (마을 4장, 방파제, 위판장, 미끼가게 완료. 물고기 그림 5장은 `godot/art/fish/`). 900×1600 원본이라 1.2배 확대된다. 원본 고해상도가 있으면 다시 뽑는다.
- **인게임 SD**: 시트(#EDEDED 배경)를 `scripts/sd_sheet.py`로 칸마다 잘라 PNG로. 크기는 바꾸지 않아 같은 시트 프레임은 같은 비율이다. 여울 8장, 고등어 12장 완료. 낚시 포즈 6장(`fish-ready` 대 세움, `fish-wait` 기다림, `fish-reel` 감기, `fish-cast` 머리 위로 휘두르기, `fish-pull` 옆으로 돌아서 버티기, `fish-joy` 앞을 보고 활짝)은 `scripts/sd_pose.py`로 넣었다. 체크무늬나 밝은 바탕이 박힌 시트에서 뽑은 것(cast, joy)은 가장자리와 이어진 밝은 무채색과 머리 둘레 흰 빛을 지운 뒤 넣었다. 포즈마다 머리 크기가 비슷해 보이도록 `poses.json`의 `body`를 손으로 맞췄다(pull 740, joy 360, cast 520). 낚싯대는 그림에 있고 게임은 그림 속 대 끝에서 줄만 그린다. 던질 때 젖혔다 휘두르기, 챔질 때 확 들기, 텐션·저항에 끌려가기는 포즈 기울기로 한다.
  ```bash
  python3 scripts/sd_sheet.py <시트.png> 2 4 godot/art/yeoul front,three-quarter,side,back,idle,wave,walk-1,walk-2
  ```
- **애니메이션**: 숨쉬기·통통 튀기는 `Tween`, 걷기·낚시 동작은 `AnimatedSprite2D` 프레임, 부품 분리 그림이 오면 `Skeleton2D` + `Polygon2D` 뼈대. 표정은 눈·입 스프라이트 교체.
- **역할 나누기 (2026-10-22 정함)**: 화면 위에서 움직이는 캐릭터는 **SD**, 대화와 감정은 **일러스트**.
  | 상황 | 그림 | 예 |
  |---|---|---|
  | 마을·낚시·트럭에서 걷기, 낚시, 요리, 짧은 반응 | SD + 한 줄 말풍선 | 여울을 누르면 손 흔들기, 고등어 "냐옹" |
  | 누군가와 이야기하는 장면 | 대화창 + 감정 일러스트(상반신이 보이게 크게) | 할머니댁 할머니, 새 게임 첫마디, 1장 클리어·차압 |
  | 감정이 터지는 순간 | 한 장짜리 컷신 일러스트 (9:16, 아래 30%는 글 상자) | 맛을 잃은 날, 처음 짠맛, 아버지 기억 |
- **대화창** `Ui/DialogueBox`: 말하는 사람이 바뀌면 일러스트가 옆에서 미끄러져 들어오고, 감정만 바뀌면 살짝 바꿔 낀다. 이름표 색과 서는 쪽은 `dialogue.json speakers`. 글은 타자기처럼 찍히고 누르면 다음 줄, 마지막 줄에 선택지를 달 수 있다(할머니댁의 갚기·잠자기).
- **일러스트 정리**: 지금 감정 그림은 웹 `public/assets/y-*.webp`, `ch-*.webp`를 PNG로 옮긴 것(`godot/art/portraits/`). 머리카락 사이 흰 배경은 `scripts/clean_halo.py`로 지웠다. 갈색 머리 여울 그림은 `--fringe`로 머리 둘레 가는 가닥 사이 분홍빛 바탕과 작은 흰 조각까지 풀었다. 새로 뽑을 대화용 표정 시트 프롬프트는 `docs/ART_PROMPTS.md`.
- 프롬프트는 `docs/ART_PROMPTS.md`.

### 8-1. 소리

| 파일 | 쓰는 곳 | 출처 |
|---|---|---|
| `godot/audio/sfx/*.ogg` (31개) | 효과음 | `scripts/sound.py` 합성 |
| `godot/audio/bgm/title.ogg` | 타이틀, 프롤로그, 차압 결말 | Pixabay Music "Title" (사용자 제공, 2026-10-22). Pixabay Content License: 상업용 무료, 출처 표시 필요 없음 |
| `godot/audio/bgm/town.ogg` | 마을, 미끼가게, 수첩, 클리어 결말 | Pixabay Music "Town" (사용자 제공) |
| `godot/audio/bgm/sea.ogg` | 낚시 | Pixabay Music "Sea" (사용자 제공) |
| `godot/audio/bgm/market.ogg` | 위판장 | Pixabay Music "Market" (사용자 제공) |
| `godot/audio/bgm/truck.ogg` | 트럭 장사 | Pixabay Music "restaurant" (사용자 제공) |

곡은 `python3 scripts/bgm.py <원본> <이름>`으로 넣는다(앞뒤 무음 자르기, 음량 −18dB RMS, 끝 3초를 처음에 겹쳐 반복, Vorbis q3). 원본 mp3는 레포에 넣지 않는다.

## 9. 단계별 일정

한 단계가 끝날 때마다 `dotnet test` 통과, CI APK 빌드와 화면 사진 확인, 폰 테스트, 이 표 체크.

| 단계 | 내용 | 확인 방법 | 상태 |
|---|---|---|---|
| U0 | 계획, 코어 프로젝트, 시세·신선도 이전과 골든 테스트, SD 프롬프트 | `dotnet test` | ✅ |
| U1 | 고닷 프로젝트, CI APK 빌드와 릴리스 `test-latest`, 화면 사진 | APK가 폰에 깔리고 켜진다 | ✅ (폰 확인 2026-10-22) |
| U2 | 코어 나머지 규칙, 골든 확장(62개), 점검 화면 `CoreLab` | `dotnet test`, 폰에서 "웹 일치 점검" | ✅ (폰 확인 2026-10-22) |
| U3 | 릴링 물리를 웹에서 `reel.ts`로 빼고 C# `ReelFight`로 | `dotnet test` | ✅ (골든 260판 프레임 단위 일치) |
| U4 | 코어 자동 플레이: 새 게임부터 30일을 규칙대로 돌려 예외가 없는지, 빚을 갚을 수 있는지 | `dotnet test` | ✅ (적당히 잘하면 20/20 클리어, 평균 22일. `GAME_DESIGN.md` 9절) |
| U5 | 공통 UI(Theme, 버튼, 모달, HUD, 토스트, 화면 전환), Boot·Title·Town | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기) |
| U6 | Fishing (터치, 게이지, 찌, 그림자, 물보라, 결과) | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). 마을 방파제에서 들어간다. 프롤로그 튜토리얼 낚시는 U9 |
| U7 | Market·Shop·Notebook | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). 공통 바탕 `Ui/PlaceScreen`, 탭 `Ui/TabStrip`, 노씨는 대화창 일러스트 |
| U8 | Truck (SD 손님, 간 맞추기, 반응) | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). SD 손님 6명×4포즈(`scripts/sd_cells.py`), 게이지 `Ui/SeasonGauge` |
| U9 | Story와 컷신, 프롤로그 튜토리얼, 클리어·차압 결말 | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). `Screens/StoryScreen`, `data/story.json`, 튜토리얼 낚시(`GameState.Tutorial`). 컷신 전용 그림은 아직 감정 일러스트로 대신한다 |
| U10 | SD 그림 교체와 애니메이션 다듬기 | 폰 | |
| U11 | 효과음·진동, 설정 화면 | 폰 | ✅ 사진 확인 (폰 확인 대기). `scripts/sound.py` 합성 효과음 31개·배경음 5곡, `App/Sound`·`Haptics`·`Settings`, 수첩 설정 탭. 낚시 개편(부채꼴 조준, 고등어 지점, 큰 놈, 고등어 도둑, 물고기 대신 들끓는 물)도 함께. 힘싸움 `Tug`, 그린 낚싯대, 어종 71종 |
| U11-1 | 하루 다섯 칸(새벽·아침·점심·저녁·밤), 시간대 마을 그림(WebP), 그림 속 여울호, 밤낚시, 새 요리·의뢰 다섯, 요리 그림 열 장 | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). `GAME_DESIGN.md` 6절 |
| U11-2 | 여울호 등급(평판·의뢰), 오늘의 메뉴 고르기, 손님별 친밀도와 단골, 새 손님 6명 | 사진, 폰 | ✅ 사진 확인 (폰 확인 대기). `GAME_DESIGN.md` 7절, `data/diner.json` |
| U11-4 | 주간 상환(4주, 이자·연체), 주간 목표, 주말 이벤트, 상환 장면 | 사진, 폰 | 사진 확인 중. `GAME_DESIGN.md` 9절, `data/weekly.json` |
| U11-3 | 날씨(`data/weather.json`), 상점 업데이트(장비 10단계·보상·미끼·통발, `data/gear.json`), 앉아 기다리는 여울 | 사진, 폰 | 사진 확인 중. `GAME_DESIGN.md` 6·8절 |
| U12 | 폰에서 1장 끝까지 플레이, 웹과 수치 비교 | 폰 | |
| U13 | iOS (맥 필요. 나중에 정한다) | 아이폰 | |

## 10. 사용자가 할 일

**없다.** Claude가 `core/`나 `godot/`를 푸시하면 GitHub Actions가 APK를 만든다. 사용자는 폰에서 받아 테스트만 한다.

### 10-1. 폰에 설치하기 (빌드마다)
1. 폰 브라우저나 GitHub 앱에서 github.com/songok14/Yeoul → **Releases** → **테스트 빌드 #번호** (태그 `test-latest`).
2. `hanjeopsi-번호.apk`를 받아 연다. 처음이면 "출처를 알 수 없는 앱 설치"를 허용한다.
3. 같은 테스트 키로 서명해서 다음 빌드는 지우지 않고 덮어 설치된다. 세이브도 남는다.
4. 빌드 하나는 10분 안팎 걸린다.

### 10-2. 지금 APK에서 볼 것
주간 상환 (U11-4)
- HUD 오른쪽 위가 "1주 D-6", 아래 줄이 "이번 주 49만 원 · 빚 309만 원". 다 내면 "이번 주 완료".
- 마을 소식 아래 "1주 목표 0/3" 알약. 누르면 목표와 진행, 보상. 목표를 채우고 마을에 오면 "목표 달성!" 알림과 보상.
- 6일 장날(손님 많음), 13일 대물 소문, 20일 차세라와 대결(40cm 넘는 고기). 소식 줄에 이벤트 이름.
- 할머니댁 "이번 주 몫 갚기". 상환일(7·14·21·28일) 다음 날 아침에 상환 장면. 못 내면 연체료, 두 번이면 차압.
- 한 주가 너무 빠듯하거나 느슨하지 않은지, 이자·연체가 이해되는지.

날씨와 상점 (U11-3)
- 마을 소식 줄 날짜 옆에 "비"·"안개"가 뜨는 날이 있는지. 그날 마을 그림이 비·안개 그림인지, 아침 요약에 날씨 한 줄.
- 비 오는 날 낚시: 화면이 어둑하고 빗줄기, 입질이 조금 빨리 온다. 안개 날: 수평선이 하얗게 묻히고 큰 놈이 자주 온다.
- 찌를 던지면 여울이 의자에 앉아 기다리고, 입질이 오면 머리 위에 주황 "!".
- 미끼가게 탭 셋: 낚시(낚싯대·릴·줄·바늘·찌, 대나무대), 보관·편의(쿨러·미끼통·뜰채·헤드랜턴·의자·장화·통발), 미끼(열 가지 카드).
- 장비 줄마다 다음 단계 그림과 효과, 값 또는 "받는 법". 4단계까지 사면 5단계는 "여울호가 동네 맛집이 되면" 같은 조건이 보인다.
- 조건을 채우면 마을에 "노씨가 미끼가게에서 부른다", 가게에 들어가면 노씨가 장비를 준다.
- 미끼를 골라 쓰면 낚시 화면 위 줄에 미끼 이름과 남은 수. 다 쓰면 크릴로 돌아간다. 생새우면 감성돔이 자주 오는지.
- 통발을 사면 다음 날 아침 요약에 "통발에 문어 들었다".

여울호 등급과 손님 (U11-2)
- 트럭에 들어가면 "오늘의 메뉴 2/2". 재료 있는 요리를 눌러 올리고 내린다. 처음엔 두 개까지, 세 개째는 "메뉴는 2개까지" 알림.
- 메뉴판 둘째 줄에 등급·평판·오늘 손님 수. 장사 시작 단추에 "손님 N명".
- 손님은 고른 메뉴 안에서만 시킨다. 좋아하는 요리가 메뉴에 있으면 자주 그걸 시킨다(어부 삼촌은 매운탕).
- 결과에 "○○ 친밀도 n/5 · 평판 +3". 친밀도 5가 되면 "○○이 단골이 됐다!". 단골은 늘 하던 말로 주문하고, 그 시간대 손님이 한 명 는다.
- 평판 40이 되면 장사 끝에 "여울호 소문이 났다! 이제 여울호는 동네 맛집". 메뉴 칸이 3개로.
- 너무 빨리 또는 너무 늦게 오르지 않는지, 메뉴 고르기가 귀찮지 않은지.

하루 다섯 칸과 새 마을 (2026-10-26)
- 위 HUD 시간대 칩이 새벽·아침·점심·저녁·밤 다섯 개인지, 글자가 겹치지 않는지.
- 시간이 지날 때마다 마을 그림이 바뀌는지: 새벽(분홍빛 새벽) → 아침(해 뜸) → 점심(맑은 낮) → 저녁(노을) → 밤(달).
- 트럭이 한 대만 보이는지(그림 속 푸드트럭). 트럭이나 "여울호" 간판을 누르면 트럭 장사로 들어가는지. 다른 간판과 겹치지 않는지.
- 밤에는 미끼가게 단추가 "밤엔 닫음"으로 흐리고, 방파제는 "밤낚시". 밤낚시에 갈치·붕장어·볼락이 잘 나오는지.
- 트럭 단추에 손님 수가 보이는지(아침 4, 점심 6, 저녁 5, 밤 3). 밤엔 트럭 화면이 푸르스름한지.
- 한 번 출조에 캐스팅 4번. 하루가 너무 짧거나 길게 느껴지지 않는지.
- 새 요리: 참돔, 도다리, 갑오징어, 우럭, 농어를 낚으면 트럭 메뉴에 참돔회, 도다리쑥국(쓴맛!), 갑오징어숙회, 우럭매운탕, 농어구이가 뜨는지. 위판장 의뢰에도 나오는지.

힘싸움과 도감 71종
- 힘싸움: 물고기가 질주하기 전 화살표가 뜨고 반대쪽에 금빛 띠. 반대쪽 화면 절반을 누르면 버티는지, 같은 쪽을 누르면 텐션이 확 오르는지. 너무 어렵거나 쉽지 않은지.
- 감기·버티기 때 여울 손에서 낚싯대가 자연스럽게 뻗고, 텐션만큼 물고기 쪽으로 휘는지(이번에 그려지지 않던 버그를 고쳤다).
- 진동: 큰 고기일수록 질주·버티기 진동이 세고 길어지는지.
- 수첩 도감: 3열×4줄 카드, 71종, 쪽 넘김. 봄 남해다운 고기(볼락, 감성돔, 도다리, 쥐노래미...)가 주로 나오는지.

이야기와 튜토리얼 (U9)
- 새로 시작 → 이야기 1부 열 장면. 글이 한 자씩 나오고, 누르면 다 보이고, 한 번 더 누르면 다음 장면. 그림이 스르륵 바뀌며 천천히 다가가는지. 건너뛰기 단추.
- 1부 끝 → 저녁 방파제 튜토리얼. 고등어와 쿨러가 없다. 헛챔질이나 늦은 챔질은 다시 기다리고, 줄이 터질 뻔하면 "손을 떼!"로 구해 준다. 볼락 19.4cm 를 낚으면 "구워 먹어 보자".
- 2부: 소금 치기 게이지(정답 구간이 안 보이다 번쩍), 숟가락 든 여울, 고등어가 볼락을 물고 달아남 → "미각 회복 1단계: 짠맛" → 마을과 첫 대화.
- 프롤로그 중간에 껐다 켜서 이어하기 → 그 자리부터. 1장 클리어(빚 0) → 결말 이야기 → 마을. 차압 → 결말 이야기 → 타이틀.
- 엄마 손님과 여학생이 트럭으로 걸어올 때 앞을 보고 걷는지.

소리와 진동, 낚시 개편 (U11)
- 소리를 켜고 본다. 화면마다 배경음이 바뀌는지(타이틀, 마을·가게·수첩, 바다, 위판장, 트럭), 바뀔 때 끊기지 않고 스르륵 넘어가는지, 곡 끝에서 처음으로 이어질 때 툭 끊기는 소리가 없는지.
- 낚시: 부채꼴 위로 금색 바늘이 오가고 누르면 멈춘다. 파워를 모으는 동안 떨어질 자리 동그라미가 바늘 위를 오간다. 고등어가 우는 곳은 물비늘이 반짝인다.
- 반짝이는 곳에 가까이 던지면 "좋은 자리다" 문구. 입질 시간은 어디든 비슷한지. 한 번 던지는 데 10~15초쯤인지.
- 진동: 속임 입질 톡, 진짜 입질 투둑, 저항 꾹, 텐션이 빨간색이면 계속 떨림, 줄 터짐 길게. 너무 세거나 잦지 않은지.
- 큰 놈이다! (가끔): 화면이 크게 흔들리고 그림자가 크고 진하다.
- 낚시 중에는 물고기를 그리지 않는다. 줄 끝 물이 어둡게 가라앉고 거품이 휘돌며 다가오는지, 저항할 때 가끔 물기둥이 철썩 솟는지, 끌어올리면 발밑에서 물이 크게 솟는지.
- 쿨러에 고기가 있으면 가끔 고등어가 쿨러로 살금살금 간다. 고등어를 누르면 말리고, 놓치면 한 마리를 물고 간다.
- 수첩 설정 탭에서 효과음, 배경음, 진동을 끄고 켜 본다. 앱을 껐다 켜도 설정이 남는지.

타이틀과 마을 (U5)
- 켜면 어두운 막이 걷히며 타이틀. 제목 → 머리말 세 줄 → 버튼 순서로 떠오르는지, 빛 알갱이가 바다 위로 오르는지, 고등어가 숨 쉬는지.
- 제목 글꼴(고운바탕 굵게)과 본문 글꼴(고운돋움)이 깨지지 않는지.
- 새로 시작 → 마을. 위쪽 HUD(날짜, 시간대 칩, D-day, 소지금, 상환 막대)와 아래 메뉴 여섯 개.
- 버튼을 누르면 살짝 눌렸다 튕기는지. 아직 없는 화면은 "…는 다음 빌드에서 열려요" 알림.
- 간판, 여울, 고등어, 트럭을 눌러 본다. 여울은 손을 흔들고 말풍선, 고등어는 냐옹.
- 새로 시작하면 여울 일러스트가 들어오며 첫마디 대화. 누르면 다음 줄, 다 읽으면 닫힌다.
- 할머니댁: 할머니 일러스트와 한마디, 그 위에 선택지. 빚 갚기(돈이 굴러가며 줄고 상환 막대가 찬다, 빨간 숫자가 떠오른다), 잠자기 → 다음 날 새벽과 어제 요약.
- 대화 일러스트의 머리카락 둘레에 흰 테두리가 보이지 않는지.
- 앱을 껐다 켜서 이어하기. 노치나 제스처 바에 글자가 가리지 않는지, 긴 화면에서 간판이 잘리지 않는지.

여울호 트럭 (U8)
- 마을 메뉴 여울호나 트럭을 눌러 들어간다(새벽은 쉰다). 간판에 "여울호", 칠판에 오늘 메뉴가 쓰여 있는지.
- 장사 시작 → 손님이 왼쪽에서 통통 걸어와 트럭을 보고 서서 주문한다. 손님마다 말투가 다르다. 몸이 아래 판에 가리지 않는지.
- 간 맞추기: 바늘이 오가고 넣기! 로 멈춘다. 아는 맛은 초록 구간이 보이고 멈추면 좋아!/조금 아쉽다. 모르는 맛은 ? 표시.
- 결과: 손님이 기뻐 뛰거나 실망하고 리뷰 말풍선, 받은 돈이 떠오른다. 다음 손님이면 왼쪽으로 걸어 나간다.
- 다섯 명을 받거나 재료가 떨어지면 장사 끝, 마을로 가면 시간이 한 칸 지나 있다.

위판장·미끼가게·수첩 (U7)
- 마을 메뉴나 간판에서 위판장. 시세판 다섯 어종, 쿨러 묶음별 팔기. 팔면 값이 툭툭 뛰다 "낙찰!" 도장이 찍히고 소지금이 오른다.
- 쿨러가 많으면 아래 ◀ ▶ 로 쪽을 넘긴다. 단추가 아래 "마을로" 막대에 가리지 않는지.
- 납품 의뢰 탭: 카드 두 장. 물고기가 모자라면 납품하기가 흐리다. 노씨 의뢰를 채우면 노씨 대화창이 뜬다.
- 미끼가게: 들어가면 노씨 일러스트와 한마디. 오른쪽 위 둥근 얼굴을 누르면 또 이야기한다. 미끼 사기, 장비 사기(산 뒤 노씨 한마디), 대나무대 보여 주기.
- 수첩: 탭 네 개(쿨러, 도감, 미각, 일지). 금색 알약이 미끄러지는지, 도감 칸을 누르면 큰 물고기 그림, 고등어를 누르면 냐옹.
- 아침 지나 낮·저녁이면 위판장은 "경매 마감"이고 팔기 단추가 흐리다.

점검 화면 (타이틀 아래 '점검')
- 마을 배경 위에 SD 여울과 고등어가 나오는지. 누르면 포즈가 바뀌고 걷기는 두 프레임이 번갈아 나온다.
- 버튼 9개로 하루를 돌려 본다: 출조(6번 던지기, 미니게임 없이 다 낚음) → 경매 → 트럭 장사 → 잠자기.
- **웹 일치 점검**을 누르면 11개 항목이 모두 ○ 인지. 하나라도 × 면 화면을 찍어 보내 준다.
- 앱을 껐다 켜면 "이어하기"로 같은 날짜, 같은 돈이 나오는지.
- 노치나 아래 제스처 바에 글자나 버튼이 가리지 않는지.

### 10-3. (선택) PC에서 고닷으로 열어 보기
1. godotengine.org에서 **Godot 4.7.2 .NET** 판을 받는다(설치 없이 압축만 풀면 된다). .NET 8 SDK도 필요하다.
2. 고닷 실행 → Import → `Yeoul/godot/project.godot`.
3. F5로 실행. 화면 크기는 창에서 540×960(절반)으로 보인다.

## 11. 위험과 메모

- **Claude는 여기서 고닷을 실행하지 못한다.** 고닷 배포처가 이 환경에서 막혀 있다. C# 코드는 `dotnet build`로 진짜 GodotSharp에 대고 컴파일하고, 실행과 화면 확인은 CI 사진으로 한다.
- **C#이 빠진 APK**: 고닷 내보내기는 C# 오류(예: `godot/Hanjeopsi.sln`이 없음)가 나도 "경고"로 끝내고 APK를 만든다. 그런 APK는 켜자마자 꺼진다(빌드 #8에서 겪었다). 워크플로가 APK 안에 `Hanjeopsi.dll`이 있는지 검사해서 없으면 실패시킨다. 솔루션 파일은 지우지 않는다.
- **CI 사진은 PC용 OpenGL(소프트웨어 렌더링)**이라 폰과 아주 조금 다를 수 있다. 최종 확인은 폰에서.
- **Actions 사용량**: 비공개 레포는 한 달 2,000분 무료. 빌드 한 번 10분 안팎. `core/`, `godot/`, 워크플로가 바뀐 푸시에만 돈다.
- **`ci-build` 브랜치**는 매번 강제로 덮어쓰는 결과물 전용 브랜치다. 작업 브랜치로 쓰지 않는다.
- **테스트 서명 키**는 레포에 공개돼 있다. 스토어에 올릴 일이 생기면 새 키를 비밀값으로 둔다.
- **한글 폰트**: TTF(약 7MB)를 그대로 쓴다. 출시 단계에서 쓰는 글자만 남겨 줄일 수 있다.
- **시세 시드**: 세이브의 `seed`와 `species.json` 순서가 같으면 웹과 같은 흐름이다.
- **규정 확인**: 금지체장·금어기는 챕터를 만들 때마다 FishingPlan 기본표와 실제 고시를 다시 확인한다.

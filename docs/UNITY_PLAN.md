# 유니티 이전 계획

웹 버전(Phaser 3.90 + TypeScript, `src/`)은 1장 물비늘포 MVP로 재미 확인을 마쳤다. 이제 모바일 게임으로 제대로 만들기 위해 유니티로 옮긴다.
웹 버전은 지우지 않고 **기획과 동작의 기준**으로 남긴다. 유니티에서 같은 상황이면 같은 결과가 나와야 한다.

## 1. 결정 사항

| 항목 | 결정 |
|---|---|
| 엔진 | Unity 6.3 LTS (`6000.3.27f1`), Personal(무료). URP 2D는 씬을 옮길 때(U5) 넣는다 |
| 목표 기기 | 안드로이드 먼저, 이후 iOS. 세로 1080×1920 기준 |
| 언어 | C# 9 (Unity 6 지원 범위). `record`, 파일 범위 namespace, `global using`은 쓰지 않는다 |
| 규칙 코드 | UnityEngine을 모르는 순수 C# 코어. 여기(클라우드)서 dotnet으로 빌드하고 xUnit으로 테스트 |
| 빌드 | **GitHub Actions + GameCI(무료)** 가 APK를 만든다. 사용자는 릴리스 `test-latest`에서 받아 폰에 설치한다. 비공개 레포는 한 달 2,000분 무료 |
| 데이터 | JSON (`Resources/HanjeopsiData/*.json`). 파싱은 Newtonsoft Json(유니티 공식 무료 패키지) |
| 웹과 같은지 확인 | 웹 코드로 기준값(골든)을 뽑아 C# 테스트가 비교한다 |
| 그림 | 배경은 지금 수채화 일러스트. 인게임 캐릭터·물체는 SD(2~2.5등신). 일러스트는 컷신 전용 |
| 돈 | 유료 에셋, 유료 서비스, 유료 플러그인 금지. 유니티 공식 무료 패키지만 쓴다 |

## 2. 폴더 구조

```
Yeoul/
├─ src/, public/, docs/ ...           웹 버전 (기준. 지우지 않는다)
├─ scripts/
│  ├─ export-golden.mjs               웹 코드로 기준값을 뽑는다 (npm run golden)
│  ├─ golden/*.ts                     기준값 만드는 코드 (market, rules, scenario)
│  └─ sd_sheet.py                     SD 시트(#EDEDED)를 칸마다 잘라 유니티용 PNG로
└─ unity/
   ├─ README.md                       빌드와 테스트 방법
   ├─ golden/                         웹에서 뽑은 기준값 JSON (커밋한다)
   ├─ com.songok.hanjeopsi.core/      ① 순수 C# 코어 (유니티 로컬 패키지)
   │  ├─ package.json
   │  ├─ Runtime/                     Hanjeopsi.Core.asmdef (noEngineReferences)
   │  │  ├─ Util/                     JsMath, Mulberry32, IRandom
   │  │  ├─ Data/                     Species, Gear, Balance, Texts, GameData(로더)
   │  │  ├─ State/                    FishItem, SaveData, SaveJson
   │  │  ├─ Systems/                  Freshness, Market, Rules, Taste, FishingRoll, SeasonJudge, Truck, (ReelFight)
   │  │  └─ Game.cs                   한 판: 세이브 + 규칙 + 행동(웹 actions.ts, time.ts)
   │  └─ Resources/HanjeopsiData/     species, gear, balance, texts, recipes, requests, taste .json
   ├─ com.songok.hanjeopsi.game/      ② 유니티 쪽 코드 (로컬 패키지)
   │  ├─ Runtime/                     Hanjeopsi.Game.asmdef → Hanjeopsi.Core 참조
   │  │  ├─ Boot/                     GameDataLoader, SaveStore, UnityRandom, (GameManager)
   │  │  ├─ Scenes/                   (씬별 컨트롤러)
   │  │  ├─ UI/                       (버튼, 모달, 게이지, HUD)
   │  │  └─ Debug/                    CoreLab (U2 점검 화면)
   │  └─ Editor/                      BuildScript(APK 빌드), ArtImport(그림 가져오기 설정)
   ├─ dotnet/                         ③ 코어 빌드·테스트 (유니티는 이 폴더를 모른다)
   │  ├─ Hanjeopsi.sln
   │  ├─ Hanjeopsi.Core/              패키지 Runtime 소스를 그대로 컴파일 (netstandard2.1, C# 9)
   │  ├─ Hanjeopsi.Core.Tests/        xUnit 테스트 (net8.0)
   │  └─ UnityCheck/                  유니티 쪽 코드 근사 점검 컴파일 (Unity 2021.1 참조본)
   └─ HanjeopsiBada/                  ④ 유니티 프로젝트
      ├─ Assets/
      │  ├─ Resources/Art/            Backgrounds, Yeoul, Cat (PNG. 스프라이트, 피벗 아래 가운데)
      │  ├─ Resources/Fonts/          고운돋움 (OFL)
      │  ├─ Scenes/                   빌드 스크립트가 만든다 (커밋하지 않음)
      │  └─ link.xml                  IL2CPP 코드 제거 방지
      ├─ Keystore/ci-test.keystore    테스트 APK 서명 키 (덮어 설치용, 공개 값)
      ├─ ProjectSettings/ProjectVersion.txt
      └─ Packages/manifest.json       ①②를 file: 로 연결
.github/workflows/android.yml          코어 테스트 → 점검 컴파일 → APK → 릴리스 test-latest
```

왜 패키지로 나누나:
- 코어 소스가 **한 벌**만 있다. 유니티는 패키지로 읽고 dotnet은 같은 파일을 컴파일한다.
- 씬, 프리팹, 그림처럼 에디터가 만드는 것만 ④에 둔다.
- 지금 씬과 플레이어 설정은 `BuildScript`가 코드로 만든다. 그래서 `ProjectSettings`가 거의 비어 있어도 CI에서 같은 APK가 나온다.

## 3. 어셈블리

| 어셈블리 | 위치 | 참조 | 하는 일 |
|---|---|---|---|
| `Hanjeopsi.Core` | core 패키지 Runtime | Newtonsoft.Json만 | 규칙 계산, 데이터 모델, 세이브 구조. `noEngineReferences: true`라 UnityEngine을 쓰면 컴파일이 안 된다 |
| `Hanjeopsi.Game` | game 패키지 Runtime | Core, Unity | 씬 컨트롤러, UI, 연출, 입력, 사운드, 세이브 파일 입출력 |
| `Hanjeopsi.Game.Editor` | game 패키지 Editor | Core, Game, UnityEditor | APK 빌드, 그림 가져오기 설정 |
| `Hanjeopsi.Core.Tests` | unity/dotnet | Core, xUnit | 웹과 같은 결과인지, 설계표 수치가 맞는지 |
| (나중) `Hanjeopsi.Game.Tests` | game 패키지 Tests | Game, Unity Test Framework | 씬이 열리는지 같은 플레이 모드 확인 |

웹의 구조 규칙을 그대로 가져간다.
- 수치와 대사는 JSON(`src/data` 자리), 계산은 Core `Systems`(`src/systems` 자리), 상태 변경은 Core `Game`(`state/actions.ts` 자리).
- 씬 컨트롤러는 그리기와 입력만 맡는다. 계산식이 씬 코드에 들어가면 테스트할 수 없으니 Core로 뺀다.

## 4. 데이터 (JSON)

| 파일 | 웹 원본 | 상태 |
|---|---|---|
| `species.json` | `src/data/fish.ts` (도트용 `look`은 뺐다) | ✅ |
| `gear.json` | `src/data/gear.ts` | ✅ |
| `balance.json` | `config.ts` 상수와 시스템 상수 (시세, 신선도, 시간, 의뢰, 고양이, 입질, 판정, 트럭) | ✅ |
| `texts.json` | 신선도 단계, 위판장 소식, 시간대, 날짜, 규정 문구, 손님 리뷰 | ✅ |
| `recipes.json` | `src/data/recipes.ts` (맛 이름, 도구, 리뷰 포함) | ✅ |
| `requests.json` | `src/data/requests.ts` (노씨 의뢰는 `flag: nossiFavor`) | ✅ |
| `story.json` | `src/data/story.ts` (페이지, 배경 키, 컷신 키, 이벤트) | 다음 |
| `dialogue.json` | 할머니 대사, 고등어 말풍선, 토스트 문구 등 씬에 흩어진 글 | 다음 |
| `taste.json` | `systems/taste.ts` 미각 단계표 | ✅ |

- 필드 이름은 웹과 같은 camelCase. C# 쪽은 PascalCase 필드에 Newtonsoft camelCase 규칙으로 읽는다.
- 모르는 필드가 있으면 바로 오류를 낸다(`MissingMemberHandling.Error`). 오타를 조용히 넘기지 않기 위해서다.
- `species.json`의 순서는 시세 시드에 들어간다. 순서를 바꾸면 시세 흐름이 바뀌니 새 어종은 뒤에 붙인다.
- 어종 id는 FishingPlan 도감 그림 이름(`fish_<id>`)과 같다. 금지체장·금어기는 FishingPlan `core/catches/Limits.kt` 기본표를 따른다(테스트 `규정이_FishingPlan_기본표와_같다`).

## 5. 웹과 같은 결과 확인 (골든 테스트)

```
웹 코드 (src/systems/*.ts)
   │  npm run golden  (esbuild로 묶어서 node에서 실행)
   ▼
unity/golden/market.json   ← 시드 4개 × 45일 × 5종 시세, 물고기 값 300건, 낙찰가, endDay 신선도 감소
unity/golden/rules.json    ← 달력·금어기 60일, 놓아주기 이유, 어종·크기 뽑기 600번, 판정 200번, 요리값, 손님 한마디
unity/golden/scenario.json ← 새 게임부터 행동 2,800여 단계. 매 단계 세이브 전체와 화면 값
   │  dotnet test
   ▼
C# Core 가 같은 입력으로 계산해서 비트 단위로 비교
```

- 난수는 웹과 같은 mulberry32와 hash를 C#으로 옮겼다(uint 연산). 시드가 같으면 시세가 소수점 끝자리까지 같다.
- JS `Math.round`(.5는 위로)와 C# `Math.Round`(은행가 반올림)가 달라서 `JsMath.Round`를 쓴다.
- 웹이 `Math.random()`을 쓰는 곳(입질 어종, 크기, 경매 낙찰 배율, 손님 메뉴)은 C#에서 `IRandom`을 받는다. 골든 스크립트에서 `Math.random`을 mulberry32로 바꿔 끼우면 같은 난수 순서로 비교할 수 있다.
- 웹에서 씬 안에 들어 있던 계산은 먼저 웹 쪽을 systems로 빼서(동작은 그대로) 골든을 뽑고, 같은 식을 C#으로 옮긴다. U2에서 `truck.ts`, `season.ts`, `chapter.ts`, `actions.ts`의 `serveDish`·`completePrologue`·`useBait`를 뺐다. 릴링 물리는 U3에서 `reel.ts`로.
- 시나리오 골든은 웹이 행동을 고르고 인자까지 남긴다. C#은 같은 인자로 같은 행동을 하고, 세이브 JSON을 키 하나하나 비교한다. 어긋나면 몇 번째 행동의 어느 필드인지 알려 준다.
- 웹 수치를 바꾸면 `npm run golden` 후 `dotnet test`. 유니티 쪽 수치만 바꾸면 테스트가 깨지니 일부러 갈라지는 경우에만 골든 대신 C# 테스트 기대값을 고치고 이 문서에 적는다.

## 6. 씬 목록

| 유니티 씬 | 웹 씬 | 내용 | 비고 |
|---|---|---|---|
| `Boot` | `BootScene` | 데이터 로드, 세이브 로드, 폰트·아틀라스 준비 | `GameManager`(DontDestroyOnLoad) 생성 |
| `Title` | `TitleScene` | 새로 시작 / 이어하기 | |
| `Story` | `PrologueScene` | 컷신 일러스트 + 타자기 글. 프롤로그 1·2부, 1장 클리어, 차압 결말 | 미니 간 맞추기(소금) 연출 포함 |
| `Town` | `TownScene` | 마을 허브. 시간대별 배경, SD 여울·고등어·트럭, 6개 메뉴, 식당 모달 | |
| `Fishing` | `FishingScene` | 방향 → 파워 → 입질 → 챔질 → 릴링, 결과 모달 | 가장 손이 많이 가는 씬 |
| `Market` | `MarketScene` | 시세판, 경매 연출, 납품 의뢰 탭 | |
| `Truck` | `TruckScene` | 손님 SD 입장, 주문, 간 맞추기 게이지, 반응 | |
| `Shop` | `ShopScene` | 미끼, 장비 업그레이드 | |
| `Notebook` | `NotebookScene` | 쿨러, 도감, 미각 회복, 조황일지 | |

- 공통 UI(HUD, 버튼, 모달, 토스트, 말풍선, 떠오르는 숫자)는 프리팹으로 만들어 모든 씬에서 쓴다.
- 씬 이동은 `SceneRouter.Go("Town", 인자)` 하나로. 웹의 `goTo`처럼 인자는 매번 새로 넘긴다.
- 화면 좌표: 웹 논리 450×800 → 유니티 1080×1920 (2.4배). 카메라 직교 크기 9.6(PPU 100). UI는 Canvas Scaler 기준 1080×1920, Match 0(가로 기준), 노치는 Safe Area로 피한다.

## 7. 웹 기능과 유니티 구현 대응표

| 웹 | 유니티 | 어디에 | 상태 |
|---|---|---|---|
| `utils.ts` mulberry32, hash, clamp, round100, won | `Util/Mulberry32`, `Util/JsMath` | Core | ✅ |
| `systems/economy.ts` 신선도 단계·배율 | `Systems/Freshness` | Core | ✅ |
| `systems/economy.ts` 시세, 물고기 값, 등락, 소식 | `Systems/Market` | Core | ✅ |
| `time.ts` endDay 신선도 감소 | `Freshness.ApplyDailyDecay` | Core | ✅ |
| `actions.ts` sellFish 낙찰가 | `Market.AuctionTotal` | Core | ✅ |
| `time.ts` 시간 칸, 날 넘김, 의뢰 갱신 | `Game.UseSlot`, `EndDay`, `RefreshRequests` | Core | ✅ |
| `calendar.ts` 날짜, 금지체장, 금어기 | `Systems/Rules` (해를 넘는 금어기도 처리. FishingPlan과 같게) | Core | ✅ |
| `chapter.ts` 1장 클리어·차압 판정 | `Game.CheckChapter` | Core | ✅ |
| `taste.ts` 미각 단계, 해금 | `Systems/Taste` | Core | ✅ |
| `fishing.ts` 거리 구간, 어종·크기 뽑기, 고등어 힌트 정확도 | `Systems/FishingRoll` | Core | ✅ |
| `FishingScene` 릴링 텐션·거리·저항·지침, 챔질 판정, 입질 대기 시간 | `Systems/ReelFight` (dt 단위 Step) | Core | U3 |
| `systems/truck.ts` 재료 고르기, 요리값, 손님 기분과 한마디 | `Systems/Truck` | Core | ✅ |
| `systems/season.ts` 간 맞추기 정확도 | `Systems/SeasonJudge` | Core | ✅ |
| `state/save.ts` SaveData, createSave, 불러오기 | `State/SaveData`, `SaveJson`, `Game.CreateSave` (필드 이름 웹과 같게. 웹 세이브 JSON을 그대로 읽는다) | Core | ✅ |
| `state/actions.ts` 상태 변경 | `Game` (행동이 끝나면 `Saved` 이벤트) | Core | ✅ |
| localStorage 저장 | `SaveStore` → `Application.persistentDataPath/save.json` (임시 파일에 쓰고 바꿔 끼움) | Game | ✅ (폰 확인 대기) |
| `ui/ui.ts` txt, Button, modal, toast, bubble | TextMeshPro + uGUI 프리팹 | Game | 다음 |
| 고운돋움·고운바탕 | TMP 폰트 에셋 (Dynamic, 한글 2350자 + 자주 쓰는 기호) | Game | 다음 |
| `ui/hud.ts` | `Hud` 프리팹 | Game | 다음 |
| `ui/art.ts` 임시 도트 손님·얼굴 | SD 손님 스프라이트 | Game | 그림 대기 |
| tween, 카메라 흔들기 | 코루틴 + 간단한 Tween 헬퍼 (무료. DOTween 무료판도 가능) | Game | 다음 |
| `scripts/art.py` (WebP) | SD 시트는 `scripts/sd_sheet.py`(PNG). 배경은 WebP를 PNG로 바꿔 넣었다 | scripts | ✅ |
| `scripts/screenshot.mjs`, `playtest.mjs` | 에디터 메뉴 "한접시/전체 씬 열어보기", 코어 자동 플레이 테스트(dotnet) | Game / Core | 나중 |
| `?scene=Town&demo=1` | Boot 씬 인스펙터의 "시작 씬"과 "체험 세이브" 옵션 | Game | 다음 |

## 8. 그림 적용 방향

- **배경**: `public/assets/bg-*.webp`를 PNG로 바꿔 `Assets/Resources/Art/Backgrounds`에 (마을 4장 완료). 1080×1920 화면에 900×1600 원본이라 1.2배 확대된다. 원본 고해상도가 있으면 다시 뽑는다.
- **인게임 SD**: 여울, 고등어, 막순 할머니, 노씨, 차세라, 손님, 트럭 여울호, 물고기, 요리. 시트(#EDEDED 배경)를 `scripts/sd_sheet.py`로 칸마다 잘라 PNG로. 크기는 바꾸지 않아 같은 시트의 프레임은 같은 비율이다.
  ```bash
  python3 scripts/sd_sheet.py <시트.png> 2 4 unity/HanjeopsiBada/Assets/Resources/Art/Yeoul front,three-quarter,side,back,idle,wave,walk-1,walk-2
  ```
  여울 8장, 고등어 12장 완료.
- **애니메이션**:
  - 간단한 것(숨쉬기, 통통 튀기, 꼬리 흔들기)은 한 장 그림 + 코드 트윈.
  - 걷기·낚시 동작은 포즈 그림을 프레임으로 넘기는 스프라이트 애니메이션.
  - 부품을 나눈 그림이 오면 유니티 **2D Animation** 패키지(공식 무료)로 뼈대를 넣는다. 표정은 눈·입 스프라이트 교체(Sprite Resolver).
- **컷신**: 다듬은 감정 일러스트를 `Story` 씬에서만 쓴다. 세로 9:16 한 장 그림, 아래 30%는 글 상자 자리.
- 그림 프롬프트는 `docs/ART_PROMPTS.md`.

## 9. 단계별 일정

한 단계가 끝날 때마다 `dotnet test` 통과, GitHub Actions APK 빌드 성공, 폰에서 확인, 이 표 체크.

| 단계 | 내용 | 확인 방법 | 상태 |
|---|---|---|---|
| U0 | 계획, 코어 프로젝트, 시세·신선도 이전과 골든 테스트, SD 프롬프트(여울·고등어) | `dotnet test` 31개 | ✅ |
| U1 | 유니티 프로젝트, 두 패키지 연결, GameCI APK 빌드와 릴리스 `test-latest` | APK가 폰에 깔리고 켜진다 | 라이선스 비밀값 대기 |
| U2 | 코어 나머지: 날짜·규정, 시간 칸·날 넘김, 의뢰, 미각, 어종 뽑기, 요리값, 간 맞추기 판정, 세이브, 행동. 골든 확장. 폰 점검 화면 `CoreLab` | `dotnet test` 62개, 폰에서 "웹 일치 점검" | ✅ (폰 확인 대기) |
| U3 | 릴링 물리를 웹에서 `reel.ts`로 빼고 C# `ReelFight`로. 같은 입력이면 같은 결과 | `dotnet test` | |
| U4 | 코어 자동 플레이: 새 게임부터 30일을 규칙대로 돌려 예외가 없는지, 빚을 갚을 수 있는지 (웹 `playtest.mjs` 자리) | `dotnet test` | |
| U5 | 유니티 공통 UI(URP 2D, TMP 폰트, 버튼, 모달, HUD, 토스트), Boot·Title·Town | 폰 | |
| U6 | Fishing 씬 (터치 입력, 게이지, 찌, 그림자, 결과) | 폰 | |
| U7 | Market·Shop·Notebook | 폰 | |
| U8 | Truck (손님, 간 맞추기, 반응) | 폰 | |
| U9 | Story 씬과 컷신, 프롤로그 튜토리얼, 클리어·차압 결말 | 폰 | |
| U10 | SD 그림 교체와 애니메이션 | 폰 | |
| U11 | 효과음·진동, 설정 화면 | 폰 | |
| U12 | 폰에서 1장 끝까지 플레이, 웹과 수치 비교 | 폰 | |
| U13 | iOS 빌드 (Mac 필요. GameCI macOS 러너는 유료 분이 많이 들어 나중에 정한다) | 아이폰 | |

## 10. 사용자가 할 일

APK는 GitHub Actions가 만든다. 사용자는 **처음 한 번 라이선스 비밀값을 넣고**, 그 뒤로는 폰에서 받아 테스트만 한다.
유니티 에디터는 비밀값을 만들 때 한 번 필요하고, 그 뒤로는 열지 않아도 된다.

### 10-1. 유니티 개인 라이선스 파일 만들기 (처음 한 번, PC)
1. Unity Hub를 설치하고 Unity 계정으로 로그인한다.
2. Hub → 설정(톱니) → Licenses → **Add** → **Get a free personal license**.
3. 라이선스 파일 `Unity_lic.ulf`가 생긴다. 위치는 아래와 같다.
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
4. 이 파일을 메모장으로 열어 **내용 전체**를 복사해 둔다. (`<?xml` 로 시작해서 `</root>` 로 끝난다)
5. 유니티 계정에 2단계 인증을 켜 두었다면 GameCI가 로그인하지 못할 수 있다. 실패하면 Actions 로그를 보내 준다.

### 10-2. GitHub에 비밀값 넣기 (처음 한 번)
1. github.com/songok14/Yeoul → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.
2. 세 개를 만든다.

| 이름 | 값 |
|---|---|
| `UNITY_LICENSE` | 10-1에서 복사한 `Unity_lic.ulf` 내용 전체 |
| `UNITY_EMAIL` | 유니티 계정 이메일 |
| `UNITY_PASSWORD` | 유니티 계정 비밀번호 |

3. 넣은 뒤 Actions 탭 → "안드로이드 테스트 빌드" → 마지막 실행 → **Re-run all jobs**. 또는 Claude에게 다시 돌려 달라고 한다.
4. 비밀값은 GitHub 안에서만 쓰이고 로그에는 가려진다. Claude도 값을 볼 수 없다.

### 10-3. 폰에 설치하기 (빌드마다)
1. 폰 브라우저나 GitHub 앱으로 github.com/songok14/Yeoul → **Releases** → **테스트 빌드 #번호** (태그 `test-latest`).
2. `hanjeopsi-번호.apk`를 받아 연다. 처음이면 "출처를 알 수 없는 앱 설치"를 허용한다.
3. 같은 테스트 키로 서명하므로 다음 빌드는 지우지 않고 덮어 설치된다. 세이브도 남는다.
4. 빌드 하나는 처음 30~40분, 캐시가 생긴 뒤로는 15분 안팎 걸린다.

### 10-4. 지금 APK(U2 점검 화면)에서 볼 것
- 마을 배경 위에 SD 여울과 고등어가 나오는지. 누르면 포즈가 바뀌고 걷기는 두 프레임이 번갈아 나온다.
- 한글이 깨지지 않는지 (고운돋움).
- 버튼 9개로 하루를 돌려 본다: 출조(6번 던지기, 미니게임 없이 다 낚음) → 경매 전부 팔기 → 트럭 장사 → 잠자기.
- **웹 일치 점검**을 누르면 11개 항목이 모두 ○ 인지. 하나라도 × 면 화면을 찍어 보내 준다.
- 앱을 껐다 켜면 "이어하기"로 같은 날짜, 같은 돈이 나오는지 (세이브 파일).
- 노치나 아래 제스처 바에 글자나 버튼이 가리지 않는지.

### 10-5. (선택) PC 에디터에서 열어 보기
1. Hub → Installs → **Unity 6000.3.27f1**(6.3 LTS) 설치. 모듈은 **Android Build Support**(OpenJDK, SDK & NDK 포함).
2. Hub → Projects → **Add** → `Yeoul/unity/HanjeopsiBada` 폴더.
3. 메뉴 **한접시 → 점검 씬 만들기** 후 Play. **한접시 → 안드로이드 APK 빌드**로 PC에서도 같은 APK를 만들 수 있다.
4. 에디터가 만든 `ProjectSettings/`와 `.meta` 파일은 커밋해도 된다. `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Assets/Scenes/`는 커밋하지 않는다(`unity/.gitignore`).

## 11. 위험과 메모

- **유니티 코드는 여기서 진짜로 컴파일하지 못한다.** `unity/dotnet/UnityCheck`가 Unity 2021.1 참조본으로 근사 점검을 하고, 진짜 확인은 Actions 빌드다. 계산은 최대한 Core로 보낸다.
- **IL2CPP 코드 제거**: Newtonsoft는 리플렉션으로 필드를 채운다. `Assets/link.xml`이 Core·Game 어셈블리를 지키고, 스트리핑은 Minimal.
- **Actions 사용량**: 비공개 레포는 한 달 2,000분 무료. 빌드 한 번 15~40분이라 하루 몇 번이면 충분하다. `unity/**`가 바뀐 푸시에만 돈다. 아티팩트는 14일 뒤 지워지고, 폰 설치용은 릴리스 하나만 덮어쓴다.
- **테스트 서명 키**는 레포에 공개돼 있다. 스토어용 키는 따로 만들어 비밀값으로만 둔다.
- **한글 폰트**: 지금은 TTF를 동적 폰트로 쓴다(약 7MB). U5에서 TMP 폰트 에셋을 만들고, 출시 전에 쓰는 글자만 모아 크기를 줄인다.
- **WebP**: 유니티는 WebP를 읽지 못한다. 배경과 물고기 그림은 PNG로 바꿔 넣는다.
- **시세 시드**: 세이브의 `seed`와 `species.json` 순서가 같으면 웹과 같은 흐름. 웹 세이브를 옮겨 와도 같은 시세가 나온다.
- **규정 확인**: 금지체장·금어기는 챕터를 만들 때마다 FishingPlan 기본표와 실제 고시를 다시 확인한다.

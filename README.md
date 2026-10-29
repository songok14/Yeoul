# 🎣 여울호

미각을 잃은 셰프 한여울이 고향 남해 물비늘포로 돌아와, 직접 낚은 물고기로 맛과 할머니의 밥집을 되찾는 **모바일 낚시 경영 게임**입니다.
할아버지가 배 이름을 붙인 1톤 트럭 '여울호'로 낚은 고기를 팔며 빚을 갚습니다.

지금은 **고닷 4(C#) 안드로이드 판**으로 옮겨 1장 물비늘포를 다듬는 중입니다(v0.3). 처음 만든 웹 판(Phaser)은 기획과 수치의 기준으로 남겨 둡니다.

## 플레이 (고닷 안드로이드 판)

- 세로 화면, 손가락 하나로 누르기와 꾹 누르기만 씁니다.
- 목표: **1차 상환금 300만 원을 4주 동안 매주 나눠 갚기**. 매주 이자가 붙고, 상환일을 두 번 넘기면 할머니댁이 차압됩니다.
- 하루는 **아침 · 점심 · 저녁 · 밤** 네 칸입니다. 아침과 점심엔 방파제 낚시와 위판장 경매, 저녁엔 여울호 장사, 8일째부터는 밤낚시도 열립니다.
- 마을: 언덕 마을 지도를 누른 곳으로 여울이 걸어갑니다. 간판을 누르면 그 가게로 들어가고, 머리 위에 ! 가 뜬 마을 사람에게 말을 걸면 납품 의뢰를 맡습니다.
- 낚시: 부채꼴 안에서 던질 곳을 고르고 게이지로 거리를 정합니다. 찌가 쑥 들어가면 챔질하고, 꾹 눌러 감다가 텐션이 빨개지면 손을 뗍니다. 고양이 고등어가 우는 쪽에 입질이 많고, 고등어 도둑도 막아야 합니다.
- 여울호: 오늘의 메뉴를 고르고 간을 맞춰 손님에게 냅니다. 명성이 쌓이면 손님이 늘고, 단골과 VIP 예약 손님이 생깁니다.
- 낚은 어종이 늘수록 미각이 돌아와 요리의 정답 구간이 보입니다.

### 폰에 설치하기

GitHub Actions가 `core/`나 `godot/`가 바뀔 때마다 APK를 만듭니다.
**Releases → 테스트 빌드 (태그 `test-latest`)** 에서 APK를 받아 설치합니다. 테스트 키로 서명해서 다음 빌드도 덮어 설치됩니다.

## 개발

```bash
dotnet test core/Hanjeopsi.sln                              # 게임 규칙 빌드와 테스트 (골든·자동 플레이 포함)
dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true   # 고닷 쪽 C# 컴파일 점검 (.NET 8 환경)
```

- 고닷 4.7.2 .NET 으로 `godot/project.godot`를 열면 PC에서 바로 실행할 수 있습니다.
- 화면 사진(약 100장)과 APK는 CI가 `ci-build` 브랜치에 올립니다.
- 작업 규칙은 `CLAUDE.md`, 다음 세션 이어가기는 `docs/HANDOFF.md`에 있습니다.

웹 판은 Node.js 20 이상에서 돌립니다.

```bash
npm install
npm run dev        # 개발 서버
npm run build      # 타입 검사 + 빌드 → dist/
npm run golden     # 웹 계산 기준값(core/golden) 다시 만들기
```

## 기술

| 항목 | 선택 |
|---|---|
| 엔진 | Godot 4.7.2 .NET (C#), Compatibility 렌더러, 안드로이드 arm64 |
| 게임 규칙 | `core/Hanjeopsi.Core` (고닷을 모르는 순수 C#, net8.0) + xUnit 테스트 |
| 데이터 | `godot/data/*.json` (어종, 장비, 요리, 손님, 대사, 수치) |
| 그림 | 수채화 배경, SD 캐릭터, 대화용 감정 일러스트, 컷신 |
| 소리 | 효과음은 `scripts/sound.py`로 합성, 배경음은 Pixabay Music |
| 글꼴 | 주아(제목) · 나눔스퀘어라운드 굵게(본문), 둘 다 SIL OFL 1.1 |
| 빌드 | GitHub Actions (`.github/workflows/android.yml`), 비밀값 필요 없음 |
| 웹 판 (기준) | Phaser 3.90.0, TypeScript, Vite |

## 폴더

```
core/
  Hanjeopsi.Core/        게임 규칙 (시세, 신선도, 낚시, 장사, 주간 상환, 이야기 장면)
  Hanjeopsi.Core.Tests/  단위 테스트, 웹과 같은지 보는 골든 테스트, 30일 자동 플레이
  golden/                웹 판에서 뽑은 기준값
godot/
  scripts/               화면(Screens), 공통 UI(Ui), 앱 상태·소리·설정(App)
  data/                  수치와 글 (JSON)
  art/ audio/ fonts/     그림, 소리, 글꼴
docs/
  STORY.md               전체 스토리
  GAME_DESIGN.md         시스템과 수치
  GODOT_PLAN.md          고닷 이전 계획과 단계
  POLISH.md              다듬기 목록
  ART_PROMPTS.md         그림 프롬프트
  ROADMAP.md             장기 계획
scripts/                 그림 자르기, 소리 합성, 사진 점검 도구
src/                     웹 판 (Phaser)
```

## 크레딧

- 글꼴: 주아 © The Jua Project Authors, 나눔스퀘어라운드 © NAVER Corporation. 모두 SIL Open Font License 1.1
- 게임 엔진: Godot Engine (MIT License), 웹 판 Phaser © Phaser Studio (MIT License)
- 배경음: Pixabay Music (Pixabay Content License)

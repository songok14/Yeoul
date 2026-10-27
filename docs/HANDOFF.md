# 이어가기 (2026-10-27, cloud, main, 다듬기 4차 뒤)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (공개). 브랜치는 main 과 ci-build(CI 결과 창고, 지우지 않는다)만 쓴다. 세션이 다른 이름의 작업 브랜치를 주더라도 main 에서 작업해 main 에 푸시한다(사용자 결정 2026-10-27). PR은 만들지 않는다. 시작하면 git fetch origin, git checkout main, git pull 후 docs/HANDOFF.md 부터 읽는다.
- main 끝: 41bb49a(코드) 다음 이 문서 커밋. 사용자에게 마지막으로 보낸 APK는 빌드 #93(41bb49a).
- 먼저 읽을 것: CLAUDE.md, docs/POLISH.md(다듬기 목록, 지금 단계의 중심), docs/GODOT_PLAN.md(단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3·6·7·8·9절과 7절 "마을 사람"·"마을 의뢰", docs/STORY.md, docs/ART_PROMPTS.md
- 단계: U0~U9, U11, U11-1~5 완료. 지금은 U12 전 다듬기(POLISH.md). 남은 큰 단계 U10(SD 다듬기 나머지), U12(폰에서 1장 끝까지).

### 지난 세션(2026-10-27)에 한 일 (다듬기 4차, 모두 main)
- SD 걷기 그림 33장(마을 사람·의뢰인 villagers/*-walk-*, 트럭 손님 customers/*-walk, 여울 yeoul/walk-*, 고등어 cat/walk-*)을 오른쪽 보기로 뒤집어 저장. 코드의 FlipH 는 "왼쪽으로 갈 때만 true"(TownScreen WalkThen·Walker, TruckScreen 손님, FishingScreen 도둑 고등어, StoryScreen CatRuns). 고등어 jump·eat·sit-side 는 여전히 왼쪽을 본다(도둑 장면은 FlipH=true 로 쿨러 쪽).
- scripts/sd_sheet.py --face=right: 머리에서 살빛이 머리 가운데보다 어느 쪽인지로 방향을 재서 뒤집는다. 이미 자른 그림은 `python3 scripts/sd_sheet.py --face=right <그림들>`. 할아버지(grandpa-walk)는 점수가 0.019로 애매하다고 나오지만 맞다.
- 낚시: 여울이 방파제 가운데(225,690), 부채꼴은 FanApex(225,560)에서 좌우 대칭(PosFor cx 고정). 고등어 왼쪽(140), 쿨러 오른쪽(318). 찌는 FishingField DrawBobber u = K*BobberScale*0.62(예전 0.9, 0.45는 너무 작았다). 추적 카드는 TownScreen.QuestSummary 한 줄. 기다리는 동안 WaitIdle(먼 물 톡 튀기, 고등어 기지개·식빵·하품), 갈매기. 파워 게이지 "최대 17.8m"는 막힌 칸 안에.
- 마을: 사람 수는 balance.json town(새벽1·아침3·점심2·저녁3·밤1, 비·안개 −1, 눈 −2, 태풍 −3), 코어 Game.VillagersWanted(). 줄 셋(Lanes −84/−112/−140), 줄마다 한 사람, 가장자리 밖에서 들어와 나가면 4~11초 뒤 새 사람. 처음 자리는 115·285·355 근처. 여울 뒤와 의뢰인 자리에서는 멈추지 않는다. 의뢰인 ! ✔ 크게 통통(ClientMark, MarkUp 130). 발밑 그림자 Ui/FootShadows(마을·낚시). 걸음마다 step 소리.
- 수첩: 설정은 탭이 아니라 제목 줄 "설정" 단추(SettingsTab 5), 탭 다섯. TabStrip 은 탭 줄 밖 번호면 노브를 숨긴다. 대화창 SD(의뢰인)는 화면 24%(최대 470px), 가로 74%.
- 테스트 129개(마을 사람 수 테스트 추가). CI 빌드 #91~#93 사진 확인.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility, 세로 1080×1920. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts, 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 세이브 클래스에 계산 속성을 두면 JSON 에 나오니 메서드로.
- 고닷은 웹과 다르게 간다. 웹 src/ 는 기준으로 남기고 골든 테스트는 TestData.Game(웹 모양)으로 본다.
- 그림 역할: 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신. 배경 수채화. 낚는 동안 물고기 그림은 안 보인다.
- SD 걷기 그림은 모두 오른쪽을 본다. 왼쪽으로 갈 때만 FlipH = true (CLAUDE.md 고닷 절).
- 바뀐 점(2026-10-27): 여울은 낚시에서 방파제 가운데에 서고 부채꼴은 좌우 대칭(예전 "여울 앞 왼쪽"). 낚시 포즈는 뒷모습 중심으로 다시 뽑는다.
- 1장은 남해 방파제(chapter1.spot). 게임 안 글은 짧고 자연스럽게. 쉼표 남발·줄표 금지.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 main 에 푸시. 끝나면 APK 파일을 보내고 폰에서 볼 것을 알려 준다.
- 사용자는 폰으로 해 보고 의견을 준다. 받으면 바로 반영하고 POLISH.md 에 줄을 붙인다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md). 그림을 주면 바로 잘라 넣고 연결한다(sd_sheet.py, sd_pose.py, clean_halo.py).
- 설계가 갈리는 건 AskUserQuestion 으로 묻고(추천안 먼저), 정하면 GAME_DESIGN 에 남긴다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy.
- 고닷은 여기서 실행 못 한다. C# 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 android.yml 이 돈다(사진 약 90장, 8~10분). 결과: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. ci-build 는 커밋 하나라 예전 빌드 사진과 비교할 수 없다.
- 기다릴 때는 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 반복(백그라운드 또는 timeout 600000). 실행 상태는 GitHub MCP actions_list.
- 사진 여러 장은 PIL 로 한 장에 이어 붙여 본다. github raw·릴리스 다운로드는 막혀 있다.

## 이번에 해 줬으면 하는 것
1. 사용자가 새 낚시 포즈 시트(ART_PROMPTS.md "SD 2-2", 2줄×4칸: ready·wait·cast·sit / bite·reel·pull·joy)를 주면: sd_sheet.py 로 잘라 sd_pose.py 로 godot/art/yeoul/fish-*.png 와 poses.json 에 넣는다(reel 은 --erase 로 대를 지운다). 지금 fish-wait 는 fish-ready 와 같은 그림이니 새 wait 를 연결한다(FishingScreen SetPose). 얼굴·후드 둘레가 잘리지 않았는지 어두운 바탕에서 확인. fishing-* 사진으로 대 끝(RodTip)과 줄이 맞는지 본다. 옛 포즈는 얼굴 앞쪽과 후드 위가 투명 작업에서 지워져 있었다(자동 메우기는 머리 둘레가 뿌옇게 번져 버렸다).
2. 사용자 폰 의견을 받으면 바로 반영하고 POLISH.md 에 줄을 붙인다. 볼 것은 GODOT_PLAN 10-2 "다듬기 4차".
3. 남은 다듬기: POLISH.md 의 [ ] 줄, U10(SD 다듬기 나머지), 그다음 U12(폰에서 1장 끝까지).
마지막: dotnet test core/Hanjeopsi.sln(129개), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 균형은 AutoPlayTests(적당히 잘함 18/20 이상, 평균 21~25일). 커밋 후 git push origin main, CI 사진 확인, APK 보내기, POLISH·GODOT_PLAN 10-2·HANDOFF 갱신.
````

사라지는 것: 컨테이너 스크래치의 CI 사진·APK는 세션이 끝나면 없어진다(APK는 릴리스 `test-latest`에 있다). 사용자가 준 원본 시트는 레포에 넣지 않는다. 잘라 넣은 그림은 레포에 있다.

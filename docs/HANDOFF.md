# 이어가기 (2026-10-27, cloud, main, 다듬기 3차·마을 퀘스트 뒤 폰 의견)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (공개). 브랜치는 main 과 ci-build(CI 결과 창고, 지우지 않는다)만 쓴다. 세션이 다른 이름의 작업 브랜치를 주더라도 main 에서 작업해 main 에 푸시한다(사용자 결정 2026-10-27). PR은 만들지 않는다. 시작하면 git fetch origin, git checkout main, git pull 후 docs/HANDOFF.md 부터 읽는다.
- 원격에 ccr-c846e49a-1tqms8, claude/name-change-issue-l462yn 브랜치가 남아 있으면 사용자가 GitHub Branches 화면에서 지운다(이 환경에서 git push --delete 는 연결이 끊겨 못 한다).
- main 끝: 9c4bb9b(코드) 다음 이 문서 커밋. 사용자에게 마지막으로 보낸 APK는 빌드 #89(9c4bb9b).
- 먼저 읽을 것: CLAUDE.md, docs/POLISH.md(다듬기 목록, 지금 단계의 중심), docs/GODOT_PLAN.md(단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3·6·7·8·9절과 7절 "마을 의뢰", docs/STORY.md, docs/ART_PROMPTS.md
- 단계: U0~U9, U11, U11-1~5 완료. 지금은 U12 전 다듬기(POLISH.md). 남은 큰 단계 U10(SD 다듬기 나머지), U12(폰에서 1장 끝까지).

### 지난 세션(2026-10-26~27)에 한 일 (모두 main 에 있음)
- 다듬기 1차: 낚시 손맛 연출, 대·줄, 가진 찌 그림(godot/art/gear/float-<단계>.png), 지친 물고기, 릴별 게이지 구간, 낚싯대별 최대 거리, species.json spots(1장 방파제 41종), 시세판 쿨러 어종 먼저, 여울식당→할머니댁, 트럭 간판·칠판 그림 손질, 상점 그림 체크무늬 지우기.
- 다듬기 2차: 던지기 단추, 여울 앞 부채꼴, 힘은 아래 게이지, 대화창 닫힐 때 아래 버튼 눌림 막기, 새 장비는 노씨 첫 부탁 뒤 판매·보상 한 단계씩, 위판장 전부 팔기 위·스크롤, 마을에서 여울이 걸어가기, 새 여울 낚시 포즈 여덟 장(sd_pose.py, poses.json), 결과 카드, 진동 세기, 트럭 메뉴 접기, 마을 간판 상태·갈매기, 수첩 쿨러 스크롤, 타이틀 그늘.
- 마을 사람 SD 넷(fisher·auntie·boy·courier)과 의뢰인 SD 다섯(nossi·sashimi·sushi·stall·minbak) godot/art/villagers/<id>-walk-1|walk-2|stand.png, dialogue.json villagers(시간대·한마디). 은행 직원 대화 일러스트(화자 banker).
- 마을 퀘스트: 의뢰인이 마을 양옆에 서서 머리 위 ! → 맡기 → "볼락 1/2" → ✔ 직접 건네기. 추적 카드, "의뢰 고기!", 수첩 의뢰 탭. 위판장은 경매만, 쿨러 줄에 한 마리씩 크기.
- 테스트 128개, 자동 플레이 적당히 잘함 20/20 평균 22일.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility, 세로 1080×1920. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts, 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 세이브 클래스에 계산 속성을 두면 JSON 에 나오니 메서드로.
- 고닷은 웹과 다르게 간다. 웹 src/ 는 기준으로 남기고 골든 테스트는 TestData.Game(웹 모양)으로 본다.
- 그림 역할: 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신. 배경 수채화. 낚는 동안 물고기 그림은 안 보인다.
- 1장은 남해 방파제(chapter1.spot). 게임 안 글은 짧고 자연스럽게. 쉼표 남발·줄표 금지.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 main 에 푸시. 끝나면 APK 파일을 보내고 폰에서 볼 것을 알려 준다.
- 사용자는 폰으로 해 보고 의견을 준다. 받으면 바로 반영하고 POLISH.md 에 줄을 붙인다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md). 그림을 주면 바로 잘라 넣고 연결한다(sd_sheet.py, sd_pose.py, clean_halo.py).
- 설계가 갈리는 건 AskUserQuestion 으로 묻고(추천안 먼저), 정하면 GAME_DESIGN 에 남긴다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy.
- 고닷은 여기서 실행 못 한다. C# 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 android.yml 이 돈다(사진 약 90장, 8~10분). 결과: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk.
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 반복. 포그라운드 sleep 금지.
- 사진 여러 장은 PIL 로 한 장에 이어 붙여 본다. github raw·릴리스 다운로드는 막혀 있다.

## 이번에 해 줬으면 하는 것 (사용자 폰 의견 2026-10-27)
1. SD 걷는 방향 통일: 마을 사람이 뒤로(등 돌린 방향으로) 걷는다. 시트마다 걷기 그림이 보는 방향이 달라서다. godot/art/villagers/*-walk-*.png, godot/art/customers/*-walk.png(트럭 손님, "식당도 마찬가지"), godot/art/yeoul/walk-*.png, godot/art/cat/walk-*.png 를 전부 열어 보고, 모두 같은 방향(오른쪽 보기로 정하길 추천)이 되게 파일 자체를 좌우 뒤집어 저장한다. 그다음 규칙을 기억해 둔다: CLAUDE.md "고닷" 절에 "SD 걷기 그림은 모두 오른쪽을 본다. 왼쪽으로 갈 때만 FlipH = true" 한 줄을 넣고, scripts/sd_sheet.py 에 걷기 그림 방향을 맞추는 옵션(예: --face=right, 자른 뒤 얼굴 쪽을 재서 뒤집기)을 더한다. 코드의 FlipH 를 이 규칙에 맞춘다(TownScreen Walker·WalkThen·의뢰인, TruckScreen 손님 _customer.FlipH = true 등). CI 사진(town, town-evening, truck-*)으로 확인.
2. 낚시 찌가 너무 크다(거의 캐릭터 만하다). FishingField.DrawFloatTex(30*u 기준)와 DrawBobber, BobberScale(날아갈 때 1.4), FishingScreen YeoulHeight(168)·SdPoses 를 보고, 찌를 줄이고 필요하면 여울 SD를 키워 둘의 크기가 자연스럽게 맞게. fishing-wait·fishing-bite·fishing-gear-wait 사진으로 확인.
3. 마을 SD가 늘 둘로 고정이다(TownScreen.AddVillagers Take(2)). 자연스럽게: 시간대·날씨별로 수가 바뀌고(새벽 적게, 아침·저녁 많이, 밤·비 적게), 화면 가장자리에서 걸어 들어와 지나가 나가고, 가끔 새 사람이 들어오게. 서로 겹치지 않게 줄을 나누고, 간판·의뢰인·여울을 가리지 않게.
4. 추가로 개선하면 좋을 것(사용자가 찾아 달라고 했다. 아래는 지난 세션에 본 후보, 고치기 전에 사용자에게 목록을 보여 주고 우선순위를 묻는다):
   - 의뢰인 머리 위 ! 표시가 작다. 더 크게, 통통 튀게.
   - 대화창의 의뢰인 SD 그림 크기와 자리(DialogueBox _sd, 화면 높이 30%)가 어색한지 확인.
   - 마을 SD 여울과 고등어가 마당에 떠 보인다(그림자 없음, U10). 발밑 그림자 타원.
   - 수첩 탭이 여섯(쿨러·의뢰·도감·미각·일지·설정)이라 좁다.
   - 낚시 화면 위 추적 카드가 여러 줄이면 바다를 가린다(한 줄로 줄이기).
   - 낚시 기다리는 동안 할 게 없다(고등어 장난, 물새, 파도 소리 등 작은 볼거리).
   - 트럭 손님 반응·마을 걷기 소리(발소리 step.ogg 를 걸음마다).
5. 끝나면: POLISH.md 줄 추가·체크, GODOT_PLAN 10-2 "폰에서 볼 것" 갱신, HANDOFF 갱신.
마지막: dotnet test core/Hanjeopsi.sln(128개), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 균형은 AutoPlayTests(적당히 잘함 18/20 이상, 평균 21~25일). 커밋 후 git push origin main, CI 사진 확인, APK 보내기.
````

사라지는 것: 컨테이너 스크래치의 CI 사진·APK는 세션이 끝나면 없어진다(APK는 릴리스 `test-latest`에 있다). 사용자가 준 원본 시트(여울 포즈, 마을 사람, 의뢰인, 은행 직원)는 레포에 넣지 않았다. 잘라 넣은 그림은 레포에 있다.

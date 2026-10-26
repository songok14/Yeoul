# 이어가기 (2026-10-26, cloud, 작업 브랜치 `ccr-c846e49a-1tqms8`, 주간 상환·상점 업데이트 뒤 폰 의견 반영)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (공개). 작업 브랜치 ccr-c846e49a-1tqms8 에서 이어서 하고, 확인이 끝나면 main 에도 올린다(git push origin ccr-c846e49a-1tqms8:main). PR은 만들지 않는다. 시작하면 git fetch origin 후 그 브랜치로 체크아웃하고 docs/HANDOFF.md 부터 읽는다.
- 브랜치와 main 끝: 256f61c 다음 이 문서 커밋(둘이 같다). 사용자에게 마지막으로 보낸 APK는 빌드 #74(256f61c). 사용자가 폰에서 해 보고 아래 의견을 줬다.
- 먼저 읽을 것: CLAUDE.md, docs/GODOT_PLAN.md(단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3절(고닷 낚시·힘싸움)·6절(하루 다섯 칸·날씨)·7절(여울호 등급·손님)·8절(고닷 장비)·9절(주간 상환), docs/STORY.md, docs/ART_PROMPTS.md
- 폰 의견 사진: docs/feedback/2026-10-26/ (market-board 위판장 시세판, truck-sign 트럭 간판 글씨, fishing-rod-line 낚싯대와 줄, shop-keep-icons 상점 그림 배경)
- 단계: U0~U9, U11, U11-1~4 완료(폰 확인은 지금 받는 중). 남은 큰 단계 U10(SD 다듬기 나머지), U12(폰에서 1장 끝까지).

### 지난 세션(2026-10-26)에 한 일 (모두 푸시됨)
- 하루 다섯 칸(새벽·아침·점심·저녁·밤), 시간대 마을 그림(WebP, time.townArt), 밤엔 미끼가게 닫음, 밤낚시(방파제 밤 그림 breakwater-night).
- 여울호: 오늘의 메뉴 고르기(메뉴 칸=등급), 등급 4단계(평판=손님 반응+납품), 손님 12명(시간대·좋아하는 요리·친밀도·단골). core/Hanjeopsi.Core/Systems/Diner.cs, godot/data/diner.json.
- 요리 열 개와 요리 그림(godot/art/dishes), 납품 의뢰 다섯 더.
- 날씨(weather.json): 맑음·비·안개·태풍·눈, 달별 확률, 효과(비 입질↑손님↓, 안개 큰 놈↑힌트↓, 태풍 낚시 금지, 눈 입질↓). 마을·방파제 날씨 그림, RainOverlay·SnowOverlay·FogOverlay.
- 상점 업데이트(gear.json, core/Hanjeopsi.Core/GameKit.cs): 장비 10단계(1~4 상점, 5~7 보상: 등급·단골·의뢰·노씨 부탁·1장 클리어, 8~10 다음 지역), 새 장비 아홉(줄·바늘·찌·미끼통·뜰채·헤드랜턴·의자·장화·통발), 미끼 열 가지(좋아하는 어종 ×2.5). 그림 160장 godot/art/gear/<종류>-<단계>.png(scripts/item_sheet.py).
- 주간 상환(weekly.json, core/Hanjeopsi.Core/GameWeek.cs): 4주, 원금 40·60·90·110만 + 주 시작 이자 3%, 연체료 10%, 연체 두 번이면 차압. 주간 목표 셋(보상), 주말 이벤트(6일 장날, 13일 대물 소문, 20일 차세라 대결 40cm), 상환 장면. STORY.md·CLAUDE.md 1장 기준을 "4주 동안 매주 나눠 낸다"로 바꿈(웹은 30일 그대로).
- 낚시 기다릴 때 의자에 앉은 뒷모습(fish-sit), 입질 때 머리 위 "!".
- 테스트 121개 통과. 자동 플레이 적당히 잘함 19/20(평균 22.9일), 초보는 2주째 차압.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility, 세로 1080×1920. 유니티 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts, 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다).
- 고닷은 웹과 다르게 간다(낚시, 71종, 다섯 칸, 여울호 등급, 날씨, 장비 10단계, 주간 상환). 웹 src/ 는 기준으로 남기고 골든 테스트는 TestData.Game(웹 모양: 다섯 종, 네 칸, 장비 셋, Diner·Weather·Weekly·Kit 없음)으로 본다. 고닷 전용 세이브 칸(diner, kit, week)은 null 이면 JSON 에 안 나온다.
- 그림 역할: 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신. 배경 수채화. 낚는 동안 물고기 그림은 안 보인다.
- 어종 이름·그림·별은 FishingPlan 도감과 같게, 규정은 그 앱 기본표(Limits.kt BASE)만.
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발·줄표 금지. 아버지 말은 STORY.md 그대로.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시하고 main 에도 올린다. 끝나면 APK 파일을 보내고 폰에서 볼 것을 알려 준다.
- 사용자는 휴대폰으로 해 보고 사진과 함께 의견을 준다. 받으면 바로 반영한다. 직접 할 일은 꼭 필요한 것만 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md). 첨부할 참고 그림도 레포에서 만들어 보내 준다. 사용자가 그림을 주면 바로 잘라 넣고 화면에 연결한다.
- 설계가 갈리는 건 AskUserQuestion 으로 묻고(추천안 먼저), 정하면 수치표를 GAME_DESIGN 에 남긴다.
- docs/GODOT_PLAN.md 단계표·10-2, docs/ROADMAP.md, docs/GAME_DESIGN.md, docs/HANDOFF.md 를 갱신한다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow·ffmpeg 는 있음). 웹을 고칠 때만 npm ci.
- 고닷은 여기서 실행 못 한다. C# 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 android.yml 이 돈다(사진 약 80장, 8~10분). 같은 브랜치에 새로 푸시하면 진행 중인 실행은 취소된다. 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 GitHub MCP actions_list.
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 반복(해시는 미리 변수에). 포그라운드 sleep 금지, 백그라운드 작업은 TaskStop 으로 멈춘다(pkill 금지).
- 사진 여러 장은 PIL 로 한 장에 이어 붙여 본다(한 번에 여러 장 열면 요청 한도).
- 음악 사이트·github raw 다운로드는 막혀 있다. 필요한 파일은 사용자에게 받는다.
- 다른 사람이 같은 브랜치나 main 에 푸시할 수 있다. 거절되면 fetch 후 merge(리베이스·강제 푸시 금지).

## 이번에 해 줬으면 하는 것 (사용자 폰 의견, 2026-10-26)
1. 위판장 시세판: 지금은 제철 어종 다섯(Game.BoardSpecies). 내 쿨러에 있는 어종 시세가 먼저 나오게(쿨러 어종 → 나머지 제철). 사진 market-board.
2. "여울식당"을 "할머니댁"으로 이름 바꾸기(사용자 제안, 확인 후 진행). 마을 간판·메뉴 버튼·대사·texts·dialogue·story·STORY.md 를 모두 찾아 바꾼다. 웹은 그대로.
3. 여울호 트럭 화면: 그림 속 간판에 원래 글씨가 비쳐 보이고 "여울호" 글자가 이상하게 얹혀 있다(사진 truck-sign). 간판 글씨 자리를 그림에서 깔끔히 지우거나(배경 그림 손질) 나무 명판을 따로 그려 얹어 예쁘고 자연스럽게. 칠판 메뉴 글씨도 함께 다듬는다.
4. 낚시 손맛이 부족하다: 입질·챔질·질주·끌어올리기 연출(진동 패턴, 화면 흔들림, 소리, 줄 떨림, 물보라)을 강하게. 어떤 순간이 밋밋한지 사용자에게 한 번 묻고 시작해도 좋다.
5. 낚싯대가 손잡이 쪽에서 끊어져 보이고 줄 방향과 대가 어색하다(사진 fishing-rod-line: 대 끝에서 줄이 옆으로 꺾임). FishingField.DrawRod/DrawLine, poses.json grip·rodDir 를 손봐서 그림 속 손잡이와 그린 대가 한 줄로 이어지고, 줄은 대 끝에서 찌(물고기) 쪽으로 자연스럽게 처지게.
6. 상점 그림 배경이 덜 지워진 것(헤드랜턴·의자 등, 사진 shop-keep-icons): godot/art/gear 의 lamp-*, chair-* 등 체크무늬 남은 그림을 다시 손질(scripts/item_sheet.py 의 바탕 지우기를 칸 안 체크무늬까지 지우게). 어두운 바탕에 올려 160장 전부 확인.
7. 낚시할 때 찌를 지금 가진 찌(KitLevel("float") 단계 그림 godot/art/gear/float-<단계>.png 또는 색)로 보여 주기. 없으면 기본 찌.
8. 물고기가 지치면(Tug.Tired) 텐션이 거의 안 오르고 쭉 끌려오게(core/Hanjeopsi.Core/Systems/Tug.cs, balance.json tug). 자동 플레이 균형 다시 확인.
9. 릴 단계에 따라 텐션 게이지의 안전·위험 구간을 다르게(ReelGauge: 드랙·Danger 를 릴 값으로, 좋은 릴은 초록이 넓고 빨강이 좁게).
10. 낚싯대 단계에 따라 던질 수 있는 최대 거리가 다르게(지금은 reach 가 1단계 0.8, 2단계부터 1). 단계마다 reach 를 늘리고 부채꼴·파워 게이지에 최대 거리 표시. 균형 확인.
11. 실제 방파제에서 잡히는 어종만: species.json 에 낚시터 필드(예: spots: breakwater/boat/deep) 를 더해 1장 방파제에서는 방파제 어종만 나오게(아귀·대구·삼치·방어 같은 선상·먼바다 고기 제외). 어떤 어종을 뺄지 표로 보여 주고 사용자 확인 후 반영. 요리·의뢰·미끼 좋아하는 어종·통발 목록과 맞는지 확인.
12. "전체적으로 어색한 부분이 많다": 새 기능을 잠시 멈추고 다듬기 단계(U12 전)로 간다. 화면마다(타이틀·마을·낚시·트럭·위판장·상점·수첩·이야기) 어색한 점 체크리스트를 docs/POLISH.md 로 만들어 사용자와 우선순위를 정하고, 사용자가 폰 사진을 주면 그 목록에 붙여 하나씩 지운다. 1~11번도 이 목록에 넣는다.
13. 보류된 것: 은행 직원 일러스트(상환 장면, 지금은 설명 글) 프롬프트 줄지, 사용자가 지난번 따로 준 여울 단독 포즈 네 장(뒷모습으로 대 든 포즈, 옆에서 당기는 포즈 등)으로 fish-ready·fish-pull 을 바꿀지. 필요하면 그 그림을 다시 첨부해 달라고 한다.
마지막: dotnet test core/Hanjeopsi.sln(지금 121개), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 균형은 AutoPlayTests(적당히 잘함 18/20 이상, 평균 21~25일). 커밋 후 git push origin ccr-c846e49a-1tqms8, CI 사진 확인, git push origin ccr-c846e49a-1tqms8:main, APK 보내기, 문서와 HANDOFF 갱신.
````

사라지는 것: 컨테이너 스크래치의 CI 사진·APK는 세션이 끝나면 없어진다(APK는 릴리스 `test-latest`에 있다). 사용자가 준 원본 시트(장비·손님·여울 포즈)는 레포에 넣지 않았다. 폰 의견 사진 네 장은 `docs/feedback/2026-10-26/`에 줄여서 넣었다.

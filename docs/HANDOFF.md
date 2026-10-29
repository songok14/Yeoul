# 이어가기 (2026-10-28 밤, cloud, 브랜치 ccr-1656a8a0-hqkk4r, 하루 네 칸·걷는 언덕 마을 뒤)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어. 끝 요약까지 꼭 한국어로.
- 설명은 간략하고 쉽게 한다. 애매하거나 모호한 부분은 질문으로 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (공개). ci-build 브랜치는 CI 결과 창고라 지우지 않는다.
- **브랜치 주의**: 2026-10-27까지는 main 에서 작업해 main 에 푸시했다(사용자 결정). 2026-10-28 세션은 세션이 준 작업 브랜치 `ccr-1656a8a0-hqkk4r` 에 올렸고 **main 에 아직 합치지 않았다**. 시작하면 git fetch origin 후 이 브랜치와 main 차이를 보고, main 에 합칠지(빨리 감기) 사용자에게 먼저 묻는다. PR은 만들지 않는다.
- 2026-10-28 밤 세션은 그 브랜치 끝(6813e03)을 이어 `ccr-86a1dc25-dssgjw` 에 올렸다(전망대 위 하늘 잇기 `ArtStage.SkyCap`, 앉은 의자를 엉덩이 너비에 맞춤 `gearlook.json chairWidth`·`poses.json hip`). 이 브랜치가 최신이고 main 에는 아직 안 합쳤다. 마지막 APK는 빌드 #131(464072d). 아침·저녁 지도를 맞바꿨다(주황 해돋이가 아침, 분홍·연보라가 저녁). 여울 위아래 걷기는 `scripts/sd_step.py` 로 만든 임시 두 장(SD 3-1 오면 교체). 전망대 위 하늘은 지도 속 하늘 조각을 이어 붙인 그림(`scripts/town_sky.py`, 지도를 바꾸면 다시 돌린다).
- 먼저 읽을 것: CLAUDE.md, docs/GAME_DESIGN.md 6·7절(새 하루, 여울호 손님 등급), 9절(이야기 장면), docs/GODOT_PLAN.md 9절 단계표(U11-6, U11-7)와 10-2 "지금 APK에서 볼 것", docs/ART_PROMPTS.md 끝 "걷는 언덕 마을"과 "대화 4. 미식 평론가", docs/POLISH.md.

### 2026-10-28 세션 뒷부분 (하루 네 칸, 걷는 언덕 마을)
- 사용자 결정: 하루를 **아침·점심·저녁·밤 네 칸**으로(데이브 더 다이브의 아침·오후 잠수 → 저녁 스시 바 → 밤 잠수). 아침·점심 낚시(경매는 아침·점심), 저녁 장사만, 밤낚시는 8일 노씨 장면 뒤, 미끼가게는 저녁까지. balance.json time slotsPerDay 4, fishSlots [0,1,3], truckSlots [2]. 어종 시간대 가중치는 species.json 다섯 칸(웹 골든) 그대로 두고 time.speciesSlots [[0,1],[2],[3],[4]]로 잇는다(코어 FishingRoll.SlotWeight). 힌트의 새벽은 아침, 낮은 점심. 출조당 캐스팅 5번. 자동 플레이 20/20 평균 21.6일.
- 걷는 언덕 마을(U11-7): 지도 네 장 godot/art/backgrounds/town-map-morning·day·dusk·night.webp(941×1672, 서로 배치가 같다. 사용자가 고른 S자 길·한옥·광장 가로등·오른쪽 아래 방파제와 빨간 등대). 길 그물 godot/data/townmap.json(점 26, 장소 6, 의뢰인 자리 2, 확대 1.4, 원근 0.74~1.06), 코어 Systems/TownPath(가장 가까운 길 위 점, 다익스트라, ScaleAt, 테스트 4), ArtStage.Focus·ClampPins, TownScreen 개편(땅 누르면 걷기, 간판 누르면 문 앞까지 걸어가 들어가기, 돌아오면 그 문 앞, 위로 오르면 back·내려오면 front 그림, 마을 사람은 길 점 사이를 오감, 깊이 순 그리기, 날씨는 색·효과만). 사진 town-walk·town-top·town-sea.
- 고친 것: HUD 시간 칩이 다섯 칸을 가정해 네 칸에서 마을이 멈추던 것(빌드 #120 실패 원인).
- 그림 프롬프트 흐름: 약도(docs/art/town-layout.png, scripts/town_layout.py)를 붙여 다시 그리면 배치가 잘 맞았다. 시간대 그림은 "relighting task only" 편집 프롬프트로 같은 배치를 지켰다(ART_PROMPTS "노을 지도에서 시간만 바꾸기").

### 2026-10-28 세션 앞부분
- 사용자 요청: 데이브 더 다이브(인 정글)처럼 마을을 직접 걸어 NPC를 찾아가고, 새벽·아침·낮은 낚시, 저녁은 장사만, 손님 수는 친밀도와 명성으로.
- 하루 개편(코어 Game.IsFishingTime/CanFishNow/IsNight/NightFishingOpen/RestUntilTruck, balance.json time fishSlots [0,1,2,4]·truckSlots [3]·nightFishBeat "nossiLamp"): 새벽·아침·낮 낚시 → 저녁 여울호 장사만 → 밤낚시(8일 노씨 케미라이트 장면 `nossiLamp` 뒤) → 잠. "점심"은 "낮"으로 이름을 바꿨다. 할머니댁 "저녁까지 쉬기", 트럭 메뉴판 "장사 쉬기"(쿨러가 비었을 때).
- 손님 개편(코어 Systems/Diner.cs, data/diner.json): 명성(평판)이 손님 수(포장마차 4명 → 줄 서는 집 10명, 등급 사이는 평판만큼 한 명씩), 친밀도가 등급. 등급 네 단계: 일반 / 단골(친밀도 5, 늘 좋아하는 요리, 20% 팁, "단골" 표) / VIP(그날 아침 예약, 등급별 확률, 예약한 요리를 메뉴와 상관없이 정해진 신선도로, 팁과 명성, 못 내면 명성 깎임. 넷: 맛집 블로거·호텔 주방장·요트 손님·여행 작가, 그림은 손님 그림을 빌려 씀) / 특별 손님(차세라 16~19일, 첫 만남 뒤 한 번, 대화창 일러스트로). PlanGuests 는 List<PlannedGuest>(Kind, Wish). 마을 추적 카드와 트럭 메뉴판에 VIP 예약이 보인다.
- 마을 간판: 방파제 "저녁엔 장사"/"밤엔 위험"/"밤낚시", 여울호 "저녁에 열어요"/"손님 N"/"오늘 장사 끝".
- 테스트 139개 통과. 자동 플레이 적당히 잘함 20/20 클리어, 평균 22.8일(목표 21~25일). VIP 성공은 자동 플레이 기준 약 30%.
- CI 사진 새로: truck-booked, truck-vip, truck-sera, story-lamp. 트럭 사진은 모두 --demo=evening.
- 그림 프롬프트를 사용자에게 줬다(ART_PROMPTS 끝): 지도 1 언덕 마을 낮(town-map-day, 9:16 최고 해상도, 45도 내려다봄, S자 길), SD 3-1 여울 앞·뒤 걷기, SD 3-2 고등어 앞·뒤 걷기, 손님 3 VIP 넷 SD, 대화 4 미식 평론가 표정.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility, 세로 1080×1920. 돈 드는 것 금지. 규칙은 core/, 화면은 godot/scripts, 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 웹 src/ 는 기준으로 남기고 골든 테스트는 TestData.Game(웹 모양, 새 시간 제한은 꺼 둔다)으로 본다.
- 그림 역할: 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신. 배경 수채화. SD 걷기 그림은 오른쪽을 본다.
- 2026-10-28 사용자 결정(바뀐 점):
  - 걷는 마을은 **B 세로 언덕 마을**: 내려다본 마을 지도 한 장(남해 언덕 마을, 아래 항구 → S자 길 → 위 전망대)을 확대해 위아래(조금 좌우)로 흐르게. 가로 긴 거리(A, 세로 화면에서 앞이 안 보임)와 구역 여러 장(C, 이어진 느낌이 덜함)은 접었다.
  - 조작은 누른 곳으로 걷기(기본값으로 정함). 마을을 걷거나 이야기하는 동안은 시간이 흐르지 않는다(인 정글의 시간 제한이 쫓긴다는 평 때문).
  - 밤낚시는 1장 중간(8일)에 연다(데이브 더 다이브는 2장에서 엶).
  - 손님은 명성=수, 친밀도=등급, 네 단계 그대로. 마을 사람과 여울호 손님은 같은 사람들로 합친다(손님 12명이 마을에 산다. 마을에서 말 걸기·부탁으로도 친밀도).
  - 순서: 규칙 먼저(끝남), 걷는 마을은 그림이 오면.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, CI 사진 직접 보고 고치기까지 한 뒤 푸시. 끝나면 APK 파일을 보내고 폰에서 볼 것을 한국어로 알려 준다.
- 사용자는 폰으로 해 보고 의견(사진)을 준다. 받으면 바로 반영하고 POLISH.md 에 줄을 붙인다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED, 영어 본문. 각도·보는 방향을 아주 구체적으로. 그림을 주면 바로 잘라 넣고 연결한다(sd_sheet.py, sd_pose.py, clean_halo.py).
- 설계가 갈리는 건 AskUserQuestion 으로 묻고(추천안 먼저), 정하면 GAME_DESIGN 에 남긴다. 잘 모르겠다고 하면 예시 그림을 만들어 보여 준다(이번에 마을 보기 세 가지 예시를 PIL 로 그려 보냈다).

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy.
- 고닷은 여기서 실행 못 한다. C# 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true.
- CI: core/, godot/, 워크플로를 바꾼 푸시(어느 브랜치든, ci-build 빼고)에 android.yml 이 돈다(사진 약 100장, 8~10분). 결과: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. BUILD.txt 에 빌드 번호·브랜치·커밋.
- 기다릴 때는 Bash 백그라운드로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 반복. 포그라운드 sleep 금지. 실행 상태는 GitHub MCP actions_list.
- 사진 여러 장은 PIL 로 한 장에 이어 붙여 본다. github raw·릴리스 다운로드는 막혀 있다. 웹 검색은 된다.

## 이번에 해 줬으면 하는 것
0. 브랜치: main 에 합칠지 먼저 묻고 정한다.
0-1. 걷는 마을 폰 의견(빌드 #123): 걷는 속도·확대(1.4)·원근·간판 자리·누르기 편한지. townmap.json 수치로 맞춘다.
1. 사용자 폰 의견(빌드 #115, 하루·손님 개편)이 오면 그것부터. 특히 하루 수입과 손님 수가 적당한지, VIP 예약이 보이고 맞추기가 너무 어렵지 않은지.
2. 그림이 오면 연결:
   - (지도·걷는 마을은 끝났다. 지도를 더 큰 파일로 받으면 같은 이름으로 바꿔 넣고, 배치가 달라졌으면 townmap.json 좌표를 다시 잰다: 길 색 마스크로 재는 방법은 이번 세션처럼 PIL 로 밝고 채도 낮은 화소를 걸러 본다.)
   - SD 3-1/3-2(앞·뒤 걷기): sd_sheet.py 로 잘라 godot/art/yeoul/, godot/art/cat/ 에 walk-front-1/2, walk-back-1/2 를 넣고 TownScreen.StepYeoul 의 위·아래 걸음(지금은 back·front 한 장)을 두 장 걷기로 바꾼다. 고등어도.
   - VIP 넷: godot/art/customers/<id>-stand|walk|happy|sad.png 로 자르고 diner.json vips art 를 자기 id로.
   - 미식 평론가: portraits/critic*, dialogue.json speakers critic, diner.json specials 에 넣는다(날은 사용자와 정하기, 예: 24~27일).
3. 마을 사람과 손님 합치기(사용자 결정): 지금 마을을 걷는 SD(villagers 아홉)와 트럭 손님(customers 열둘)이 다르다. 손님 열둘이 마을을 걷게 하고, 말 걸기 → 친밀도 +1(하루 한 번) 코어 규칙(Diner)을 넣는다. 손님 앞·뒤 걷기 그림은 필요해 보이면 그때 프롬프트를 준다.
마지막: dotnet test core/Hanjeopsi.sln(139개 이상), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 균형은 AutoPlayTests(적당히 잘함 18/20 이상, 평균 21~25일). 커밋 후 푸시, CI 사진 확인, APK 보내기, GAME_DESIGN·GODOT_PLAN 10-2·HANDOFF 갱신.
````

사라지는 것: 스크래치의 CI 사진·APK, 마을 보기 예시 그림(town-options.png)은 세션이 끝나면 없어진다. APK는 릴리스 `test-latest`에 있다.

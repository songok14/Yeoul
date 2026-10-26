# 이어가기 (2026-10-26, 작업 브랜치 `ccr-c846e49a-1tqms8`, 하루 다섯 칸·요리 그림 뒤)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 원격 https://github.com/songok14/Yeoul (공개 레포). 작업 브랜치 ccr-c846e49a-1tqms8 에서 이어서 하고, 확인이 끝나면 main 에도 올린다(git push origin ccr-c846e49a-1tqms8:main). PR은 만들지 않는다. 시작하면 git fetch origin 후 그 브랜치로 체크아웃.
- 브랜치와 main 끝: 530f5a5 다음 이 문서 커밋(둘이 같다). main 의 PR #1(밤·비 배경, RainOverlay, --weather)과 #2(WebP 우선 로드)는 이미 합쳤다.
- 사용자에게 마지막으로 보낸 APK는 빌드 #58(530f5a5). CI 사진은 모두 직접 봤다(겹침 없음).
- 먼저 읽을 것: CLAUDE.md, docs/GODOT_PLAN.md(단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3절(고닷 낚시·힘싸움)·4절(어종 71종)·6절(하루 다섯 칸)·7절(요리)·9절, docs/STORY.md, docs/ART_PROMPTS.md
- 단계: U0~U9, U11, U11-1 완료(U5~U11-1 폰 확인 대기). 남은 큰 단계 U10(SD 다듬기), U12(폰에서 1장 끝까지).

### 이번 세션(2026-10-26)에 한 일
- main 합치기(TownScreen·GameState 겹침 해결). 사용자가 GPT로 뽑은 WebP 마을 그림 일곱 장을 쓴다.
- 하루 다섯 칸: 새벽·아침·점심·저녁·밤(texts.json slots, balance.json time). 사용자가 정한 그림 짝: 새벽 village-2, 아침 village-0, 점심 village-1, 저녁 village-3, 밤 village-night(time.townArt, Game.TownArt()). 옛 village-0~3.png 와 scripts/village_time.py 는 사용자 요청으로 지웠다. 타이틀·이야기도 ArtStage.LoadBackground(WebP 먼저, 없으면 PNG).
- 시간대별 행동: 경매 새벽·아침, 트럭 아침부터(손님 0·4·6·5·3, truck.customersBySlot, Game.CustomersNow()), 미끼가게는 밤에 닫음(time.shopLastSlot 3, Game.IsShopOpen(), BuyBait·UpgradeGear 도 거절). 밤엔 "밤낚시". 어종 slot 가중치를 다섯 칸으로(밤 무리표는 GAME_DESIGN 4절), 힌트의 "낮"은 "점심"으로.
- 균형: 캐스팅은 출조 한 번에 4번(하루 총량 12번은 예전과 같다). 자동 플레이 적당히 잘함 20/20, 평균 23.6일. 초보 0/20.
- 새 그림 속 푸드트럭을 여울호로 쓴다. 소품 트럭은 뺐고, 그림마다 트럭 자리(TownScreen.TruckArea, 비 두 장은 TruckAreaByArt)에 보이지 않는 누름 영역과 "여울호" 간판.
- 그린 낚싯대가 안 그려지던 버그(FishingScreen.UpdateRod 를 아무도 안 부르고 자기 자신을 부름)를 고쳤다. fishing-reel 사진은 첫 질주 전에 찍히게 2.3초 늦게 건다.
- 밤낚시: 방파제 낮 그림을 구름까지 푸르게 칠한다. godot/art/backgrounds/breakwater-night.(png|webp) 가 생기면 그 그림을 쓴다(프롬프트는 ART_PROMPTS 배경 3).
- 새 요리 다섯(참돔회, 도다리쑥국[짠맛·쓴맛], 갑오징어숙회, 우럭매운탕, 농어구이)과 같은 어종 납품 의뢰 다섯.
- 요리 그림 열 장: 사용자가 준 시트 두 장을 scripts/dish_sheet.py 로 잘라 godot/art/dishes/<요리 id>.png. 트럭 메뉴판(못 낚은 고기는 그림자), 간 맞추기 위, 결과에 나온다(PlaceScreen.DishIcon).
- 테스트 93개(TimeTests 추가). 웹 골든은 TestData.Game 에서 웹 모양(네 칸, 캐스팅 6, 웹 요리·의뢰, 가게 언제나 열림)으로 되돌려 본다.
- 문서: GAME_DESIGN(힘싸움 수치표, 어종 71종 필드와 밤 무리표, 다섯 칸 표, 요리 표, 균형), GODOT_PLAN(U11-1, 10-2), ROADMAP, ART_PROMPTS(날씨 배경 2-1~2-4, 방파제 밤, 요리 시트 1·2 받음).

### 확인 필요
- 폰: 다섯 칸 칩, 시간대 그림 바뀜, 트럭 한 대와 누르기, 밤 미끼가게 닫힘·밤낚시 어종, 캐스팅 4번 체감, 새 요리 다섯과 요리 그림, 힘싸움 손맛, 그린 낚싯대, 진동, 도감 71종. (GODOT_PLAN 10-2)
- 도다리쑥국의 쓴맛은 아직 미각 해금 전이라 ? 로 나온다(미각 4단계는 ROADMAP 남은 일). 괜찮은지 사용자 의견.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 웹 계산을 바꾸면 npm run golden.
- 고닷은 웹과 다르게 간다: 낚시(부채꼴·고등어 지점·힘싸움), 어종 71종, 하루 다섯 칸, 요리 열 개. 웹 src/ 는 다섯 어종·네 칸·옛 낚시 그대로 기준으로 남긴다.
- 마을 시간대 그림 짝(위)은 사용자가 정했다. 비는 게임 날씨가 아직 없다(사용자가 "아직 비 안 넣음"으로 정함). 그림과 RainOverlay 는 사진 확인용 --weather=rain|rain-dusk 로만.
- 그림 역할: 화면 위에서 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신(아래 30% 글 상자). 배경은 수채화. 낚는 동안 물고기 그림은 보이지 않는다.
- 어종 이름·그림·별은 FishingPlan 도감과 같게, 규정은 그 앱 기본표만.
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.
- 배경음은 Pixabay 무료 곡, 효과음은 합성.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시. 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 사용자는 폰에서 직접 해 보고 사진과 함께 의견을 준다. 받으면 바로 반영한다. 휴대폰으로 작업할 때가 많아 직접 할 일은 꼭 필요한 것만 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md 형식). 사용자가 그림을 주면 바로 잘라 넣고 화면에 연결한다.
- 시간대별 물고기·행동을 더 넓히고 싶어 한다(이번에 다섯 칸으로 시작). 날씨(비·눈·태풍)는 나중에.
- docs/GODOT_PLAN.md 단계표, docs/ROADMAP.md 체크박스, docs/GAME_DESIGN.md 표, docs/HANDOFF.md 를 갱신한다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow·ffmpeg 는 있음). 웹을 고칠 때만 npm ci.
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 .github/workflows/android.yml 이 돈다(사진 약 65장, 8~10분). 같은 브랜치에 새로 푸시하면 진행 중인 실행은 취소된다. 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 GitHub MCP actions_list(ToolSearch로 불러옴).
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 를 반복한다. 포그라운드 sleep 은 막힌다. 백그라운드 작업은 pkill 말고 TaskStop 으로 멈춘다(pkill 은 자기 셸까지 죽였다).
- 음악·소리 사이트와 github.com raw 다운로드는 네트워크가 막혀 있다. 필요한 파일은 사용자에게 받는다.
- 사진 여러 장을 한꺼번에 열면 요청 한도에 걸린다. 여러 사진은 PIL 로 한 장에 이어 붙여 본다.
- 다른 사람이 같은 브랜치나 main 에 푸시할 수 있다(사용자가 GPT 세션에서 PR #1·#2 를 올렸다). 푸시가 거절되면 fetch 후 merge(리베이스·강제 푸시 금지).

## 이번에 해 줬으면 하는 것
1. 사용자가 폰(빌드 #58)에서 본 의견이 있으면 먼저 반영하고 CI 사진으로 확인한다.
2. 사용자가 그림을 주면 바로 넣는다: 방파제 밤(breakwater-night, 코드는 준비됨), 날씨 배경(village-snow 등, 날씨 규칙은 그때 사용자와 정한다), SD 시트.
3. 시간대 확장 다음 걸음을 사용자와 정한다(예: 시간대별 손님 종류, 밤 전용 이벤트, 물때). 정하면 코어 규칙·데이터·테스트·자동 플레이 균형(적당히 잘함 18/20 이상, 평균 22~25일)을 같이 맞춘다.
4. 그다음: U10(여울 SD 17포즈 시트의 앉아 기다리기·놀람 포즈를 낚시 대기·입질에. 시트는 다시 첨부 부탁), 컷신 전용 그림(ART_PROMPTS 컷 1~8), U12 폰 1장 통 플레이.
마지막: dotnet test core/Hanjeopsi.sln(지금 93개), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push origin ccr-c846e49a-1tqms8, CI 사진 확인, git push origin ccr-c846e49a-1tqms8:main, APK 보내기, 문서와 HANDOFF 갱신.
````

사라지는 것: 컨테이너 스크래치 폴더의 CI 사진과 APK는 세션이 끝나면 없어진다. APK는 릴리스 `test-latest`에서 다시 받을 수 있다. 사용자가 준 요리 시트 원본은 레포에 넣지 않았다(잘라 낸 PNG만 있다).

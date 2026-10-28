# 그림 프롬프트

이미지 AI에 그대로 붙여 넣는 프롬프트 모음.

## 지금 방향 (2026-10 고닷 이전부터)

| 쓰임 | 그림체 | 비고 |
|---|---|---|
| 배경 | 수채화풍 일러스트 (`public/assets/bg-*.webp`) | 지금 그림을 그대로 쓴다 |
| 인게임 캐릭터·물체 | 귀여운 SD (2~2.5등신) | 화면 위에서 움직이는 것. 여울, 고양이 고등어, 막순 할머니, 노씨, 차세라, 손님, 트럭 여울호, 물고기, 요리 |
| 대화 | 애니메이션 채색 일러스트 (스탠딩, 감정별) | 누군가와 이야기할 때 대화창 옆에 크게. 머리부터 허리까지 보인다 |
| 컷신 | 애니메이션 채색 일러스트 (9:16 한 장) | 감정이 터지는 순간. 여울 감정 연기를 더 섬세하게 다듬는다 |

**정한 것 (2026-10-22)**: 인게임 캐릭터는 SD, 감정과 대화는 일러스트. 그래서 SD 3(여울 표정 시트)은 뒤로 미루고, 아래 "대화 일러스트" 시트를 먼저 뽑는다.

### 주는 방식 (규칙)
- 그림 **한 장당 코드 블록 하나**. 블록 하나만 복사해서 붙여 넣으면 되도록 매번 모든 조건을 다시 적는다.
- 붙여 넣을 때 **참조 그림을 같이 첨부**한다. 배경 일러스트 한두 장(색감과 빛 참고)과 그 캐릭터의 지금 그림(디자인 참고).
- SD는 **시트형**: 한 장에 여러 포즈·표정을 칸마다 넉넉히 띄워서. 모든 칸은 같은 크기 비율, 같은 바닥선.
- 시트 배경은 **연한 회색 단색 #EDEDED**. 투명 체크무늬, 글자, 라벨, 숫자, 테두리, 칸 선, 바닥 그림자 금지. (`scripts/art.py`가 가장자리와 이어진 밝은 무채색을 지운다)
- 빛은 모든 그림에서 **왼쪽 위**에서. 배경 그림의 맑은 남해 오후 빛과 맞춘다.
- 뼈대 애니메이션용 **부품 분리 시트**는 따로 뽑는다.
- 컷신은 세로 9:16 한 장 그림. 아래 30%는 글 상자가 올라가니 단순하고 차분하게 둔다.

### 캐릭터 고정 설정
- **한여울**: 29살 전직 수셰프. 화재 뒤 미각을 잃었다. 긴 갈색 머리를 높이 올린 헝클어진 똥머리(잔머리 몇 가닥), 갈색 눈, 크림빛 흰 오버핏 후드 재킷(토글 단추), 흰 티셔츠, 파란 포인트가 있는 검은 하네스 가방, 짙은 갈색 카고 조거 바지, 회백색 운동화. 표정은 조용하고 조금 지친 편.
- **고등어**: 은회색 고등어무늬(짙은 줄무늬) 길고양이. 가슴·주둥이·발은 흰색, 연두빛 노란 눈, 분홍 코와 귀 안쪽. 빨간 목걸이에 작은 금방울, 그리고 **목걸이에 묶인 낡은 수제 나무 찌**(빛바랜 빨강·흰색. 설정상 D.Y. 이니셜이 있지만 그림에는 글자를 넣지 않는다).

### 받은 손님 디자인 (2026-10, 일러스트)
손님 전신 시트·반응 얼굴 시트·요리 시트를 받았다. SD로 바꿀 때 아래 디자인을 그대로 옮긴다.
1. 낚시꾼 아저씨: 물고기 그림 남색 캡, 수염, 남색 후드 위 카키 낚시 조끼와 주황 멜빵, 장화, 낚싯대와 파란 쿨러
2. 젊은 엄마와 아이: 똥머리, 베이지 니트와 긴 치마, 에코백 / 양갈래 꼬마, 분홍 후드, 토끼 가방
3. 고등학생: 긴 생머리, 흰 셔츠와 빨간 리본, 남색 조끼, 체크 치마, 백팩
4. 집배원: 안경, 남색 캡, 빨강·남색 조끼, 우편 가방
5. 할아버지: 베이지 버킷햇, 흰 수염, 녹색 조끼와 체크 셔츠, 지팡이
6. 관광객 언니: 밀짚모자와 남색 리본, 긴 웨이브 머리, 파란 꽃무늬 원피스, 카메라
- 요리 시트의 볼락구이는 볼락보다 감성돔처럼 보이고, 감성돔 숙성회는 껍질이 등푸른 생선처럼 보인다. SD 요리를 뽑을 때 바로잡는다.

---

### 받은 SD 시트
- 2026-10-27: **차세라 표정 시트**(대화 4)와 **컷 9 고등어 찌·컷 10 차세라 첫 만남**. 옷은 파랑 대신 빨강·흰 바람막이에 검은 탱크톱, 빨간 고글로 나와 그대로 쓴다(앞으로 차세라 그림은 이 디자인). 시트는 칸 사이가 고르지 않아 칸을 직접 잘라 `sd_sheet.py`(1×1)와 `clean_halo.py --fringe`로 `portraits/sera*.png`에. 컷은 `godot/art/cuts/`.
- 2026-10-27: **뒤에서 본 릴 열 개 다시**(두 번째 시트: 릴 다리 위, 스풀 너머, 핸들 왼쪽). `godot/art/gear/reel-back-N.png`, 다리 꼭대기 가로 위치는 `data/gearlook.json reelFoot`. 낚시 화면에서 손 위 대 아래에 매단다. 앞 시트(스풀 앞면이 보이던 것)는 버렸다.
- 2026-10-27: **여울 낚시 포즈 다시**(SD 2-2, 뒷모습 중심 2×4). `sd_sheet.py`로 자르고 `godot/art/yeoul/fish-*.png`, `poses.json`에 넣었다. 테두리가 또렷해 `clean_halo.py`는 쓰지 않았다(쓰면 가는 낚싯대가 끊긴다). 감기는 손 위 대를 지웠다. 놀람(bite) 옆 주황 표시는 잘릴 때 빠진다.
- 2026-10-22: **손님 SD 시트 두 장**(6명 × 서기·걷기·기쁨·실망). `scripts/sd_cells.py`로 잘라 `godot/art/customers/<id>-<포즈>.png`. 두 번째 시트는 체크무늬가 박혀 있어도 지워진다. 흰 수염 할아버지는 `--hair=0`.
- 2026-10-27: **마을 의뢰인 SD 2**(노씨·바다횟집 사장·읍내 일식집·포장마차 아저씨·민박집 할머니)를 `godot/art/villagers/{nossi,sashimi,sushi,stall,minbak}-*.png`로 넣고 `requests.json sd`를 바꿨다.
- 2026-10-26: **마을 사람 SD 시트**(어부·위판장 아주머니·초등학생·택배 기사 × 걷기 둘·서기)를 `scripts/sd_sheet.py`로 `godot/art/villagers/`에, **은행 직원 표정 시트**(여섯 표정)를 `godot/art/portraits/banker*.png`로 넣었다.
- 2026-10-26: **여울 낚시 SD 포즈 두 장 다시**(체크무늬가 박힌 시트). 서서 기다리기(뒷모습), 의자에 앉기(뒷모습), 입질에 놀라기(앉아서 옆), 던지기(뒤에서 머리 위로), 감기(뒷모습, 대는 게임이 그린다), 버티기(옆, 휜 대), 기뻐하기(정면) 여덟 장을 `scripts/sd_pose.py`로 넣고 `clean_halo.py --fringe`로 정리했다. 붙어 있던 두 포즈는 손으로 나눴다.
- 2026-10-22: **여울 낚시 SD 포즈**(15포즈·17포즈·6포즈 시트, 옆모습 당기기). 던지기·버티기·기쁨 셋을 넣었다. 17포즈 시트의 앉아 기다리기, 놀람(!), 엎드려 졸기는 아직 안 썼다.
- 2026-10-22: **여울 일러스트 일곱 장 고해상도 판**(사용자가 다듬음). 웹 900, 고닷 1400 높이.
- 2026-10: **SD 1 여울 기본 시트**, **SD 5 고등어 시트**. 디자인과 찌가 잘 나왔다. `scripts/sd_sheet.py`로 여울 8장, 고등어 12장을 잘라 `godot/art/`에 넣었다.
- 머리카락 끝에 회색 배경이 조금 남는다. 다음 시트는 같은 프롬프트로 뽑되, 머리 둘레가 배경과 또렷이 갈리면 더 깔끔하다.

## SD 1. 여울 기본 시트 (정면·옆·뒤·걷기)

```
Create one game character sheet. Attached images: watercolor background illustrations from my game (use them only for color palette and lighting) and the current illustration of the heroine (use it for her design).

Character: Han Yeoul, a 29-year-old Korean woman, former sous-chef. Long brown hair pulled up into a high messy bun with a few loose strands, brown eyes, calm and slightly tired face. Oversized cream-white hooded jacket with toggle buttons, plain white t-shirt, black crossbody harness bag with small blue accents, dark brown cargo jogger pants with side pockets, chunky white-gray sneakers.

Style: cute chibi SD character, 2.5 heads tall, big head, small rounded body, short limbs, soft anime face with big gentle eyes, clean thin dark brown line art, soft cel shading with a light watercolor texture, warm sunny Korean seaside colors that match the attached backgrounds, light always coming from the upper left.

Layout: 2 rows x 4 columns, every figure full body, exactly the same scale, all standing on the same baseline, generous empty space between figures so nothing touches or overlaps.
Row 1: front view standing relaxed, three-quarter front view, side view facing left, back view.
Row 2: idle pose with hands in jacket pockets, waving hello with a small smile, walking side view facing right with right foot forward, walking side view facing right with left foot forward.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow.
```

## SD 2. 여울 동작 시트 (낚시·요리)

```
Create one game action sheet. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the heroine (her design).

Character: Han Yeoul, a 29-year-old Korean woman, former sous-chef. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket with toggle buttons, white t-shirt, black crossbody harness bag with small blue accents, dark brown cargo jogger pants, chunky white-gray sneakers.

Style: cute chibi SD, 2.5 heads tall, big head, small rounded body, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, warm sunny seaside colors matching the attached backgrounds, light from the upper left.

Props: a simple float fishing rod with a small spinning reel (thin, dark gray), a red and yellow fishing float, a small black frying pan, a white ceramic plate.

Layout: 2 rows x 4 columns, full body, same scale, same baseline, all facing left in side or three-quarter view, generous empty space between poses, the fishing rod must stay inside each pose's own space.
Row 1: casting the rod overhead with both hands, sitting on a small stool holding the rod and watching the float calmly, striking with the rod jerked up and eyes sharp, reeling with the rod bent and leaning back with effort.
Row 2: holding up a fish proudly with both hands and a bright smile, shoulders dropped holding an empty line after the fish got away, pinching salt over a frying pan with careful focus, serving a plate forward with both hands and a nervous smile.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow.
```

## SD 2-2. 여울 낚시 포즈 다시 (방파제 가운데, 2026-10-27)

지금 낚시 포즈 여덟 장은 투명하게 만들 때 얼굴 앞쪽(이마·뺨)과 후드 위가 배경과 함께 지워졌다. 그리고 여울이 방파제 가운데 서서 좌우 대칭 부채꼴로 던지도록 바꿨다(사용자 결정 2026-10-27). 그래서 **뒤에서 본 모습 중심**으로 다시 뽑는다. 지금 여울 SD(기본 시트)와 방파제 배경을 같이 첨부한다.

받으면: `python3 scripts/sd_sheet.py <시트> 2 4 <임시 폴더> ready,wait,cast,sit,bite,reel,pull,joy`로 자르고, 한 장씩 `python3 scripts/sd_pose.py <그림> fish-<이름> --body=<몸 오른쪽 끝 x>`로 `godot/art/yeoul/`와 `poses.json`에 넣는다(감기 `reel`은 `--erase`로 대를 지운다. 게임이 대를 그린다). 얼굴과 후드 둘레는 어두운 바탕에 올려 눈으로 확인한다.

```
Create one game character pose sheet for a mobile fishing game. Attached images: the current chibi SD sheet of the heroine (keep exactly her design and the same chibi style) and the watercolor breakwater background (use it only for colors and light).

Character: Han Yeoul, a 29-year-old Korean woman. Long brown hair pulled up into a high messy bun with a few loose strands, brown eyes, oversized cream-white hooded jacket with toggle buttons, white t-shirt, black crossbody harness bag with small blue accents, dark brown cargo jogger pants with side pockets, chunky white-gray sneakers.

Scene idea: she stands in the middle of a seawall, facing the open sea, so the player mostly sees her from behind. She is right-handed. The fishing rod is thin, dark and straight unless bent by a fish, held in front of her and pointing up and forward toward the sea, leaning a little to the right so it never hides behind her head. The whole rod and its tip must stay inside each pose's own cell. No fishing line, no float, no fish, no water, no ground.

Style: cute chibi SD, 2.5 heads tall, big head, small rounded body, soft anime coloring, soft cel shading with a light watercolor texture, light from the upper left. A clean continuous dark brown outline around the whole silhouette, including her face, cheeks and the white hood, so every light part separates clearly from the background.

Layout: 2 rows x 4 columns, full body with both feet visible, exactly the same scale, all on the same baseline, wide equal spacing so nothing touches.
Row 1:
1. ready: standing back view, both hands holding the rod at waist height, rod pointing up and forward.
2. waiting: standing back view, relaxed, rod in the right hand pointing up and forward, left hand in the jacket pocket, head tilted slightly.
3. casting: back view, rod swung back over her right shoulder with both hands, about to throw forward.
4. sitting: sitting on a small black folding camp stool, back view, rod held up and forward with both hands.
Row 2:
5. bite: sitting on the same stool, three-quarter back view turned slightly to the right, startled, gripping the rod, a small part of her surprised face visible.
6. reeling: standing back view, leaning back a little, right hand turning the reel and left hand on the grip, rod pointing up and forward.
7. fighting: three-quarter back view turned slightly to the right, feet planted wide, leaning back hard, rod bent strongly up and to the right.
8. joy: front view facing the viewer, big happy smile, rod in the right hand, left fist raised.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow.
```

## SD 2-3. 낚시 의자와 릴을 가진 장비로 (2026-10-27)

낚시 화면의 의자와 릴이 지금은 포즈 그림에 박혀 있어 늘 같은 모양이다. 의자는 여울을 "의자 없이 앉은" 포즈로 다시 뽑고 그 아래에 가진 의자 그림(뒤에서 본 것)을 놓는다. 릴은 그림 속 릴 위에 가진 릴 그림(뒤에서 본 것)을 덮는다. 세 장 모두 상점 그림(`godot/art/gear/chair-*.png`, `reel-*.png`)과 지금 여울 낚시 포즈 시트를 첨부한다.

받으면: 의자 없는 포즈는 `sd_sheet.py`로 잘라 `fish-sit`, `fish-bite`를 바꾸고 손 위 대를 지운다(대는 게임이 그린다). 의자는 `godot/art/gear/chair-back-<단계>.png`, 릴은 `godot/art/gear/reel-back-<단계>.png`로 자른다.

### SD 2-3-1. 의자 없이 앉은 여울 (두 포즈)

```
Create one game character pose sheet for a mobile fishing game. Attached: the current chibi SD fishing pose sheet of the heroine (keep exactly her design, scale and chibi style).

Character: Han Yeoul, a 29-year-old Korean woman. Long brown hair in a high messy bun with a few loose strands, brown eyes, oversized cream-white hooded jacket with toggle buttons, white t-shirt, black crossbody harness bag with small blue accents, dark brown cargo jogger pants, chunky white-gray sneakers.

She sits on an invisible seat at the height of a low camping chair, knees bent, feet flat on the ground, as if a chair were there. Do not draw any chair, stool or seat. She holds only the rod handle and a small spinning reel in both hands at chest height. Draw no rod above her hands, no fishing line, no float.

Style: cute chibi SD, 2.5 heads tall, soft anime coloring, soft cel shading with a light watercolor texture, light from the upper left, a clean continuous dark brown outline around the whole silhouette including the face and the white hood.

Layout: 1 row x 2 columns, same scale as the attached standing poses, same baseline, wide spacing.
1. sitting and waiting: back view, calm, shoulders relaxed.
2. bite: three-quarter back view turned slightly to the right, startled, gripping the handle, a small part of her surprised face visible.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no labels, no borders, no grid lines, no ground shadow.
```

### SD 2-3-2. 낚시 의자 열 개 (뒤에서 본 것)

```
Create one item sheet for a mobile fishing game. Attached: my 10 fishing chair shop icons (keep each design, colors and decorations exactly, in the same order) and the heroine's back-view sitting pose (for scale and angle).

Draw the same 10 chairs seen from behind and slightly above, as the player sees them when the heroine sits on them facing the sea, empty, no person. Each chair stands upright on its legs, seat at the same height for all, the backrest facing the viewer. Keep the order: 1 simple beige folding stool, 2 blue folding chair, 3 olive camping chair, 4 blue reclining chair, 5 red and black chair, 6 yellow and black chair, 7 purple chair, 8 black and gold chair, 9 red and gold chair, 10 white and blue legendary chair with a small sunshade.

Style: the same painted game icon style as the attached icons, soft shading, light from the upper left, a clean dark outline around each chair.

Layout: 2 rows x 5 columns, all chairs at the same scale and on the same baseline, wide equal spacing so nothing touches.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no numbers, no labels, no borders, no grid lines, no ground shadow.
```

### SD 2-3-3. 릴 열 개 (낚싯대 아래 단 모습, 뒤에서)

```
Create one item sheet for a mobile fishing game. Attached: my 10 spinning reel shop icons (keep each design, colors and decorations exactly, in the same order) and the heroine's back-view fishing pose (for scale and angle).

Draw the same 10 spinning reels as they look mounted under a vertical fishing rod, seen from behind the angler: the reel foot at the top, the spool facing the viewer, the handle knob on the left side, no rod, no line, no hands. Keep the order: 1 silver reel with a wooden knob, 2 silver and blue, 3 olive green, 4 gold and black, 5 red, 6 blue and silver, 7 purple and gold, 8 black and gold, 9 red and gold, 10 blue and gold legendary reel.

Style: the same painted game icon style as the attached icons, soft shading, light from the upper left, a clean dark outline around each reel.

Layout: 2 rows x 5 columns, all reels at the same scale, centered in their cells, wide equal spacing so nothing touches.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no numbers, no labels, no borders, no grid lines, no shadow.
```

## SD 3. 여울 표정 시트

```
Create one expression sheet. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the heroine (her design).

Character: Han Yeoul, a 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, cream-white hooded jacket collar visible.

Style: cute chibi SD head and shoulders, big round head, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, warm seaside colors matching the attached backgrounds, light from the upper left.

Layout: 3 rows x 4 columns, head and shoulders only, every head facing front at exactly the same angle, size and position inside its cell so the faces can be swapped in a game, generous empty space between heads.
Row 1: neutral calm, gentle smile, big happy laugh with eyes closed, surprised with round eyes.
Row 2: quietly sad, crying with tears, worried with a small frown, pouting and annoyed.
Row 3: sharp focused eyes, tasting with eyes closed, a single tear of joy while tasting, embarrassed with blushing cheeks.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines.
```

## SD 4. 여울 부품 분리 시트 (뼈대 애니메이션용)

```
Create one cut-out puppet parts sheet for 2D skeletal animation in a game engine. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the heroine (her design).

Character: Han Yeoul in cute chibi SD style, 2.5 heads tall, front view. Long brown hair in a high messy bun, brown eyes, oversized cream-white hooded jacket with toggle buttons, white t-shirt, black crossbody harness bag with blue accents, dark brown cargo jogger pants, chunky white-gray sneakers. Clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Draw every body part as a separate piece, all at the same scale as if taken apart from one front-view character, arranged in a tidy layout with wide empty gaps so no piece touches another:
head with face but without eyes and mouth, front bangs, back hair, hair bun, torso with jacket and harness bag, left upper arm, left forearm with hand, right upper arm, right forearm with hand, hips, left leg with sneaker, right leg with sneaker.
Joints must have rounded overlapping ends so the parts can rotate without gaps.
Below them, separate face pieces at the same scale: eyes open, eyes closed, happy curved eyes, sad eyes, mouth small smile, mouth open laugh, mouth small o, mouth frown.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no arrows, no borders, no frames, no grid lines.
```

## SD 5. 고양이 고등어 시트

```
Create one game character sheet. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the cat (its design).

Character: Godeungeo, a stray cat with a silver-gray mackerel tabby coat and dark stripes, white chest, white muzzle and white paws, yellow-green eyes, pink nose and pink inner ears, a red collar with a small gold bell and a tiny old handmade wooden fishing float tied to the collar (faded red and white, no letters).

Style: cute chibi SD animal, big round head, small chubby body, short legs, fluffy tail, soft anime face with big eyes, clean thin dark brown line art, soft cel shading with a light watercolor texture, warm sunny seaside colors matching the attached backgrounds, light from the upper left.

Layout: 3 rows x 4 columns, full body, same scale, same baseline, generous empty space between poses so nothing touches.
Row 1: sitting front view, sitting side view facing left, walking side view facing right with front right paw forward, walking side view facing right with front left paw forward.
Row 2: sitting back view with tail curled, meowing while looking to the left, meowing while looking to the right, loaf pose with eyes half closed.
Row 3: sleeping curled up, stretching with rear up, eating a small fish happily, jumping up excitedly.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow.
```

## SD 6. 고등어 부품 분리 시트 (뼈대 애니메이션용)

```
Create one cut-out puppet parts sheet for 2D skeletal animation in a game engine. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the cat (its design).

Character: Godeungeo, a cute chibi SD stray cat, side view facing left. Silver-gray mackerel tabby with dark stripes, white chest, muzzle and paws, yellow-green eyes, pink nose, red collar with a small gold bell and a tiny old wooden fishing float (faded red and white, no letters). Clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Draw every body part as a separate piece, all at the same scale as if taken apart from one side-view cat, arranged in a tidy layout with wide empty gaps so no piece touches another:
head without eyes and mouth, left ear, right ear, body with collar, front left leg, front right leg, back left leg, back right leg, tail split into three segments from base to tip, the bell and float as one separate piece.
Joints must have rounded overlapping ends so the parts can rotate without gaps.
Below them, separate face pieces at the same scale: eyes open, eyes closed, happy curved eyes, sleepy half closed eyes, mouth closed, mouth open meow, mouth eating.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no arrows, no borders, no frames, no grid lines.
```

## 손님 2차 (새 손님 6명, 2026-10-26)
**받음 (2026-10-26)**: 디자인 시트와 SD 시트 두 장. `sd_cells.py`로 잘라 넣었다(시트 A는 회색 곱슬머리라 `--hair=0`).

여울호 등급과 단골이 생겨서 손님을 늘린다. 밤(지금 2명)과 아침(3명)이 비어 있어서 그 시간대 손님 위주. 먼저 **손님 2-1 디자인 시트**를 뽑고, 그 결과를 첨부해 **SD 시트 2-2, 2-3**을 뽑는다. 받으면 `python3 scripts/sd_cells.py <시트> 3 4 godot/art/customers <id>-stand,<id>-walk,<id>-happy,<id>-sad,...`로 잘라 넣고, `dialogue.json` customers와 `diner.json` guests에 더한다.

| id | 손님 | 오는 시간 | 좋아할 요리 (안) |
|---|---|---|---|
| haenyeo | 해녀 할머니 | 아침 · 점심 | 갑오징어숙회, 참돔회 |
| taxi | 택시 기사 아저씨 | 점심 · 밤 | 우럭매운탕, 고등어조림 |
| nurse | 밤 근무 간호사 | 아침 · 밤 | 도다리쑥국, 볼락구이 |
| welder | 조선소 청년 | 점심 · 저녁 · 밤 | 농어구이, 고등어조림 |
| boy | 동네 꼬마 | 저녁 | 볼락구이, 학공치 회무침 |
| pension | 민박집 사장님 | 아침 · 저녁 | 노래미 매운탕, 감성돔 숙성회 |

### 손님 2-1. 새 손님 디자인 시트 (일러스트)

```
Create one character design sheet of 6 new food truck customers for a cozy Korean seaside village mobile game. Attached: watercolor background illustrations from my game (color palette and lighting) and my current customer illustrations (match their art style, proportions, line art and coloring exactly so the new people belong to the same village).

Layout: 6 characters in one row, full body, front three quarter view facing slightly left, all at the same scale on the same baseline, generous empty space between them so nothing touches.

Characters, from left to right:
1. Haenyeo grandmother (Korean female free diver, about 70): short permed gray hair, sun tanned smiling wrinkled face, faded floral print loose work pants (momppae), plain navy long sleeve top with rolled sleeves, rubber sandals, an orange net bag (mangsari) over one shoulder and a round orange buoy tucked under her arm. Strong and cheerful.
2. Taxi driver uncle (about 50): round face, short black hair with a little gray, sunglasses pushed up on his head, light blue short sleeve collared shirt, dark slacks, a small white towel around his neck, car keys on a lanyard, comfortable brown loafers. Friendly and chatty.
3. Night shift nurse (about 30): dark bob hair with a clip, tired kind eyes, light teal hospital scrubs under a long navy knit cardigan, a lanyard ID card with no readable text, white sneakers, holding a thermos. Gentle and sleepy.
4. Shipyard worker young man (about 25): short messy hair, a band-aid on his cheek, gray blue work coverall with the top tied around his waist over a white t-shirt, work gloves stuck in a pocket, a white safety helmet held under his arm, steel toe boots. Shy but hungry.
5. Village boy (about 9): short spiky hair, missing front tooth grin, yellow t-shirt, denim shorts with a sticking plaster on one knee, a toy fishing rod with a small red float, a little plastic bucket. Energetic.
6. Guesthouse owner (minbak, about 45): long hair in a low ponytail with a scarf headband, bright pink apron with a pocket over a striped shirt and beige pants, slippers, a bunch of house keys on a ring. Warm and busy.

Style: same soft anime illustration as the attached customers, delicate clean line art, gentle cel shading with a light watercolor texture, warm sunny south coast colors, light from the upper left. Each person has one clear signature color so they read well at small size.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no name tags with writing, no borders, no frames, no grid lines, no ground shadow.
```

### 손님 2-2. SD 시트 A (해녀 할머니, 택시 기사, 간호사)

```
Create one game SD character sheet. Attached: watercolor background illustrations from my game (color palette and lighting only), the new customer design sheet (use it for the three designs below exactly) and my current SD customer sheet (match its chibi proportions, line art, coloring and pose style exactly).

Characters, one per row:
Row 1: Haenyeo grandmother: short permed gray hair, tanned smiling face, faded floral loose work pants, navy long sleeve top with rolled sleeves, rubber sandals, orange net bag over the shoulder, round orange buoy under the arm.
Row 2: Taxi driver uncle: short black hair with a little gray, sunglasses pushed up on his head, light blue short sleeve collared shirt, dark slacks, small white towel around the neck, brown loafers.
Row 3: Night shift nurse: dark bob hair with a clip, tired kind eyes, light teal scrubs under a long navy knit cardigan, lanyard card with no text, white sneakers, holding a thermos.

Columns, the same for every row, every figure facing LEFT in side or three quarter view:
1. standing still, relaxed, waiting to order
2. walking to the left, one foot forward, a small bounce
3. delighted after tasting: both arms up or hands clapping, big happy smile, a little hop
4. disappointed: shoulders dropped, head slightly down, small frown

Style: cute chibi SD, 2 to 2.5 heads tall, big head, small rounded body, short limbs, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Layout: 3 rows x 4 columns, full body, every figure exactly the same scale and standing on the same baseline in its row, generous empty space between figures so nothing touches or overlaps, props stay inside each figure's own space.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow, no speech bubbles, no effect marks.
```

### 손님 2-3. SD 시트 B (조선소 청년, 동네 꼬마, 민박집 사장님)

```
Create one game SD character sheet. Attached: watercolor background illustrations from my game (color palette and lighting only), the new customer design sheet (use it for the three designs below exactly) and my current SD customer sheet (match its chibi proportions, line art, coloring and pose style exactly).

Characters, one per row:
Row 1: Shipyard worker young man: short messy hair, band-aid on his cheek, gray blue work coverall with the top tied around the waist over a white t-shirt, gloves in a pocket, white safety helmet under his arm, steel toe boots.
Row 2: Village boy about 9 years old (draw him a little shorter than the adults): short spiky hair, missing front tooth grin, yellow t-shirt, denim shorts, a plaster on one knee, a toy fishing rod with a small red float, a little plastic bucket.
Row 3: Guesthouse owner woman: long hair in a low ponytail with a scarf headband, bright pink apron over a striped shirt and beige pants, slippers, a ring of house keys.

Columns, the same for every row, every figure facing LEFT in side or three quarter view:
1. standing still, relaxed, waiting to order
2. walking to the left, one foot forward, a small bounce
3. delighted after tasting: both arms up or hands clapping, big happy smile, a little hop
4. disappointed: shoulders dropped, head slightly down, small frown

Style: cute chibi SD, 2 to 2.5 heads tall, big head, small rounded body, short limbs, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Layout: 3 rows x 4 columns, full body, every figure exactly the same scale and standing on the same baseline in its row, generous empty space between figures so nothing touches or overlaps, props stay inside each figure's own space.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow, no speech bubbles, no effect marks.
```

## 다음 SD 차례
막순 할머니 → 노씨 → 손님 6명(위 디자인) → 트럭 여울호 → 물고기 5종 → 요리 5종 → 차세라. 여울·고등어 기본 시트는 받았다. 다음은 SD 2(여울 동작), SD 3(표정)이고 그 뒤 같은 형식으로 준다.

---

## 대화 일러스트 (스탠딩 표정 시트)

대화창 옆에 크게 서는 그림. 같은 옷, 같은 자세 틀에서 표정과 손만 바뀌어야 대화 중에 바꿔 껴도 자연스럽다. 한 시트에 표정 4칸(2×2), 칸마다 머리부터 무릎 위까지.
받으면 `scripts/sd_sheet.py`로 칸을 자르고 `scripts/clean_halo.py`로 머리카락 사이 흰 테두리를 정리해 `godot/art/portraits/`에 넣고 `dialogue.json`의 `faces`에 이름을 붙인다.
머리카락 사이로 배경이 갇히면 지우기 어렵다. 프롬프트에 잔머리 사이를 비우지 말라는 말을 넣었다.

### 대화 1. 여울 표정 시트 (기본·웃음·걱정·놀람)

```
Create a 2x2 character expression sheet of standing dialogue illustrations for a mobile game visual novel scene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustrations (match their palette).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with a few loose strands, brown eyes, oversized cream-white hooded jacket with toggle buttons, white t-shirt, black crossbody harness bag with blue accents, dark brown cargo jogger pants.

Layout: four panels of exactly the same size, same framing from the top of the head to just above the knees, same three-quarter pose facing slightly right, same scale and same position in every panel so the panels can be swapped during dialogue.
Panel 1: calm neutral face, slightly tired, hands relaxed.
Panel 2: small warm smile, eyes a little narrowed, one hand touching the strap of her bag.
Panel 3: worried, eyebrows drawn together, lips pressed, hand on her chest.
Panel 4: surprised, eyes wide, mouth slightly open, one hand raised near her mouth.

Style: soft anime illustration, delicate clean line art, subtle natural facial acting, soft cel shading with a light watercolor texture, light from the upper left.
Background: plain flat light gray #EDEDED in every panel. Keep loose hair strands as clean separate lines without the gray background trapped between strands, no glow or white outline around the hair. No text, no labels, no numbers, no panel borders, no grid lines, no ground shadow, no checkerboard, no signature, no watermark.
```

### 대화 2. 막순 할머니 표정 시트 (기본·웃음·걱정·말없이 내밀기)

```
Create a 2x2 character expression sheet of standing dialogue illustrations for a mobile game visual novel scene. Attached: the current illustration of the grandmother (keep her design exactly) and my watercolor background illustrations (match their palette).

Character: Grandma Maksun, Korean grandmother in her late 70s who runs the small harbor restaurant Yeoul Sikdang, blunt but always on her granddaughter's side. Short curly gray-white hair with a red patterned bandana, warm wrinkled face, pink floral blouse with rolled sleeves, dark brown work apron with a white towel tucked in, dark loose pants, black rubber boots.

Layout: four panels of exactly the same size, same framing from the top of the head to just above the knees, same three-quarter pose facing slightly left, same scale and same position in every panel.
Panel 1: calm, gruff but kind face, hands on her hips.
Panel 2: big crinkly laugh, eyes closed, one hand on her bandana.
Panel 3: quiet worry, looking down, hands clasped in front of her apron.
Panel 4: no words, gently holding out a plate of grilled rockfish with both hands, soft eyes.

Style: soft anime illustration, delicate clean line art, subtle natural facial acting, soft cel shading with a light watercolor texture, light from the upper left.
Background: plain flat light gray #EDEDED in every panel. Keep curly hair as clean lines without the gray background trapped inside, no glow or white outline around the hair. No text, no labels, no numbers, no panel borders, no grid lines, no ground shadow, no checkerboard, no signature, no watermark.
```

### 대화 3. 노씨 표정 시트 (기본·웃음·무뚝뚝·쪽지)

```
Create a 2x2 character expression sheet of standing dialogue illustrations for a mobile game visual novel scene. Attached: the current illustration of Mr. No (keep his design exactly) and my watercolor background illustrations (match their palette).

Character: Mr. No, an old Korean man in his sixties who runs the bait shop at the harbor and was once the fishing partner of the heroine's father. Tanned skin, short graying hair under a navy bandana, stubble, dark navy work shirt with rolled sleeves, worn dark canvas apron, dark pants, rubber boots. Almost never talks, gruff, hiding something.

Layout: four panels of exactly the same size, same framing from the top of the head to just above the knees, same three-quarter pose facing slightly left, same scale and same position in every panel.
Panel 1: arms crossed, flat expression, looking aside.
Panel 2: rare wide grin, eyes closed, arms still crossed.
Panel 3: frowning, one eyebrow raised, scratching the back of his neck.
Panel 4: quietly holding out a folded old paper note between two fingers, not looking at the viewer.

Style: soft anime illustration, delicate clean line art, subtle natural facial acting, soft cel shading with a light watercolor texture, light from the upper left.
Background: plain flat light gray #EDEDED in every panel. No glow or white outline around the hair. No text on the note, no labels, no numbers, no panel borders, no grid lines, no ground shadow, no checkerboard, no signature, no watermark.
```

## 배경 다시 뽑기

### 배경 1. 물비늘포 마을 (발 디딜 앞마당이 있는 구도)
지금 마을 그림은 위에서 내려다보는 구도라 SD 여울·고등어·트럭이 꽃밭과 난간 위에 떠 보인다(2026-10-22 사용자 의견). 눈높이에서 본 항구 길을 아래쪽에 넓게 두고, 장소 네 곳(위판장, 미끼가게, 여울식당, 방파제)이 위쪽 절반에 보이게 다시 뽑는다.
- **받음 (2026-10-22)**: 밤과 낮 두 장. 새벽(`village-0`)은 밤 그림, 낮(`village-2`)은 낮 그림 그대로, 아침(`village-1`)과 저녁(`village-3`)은 낮 그림에 색을 입혀 만들었다. 진짜 아침·저녁 그림을 뽑으면 바꿔 넣는다.
- **바꿈 (2026-10-25, 사용자가 GPT로 뽑음)**: WebP 일곱 장 `village-0`(해 뜸) `village-1`(맑은 낮) `village-2`(분홍빛 새벽) `village-3`(노을) `village-night` `village-rain` `village-rain-dusk`. 푸드트럭이 그림 안에 있어서 소품 트럭은 뺐다. 시간대와 그림 짝은 `balance.json` time.townArt(새벽 2, 아침 0, 점심 1, 저녁 3, 밤 night). 옛 PNG 네 장은 지웠다.
- 먼저 **1-1 낮**을 뽑고, 나머지 세 장은 1-1 결과를 같이 첨부해서 같은 구도로 시간만 바꾼다.
- 받으면 `godot/art/backgrounds/village-<0~3>.png`(900×1600 PNG)로 넣고, `TownScreen` 간판 자리(`Pin`)와 발 높이(`_footY`)를 새 그림에 맞춘다. 웹은 `public/assets/bg-village-<n>.webp`.

#### 배경 1-1. 마을 낮 (`village-2`, 먼저 뽑기)

```
Create one vertical background illustration for a mobile game. Attached: my current watercolor background illustrations (match their palette and painterly texture).

Scene: the small harbor village Mulbinulpo on the south coast of Korea, seen from eye level as if standing on the village harbor road (not a high bird's-eye view). Layout from left to right, all clearly readable as places:
- left middle: a fish auction market shed with a blue metal roof, stacked blue and yellow fish crates
- left of center, closer: a tiny bait and tackle shop with a green and white striped awning, fishing rods and floats by the door
- right middle: a traditional Korean hanok restaurant with a dark tiled roof, wooden lattice doors and warm paper lanterns
- far right in the distance: a breakwater lined with concrete tetrapods ending in a small red lighthouse
- behind: calm sea, a few fishing boats at the pier, soft green islands on the horizon

Very important composition for a game screen:
- The lower half is a wide, flat, open paved harbor road and plaza made of worn concrete, seen almost straight on, with a clear walkable empty area from 55 percent to 85 percent of the image height across the whole width. Characters and a small truck will be placed standing on this ground later, so keep it flat, sunlit and empty: no flowers, no railings, no stairs, no crates, no people, no cars in that area.
- The bottom 15 percent is simple ground (a menu panel will cover it).
- The top 15 percent is calm sky (a status bar will cover it).
- Light comes from the upper left.

Time of day: Bright clear early afternoon, deep blue sea with sparkles, white clouds.

Style: soft painterly watercolor illustration matching the attached background images (same palette, brush texture and level of detail), gentle anime background art, clean shapes. Vertical 9:16, at least 1080x1920. No people, no animals, no vehicles, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 1-2. 마을 새벽 (`village-0`)

```
Create one vertical background illustration for a mobile game. Attached: my current watercolor background illustrations (match their palette and painterly texture) and the daytime version of this exact village scene (keep the same composition, buildings, camera and ground area exactly; change only the time of day and lighting).

Scene: the small harbor village Mulbinulpo on the south coast of Korea, seen from eye level as if standing on the village harbor road (not a high bird's-eye view). Layout from left to right, all clearly readable as places:
- left middle: a fish auction market shed with a blue metal roof, stacked blue and yellow fish crates
- left of center, closer: a tiny bait and tackle shop with a green and white striped awning, fishing rods and floats by the door
- right middle: a traditional Korean hanok restaurant with a dark tiled roof, wooden lattice doors and warm paper lanterns
- far right in the distance: a breakwater lined with concrete tetrapods ending in a small red lighthouse
- behind: calm sea, a few fishing boats at the pier, soft green islands on the horizon

Very important composition for a game screen:
- The lower half is a wide, flat, open paved harbor road and plaza made of worn concrete, seen almost straight on, with a clear walkable empty area from 55 percent to 85 percent of the image height across the whole width. Characters and a small truck will be placed standing on this ground later, so keep it flat, sunlit and empty: no flowers, no railings, no stairs, no crates, no people, no cars in that area.
- The bottom 15 percent is simple ground (a menu panel will cover it).
- The top 15 percent is calm sky (a status bar will cover it).
- Light comes from the upper left.

Time of day: Blue hour dawn before sunrise: deep indigo and lavender sky with the last stars, a thin pale glow on the horizon, lanterns of the restaurant and street lamps still lit, lights on the fishing boats, calm dark water with soft reflections.

Style: soft painterly watercolor illustration matching the attached background images (same palette, brush texture and level of detail), gentle anime background art, clean shapes. Vertical 9:16, at least 1080x1920. No people, no animals, no vehicles, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 1-3. 마을 아침 (`village-1`)

```
Create one vertical background illustration for a mobile game. Attached: my current watercolor background illustrations (match their palette and painterly texture) and the daytime version of this exact village scene (keep the same composition, buildings, camera and ground area exactly; change only the time of day and lighting).

Scene: the small harbor village Mulbinulpo on the south coast of Korea, seen from eye level as if standing on the village harbor road (not a high bird's-eye view). Layout from left to right, all clearly readable as places:
- left middle: a fish auction market shed with a blue metal roof, stacked blue and yellow fish crates
- left of center, closer: a tiny bait and tackle shop with a green and white striped awning, fishing rods and floats by the door
- right middle: a traditional Korean hanok restaurant with a dark tiled roof, wooden lattice doors and warm paper lanterns
- far right in the distance: a breakwater lined with concrete tetrapods ending in a small red lighthouse
- behind: calm sea, a few fishing boats at the pier, soft green islands on the horizon

Very important composition for a game screen:
- The lower half is a wide, flat, open paved harbor road and plaza made of worn concrete, seen almost straight on, with a clear walkable empty area from 55 percent to 85 percent of the image height across the whole width. Characters and a small truck will be placed standing on this ground later, so keep it flat, sunlit and empty: no flowers, no railings, no stairs, no crates, no people, no cars in that area.
- The bottom 15 percent is simple ground (a menu panel will cover it).
- The top 15 percent is calm sky (a status bar will cover it).
- Light comes from the upper left.

Time of day: Fresh morning just after sunrise: warm golden light from the left, long soft shadows on the road, light mist over the sea, pale sky with peach clouds.

Style: soft painterly watercolor illustration matching the attached background images (same palette, brush texture and level of detail), gentle anime background art, clean shapes. Vertical 9:16, at least 1080x1920. No people, no animals, no vehicles, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 1-4. 마을 저녁 (`village-3`)

```
Create one vertical background illustration for a mobile game. Attached: my current watercolor background illustrations (match their palette and painterly texture) and the daytime version of this exact village scene (keep the same composition, buildings, camera and ground area exactly; change only the time of day and lighting).

Scene: the small harbor village Mulbinulpo on the south coast of Korea, seen from eye level as if standing on the village harbor road (not a high bird's-eye view). Layout from left to right, all clearly readable as places:
- left middle: a fish auction market shed with a blue metal roof, stacked blue and yellow fish crates
- left of center, closer: a tiny bait and tackle shop with a green and white striped awning, fishing rods and floats by the door
- right middle: a traditional Korean hanok restaurant with a dark tiled roof, wooden lattice doors and warm paper lanterns
- far right in the distance: a breakwater lined with concrete tetrapods ending in a small red lighthouse
- behind: calm sea, a few fishing boats at the pier, soft green islands on the horizon

Very important composition for a game screen:
- The lower half is a wide, flat, open paved harbor road and plaza made of worn concrete, seen almost straight on, with a clear walkable empty area from 55 percent to 85 percent of the image height across the whole width. Characters and a small truck will be placed standing on this ground later, so keep it flat, sunlit and empty: no flowers, no railings, no stairs, no crates, no people, no cars in that area.
- The bottom 15 percent is simple ground (a menu panel will cover it).
- The top 15 percent is calm sky (a status bar will cover it).
- Light comes from the upper left.

Time of day: Sunset evening: orange and pink sky, the sea glowing gold, warm lantern light starting in the restaurant, long blue shadows on the road.

Style: soft painterly watercolor illustration matching the attached background images (same palette, brush texture and level of detail), gentle anime background art, clean shapes. Vertical 9:16, at least 1080x1920. No people, no animals, no vehicles, no text, no readable signs, no letters, no signature, no watermark.
```

### 배경 2. 날씨 (비·눈·태풍·안개, 나중에 날씨를 넣을 때)
지금 마을 그림(`village-1.webp` 점심, 저녁 판은 `village-3.webp`)을 **꼭 같이 첨부**해서 구도·건물·푸드트럭 자리를 그대로 두고 날씨만 바꾼다. 받으면 `godot/art/backgrounds/village-<날씨>.webp`(864×1536 이상, 9:16)로 넣는다. 빗줄기·눈송이는 화면이 따로 그리니 그림에는 약하게만 넣는다(`Ui/RainOverlay`). 이미 있는 비 두 장은 다시 뽑지 않아도 된다.

**받음 (2026-10-26)**: 눈 네 장 `village-snow.webp`, `village-snow-night.webp`(트럭 자리가 맑은 날과 같다), `breakwater-snow.png`, `breakwater-snow-night.png`(사용자가 밤 판도 줬다). 게임 날씨는 아직 없어 `--weather=snow` 사진으로만 본다.

#### 배경 2-1. 눈 오는 낮 (`village-snow`)

```
Create one vertical background illustration for a mobile game. Attached: the current daytime village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: a quiet snowy winter day in a south coast Korean harbor village. Soft overcast pale grey sky, light snow resting on the tiled roofs, the market shed roof, the awning and the top of the food truck, a thin even layer of fresh snow on the plaza with a few faint footprint-free patches near the buildings, the sea a calm muted grey blue, warm lanterns glowing in the restaurant windows. Only a few soft falling snowflakes, no heavy blizzard (the game draws falling snow on top).

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent calm sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-2. 눈 오는 밤 (`village-snow-night`)

```
Create one vertical background illustration for a mobile game. Attached: the current night village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: a calm snowy winter night. Deep navy sky with soft snow clouds and a hidden moon glow, snow on the roofs, the awning and the food truck, a thin white layer on the plaza that reflects the warm orange light of the restaurant lanterns, street lamps and the truck lights, the sea dark and still with a few boat lights. Only a few soft falling snowflakes (the game draws falling snow on top).

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent calm sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-5. 눈 오는 방파제 (`breakwater-snow`, 겨울 낚시)

```
Create one vertical background illustration for a mobile fishing game. Attached: the current daytime breakwater background of this exact scene. Keep the composition, camera, the concrete tetrapods on both sides, the flat breakwater walkway at the bottom, the mooring bollards, the distant red lighthouse and the islands exactly the same. Change only the season and weather.

Weather: a quiet snowy winter morning on the south coast of Korea. Soft pale gray sky, light snow resting on top of the tetrapods, the bollards and the walkway rails, a thin even layer of fresh snow on the walkway, the islands dusted white, the sea a calm cold gray blue with gentle waves, the lighthouse a small red accent. Only a few soft falling snowflakes (the game draws falling snow on top). The sea in the middle of the image stays fairly smooth and readable (the game draws a float, ripples and splashes there).

Keep for the game screen: the walkway at the bottom stays flat and empty (a character and a cooler box will stand there), the top 15 percent calm sky, the bottom 20 percent simple (a panel covers it).

Style: the same soft painterly watercolor anime background as the attached image, same brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no fish, no footprints, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-3. 태풍 오는 날 (`village-typhoon`)
**받음 (2026-10-26)**: `village-typhoon.webp`, `village-typhoon-night.webp`(밤은 산 위치가 맞는 판으로 다시 받았다). 트럭 자리가 맑은 날과 같다. 사진 확인용 `--weather=typhoon`(비 효과를 얹는다).
방파제 태풍 그림은 뽑지 않는다. 태풍 날은 방파제 출입이 막혀 낚시를 못 하는 날로 둔다.

```
Create one vertical background illustration for a mobile game. Attached: the current daytime village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: a typhoon approaching a south coast Korean harbor village in late summer. Heavy dark slate and green gray clouds rolling in fast, a strange yellowish light low on the horizon, big white waves crashing over the tetrapods and the breakwater with spray around the red lighthouse, fishing boats tied tightly at the pier with extra ropes, the restaurant wooden doors shut with lanterns swinging, the food truck awning rolled up and its stools stacked, the bait shop awning pulled in, wet dark plaza with puddles reflecting the gray sky, pine trees bending hard in strong wind, a few leaves flying. Only light rain streaks (the game draws rain on top).

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail, dramatic but still gentle and cozy. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-3-2. 태풍 오는 밤 (`village-typhoon-night`)

```
Create one vertical background illustration for a mobile game. Attached: the current night village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: a typhoon night in a south coast Korean harbor village. Pitch dark churning clouds lit from inside by a distant flash of lightning, the sea black and violent with huge white waves breaking over the tetrapods, spray hiding the base of the red lighthouse whose light still blinks, fishing boats tied tightly at the pier and rocking, warm light leaking through the shut wooden doors of the restaurant, the food truck closed with its awning rolled up and one small light on inside, street lamps swaying and casting long trembling reflections on the soaked plaza, pine trees bending hard in the wind. Only light rain streaks (the game draws rain on top).

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail, dramatic but still warm and safe feeling. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-4. 안개 낀 마을 (`village-fog`)
**받음 (2026-10-26)**: `village-fog.webp`, `village-fog-night.webp`(트럭 자리가 맑은 날과 같다). 방파제 안개는 아직.
날씨가 나쁜 날은 하늘이 가려 시간대 차이가 작다. 낮 판 하나에 게임이 시간대 색을 입히고, 불빛이 달라지는 밤만 따로 뽑는다(비·눈·태풍·안개 모두 같다). 안개 날은 낚시를 막지 않아서 방파제 판도 뽑는다.

```
Create one vertical background illustration for a mobile game. Attached: the current daytime village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: thick sea fog (haemu) rolling in on a quiet spring day in a south coast Korean harbor village. A soft milky white mist hides the islands and most of the sea, the red lighthouse is only a faint red shape with its glowing light, the fishing boats at the pier fade into the fog, the buildings and the food truck in front stay clear but a little softened, warm lanterns glow through the haze, the plaza is damp and pale with soft blurry reflections. Calm and quiet, no wind, sky a flat bright white gray.

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-4-2. 안개 낀 마을 밤 (`village-fog-night`)

```
Create one vertical background illustration for a mobile game. Attached: the current night village background of this exact scene. Keep the composition, camera, every building, the blue and white food truck and the empty paved plaza exactly the same. Change only the weather.

Weather: a foggy spring night in a south coast Korean harbor village. Thick sea fog glows softly in deep blue gray, the moon is only a faint blurred halo, the sea and islands disappear into the mist, the red lighthouse light makes a hazy red glow, every street lamp, lantern and the food truck lights bloom into soft round halos of warm orange light, the boats at the pier are dim silhouettes, the damp plaza shows long soft blurry light reflections. Calm, quiet and a little mysterious but cozy.

Keep for the game screen: the lower half stays a flat open plaza with no new objects, the bottom 15 percent simple ground, the top 15 percent sky.

Style: the same soft painterly watercolor anime background as the attached image, same palette family, brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no text, no readable signs, no letters, no signature, no watermark.
```

#### 배경 2-4-3. 안개 낀 방파제 (`breakwater-fog`)

```
Create one vertical background illustration for a mobile fishing game. Attached: the current daytime breakwater background of this exact scene. Keep the composition, camera, the concrete tetrapods on both sides, the flat breakwater walkway at the bottom, the mooring bollards, the distant red lighthouse and the islands exactly the same. Change only the weather.

Weather: thick sea fog (haemu) on a calm spring day. A soft milky white mist hides the islands and the horizon, the red lighthouse is only a faint red shape with a glowing light, the sea near the walkway stays clear and readable with calm gray green water and gentle ripples, then fades into white further out, the tetrapods and walkway in front are damp and clear, sky a flat bright white gray. Calm, no wind. The sea in the middle of the image stays readable (the game draws a float, ripples and splashes there).

Keep for the game screen: the walkway at the bottom stays flat and empty (a character and a cooler box will stand there), the top 15 percent sky, the bottom 20 percent simple (a panel covers it).

Style: the same soft painterly watercolor anime background as the attached image, same brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no fish, no text, no readable signs, no letters, no signature, no watermark.
```

### 배경 3. 방파제 밤 (`breakwater-night`, 밤낚시용)
**받음 (2026-10-26)**: `godot/art/backgrounds/breakwater-night.png`(900×1600). 밤 칸 낚시에서 색을 입히지 않고 그대로 쓴다.
지금 밤낚시는 낮 방파제 그림(`breakwater.png`)에 푸른 색만 입힌다. 이 그림을 받으면 밤 칸에서 바꿔 쓴다. `breakwater.png`를 꼭 같이 첨부한다.

```
Create one vertical background illustration for a mobile fishing game. Attached: the current daytime breakwater background of this exact scene. Keep the composition, camera, the concrete tetrapods on the left, the flat breakwater walkway at the bottom, the mooring bollards, the distant red lighthouse and the islands exactly the same. Change only the time to night.

Night: a calm clear spring night on the south coast of Korea. Deep navy sky with scattered stars and a bright moon high on the right, a long shimmering moonlight path on the dark sea, the red lighthouse light glowing, a few small squid boat lights far on the horizon, soft blue moonlight on the tetrapods and the walkway, one warm street lamp glow at the far end of the breakwater. The sea in the middle of the image stays fairly smooth and readable (the game draws a float, ripples and splashes there).

Keep for the game screen: the walkway at the bottom stays flat and empty (a character and a cooler box will stand there), the top 15 percent calm sky, the bottom 20 percent simple (a panel covers it).

Style: the same soft painterly watercolor anime background as the attached image, same brush texture and detail. Vertical 9:16, at least 1080x1920. No people, no animals, no fish, no text, no readable signs, no letters, no signature, no watermark.
```

## 요리 시트 (트럭 메뉴·결과창용, 2026-10-26)
**받음 (2026-10-26)**: 두 장 모두. `python3 scripts/dish_sheet.py <시트> <이름들>`로 잘라 `godot/art/dishes/<요리 id>.png`(예: `chamdom-hoe.png`)에 넣었다. 트럭 메뉴판·간 맞추기·결과에 나온다(`PlaceScreen.DishIcon`). 붙여 넣을 때 마을 배경 그림 한 장(색감과 빛)을 같이 첨부한다. 두 시트는 같은 접시 크기와 같은 각도로 맞춘다(요리 2를 뽑을 때 요리 1 결과도 첨부).

### 요리 1. 새 요리 다섯 (참돔회, 도다리쑥국, 갑오징어숙회, 우럭매운탕, 농어구이)

```
Create one game item sheet of 5 Korean seafood dishes for a cozy mobile fishing and food truck game. Attached: a background illustration from the game (match its warm watercolor palette and soft light).

Layout: 3 dishes on the top row and 2 on the bottom row, centered, wide equal spacing so nothing touches. Every dish is seen from the same three quarter top view (about 45 degrees from above), the same plate or pot size, sitting on the same invisible floor line. Light comes from the upper left.

Dishes, in this order:
1. Red sea bream sashimi (chamdom hoe): thin glossy slices of white translucent flesh with a thin pink red skin edge on each slice, fanned neatly on a light wooden board with shredded white radish, a green shiso leaf, a small dab of wasabi and a tiny dish of soy sauce.
2. Flounder and mugwort soup (dodari ssukguk), a Korean spring dish: a round light brown earthenware bowl of clear pale soybean paste broth, two pieces of white flounder flesh with a bit of speckled brown flatfish skin, plenty of fresh bright green mugwort leaves floating on top, a few slices of green onion, soft steam rising.
3. Blanched cuttlefish (gabojingeo sukhoe): thick pure white cuttlefish slices scored in a fine crosshatch pattern and gently curled from blanching, arranged on a white ceramic plate with cucumber slices and a small dish of glossy red vinegar chili paste (chojang).
4. Spicy rockfish stew (ureok maeuntang): a black earthenware pot (ttukbaegi) of bubbling red spicy broth with a whole dark grey brown rockfish with spiny fins, chunks of white radish, tofu, crown daisy greens, sliced red and green chili, steam rising.
5. Grilled sea bass (nongeo gui): one whole sea bass with a long silver grey body, darker grey back and a slightly protruding lower jaw, crisp golden grill marks, sprinkled with coarse sea salt, on a long rectangular ceramic plate with a lemon wedge and a few grilled green onions.

Style: cute appetizing game food art, soft anime illustration with gentle painterly shading, clean simple shapes and slightly chunky friendly proportions so each dish reads clearly at small size, warm and cozy. The fish in each dish must look like the real species described.

Background: one flat solid light grey color #EDEDED. No checkerboard, no table, no tablecloth, no shadows on the floor, no text, no labels, no numbers, no borders, no frames, no signature, no watermark.
```

### 요리 2. 원래 요리 다섯 다시 (볼락구이, 고등어조림, 노래미 매운탕, 학공치 회무침, 감성돔 숙성회)

```
Create one game item sheet of 5 Korean seafood dishes for a cozy mobile fishing and food truck game. Attached: a background illustration from the game (match its warm watercolor palette and soft light) and my other dish sheet (match its art style, plate size and camera angle exactly).

Layout: 3 dishes on the top row and 2 on the bottom row, centered, wide equal spacing so nothing touches. Every dish is seen from the same three quarter top view (about 45 degrees from above), the same plate or pot size, sitting on the same invisible floor line. Light comes from the upper left.

Dishes, in this order:
1. Grilled black rockfish (bollak gui): two small whole black rockfish, short body, dark brown grey skin, very large round eyes and spiny dorsal fins (clearly a small rockfish, not a sea bream), crisp grill marks and coarse salt, on a round white ceramic plate.
2. Braised mackerel with radish (godeungeo jorim): mackerel pieces with blue green back skin and dark wavy stripes, thick radish chunks, glossy red brown sauce, sliced green onion, in a shallow wide pot.
3. Spicy greenling stew (noraemi maeuntang): a black earthenware pot of red spicy broth with pieces of mottled brown olive greenling, tofu, crown daisy greens and radish, steam rising.
4. Spicy raw halfbeak salad (hakgongchi hoe muchim): thin strips of silver skinned halfbeak flesh tossed with red chili dressing, cucumber, onion, perilla leaves and sesame seeds on a ceramic plate.
5. Aged black sea bream sashimi (gamseongdom sukseong hoe): slices of white flesh with a thin silver dark grey skin edge (not a blue backed fish), neatly fanned on a wooden board with shiso leaves and a small dish of soy sauce.

Style: cute appetizing game food art, soft anime illustration with gentle painterly shading, clean simple shapes and slightly chunky friendly proportions so each dish reads clearly at small size, warm and cozy. The fish in each dish must look like the real species described.

Background: one flat solid light grey color #EDEDED. No checkerboard, no table, no tablecloth, no shadows on the floor, no text, no labels, no numbers, no borders, no frames, no signature, no watermark.
```

## 마을 사람 SD (마을이 움직이게, 2026-10-26)

마을 그림 위를 시간대마다 오가는 사람들. 손님 SD와 같은 그림체로 걷기 두 장과 서기 한 장. 지금 여울 SD 그림과 손님 SD 그림을 같이 첨부한다.

```
Create one SD character sheet for a mobile game, background solid light gray #EDEDED (no checkerboard, no text, no borders). Attached: my current SD characters (keep exactly the same chibi style, 2 to 2.5 heads tall, soft anime coloring, clean outline).

Make 4 Korean seaside village people, each in 3 poses in one row: walking step A (side view facing right), walking step B (side view facing right, other leg forward), standing front view. 4 rows x 3 columns, wide equal spacing, same scale, same ground line, full body with feet visible.
Row 1: a village fisherman in his 50s with a knit cap, navy vest and yellow rubber boots, carrying a blue fish basket.
Row 2: an auntie from the fish market in a floral apron and arm sleeves, rubber gloves, carrying a red plastic tub.
Row 3: an elementary school boy with a backpack and a toy fishing net.
Row 4: a young delivery woman on foot with a cap and a small parcel box.
```

## 마을 의뢰인 SD 2 (2026-10-27)

납품 의뢰가 마을 퀘스트로 바뀌어 의뢰인이 마을에 서 있다. 그림이 없는 넷은 지금 손님 SD로 대신 서 있다(바다횟집 사장 → 어부 삼촌, 읍내 일식집 → 조선소 청년, 포장마차 아저씨 → 택시 기사, 노씨 → 할아버지). 받으면 `godot/art/villagers/<id>-stand|walk-1|walk-2.png`로 넣고 `requests.json sd`를 바꾼다. 지금 여울 SD, 손님 SD, 노씨 대화 그림을 같이 첨부한다.

```
Create one SD character sheet for a mobile game, background solid light gray #EDEDED (no checkerboard, no text, no borders). Attached: my current SD characters and the dialogue illustration of the bait shop owner (keep exactly the same chibi style, 2 to 2.5 heads tall, soft anime coloring, clean outline).

Make 5 Korean seaside village people, each in 3 poses in one row: walking step A (side view facing right), walking step B (side view facing right, other leg forward), standing front view. 5 rows x 3 columns, wide equal spacing, same scale, same ground line, full body with feet visible.
Row 1: Mr. No, the gruff bait shop owner from the attached illustration, in his 60s, same face, hair and apron, holding a small bait box.
Row 2: the owner of a seaside raw fish restaurant, man in his 40s, white chef coat with rolled sleeves, rubber apron and rubber boots, a towel around his neck.
Row 3: a Japanese restaurant chef from the town, man in his 30s, white chef jacket and white chef hat, neat and polite.
Row 4: an old street food stall owner, man in his 60s, red cap, padded vest, a hand towel on his shoulder.
Row 5: a guesthouse grandmother in her 70s, short curly gray hair, warm cardigan, holding a straw basket.
```

## 상환 장면 은행 직원 (대화 일러스트, 2026-10-26)

주간 상환 장면에 들어갈 감정 일러스트. 지금 막순 할머니·노씨 대화 일러스트를 같이 첨부한다.

```
Create one character expression sheet for a mobile game dialogue box, background solid light gray #EDEDED (no checkerboard, no text, no borders). Attached: my current dialogue illustrations (keep the same soft anime style, line weight and coloring).

Character: a bank loan officer from the town branch, Korean man in his early 30s, neat short black hair, thin silver glasses, white shirt with a loose navy tie, navy cardigan, a bank ID lanyard, holding a slim document folder. Polite and a little awkward, not a villain.
Upper body, 3 columns x 2 rows, same size and same framing: polite business smile, apologetic while explaining a late fee, stamping a document, surprised and impressed, worried, gentle relieved smile.
```

## 컷신 일러스트 다듬기 (여울 감정 연기)

이야기 장면 전용. 지금 감정 그림 10장(`y-*.webp`)을 한 장 그림 컷신으로 다시 뽑는다. 붙여 넣을 때 지금 여울 그림과 해당 배경 그림을 같이 첨부한다.

### 컷 1. 맛을 잃은 날 (프롤로그, `y-notaste` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustrations (match their palette and painterly texture).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef of a Michelin two-star French restaurant in Seoul, who lost her sense of taste and smell after a kitchen fire. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants.

Scene: a quiet hospital room in pale morning light. She sits on the edge of the bed holding a spoon of warm soup just below her lips. Steam rises but her face does not react. Her eyes are unfocused and a little wet, her eyebrows barely lifted, lips slightly parted. Not dramatic crying, but the empty stillness of someone who just realized something is gone. Her other hand loosely grips the blanket.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background, gentle cool light from the window on the left, cinematic composition with her upper body in the upper 70 percent. Keep the lower 30 percent of the image calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 2. 그날 밤 전화 (`y-phone` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustrations (match their palette and painterly texture).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants.

Scene: a small dark studio apartment in Seoul at night, city lights blurred through the window. She sits on the floor against the bed, phone pressed to her ear, lit only by the phone screen. She just heard that her grandmother collapsed. Her free hand covers her mouth, eyes wide and trembling, shoulders frozen. Fear and guilt at the same time, holding her breath.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background, cold blue night tones with a small warm glow from the phone, cinematic composition with her upper body in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 3. 10년 만의 물비늘포 (`y-back-bag` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustration of the harbor village (match it closely).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, oversized cream-white hooded jacket, black crossbody harness bag, dark brown cargo pants, a small travel bag in her hand.

Scene: seen from behind and slightly to the side, she stands at the top of the road looking down at a small southern Korean harbor village with a red lighthouse and bright blue sea. Sea wind lifts her loose hair strands. Her grip on the bag is tight and her shoulders are a little stiff. We see a sliver of her profile: she does not want to be here, but she came anyway.

Style: soft anime illustration, delicate line art, painterly watercolor background, bright late spring afternoon light from the upper left, wide calm composition with the character in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 4. 부러진 대나무 낚싯대 (`y-rod-broken` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustrations (match their palette and painterly texture).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants.

Scene: a dusty old storage shed with a beam of light through a gap in the wooden wall, an old one-ton truck half covered by a tarp in the background. She kneels holding a bamboo fishing rod broken in half, one piece in each hand. A worn fishing journal with torn pages lies open beside her knee. Her thumb gently touches the splintered end. Her expression is complicated: lips pressed together, eyes soft and distant, as if a memory she pushed away for years just came back.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor rendering, warm dusty light with floating dust, cinematic composition with her upper body in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 5. 짠맛 (`y-salty-tear` 대신, 가장 중요한 장면)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustration of the breakwater (match it closely).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste six months ago. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants.

Scene: on a concrete breakwater at sunset, beside a tiny portable grill with one small grilled rockfish. She has just taken the first bite with chopsticks. Her eyes widen slowly, one tear runs down her cheek before she even notices it, her free hand rises halfway to her mouth. She is not smiling yet. It is the exact second of disbelief before joy: she can taste salt.

Style: soft anime illustration, delicate line art, very subtle and natural facial acting, painterly watercolor background, warm golden sunset light from the left with a soft rim light on her hair, cinematic close-up of her upper body in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 6. 아버지의 일지 (`y-journal` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustrations of the heroine and the gray tabby cat (keep their designs exactly) and my watercolor background illustrations (match their palette and painterly texture).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants. The silver-gray tabby cat with a red collar, gold bell and a tiny old wooden fishing float.

Scene: night, she sits in the open back of an old blue and white one-ton truck under a small hanging lamp, reading a worn fishing journal with half of its pages torn out. The cat sleeps curled against her leg. Her fingertip rests on a line of handwriting. A quiet determined look grows in her eyes, mixed with longing. Calm, small hope, not a big smile.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background, deep blue night with a warm lamp glow on her face, harbor lights in the distance, cinematic composition with the characters in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. The journal pages show only illegible scribbles, no readable text, no letters, no signature, no watermark.
```

### 컷 7. 1차 상환 (1장 클리어)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustrations of the heroine and the grandmother (keep their designs exactly) and my watercolor background illustration of the restaurant (match it closely).

Characters: Han Yeoul, 29-year-old Korean woman, former sous-chef slowly regaining her taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket. Her grandmother Maksun, gray curly short hair, red polka-dot headband, purple floral blouse, black apron.

Scene: early dawn inside a small old Korean seaside restaurant. The grandmother silently slides a plate of grilled rockfish across the wooden table without looking at her, pretending to be indifferent but with wet eyes. Yeoul holds chopsticks above the fish, her lips trembling into a small smile, eyes shining. Warmth that neither of them says out loud.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor rendering, soft pink and gold dawn light through the window on the left, cinematic composition with the characters in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 8. 차압 (실패 결말, `y-knees` 대신)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly) and my watercolor background illustrations (match their palette and painterly texture).

Character: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants.

Scene: night in front of a small closed seaside restaurant, a red seizure notice is stuck on the sliding door (no readable text on it). She sits on the doorstep hugging her knees, chin on her arms. She is not sobbing. Her eyes are red and tired, staring at the ground, jaw tight, holding everything in. A thin line of warm light leaks from inside where her grandmother is.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background, cold blue night with one warm light strip, cinematic composition with her in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

## 1장 이야기 장면 (2026-10-27)

1장에 새로 넣은 이야기 장면(`dialogue.json beats`) 그림. 지금은 차세라 그림이 없어 이름표만 나오고, 찌·첫 만남 장면은 여울 감정 일러스트로 대신한다. 받으면 `sera` 화자와 장면 그림으로 바꾼다.

### 대화 4. 차세라 표정 시트 (기본·여유 웃음·도발·놀람·짜증·진지)

지금 여울·은행 직원 대화 일러스트를 같이 첨부한다.

```
Create one character expression sheet for a mobile game dialogue box, background solid light gray #EDEDED (no checkerboard, no text, no logo letters, no borders, no grid lines). Attached: my current dialogue illustrations of the heroine and the bank officer (keep exactly the same soft anime style, line weight, coloring and upper-body framing).

Character: Cha Sera, 27-year-old Korean woman, heir of a big fishing gear company called Blue Hook and a tournament champion angler. Rival of the heroine but not a villain: confident, proud, a little lonely. Sleek dark navy-black hair in a high ponytail with straight bangs, sharp dark eyes, small silver ear studs. Fitted white technical fishing jacket with deep blue panels and a small plain blue fish-hook emblem on the chest (a simple shape, no letters), slim black pants, polarized sunglasses pushed up on her head.

Layout: 3 columns x 2 rows, six upper-body poses from the head to the waist, all the same size, same eye height and same framing, each facing three-quarters to the viewer's left (her body turned toward the left side of the image, as if talking to someone standing on the left).
Row 1: calm neutral face with arms crossed / small confident smirk with one hand on her hip / provoking look, chin raised, pointing forward with her index finger.
Row 2: surprised, eyes wide and lips parted, one hand half raised / annoyed, frowning and looking away to the side / serious and quiet, sunglasses in her hand, looking straight ahead.

Lighting from the upper left, soft and warm like a clear southern Korean afternoon. No text, no letters, no signature, no watermark.
```

### 컷 9. 고등어 목의 찌 (`catFloat`)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustrations of the heroine and the gray tabby cat (keep both designs exactly) and my watercolor background illustration of the harbor village yard (match its palette and painterly texture).

Characters: Han Yeoul, 29-year-old Korean woman, former sous-chef who lost her sense of taste. Long brown hair in a high messy bun with loose strands, brown eyes, oversized cream-white hooded jacket, dark brown cargo pants. The silver-gray mackerel tabby cat with white chest and paws, yellow-green eyes, red collar with a small gold bell.

Scene: a close, low camera at the height of a crouching person, on the stone yard of the harbor village in soft morning light. Yeoul crouches on the left side of the image, facing right. The cat sits on the right side facing left, toward her, eyes half closed and calm. With her right hand she gently lifts a small old hand-carved wooden fishing float that hangs from the cat's red collar on a short faded string. The float is about the length of a finger, faded red on top and off-white below, with knife-carved scratches on the white part (only rough scratches, no readable letters). Her face is in three-quarter view: eyebrows slightly drawn together, head tilted, lips parted as if she is trying to remember something. Curious and a little uneasy, not smiling.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background softly blurred, light from the upper left, the float and her hand are the sharpest point in the image, characters in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no signature, no watermark.
```

### 컷 10. 차세라 첫 만남 (`seraMeet`)

```
Create one vertical 9:16 story illustration for a mobile game cutscene. Attached: the current illustration of the heroine (keep her design exactly), the Cha Sera expression sheet (keep her design exactly) and my watercolor background illustration of the breakwater (match it closely).

Characters: Han Yeoul, 29-year-old Korean woman, long brown hair in a high messy bun with loose strands, oversized cream-white hooded jacket, black crossbody harness bag, dark brown cargo pants, holding a simple fishing rod upright. Cha Sera, 27-year-old Korean woman, sleek dark navy-black high ponytail with straight bangs, fitted white technical fishing jacket with deep blue panels and a small plain blue fish-hook emblem (no letters), slim black pants, sunglasses pushed up on her head.

Scene: the entrance of a concrete breakwater with tetrapods and a red lighthouse far behind, early afternoon. A glossy deep blue van is parked in the background with a plain blue fish-hook shape on its side (no letters). Camera behind Yeoul's left shoulder: Yeoul stands in the lower left foreground, seen from behind and a little from the side, only her back, bun and the edge of her cheek visible. Sera stands in the middle ground on the right, three or four steps away, facing the camera and Yeoul, one hand on her hip and a small confident smirk, looking slightly down at Yeoul. Sea wind pushes Sera's ponytail to the left. The feeling: a calm first meeting with tension, not a fight.

Style: soft anime illustration, delicate line art, subtle and natural facial acting, painterly watercolor background, bright clear light from the upper left, cinematic composition with both characters in the upper 70 percent. Keep the lower 30 percent calm and simple for a subtitle box. No text, no letters, no logo words, no signature, no watermark.
```

---

## 웹 버전 일러스트 기록 (v0.2)

웹 버전에서 쓰던 규칙: 시트는 흰 배경(#FFFFFF), 배경 그림은 한 장에 하나, 세로 9:16, 위 15%·아래 20%는 비워 둔다.

### 이미 받은 그림 (2026-10)
배경 10장, 여울 정면·뒷모습·표정 시트, 막순 할머니 4종, 앞치마 아저씨(노씨로 사용), 차세라 시트 3종, 고등어 마스코트 시트, 고양이 시트 2종(두 번째를 사용), 배 여울호 시트, 트럭 여울호 시트.

### 그때 쓴 시트 프롬프트 (손님·요리 시트는 받았다)

```
첨부한 캐릭터 그림들과 같은 그림체로 게임용 시트를 만들어 줘. 아래 [규칙]을 반드시 지켜.

[규칙]
1. 한 번의 답변에 시트 한 장만 만들어. 내가 "다음"이라고 하면 다음 번호, "다시"라고 하면 같은 번호를 다시 만들어.
2. 배경은 진짜 흰색(#FFFFFF) 단색. 투명 체크무늬를 그리지 마.
3. 시트 안의 그림끼리 겹치지 않게, 칸 사이를 넉넉히 띄워. 칸 테두리선과 글자, 라벨은 넣지 마.
4. 모든 그림은 같은 크기 비율, 같은 방향(물고기와 요리는 아래 설명대로).

[공통 그림체]
Same art style as the attached reference images: soft anime illustration, delicate linework, warm natural lighting, gentle painterly shading, cozy Korean seaside village mood, high resolution, pure flat white background (#FFFFFF), no checkerboard, no text, no labels, no borders.

1. 물고기 도감 시트
Fish illustration sheet, 5 fish in a single row with wide equal spacing, all in side view facing left, same scale, realistic but soft anime-painterly style: black rockfish (Sebastes inermis, dark brown-gray, very large eyes, spiny dorsal fin), chub mackerel (blue-green back with dark wavy stripes, silver belly), fat greenling (Hexagrammos otakii, mottled brown and olive), Japanese halfbeak (very slender silver body, long needle lower jaw with red tip), black sea bream (Acanthopagrus schlegelii, silver-black with faint vertical bands, tall body).

2. 물고기 반응 시트 (낚을 때 연출용)
Same 5 fish as #1, each one drawn twice side by side: jumping and wriggling out of the water with small water splashes, and lying calm. 5 rows x 2 columns, wide spacing.

3. 요리 시트
Korean dish illustration sheet, 5 dishes in top-down view, 3 on the top row and 2 on the bottom row, wide equal spacing, same plate size: grilled black rockfish on a round ceramic plate with coarse salt; braised mackerel with radish in a shallow pot with glossy red-brown sauce; spicy greenling fish stew in a black earthenware pot with red broth, tofu and crown daisy, steam rising; spicy raw halfbeak salad with red chili dressing, cucumber and sesame on a ceramic plate; aged black sea bream sashimi neatly sliced on a wooden board with shiso leaves and a small dish of soy sauce.

4. 손님 시트 (푸드트럭 손님)
Character sheet of 6 different everyday Korean village customers, full body front view, standing, each about the same height, in one row with wide spacing: a fisherman uncle in a vest and rubber boots, a young mother with a toddler, a high school student in uniform, a delivery postman, an elderly grandfather with a cane, a tourist couple's girlfriend with a camera. Friendly faces.

5. 손님 반응 얼굴 시트
Head and shoulders of the same 6 customers as #4, each with 3 expressions in a row (delighted, so-so, disappointed), 6 rows x 3 columns, wide spacing.

6. 여울 감정 시트
Expression and emotion sheet of the same young woman as the attached reference (brown hair in a high messy bun, white hooded jacket, black harness bag): upper body, 4 columns x 3 rows, same size: numb and empty after losing her sense of taste, quietly holding back tears, crying, shocked holding a phone, worried, focused while fishing, sharp when striking a fish, bright proud smile holding a fish, tasting food with a single tear, nervous waiting for a customer, relieved smile, peaceful eyes closed.

7. 막순 할머니 감정 시트
Expression sheet of the same grandmother as the attached reference (gray curly short hair, red polka-dot headband, purple floral blouse, black apron): upper body, 3 columns x 2 rows: stern, warm smile, worried, scolding with ladle raised, teary proud, laughing.

8. 고양이 고등어 소품 시트
The same gray tabby cat as the attached reference with red collar and gold bell, 6 poses in two rows with wide spacing: meowing while looking to the left, meowing while looking to the right, eating a fish, sitting on a blue cooler box, sleeping on a truck roof rack, stretching.
```

---

## 걷는 언덕 마을 (2026-10-28)

마을을 직접 걸어 다니게 바꾼다(사용자 결정: B 세로 언덕 마을). 남해 언덕 마을처럼 아래 항구에서 위 언덕으로 골목이 오르고, 화면은 여울을 따라 위아래(조금 좌우)로 흐른다. 그래서 **내려다본 마을 지도 한 장**과 **앞·뒤로 걷는 SD**가 새로 필요하다.
- 지도는 화면보다 크게 확대해서 쓴다(가로 약 1.8배). 이미지 AI가 낼 수 있는 **가장 높은 해상도**로 받는다.
- 먼저 **지도 1 낮**을 뽑고, 받으면 길과 건물 문 자리를 재서 걷는 길(코드)을 만든다. 새벽·저녁·밤은 지도 1 결과를 첨부해 같은 구도로 시간만 바꾼다.
- SD는 지금 옆걸음 그림과 같은 사람으로 보이게 지금 SD 시트를 꼭 첨부한다.

### 지도 1. 물비늘포 언덕 마을 낮 (`town-map-day`)

```
Create one vertical game map illustration. Attached: my current watercolor village background illustrations (match their palette, light and painterly texture exactly) and my current SD characters (only for scale reference, do not draw them).

Scene: the small harbor village Mulbinulpo on the south coast of Korea, built on a hillside that rises from the sea. Seen from a high three-quarter bird's-eye view, about 45 degrees from above, like a cozy RPG town map. The whole village fits in the picture.

Layout from bottom to top:
- Bottom 20 percent: calm sea with small waves, a concrete pier with two small fishing boats. At the bottom right edge a breakwater with gray tetrapods leads off the picture toward a small red lighthouse.
- Above the sea: a wide flat harbor plaza of worn concrete running across the whole width. On the left side of the plaza a fish auction shed with a blue metal roof and stacked blue and yellow fish crates. On the right side of the plaza a small blue and white food truck with a striped awning and a little chalkboard, parked with empty space in front of it.
- Middle: one wide stone paved path starts at the middle of the plaza and climbs the hill in a gentle S shape (first to the left, then to the right, then to the left again). Along the first bend, a tiny bait and tackle shop with a green and white striped awning and fishing rods by the door. At the second bend, a traditional Korean hanok house with a dark tiled roof, wooden lattice doors, warm paper lanterns and a small yard (the grandmother's house).
- Upper part: small houses with colorful roofs (blue, orange, mint) and painted walls, a cozy guesthouse with a pink door and a laundry line, potted plants, low stone walls, a few camellia trees.
- Top: the path ends at a small lookout with a wooden bench and a big old tree, green hills and sky above.

Very important for a game map:
- The S shaped path and the harbor plaza must be wide, flat, clearly visible and empty everywhere, about three times wider than a person, so small characters can walk on them. Every building door opens onto the path or the plaza with a little empty space in front of it.
- Nothing covers the path: no people, no animals, no cars except the parked food truck, no crates or plants on the path itself, no overhanging roofs.
- Keep the left and right 8 percent of the picture as simple scenery (hills, sea, trees) because the screen will crop them a little.
- Light from the upper left, bright clear early afternoon, deep blue sea with sparkles.

Style: soft painterly watercolor illustration matching the attached backgrounds (same palette, brush texture and level of detail), gentle anime background art, clean readable shapes. Vertical 9:16, the highest resolution you can make. No people, no text, no readable signs, no letters, no signature, no watermark.
```

#### 지도 1 받음 (2026-10-28, 941×1672) → 다시 뽑기
- 배치는 좋았다(광장 왼쪽 위판장, 오른쪽 트럭, 첫 굽이 미끼가게, 둘째 굽이 한옥, 위 민박, 꼭대기 벤치와 큰 나무).
- 고칠 점: 원근이 세서 위로 갈수록 집과 길이 작아진다(SD 크기·걷는 속도를 위치마다 바꿔야 하고 꼭대기에선 너무 작다). 등대가 부두 모서리에 그냥 서 있고 테트라포드가 바다에 흩어져 어색하다. 해상도가 작다(뽑는 도구의 고화질·업스케일로 가로 1800px 이상).
- 앞 지도를 배치 참고로 첨부하고 아래 프롬프트로 다시 뽑는다. 새벽·저녁·밤은 새 지도를 첨부해 시간만 바꾼다.

```
Create one vertical game map illustration. Attached: (1) my previous village map: use it as the layout reference and keep the same places in the same order, (2) my watercolor village background illustrations: match their palette, light and painterly texture exactly.

Scene: the small harbor village Mulbinulpo on the south coast of Korea, built on a hillside that rises from the sea.

Camera, very important: a high three-quarter view from about 45 degrees above with almost no perspective, like a hand painted 2D RPG town map. No strong vanishing point. Buildings, trees, stone walls and the path keep nearly the same size and the same angle from the bottom of the picture to the top. Only the far islands and the sea horizon in the top corners may look small and distant.

Layout from bottom to top (same as the attached map):
- Bottom: calm sea. A harbor basin on the left with a concrete pier and two small fishing boats moored inside it.
- Breakwater, very important: from the right end of the harbor plaza, one long straight concrete breakwater runs out into the sea diagonally toward the bottom center of the picture and protects the harbor basin. Its top is a flat walkway about three people wide with a low metal railing on both sides, clearly walkable from the plaza. Gray tetrapods are stacked neatly in a tidy row only along the outer sea side of the breakwater, never inside the harbor and never on the walkway. A small red cylindrical lighthouse with a white balcony railing stands on a round concrete base at the far tip of the breakwater. Waves break softly on the tetrapods.
- Above the sea: a wide flat harbor plaza of worn concrete across the whole width. On the left, a fish auction shed with a blue metal roof and stacked blue and yellow fish crates. On the right, near the start of the breakwater, a small blue and white food truck with a striped awning, little stools and a chalkboard, parked with empty space in front of it.
- Middle: one wide stone paved path climbs the hill in a gentle S shape. At the first bend, a tiny bait and tackle shop with a green and white striped awning and fishing rods by the door. At the second bend, a traditional Korean hanok house with a dark tiled roof, wooden lattice doors, warm paper lanterns and a small yard with an open gate facing the path.
- Upper part: small houses with blue, orange and mint roofs, a guesthouse with a pink door and a laundry line, potted plants, low stone walls, camellia trees.
- Top: the path ends at a small lookout with a wooden bench and a big old tree. Green hills, and the sea with small islands in the top corners.

Game map rules:
- The S shaped path, the plaza and the breakwater walkway are wide, flat, clearly visible and completely empty, at least three times wider than a person, the same width from bottom to top.
- Every building door opens onto the path or the plaza with a little empty space in front of it.
- Nothing covers the path: no people, no animals, no cars except the parked food truck, no crates or plants on the path, no overhanging roofs.
- Keep the left and right 8 percent of the picture as simple scenery (trees, rocks, sea) because the screen will crop them a little.
- Light from the upper left, bright clear early afternoon, deep blue sea with sparkles.

Style: soft painterly watercolor illustration matching the attached backgrounds (same palette, brush texture and level of detail), gentle anime background art, clean readable shapes, crisp fine details. Vertical 9:16, the highest resolution available, at least 1800 pixels wide. No people, no text, no readable signs, no letters, no signature, no watermark.
```

#### 지도 1 확정 (2026-10-28, 세 번째 그림, 가로등 있는 것)
- 사용자가 고른 그림: 광장·부두 가로등, 넓은 항구, 배 둘. 원근은 남아 있어 SD 크기·걸음을 높이에 따라 코드로 맞춘다. 해상도 941×1672(더 큰 파일이나 업스케일을 받으면 바꾼다).
- 방파제는 오른쪽 아래에 짧게 보이고 화면 밖으로 이어진다. 오른쪽 아래 끝으로 걸어가면 낚시 화면으로 넘어가고 "방파제 →" 표지를 단다. 등대를 그림에 넣고 싶으면 아래 (선택) 편집 프롬프트.
- 낮 그림에 가로등이 있으니 새벽·저녁·밤은 **같은 가로등을 켜고 끄기만** 한다. 새 가로등이나 물건을 더하지 않는다.

#### 지도 기준 바꿈 (2026-10-28, 사용자 결정): 노을 지도
- 사용자가 고른 기준 그림은 S자 길·할머니댁 한옥·광장 가로등·오른쪽 아래 방파제와 빨간 등대가 있는 **노을 지도**(`godot/art/backgrounds/town-map-dusk.webp`, 941×1672)다. 걷는 길 데이터는 이 배치로 만든다.
- 아래 "지도 1 최종"(네 번째 그림)은 배치가 달라서 레포에서 뺐다. 낮·새벽·밤은 **노을 지도를 첨부**해 "지도 낮" 프롬프트와 지도 2~4의 새벽·밤 프롬프트로 다시 뽑는다.

```
Repaint the attached village map illustration in bright daylight. Keep exactly the same composition, camera angle, buildings, S shaped path, plaza, pier, breakwater, lighthouse, tetrapods, boats, food truck, street lamps and every object in the same place and size. Do not add or remove any object. Only change the time of day and lighting.

Day: bright clear early afternoon, blue sky with a few soft white clouds, light from the upper left, deep blue sea with white sparkles, green islands on the horizon, fresh green trees and colorful flowers. The street lamps and the windows are off. The food truck is open with its awning out. The path, the plaza and the breakwater walkway stay clearly visible and empty.

Style: the same soft painterly watercolor as the attached map, gentle anime background art. Vertical 9:16, the same size as the attached image or larger. No people, no text, no signature, no watermark.
```

#### 노을 지도에서 시간만 바꾸기 (편집으로 빛만, 2026-10-28)
노을 지도를 첨부하고 도구의 편집 기능으로 뽑는다. 해가 오른쪽 바다로 지니 오른쪽이 서쪽: 낮 해는 왼쪽 위, 새벽은 왼쪽 산 너머가 밝아 온다. 받으면 노을 지도와 겹쳐 보고 어긋나면 맞춘다.

```
Edit the attached image. This is a relighting task only: produce the exact same picture at a different time of day, as if the same camera took a photo of the same village a few hours earlier.

Do not change anything except light, sky and colors. Keep pixel for pixel the same: the camera angle and framing, the S shaped stone path and every curve and step of it, the harbor plaza and its paving lines, every house with the same roof color, shape, windows and position, the hanok house, the bait and tackle shop with the striped awning, the fish auction shed with the same crates, the food truck in the same place, every street lamp and utility pole, the stone walls, every tree and flower bush, the big tree and the bench at the top, the cliffs on the right, the pier with the same bollards and tires, the two fishing boats in the same position, the breakwater with its railing, the tetrapods and the red lighthouse. Do not add, remove, move or resize anything.

New time: bright clear early afternoon. The sun is high in the upper left outside the picture, so short soft shadows fall toward the lower right. Clear blue sky with a few soft white clouds, the sea on the right is deep blue with white sparkles and a clear horizon. Trees are fresh green, flowers are bright. All street lamps, the lighthouse lamp and the windows are turned off. The food truck is open in daylight. The path, the plaza and the breakwater walkway stay empty.

Style: exactly the same soft painterly watercolor, brush texture and level of detail as the attached image. Same size as the attached image or larger. No people, no text, no signature, no watermark.
```

```
Edit the attached image. This is a relighting task only: produce the exact same picture at a different time of day, as if the same camera took a photo of the same village just before sunrise.

Do not change anything except light, sky and colors. Keep pixel for pixel the same: the camera angle and framing, the S shaped stone path and every curve and step of it, the harbor plaza and its paving lines, every house with the same roof color, shape, windows and position, the hanok house, the bait and tackle shop with the striped awning, the fish auction shed with the same crates, the food truck in the same place, every street lamp and utility pole, the stone walls, every tree and flower bush, the big tree and the bench at the top, the cliffs on the right, the pier with the same bollards and tires, the two fishing boats in the same position, the breakwater with its railing, the tetrapods and the red lighthouse. Do not add, remove, move or resize anything.

New time: early dawn, the sun has not risen yet. The sky glows soft pink and pale gold behind the hills on the upper left and fades to lavender and light blue over the sea on the right. Cool blue shadows everywhere, a thin morning mist over the sea and the harbor. The street lamps are still on with a soft warm glow, a few windows glow warm yellow, the red lighthouse lamp is on. The sea is calm and reflects pink and lavender. The food truck is closed. The path, the plaza and the breakwater walkway stay empty.

Style: exactly the same soft painterly watercolor, brush texture and level of detail as the attached image. Same size as the attached image or larger. No people, no text, no signature, no watermark.
```

```
Edit the attached image. This is a relighting task only: produce the exact same picture at a different time of day, as if the same camera took a photo of the same village late at night.

Do not change anything except light, sky and colors. Keep pixel for pixel the same: the camera angle and framing, the S shaped stone path and every curve and step of it, the harbor plaza and its paving lines, every house with the same roof color, shape, windows and position, the hanok house, the bait and tackle shop with the striped awning, the fish auction shed with the same crates, the food truck in the same place, every street lamp and utility pole, the stone walls, every tree and flower bush, the big tree and the bench at the top, the cliffs on the right, the pier with the same bollards and tires, the two fishing boats in the same position, the breakwater with its railing, the tetrapods and the red lighthouse. Do not add, remove, move or resize anything.

New time: late night. Deep navy blue sky full of stars with a bright moon over the sea on the right, silver moonlight shimmering on the water. The existing street lamps shine brightly and cast warm round pools of light on the path, the plaza and the pier, so the whole path is easy to see. Warm yellow light in many house windows and the hanok. The food truck is closed with its awning rolled up and its lights off. The red lighthouse lamp glows, and small lights glow on the two fishing boats. Everything outside the lamp light is cool blue and dark but still readable. The path, the plaza and the breakwater walkway stay empty.

Style: exactly the same soft painterly watercolor, brush texture and level of detail as the attached image. Same size as the attached image or larger. No people, no text, no signature, no watermark.
```

#### 지도 1 최종 (2026-10-28, 약도로 다시 그린 네 번째 그림) → 배치가 달라 쓰지 않음
- 약도대로 방파제(난간 있는 길, 바깥쪽 테트라포드)와 빨간 등대가 그림 안에 들어왔다. 원근이 많이 줄어 위아래 집 크기가 비슷하다. 가로등이 길 따라 여럿 있고 낮에도 불이 켜진 늦은 오후 빛이다.
- `godot/art/backgrounds/town-map-day.webp`(941×1672). 더 큰 파일이나 업스케일을 받으면 바꾼다. 새벽·저녁·밤은 이 그림을 첨부해 아래 지도 2~4 프롬프트로 뽑는다.

#### 지도 1 약도와 함께 다시 그리기 (방파제·등대를 그림 안에)
약도 `docs/art/town-layout.png`(글자 없는 단색 배치도, `scripts/town_layout.py`로 그린다. 이름표판 `town-layout-labeled.png`)를 고른 낮 지도와 같이 첨부한다.

```
Repaint the attached village map illustration so that it follows the attached flat color layout sketch. Attached: (1) the flat layout sketch: a placement guide only, (2) my village map illustration: keep its painterly watercolor style, colors, light, level of detail and all its buildings, (3) optionally my other watercolor backgrounds for the palette.

How to read the sketch: beige is the stone paved S shaped path and the harbor plaza, gray bands are low stone walls and the concrete pier, blue roofs, orange roofs and the mint roof are small houses, the dark gray roof with the brown wall is the traditional hanok house, the blue roof with the green and white striped awning is the bait and tackle shop, the big blue roof at the bottom left with yellow and blue boxes is the fish auction shed, the white and blue box with wheels is the food truck, yellow dots on poles are street lamps, white shapes are two small fishing boats, green blobs are trees, the big tree with the brown bench at the top is the lookout.

Bottom right, very important: follow the sketch here. A straight concrete breakwater starts at the right end of the harbor plaza and runs down into the sea. Its top is a flat walkway with a low metal railing on both sides. Gray tetrapods are stacked neatly along its outer right side only. A small red cylindrical lighthouse with a white balcony railing stands on a round concrete base at the tip of the breakwater, fully inside the picture. Waves break softly on the tetrapods.

Keep everything else as it is in the attached illustration: the same camera angle, the same places, the same street lamps, the same boats and the same food truck. The path, the plaza and the breakwater walkway stay wide, flat and completely empty.

Style: soft painterly watercolor illustration exactly like the attached map, gentle anime background art, crisp fine details. Vertical 9:16, the highest resolution available, at least 1800 pixels wide. Do not copy the flat colors or hard edges of the sketch. No people, no text, no letters, no signature, no watermark.
```

#### 지도 2~4. 새벽 · 저녁 · 밤 (최종 낮 지도를 첨부해 시간만 바꾼다, 2026-10-28 방파제·등대·길가 가로등 반영)

```
Repaint the attached village map illustration at dawn. Keep exactly the same composition, camera angle, buildings, path, plaza, pier, breakwater, lighthouse, tetrapods, boats, food truck, street lamps and every object in the same place and size. Do not add or remove any object. Only change the time of day and lighting.

Dawn: the sun has not risen yet, soft pink and lavender sky with a pale gold glow on the horizon at the left, cool blue shadows, light mist floating over the sea and between the islands. The existing street lamps are still on with a soft warm glow, a few windows glow warm yellow, and the lighthouse lamp is on. Calm sea with soft pink reflections. The path, the plaza and the breakwater walkway stay clearly visible and empty.

Style: the same soft painterly watercolor as the attached map, gentle anime background art. Vertical 9:16, the same size as the attached image or larger. No people, no text, no signature, no watermark.
```

```
Repaint the attached village map illustration at sunset. Keep exactly the same composition, camera angle, buildings, path, plaza, pier, breakwater, lighthouse, tetrapods, boats, food truck, street lamps and every object in the same place and size. Do not add or remove any object. Only change the time of day and lighting.

Sunset: warm golden orange light from the left, long soft shadows, orange and pink sky with glowing clouds, the sea reflecting orange and gold. The existing street lamps glow warmly. The food truck is open with its string lights and little lamps glowing, paper lanterns are lit at the hanok house, and many windows glow. The path, the plaza and the breakwater walkway stay clearly visible and empty.

Style: the same soft painterly watercolor as the attached map, gentle anime background art. Vertical 9:16, the same size as the attached image or larger. No people, no text, no signature, no watermark.
```

```
Repaint the attached village map illustration at night. Keep exactly the same composition, camera angle, buildings, path, plaza, pier, breakwater, lighthouse, tetrapods, boats, food truck, street lamps and every object in the same place and size. Do not add any new lamps or objects. Only change the time of day and lighting.

Night: deep blue night sky with stars and a bright moon, moonlight shimmering on the sea. The existing street lamps along the path, on the plaza and on the pier shine brightly and cast soft warm pools of light on the ground, so the whole path is easy to see. Warm yellow windows in the houses. The food truck is closed with its awning rolled up and its lights off. The red lighthouse lamp glows at the tip of the breakwater, and small lights glow on the fishing boats. The path, the plaza and the breakwater walkway stay clearly visible and empty.

Style: the same soft painterly watercolor as the attached map, gentle anime background art. Vertical 9:16, the same size as the attached image or larger. No people, no text, no signature, no watermark.
```

- 마을 사람(손님 12명) 앞·뒤 걷기는 미룬다. 처음엔 길가 자리에 서 있거나 광장을 옆으로만 걷는다(지금 옆걸음 그림).

#### (선택) 오른쪽 아래에 등대 더하기 (편집이 되는 도구에서, 시간대 그림을 뽑기 전에)

```
Edit the attached village map illustration. Keep everything exactly the same: composition, camera angle, colors, buildings, path, plaza, boats, food truck and street lamps. Only change the bottom right corner of the picture.

In the bottom right corner, extend the short concrete breakwater that starts at the right end of the pier: it continues a little further into the sea as a flat walkway with a low metal railing, gray tetrapods are stacked neatly along its outer sea side, and a small red cylindrical lighthouse with a white balcony railing stands on a round concrete base at its tip, fully inside the picture. Waves break softly on the tetrapods. Match the same soft painterly watercolor style and lighting.

No people, no text, no signature, no watermark.
```

### SD 3-1. 여울 앞·뒤 걷기 (언덕 마을용)

```
Create one game SD character sheet. Attached: my current SD sheet of the heroine (match her design, chibi proportions, line art and coloring exactly) and watercolor background illustrations from my game (color palette and lighting only).

Character: Han Yeoul, a 29-year-old Korean woman. Long brown hair pulled up into a high messy bun with a few loose strands, brown eyes, calm slightly tired face. Oversized cream-white hooded jacket with toggle buttons, plain white t-shirt, black crossbody harness bag with small blue accents, dark brown cargo jogger pants, chunky white-gray sneakers.

The figures are seen from slightly above, about 20 degrees, because they will walk on a high-angle village map.
Row 1, facing the viewer (walking down the map toward the camera): 1. standing still, 2. walking with the right foot forward, 3. walking with the left foot forward.
Row 2, back view (walking up the map away from the camera, we see the bun and the back of the jacket and bag): 1. standing still, 2. walking with the right foot forward, 3. walking with the left foot forward.
Walking frames have a small natural arm swing and a slight bounce.

Style: cute chibi SD, 2.5 heads tall, big head, small rounded body, short limbs, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Layout: 2 rows x 3 columns, full body, every figure exactly the same scale and standing on the same baseline in its row, generous empty space between figures so nothing touches or overlaps.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow, no effect marks.
```

### SD 3-2. 고등어 앞·뒤 걷기 (언덕 마을용)

```
Create one game SD character sheet. Attached: my current SD sheet of the cat (match its design, chibi proportions, line art and coloring exactly) and watercolor background illustrations from my game (color palette and lighting only).

Character: Godeungeo, a small silver gray mackerel tabby street cat with dark stripes, white chest, muzzle and paws, yellow green eyes, pink nose and inner ears, a red collar with a tiny gold bell and an old handmade wooden fishing float tied to the collar (faded red and white, no letters).

The cat is seen from slightly above, about 20 degrees, because it will walk on a high-angle village map.
Row 1, facing the viewer (walking toward the camera): 1. sitting, 2. walking with the front right paw forward, 3. walking with the front left paw forward.
Row 2, back view (walking away, tail up and slightly curved): 1. sitting seen from behind, 2. walking with the front right paw forward, 3. walking with the front left paw forward.

Style: cute chibi SD animal, big round head, small body, soft anime style, clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Layout: 2 rows x 3 columns, every figure exactly the same scale and standing on the same baseline in its row, generous empty space between figures so nothing touches or overlaps.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow, no effect marks.
```

### 손님 3. VIP 손님 SD 시트 (넷, 여울호 VIP 예약)

VIP 손님 넷은 아직 그림이 없어 손님 그림을 빌려 쓴다(`diner.json` vips art: 블로거→관광객, 호텔 주방장→민박집 사장님, 요트 손님→택시 기사, 여행 작가→할아버지). 받으면 `godot/art/customers/<id>-stand|walk|happy|sad.png`로 자르고 `art`를 자기 id로 바꾼다.

```
Create one game SD character sheet. Attached: watercolor background illustrations from my game (color palette and lighting only) and my current SD customer sheet (match its chibi proportions, line art, coloring and pose style exactly).

Characters, one per row. They are special guests who came from the city, so they look a little more stylish than the village people:
Row 1: Food blogger young woman: wavy shoulder length hair, beige beret, round glasses, cream trench coat over a striped shirt, small crossbody bag, a smartphone in one hand and a camera strap around the neck (no logos).
Row 2: Hotel head chef, a man in his fifties: neat short gray hair and trimmed gray beard, white double breasted chef jacket under an open navy coat, dark trousers, polished shoes, a rolled leather knife bag under his arm.
Row 3: Yacht guest, a cheerful man in his forties: navy captain cap without any logo, sunglasses, white linen shirt with rolled sleeves, light khaki shorts, boat shoes, a coiled rope keychain on his belt.
Row 4: Travel writer woman in her thirties: short neat hair, khaki vest with many pockets over a white shirt, jeans, hiking shoes, a big backpack, a small notebook and a pen in her hands.

Columns, the same for every row, every figure facing LEFT in side or three quarter view:
1. standing still, relaxed, waiting to order
2. walking to the left, one foot forward, a small bounce
3. delighted after tasting: both arms up or hands clapping, big happy smile, a little hop
4. disappointed: shoulders dropped, head slightly down, small frown

Style: cute chibi SD, 2 to 2.5 heads tall, big head, small rounded body, short limbs, soft anime face, clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Layout: 4 rows x 4 columns, full body, every figure exactly the same scale and standing on the same baseline in its row, generous empty space between figures so nothing touches or overlaps, props stay inside each figure's own space.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no borders, no frames, no grid lines, no ground shadow, no speech bubbles, no effect marks.
```

### 대화 4. 미식 평론가 표정 시트 (특별 손님, 2026-10-28)

여울호 특별 손님(이야기 인물). 받으면 `portraits/critic*`으로 자르고 `dialogue.json` speakers에 `critic`(faces default·happy·annoyed·taste), `diner.json` specials에 넣는다.

```
Create a 2x2 character expression sheet of standing dialogue illustrations for a mobile game visual novel scene. Attached: my watercolor background illustrations (match their palette) and my current dialogue illustrations of other characters (match their art style, line weight and framing exactly).

Character: a famous Korean food critic, a woman in her fifties who came down from Seoul to judge the little food truck. Sharp but fair. Silver gray chin length bob haircut, thin black rimmed glasses, light makeup, charcoal tailored long coat over a cream turtleneck, a deep green silk scarf, a small black leather notebook and a fountain pen.

Layout: four panels of exactly the same size, same framing from the top of the head to just above the knees, same three-quarter pose facing slightly left, same scale and same position in every panel so the panels can be swapped during dialogue.
Panel 1: calm appraising look, notebook held closed at her side, one eyebrow very slightly raised.
Panel 2: genuinely impressed, a small surprised smile, pen paused above the open notebook.
Panel 3: disappointed, lips pressed thin, closing the notebook with a snap, looking away.
Panel 4: tasting with her eyes closed, savoring, holding chopsticks near her lips.

Style: soft anime illustration, delicate clean line art, subtle natural facial acting, soft cel shading with a light watercolor texture, light from the upper left.
Background: plain flat light gray #EDEDED in every panel. Keep hair strands as clean lines without the gray background trapped between them, no glow or white outline around the hair. No text in the notebook, no labels, no numbers, no panel borders, no grid lines, no ground shadow, no checkerboard, no signature, no watermark.
```

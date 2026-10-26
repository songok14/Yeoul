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
- 2026-10-22: **손님 SD 시트 두 장**(6명 × 서기·걷기·기쁨·실망). `scripts/sd_cells.py`로 잘라 `godot/art/customers/<id>-<포즈>.png`. 두 번째 시트는 체크무늬가 박혀 있어도 지워진다. 흰 수염 할아버지는 `--hair=0`.
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
Row 2: idle pose with hands in jacket pockets, waving hello with a small smile, walking side view facing left with left foot forward, walking side view facing left with right foot forward.

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
Row 1: sitting front view, sitting side view facing left, walking side view facing left with front left paw forward, walking side view facing left with front right paw forward.
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

Make 4 Korean seaside village people, each in 3 poses in one row: walking step A (side view facing left), walking step B (side view facing left, other leg forward), standing front view. 4 rows x 3 columns, wide equal spacing, same scale, same ground line, full body with feet visible.
Row 1: a village fisherman in his 50s with a knit cap, navy vest and yellow rubber boots, carrying a blue fish basket.
Row 2: an auntie from the fish market in a floral apron and arm sleeves, rubber gloves, carrying a red plastic tub.
Row 3: an elementary school boy with a backpack and a toy fishing net.
Row 4: a young delivery woman on foot with a cap and a small parcel box.
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

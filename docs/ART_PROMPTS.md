# 그림 프롬프트

이미지 AI에 그대로 붙여 넣는 프롬프트 모음.

## 지금 방향 (2026-10 유니티 이전부터)

| 쓰임 | 그림체 | 비고 |
|---|---|---|
| 배경 | 수채화풍 일러스트 (`public/assets/bg-*.webp`) | 지금 그림을 그대로 쓴다 |
| 인게임 캐릭터·물체 | 귀여운 SD (2~2.5등신) | 여울, 고양이 고등어, 막순 할머니, 노씨, 차세라, 손님, 트럭 여울호, 물고기, 요리 |
| 컷신 | 애니메이션 채색 일러스트 | 이야기 장면에서만. 여울 감정 연기를 더 섬세하게 다듬는다 |

### 주는 방식 (규칙)
- 그림 **한 장당 코드 블록 하나**. 블록 하나만 복사해서 붙여 넣으면 되도록 매번 모든 조건을 다시 적는다.
- 붙여 넣을 때 **참조 그림을 같이 첨부**한다. 배경 일러스트 한두 장(색감과 빛 참고)과 그 캐릭터의 지금 그림(디자인 참고).
- SD는 **시트형**: 한 장에 여러 포즈·표정을 칸마다 넉넉히 띄워서. 모든 칸은 같은 크기 비율, 같은 바닥선.
- 시트 배경은 **연한 회색 단색 #EDEDED**. 투명 체크무늬, 글자, 라벨, 숫자, 테두리, 칸 선, 바닥 그림자 금지. (`scripts/art.py`가 가장자리와 이어진 밝은 무채색을 지운다)
- 빛은 모든 그림에서 **왼쪽 위**에서. 배경 그림의 맑은 남해 오후 빛과 맞춘다.
- 유니티 뼈대 애니메이션용 **부품 분리 시트**는 따로 뽑는다.
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
- 2026-10: **SD 1 여울 기본 시트**, **SD 5 고등어 시트**. 디자인과 찌가 잘 나왔다. `scripts/sd_sheet.py`로 여울 8장, 고등어 12장을 잘라 `unity/HanjeopsiBada/Assets/Resources/Art/`에 넣었다.
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

## SD 4. 여울 부품 분리 시트 (유니티 뼈대 애니메이션용)

```
Create one cut-out puppet parts sheet for 2D skeletal animation in Unity. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the heroine (her design).

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

## SD 6. 고등어 부품 분리 시트 (유니티 뼈대 애니메이션용)

```
Create one cut-out puppet parts sheet for 2D skeletal animation in Unity. Attached images: watercolor background illustrations from my game (color palette and lighting only) and the current illustration of the cat (its design).

Character: Godeungeo, a cute chibi SD stray cat, side view facing left. Silver-gray mackerel tabby with dark stripes, white chest, muzzle and paws, yellow-green eyes, pink nose, red collar with a small gold bell and a tiny old wooden fishing float (faded red and white, no letters). Clean thin dark brown line art, soft cel shading with a light watercolor texture, light from the upper left.

Draw every body part as a separate piece, all at the same scale as if taken apart from one side-view cat, arranged in a tidy layout with wide empty gaps so no piece touches another:
head without eyes and mouth, left ear, right ear, body with collar, front left leg, front right leg, back left leg, back right leg, tail split into three segments from base to tip, the bell and float as one separate piece.
Joints must have rounded overlapping ends so the parts can rotate without gaps.
Below them, separate face pieces at the same scale: eyes open, eyes closed, happy curved eyes, sleepy half closed eyes, mouth closed, mouth open meow, mouth eating.

Background: plain solid light gray #EDEDED. No checkerboard, no text, no letters, no labels, no numbers, no arrows, no borders, no frames, no grid lines.
```

## 다음 SD 차례
막순 할머니 → 노씨 → 손님 6명(위 디자인) → 트럭 여울호 → 물고기 5종 → 요리 5종 → 차세라. 여울·고등어 기본 시트는 받았다. 다음은 SD 2(여울 동작), SD 3(표정)이고 그 뒤 같은 형식으로 준다.

---

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

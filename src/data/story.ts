// 프롤로그와 1장 엔딩 대사

/** 타이틀 제목과 부제. 웹은 1장뿐이라 부제 하나 (고닷은 챕터마다 texts.json title) */
export const TITLE = { name: '여울호', subtitle: '짠맛이 돌아온 봄' };

export type StoryBg = 'kitchen' | 'ash' | 'phone' | 'harbor' | 'notice' | 'storage' | 'memory' | 'breakwater' | 'grill' | 'night' | 'dawn' | 'seize';

export interface StoryPage {
  bg: StoryBg;
  text: string;
  /** 페이지를 보여 준 뒤 실행할 연출 */
  event?: 'salt' | 'cat';
  /** 배경 위에 올릴 여울 그림 */
  pic?: string;
}

export const PROLOGUE_1: StoryPage[] = [
  { bg: 'kitchen', text: '서울 프렌치 레스토랑 \'르 블랑\'.\n미슐랭 2스타를 받은 그 주에 주방에 불이 났다.' },
  { bg: 'ash', pic: 'y-notaste', text: '깨어나 보니 아무 맛도 냄새도 느껴지지 않았다.\n맛을 모르는 요리사는 그날로 해고됐다.' },
  { bg: 'phone', pic: 'y-phone', text: '그날 밤 전화가 왔다.\n할머니가 식당에서 쓰러지셨다고 했다.' },
  { bg: 'harbor', pic: 'y-back-bag', text: '10년 만에 돌아온 남해 물비늘포.\n바다는 그대로였다.\n나는 여전히 바다가 싫었다.' },
  { bg: 'notice', text: '할머니의 밥집 \'여울식당\'은 빚더미였다.\n90일 뒤 차압 예정.\n30일 안에 1차 상환금 300만 원을 내야 기다려 준다고 했다.' },
  { bg: 'storage', pic: 'y-rod-broken', text: '창고 구석에서 찾은 것들.\n할아버지의 1톤 트럭 \'여울호\'.\n반으로 부러진 아버지의 대나무 낚싯대.\n페이지 절반이 뜯긴 조황일지.' },
  { bg: 'memory', text: '아홉 살 여름. 아버지와 처음 볼락을 낚았다.\n"고기는 먼저 낚은 사람 게 아니야.\n제일 맛있게 먹어주는 사람 거지."' },
  { bg: 'memory', text: '열한 살 겨울. 아버지는 전설의 돗돔 \'물그림자\'를 쫓아 나가서 돌아오지 않았다.\n그날부터 생선은 쳐다보지도 않았다.' },
  { bg: 'breakwater', text: '할 일이 없어 방파제에 나왔다.\n손에는 할머니가 쓰던 이천 원짜리 낚싯대.\n...한 마리만 낚아 볼까.' },
];

export const PROLOGUE_2: StoryPage[] = [
  { bg: 'grill', text: '손바닥만 한 볼락 한 마리.\n할머니 몰래 불을 피워 대충 구웠다.\n소금은 감으로 쳤다.', event: 'salt' },
  { bg: 'grill', pic: 'y-salty-tear', text: '한 입 베어 문 순간.\n\n...짜다.' },
  { bg: 'grill', pic: 'y-salty-tear', text: '짠맛이 느껴진다.\n6개월 만의 맛이었다.' },
  { bg: 'grill', text: '그때 고등어무늬 고양이 한 마리가 남은 볼락을 물고 달아났다.\n목에 낡은 찌 하나가 달랑거렸다.', event: 'cat' },
  { bg: 'night', pic: 'y-journal', text: '내 손으로 낚은 물고기라면.\n어쩌면 맛을 되찾을 수 있을지도 모른다.\n\n그리고 여울식당도.' },
];

export const CLEAR_PAGES: StoryPage[] = [
  { bg: 'dawn', text: '여울식당의 1차 상환금 300만 원을 모두 갚았다.\n은행 직원은 통지서에 도장을 하나 더 찍어 주었다.' },
  { bg: 'dawn', text: '할머니는 아무 말 없이 볼락구이 한 접시를 내밀었다.\n이번엔 짠맛 말고 다른 맛도 조금 났다.' },
  { bg: 'storage', text: '그날 밤 노씨가 가게 문틈에 쪽지를 끼워 두었다.\n"동해. 순자 할매를 찾아가라."\n쪽지 사이에 낯익은 글씨의 일지 한 장이 들어 있었다.' },
  { bg: 'night', text: '[조황일지 1장]\n"물비늘포 볼락은 해 질 녘 테트라포드 그늘에 있다.\n여울이는 소금을 아끼면 안 된다. 짜게 먹는 애다."\n\n...아버지 글씨였다.' },
];

export const FAIL_PAGES: StoryPage[] = [
  { bg: 'seize', text: '30일이 지났다.\n여울식당 문에 빨간 딱지가 붙었다.' },
  { bg: 'night', pic: 'y-knees', text: '할머니는 괜찮다고 했다.\n괜찮지 않다는 걸 나는 맛이 아니라 얼굴로 알았다.' },
  { bg: 'night', text: '아직 바다에는 물고기가 있다.\n처음부터 다시 해 보자.' },
];

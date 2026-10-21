using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Hanjeopsi.Game.Boot;
using UnityEngine;
using CoreGame = Hanjeopsi.Core.Game;

namespace Hanjeopsi.Game.DebugTools
{
    /// <summary>
    /// U2 점검 화면. 씬을 옮기기 전에 폰에서 코어 규칙을 직접 돌려 본다.
    /// 미니게임 없이 버튼으로 출조, 경매, 트럭 장사, 납품, 잠자기를 하고
    /// SD 여울·고등어와 마을 배경, 한글 글꼴, 세이브 파일이 폰에서 되는지 본다.
    /// "웹 일치 점검"은 웹 버전에서 뽑은 값과 같은지 폰에서 확인한다.
    /// </summary>
    public class CoreLab : MonoBehaviour
    {
        private const float RefWidth = 1080f;
        private const int LogLines = 6;

        private static readonly string[] YeoulPoses = { "front", "wave", "three-quarter", "side", "walk", "back", "idle" };
        private static readonly string[] CatPoses = { "sit-front", "meow-1", "walk", "loaf", "eat", "jump", "sleep", "stretch", "sit-side", "back" };

        private GameData _data;
        private CoreGame _game;
        private readonly UnityRandom _rng = new UnityRandom();
        private readonly List<string> _log = new List<string>();
        private readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private Font _font;
        private string _loadError;

        private int _yeoulPose;
        private int _catPose;
        private float _time;
        private bool _showChecks;
        private List<string> _checks = new List<string>();

        private GUIStyle _text, _small, _title, _button, _check, _clear;
        private Texture2D _dark, _panel, _buttonBg, _buttonDown;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _font = Resources.Load<Font>("Fonts/GowunDodum-Regular");
            try
            {
                _data = GameDataLoader.Load();
                var save = SaveStore.Load(_data);
                if (save != null)
                {
                    _game = new CoreGame(_data, save);
                    Log("이어하기: " + _game.Save.Day + "일차 " + _game.SlotName());
                }
                else
                {
                    StartNewGame();
                }
                _game.Saved += SaveStore.Write;
            }
            catch (System.Exception e)
            {
                _loadError = "데이터를 읽지 못했다\n" + e.GetType().Name + ": " + e.Message;
                Debug.LogException(e);
            }
        }

        private void Update()
        {
            _time += Time.deltaTime;
        }

        // ───────── 행동 ─────────

        private void StartNewGame()
        {
            if (_game != null) _game.Saved -= SaveStore.Write;
            _game = CoreGame.NewGame(_data, Random.Range(1, 1000000000));
            _game.CompletePrologue();
            _game.Saved += SaveStore.Write;
            SaveStore.Write(_game.Save);
            _log.Clear();
            Log("새 게임 (프롤로그는 건너뜀). 시드 " + _game.Save.Seed);
        }

        /// <summary>출조 한 번 = 캐스팅 6번 = 시간 한 칸. 미니게임 대신 던지면 다 낚는다</summary>
        private void GoFishing()
        {
            var s = _game.Save;
            int casts = _data.Balance.Chapter1.CastsPerTrip;
            int caught = 0;
            for (int i = 0; i < casts; i++)
            {
                if (s.Bait <= 0)
                {
                    Log("미끼가 떨어졌다");
                    break;
                }
                _game.UseBait();
                int hotLane = _rng.RangeInt(-1, 1);
                int lane = _rng.RangeInt(-1, 1);
                bool hot = lane == hotLane;
                int zone = _game.Fishing.ZoneOf(_rng.Next());
                var sp = _game.Fishing.RollSpecies(zone, s.Slot, hot, _rng);
                double size = _game.Fishing.RollSize(sp, hot, _rng);
                string reason = _game.ReleaseReason(sp.Id, size);
                bool released = reason != null || _game.CoolerFull();
                bool isNew = _game.RecordCatch(sp.Id, size, released);
                if (!released) _game.AddToCooler(sp.Id, size);
                caught++;
                string line = sp.Name + " " + size.ToString("0.0") + "cm" + (isNew ? " (새 어종!)" : "");
                if (reason != null) line += " → " + reason;
                else if (released) line += " → 쿨러가 가득 차서 놓아줬다";
                Log(line);
                var unlocked = _game.CheckTasteUnlock();
                if (unlocked != null) Log("미각이 돌아온다: " + unlocked.Name + ". " + unlocked.Effect);
            }
            if (caught > 0)
            {
                _game.UseSlot();
                AfterTime();
            }
        }

        private void SellAll()
        {
            if (!_game.IsMarketOpen())
            {
                Log("경매는 새벽과 아침에만 열린다");
                return;
            }
            var uids = _game.Save.Cooler.Where(f => _game.Fresh.StageOf(f.Fresh) != FreshStage.Spoiled).Select(f => f.Uid).ToList();
            if (uids.Count == 0)
            {
                Log("팔 물고기가 없다");
                return;
            }
            double mul = _rng.Range(0.92, 1.15);
            int total = _game.SellFish(uids, mul);
            Log("경매 " + uids.Count + "마리 낙찰 " + JsMath.Won(total) + " (" + Mathf.RoundToInt((float)mul * 100) + "%)");
        }

        /// <summary>장사 한 번 = 손님 5명 = 시간 한 칸. 아는 맛은 정답 근처, 모르는 맛은 감으로 멈춘다</summary>
        private void RunTruck()
        {
            if (!_game.IsTruckOpen())
            {
                Log("새벽엔 손님이 없다");
                return;
            }
            if (_game.Truck.Cookable(_game.Save).Count == 0)
            {
                Log("쿨러에 요리할 재료가 없다");
                return;
            }
            int served = 0, sales = 0;
            for (int i = 0; i < _data.Balance.Chapter1.CustomersPerShift; i++)
            {
                var menu = _game.Truck.Cookable(_game.Save);
                if (menu.Count == 0) break;
                var recipe = menu[(int)(_rng.Next() * menu.Count)];
                var results = new List<SeasonResult>();
                foreach (var step in recipe.Steps)
                {
                    double target = _game.Truck.RollTarget(step, _rng);
                    bool known = _game.Taste.CanTaste(_game.Save, step.Flavor);
                    double pos = known ? JsMath.Clamp(target + _rng.Range(-0.12, 0.12), 0, 1) : _rng.Next();
                    results.Add(_game.Truck.Judge(pos, target));
                }
                var dish = _game.ServeDish(recipe, results);
                if (dish == null) break;
                served++;
                sales += dish.Pay;
                int mood = _game.Truck.MoodOf(dish.Avg);
                var lines = _game.Truck.ReviewLines(_game.Save, recipe, results, dish.Stage, mood);
                Log(recipe.Name + " " + JsMath.Won(dish.Pay) + " \"" + string.Join(" ", lines) + "\"");
            }
            Log("장사 끝: 손님 " + served + "명 매출 " + JsMath.Won(sales));
            _game.UseSlot();
            AfterTime();
        }

        private void Deliver()
        {
            foreach (var r in _game.Save.Requests)
            {
                if (_game.FishForRequest(r.Id) == null) continue;
                var t = _data.RequestTemplates[r.Template];
                int reward = _game.DeliverRequest(r.Id);
                Log(t.Client + "에게 납품 " + JsMath.Won(reward));
                return;
            }
            Log("납품할 수 있는 의뢰가 없다 (선어 이상, 마릿수)");
        }

        private void FeedCat()
        {
            int n = _game.FeedCat();
            Log(n > 0 ? "고등어가 골골거린다 (호감 " + _game.Save.CatAffinity + "/" + _data.Balance.Cat.MaxAffinity + ")" : "상한 생선이 없다");
            if (n > 0) _catPose = System.Array.IndexOf(CatPoses, "eat");
        }

        /// <summary>살 수 있는 다음 단계 장비 중 가장 싼 것</summary>
        private void BuyGear()
        {
            GearSlot? best = null;
            int bestPrice = int.MaxValue;
            foreach (GearSlot slot in new[] { GearSlot.Rod, GearSlot.Reel, GearSlot.Tackle })
            {
                int next = _game.GearLevel(slot) + 1;
                if (next >= _game.GearCount(slot)) continue;
                int price = _game.GearPrice(slot, next);
                if (price < bestPrice)
                {
                    bestPrice = price;
                    best = slot;
                }
            }
            if (best == null)
            {
                Log("가장 좋은 장비를 다 갖췄다");
                return;
            }
            if (_game.UpgradeGear(best.Value)) Log(GearName(best.Value) + " 장만! (" + JsMath.Won(bestPrice) + ")");
            else if (_game.Save.Money >= _data.Gear.Bait.Price && _game.BuyBait()) Log("장비는 비싸서 미끼만 샀다 (+" + _data.Gear.Bait.Amount + ")");
            else Log("돈이 모자라다");
        }

        private string GearName(GearSlot slot)
        {
            var g = _data.Gear;
            switch (slot)
            {
                case GearSlot.Rod: return g.Rods[_game.Save.Rod].Name;
                case GearSlot.Reel: return g.Reels[_game.Save.Reel].Name;
                default: return g.Tackles[_game.Save.Tackle].Name;
            }
        }

        private void Repay()
        {
            int paid = _game.Repay(_data.Balance.Chapter1.RepayStep);
            Log(paid > 0 ? "빚을 " + JsMath.Won(paid) + " 갚았다" : "갚을 돈이 없다");
            AfterTime();
        }

        private void NextSlot()
        {
            _game.UseSlot();
            AfterTime();
        }

        private void Sleep()
        {
            _game.EndDay();
            AfterTime();
        }

        /// <summary>시간이 지난 뒤: 어제 요약과 1장 결말 확인 (웹 TownScene)</summary>
        private void AfterTime()
        {
            var s = _game.Save;
            if (s.PendingSummary != null)
            {
                var sum = s.PendingSummary;
                s.PendingSummary = null;
                string line = s.Day + "일차가 밝았다. 어제 번 돈 " + JsMath.Won(sum.Earned);
                if (sum.Spoiled > 0) line += ", " + sum.Spoiled + "마리 상함";
                if (sum.ExpiredRequests > 0) line += ", 의뢰 " + sum.ExpiredRequests + "건 놓침";
                Log(line);
                _game.Persist();
            }
            var outcome = _game.CheckChapter();
            if (outcome == ChapterOutcome.Clear)
            {
                _game.Persist();
                Log("1차 상환 완료! 조황일지 1장을 얻었다");
            }
            else if (outcome == ChapterOutcome.Fail)
            {
                Log("30일이 지났다. 여울식당에 빨간 딱지가 붙었다");
                SaveStore.Delete();
                StartNewGame();
            }
        }

        private void Log(string line)
        {
            _log.Add(line);
            while (_log.Count > 40) _log.RemoveAt(0);
        }

        // ───────── 웹 일치 점검 ─────────

        /// <summary>웹 버전 코드로 뽑은 값(unity/golden)이 폰에서도 같은지</summary>
        private void RunChecks()
        {
            _checks = new List<string>();
            var d = _data;
            var fresh = new Freshness(d);
            Check("데이터: 어종 5 · 요리 5 · 의뢰 8", d.Species.Count == 5 && d.Recipes.Recipes.Count == 5 && d.RequestTemplates.Count == 8);
            Check("난수 mulberry32(12345) 첫 값", new Mulberry32(12345).Next() == 0.9797282677609473);

            var m = new Market(d, fresh, 12345);
            int[] prices = d.Species.Select(sp => m.TodayBasePrice(sp.Id, 4)).ToArray();
            Check("시세 시드 12345 · 4일차 " + string.Join(", ", prices), prices.SequenceEqual(new[] { 7900, 3400, 3800, 4900, 46400 }));
            Check("소식 \"" + m.Headline(4) + "\"", m.Headline(4) == "볼락 값이 38% 뛰었다");
            int v1 = m.FishValue(new FishItem(0, "noraemi", 23.6, 1, 66), 28);
            int v2 = new Market(d, fresh, 987654321).FishValue(new FishItem(0, "bollak", 16.3, 1, 68), 37);
            Check("물고기 값 " + JsMath.Won(v1) + ", " + JsMath.Won(v2), v1 == 2200 && v2 == 6600);

            var g = CoreGame.NewGame(d, 12345);
            var req = g.Save.Requests;
            Check("새 게임 첫 의뢰 보상 " + string.Join(", ", req.Select(r => r.Reward)),
                req.Count == 2 && req[0].Template == 3 && req[0].Reward == 19400 && req[0].Deadline == 5 && req[1].Template == 7 && req[1].Reward == 19200 && req[1].Deadline == 6);

            var rules = new Rules(d);
            Check("16일차는 " + rules.DateLabel(16), rules.DateLabel(16) == "5월 1일");
            Check("감성돔 20cm: " + rules.ReleaseReason("gamseongdom", 20, 16), rules.ReleaseReason("gamseongdom", 20, 16) == "금지체장 25cm 이하라 놓아줘야 한다");
            Check("감성돔 30cm 5월: " + rules.ReleaseReason("gamseongdom", 30, 16), rules.ReleaseReason("gamseongdom", 30, 16) == "금어기(5/1~5/31)라 놓아줘야 한다");

            string json = SaveJson.ToJson(_game.Save);
            var back = SaveJson.FromJson(json, d);
            Check("세이브 JSON 왕복 (" + json.Length + "자)", back != null && SaveJson.ToJson(back) == json);
            Check("세이브 파일 " + (SaveStore.Has() ? "있음" : "없음"), SaveStore.Has());
            int ok = _checks.Count(c => c.StartsWith("○"));
            _checks.Insert(0, ok + " / " + _checks.Count + " 통과");
        }

        private void Check(string label, bool ok)
        {
            _checks.Add((ok ? "○ " : "× ") + label);
        }

        // ───────── 그리기 ─────────

        private Texture2D Tex(string path)
        {
            Texture2D t;
            if (!_tex.TryGetValue(path, out t))
            {
                t = Resources.Load<Texture2D>(path);
                _tex[path] = t;
            }
            return t;
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void MakeStyles()
        {
            if (_text != null) return;
            _dark = Solid(new Color(0.08f, 0.12f, 0.19f, 0.82f));
            _panel = Solid(new Color(0.12f, 0.17f, 0.24f, 0.94f));
            _buttonBg = Solid(new Color(0.85f, 0.33f, 0.31f, 1f));
            _buttonDown = Solid(new Color(0.62f, 0.2f, 0.19f, 1f));
            _text = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 40, wordWrap = true, richText = true };
            _text.normal.textColor = new Color(0.96f, 0.93f, 0.85f);
            _small = new GUIStyle(_text) { fontSize = 32 };
            _title = new GUIStyle(_text) { fontSize = 48 };
            _check = new GUIStyle(_text) { fontSize = 34 };
            _button = new GUIStyle(GUI.skin.button) { font = _font, fontSize = 38, wordWrap = true, richText = true };
            _button.normal.background = _buttonBg;
            _button.hover.background = _buttonBg;
            _button.active.background = _buttonDown;
            _button.normal.textColor = Color.white;
            _button.hover.textColor = Color.white;
            _button.active.textColor = Color.white;
            _clear = new GUIStyle();
        }

        private void OnGUI()
        {
            MakeStyles();
            float s = Screen.width / RefWidth;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            float w = RefWidth;
            float h = Screen.height / s;
            Rect safe = Screen.safeArea;
            float top = (Screen.height - safe.yMax) / s + 10;
            float bottom = safe.yMin / s + 20;

            if (_loadError != null)
            {
                GUI.DrawTexture(new Rect(0, 0, w, h), _panel);
                GUI.Label(new Rect(40, top + 40, w - 80, h - 80), _loadError, _text);
                return;
            }
            var save = _game.Save;

            var bg = Tex("Art/Backgrounds/village-" + Mathf.Clamp(save.Slot, 0, 3));
            if (bg != null) GUI.DrawTexture(new Rect(0, 0, w, h), bg, ScaleMode.ScaleAndCrop);

            // 상태 막대
            GUI.DrawTexture(new Rect(0, 0, w, top + 140), _dark);
            GUI.Label(new Rect(30, top, w - 260, 60), _game.DateLabel() + " · " + save.Day + "일차 · " + _game.SlotName(), _title);
            GUI.Label(new Rect(w - 230, top, 200, 60), "<color=#e0605b>D-" + Mathf.Max(0, _game.DaysLeft()) + "</color>", _title);
            GUI.Label(new Rect(30, top + 64, w - 60, 60),
                "<color=#f2c14e>" + JsMath.Won(save.Money) + "</color>  빚 " + JsMath.Won(save.Debt) + "  미끼 " + save.Bait + "  미각 " + _game.Taste.Stage(_game.TasteLevel()).Name, _small);

            if (GUI.Button(new Rect(w - 450, top + 150, 200, 76), "새 게임", _button)) StartNewGame();
            if (GUI.Button(new Rect(w - 240, top + 150, 210, 76), "웹 일치 점검", _button))
            {
                RunChecks();
                _showChecks = true;
            }

            // 여울과 고등어. 누르면 포즈가 바뀐다
            float baseLine = h * 0.45f;
            DrawYeoul(new Vector2(w * 0.36f, baseLine), 400);
            DrawCat(new Vector2(w * 0.7f, baseLine), 210);

            // 아래 패널
            float panelTop = baseLine + 20;
            GUI.DrawTexture(new Rect(0, panelTop, w, h - panelTop), _panel);
            float y = panelTop + 16;
            GUI.Label(new Rect(30, y, w - 60, 50), "오늘 시세 · " + _game.Market.Headline(save.Day), _small);
            y += 50;
            for (int i = 0; i < _data.Species.Count; i++)
            {
                var sp = _data.Species[i];
                int c = _game.Market.ChangePercent(sp.Id, save.Day);
                string arrow = c > 0 ? "<color=#e0605b>▲" + c + "%</color>" : c < 0 ? "<color=#8cc8ff>▼" + (-c) + "%</color>" : "-";
                float cx = 30 + (i % 2) * (w / 2);
                GUI.Label(new Rect(cx, y + (i / 2) * 44, w / 2 - 40, 44), sp.Name + " " + JsMath.Won(_game.Market.TodayBasePrice(sp.Id, save.Day)) + " " + arrow, _small);
            }
            y += 44 * 3 + 6;

            var stages = save.Cooler.GroupBy(f => _game.Fresh.StageOf(f.Fresh)).OrderBy(gr => gr.Key)
                .Select(gr => _game.Fresh.StageName(gr.Key) + " " + gr.Count());
            GUI.Label(new Rect(30, y, w - 60, 44), "쿨러 " + save.Cooler.Count + "/" + _game.CoolerCapacity() + "  " + string.Join(" · ", stages) + "  고등어 호감 " + save.CatAffinity, _small);
            y += 46;
            foreach (var r in save.Requests)
            {
                var t = _data.RequestTemplates[r.Template];
                GUI.Label(new Rect(30, y, w - 60, 44), "의뢰 " + t.Client + ": " + _data.SpeciesById(t.Species).Name + " " + t.Count + "마리 · D-" + (r.Deadline - save.Day) + " · " + JsMath.Won(r.Reward), _small);
                y += 44;
            }

            // 버튼 3 × 3
            float bw = (w - 30 * 4) / 3f;
            float bh = 112;
            float gridTop = h - bottom - (bh + 18) * 3;
            float logTop = y + 6;
            GUI.Label(new Rect(30, logTop, w - 60, gridTop - logTop - 10), string.Join("\n", _log.Skip(Mathf.Max(0, _log.Count - LogLines))), _small);

            string[] labels =
            {
                "출조\n<size=28>6번 던지기 · 1칸</size>", "경매 전부 팔기", "트럭 장사\n<size=28>손님 5명 · 1칸</size>",
                "납품", "고등어 밥", "장비 사기",
                "시간 보내기", "잠자기", "빚 10만 원 갚기",
            };
            System.Action[] actions = { GoFishing, SellAll, RunTruck, Deliver, FeedCat, BuyGear, NextSlot, Sleep, Repay };
            for (int i = 0; i < 9; i++)
            {
                var rect = new Rect(30 + (i % 3) * (bw + 30), gridTop + (i / 3) * (bh + 18), bw, bh);
                if (GUI.Button(rect, labels[i], _button) && !_showChecks) actions[i]();
            }

            if (_showChecks) DrawChecks(w, h, top);
        }

        private void DrawYeoul(Vector2 foot, float height)
        {
            string pose = YeoulPoses[_yeoulPose];
            if (pose == "walk") pose = Mathf.FloorToInt(_time / 0.35f) % 2 == 0 ? "walk-1" : "walk-2";
            var t = Tex("Art/Yeoul/" + pose);
            if (t == null) return;
            float bob = pose.StartsWith("walk") ? 0 : Mathf.Sin(_time * 2.2f) * 4;
            var rect = SpriteRect(t, foot, height, bob);
            GUI.DrawTexture(rect, t);
            if (GUI.Button(rect, GUIContent.none, _clear)) _yeoulPose = (_yeoulPose + 1) % YeoulPoses.Length;
        }

        private void DrawCat(Vector2 foot, float height)
        {
            string pose = CatPoses[_catPose];
            if (pose == "walk") pose = Mathf.FloorToInt(_time / 0.3f) % 2 == 0 ? "walk-1" : "walk-2";
            var t = Tex("Art/Cat/" + pose);
            if (t == null) return;
            var rect = SpriteRect(t, foot, height, 0);
            GUI.DrawTexture(rect, t);
            if (GUI.Button(rect, GUIContent.none, _clear))
            {
                _catPose = (_catPose + 1) % CatPoses.Length;
                Log(_catPose % 2 == 0 ? "냐옹" : "(꼬리를 흔든다)");
            }
        }

        /// <summary>발이 foot 에 닿게. 시트 원래 비율을 지키려고 기준 높이(여울 490px, 고양이 310px)로 맞춘다</summary>
        private static Rect SpriteRect(Texture2D t, Vector2 foot, float height, float bob)
        {
            float refH = t.height > 400 ? 490f : 310f;
            float k = height / refH;
            float tw = t.width * k, th = t.height * k;
            return new Rect(foot.x - tw / 2, foot.y - th + bob, tw, th);
        }

        private void DrawChecks(float w, float h, float top)
        {
            GUI.DrawTexture(new Rect(0, 0, w, h), _dark);
            var box = new Rect(40, top + 120, w - 80, h - top - 300);
            GUI.DrawTexture(box, _panel);
            GUI.Label(new Rect(box.x + 30, box.y + 24, box.width - 60, 60), "웹 일치 점검", _title);
            GUI.Label(new Rect(box.x + 30, box.y + 100, box.width - 60, box.height - 240), string.Join("\n", _checks), _check);
            if (GUI.Button(new Rect(w / 2 - 160, box.yMax - 120, 320, 96), "닫기", _button)) _showChecks = false;
        }
    }
}

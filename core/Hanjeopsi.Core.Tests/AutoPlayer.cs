using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>자동 플레이어의 손 실력과 판단</summary>
    public sealed class PlayerSkill
    {
        public string Name;
        /// <summary>찌가 들어가고 누르기까지 (초) 평균과 흔들림</summary>
        public double ReactMean;
        public double ReactSd;
        /// <summary>속임 입질에 속아 일찍 누를 확률 (한 번 톡 할 때마다)</summary>
        public double FooledByNibble;
        /// <summary>파워 게이지 멈추는 오차</summary>
        public double PowerSd;
        /// <summary>이 텐션 아래면 누르고 위면 뗀다</summary>
        public double HoldBelow;
        /// <summary>텐션을 보고 손을 바꾸기까지 걸리는 시간 (초)</summary>
        public double HandLag;
        /// <summary>간 맞추기 바늘을 멈추는 오차 (아는 맛). 모르는 맛은 두 배</summary>
        public double SeasonSd;
        /// <summary>고등어가 가리키는 쪽으로 던진다</summary>
        public bool FollowHint;
        /// <summary>물고기가 질주할 때 반대쪽을 고를 확률</summary>
        public double BraceSkill;
        /// <summary>쿨러를 노리는 고등어를 제때 누를 확률</summary>
        public double CatchThief;
        /// <summary>장사 판단(남길 고기, 장비, 상환)을 따진다. 아니면 아무렇게나</summary>
        public bool Thoughtful;

        /// <summary>적당히 잘하는 사람: 입질에 0.3초 안팎으로 반응하고 텐션을 70 근처로 유지한다</summary>
        public static PlayerSkill Decent()
        {
            return new PlayerSkill
            {
                Name = "적당히 잘함",
                ReactMean = 0.33,
                ReactSd = 0.1,
                FooledByNibble = 0.03,
                PowerSd = 0.08,
                HoldBelow = 68,
                HandLag = 0.12,
                SeasonSd = 0.06,
                FollowHint = true,
                CatchThief = 0.8,
                BraceSkill = 0.85,
                Thoughtful = true,
            };
        }

        /// <summary>처음 해 보는 사람: 반응이 조금 느리고 텐션을 높게 끌다 가끔 터뜨린다. 힌트와 장사 판단은 신경 쓰지 않는다</summary>
        public static PlayerSkill Beginner()
        {
            return new PlayerSkill
            {
                Name = "초보",
                ReactMean = 0.42,
                ReactSd = 0.12,
                FooledByNibble = 0.05,
                PowerSd = 0.2,
                HoldBelow = 80,
                HandLag = 0.22,
                SeasonSd = 0.12,
                FollowHint = false,
                CatchThief = 0.4,
                BraceSkill = 0.55,
                Thoughtful = false,
            };
        }
    }

    /// <summary>한 판 결과</summary>
    public sealed class PlayResult
    {
        public int Seed;
        public ChapterOutcome Outcome;
        /// <summary>클리어한 날 (못 했으면 0)</summary>
        public int ClearDay;
        public int Debt;
        public int TotalEarned;
        public int Casts;
        public int Landed;
        public int Snapped;
        public int Slipped;
        public int MissedBites;
        /// <summary>고닷 낚시 기록: 지점에 가까운 정도 합, 큰 놈, 고등어 도둑, 기다린 시간과 릴링 시간 (초)</summary>
        public double Closeness;
        public int BigBites;
        public int Thieves;
        public int Stolen;
        public double WaitTime;
        public double FightTime;
        public int Fights;
        public int Runs;
        public int RunsHeld;
        public int Dishes;
        public int AuctionIncome;
        public int TruckIncome;
        public int RequestIncome;
        public int GearSpent;
        public string Gear;
        public List<string> DayLog = new List<string>();
    }

    /// <summary>
    /// 새 게임부터 화면 없이 규칙대로 30일을 돈다 (U4).
    /// 낚시는 ReelRules 로 입질·챔질·릴링을 프레임 단위로 실제로 돌리고, 손은 PlayerSkill 로 흉내 낸다.
    /// 화면이 하는 순서(웹 FishingScene, MarketScene, TruckScene, TownScene)를 그대로 따른다.
    /// </summary>
    public sealed class AutoPlayer
    {
        private const double Frame = 1.0 / 60;

        private readonly Game _g;
        private readonly PlayerSkill _s;
        /// <summary>게임 쪽 난수 (웹 Math.random 자리)</summary>
        private readonly IRandom _rng;
        /// <summary>사람 손 난수</summary>
        private readonly IRandom _hand;
        private readonly PlayResult _r;
        /// <summary>매 행동 뒤 상태 검사</summary>
        private readonly Action<Game, string> _check;

        public AutoPlayer(GameData data, int seed, PlayerSkill skill, Action<Game, string> check)
        {
            _g = Game.NewGame(data, seed);
            _s = skill;
            _rng = new Mulberry32((uint)seed * 2654435761u);
            _hand = new Mulberry32((uint)seed ^ 0xbeefu);
            _r = new PlayResult { Seed = seed };
            _check = check ?? ((g, at) => { });
        }

        public Game Game { get { return _g; } }

        private SaveData S { get { return _g.Save; } }
        private GameData D { get { return _g.Data; } }

        private double Gauss(double mean, double sd)
        {
            double u1 = 1 - _hand.Next();
            double u2 = _hand.Next();
            return mean + sd * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }

        private void Check(string at)
        {
            _check(_g, at);
        }

        public PlayResult Play()
        {
            Prologue();
            int lastDay = D.Balance.Chapter1.DeadlineDays;
            while (true)
            {
                // 마을에 들어올 때마다 결말 확인 (웹 TownScene.create)
                var outcome = _g.CheckChapter();
                if (outcome == ChapterOutcome.Clear)
                {
                    _r.Outcome = outcome;
                    _r.ClearDay = S.Day;
                    break;
                }
                if (outcome == ChapterOutcome.Fail || S.Day > lastDay + 1)
                {
                    _r.Outcome = ChapterOutcome.Fail;
                    break;
                }
                PlayDay();
            }
            _r.Debt = S.Debt;
            _r.TotalEarned = S.TotalEarned;
            _r.Dishes = S.DishesCooked;
            _r.Gear = "낚싯대 " + S.Rod + " · 릴 " + S.Reel + " · 쿨러 " + S.Tackle + " · " + _g.Diner.Rank(S).Name + " 평판 " + S.Diner.Rep + " 단골 " + D.Diner.Guests.Count(gu => _g.Diner.IsRegular(S, gu.Id));
            return _r;
        }

        /// <summary>프롤로그: 튜토리얼 볼락 한 마리, 소금 간 맞추기 (웹 FishingScene tutorial, PrologueScene)</summary>
        private void Prologue()
        {
            _g.RecordCatch("bollak", 19.4, false);
            S.Flags["tutorialFish"] = true;
            _g.Persist();
            _g.CompletePrologue();
            Check("프롤로그");
        }

        private void PlayDay()
        {
            int day0 = S.Day;
            int money0 = S.Money;
            int debt0 = S.Debt;
            // 새벽 낚시 → 아침 위판장과 트럭 → 점심 낚시 → 저녁 트럭 → 밤낚시 → 잠
            FishingTrip();
            if (_g.IsMarketOpen()) Market();
            TruckShift();
            if (S.Slot <= 2) FishingTrip();
            if (S.Slot <= 3 && S.Slot >= D.Balance.Time.TruckFirstSlot) TruckShift();
            if (S.Day == day0 && S.Slot <= 4) FishingTrip();
            // 밤낚시가 끝나면 시간이 다 가서 날이 저절로 바뀐다. 아니면 식당에서 잠자기.
            // 미끼가게가 밤에 닫혀 있었으면 다음 날 새벽에 장을 본다
            Town();
            if (S.Day == day0) _g.EndDay();
            _g.CheckGifts();
            if (_s.Thoughtful) Upgrade();
            BuyBait(3 * D.Balance.Chapter1.CastsPerTrip);
            Check("잠 " + day0);
            if (S.Day != day0 + 1) throw new InvalidOperationException("하루가 " + (S.Day - day0) + "일 지나갔다");
            _r.DayLog.Add(day0 + "일: 돈 " + money0 + "→" + S.Money + ", 빚 " + debt0 + "→" + S.Debt + ", 쿨러 " + S.Cooler.Count + ", 장비 " + S.Rod + S.Reel + S.Tackle);
            if (S.Slot != 0) throw new InvalidOperationException("잠자고 나서 새벽이 아니다");
        }

        // ───────── 낚시 (웹 FishingScene) ─────────

        private void FishingTrip()
        {
            // 태풍 날은 방파제 출입 금지
            if (!_g.CanFishToday()) return;
            if (S.Bait <= 0) BuyBait(12);
            if (S.Bait <= 0) return;
            int casts = 0;
            int castsLeft = D.Balance.Chapter1.CastsPerTrip;
            while (castsLeft > 0 && S.Bait > 0)
            {
                // 쿨러가 가득 차면 일찍 철수한다 (생각 있는 사람만)
                if (_s.Thoughtful && _g.CoolerFull()) break;
                _g.UseBait();
                castsLeft -= 1;
                casts += 1;
                OneCast();
                Check("캐스팅");
            }
            // 철수: 한 번이라도 던졌으면 시간 한 칸
            if (casts > 0) _g.UseSlot();
            Check("철수");
        }

        private void OneCast()
        {
            _r.Casts += 1;
            var reel = _g.Reel;
            var spot = _g.Spot;
            var rod = _g.EffectiveRod();
            var (hot, hint) = spot.RollSpots(S.CatAffinity, rod.Reach, _rng, _g.HintPenalty());

            // 방향 바늘을 멈추고 파워 게이지로 거리를 정한다. 힌트를 따르면 고등어가 가리킨 지점을 노린다
            double angle = _s.FollowHint ? JsMath.Clamp(Gauss(hint.Angle, _s.PowerSd * 1.5), -1, 1) : _hand.Next() * 2 - 1;
            double want = _s.FollowHint ? hint.Dist : _s.Thoughtful ? BestDistance(rod.Reach, false) : _hand.Next();
            double power = JsMath.Clamp(Gauss(want / rod.Reach, _s.PowerSd), 0, 1);
            double dist01 = reel.CastDistance(power, rod.Reach);
            double close = spot.Closeness(new CastSpot(angle, dist01), hot);
            _r.Closeness += close;

            // 고등어 도둑: 제때 누르면 막는다
            if (spot.RollThief(S.Cooler.Count, _rng) >= 0)
            {
                _r.Thieves += 1;
                if (_hand.Next() >= _s.CatchThief && _g.CatSteal() != null) _r.Stolen += 1;
            }

            // 입질 대기: 어디에 던지든 같다. 속임 입질에 속으면 헛챔질
            var wait = reel.StartWait(false, false, _rng);
            wait.Scale(_g.WaitMul());
            for (int i = 0; i < 100000; i++)
            {
                var w = wait.Step(Frame, _rng);
                _r.WaitTime += Frame;
                if (w.Nibble && _hand.Next() < _s.FooledByNibble)
                {
                    _r.MissedBites += 1;
                    return;
                }
                if (w.Bite) break;
            }

            int zone = _g.Fishing.ZoneOf(dist01);
            bool big = spot.IsBigBite(close, _rng, _g.BigAdd());
            if (big) _r.BigBites += 1;
            var sp = _g.Fishing.RollSpeciesNear(zone, S.Slot, _g.MonthNow(), _g.SeaNow(), close, _rng, _g.BaitBias());
            double size = _g.Fishing.RollSizeNear(sp, close, big, _rng);
            double window = reel.BiteWindow(rod, sp, false);
            double react = Math.Max(0.12, Gauss(_s.ReactMean, _s.ReactSd));
            // 프레임 단위로 재서 웹과 같은 방식으로 판정한다
            double elapsed = 0;
            while (elapsed < react)
            {
                elapsed += Frame;
                if (reel.BiteMissed(elapsed, window))
                {
                    _r.MissedBites += 1;
                    return;
                }
            }
            bool perfect = reel.IsPerfectStrike(elapsed);

            // 고닷 힘싸움: 질주가 오면 손이 늦게 알아채고, 실력만큼 반대쪽을 눌러 버틴다
            var fight = _g.Spot.StartTug(_g.Fight(sp, size, dist01, perfect, false), _rng);
            bool holding = true;
            int side = 1;
            double sinceLook = 0;
            TugPhase seen = TugPhase.Calm;
            TugStep res = default;
            for (int i = 0; i < 60 * 300 && res.End == FightEnd.None; i++)
            {
                sinceLook += Frame;
                if (sinceLook >= _s.HandLag)
                {
                    sinceLook = 0;
                    if (fight.Phase != seen)
                    {
                        seen = fight.Phase;
                        if (seen != TugPhase.Calm) side = _hand.Next() < _s.BraceSkill ? -fight.RunDir : fight.RunDir;
                    }
                    if (fight.Phase == TugPhase.Run) holding = fight.Tension < _s.HoldBelow + 12;
                    else holding = fight.Tension < _s.HoldBelow;
                }
                res = fight.Step(holding, side, Frame);
                if (res.RunHeld) _r.RunsHeld += 1;
            }
            _r.Runs += fight.Runs;
            _r.FightTime += fight.FightTime;
            _r.Fights += 1;
            switch (res.End)
            {
                case FightEnd.Snap: _r.Snapped += 1; return;
                case FightEnd.Slack: _r.Slipped += 1; return;
                case FightEnd.None: throw new InvalidOperationException("릴링이 5분 넘게 끝나지 않았다");
            }
            _r.Landed += 1;
            Landed(sp, size);
        }

        /// <summary>웹 FishingScene.showResult 의 finish 순서: 도감 → 쿨러 → 미각</summary>
        private void Landed(Species sp, double size)
        {
            string rule = _g.ReleaseReason(sp.Id, size);
            bool full = _g.CoolerFull();
            bool released = rule != null || full;
            _g.RecordCatch(sp.Id, size, released);
            if (!released && _g.AddToCooler(sp.Id, size) == null) throw new InvalidOperationException("쿨러에 못 넣었다");
            if (released)
            {
                S.Flags["released"] = true;
                _g.Persist();
            }
            _g.CheckTasteUnlock();
            _g.Persist();
        }

        /// <summary>지금 시간대에 기대 값이 가장 큰 거리 구간의 가운데 (낚싯대가 닿는 데까지)</summary>
        private double BestDistance(double reach, bool hot)
        {
            double[] centers = { 0.2, 0.52, 0.85 };
            double best = 0.2;
            double bestValue = -1;
            for (int zone = 0; zone < 3; zone++)
            {
                double d = Math.Min(centers[zone], reach - 0.02);
                if (_g.Fishing.ZoneOf(d) != zone) continue;
                double wsum = 0, vsum = 0;
                foreach (var sp in D.Species)
                {
                    double w = sp.Zone[zone] * sp.Slot[Math.Min(S.Slot, sp.Slot.Length - 1)];
                    if (sp.Rare) w *= hot ? D.Balance.Fishing.RareHotMul : D.Balance.Fishing.RareColdMul;
                    wsum += w;
                    vsum += w * sp.BasePrice;
                }
                double v = wsum > 0 ? vsum / wsum : 0;
                if (v > bestValue)
                {
                    bestValue = v;
                    best = d;
                }
            }
            return best;
        }

        // ───────── 위판장 (웹 MarketScene) ─────────

        private void Market()
        {
            // 의뢰 먼저
            foreach (var req in S.Requests.ToList())
            {
                if (_g.FishForRequest(req.Id) == null) continue;
                int reward = _g.DeliverRequest(req.Id);
                if (reward <= 0) throw new InvalidOperationException("납품 실패");
                _r.RequestIncome += reward;
                Check("납품");
            }
            // 트럭에 쓸 고기만 남기고 판다. 서툰 사람은 반쯤 아무렇게나 판다
            var keep = new HashSet<int>(_s.Thoughtful ? FishForTruck() : S.Cooler.Where(f => _hand.Next() < 0.5).Select(f => f.Uid));
            // 의뢰에 필요한 어종도 남긴다
            if (_s.Thoughtful)
            {
                foreach (var req in S.Requests)
                {
                    var t = D.RequestTemplates[req.Template];
                    foreach (var f in S.Cooler.Where(f => f.Species == t.Species).OrderByDescending(f => f.Fresh).Take(t.Count)) keep.Add(f.Uid);
                }
            }
            var sell = S.Cooler.Where(f => !keep.Contains(f.Uid) && _g.Fresh.StageOf(f.Fresh) != FreshStage.Spoiled).Select(f => f.Uid).ToList();
            if (sell.Count == 0) return;
            // 웹은 어종·신선도 묶음마다 경매한다. 묶음마다 낙찰 배율이 따로 나온다
            var groups = S.Cooler.Where(f => sell.Contains(f.Uid)).GroupBy(f => f.Species + "-" + _g.Fresh.StageOf(f.Fresh)).ToList();
            foreach (var grp in groups)
            {
                int total = _g.SellFish(grp.Select(f => f.Uid).ToList(), _rng.Range(0.92, 1.15));
                _r.AuctionIncome += total;
                Check("경매");
            }
        }

        /// <summary>트럭에서 요리로 내는 게 경매보다 나은 고기 (한 번 장사 손님 수까지)</summary>
        private List<int> FishForTruck()
        {
            var keep = new List<int>();
            int seats = D.Balance.Chapter1.CustomersPerShift * 2;
            var candidates = new List<(int Uid, int Gain)>();
            foreach (var f in S.Cooler)
            {
                var stage = _g.Fresh.StageOf(f.Fresh);
                if (stage == FreshStage.Spoiled) continue;
                var recipes = D.Recipes.Recipes.Where(r => r.Species == f.Species).ToList();
                if (recipes.Count == 0) continue;
                int dish = recipes.Max(r => _g.Truck.DishPay(r, stage, 0.8));
                int auction = _g.FishValue(f);
                if (dish > auction) candidates.Add((f.Uid, dish - auction));
            }
            foreach (var c in candidates.OrderByDescending(c => c.Gain).Take(seats)) keep.Add(c.Uid);
            return keep;
        }

        // ───────── 트럭 (웹 TruckScene) ─────────

        private void TruckShift()
        {
            if (!_g.IsTruckOpen()) return;
            if (_g.Truck.Cookable(S).Count == 0) return;
            // 오늘의 메뉴: 생각 있는 사람은 값이 큰 요리를, 초보는 아무거나 메뉴 칸만큼
            var menu = _s.Thoughtful
                ? _g.SuggestMenu()
                : _g.Truck.Cookable(S).OrderBy(_ => _rng.Next()).Take(_g.Diner.MenuSlots(S)).ToList();
            foreach (var guest in _g.PlanGuests(_rng))
            {
                var left = menu.Where(r => _g.Truck.BestFishFor(S, r) != null).ToList();
                if (left.Count == 0) break;
                var recipe = _g.Diner.Order(guest, left, _rng);
                var results = new List<SeasonResult>();
                foreach (var step in recipe.Steps)
                {
                    double target = _g.Truck.RollTarget(step, _rng);
                    _g.Truck.RollGaugeSpeed(_rng);
                    bool visible = _g.Taste.CanTaste(S, step.Flavor);
                    // 모르는 맛은 요리마다 거의 같은 자리라는 감으로 맞춘다
                    double aim = visible ? target : step.Target;
                    double pos = JsMath.Clamp(Gauss(aim, _s.SeasonSd * (visible ? 1 : 2)), 0, 1);
                    results.Add(_g.Truck.Judge(pos, target));
                }
                var dish = _g.ServeGuest(recipe, results, guest);
                if (dish == null) throw new InvalidOperationException("만들 수 있다던 요리를 못 만들었다");
                _r.TruckIncome += dish.Pay;
                Check("요리");
            }
            _g.CheckDinerRankUp();
            _g.UseSlot();
            Check("장사 끝");
        }

        // ───────── 마을: 고양이, 미끼가게, 식당 (웹 TownScene, ShopScene) ─────────

        private void Town()
        {
            if (_g.SpoiledFish().Count > 0)
            {
                _g.FeedCat();
                Check("고양이");
            }
            if (_s.Thoughtful) Upgrade();
            BuyBait(3 * D.Balance.Chapter1.CastsPerTrip);
            Repay();
        }

        private void BuyBait(int target)
        {
            if (!_g.IsShopOpen()) return;
            while (S.Bait < target && S.Money >= D.Gear.Bait.Price)
            {
                if (!_g.BuyBait()) throw new InvalidOperationException("미끼를 못 샀다");
                Check("미끼");
            }
        }

        /// <summary>남은 날이 넉넉할 때 값이 싼 장비부터 산다</summary>
        private void Upgrade()
        {
            if (!_g.IsShopOpen()) return;
            int reserve = 2 * D.Gear.Bait.Price;
            if (_g.DaysLeft() < 8) return;
            while (true)
            {
                GearSlot? next = null;
                string kit = null;
                int price = int.MaxValue;
                foreach (GearSlot slot in new[] { GearSlot.Tackle, GearSlot.Reel, GearSlot.Rod })
                {
                    int lv = _g.GearLevel(slot) + 1;
                    // 적당히 잘하는 사람은 3단계(웹과 같은 마지막 단계)까지만 산다
                    if (lv >= Math.Min(3, _g.GearCount(slot)) || !Data.Unlock.IsShop(_g.GearUnlock(slot, lv))) continue;
                    int p = _g.GearPrice(slot, lv);
                    if (p < price)
                    {
                        price = p;
                        next = slot;
                    }
                }
                // 고닷 장비: 상점에서 파는 다음 단계 (통발 빼고. 통발은 크릴을 쓴다)
                foreach (var k in D.Gear.Kits)
                {
                    int lv = _g.KitLevel(k.Id);
                    if (k.Id == "trap" || lv >= Math.Min(2, k.Levels.Count) || !Data.Unlock.IsShop(k.Levels[lv].Unlock)) continue;
                    if (k.Levels[lv].Price < price)
                    {
                        price = k.Levels[lv].Price;
                        next = null;
                        kit = k.Id;
                    }
                }
                if ((next == null && kit == null) || S.Money - price < reserve) return;
                if (kit != null ? !_g.BuyKit(kit) : !_g.UpgradeGear(next.Value)) throw new InvalidOperationException("장비를 못 샀다");
                _r.GearSpent += price;
                Check("장비");
            }
        }

        /// <summary>미끼값만 남기고 갚는다. 서툰 사람은 10만 원 단위로만 (식당 "10만 원 갚기" 버튼)</summary>
        private void Repay()
        {
            if (!_g.IsShopOpen()) return;
            int reserve = 2 * D.Gear.Bait.Price;
            if (_s.Thoughtful)
            {
                // 장비를 다 사기 전 초반에는 장비값을 모은다
                bool saving = _g.DaysLeft() >= 8 && (S.Rod < 2 || S.Reel < 2 || S.Tackle < 2);
                if (saving && S.Debt > S.Money - reserve) return;
                if (S.Money > reserve) _g.Repay(S.Money - reserve);
            }
            else
            {
                int step = D.Balance.Chapter1.RepayStep;
                while (S.Money >= step && S.Debt > 0) _g.Repay(step);
            }
            Check("상환");
        }
    }
}

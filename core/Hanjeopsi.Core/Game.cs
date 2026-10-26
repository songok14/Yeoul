using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core
{
    /// <summary>1장 결말 판정 결과</summary>
    public enum ChapterOutcome
    {
        None,
        Clear,
        Fail,
    }

    /// <summary>요리 한 접시를 낸 결과</summary>
    public sealed class ServedDish
    {
        public FreshStage Stage;
        public double Avg;
        public int Pay;
        /// <summary>처음 만든 요리라 레시피에 기록됐다</summary>
        public bool FirstTime;
        /// <summary>손님 평판·친밀도 (여울호 손님 규칙이 있을 때만)</summary>
        public GuestResult Guest;
    }

    /// <summary>
    /// 게임 한 판. 세이브 하나와 모든 규칙을 묶고, 상태를 바꾸는 행동을 모은다
    /// (웹 state/actions.ts, systems/time.ts, systems/chapter.ts).
    /// 행동이 끝나면 Saved 를 부른다. 고닷 쪽은 여기에 파일 저장을 건다.
    /// </summary>
    /// <summary>위판장 쿨러 한 줄: 같은 어종·같은 신선도 단계 묶음</summary>
    public sealed class CoolerGroup
    {
        public string Species;
        public FreshStage Stage;
        public List<FishItem> Items = new List<FishItem>();
        /// <summary>오늘 시세로 묶음 전체 값</summary>
        public int Value;
    }

    public sealed partial class Game
    {
        public GameData Data { get; }
        public SaveData Save { get; private set; }
        public Freshness Fresh { get; }
        public Market Market { get; private set; }
        public Rules Rules { get; }
        public Taste Taste { get; }
        public FishingRoll Fishing { get; }
        public ReelRules Reel { get; }
        public SpotRules Spot { get; }
        public Truck Truck { get; }
        /// <summary>여울호 등급과 손님. 웹 모양 데이터에서는 null</summary>
        public Diner Diner { get; }

        /// <summary>상태가 바뀌어 저장할 때 (웹 persist)</summary>
        public event Action<SaveData> Saved;

        public Game(GameData data, SaveData save)
        {
            Data = data;
            Fresh = new Freshness(data);
            Rules = new Rules(data);
            Taste = new Taste(data);
            Fishing = new FishingRoll(data);
            Reel = new ReelRules(data);
            Spot = new SpotRules(data);
            Truck = new Truck(data, Fresh, Taste);
            if (data.Diner != null) Diner = new Diner(data.Diner);
            Load(save);
            if (save.Day > 1 || save.Flag("prologueDone")) BeginWeeks();
        }

        /// <summary>새 세이브 기본값 (웹 createSave)</summary>
        public static SaveData CreateSave(GameData data, int seed)
        {
            var c = data.Balance.Chapter1;
            return new SaveData
            {
                Seed = seed,
                Day = 1,
                Slot = 0,
                Money = c.StartMoney,
                Debt = c.Debt,
                Bait = c.StartBait,
                NextUid = 1,
                NextRequestId = 1,
                Diner = data.Diner != null ? new DinerState() : null,
                Kit = data.Gear.Kits.Count > 0 ? new KitState() : null,
            };
        }

        /// <summary>새 게임. 첫 의뢰를 받고 저장한다 (웹 newGame)</summary>
        public static Game NewGame(GameData data, int seed)
        {
            var g = new Game(data, CreateSave(data, seed));
            g.BeginWeeks();
            g.RefreshRequests();
            g.Persist();
            return g;
        }

        /// <summary>세이브를 바꿔 끼운다. 시세는 세이브 시드로 다시 만든다</summary>
        public void Load(SaveData save)
        {
            Save = save;
            Market = new Market(Data, Fresh, save.Seed);
        }

        public void Persist()
        {
            var h = Saved;
            if (h != null) h(Save);
        }

        // ───────── 시간 (웹 systems/time.ts) ─────────

        public string SlotName()
        {
            return Save.Slot < Data.Texts.Slots.Count ? Data.Texts.Slots[Save.Slot] : Data.Texts.SlotAfter;
        }

        public int DaysLeft()
        {
            if (IsWeekly) return W.Weeks.Count * W.DaysPerWeek - Save.Day;
            return Data.Balance.Chapter1.DeadlineDays - Save.Day;
        }

        /// <summary>경매는 새벽과 아침에만</summary>
        public bool IsMarketOpen()
        {
            return Save.Slot <= Data.Balance.Time.MarketLastSlot;
        }

        /// <summary>트럭은 새벽에 쉰다</summary>
        public bool IsTruckOpen()
        {
            return Save.Slot >= Data.Balance.Time.TruckFirstSlot;
        }

        /// <summary>미끼가게는 밤에 닫는다</summary>
        public bool IsShopOpen()
        {
            return Save.Slot <= Data.Balance.Time.ShopLastSlot;
        }

        /// <summary>이번 장사에 올 손님 수. 여울호 등급과 시간대, 그 시간대 단골로 정한다 (웹은 늘 같은 수)</summary>
        public int CustomersNow()
        {
            if (Diner == null) return Data.Balance.Truck.CustomersPerShift;
            return Diner.GuestCount(Save, Save.Slot, WeatherToday().GuestAdd + EventGuests());
        }

        // ───────── 날씨 (고닷만) ─────────

        /// <summary>화면 확인용: 이 날씨로 고정한다 (세이브에 남지 않는다)</summary>
        public string ForcedWeather;

        private static readonly WeatherKind Clear = new WeatherKind { Id = "clear", Name = "맑음", Note = "" };

        /// <summary>오늘 날씨. 시드와 날짜로 정해서 같은 날은 늘 같다. 날씨 데이터가 없으면 늘 맑음</summary>
        public WeatherKind WeatherToday()
        {
            var w = Data.Weather;
            if (w == null || w.Kinds.Count == 0) return Clear;
            if (!string.IsNullOrEmpty(ForcedWeather)) return w.ById(ForcedWeather);
            var weights = w.Months[Math.Max(0, Math.Min(11, MonthNow() - 1))];
            uint seed = unchecked((uint)(Save.Seed * 2654435761u + (uint)Save.Day * 40503u + (uint)w.SeedSalt));
            double r = new Mulberry32(seed).Next() * weights.Sum();
            for (int i = 0; i < weights.Count; i++)
            {
                r -= weights[i];
                if (r < 0) return w.Kinds[i];
            }
            return w.Kinds[0];
        }

        /// <summary>이번 장사 손님 차례 (날씨 반영)</summary>
        public List<string> PlanGuests(IRandom rng)
        {
            return Diner.PlanGuests(Save, Save.Slot, rng, WeatherToday().GuestAdd + EventGuests());
        }

        /// <summary>고등어 힌트 정확도 빼기 (안개)</summary>
        public double HintPenalty()
        {
            return WeatherToday().HintPenalty;
        }

        /// <summary>큰 놈 입질 확률 더하기 (안개)</summary>
        public double BigAdd()
        {
            return WeatherToday().BigAdd + EventBig();
        }

        /// <summary>오늘 방파제에 나갈 수 있나 (태풍이면 출입 금지)</summary>
        public bool CanFishToday()
        {
            return !WeatherToday().NoFishing;
        }

        /// <summary>메뉴판 기본 선택: 지금 만들 수 있는 요리 중 한 접시 값이 큰 순서로 메뉴 칸만큼</summary>
        public List<Recipe> SuggestMenu()
        {
            int slots = Diner != null ? Diner.MenuSlots(Save) : int.MaxValue;
            return Truck.Cookable(Save)
                .OrderByDescending(r => Truck.DishPay(r, Fresh.StageOf(Truck.BestFishFor(Save, r).Fresh), 0.8))
                .Take(slots)
                .ToList();
        }

        /// <summary>
        /// 손님에게 한 접시를 낸다. 값은 ServeDish 와 같고, 손님 반응만큼 평판과 친밀도가 오른다.
        /// 재료가 없으면 null
        /// </summary>
        public ServedDish ServeGuest(Recipe recipe, IList<SeasonResult> results, string guest)
        {
            var dish = ServeDish(recipe, results);
            if (dish == null || Diner == null) return dish;
            dish.Guest = Diner.AfterServe(Save, guest, recipe, Truck.MoodOf(dish.Avg));
            if (Truck.MoodOf(dish.Avg) == 2) Tally("happy");
            Persist();
            return dish;
        }

        /// <summary>새로 오른 여울호 등급 (없으면 null)</summary>
        public DinerRank CheckDinerRankUp()
        {
            if (Diner == null) return null;
            var up = Diner.CheckRankUp(Save);
            if (up != null) Persist();
            return up;
        }

        /// <summary>지금 시간대 마을 배경 그림 이름</summary>
        public string TownArt()
        {
            var art = Data.Balance.Time.TownArt;
            return art.Count > 0 ? art[Math.Min(Save.Slot, art.Count - 1)] : "village-" + Math.Min(Save.Slot, 3);
        }

        /// <summary>행동 하나로 시간 한 칸을 쓴다. 저녁이 지나면 날이 바뀐다</summary>
        public void UseSlot()
        {
            Save.Slot += 1;
            if (Save.Slot >= Data.Balance.Time.SlotsPerDay) EndDay();
            else Persist();
        }

        /// <summary>날 넘김: 신선도 감소, 지난 의뢰 정리, 새 의뢰. 요약은 PendingSummary 에 남긴다</summary>
        public void EndDay()
        {
            var summary = new DaySummary { Day = Save.Day, Earned = Save.EarnedToday };
            WeekEndOfDay(summary, Save.Day);
            Save.Day += 1;
            Save.Slot = 0;
            Save.EarnedToday = 0;

            summary.Spoiled = Fresh.ApplyDailyDecay(Save.Cooler, Save.Tackle);

            int before = Save.Requests.Count;
            Save.Requests = Save.Requests.Where(r => r.Deadline >= Save.Day).ToList();
            summary.ExpiredRequests = before - Save.Requests.Count;
            RefreshRequests();
            summary.Trap = CheckTrap();

            Save.PendingSummary = summary;
            Persist();
        }

        /// <summary>의뢰는 최대 2개. 빈자리가 있으면 그날 시드로 새 의뢰가 들어온다</summary>
        public void RefreshRequests()
        {
            var b = Data.Balance.Requests;
            var templates = Data.RequestTemplates;
            var rng = new Mulberry32(Mulberry32.Hash(Save.Seed, Save.Day, b.SeedSalt));
            int guard = 0;
            while (Save.Requests.Count < b.Max && guard++ < b.Guard)
            {
                int template = (int)Math.Floor(rng.Next() * templates.Count);
                if (Save.Requests.Any(r => r.Template == template)) continue;
                var t = templates[template];
                // 귀한 고기 의뢰는 첫 주에는 나오지 않는다
                if (Data.SpeciesById(t.Species).Rare && Save.Day < b.RareFromDay) continue;
                Save.Requests.Add(new DeliveryRequest
                {
                    Id = Save.NextRequestId++,
                    Template = template,
                    Reward = JsMath.Round100((double)Market.TodayBasePrice(t.Species, Save.Day) * t.Count * t.Mul * b.RewardMul),
                    Deadline = Save.Day + b.DeadlineMin + (int)Math.Floor(rng.Next() * b.DeadlineRange),
                });
            }
        }

        // ───────── 행동 (웹 state/actions.ts) ─────────

        public void Earn(int amount)
        {
            Save.Money += amount;
            Save.EarnedToday += amount;
            Save.TotalEarned += amount;
            Tally("earn", amount);
        }

        /// <summary>도감 기록. 처음 낚은 어종이면 true</summary>
        public bool RecordCatch(string species, double size, bool released)
        {
            DexEntry e;
            Save.Dex.TryGetValue(species, out e);
            bool isNew = e == null || e.Count == 0;
            if (e == null) e = new DexEntry();
            e.Count += 1;
            e.Best = Math.Max(e.Best, size);
            if (released) e.Released += 1;
            Save.Dex[species] = e;
            TallyCatch(species, size);
            Persist();
            return isNew;
        }

        public int CoolerCapacity()
        {
            return Data.Gear.Tackles[Save.Tackle].Capacity;
        }

        public bool CoolerFull()
        {
            return Save.Cooler.Count >= CoolerCapacity();
        }

        /// <summary>쿨러에 넣는다. 가득 찼으면 null</summary>
        public FishItem AddToCooler(string species, double size)
        {
            if (CoolerFull()) return null;
            var item = new FishItem(Save.NextUid++, species, size, Save.Day, Data.Balance.Fresh.Start);
            Save.Cooler.Add(item);
            Persist();
            return item;
        }

        /// <summary>고등어가 쿨러에서 제일 값싼 고기 한 마리를 물고 간다. 없으면 null</summary>
        public FishItem CatSteal()
        {
            if (Save.Cooler.Count == 0) return null;
            var fish = Save.Cooler.OrderBy(f => FishValue(f)).ThenBy(f => f.Uid).First();
            Save.Cooler.Remove(fish);
            Persist();
            return fish;
        }

        public void RemoveFish(IEnumerable<int> uids)
        {
            var set = new HashSet<int>(uids);
            Save.Cooler = Save.Cooler.Where(f => !set.Contains(f.Uid)).ToList();
        }

        /// <summary>위판장 경매. 낙찰 배율(0.92~1.15)을 곱해 100원 단위로 받는다</summary>
        public int SellFish(IList<int> uids, double mul)
        {
            var set = new HashSet<int>(uids);
            int total = Market.AuctionTotal(Save.Cooler.Where(f => set.Contains(f.Uid)), Save.Day, mul);
            RemoveFish(uids);
            Earn(total);
            Persist();
            return total;
        }

        /// <summary>캐스팅 한 번에 미끼 하나</summary>
        public void UseBait()
        {
            Save.Bait -= 1;
            Persist();
        }

        public bool BuyBait()
        {
            var bait = Data.Gear.Bait;
            if (!IsShopOpen() || Save.Money < bait.Price) return false;
            Save.Money -= bait.Price;
            Save.Bait += bait.Amount + (int)KitValue("baitbox");
            Persist();
            return true;
        }

        public int GearLevel(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Rod: return Save.Rod;
                case GearSlot.Reel: return Save.Reel;
                default: return Save.Tackle;
            }
        }

        public int GearCount(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Rod: return Data.Gear.Rods.Count;
                case GearSlot.Reel: return Data.Gear.Reels.Count;
                default: return Data.Gear.Tackles.Count;
            }
        }

        public int GearPrice(GearSlot slot, int level)
        {
            switch (slot)
            {
                case GearSlot.Rod: return Data.Gear.Rods[level].Price;
                case GearSlot.Reel: return Data.Gear.Reels[level].Price;
                default: return Data.Gear.Tackles[level].Price;
            }
        }

        /// <summary>다음 단계 장비를 산다. 가게가 닫혔거나 마지막 단계거나 돈이 모자라면 false</summary>
        public bool UpgradeGear(GearSlot slot)
        {
            if (!IsShopOpen()) return false;
            int next = GearLevel(slot) + 1;
            if (next >= GearCount(slot)) return false;
            if (!Unlock.IsShop(GearUnlock(slot, next))) return false;
            int price = GearPrice(slot, next);
            if (Save.Money < price) return false;
            Save.Money -= price;
            switch (slot)
            {
                case GearSlot.Rod: Save.Rod = next; break;
                case GearSlot.Reel: Save.Reel = next; break;
                default: Save.Tackle = next; break;
            }
            Persist();
            return true;
        }

        /// <summary>빚을 갚는다. 실제로 갚은 돈을 돌려준다</summary>
        public int Repay(int amount)
        {
            int pay = Math.Min(amount, Math.Min(Save.Money, Save.Debt));
            if (pay <= 0) return 0;
            Save.Money -= pay;
            Save.Debt -= pay;
            Persist();
            return pay;
        }

        /// <summary>의뢰에 맞는 물고기 (활어·선어 중 큰 순서). 모자라면 null</summary>
        public List<FishItem> FishForRequest(int requestId)
        {
            var req = Save.Requests.FirstOrDefault(r => r.Id == requestId);
            if (req == null) return null;
            var t = Data.RequestTemplates[req.Template];
            var ok = Save.Cooler
                .Where(f => f.Species == t.Species && (Fresh.StageOf(f.Fresh) == FreshStage.Live || Fresh.StageOf(f.Fresh) == FreshStage.Fresh))
                .OrderByDescending(f => f.Size)
                .ToList();
            return ok.Count >= t.Count ? ok.Take(t.Count).ToList() : null;
        }

        /// <summary>납품. 보상을 돌려준다. 못 하면 0</summary>
        public int DeliverRequest(int requestId)
        {
            var fish = FishForRequest(requestId);
            var req = Save.Requests.FirstOrDefault(r => r.Id == requestId);
            if (fish == null || req == null) return 0;
            RemoveFish(fish.Select(f => f.Uid));
            Save.Requests = Save.Requests.Where(r => r.Id != requestId).ToList();
            Earn(req.Reward);
            var flag = Data.RequestTemplates[req.Template].Flag;
            if (!string.IsNullOrEmpty(flag)) Save.Flags[flag] = true;
            if (Diner != null) Diner.AddRequestRep(Save);
            Tally("request");
            if (Save.Kit != null) Save.Kit.Requests += 1;
            Persist();
            return req.Reward;
        }

        /// <summary>
        /// 쿨러를 어종·신선도 단계별로 묶는다 (웹 MarketScene groups).
        /// species.json 순서, 같은 어종이면 활어 → 선어 → 숙성 → 상함
        /// </summary>
        public List<CoolerGroup> CoolerGroups()
        {
            var map = new Dictionary<string, CoolerGroup>();
            foreach (var f in Save.Cooler)
            {
                var stage = Fresh.StageOf(f.Fresh);
                string key = f.Species + "-" + Freshness.Key(stage);
                CoolerGroup g;
                if (!map.TryGetValue(key, out g))
                {
                    g = new CoolerGroup { Species = f.Species, Stage = stage };
                    map.Add(key, g);
                }
                g.Items.Add(f);
                g.Value += FishValue(f);
            }
            return map.Values
                .OrderBy(g => Data.SpeciesIndex(g.Species))
                .ThenBy(g => (int)g.Stage)
                .ToList();
        }

        /// <summary>경매에 낼 수 있는 물고기 (상한 것 빼고 전부)</summary>
        public List<FishItem> SellableFish()
        {
            return Save.Cooler.Where(f => Fresh.StageOf(f.Fresh) != FreshStage.Spoiled).ToList();
        }

        /// <summary>의뢰에 쓸 수 있는(활어·선어) 해당 어종 마릿수</summary>
        public int RequestFishHave(string species)
        {
            return Save.Cooler.Count(f => f.Species == species && Fresh.StageOf(f.Fresh) <= FreshStage.Fresh);
        }

        /// <summary>그 어종으로 만드는 요리 (없으면 null)</summary>
        public Recipe RecipeForSpecies(string species)
        {
            return Data.Recipes.Recipes.FirstOrDefault(r => r.Species == species);
        }

        public List<FishItem> SpoiledFish()
        {
            return Save.Cooler.Where(f => Fresh.StageOf(f.Fresh) == FreshStage.Spoiled).ToList();
        }

        /// <summary>상한 생선을 고등어에게 준다. 호감이 오르면 입질 힌트가 정확해진다</summary>
        public int FeedCat()
        {
            var bad = SpoiledFish();
            if (bad.Count == 0) return 0;
            RemoveFish(bad.Select(f => f.Uid));
            Save.CatAffinity = Math.Min(Data.Balance.Cat.MaxAffinity, Save.CatAffinity + bad.Count);
            Persist();
            return bad.Count;
        }

        /// <summary>트럭에서 요리 한 접시를 낸다. 가장 좋은 재료를 쓰고 값을 받는다. 재료가 없으면 null</summary>
        public ServedDish ServeDish(Recipe recipe, IList<SeasonResult> results)
        {
            var fish = Truck.BestFishFor(Save, recipe);
            if (fish == null) return null;
            var stage = Fresh.StageOf(fish.Fresh);
            RemoveFish(new[] { fish.Uid });
            double avg = Truck.AverageQuality(results);
            int pay = Truck.DishPay(recipe, stage, avg);
            Earn(pay);
            Save.DishesCooked += 1;
            bool firstTime = !Save.RecipesCooked.Contains(recipe.Id);
            if (firstTime) Save.RecipesCooked.Add(recipe.Id);
            Persist();
            return new ServedDish { Stage = stage, Avg = avg, Pay = pay, FirstTime = firstTime };
        }

        /// <summary>프롤로그를 마쳤다. 볼락구이로 짠맛이 돌아온다</summary>
        public void CompletePrologue()
        {
            Save.Flags["prologueDone"] = true;
            Save.Flags["firstTaste"] = true;
            Save.Flags["taste1"] = true;
            Save.DishesCooked = Math.Max(1, Save.DishesCooked);
            Persist();
        }

        /// <summary>새로 열린 미각 단계 (없으면 null). 플래그만 켜고 저장은 부르는 쪽이 한다</summary>
        public TasteStage CheckTasteUnlock()
        {
            return Taste.CheckUnlock(Save);
        }

        /// <summary>
        /// 마을에 들어올 때 확인한다. 빚을 다 갚았으면 클리어(조황일지 1장),
        /// 기한이 지났는데 빚이 남았으면 차압 (웹 systems/chapter.ts)
        /// </summary>
        public ChapterOutcome CheckChapter()
        {
            if (Save.Debt <= 0 && !Save.Flag("ch1Clear"))
            {
                Save.Flags["ch1Clear"] = true;
                Save.JournalPages = Math.Max(1, Save.JournalPages);
                return ChapterOutcome.Clear;
            }
            if (IsWeekly)
            {
                // 주간 상환: 연체를 두 번 하거나 마지막 상환일에 빚이 남으면 차압
                if (Save.Flag("ch1Seized") && !Save.Flag("ch1Clear")) return ChapterOutcome.Fail;
                return ChapterOutcome.None;
            }
            if (Save.Day > Data.Balance.Chapter1.DeadlineDays && Save.Debt > 0 && !Save.Flag("ch1Clear")) return ChapterOutcome.Fail;
            return ChapterOutcome.None;
        }

        // ───────── 화면에서 자주 묻는 값 ─────────

        /// <summary>오늘이 몇 월인지 (어종 제철)</summary>
        public int MonthNow()
        {
            return Rules.DateOf(Save.Day).Month;
        }

        /// <summary>지금 바다에 살고 이달이 제철(걸쳐서 포함)인 어종</summary>
        public List<Species> InSeason()
        {
            int m = MonthNow();
            string sea = SeaNow();
            string spot = SpotNow();
            return Data.Species.Where(sp => FishingRoll.LivesIn(sp, sea, spot) && FishingRoll.MonthWeight(sp, m) >= 0.5).ToList();
        }

        /// <summary>
        /// 위판장 시세판 다섯 칸: 내 쿨러에 있는 어종 먼저(많이 든 순), 그다음 낚아 본 제철 어종,
        /// 아직 못 낚은 제철 어종 (값이 크게 움직인 순)
        /// </summary>
        public List<Species> BoardSpecies(int n = 5)
        {
            int day = Save.Day;
            var inCooler = Save.Cooler
                .GroupBy(f => f.Species)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => Data.SpeciesIndex(g.Key))
                .Select(g => Data.SpeciesById(g.Key))
                .ToList();
            var rest = InSeason()
                .Where(sp => !inCooler.Contains(sp))
                .OrderByDescending(sp => Save.Dex.TryGetValue(sp.Id, out var e) && e.Count > 0)
                .ThenByDescending(sp => Math.Abs(Market.ChangePercent(sp.Id, day)))
                .ThenBy(sp => Data.SpeciesIndex(sp.Id));
            return inCooler.Concat(rest).Take(n).ToList();
        }

        /// <summary>오늘 마을 소식 (제철 어종 중 가장 크게 움직인 것)</summary>
        public string HeadlineToday()
        {
            return Market.Headline(Save.Day, InSeason());
        }

        /// <summary>지금 장의 바다 (어종이 사는 곳)</summary>
        public string SeaNow()
        {
            return Data.Balance.Chapter1.Sea;
        }

        /// <summary>지금 장의 낚시터 (1장은 방파제)</summary>
        public string SpotNow()
        {
            return Data.Balance.Chapter1.Spot;
        }

        public string DateLabel()
        {
            return Rules.DateLabel(Save.Day);
        }

        /// <summary>오늘 금어기인 어종 이름 (지금 장의 바다와 낚시터에서 낚이는 것만)</summary>
        public List<string> ClosedToday()
        {
            string sea = SeaNow(), spot = SpotNow();
            var here = new HashSet<string>(Data.Species.Where(sp => FishingRoll.LivesIn(sp, sea, spot)).Select(sp => sp.Name));
            return Rules.ClosedToday(Save.Day).Where(here.Contains).ToList();
        }

        public string ReleaseReason(string species, double size)
        {
            return Rules.ReleaseReason(species, size, Save.Day);
        }

        public int TasteLevel()
        {
            return Taste.Level(Save);
        }

        public int FishValue(FishItem item)
        {
            return Market.FishValue(item, Save.Day);
        }
    }

    public enum GearSlot
    {
        Rod,
        Reel,
        Tackle,
    }
}

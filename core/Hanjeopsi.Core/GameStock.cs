using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;

namespace Hanjeopsi.Core
{
    /// <summary>메뉴판 한 칸: 요리 하나와 오늘 낼 접시 수</summary>
    public sealed class MenuEntry
    {
        public Recipe Recipe;
        /// <summary>오늘 낼 접시 수 (고른 수)</summary>
        public int Count;
        /// <summary>이번 장사에 이미 낸 접시</summary>
        public int Served;

        public int Left
        {
            get { return Count - Served; }
        }
    }

    /// <summary>내 장비 화면 능력치 한 줄. Value 는 보여 줄 수, Fill 은 막대 채움(0~1, 마지막 단계 장비가 1)</summary>
    public sealed class AbilityLine
    {
        /// <summary>texts.json notebook "stat" + Id 이름, "statv" + Id 값 글</summary>
        public string Id;
        public double Value;
        public double Fill;
    }

    /// <summary>
    /// 고닷 보관과 메뉴판: 여울호 활어 수조(넣은 고기는 활어 그대로 산다), 낚시 중 쿨러에서 놓아주기,
    /// 메뉴판 칸마다 접시 수를 정해 한 고기가 두 요리에 겹쳐 잡히지 않게 한다 (2026-10-29 사용자 요청)
    /// </summary>
    public sealed partial class Game
    {
        // ───────── 쿨러와 수조 ─────────

        /// <summary>쿨러와 수조의 고기 모두 (수조가 없으면 쿨러만)</summary>
        public static IEnumerable<FishItem> AllFish(SaveData save)
        {
            return save.Tank == null ? save.Cooler : save.Cooler.Concat(save.Tank.Fish);
        }

        public IEnumerable<FishItem> AllFish()
        {
            return AllFish(Save);
        }

        public bool HasTank()
        {
            return Save.Tank != null && Data.Gear.Tanks.Count > 0;
        }

        public List<FishItem> TankFish()
        {
            return Save.Tank != null ? Save.Tank.Fish : new List<FishItem>();
        }

        public bool InTank(FishItem fish)
        {
            return Save.Tank != null && Save.Tank.Fish.Contains(fish);
        }

        public TankLevel TankNow()
        {
            return HasTank() ? Data.Gear.Tanks[Save.Tank.Level] : null;
        }

        /// <summary>다음 수조 (마지막 단계면 null)</summary>
        public TankLevel TankNext()
        {
            if (!HasTank()) return null;
            int next = Save.Tank.Level + 1;
            return next < Data.Gear.Tanks.Count ? Data.Gear.Tanks[next] : null;
        }

        public int TankCapacity()
        {
            var t = TankNow();
            return t != null ? t.Capacity : 0;
        }

        public bool TankFull()
        {
            return TankFish().Count >= TankCapacity();
        }

        /// <summary>활어만 수조에 들어간다</summary>
        public bool CanMoveToTank(FishItem fish)
        {
            return HasTank() && !TankFull() && Save.Cooler.Contains(fish) && Fresh.StageOf(fish.Fresh) == FreshStage.Live;
        }

        /// <summary>쿨러의 활어를 수조로 옮긴다</summary>
        public bool MoveToTank(int uid)
        {
            var fish = Save.Cooler.FirstOrDefault(f => f.Uid == uid);
            if (fish == null || !CanMoveToTank(fish)) return false;
            Save.Cooler.Remove(fish);
            Save.Tank.Fish.Add(fish);
            Persist();
            return true;
        }

        /// <summary>수조 빈자리</summary>
        public int TankRoom()
        {
            return HasTank() ? System.Math.Max(0, TankCapacity() - TankFish().Count) : 0;
        }

        /// <summary>수조에 넣을 수 있는 쿨러 고기 (활어만, 비싼 것부터)</summary>
        public List<FishItem> TankCandidates()
        {
            if (!HasTank()) return new List<FishItem>();
            return Save.Cooler
                .Where(f => Fresh.StageOf(f.Fresh) == FreshStage.Live)
                .OrderByDescending(f => FishValue(f))
                .ThenBy(f => f.Uid)
                .ToList();
        }

        /// <summary>미리 골라 둘 고기: 비싼 활어부터 수조 빈자리만큼</summary>
        public List<FishItem> TankPicks()
        {
            return TankCandidates().Take(TankRoom()).ToList();
        }

        /// <summary>
        /// 밤낚시를 마치고 날을 넘기기 전에 수조에 넣을지 물어야 하나.
        /// 밤이 지나면 쿨러 신선도가 깎여 활어가 아니게 되니 그 전에 한 번 고를 틈을 준다
        /// </summary>
        public bool AsksTankBeforeSleep()
        {
            return IsNight() && TankRoom() > 0 && TankCandidates().Count > 0;
        }

        /// <summary>수조 고기를 쿨러로 꺼낸다 (쿨러에 자리가 있어야)</summary>
        public bool MoveToCooler(int uid)
        {
            if (!HasTank() || CoolerFull()) return false;
            var fish = Save.Tank.Fish.FirstOrDefault(f => f.Uid == uid);
            if (fish == null) return false;
            Save.Tank.Fish.Remove(fish);
            Save.Cooler.Add(fish);
            Persist();
            return true;
        }

        /// <summary>수조를 다음 단계로 넓힌다. 돈이 모자라거나 마지막 단계면 false</summary>
        public bool UpgradeTank()
        {
            var next = TankNext();
            if (next == null || Save.Money < next.Price) return false;
            Save.Money -= next.Price;
            Save.Tank.Level += 1;
            Persist();
            return true;
        }

        /// <summary>쿨러에서 놓아줄 수 있는 고기 (아직 활어인 것만, 큰 놈은 뒤로)</summary>
        public List<FishItem> ReleasableFish()
        {
            return Save.Cooler
                .Where(f => Fresh.StageOf(f.Fresh) == FreshStage.Live)
                .OrderBy(f => FishValue(f))
                .ThenBy(f => f.Uid)
                .ToList();
        }

        /// <summary>
        /// 낚시 중 쿨러의 활어 한 마리를 바다로 돌려보낸다 (쿨러가 가득 찼는데 더 좋은 고기를 낚았을 때).
        /// 도감에 놓아준 수로 센다
        /// </summary>
        public bool ReleaseFromCooler(int uid)
        {
            var fish = Save.Cooler.FirstOrDefault(f => f.Uid == uid);
            if (fish == null || Fresh.StageOf(fish.Fresh) != FreshStage.Live) return false;
            Save.Cooler.Remove(fish);
            DexEntry e;
            if (Save.Dex.TryGetValue(fish.Species, out e)) e.Released += 1;
            Save.Flags["released"] = true;
            Persist();
            return true;
        }

        // ───────── 내 장비 능력치 ─────────

        /// <summary>
        /// 지금 장비로 정해지는 낚시 능력치 (찌·낚싯줄·의자까지 더한 값).
        /// 막대는 마지막 단계 장비를 다 갖췄을 때를 꽉 찬 것으로 잰다. 낮을수록 좋은 값(텐션, 신선도 감소)은 뒤집어 잰다
        /// </summary>
        public List<AbilityLine> Abilities()
        {
            var g = Data.Gear;
            var rod = EffectiveRod();
            var reel = EffectiveReel();
            var tackle = g.Tackles[Save.Tackle];
            double bestFloat = MaxKit("float"), bestLine = MaxKit("line");
            double bestReach = g.Rods.Max(r => r.Reach);
            double bestHook = g.Rods.Max(r => r.HookBonus) + bestFloat;
            double bestAbsorb = g.Rods.Max(r => r.Absorb);
            double bestSpeed = g.Reels.Max(r => r.Speed);
            double bestTension = g.Reels.Min(r => r.TensionMul) * (1 - bestLine / 100);
            double bestKeep = g.Tackles.Max(t => t.Capacity);
            double bestDecay = g.Tackles.Min(t => t.Decay);
            var list = new List<AbilityLine>
            {
                Line("Cast", System.Math.Round(Reel.CastMeters(rod.Reach), 1), Reel.CastMeters(rod.Reach) / Reel.CastMeters(bestReach)),
                Line("Hook", rod.HookBonus, bestHook > 0 ? rod.HookBonus / bestHook : 0),
                Line("Absorb", System.Math.Round(rod.Absorb * 100), bestAbsorb > 0 ? rod.Absorb / bestAbsorb : 0),
                Line("Speed", reel.Speed, reel.Speed / bestSpeed),
                Line("Tension", System.Math.Round(reel.TensionMul, 2), bestTension / reel.TensionMul),
                Line("Keep", tackle.Capacity, tackle.Capacity / bestKeep),
                Line("Fresh", tackle.Decay, bestDecay / tackle.Decay),
            };
            if (Save.Kit != null) list.Add(Line("Wait", KitValue("chair"), MaxKit("chair") > 0 ? KitValue("chair") / MaxKit("chair") : 0));
            if (HasTank()) list.Add(Line("Tank", TankCapacity(), (double)TankCapacity() / Data.Gear.Tanks.Max(t => t.Capacity)));
            return list;
        }

        private static AbilityLine Line(string id, double value, double fill)
        {
            return new AbilityLine { Id = id, Value = value, Fill = System.Math.Max(0, System.Math.Min(1, fill)) };
        }

        /// <summary>장비 종류 마지막 단계 값 (없으면 0)</summary>
        private double MaxKit(string id)
        {
            var k = KitDef(id);
            return k == null || k.Levels.Count == 0 ? 0 : k.Levels.Max(l => l.Value);
        }

        // ───────── 메뉴판 ─────────

        /// <summary>요리에 쓸 수 있는(상하지 않은) 그 어종 고기 수. 쿨러와 수조 모두</summary>
        public int DishStock(string species)
        {
            return AllFish().Count(f => f.Species == species && Fresh.StageOf(f.Fresh) != FreshStage.Spoiled);
        }

        /// <summary>
        /// 이 요리에 더 잡을 수 있는 접시 수. 같은 어종을 쓰는 다른 칸이 잡아 둔 만큼 뺀다 (self 칸은 빼고 센다)
        /// </summary>
        public int MenuFree(IList<MenuEntry> plan, Recipe recipe, MenuEntry self = null)
        {
            int held = plan.Where(e => e != self && e.Recipe.Species == recipe.Species).Sum(e => e.Left);
            return System.Math.Max(0, DishStock(recipe.Species) - held);
        }

        /// <summary>고기가 줄었으면(팔았거나 상했거나) 칸마다 접시 수를 남은 고기에 맞춰 줄이고, 0 이 된 칸은 비운다</summary>
        public void FitMenu(List<MenuEntry> plan)
        {
            foreach (var e in plan.ToList())
            {
                int free = MenuFree(plan, e.Recipe, e);
                if (e.Left > free) e.Count = e.Served + free;
                if (e.Count <= 0) plan.Remove(e);
            }
        }

        /// <summary>
        /// 메뉴판 기본 칸: 한 접시 값이 큰 요리부터 그 어종 남은 고기를 다 잡는다.
        /// 고기 하나가 두 요리에 겹치지 않고, 메뉴 칸 수를 넘지 않는다
        /// </summary>
        public List<MenuEntry> SuggestMenuPlan()
        {
            int slots = Diner != null ? Diner.MenuSlots(Save) : int.MaxValue;
            var plan = new List<MenuEntry>();
            foreach (var r in SuggestOrder())
            {
                if (plan.Count >= slots) break;
                int free = MenuFree(plan, r);
                if (free > 0) plan.Add(new MenuEntry { Recipe = r, Count = free });
            }
            return plan;
        }

        /// <summary>만들 수 있는 요리를 한 접시 값이 큰 순서로</summary>
        private List<Recipe> SuggestOrder()
        {
            return Truck.Cookable(Save)
                .OrderByDescending(r => Truck.DishPay(r, Fresh.StageOf(Truck.BestFishFor(Save, r).Fresh), 0.8))
                .ToList();
        }

        /// <summary>지금 손님이 시킬 수 있는 요리: 접시가 남았고 고기도 남은 칸</summary>
        public List<Recipe> MenuOpen(IList<MenuEntry> plan)
        {
            return plan.Where(e => e.Left > 0 && Truck.BestFishFor(Save, e.Recipe) != null).Select(e => e.Recipe).ToList();
        }

        /// <summary>이 요리를 한 접시 냈다. 메뉴판 칸에서 하나 줄인다 (메뉴에 없던 VIP 요리면 그대로)</summary>
        public static void MarkServed(IList<MenuEntry> plan, Recipe recipe)
        {
            var e = plan.FirstOrDefault(x => x.Recipe == recipe && x.Left > 0);
            if (e != null) e.Served += 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core
{
    /// <summary>받은 보상 하나 (노씨가 챙겨 준다)</summary>
    public sealed class GearGift
    {
        public string Kind;
        public int Tier;
        public string Name;
    }

    /// <summary>
    /// 고닷 장비: 상점 단계(1~4), 보상 단계(5~7, 여울호 등급·단골·의뢰·노씨·1장 클리어), 다음 지역(8~10).
    /// 낚싯대·릴·쿨러는 웹과 같은 Save.Rod/Reel/Tackle 단계(0부터)를 쓰고, 나머지는 Save.Kit.
    /// 미끼 열 가지, 밤새 통발. 웹 데이터(장비 셋, 세 단계)에서는 웹과 같은 계산이 된다
    /// </summary>
    public sealed partial class Game
    {
        public static readonly string[] CoreKinds = { "rod", "reel", "cooler" };

        // ───────── 단계와 얻는 법 ─────────

        public string GearUnlock(GearSlot slot, int level)
        {
            switch (slot)
            {
                case GearSlot.Rod: return Data.Gear.Rods[level].Unlock;
                case GearSlot.Reel: return Data.Gear.Reels[level].Unlock;
                default: return Data.Gear.Tackles[level].Unlock;
            }
        }

        public KitKind KitDef(string id)
        {
            return Data.Gear.Kits.FirstOrDefault(k => k.Id == id);
        }

        /// <summary>장비 단계 (0 없음, 1~10)</summary>
        public int KitLevel(string id)
        {
            if (Save.Kit == null) return 0;
            int lv;
            return Save.Kit.Own.TryGetValue(id, out lv) ? lv : 0;
        }

        /// <summary>지금 단계의 효과 값 (없으면 0)</summary>
        public double KitValue(string id)
        {
            int lv = KitLevel(id);
            var k = KitDef(id);
            return lv <= 0 || k == null ? 0 : k.Levels[lv - 1].Value;
        }

        /// <summary>다음 단계를 상점에서 산다</summary>
        public bool BuyKit(string id)
        {
            var k = KitDef(id);
            if (k == null || Save.Kit == null || !IsShopOpen()) return false;
            int next = KitLevel(id);
            if (next >= k.Levels.Count) return false;
            var lv = k.Levels[next];
            if (!Unlock.IsShop(lv.Unlock) || Save.Money < lv.Price) return false;
            Save.Money -= lv.Price;
            Save.Kit.Own[id] = next + 1;
            Persist();
            return true;
        }

        /// <summary>보상 조건을 채웠나</summary>
        public bool UnlockMet(string u)
        {
            if (!Unlock.IsReward(u)) return false;
            var parts = u.Split(':');
            int n = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            switch (parts[0])
            {
                case "rank": return Diner != null && Diner.RankOf(Save) >= n;
                case "regulars": return Diner != null && Data.Diner.Guests.Count(g => Diner.IsRegular(Save, g.Id)) >= n;
                case "requests": return Save.Kit != null && Save.Kit.Requests >= n;
                case "favor": return Save.Flag("nossiFavor");
                case "clear": return Save.Flag("ch1Clear");
                default: return false;
            }
        }

        /// <summary>
        /// 조건을 채운 보상 장비를 준다. 지금보다 좋은 것만 (가장 높은 단계로 바로 오른다).
        /// 받은 것 목록을 돌려준다
        /// </summary>
        public List<GearGift> CheckGifts()
        {
            return Gifts(true);
        }

        /// <summary>받을 보상이 있나 (주지는 않는다)</summary>
        public bool HasGift()
        {
            return Gifts(false).Count > 0;
        }

        private List<GearGift> Gifts(bool give)
        {
            var gifts = new List<GearGift>();
            if (Save.Kit == null) return gifts;
            void Try(string kind, int have, int count, Func<int, string> unlock, Func<int, string> name, Action<int> set)
            {
                // 단계 번호는 1부터. have 는 지금 단계
                for (int t = count; t > have; t--)
                {
                    string key = kind + "-" + t;
                    if (Save.Kit.Gifts.Contains(key) || !UnlockMet(unlock(t))) continue;
                    if (give)
                    {
                        Save.Kit.Gifts.Add(key);
                        set(t);
                    }
                    gifts.Add(new GearGift { Kind = kind, Tier = t, Name = name(t) });
                    break;
                }
            }
            var g = Data.Gear;
            Try("rod", Save.Rod + 1, g.Rods.Count, t => g.Rods[t - 1].Unlock, t => g.Rods[t - 1].Name, t => Save.Rod = t - 1);
            Try("reel", Save.Reel + 1, g.Reels.Count, t => g.Reels[t - 1].Unlock, t => g.Reels[t - 1].Name, t => Save.Reel = t - 1);
            Try("cooler", Save.Tackle + 1, g.Tackles.Count, t => g.Tackles[t - 1].Unlock, t => g.Tackles[t - 1].Name, t => Save.Tackle = t - 1);
            foreach (var k in g.Kits)
                Try(k.Id, KitLevel(k.Id), k.Levels.Count, t => k.Levels[t - 1].Unlock, t => k.Levels[t - 1].Name, t => Save.Kit.Own[k.Id] = t);
            if (give && gifts.Count > 0) Persist();
            return gifts;
        }

        // ───────── 낚시에 쓰는 장비 값 ─────────

        /// <summary>찌까지 더한 낚싯대 (챔질 시간)</summary>
        public Rod EffectiveRod(int? level = null)
        {
            var r = Data.Gear.Rods[level ?? Save.Rod];
            double f = KitValue("float");
            if (f <= 0) return r;
            return new Rod { Name = r.Name, Desc = r.Desc, Price = r.Price, Reach = r.Reach, Absorb = r.Absorb, HookBonus = r.HookBonus + (int)f, Unlock = r.Unlock };
        }

        /// <summary>낚싯줄까지 더한 릴 (텐션이 덜 오른다)</summary>
        public Reel EffectiveReel(int? level = null)
        {
            var r = Data.Gear.Reels[level ?? Save.Reel];
            double line = KitValue("line");
            if (line <= 0) return r;
            return new Reel { Name = r.Name, Desc = r.Desc, Price = r.Price, Speed = r.Speed, TensionMul = r.TensionMul * (1 - line / 100), Drag = r.Drag, Unlock = r.Unlock };
        }

        /// <summary>힘싸움 준비. 바늘과 뜰채를 더한다</summary>
        public FightSetup Fight(Species sp, double size, double dist01, bool perfect, bool tutorial)
        {
            return new FightSetup
            {
                Species = sp,
                Size = size,
                Dist01 = dist01,
                Perfect = perfect,
                Rod = tutorial ? Data.Gear.Rods[1] : EffectiveRod(),
                Reel = tutorial ? Data.Gear.Reels[1] : EffectiveReel(),
                Tutorial = tutorial,
                SlackBonus = tutorial ? 0 : KitValue("hook"),
                LandAt = tutorial ? 0 : KitValue("net"),
            };
        }

        /// <summary>입질 대기 배율: 날씨 × 의자 × 밤이면 헤드랜턴 × 궂은 날 장화 × 미끼</summary>
        public double WaitMul()
        {
            var w = WeatherToday();
            double boots = KitValue("boots");
            double mul = w.WaitMul;
            if (w.BootsCancel && KitLevel("boots") > 0) mul = Math.Min(1, mul);
            if (w.Id == "rain" || w.Id == "snow") mul *= 1 - boots / 100;
            mul *= 1 - KitValue("chair") / 100;
            if (Save.Slot == Data.Balance.Time.SlotsPerDay - 1) mul *= 1 - KitValue("lamp") / 100;
            var bait = BaitNow();
            if (bait != null) mul *= bait.WaitMul;
            return mul;
        }

        // ───────── 미끼 ─────────

        public BaitKind BaitDef(string id)
        {
            return Data.Gear.Baits.FirstOrDefault(b => b.Id == id);
        }

        /// <summary>쓰는 미끼 (웹 데이터면 null)</summary>
        public BaitKind BaitNow()
        {
            return Save.Kit == null ? null : BaitDef(Save.Kit.Pick);
        }

        public int BaitCount(string id)
        {
            if (id == "krill" || Save.Kit == null) return Save.Bait;
            int n;
            return Save.Kit.Bait.TryGetValue(id, out n) ? n : 0;
        }

        /// <summary>쓰는 미끼 남은 수. 다 떨어지면 크릴로 돌아간다</summary>
        public int BaitLeft()
        {
            if (Save.Kit == null) return Save.Bait;
            if (Save.Kit.Pick != "krill" && BaitCount(Save.Kit.Pick) <= 0) Save.Kit.Pick = "krill";
            return BaitCount(Save.Kit.Pick);
        }

        public void PickBait(string id)
        {
            if (Save.Kit == null || BaitDef(id) == null) return;
            Save.Kit.Pick = id;
            Persist();
        }

        /// <summary>미끼 한 봉지 (크릴은 BuyBait). 미끼통만큼 더 들어간다</summary>
        public bool BuyBaitKind(string id)
        {
            if (id == "krill") return BuyBait();
            var b = BaitDef(id);
            if (b == null || Save.Kit == null || !IsShopOpen() || Save.Money < b.Price) return false;
            Save.Money -= b.Price;
            Save.Kit.Bait[id] = BaitCount(id) + b.Amount + (int)KitValue("baitbox");
            Persist();
            return true;
        }

        /// <summary>던질 때 미끼 하나. 쓰는 미끼에서 빠진다</summary>
        public void UseBaitNow()
        {
            if (Save.Kit == null || BaitLeft() <= 0 || Save.Kit.Pick == "krill")
            {
                UseBait();
                return;
            }
            Save.Kit.Bait[Save.Kit.Pick] -= 1;
            Persist();
        }

        /// <summary>어종 뽑기 가중치: 쓰는 미끼를 좋아하는 어종이 잘 문다</summary>
        public Func<Species, double> BaitBias()
        {
            var b = BaitNow();
            if (b == null || b.Likes.Count == 0) return null;
            double mul = Data.Gear.BaitLikeMul;
            return sp => b.Likes.Contains(sp.Id) ? mul : 1;
        }

        // ───────── 통발 ─────────

        /// <summary>
        /// 밤새 통발: 통발이 있고 크릴이 남았으면 하나 써서 놓아 두고, 아침에 든 고기를 쿨러에 넣는다.
        /// 금지체장·금어기 고기는 놓아준다. 든 어종 목록(없으면 null)
        /// </summary>
        private List<string> CheckTrap()
        {
            int n = (int)KitValue("trap");
            if (n <= 0 || Save.Bait <= 0 || Data.Gear.TrapPool.Count == 0) return null;
            Save.Bait -= 1;
            var rng = new Mulberry32(Mulberry32.Hash(Save.Seed, Save.Day, 7717));
            int month = MonthNow();
            var pool = Data.Gear.TrapPool.Select(Data.SpeciesById).Where(sp => FishingRoll.LivesIn(sp, SeaNow())).ToList();
            var weights = pool.Select(sp => FishingRoll.MonthWeight(sp, month)).ToList();
            var got = new List<string>();
            for (int i = 0; i < n && !CoolerFull(); i++)
            {
                var sp = FishingRoll.WeightedPick(pool, weights, rng.Next());
                double size = JsMath.Round((sp.AvgSize + (sp.MaxSize - sp.AvgSize) * rng.Next() * 0.8) * 10) / 10;
                bool release = Rules.ReleaseReason(sp.Id, size, Save.Day) != null;
                RecordCatch(sp.Id, size, release);
                if (release) continue;
                AddToCooler(sp.Id, size);
                got.Add(sp.Name);
            }
            return got;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>손님 등급 (친밀도와 예약, 이야기로 정한다)</summary>
    public enum GuestKind
    {
        Normal,
        /// <summary>친밀도가 단골 기준을 넘은 손님. 좋아하는 요리를 시키고 팁을 준다</summary>
        Regular,
        /// <summary>그날 예약한 VIP. 정해진 요리를 정해진 신선도로 바란다</summary>
        Vip,
        /// <summary>이야기 인물</summary>
        Special,
    }

    /// <summary>이번 장사에 올 손님 한 명</summary>
    public sealed class PlannedGuest
    {
        /// <summary>일반·단골은 diner guests id, VIP 는 vips id, 특별 손님은 specials id</summary>
        public string Id;
        public GuestKind Kind;
        /// <summary>VIP·특별 손님이 바라는 요리 id (없으면 null)</summary>
        public string Wish;

        public override string ToString()
        {
            return Kind + ":" + Id;
        }
    }

    /// <summary>그날 들어온 VIP 예약</summary>
    public sealed class VipBooking
    {
        public DinerVip Vip;
        public Recipe Recipe;
    }

    /// <summary>손님 한 명에게 요리를 낸 뒤 바뀐 것</summary>
    public sealed class GuestResult
    {
        public GuestKind Kind;
        public int RepGain;
        public int Affinity;
        /// <summary>이번에 단골이 됐다</summary>
        public bool BecameRegular;
        /// <summary>좋아하는 요리였다</summary>
        public bool Favorite;
        /// <summary>요리값과 따로 받은 팁 (단골·VIP)</summary>
        public int Tip;
        /// <summary>VIP 가 바란 대로 먹었다</summary>
        public bool VipOk;
    }

    /// <summary>
    /// 여울호 등급과 손님 (고닷만, diner.json).
    /// 저녁 손님 수는 평판(명성)이 정한다. 단골은 자리를 먼저 차지하고, VIP 예약과 특별 손님은 그 위에 더 온다.
    /// 메뉴는 플레이어가 등급만큼 고르고, 일반 손님은 메뉴 안에서 좋아하는 요리를 먼저, 단골은 늘 좋아하는 요리를 시킨다.
    /// </summary>
    public sealed class Diner
    {
        private readonly DinerTable _t;

        public Diner(DinerTable table)
        {
            _t = table;
        }

        public DinerTable Table
        {
            get { return _t; }
        }

        private static DinerState State(SaveData save)
        {
            return save.Diner ??= new DinerState();
        }

        /// <summary>지금 평판으로 되는 등급 (0부터)</summary>
        public int RankOf(SaveData save)
        {
            int rep = State(save).Rep;
            int rank = 0;
            for (int i = 0; i < _t.Ranks.Count; i++)
                if (rep >= _t.Ranks[i].Rep) rank = i;
            return rank;
        }

        public DinerRank Rank(SaveData save)
        {
            return _t.Ranks[RankOf(save)];
        }

        /// <summary>다음 등급 (마지막 등급이면 null)</summary>
        public DinerRank NextRank(SaveData save)
        {
            int i = RankOf(save) + 1;
            return i < _t.Ranks.Count ? _t.Ranks[i] : null;
        }

        public int MenuSlots(SaveData save)
        {
            return Rank(save).Menu;
        }

        public int AffinityOf(SaveData save, string guest)
        {
            int a;
            return State(save).Affinity.TryGetValue(guest, out a) ? a : 0;
        }

        public bool IsRegular(SaveData save, string guest)
        {
            return AffinityOf(save, guest) >= _t.RegularAt;
        }

        public DinerVip VipById(string id)
        {
            return _t.Vips.First(v => v.Id == id);
        }

        public DinerSpecial SpecialById(string id)
        {
            return _t.Specials.First(v => v.Id == id);
        }

        /// <summary>단골 손님 모두</summary>
        public List<DinerGuest> Regulars(SaveData save)
        {
            return _t.Guests.Where(g => IsRegular(save, g.Id)).ToList();
        }

        /// <summary>
        /// 평판으로 정하는 저녁 손님 수. 등급이 오를 때 그 등급 손님 수가 되고,
        /// 다음 등급까지는 평판이 쌓이는 만큼 한 명씩 는다 (마지막 등급은 그대로)
        /// </summary>
        public int BaseGuests(SaveData save)
        {
            int rep = State(save).Rep;
            int i = RankOf(save);
            var now = _t.Ranks[i];
            if (i + 1 >= _t.Ranks.Count) return now.Guests;
            var next = _t.Ranks[i + 1];
            return now.Guests + (rep - now.Rep) * (next.Guests - now.Guests) / Math.Max(1, next.Rep - now.Rep);
        }

        /// <summary>이번 장사 손님 수 (VIP·특별 손님 빼고) = 평판 손님 + 날씨·장날 보정. 단골이 더 많으면 단골 수만큼, 최대 MaxGuests</summary>
        public int GuestCount(SaveData save, int weatherAdd = 0)
        {
            int n = Math.Max(1, BaseGuests(save) + weatherAdd);
            n = Math.Max(n, Regulars(save).Count);
            return Math.Min(_t.MaxGuests, n);
        }

        /// <summary>
        /// 이번 장사에 올 손님 차례. 단골은 꼭 한 번씩 오고, 나머지는 단골이 아닌 손님 중에서 뽑는다(앞 손님과 다른 사람).
        /// VIP 는 첫 손님 뒤 아무 자리에, 특별 손님은 맨 끝에 온다
        /// </summary>
        public List<PlannedGuest> PlanGuests(SaveData save, IRandom rng, int weatherAdd = 0, IList<VipBooking> vips = null, PlannedGuest special = null)
        {
            var regulars = Regulars(save).Select(g => g.Id).ToList();
            var pool = _t.Guests.Where(g => !regulars.Contains(g.Id)).ToList();
            if (pool.Count == 0) pool = _t.Guests;
            int n = GuestCount(save, weatherAdd);
            var list = new List<string>();
            for (int i = regulars.Count; i < n; i++)
            {
                var choices = pool.Where(g => pool.Count == 1 || list.Count == 0 || g.Id != list[list.Count - 1]).ToList();
                list.Add(choices[(int)(rng.Next() * choices.Count)].Id);
            }
            // 단골은 앞뒤 손님과 다른 자리에 끼워 넣는다
            foreach (var r in regulars.Take(n))
            {
                var spots = Enumerable.Range(0, list.Count + 1)
                    .Where(p => (p == 0 || list[p - 1] != r) && (p == list.Count || list[p] != r))
                    .ToList();
                int at = spots.Count > 0 ? spots[(int)(rng.Next() * spots.Count)] : list.Count;
                list.Insert(at, r);
            }
            var plan = list.Select(id => new PlannedGuest { Id = id, Kind = regulars.Contains(id) ? GuestKind.Regular : GuestKind.Normal }).ToList();
            if (vips != null)
                foreach (var v in vips)
                {
                    int at = 1 + (int)(rng.Next() * plan.Count);
                    plan.Insert(Math.Min(at, plan.Count), new PlannedGuest { Id = v.Vip.Id, Kind = GuestKind.Vip, Wish = v.Recipe.Id });
                }
            if (special != null) plan.Add(special);
            return plan;
        }

        /// <summary>
        /// 손님 주문. VIP 는 예약한 요리(메뉴에 없어도 시킨다), 특별 손님은 바라는 요리나 메뉴에서 제일 비싼 요리,
        /// 단골은 메뉴에 좋아하는 요리가 있으면 늘 그걸, 일반 손님은 대개 그걸 시킨다
        /// </summary>
        public Recipe Order(PlannedGuest guest, IList<Recipe> menu, IRandom rng, Func<string, Recipe> recipeById = null)
        {
            if (guest.Kind == GuestKind.Vip && recipeById != null && guest.Wish != null) return recipeById(guest.Wish);
            if (menu.Count == 0) return null;
            if (guest.Kind == GuestKind.Special)
            {
                var want = SpecialById(guest.Id).Wants.Select(id => menu.FirstOrDefault(r => r.Id == id)).FirstOrDefault(r => r != null);
                return want ?? menu.OrderByDescending(r => r.Price).First();
            }
            if (guest.Kind == GuestKind.Vip) return menu[(int)(rng.Next() * menu.Count)];
            var likes = _t.GuestById(guest.Id).Likes;
            var fav = menu.Where(r => likes.Contains(r.Id)).ToList();
            double chance = guest.Kind == GuestKind.Regular ? 1 : _t.FavoriteChance;
            if (fav.Count > 0 && rng.Next() < chance) return fav[(int)(rng.Next() * fav.Count)];
            return menu[(int)(rng.Next() * menu.Count)];
        }

        /// <summary>요리를 낸 뒤 평판·친밀도·팁. mood 0 실망, 1 보통, 2 만족. stage 는 쓴 고기 신선도</summary>
        public GuestResult AfterServe(SaveData save, PlannedGuest guest, Recipe recipe, int mood, int pay, FreshStage stage)
        {
            var st = State(save);
            if (guest.Kind == GuestKind.Vip)
            {
                var vip = VipById(guest.Id);
                bool ok = mood >= 1 && (int)stage <= vip.Fresh && recipe.Id == guest.Wish;
                int vrep = ok ? vip.Rep : vip.MissRep;
                st.Rep = Math.Max(0, st.Rep + vrep);
                return new GuestResult { Kind = GuestKind.Vip, RepGain = vrep, VipOk = ok, Tip = ok ? JsMath.Round100(pay * vip.TipMul) : 0 };
            }
            if (guest.Kind == GuestKind.Special)
            {
                var sp = SpecialById(guest.Id);
                int srep = sp.Rep[Math.Min(mood, sp.Rep.Count - 1)];
                st.Rep = Math.Max(0, st.Rep + srep);
                save.Flags[SpecialFlag(sp.Id)] = true;
                return new GuestResult { Kind = GuestKind.Special, RepGain = srep };
            }
            bool wasRegular = IsRegular(save, guest.Id);
            bool fav = _t.GuestById(guest.Id).Likes.Contains(recipe.Id);
            int rep = _t.RepByMood[mood];
            st.Rep = Math.Max(0, st.Rep + rep);
            int aff = AffinityOf(save, guest.Id) + _t.AffinityByMood[mood] + (fav && mood >= 1 ? _t.AffinityFavorite : 0);
            aff = Math.Max(0, Math.Min(_t.AffinityMax, aff));
            st.Affinity[guest.Id] = aff;
            return new GuestResult
            {
                Kind = wasRegular ? GuestKind.Regular : GuestKind.Normal,
                RepGain = rep,
                Affinity = aff,
                BecameRegular = !wasRegular && aff >= _t.RegularAt,
                Favorite = fav,
                Tip = wasRegular && mood >= 1 ? JsMath.Round100(pay * _t.RegularTip) : 0,
            };
        }

        /// <summary>VIP 가 바란 요리를 못 냈다 (재료가 없거나 신선도가 모자람)</summary>
        public int VipMissed(SaveData save, PlannedGuest guest)
        {
            int rep = VipById(guest.Id).MissRep;
            State(save).Rep = Math.Max(0, State(save).Rep + rep);
            return rep;
        }

        /// <summary>특별 손님이 이미 왔나</summary>
        public static string SpecialFlag(string id)
        {
            return "special:" + id;
        }

        /// <summary>
        /// 그날 VIP 예약. 시드와 날짜, 지금 등급으로 정해서 같은 날은 늘 같다.
        /// 등급마다 첫째·둘째 예약 확률이 있고, VIP 는 서로 다른 사람이다
        /// </summary>
        public List<VipBooking> Bookings(SaveData save, int day, Func<string, Recipe> recipeById, Func<Recipe, bool> possible = null)
        {
            var list = new List<VipBooking>();
            if (_t.Vips.Count == 0 || _t.VipChance.Count == 0) return list;
            var chances = _t.VipChance[Math.Min(RankOf(save), _t.VipChance.Count - 1)];
            var rng = new Mulberry32(Mulberry32.Hash(save.Seed, day, _t.VipSeedSalt));
            var left = _t.Vips.ToList();
            foreach (double c in chances)
            {
                double roll = rng.Next();
                double pick = rng.Next();
                double wish = rng.Next();
                if (roll >= c || left.Count == 0) break;
                var vip = left[(int)(pick * left.Count)];
                left.Remove(vip);
                // 오늘 낚을 수 없는 고기(제철이 아니거나 금어기) 요리는 바라지 않는다
                var wants = vip.Wants.Select(recipeById).Where(r => possible == null || possible(r)).ToList();
                if (wants.Count == 0) continue;
                list.Add(new VipBooking { Vip = vip, Recipe = wants[(int)(wish * wants.Count)] });
            }
            return list;
        }

        /// <summary>납품 의뢰를 마치면 마을에 소문이 난다</summary>
        public void AddRequestRep(SaveData save)
        {
            State(save).Rep += _t.RepRequest;
        }

        /// <summary>새로 오른 등급 (없으면 null). 한 번 알리면 다시 알리지 않는다</summary>
        public DinerRank CheckRankUp(SaveData save)
        {
            var st = State(save);
            int now = RankOf(save);
            if (now <= st.Rank) return null;
            st.Rank = now;
            return _t.Ranks[now];
        }
    }
}

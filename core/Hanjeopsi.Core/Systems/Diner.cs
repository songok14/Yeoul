using System;
using System.Collections.Generic;
using System.Linq;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Util;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>손님 한 명에게 요리를 낸 뒤 바뀐 것</summary>
    public sealed class GuestResult
    {
        public int RepGain;
        public int Affinity;
        /// <summary>이번에 단골이 됐다</summary>
        public bool BecameRegular;
        /// <summary>좋아하는 요리였다</summary>
        public bool Favorite;
    }

    /// <summary>
    /// 여울호 등급과 손님 (고닷만, diner.json).
    /// 기본 손님은 등급과 시간대로 정하고, 그 시간대 단골은 그 위에 꼭 온다.
    /// 메뉴는 플레이어가 등급만큼 고르고, 손님은 메뉴 안에서 좋아하는 요리를 먼저 시킨다.
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

        /// <summary>이 시간대에 오는 손님</summary>
        public List<DinerGuest> GuestsAt(int slot)
        {
            return _t.Guests.Where(g => g.Slots.Contains(slot)).ToList();
        }

        /// <summary>이 시간대 단골</summary>
        public List<DinerGuest> RegularsAt(SaveData save, int slot)
        {
            return GuestsAt(slot).Where(g => IsRegular(save, g.Id)).ToList();
        }

        /// <summary>이번 장사 손님 수 = 등급 기본 + 시간대 보정 + 이 시간대 단골 (최대 MaxGuests)</summary>
        public int GuestCount(SaveData save, int slot, int weatherAdd = 0)
        {
            if (GuestsAt(slot).Count == 0) return 0;
            int bonus = (slot < _t.SlotBonus.Count ? _t.SlotBonus[slot] : 0) + weatherAdd;
            int n = Math.Max(1, Rank(save).Guests + bonus) + RegularsAt(save, slot).Count;
            return Math.Min(_t.MaxGuests, n);
        }

        /// <summary>
        /// 이번 장사에 올 손님 차례. 단골은 꼭 한 번씩 오고, 나머지는 이 시간대 손님 중에서 뽑는다.
        /// 같은 손님이 두 번 잇달아 오지는 않는다
        /// </summary>
        public List<string> PlanGuests(SaveData save, int slot, IRandom rng, int weatherAdd = 0)
        {
            var pool = GuestsAt(slot);
            var regulars = RegularsAt(save, slot).Select(g => g.Id).ToList();
            int n = GuestCount(save, slot, weatherAdd);
            // 단골이 아닌 손님: 바로 앞 손님과 다른 사람으로 뽑는다
            var list = new List<string>();
            for (int i = regulars.Count; i < n; i++)
            {
                var choices = pool.Where(g => pool.Count == 1 || list.Count == 0 || g.Id != list[list.Count - 1]).ToList();
                list.Add(choices[(int)(rng.Next() * choices.Count)].Id);
            }
            // 단골은 앞뒤 손님과 다른 자리에 끼워 넣는다
            foreach (var r in regulars)
            {
                var spots = Enumerable.Range(0, list.Count + 1)
                    .Where(p => (p == 0 || list[p - 1] != r) && (p == list.Count || list[p] != r))
                    .ToList();
                int at = spots.Count > 0 ? spots[(int)(rng.Next() * spots.Count)] : list.Count;
                list.Insert(at, r);
            }
            return list;
        }

        /// <summary>손님 주문: 메뉴에 좋아하는 요리가 있으면 대개 그걸, 아니면 메뉴에서 아무거나</summary>
        public Recipe Order(string guest, IList<Recipe> menu, IRandom rng)
        {
            if (menu.Count == 0) return null;
            var likes = _t.GuestById(guest).Likes;
            var fav = menu.Where(r => likes.Contains(r.Id)).ToList();
            if (fav.Count > 0 && rng.Next() < _t.FavoriteChance) return fav[(int)(rng.Next() * fav.Count)];
            return menu[(int)(rng.Next() * menu.Count)];
        }

        /// <summary>요리를 낸 뒤 평판과 친밀도. mood 0 실망, 1 보통, 2 만족</summary>
        public GuestResult AfterServe(SaveData save, string guest, Recipe recipe, int mood)
        {
            var st = State(save);
            bool wasRegular = IsRegular(save, guest);
            bool fav = _t.GuestById(guest).Likes.Contains(recipe.Id);
            int rep = _t.RepByMood[mood];
            st.Rep = Math.Max(0, st.Rep + rep);
            int aff = AffinityOf(save, guest) + _t.AffinityByMood[mood] + (fav && mood >= 1 ? _t.AffinityFavorite : 0);
            aff = Math.Max(0, Math.Min(_t.AffinityMax, aff));
            st.Affinity[guest] = aff;
            return new GuestResult
            {
                RepGain = rep,
                Affinity = aff,
                BecameRegular = !wasRegular && aff >= _t.RegularAt,
                Favorite = fav,
            };
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

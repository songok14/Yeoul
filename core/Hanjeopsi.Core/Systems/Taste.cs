using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>미각 회복 단계 (웹 systems/taste.ts). 낚은 어종 수로 맛이 돌아온다</summary>
    public sealed class Taste
    {
        private readonly GameData _data;

        public Taste(GameData data)
        {
            _data = data;
        }

        public int SpeciesCaught(SaveData save)
        {
            int n = 0;
            foreach (var e in save.Dex.Values)
                if (e != null && e.Count > 0) n++;
            return n;
        }

        /// <summary>
        /// 프롤로그 볼락구이(firstTaste) 전에는 0. 그 뒤로는 1에서 시작해
        /// needSpecies 를 채운 단계까지 오른다 (1장: 3종 → 2, 5종 → 3)
        /// </summary>
        public int Level(SaveData save)
        {
            if (!save.Flag("firstTaste")) return 0;
            int n = SpeciesCaught(save);
            int level = 1;
            var stages = _data.TasteStages;
            for (int i = 2; i < stages.Count; i++)
            {
                var need = stages[i].NeedSpecies;
                if (!need.HasValue || n < need.Value) break;
                level = i;
            }
            return level;
        }

        public TasteStage Stage(int level)
        {
            return _data.TasteStages[level];
        }

        public bool CanTaste(SaveData save, string flavor)
        {
            return Stage(Level(save)).Flavors.Contains(flavor);
        }

        /// <summary>새로 열린 미각 단계가 있으면 돌려주고 플래그(tasteN)를 켠다. 없으면 null</summary>
        public TasteStage CheckUnlock(SaveData save)
        {
            int lv = Level(save);
            string key = "taste" + lv;
            if (lv > 0 && !save.Flag(key))
            {
                save.Flags[key] = true;
                return Stage(lv);
            }
            return null;
        }
    }
}

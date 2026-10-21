using System.Text;
using Hanjeopsi.Core.State;
using Hanjeopsi.Core.Systems;
using Hanjeopsi.Core.Util;
using Hanjeopsi.Game.Boot;
using UnityEngine;

namespace Hanjeopsi.Game.DebugTools
{
    /// <summary>
    /// 에디터에서 코어가 제대로 붙었는지 보는 확인용 컴포넌트.
    /// 빈 오브젝트에 붙이고 Play 하면 콘솔과 화면에 시세표를 찍는다.
    /// 시드 12345 의 값은 unity/golden/market.json 과 같아야 한다.
    /// </summary>
    public class CoreCheck : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField] private int day = 4;

        private string _report = "";

        private void Start()
        {
            var data = GameDataLoader.Load();
            var fresh = new Freshness(data);
            var market = new Market(data, fresh, seed);

            var sb = new StringBuilder();
            sb.AppendLine("시드 " + seed + " · " + day + "일차 · " + market.Headline(day));
            foreach (var sp in data.Species)
            {
                sb.AppendLine(sp.Name + "  " + JsMath.Won(market.TodayBasePrice(sp.Id, day)) + "  " + market.ChangePercent(sp.Id, day) + "%");
            }
            var sample = new FishItem(1, "noraemi", 26.4, 1, 35);
            sb.AppendLine("노래미 26.4cm 신선도 35 (" + fresh.StageName(fresh.StageOf(sample.Fresh)) + ") → " + JsMath.Won(market.FishValue(sample, day)));
            _report = sb.ToString();
            Debug.Log(_report);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 36 };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 40, Screen.width - 80, Screen.height - 80), _report, style);
        }
    }
}

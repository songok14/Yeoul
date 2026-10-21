using Hanjeopsi.Core.Data;
using UnityEngine;

namespace Hanjeopsi.Game.Boot
{
    /// <summary>코어 패키지 Resources/HanjeopsiData/*.json 을 읽어 GameData 를 만든다</summary>
    public static class GameDataLoader
    {
        private static GameData _cached;

        public static GameData Load()
        {
            if (_cached != null) return _cached;
            _cached = GameData.Load(name =>
            {
                var asset = Resources.Load<TextAsset>("HanjeopsiData/" + name);
                if (asset == null) throw new System.IO.FileNotFoundException("데이터 파일이 없다: HanjeopsiData/" + name);
                return asset.text;
            });
            return _cached;
        }
    }
}

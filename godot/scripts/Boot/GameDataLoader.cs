using Godot;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.App.Boot
{
    /// <summary>res://data/*.json 을 읽어 GameData 를 만든다. 한 번 읽으면 계속 쓴다</summary>
    public static class GameDataLoader
    {
        private static GameData _cached;

        public static GameData Load()
        {
            if (_cached != null) return _cached;
            _cached = GameData.Load(name =>
            {
                string path = "res://data/" + name + ".json";
                if (!FileAccess.FileExists(path)) throw new System.IO.FileNotFoundException("데이터 파일이 없다: " + path);
                return FileAccess.GetFileAsString(path);
            });
            return _cached;
        }
    }
}

using Godot;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;

namespace Hanjeopsi.App.Boot
{
    /// <summary>세이브 파일 하나 (웹 localStorage 자리). user://save.json</summary>
    public static class SaveStore
    {
        public const string Path = "user://save.json";
        private const string Temp = "user://save.json.tmp";

        public static bool Has()
        {
            return FileAccess.FileExists(Path);
        }

        /// <summary>읽지 못하면 null (버전이 다르거나 깨졌을 때)</summary>
        public static SaveData Load(GameData data)
        {
            if (!Has()) return null;
            return SaveJson.FromJson(FileAccess.GetFileAsString(Path), data);
        }

        /// <summary>임시 파일에 쓰고 바꿔 끼운다. 쓰는 도중 앱이 꺼져도 옛 세이브는 남는다</summary>
        public static void Write(SaveData save)
        {
            using (var f = FileAccess.Open(Temp, FileAccess.ModeFlags.Write))
            {
                if (f == null)
                {
                    GD.PushWarning("저장 실패: " + FileAccess.GetOpenError());
                    return;
                }
                f.StoreString(SaveJson.ToJson(save));
            }
            var err = DirAccess.RenameAbsolute(Temp, Path);
            if (err != Error.Ok) GD.PushWarning("저장 실패: " + err);
        }

        public static void Delete()
        {
            if (Has()) DirAccess.RemoveAbsolute(Path);
        }
    }
}

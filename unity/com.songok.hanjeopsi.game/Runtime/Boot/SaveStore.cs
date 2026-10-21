using System;
using System.IO;
using Hanjeopsi.Core.Data;
using Hanjeopsi.Core.State;
using UnityEngine;

namespace Hanjeopsi.Game.Boot
{
    /// <summary>세이브 파일 하나 (웹 localStorage 자리). persistentDataPath/save.json</summary>
    public static class SaveStore
    {
        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "save.json"); }
        }

        public static bool Has()
        {
            return File.Exists(FilePath);
        }

        /// <summary>읽지 못하면 null (버전이 다르거나 깨졌을 때)</summary>
        public static SaveData Load(GameData data)
        {
            try
            {
                return Has() ? SaveJson.FromJson(File.ReadAllText(FilePath), data) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("불러오기 실패: " + e.Message);
                return null;
            }
        }

        /// <summary>임시 파일에 쓰고 바꿔 끼운다. 쓰는 도중 앱이 꺼져도 옛 세이브는 남는다</summary>
        public static void Write(SaveData save)
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, SaveJson.ToJson(save));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("저장 실패: " + e.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("세이브 지우기 실패: " + e.Message);
            }
        }
    }
}

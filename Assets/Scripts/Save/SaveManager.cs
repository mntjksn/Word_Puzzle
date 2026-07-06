using UnityEngine;

namespace WordPuzzle.Save
{
    // PlayerPrefs 기반 저장 / 로드 (추후 Firebase로 확장 가능)
    public static class SaveManager
    {
        private const string KeySingle = "save_single";
        private const string KeyDaily  = "save_daily";
        private const string KeyMulti  = "save_multi";

        public static SingleSaveData LoadSingle()
        {
            string json = PlayerPrefs.GetString(KeySingle, "");
            return string.IsNullOrEmpty(json) ? new SingleSaveData() : JsonUtility.FromJson<SingleSaveData>(json);
        }

        public static void SaveSingle(SingleSaveData data)
        {
            PlayerPrefs.SetString(KeySingle, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static DailySaveData LoadDaily()
        {
            string json = PlayerPrefs.GetString(KeyDaily, "");
            return string.IsNullOrEmpty(json) ? new DailySaveData() : JsonUtility.FromJson<DailySaveData>(json);
        }

        public static void SaveDaily(DailySaveData data)
        {
            PlayerPrefs.SetString(KeyDaily, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static MultiSaveData LoadMulti()
        {
            string json = PlayerPrefs.GetString(KeyMulti, "");
            return string.IsNullOrEmpty(json) ? new MultiSaveData() : JsonUtility.FromJson<MultiSaveData>(json);
        }

        public static void SaveMulti(MultiSaveData data)
        {
            PlayerPrefs.SetString(KeyMulti, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}

using System;
using System.Collections.Generic;

namespace WordPuzzle.Save
{
    [Serializable]
    public class SingleSaveData
    {
        // 글자 수별 클리어 수 (인덱스 = 글자 수, 0~1은 미사용, 2~6 사용)
        public int[] ClearCountByLength = new int[7];
        public List<int> ClearedWordIds = new List<int>();
        public int TotalHintsUsed;

        public void IncrementClear(int length)
        {
            if (length >= 0 && length < ClearCountByLength.Length)
                ClearCountByLength[length]++;
        }
    }

    [Serializable]
    public class DailySaveData
    {
        public string LastPlayDate;        // yyyyMMdd
        public bool   IsClearedToday;
        public int    TodayAttempts;
        public float  TodayClearTime;      // 초 단위
        public int    StreakDays;
    }

    [Serializable]
    public class MultiSaveData
    {
        public string Nickname;
        public int    WinCount;
        public int    LoseCount;
        public int    PlayCount;
    }
}

using System;
using System.Collections.Generic;

namespace WordPuzzle.Save
{
    [Serializable]
    public class HistoryEntryData
    {
        public string word;
        public bool   isError;
        public int[]  hits;           // TokenHit 값을 int로 저장
        public int    expectedTokenCount;
    }

    [Serializable]
    public class MidGameSave
    {
        public string word;
        public int    wordId;
        public int    wordLength;
        public int    attempts;
        public int[]  revealedPositions;
        public string[] hintMessages;
        // TokenView 상태
        public int[]    fixedPosKeys;
        public string[] fixedPosValues;
        public string[] unfixedPool;
        // 히스토리 로그
        public List<HistoryEntryData> history = new List<HistoryEntryData>();
    }
}

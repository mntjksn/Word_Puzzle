using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Core;
using WordPuzzle.Save;

namespace WordPuzzle.UI
{
    // 입력 기록 리스트 UI (싱글 / 일일 / 멀티 공용)
    public class InputHistoryView : MonoBehaviour
    {
        [SerializeField] private InputHistoryItem itemPrefab;
        [SerializeField] private Transform        listContainer;

        private ScrollRect _scrollRect;
        private readonly List<InputHistoryItem> _items = new List<InputHistoryItem>();

        private void Awake()
        {
            _scrollRect = GetComponentInParent<ScrollRect>();
        }

        public void AddErrorEntry(string inputWord)
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.SetError(_items.Count + 1, inputWord);
            _items.Add(item);
            ScrollToBottom();
        }

        public void AddEntry(string inputWord, JudgeResult result, int expectedTokenCount = 0)
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.Set(_items.Count + 1, inputWord, result, expectedTokenCount);
            _items.Add(item);
            ScrollToBottom();
        }

        public void AddHiddenEntry()
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.SetHidden();
            _items.Add(item);
        }

        public void AddSkippedEntry()
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.SetSkipped();
            _items.Add(item);
        }

        public void RestoreEntries(List<HistoryEntryData> entries)
        {
            Clear();
            foreach (var e in entries)
            {
                var item = Instantiate(itemPrefab, listContainer);
                if (e.isError)
                {
                    item.SetError(_items.Count + 1, e.word);
                }
                else
                {
                    var result = new JudgeResult { Hits = System.Array.ConvertAll(e.hits, h => (TokenHit)h) };
                    item.Set(_items.Count + 1, e.word, result, e.expectedTokenCount);
                }
                _items.Add(item);
            }
        }

        public void Clear()
        {
            foreach (Transform child in listContainer)
                Destroy(child.gameObject);
            _items.Clear();
        }

        private void ScrollToBottom()
        {
            if (_scrollRect != null)
                StartCoroutine(ForceScrollToBottom());
        }

        private IEnumerator ForceScrollToBottom()
        {
            // 레이아웃 재계산 후 스크롤
            yield return new WaitForEndOfFrame();
            _scrollRect.normalizedPosition = new Vector2(0f, 0f);
        }
    }
}

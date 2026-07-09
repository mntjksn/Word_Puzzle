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
        [SerializeField] private bool             compactItems; // 멀티: 패널 폭이 좁아 단어/도트를 두 줄로 표시

        private ScrollRect _scrollRect;
        private readonly List<InputHistoryItem> _items = new List<InputHistoryItem>();

        private void Awake()
        {
            _scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (_scrollRect == null)
                _scrollRect = GetComponentInParent<ScrollRect>();
        }

        private InputHistoryItem SpawnItem()
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.Init(compactItems);
            return item;
        }

        public void AddErrorEntry(string inputWord)
        {
            var item = SpawnItem();
            item.SetError(_items.Count + 1, inputWord);
            _items.Add(item);
            ScrollToBottom();
        }

        public void AddEntry(string inputWord, JudgeResult result, int expectedTokenCount = 0)
        {
            var item = SpawnItem();
            item.Set(_items.Count + 1, inputWord, result, expectedTokenCount);
            _items.Add(item);
            ScrollToBottom();
        }

        public void AddHiddenEntry()
        {
            var item = SpawnItem();
            item.SetHidden();
            _items.Add(item);
            ScrollToBottom();
        }

        public void AddSkippedEntry()
        {
            var item = SpawnItem();
            item.SetSkipped();
            _items.Add(item);
            ScrollToBottom();
        }

        public void RestoreEntries(List<HistoryEntryData> entries)
        {
            Clear();
            foreach (var e in entries)
            {
                var item = SpawnItem();
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
            ScrollToBottom();
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
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

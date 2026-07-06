using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class TokenView : MonoBehaviour
    {
        [SerializeField] private GameObject tokenCellPrefab;
        [SerializeField] private Transform  cellContainer;

        private const float MaxCellSize = 130f;

        private static readonly Color LockedColor = new Color(1f, 0.90f, 0.45f);

        private int                     _totalCount;
        private float                   _cellSize;
        private Dictionary<int, string> _fixedPositions = new Dictionary<int, string>();
        private List<string>            _unfixedPool    = new List<string>();

        public void Build(List<string> displayTokens)
        {
            _totalCount = displayTokens.Count;
            _fixedPositions.Clear();
            _unfixedPool = new List<string>(displayTokens);

            float containerWidth = ((RectTransform)cellContainer).rect.width;
            if (containerWidth < 1f) containerWidth = 1020f;

            // 토큰 수에 따라 갭 축소 (많을수록 좁게)
            float gap = Mathf.Max(3f, 10f - Mathf.Max(0, _totalCount - 8) * 0.5f);
            float totalGap = Mathf.Max(0, _totalCount - 1) * gap;
            _cellSize = Mathf.Min((containerWidth - totalGap) / _totalCount, MaxCellSize);

            var hLayout = cellContainer.GetComponent<HorizontalLayoutGroup>();
            if (hLayout != null) hLayout.spacing = gap;

            RenderCells();
        }

        // answerIndex 위치를 correctJamo(노란색)으로 고정
        // displayJamo: 풀에서 제거할 회전된 자모
        public void LockPosition(int answerIndex, string correctJamo, string displayJamo)
        {
            if (_fixedPositions.ContainsKey(answerIndex)) return;
            int idx = _unfixedPool.IndexOf(displayJamo);
            if (idx >= 0) _unfixedPool.RemoveAt(idx);
            _fixedPositions[answerIndex] = correctJamo;
            RenderCells();
        }

        private void RenderCells()
        {
            foreach (Transform child in cellContainer)
                Destroy(child.gameObject);

            int unfixedIdx = 0;
            for (int i = 0; i < _totalCount; i++)
            {
                bool isFixed = _fixedPositions.TryGetValue(i, out string jamo);
                if (!isFixed)
                    jamo = unfixedIdx < _unfixedPool.Count ? _unfixedPool[unfixedIdx++] : "";

                var cell = Instantiate(tokenCellPrefab, cellContainer);
                cell.GetComponent<RectTransform>().sizeDelta = new Vector2(_cellSize, _cellSize);

                var text = cell.GetComponentInChildren<TextMeshProUGUI>();
                text.text     = jamo;
                text.fontSize = Mathf.Clamp(_cellSize * 0.50f, 14f, 54f);
                if (isFixed) text.color = LockedColor;
            }
        }
    }
}

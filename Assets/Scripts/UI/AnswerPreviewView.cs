using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class AnswerPreviewView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;

        private static readonly Color RevealColor = new Color(1f, 0.90f, 0.45f);
        private static readonly Color SlotBgColor = new Color(0.22f, 0.24f, 0.32f, 0.85f);

        private const float CellGap    = 10f;
        private const float MaxCellSize = 130f;
        private const float MinCellSize =  54f;

        private List<TextMeshProUGUI> _slots = new List<TextMeshProUGUI>();

        public void Build(int count, float overrideCellSize = 0f)
        {
            Clear();

            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.spacing = CellGap;

            float cellSize = overrideCellSize > 0f ? overrideCellSize : CalcCellSize(count);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"slot_{i}", typeof(RectTransform));
                go.transform.SetParent(transform, false);

                var img = go.AddComponent<Image>();
                img.color = SlotBgColor;

                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(cellSize, cellSize);

                var textGO = new GameObject("text", typeof(RectTransform));
                textGO.transform.SetParent(go.transform, false);

                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = "";
                tmp.fontSize  = Mathf.Clamp(cellSize * 0.42f, 22f, 54f);
                tmp.color     = RevealColor;
                tmp.alignment = TextAlignmentOptions.Center;
                if (_font != null) tmp.font = _font;

                var textRT = (RectTransform)textGO.transform;
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                _slots.Add(tmp);
            }
        }

        public void Reveal(int position, string jamo)
        {
            if (position < 0 || position >= _slots.Count) return;
            _slots[position].text = jamo;
        }

        private float CalcCellSize(int count)
        {
            float w = ((RectTransform)transform).rect.width;
            if (w < 1f) w = 1020f;
            float gap = Mathf.Max(0, count - 1) * CellGap;
            return Mathf.Clamp((w - gap) / count, MinCellSize, MaxCellSize);
        }

        private void Clear()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _slots.Clear();
        }
    }
}

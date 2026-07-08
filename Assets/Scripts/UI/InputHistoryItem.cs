using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Core;

namespace WordPuzzle.UI
{
    public class InputHistoryItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI wordText;

        private static readonly Color StrikeColor = new Color(0.25f, 0.75f, 0.35f);
        private static readonly Color BallColor   = new Color(0.97f, 0.60f, 0.10f);
        private static readonly Color OutColor    = new Color(0.82f, 0.28f, 0.28f);
        private static readonly Color GrayColor   = new Color(0.55f, 0.58f, 0.65f);

        private static Sprite _dotSprite;
        private RectTransform _dotContainer;

        private void Awake()
        {
            // 기존 ResultText 비활성화
            var old = transform.Find("ResultText");
            if (old != null) old.gameObject.SetActive(false);

            // ── 아이템 자체를 세로 레이아웃으로 전환 ─────────────────────────
            // wordText는 위쪽 행, DotContainer는 아래 행
            var selfRT  = GetComponent<RectTransform>();
            var selfVLG = gameObject.AddComponent<VerticalLayoutGroup>();
            selfVLG.childAlignment       = TextAnchor.UpperLeft;
            selfVLG.childControlWidth    = true;
            selfVLG.childControlHeight   = false;
            selfVLG.childForceExpandWidth  = true;
            selfVLG.childForceExpandHeight = false;
            selfVLG.spacing              = 2f;
            selfVLG.padding              = new RectOffset(8, 8, 4, 4);

            // wordText에 LayoutElement 추가 (높이 고정)
            if (wordText != null)
            {
                var le = wordText.GetComponent<LayoutElement>();
                if (le == null) le = wordText.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight  = 40f;
                le.flexibleHeight   = 0f;
                wordText.alignment  = TMPro.TextAlignmentOptions.MidlineLeft;
            }

            // DotContainer — 아이템 하단 행
            var go = new GameObject("DotContainer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dotContainer = (RectTransform)go.transform;

            var dotLE = go.AddComponent<LayoutElement>();
            dotLE.preferredHeight = 24f;
            dotLE.flexibleHeight  = 0f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment       = TextAnchor.MiddleRight;
            hlg.spacing              = 4f;
            hlg.padding              = new RectOffset(0, 8, 0, 0);
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            // 프리팹의 고정 높이(LayoutElement.preferredHeight=88)를 제거해서
            // 부모 VLG(Content)가 우리 VLG의 계산값을 읽게 함
            var existingLE = GetComponent<LayoutElement>();
            if (existingLE != null) existingLE.preferredHeight = -1f;

            // ContentSizeFitter (부모 VLG가 childControlHeight=false 인 경우 대비 fallback)
            var csf = gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void SetError(int number, string word)
        {
            wordText.text = $"<color=#6B7A99>#{number}</color>\t{word}";
            ClearDots();

            AddDot(TokenHit.Out, true); // 회색 원 1개

            var labelGO = new GameObject("errLabel", typeof(RectTransform));
            labelGO.transform.SetParent(_dotContainer, false);
            var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text      = "error";
            tmp.fontSize  = 22f;
            tmp.color     = GrayColor;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            var rt = (RectTransform)labelGO.transform;
            rt.sizeDelta  = new Vector2(80f, 28f);
        }

        public void Set(int number, string word, JudgeResult result, int expectedTokenCount = 0)
        {
            wordText.text = $"<color=#6B7A99>#{number}</color>\t{word}";
            ClearDots();
            int hitsLen = result?.Hits?.Length ?? 0;
            for (int i = 0; i < hitsLen; i++)
                AddDot(result.Hits[i], false);
            int remaining = expectedTokenCount - hitsLen;
            for (int i = 0; i < remaining; i++)
                AddDot(TokenHit.Out, true);
        }

        public void SetHidden()
        {
            wordText.text = "???";
            ClearDots();
        }

        public void SetSkipped()
        {
            wordText.text = "-";
            ClearDots();
        }

        private void ClearDots()
        {
            if (_dotContainer == null) return;
            foreach (Transform child in _dotContainer)
                Destroy(child.gameObject);
        }

        private void AddDot(TokenHit hit, bool isGray)
        {
            var go  = new GameObject("dot", typeof(RectTransform));
            go.transform.SetParent(_dotContainer, false);

            var img    = go.AddComponent<Image>();
            img.sprite = GetDotSprite();
            img.color  = isGray                  ? GrayColor
                       : hit == TokenHit.Strike  ? StrikeColor
                       : hit == TokenHit.Ball    ? BallColor
                                                 : OutColor;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(16f, 16f);
        }

        // 런타임에 원형 스프라이트를 한 번만 생성
        private static Sprite GetDotSprite()
        {
            if (_dotSprite != null) return _dotSprite;
            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float cx = N / 2f - 0.5f, cy = cx, r = cx - 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // 안티앨리어싱: 경계에서 부드럽게 페이드
                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            _dotSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            return _dotSprite;
        }
    }
}

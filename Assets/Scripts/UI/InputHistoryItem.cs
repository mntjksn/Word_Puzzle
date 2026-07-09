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
        private bool _initialized;

        // compact=false: 싱글/일일 — 단어(왼쪽)·도트(오른쪽) 한 줄
        // compact=true : 멀티 — 히스토리 패널이 좁아 한 줄에 다 안 들어가므로 단어 위 / 도트 아래 두 줄
        public void Init(bool compact)
        {
            if (_initialized) return;
            _initialized = true;

            // 기존 ResultText 비활성화
            var old = transform.Find("ResultText");
            if (old != null) old.gameObject.SetActive(false);

            if (compact) BuildCompactLayout();
            else         BuildSingleLineLayout();
        }

        // ── 싱글/일일: 한 줄 레이아웃 ─────────────────────────
        // wordText(왼쪽) · DotContainer(오른쪽)를 같은 행에 두고 둘 다 세로 중앙 정렬
        private void BuildSingleLineLayout()
        {
            var selfHLG = gameObject.AddComponent<HorizontalLayoutGroup>();
            selfHLG.childAlignment       = TextAnchor.MiddleLeft;
            selfHLG.childControlWidth    = true;
            selfHLG.childControlHeight   = true;
            selfHLG.childForceExpandWidth  = false;
            selfHLG.childForceExpandHeight = false;
            selfHLG.spacing              = 8f;
            selfHLG.padding              = new RectOffset(16, 16, 28, 28);

            // wordText — 남는 가로 공간을 모두 차지, 도트와 겹치지 않도록 줄바꿈 대신 말줄임 처리
            if (wordText != null)
            {
                var le = wordText.GetComponent<LayoutElement>();
                if (le == null) le = wordText.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 48f;
                le.flexibleWidth   = 1f;
                le.flexibleHeight  = 0f;
                wordText.alignment        = TMPro.TextAlignmentOptions.MidlineLeft;
                wordText.enableWordWrapping = false;
                wordText.overflowMode     = TMPro.TextOverflowModes.Ellipsis;
            }

            // DotContainer — 같은 행의 오른쪽, 필요한 만큼만 너비 차지(도트 개수에 따라 자동 계산)
            var go = new GameObject("DotContainer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dotContainer = (RectTransform)go.transform;

            var dotLE = go.AddComponent<LayoutElement>();
            dotLE.preferredHeight = 48f;
            dotLE.flexibleWidth   = 0f;
            dotLE.flexibleHeight  = 0f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment       = TextAnchor.MiddleRight;
            hlg.spacing              = 4f;
            hlg.padding              = new RectOffset(0, 0, 0, 0);
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            FinalizeLayout();
        }

        // ── 멀티: 두 줄 레이아웃 ─────────────────────────
        // wordText는 위쪽 행, DotContainer는 아래쪽 행 (히스토리 패널 폭이 좁아 한 줄에 안 들어감)
        private void BuildCompactLayout()
        {
            var selfVLG = gameObject.AddComponent<VerticalLayoutGroup>();
            selfVLG.childAlignment       = TextAnchor.MiddleLeft;
            selfVLG.childControlWidth    = true;
            selfVLG.childControlHeight   = true;
            selfVLG.childForceExpandWidth  = true;
            selfVLG.childForceExpandHeight = false;
            selfVLG.spacing              = 8f;
            selfVLG.padding              = new RectOffset(10, 10, 8, 14);

            if (wordText != null)
            {
                var le = wordText.GetComponent<LayoutElement>();
                if (le == null) le = wordText.gameObject.AddComponent<LayoutElement>();
                // preferredHeight가 폰트 크기(34)보다 작으면 TMP가 한 줄도 못 그려서 텍스트가 통째로 사라짐
                le.preferredHeight = 44f;
                le.flexibleHeight  = 0f;
                wordText.alignment        = TMPro.TextAlignmentOptions.MidlineLeft;
                wordText.enableWordWrapping = false;
                wordText.overflowMode     = TMPro.TextOverflowModes.Ellipsis;
            }

            var go = new GameObject("DotContainer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dotContainer = (RectTransform)go.transform;

            var dotLE = go.AddComponent<LayoutElement>();
            dotLE.preferredHeight = 24f;
            dotLE.flexibleHeight  = 0f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.spacing              = 4f;
            hlg.padding              = new RectOffset(0, 0, 0, 0);
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            FinalizeLayout();
        }

        private void FinalizeLayout()
        {
            // 프리팹의 고정 높이(LayoutElement.preferredHeight=88)를 제거해서
            // 부모 VLG(Content)가 우리 레이아웃의 계산값을 읽게 함
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
            wordText.text      = "???";
            wordText.alignment = TMPro.TextAlignmentOptions.Center;
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
            rt.sizeDelta = new Vector2(20f, 20f);
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

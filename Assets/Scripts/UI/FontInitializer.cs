using TMPro;
using UnityEngine;

namespace WordPuzzle.UI
{
    // 씬 내 모든 TMP 컴포넌트에 Maplestory 폰트를 런타임에 적용
    // GO 이름에 "Bold" 또는 GO 태그 "FontBold" → boldFont
    // 나머지 → lightFont
    public class FontInitializer : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset boldFont;
        [SerializeField] private TMP_FontAsset lightFont;

        private void Awake()
        {
            if (boldFont == null || lightFont == null) return;
            // 비활성 포함 씬 전체 TMP 탐색
            var all = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tmp in all)
            {
                string n = tmp.gameObject.name;
                bool useBold = n.Contains("Bold") || n.Contains("Title")
                            || n.Contains("NumText") || n.Contains("Token")
                            || n.Contains("Word") || n.Contains("Clear")
                            || n.Contains("Difficulty") || n.Contains("Submit")
                            || tmp.fontStyle == FontStyles.Bold;
                tmp.font = useBold ? boldFont : lightFont;
            }
        }
    }
}

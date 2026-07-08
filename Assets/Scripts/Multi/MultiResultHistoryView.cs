#if PHOTON_UNITY_NETWORKING
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordPuzzle.Core;
using WordPuzzle.UI;

namespace WordPuzzle.Multi
{
    // 내 기록(왼쪽)과 상대 기록(오른쪽) UI 표시 — 히든/스킵 처리 포함
    public class MultiResultHistoryView : MonoBehaviour
    {
        [Header("내 기록 뷰")]
        [SerializeField] private InputHistoryView myHistoryView;

        [Header("상대 기록 뷰")]
        [SerializeField] private InputHistoryView opponentHistoryView;

        // MultiGameController에서 로컬 계산된 result를 받아 표시
        public void OnSubmitReceived(SubmitData data, bool isLocal, JudgeResult result, int tokenCount = 0)
        {
            var view = isLocal ? myHistoryView : opponentHistoryView;

            // 상대가 히든 아이템 사용 시 내용 숨김
            if (!isLocal && data.UseHidden)
            {
                view.AddHiddenEntry();
                return;
            }

            if (data.InputWord == null)
            {
                view.AddSkippedEntry();
                return;
            }

            view.AddEntry(data.InputWord, result, tokenCount);
        }

        // 타임아웃 스킵 엔트리 추가
        public void AddSkipped(bool isLocal)
        {
            var view = isLocal ? myHistoryView : opponentHistoryView;
            view.AddSkippedEntry();
        }

        public void Clear()
        {
            if (myHistoryView)       myHistoryView.Clear();
            if (opponentHistoryView) opponentHistoryView.Clear();
        }
    }

    // 게임 종료 결과 화면
    public class MultiResultScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI       emojiText;
        [SerializeField] private TextMeshProUGUI       resultText;
        [SerializeField] private TextMeshProUGUI       subtitleText;
        [SerializeField] private TextMeshProUGUI       winnerWordText;
        [SerializeField] private TextMeshProUGUI       statsText;
        [SerializeField] private GameObject            panel;
        [SerializeField] private UnityEngine.UI.Button leaveButton;
        [SerializeField] private UnityEngine.UI.Image  topAccentImage;

        private static readonly Color ColWinText    = new Color(1.00f, 0.88f, 0.20f, 1.00f);
        private static readonly Color ColLossText   = new Color(1.00f, 0.45f, 0.45f, 1.00f);
        private static readonly Color ColWinAccent  = new Color(0.18f, 0.72f, 0.64f, 1.00f);
        private static readonly Color ColLossAccent = new Color(0.72f, 0.22f, 0.22f, 1.00f);

        private void Start()
        {
            if (leaveButton == null)
                leaveButton = GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (leaveButton) leaveButton.onClick.AddListener(OnLeave);

            if (emojiText == null)
                foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.name == "EmojiText") { emojiText = t; break; }

            if (topAccentImage == null)
                foreach (var img in GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    if (img.name == "TopAccent") { topAccentImage = img; break; }
        }

        public void Show(bool isWin, Dictionary<int, MultiPlayerState> players,
                         string correctWord, int turnCount, int wins, int losses)
        {
            panel.SetActive(true);

            if (emojiText)      emojiText.text   = isWin ? "★★★" : "●";
            if (resultText)
            {
                resultText.text  = isWin ? "승리!" : "패배";
                resultText.color = isWin ? ColWinText : ColLossText;
            }
            if (subtitleText)
                subtitleText.text = isWin
                    ? turnCount + "턴 만에 정답!"
                    : "상대방이 먼저 맞췄어요";
            if (winnerWordText)
                winnerWordText.text = string.IsNullOrEmpty(correctWord)
                    ? "" : "정답: " + correctWord;
            if (statsText)
                statsText.text = "전적  " + wins + "승  " + losses + "패";
            if (topAccentImage)
                topAccentImage.color = isWin ? ColWinAccent : ColLossAccent;
        }

        public void OnLeave()
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("MultiMenu");
        }
    }
}
#endif

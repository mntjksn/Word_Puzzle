#if PHOTON_UNITY_NETWORKING
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using WordPuzzle.UI;

namespace WordPuzzle.Multi
{
    // 내 기록과 상대 기록 UI 표시 (히든 처리 포함)
    public class MultiResultHistoryView : MonoBehaviour
    {
        [Header("내 기록")]
        [SerializeField] private InputHistoryView myHistoryView;

        [Header("상대 기록")]
        [SerializeField] private InputHistoryView opponentHistoryView;

        public void OnSubmitReceived(SubmitData data, bool isLocalPlayer)
        {
            var view = isLocalPlayer ? myHistoryView : opponentHistoryView;

            // 상대가 히든 사용 시 숨김 표시
            if (!isLocalPlayer && data.UseHidden)
            {
                view.AddHiddenEntry();
                return;
            }

            if (data.InputWord == null)
            {
                view.AddSkippedEntry();
                return;
            }

            var result = new Core.JudgeResult
            {
                Strike    = data.Strike,
                Ball      = data.Ball,
                OutCount  = data.OutCount,
                IsCorrect = data.IsCorrect,
            };
            view.AddEntry(data.InputWord, result);
        }

        public void Clear()
        {
            myHistoryView.Clear();
            opponentHistoryView.Clear();
        }
    }

    // 게임 종료 결과 화면
    public class MultiResultScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private GameObject      panel;

        public void Show(string result, Dictionary<int, MultiPlayerState> players)
        {
            panel.SetActive(true);
            resultText.text = result;
        }

        public void OnPlayAgain()
            => panel.SetActive(false);

        public void OnLeave()
            => PhotonNetwork.LeaveRoom();
    }
}
#endif

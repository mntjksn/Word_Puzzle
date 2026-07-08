#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace WordPuzzle.Multi
{
    // Photon RaiseEvent 이벤트 코드 정의
    public static class MultiNetworkEvents
    {
        public const byte SubmitTurn         = 1;
        public const byte TurnEnded          = 2;
        public const byte GameOver           = 3;
        public const byte PlayerReadyChanged = 4;
        public const byte StartGame          = 5;
        public const byte NextTurn           = 6;
        public const byte CountdownStart     = 7;
        public const byte CountdownCancel    = 8;
        public const byte TurnOrderSet       = 9;
        public const byte CardTapped         = 10; // 선공/후공 카드 탭 동기화

        // 모든 플레이어 대상
        public static readonly RaiseEventOptions All = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        // 나를 제외한 다른 플레이어 대상 (히든 아이템 상대 전송용)
        public static readonly RaiseEventOptions Others = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };

        public static readonly SendOptions Reliable = SendOptions.SendReliable;
    }
}
#endif

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

        // 모든 플레이어 대상, 신뢰성 있음
        public static readonly RaiseEventOptions All = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        public static readonly SendOptions Reliable = SendOptions.SendReliable;
    }
}
#endif

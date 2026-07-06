namespace WordPuzzle.Multi
{
    // 멀티 플레이어 한 명의 상태
    public class MultiPlayerState
    {
        public int        ActorNumber;
        public string     Nickname;
        public bool       IsReady;
        public bool       HasSubmitted;
        public bool       HiddenUsed;   // 게임당 1회 제한
        public SubmitData LastSubmitData;

        public MultiPlayerState(int actorNumber, string nickname)
        {
            ActorNumber = actorNumber;
            Nickname    = nickname;
        }

        public void ResetTurn()
        {
            HasSubmitted  = false;
            LastSubmitData = null;
        }
    }
}

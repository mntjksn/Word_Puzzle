namespace WordPuzzle.Multi
{
    // 턴 제출 데이터 (서버 검증 확장 가능하도록 분리)
    public class SubmitData
    {
        public int    ActorNumber;
        public int    TurnIndex;
        public string InputWord;   // 히든 사용 시 null
        public int    Strike;      // 히든 사용 시 -1
        public int    Ball;        // 히든 사용 시 -1
        public int    OutCount;    // 히든 사용 시 -1
        public bool   IsCorrect;
        public double SubmitTime;  // PhotonNetwork.Time 기준
        public bool   UseHidden;

        // 히든 적용 시 상대에게 전송할 복사본 생성
        public SubmitData ToHiddenView()
        {
            return new SubmitData
            {
                ActorNumber = ActorNumber,
                TurnIndex   = TurnIndex,
                InputWord   = null,
                Strike      = -1,
                Ball        = -1,
                OutCount    = -1,
                IsCorrect   = IsCorrect,   // 승패 판정용으로는 전달
                SubmitTime  = SubmitTime,
                UseHidden   = true,
            };
        }

        // Photon RaiseEvent 전송용 object 배열로 직렬화
        public object[] Serialize()
            => new object[] { ActorNumber, TurnIndex, InputWord, Strike, Ball, OutCount, IsCorrect, SubmitTime, UseHidden };

        public static SubmitData Deserialize(object[] data)
            => new SubmitData
            {
                ActorNumber = (int)data[0],
                TurnIndex   = (int)data[1],
                InputWord   = data[2] as string,
                Strike      = (int)data[3],
                Ball        = (int)data[4],
                OutCount    = (int)data[5],
                IsCorrect   = (bool)data[6],
                SubmitTime  = (double)data[7],
                UseHidden   = (bool)data[8],
            };
    }
}

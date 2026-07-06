namespace WordPuzzle.Core
{
    public enum TokenHit { Strike, Ball, Out }

    public class JudgeResult
    {
        public int Strike;
        public int Ball;
        public int OutCount;
        public bool IsCorrect;
        public TokenHit[] Hits; // 입력 토큰 인덱스별 S/B/O
    }
}

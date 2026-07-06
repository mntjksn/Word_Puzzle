using System.Collections.Generic;

namespace WordPuzzle.Core
{
    // 자모 단위 숫자야구 판정
    public static class BaseballJudge
    {
        public static JudgeResult Judge(List<string> answerTokens, List<string> inputTokens)
        {
            int answerLen = answerTokens.Count;
            int inputLen  = inputTokens.Count;

            bool[] answerUsed  = new bool[answerLen];
            bool[] inputUsed   = new bool[inputLen];
            bool[] inputStrike = new bool[inputLen];

            int strike = 0;

            // 1단계: 같은 위치 Strike 처리
            int minLen = answerLen < inputLen ? answerLen : inputLen;
            for (int i = 0; i < minLen; i++)
            {
                if (answerTokens[i] == inputTokens[i])
                {
                    strike++;
                    answerUsed[i]  = true;
                    inputUsed[i]   = true;
                    inputStrike[i] = true;
                }
            }

            int ball = 0;

            // 2단계: Ball 처리 (Strike 제외한 나머지에서 위치 다른 일치)
            for (int i = 0; i < inputLen; i++)
            {
                if (inputUsed[i]) continue;
                for (int j = 0; j < answerLen; j++)
                {
                    if (!answerUsed[j] && answerTokens[j] == inputTokens[i])
                    {
                        ball++;
                        answerUsed[j] = true;
                        inputUsed[i]  = true;
                        break;
                    }
                }
            }

            // 3단계: Out (매칭되지 않은 입력 토큰 수)
            int outCount = 0;
            for (int i = 0; i < inputLen; i++)
            {
                if (!inputUsed[i]) outCount++;
            }

            // 토큰별 결과 배열
            var hits = new TokenHit[inputLen];
            for (int i = 0; i < inputLen; i++)
            {
                if      (inputStrike[i]) hits[i] = TokenHit.Strike;
                else if (inputUsed[i])   hits[i] = TokenHit.Ball;
                else                     hits[i] = TokenHit.Out;
            }

            // 정답: 토큰 수가 같고 전부 Strike
            bool isCorrect = inputLen == answerLen && strike == answerLen;

            return new JudgeResult
            {
                Strike    = strike,
                Ball      = ball,
                OutCount  = outCount,
                IsCorrect = isCorrect,
                Hits      = hits,
            };
        }
    }
}

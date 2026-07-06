using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordPuzzle.Single
{
    public class HintController : MonoBehaviour
    {
        private List<string>        _answerTokens;
        private HashSet<int>        _usedIndices    = new HashSet<int>();
        private HashSet<int>        _revealedByStrike;
        private Action<int, string> _onReveal;
        private int                 _hintsUsed;

        public int HintsUsed => _hintsUsed;

        public void Setup(List<string> answerTokens, Action<int, string> onReveal, HashSet<int> revealedPositions, int hintsAlreadyUsed = 0)
        {
            _answerTokens     = answerTokens;
            _onReveal         = onReveal;
            _revealedByStrike = revealedPositions;
            _usedIndices.Clear();
            _hintsUsed = hintsAlreadyUsed;
        }

        // 사용 가능한 위치가 있으면 공개하고 true 반환
        public bool TryReveal()
        {
            var available = new List<int>();
            for (int i = 0; i < _answerTokens.Count; i++)
                if (!_usedIndices.Contains(i) && !(_revealedByStrike?.Contains(i) ?? false))
                    available.Add(i);

            if (available.Count == 0) return false;

            int idx = available[UnityEngine.Random.Range(0, available.Count)];
            _usedIndices.Add(idx);
            _hintsUsed++;
            _onReveal?.Invoke(idx + 1, _answerTokens[idx]);
            return true;
        }
    }
}

using System.Collections.Generic;

namespace WordPuzzle.Core
{
    // 한글 단어를 자모 단위로 분해하고 보기용 자모를 생성
    public static class JamoConverter
    {
        // 초성 테이블 (19개)
        private static readonly string[] Initials =
        {
            "ㄱ","ㄲ","ㄴ","ㄷ","ㄸ","ㄹ","ㅁ","ㅂ","ㅃ",
            "ㅅ","ㅆ","ㅇ","ㅈ","ㅉ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ"
        };

        // 중성 테이블 (21개)
        private static readonly string[] Middles =
        {
            "ㅏ","ㅐ","ㅑ","ㅒ","ㅓ","ㅔ","ㅕ","ㅖ","ㅗ","ㅘ",
            "ㅙ","ㅚ","ㅛ","ㅜ","ㅝ","ㅞ","ㅟ","ㅠ","ㅡ","ㅢ","ㅣ"
        };

        // 종성 테이블 (28개, index 0 = 없음)
        private static readonly string[] Finals =
        {
            "","ㄱ","ㄲ","ㄳ","ㄴ","ㄵ","ㄶ","ㄷ","ㄹ","ㄺ",
            "ㄻ","ㄼ","ㄽ","ㄾ","ㄿ","ㅀ","ㅁ","ㅂ","ㅄ","ㅅ",
            "ㅆ","ㅇ","ㅈ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ"
        };

        // 쌍자음 분해 규칙
        private static readonly Dictionary<string, string[]> DoubleConsonants = new Dictionary<string, string[]>
        {
            { "ㄲ", new[] { "ㄱ", "ㄱ" } },
            { "ㄸ", new[] { "ㄷ", "ㄷ" } },
            { "ㅃ", new[] { "ㅂ", "ㅂ" } },
            { "ㅆ", new[] { "ㅅ", "ㅅ" } },
            { "ㅉ", new[] { "ㅈ", "ㅈ" } },
        };

        // 겹받침 분해 규칙
        private static readonly Dictionary<string, string[]> CompoundFinals = new Dictionary<string, string[]>
        {
            { "ㄳ", new[] { "ㄱ", "ㅅ" } },
            { "ㄵ", new[] { "ㄴ", "ㅈ" } },
            { "ㄶ", new[] { "ㄴ", "ㅎ" } },
            { "ㄺ", new[] { "ㄹ", "ㄱ" } },
            { "ㄻ", new[] { "ㄹ", "ㅁ" } },
            { "ㄼ", new[] { "ㄹ", "ㅂ" } },
            { "ㄽ", new[] { "ㄹ", "ㅅ" } },
            { "ㄾ", new[] { "ㄹ", "ㅌ" } },
            { "ㄿ", new[] { "ㄹ", "ㅍ" } },
            { "ㅀ", new[] { "ㄹ", "ㅎ" } },
            { "ㅄ", new[] { "ㅂ", "ㅅ" } },
        };

        // 복합모음 분해 규칙 (원본 기준, 회전 미적용)
        private static readonly Dictionary<string, string[]> CompoundVowels = new Dictionary<string, string[]>
        {
            { "ㅐ", new[] { "ㅏ", "ㅣ" } },
            { "ㅔ", new[] { "ㅓ", "ㅣ" } },
            { "ㅒ", new[] { "ㅑ", "ㅣ" } },
            { "ㅖ", new[] { "ㅕ", "ㅣ" } },
            { "ㅘ", new[] { "ㅗ", "ㅏ" } },
            { "ㅙ", new[] { "ㅗ", "ㅏ", "ㅣ" } },
            { "ㅚ", new[] { "ㅗ", "ㅣ" } },
            { "ㅝ", new[] { "ㅜ", "ㅓ" } },
            { "ㅞ", new[] { "ㅜ", "ㅓ", "ㅣ" } },
            { "ㅟ", new[] { "ㅜ", "ㅣ" } },
            { "ㅢ", new[] { "ㅡ", "ㅣ" } },
        };

        // 회전 치환 규칙
        // ㅗ/ㅜ/ㅓ/ㅏ → ㅏ  |  ㅛ/ㅠ/ㅕ/ㅑ → ㅑ
        private static readonly Dictionary<string, string> RotationMap = new Dictionary<string, string>
        {
            { "ㄴ", "ㄱ" },
            { "ㅗ", "ㅏ" },
            { "ㅜ", "ㅏ" },
            { "ㅓ", "ㅏ" },
            { "ㅡ", "ㅣ" },
            { "ㅛ", "ㅑ" },
            { "ㅠ", "ㅑ" },
            { "ㅕ", "ㅑ" },
        };

        // 판정용 자모 리스트 생성
        public static List<string> GetAnswerTokens(string word)
        {
            var tokens = new List<string>();
            foreach (char c in word)
                DecomposeChar(c, tokens, applyRotation: false);
            return tokens;
        }

        // 단일 자모를 보기용(회전 치환)으로 변환
        public static string ToDisplayJamo(string jamo)
            => RotationMap.TryGetValue(jamo, out var r) ? r : jamo;

        // 보기용 자모 리스트 생성 (회전 치환 적용)
        public static List<string> GetDisplayTokens(string word)
        {
            var tokens = new List<string>();
            foreach (char c in word)
                DecomposeChar(c, tokens, applyRotation: true);
            return tokens;
        }

        private static void DecomposeChar(char c, List<string> tokens, bool applyRotation)
        {
            if (c < '가' || c > '힣')
            {
                tokens.Add(c.ToString());
                return;
            }

            int syllable   = c - '가';
            int initialIdx = syllable / (21 * 28);
            int middleIdx  = (syllable % (21 * 28)) / 28;
            int finalIdx   = syllable % 28;

            AddConsonant(Initials[initialIdx], tokens, applyRotation, isInitial: true);
            AddVowel(Middles[middleIdx], tokens, applyRotation);

            if (finalIdx > 0)
                AddConsonant(Finals[finalIdx], tokens, applyRotation, isInitial: false);
        }

        private static void AddConsonant(string jamo, List<string> tokens, bool applyRotation, bool isInitial)
        {
            // 쌍자음 분해
            if (DoubleConsonants.TryGetValue(jamo, out var doubleParts))
            {
                foreach (var p in doubleParts)
                    tokens.Add(applyRotation ? Rotate(p) : p);
                return;
            }

            // 겹받침 분해
            if (!isInitial && CompoundFinals.TryGetValue(jamo, out var finalParts))
            {
                foreach (var p in finalParts)
                    tokens.Add(applyRotation ? Rotate(p) : p);
                return;
            }

            tokens.Add(applyRotation ? Rotate(jamo) : jamo);
        }

        private static void AddVowel(string jamo, List<string> tokens, bool applyRotation)
        {
            // 복합모음 분해
            if (CompoundVowels.TryGetValue(jamo, out var parts))
            {
                foreach (var p in parts)
                    tokens.Add(applyRotation ? Rotate(p) : p);
                return;
            }

            tokens.Add(applyRotation ? Rotate(jamo) : jamo);
        }

        private static string Rotate(string jamo)
            => RotationMap.TryGetValue(jamo, out var r) ? r : jamo;
    }
}

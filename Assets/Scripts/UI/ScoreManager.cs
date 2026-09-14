using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 53: tracks a correct-answer streak and applies an escalating score
    /// multiplier (3 in a row = 1.5x, etc.), firing an event QuizController's
    /// confetti hook can subscribe to whenever the multiplier increases.
    /// </summary>
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int CurrentStreak { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;

        public event System.Action<float> OnMultiplierIncreased;

        /// <summary>Pure function so the multiplier curve is unit-testable.</summary>
        public static float ComputeMultiplier(int streak)
        {
            if (streak >= 9) return 2.5f;
            if (streak >= 6) return 2f;
            if (streak >= 3) return 1.5f;
            return 1f;
        }

        public int RegisterCorrectAnswer(int baseScore)
        {
            CurrentStreak++;
            float previousMultiplier = CurrentMultiplier;
            CurrentMultiplier = ComputeMultiplier(CurrentStreak);

            if (CurrentMultiplier > previousMultiplier)
            {
                OnMultiplierIncreased?.Invoke(CurrentMultiplier);
            }

            int awarded = Mathf.RoundToInt(baseScore * CurrentMultiplier);
            Score += awarded;
            return awarded;
        }

        public void RegisterWrongAnswer(int penalty)
        {
            CurrentStreak = 0;
            CurrentMultiplier = 1f;
            Score = Mathf.Max(0, Score - penalty);
        }
    }
}

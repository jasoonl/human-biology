using HumanBodyExplorer.UI;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class ScoreManagerTests
    {
        [Test]
        public void RegisterCorrectAnswer_BuildsStreakMultiplier()
        {
            var score = new ScoreManager();

            score.RegisterCorrectAnswer(10);
            score.RegisterCorrectAnswer(10);
            Assert.AreEqual(1f, score.CurrentMultiplier);

            score.RegisterCorrectAnswer(10);
            Assert.AreEqual(1.5f, score.CurrentMultiplier);
        }

        [Test]
        public void RegisterWrongAnswer_ResetsStreakAndMultiplier()
        {
            var score = new ScoreManager();
            score.RegisterCorrectAnswer(10);
            score.RegisterCorrectAnswer(10);
            score.RegisterCorrectAnswer(10);

            score.RegisterWrongAnswer(10);

            Assert.AreEqual(0, score.CurrentStreak);
            Assert.AreEqual(1f, score.CurrentMultiplier);
        }

        [Test]
        public void OnMultiplierIncreased_FiresOnlyWhenMultiplierRises()
        {
            var score = new ScoreManager();
            int fireCount = 0;
            score.OnMultiplierIncreased += _ => fireCount++;

            for (int i = 0; i < 5; i++) score.RegisterCorrectAnswer(10);

            // Multiplier rises at streak 3 only within this range (next rise is at 6).
            Assert.AreEqual(1, fireCount);
        }

        [TestCase(0, 1f)]
        [TestCase(2, 1f)]
        [TestCase(3, 1.5f)]
        [TestCase(6, 2f)]
        [TestCase(9, 2.5f)]
        public void ComputeMultiplier_MatchesExpectedTiers(int streak, float expected)
        {
            Assert.AreEqual(expected, ScoreManager.ComputeMultiplier(streak));
        }
    }
}

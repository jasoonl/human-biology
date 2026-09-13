using System;
using HumanBodyExplorer.Data;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class StudyTrackerTests
    {
        [Test]
        public void LogQuizResult_PerfectScoreRepeatedly_IncreasesInterval()
        {
            var db = new FakeDatabaseManager();
            var tracker = new StudyTracker(db);

            tracker.LogQuizResult("NODE_A", 5);
            var afterFirst = db.GetProgress("NODE_A");
            Assert.AreEqual(1f, afterFirst.Value.ReviewInterval);

            tracker.LogQuizResult("NODE_A", 5);
            var afterSecond = db.GetProgress("NODE_A");
            Assert.AreEqual(6f, afterSecond.Value.ReviewInterval);

            tracker.LogQuizResult("NODE_A", 5);
            var afterThird = db.GetProgress("NODE_A");
            Assert.Greater(afterThird.Value.ReviewInterval, afterSecond.Value.ReviewInterval);
        }

        [Test]
        public void LogQuizResult_FailingScore_ResetsStreakAndInterval()
        {
            var db = new FakeDatabaseManager();
            var tracker = new StudyTracker(db);

            tracker.LogQuizResult("NODE_B", 5);
            tracker.LogQuizResult("NODE_B", 5);
            tracker.LogQuizResult("NODE_B", 1); // fail

            var progress = db.GetProgress("NODE_B");
            Assert.AreEqual(0, progress.Value.CorrectStrikes);
            Assert.AreEqual(1f, progress.Value.ReviewInterval);
        }

        [Test]
        public void LogQuizResult_OutOfRangeQuality_Throws()
        {
            var db = new FakeDatabaseManager();
            var tracker = new StudyTracker(db);

            Assert.Throws<ArgumentOutOfRangeException>(() => tracker.LogQuizResult("NODE_C", 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => tracker.LogQuizResult("NODE_C", -1));
        }

        [Test]
        public void GetDueNodes_OverdueNode_IsReturned()
        {
            var db = new FakeDatabaseManager();
            db.UpsertProgress(new UserProgressRow
            {
                NodeID = "NODE_D",
                CorrectStrikes = 2,
                LastReviewed = DateTime.UtcNow.AddDays(-10).ToString("O"),
                ReviewInterval = 6f,
                EaseFactor = 2.5f
            });

            var tracker = new StudyTracker(db);
            var due = tracker.GetDueNodes(DateTime.UtcNow);

            CollectionAssert.Contains(due, "NODE_D");
        }
    }
}

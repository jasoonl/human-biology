using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HumanBodyExplorer.Data
{
    public interface IStudyTracker
    {
        void LogQuizResult(string nodeId, int qualityScore0To5);
        List<string> GetDueNodes(DateTime asOf);
    }

    /// <summary>
    /// SuperMemo-2 spaced repetition scheduling on top of DatabaseManager's UserProgress table.
    /// </summary>
    public class StudyTracker : IStudyTracker
    {
        private const float MinEaseFactor = 1.3f;
        private readonly IDatabaseManager _db;

        public StudyTracker(IDatabaseManager db)
        {
            _db = db;
        }

        public void LogQuizResult(string nodeId, int qualityScore0To5)
        {
            if (qualityScore0To5 < 0 || qualityScore0To5 > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(qualityScore0To5), "Quality score must be 0-5.");
            }

            var existing = _db.GetProgress(nodeId) ?? new UserProgressRow
            {
                NodeID = nodeId,
                CorrectStrikes = 0,
                ReviewInterval = 1f,
                EaseFactor = 2.5f
            };

            float oldEase = existing.EaseFactor <= 0 ? 2.5f : existing.EaseFactor;
            float quality = qualityScore0To5;

            float newEase = oldEase + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));
            newEase = Mathf.Max(MinEaseFactor, newEase);

            float newInterval;
            int newStrikes;

            if (qualityScore0To5 < 3)
            {
                newStrikes = 0;
                newInterval = 1f;
            }
            else
            {
                newStrikes = existing.CorrectStrikes + 1;
                newInterval = newStrikes switch
                {
                    1 => 1f,
                    2 => 6f,
                    _ => Mathf.Round(existing.ReviewInterval * newEase)
                };
            }

            var updated = new UserProgressRow
            {
                NodeID = nodeId,
                CorrectStrikes = newStrikes,
                LastReviewed = DateTime.UtcNow.ToString("O"),
                ReviewInterval = newInterval,
                EaseFactor = newEase
            };

            _db.UpsertProgress(updated);
        }

        public List<string> GetDueNodes(DateTime asOf)
        {
            var due = new List<string>();
            foreach (var row in _db.GetAllProgress())
            {
                if (!DateTime.TryParse(row.LastReviewed, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var lastReviewed))
                {
                    due.Add(row.NodeID);
                    continue;
                }

                var dueDate = lastReviewed.AddDays(row.ReviewInterval);
                if (dueDate <= asOf)
                {
                    due.Add(row.NodeID);
                }
            }

            return due;
        }
    }
}

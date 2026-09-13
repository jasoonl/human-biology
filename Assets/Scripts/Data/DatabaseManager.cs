using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HumanBodyExplorer.Data
{
    [Serializable]
    public struct UserProgressRow
    {
        public string NodeID;
        public int CorrectStrikes;
        public string LastReviewed; // ISO-8601
        public float ReviewInterval;
        public float EaseFactor;
    }

    public interface IDatabaseManager
    {
        void Initialize();
        void Shutdown();
        UserProgressRow? GetProgress(string nodeId);
        void UpsertProgress(UserProgressRow row);
        List<UserProgressRow> GetAllProgress();
    }

    /// <summary>
    /// Local SQLite-backed store for mutable user progress data (userdata.db).
    /// Uses a thin P/Invoke layer (SqliteNative) against the OS-provided libsqlite3,
    /// avoiding a dependency on the deprecated Mono.Data.Sqlite package.
    /// </summary>
    public class DatabaseManager : IDatabaseManager
    {
        private IntPtr _db = IntPtr.Zero;
        private string _dbPath;

        public void Initialize()
        {
            _dbPath = Path.Combine(Application.persistentDataPath, "userdata.db");

            int rc = SqliteNative.Open(_dbPath, out _db);
            if (rc != SqliteNative.SQLITE_OK)
            {
                Debug.LogError($"[DatabaseManager] Failed to open {_dbPath}: {SqliteNative.ErrMsg(_db)}");
                return;
            }

            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS UserProgress (
                    NodeID VARCHAR PRIMARY KEY,
                    CorrectStrikes INT,
                    LastReviewed DATETIME,
                    ReviewInterval FLOAT,
                    EaseFactor FLOAT
                );";

            Execute(createTableSql);
            Debug.Log($"[DatabaseManager] Initialized at {_dbPath}");
        }

        public void Shutdown()
        {
            if (_db != IntPtr.Zero)
            {
                SqliteNative.Close(_db);
                _db = IntPtr.Zero;
            }
        }

        private void Execute(string sql)
        {
            if (!Prepare(sql, out IntPtr stmt)) return;
            int rc = SqliteNative.Step(stmt);
            if (rc != SqliteNative.SQLITE_DONE && rc != SqliteNative.SQLITE_ROW)
            {
                Debug.LogError($"[DatabaseManager] Step failed: {SqliteNative.ErrMsg(_db)}");
            }
            SqliteNative.Finalize(stmt);
        }

        private bool Prepare(string sql, out IntPtr stmt)
        {
            byte[] sqlBytes = SqliteNative.ToUtf8(sql);
            int rc = SqliteNative.Prepare(_db, sqlBytes, sqlBytes.Length, out stmt, IntPtr.Zero);
            if (rc != SqliteNative.SQLITE_OK)
            {
                Debug.LogError($"[DatabaseManager] Prepare failed for '{sql}': {SqliteNative.ErrMsg(_db)}");
                return false;
            }
            return true;
        }

        public UserProgressRow? GetProgress(string nodeId)
        {
            const string sql = "SELECT NodeID, CorrectStrikes, LastReviewed, ReviewInterval, EaseFactor FROM UserProgress WHERE NodeID = ?;";
            if (!Prepare(sql, out IntPtr stmt)) return null;

            byte[] idBytes = SqliteNative.ToUtf8(nodeId);
            SqliteNative.BindText(stmt, 1, idBytes, idBytes.Length - 1, SqliteNative.SQLITE_TRANSIENT);

            UserProgressRow? result = null;
            if (SqliteNative.Step(stmt) == SqliteNative.SQLITE_ROW)
            {
                result = new UserProgressRow
                {
                    NodeID = SqliteNative.ColumnText(stmt, 0),
                    CorrectStrikes = SqliteNative.ColumnInt(stmt, 1),
                    LastReviewed = SqliteNative.ColumnText(stmt, 2),
                    ReviewInterval = (float)SqliteNative.ColumnDouble(stmt, 3),
                    EaseFactor = (float)SqliteNative.ColumnDouble(stmt, 4)
                };
            }

            SqliteNative.Finalize(stmt);
            return result;
        }

        public void UpsertProgress(UserProgressRow row)
        {
            const string sql = @"
                INSERT INTO UserProgress (NodeID, CorrectStrikes, LastReviewed, ReviewInterval, EaseFactor)
                VALUES (?, ?, ?, ?, ?)
                ON CONFLICT(NodeID) DO UPDATE SET
                    CorrectStrikes = excluded.CorrectStrikes,
                    LastReviewed = excluded.LastReviewed,
                    ReviewInterval = excluded.ReviewInterval,
                    EaseFactor = excluded.EaseFactor;";

            if (!Prepare(sql, out IntPtr stmt)) return;

            byte[] idBytes = SqliteNative.ToUtf8(row.NodeID);
            byte[] dateBytes = SqliteNative.ToUtf8(row.LastReviewed);

            SqliteNative.BindText(stmt, 1, idBytes, idBytes.Length - 1, SqliteNative.SQLITE_TRANSIENT);
            SqliteNative.BindInt(stmt, 2, row.CorrectStrikes);
            SqliteNative.BindText(stmt, 3, dateBytes, dateBytes.Length - 1, SqliteNative.SQLITE_TRANSIENT);
            SqliteNative.BindDouble(stmt, 4, row.ReviewInterval);
            SqliteNative.BindDouble(stmt, 5, row.EaseFactor);

            int rc = SqliteNative.Step(stmt);
            if (rc != SqliteNative.SQLITE_DONE)
            {
                Debug.LogError($"[DatabaseManager] Upsert failed: {SqliteNative.ErrMsg(_db)}");
            }
            SqliteNative.Finalize(stmt);
        }

        public List<UserProgressRow> GetAllProgress()
        {
            var rows = new List<UserProgressRow>();
            const string sql = "SELECT NodeID, CorrectStrikes, LastReviewed, ReviewInterval, EaseFactor FROM UserProgress;";
            if (!Prepare(sql, out IntPtr stmt)) return rows;

            while (SqliteNative.Step(stmt) == SqliteNative.SQLITE_ROW)
            {
                rows.Add(new UserProgressRow
                {
                    NodeID = SqliteNative.ColumnText(stmt, 0),
                    CorrectStrikes = SqliteNative.ColumnInt(stmt, 1),
                    LastReviewed = SqliteNative.ColumnText(stmt, 2),
                    ReviewInterval = (float)SqliteNative.ColumnDouble(stmt, 3),
                    EaseFactor = (float)SqliteNative.ColumnDouble(stmt, 4)
                });
            }

            SqliteNative.Finalize(stmt);
            return rows;
        }
    }
}

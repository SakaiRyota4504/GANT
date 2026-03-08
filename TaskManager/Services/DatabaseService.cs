using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using TaskManager.Models;

namespace TaskManager.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskManager");
            Directory.CreateDirectory(appDataPath);

            string dbPath = Path.Combine(appDataPath, "tasks.db");
            _connectionString = $"Data Source={dbPath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Tasks (
                            Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                            Date                TEXT NOT NULL,
                            OrderIndex          INTEGER NOT NULL DEFAULT 0,
                            Title               TEXT NOT NULL,
                            Category            TEXT,
                            Note                TEXT,
                            PlannedMinutes      INTEGER NOT NULL DEFAULT 0,
                            PlannedStartTime    TEXT,
                            PlannedEndTime      TEXT,
                            ActualStartedAt     TEXT,
                            ActualEndedAt       TEXT,
                            AccumulatedSeconds  INTEGER NOT NULL DEFAULT 0,
                            Status              INTEGER NOT NULL DEFAULT 0
                        );
                        CREATE INDEX IF NOT EXISTS idx_tasks_date ON Tasks(Date);
                    ";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── CRUD ──────────────────────────────────────────────

        public int InsertTask(TaskItem task)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO Tasks (Date, OrderIndex, Title, Category, Note,
                            PlannedMinutes, PlannedStartTime, PlannedEndTime,
                            ActualStartedAt, ActualEndedAt, AccumulatedSeconds, Status)
                        VALUES (@date, @order, @title, @category, @note,
                            @plannedMin, @plannedStart, @plannedEnd,
                            @actualStart, @actualEnd, @accumulated, @status);
                        SELECT last_insert_rowid();";
                    BindTaskParameters(cmd, task);
                    task.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    return task.Id;
                }
            }
        }

        public void UpdateTask(TaskItem task)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE Tasks SET
                            Date = @date,
                            OrderIndex = @order,
                            Title = @title,
                            Category = @category,
                            Note = @note,
                            PlannedMinutes = @plannedMin,
                            PlannedStartTime = @plannedStart,
                            PlannedEndTime = @plannedEnd,
                            ActualStartedAt = @actualStart,
                            ActualEndedAt = @actualEnd,
                            AccumulatedSeconds = @accumulated,
                            Status = @status
                        WHERE Id = @id";
                    BindTaskParameters(cmd, task);
                    cmd.Parameters.AddWithValue("@id", task.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTask(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Tasks WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TaskItem> GetTasksByDate(string date)
        {
            var list = new List<TaskItem>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Tasks WHERE Date = @date ORDER BY OrderIndex";
                    cmd.Parameters.AddWithValue("@date", date);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadTask(reader));
                    }
                }
            }
            return list;
        }

        /// <summary>集計：指定期間のタスクを取得</summary>
        public List<TaskItem> GetTasksByDateRange(string fromDate, string toDate)
        {
            var list = new List<TaskItem>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM Tasks
                        WHERE Date >= @from AND Date <= @to
                        ORDER BY Date, OrderIndex";
                    cmd.Parameters.AddWithValue("@from", fromDate);
                    cmd.Parameters.AddWithValue("@to", toDate);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadTask(reader));
                    }
                }
            }
            return list;
        }

        /// <summary>順序の一括更新</summary>
        public void UpdateOrders(IEnumerable<TaskItem> tasks)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var tr = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Tasks SET OrderIndex = @order WHERE Id = @id";
                    var pOrder = cmd.Parameters.Add("@order", System.Data.DbType.Int32);
                    var pId = cmd.Parameters.Add("@id", System.Data.DbType.Int32);
                    foreach (var t in tasks)
                    {
                        pOrder.Value = t.OrderIndex;
                        pId.Value = t.Id;
                        cmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
            }
        }

        // ── Helper ────────────────────────────────────────────

        private static void BindTaskParameters(SQLiteCommand cmd, TaskItem t)
        {
            cmd.Parameters.AddWithValue("@date", t.Date);
            cmd.Parameters.AddWithValue("@order", t.OrderIndex);
            cmd.Parameters.AddWithValue("@title", t.Title ?? "");
            cmd.Parameters.AddWithValue("@category", (object)t.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@note", (object)t.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@plannedMin", t.PlannedMinutes);
            cmd.Parameters.AddWithValue("@plannedStart", (object)t.PlannedStartTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@plannedEnd", (object)t.PlannedEndTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@actualStart",
                t.ActualStartedAt.HasValue ? (object)t.ActualStartedAt.Value.ToString("o") : DBNull.Value);
            cmd.Parameters.AddWithValue("@actualEnd",
                t.ActualEndedAt.HasValue ? (object)t.ActualEndedAt.Value.ToString("o") : DBNull.Value);
            cmd.Parameters.AddWithValue("@accumulated", t.AccumulatedSeconds);
            cmd.Parameters.AddWithValue("@status", (int)t.Status);
        }

        private static TaskItem ReadTask(SQLiteDataReader r)
        {
            var task = new TaskItem
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                Date = r.GetString(r.GetOrdinal("Date")),
                OrderIndex = r.GetInt32(r.GetOrdinal("OrderIndex")),
                Title = r.GetString(r.GetOrdinal("Title")),
                Category = r.IsDBNull(r.GetOrdinal("Category")) ? null : r.GetString(r.GetOrdinal("Category")),
                Note = r.IsDBNull(r.GetOrdinal("Note")) ? null : r.GetString(r.GetOrdinal("Note")),
                PlannedMinutes = r.GetInt32(r.GetOrdinal("PlannedMinutes")),
                PlannedStartTime = r.IsDBNull(r.GetOrdinal("PlannedStartTime")) ? null : r.GetString(r.GetOrdinal("PlannedStartTime")),
                PlannedEndTime = r.IsDBNull(r.GetOrdinal("PlannedEndTime")) ? null : r.GetString(r.GetOrdinal("PlannedEndTime")),
                AccumulatedSeconds = r.GetInt32(r.GetOrdinal("AccumulatedSeconds")),
                Status = (Models.TaskStatus)r.GetInt32(r.GetOrdinal("Status")),
            };

            int startOrd = r.GetOrdinal("ActualStartedAt");
            if (!r.IsDBNull(startOrd))
                task.ActualStartedAt = DateTime.Parse(r.GetString(startOrd));

            int endOrd = r.GetOrdinal("ActualEndedAt");
            if (!r.IsDBNull(endOrd))
                task.ActualEndedAt = DateTime.Parse(r.GetString(endOrd));

            return task;
        }
    }
}

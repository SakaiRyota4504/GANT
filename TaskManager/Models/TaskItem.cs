using System;

namespace TaskManager.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        /// <summary>対象日付 (yyyy-MM-dd)</summary>
        public string Date { get; set; }

        /// <summary>表示順序</summary>
        public int OrderIndex { get; set; }

        /// <summary>タスク名</summary>
        public string Title { get; set; }

        /// <summary>カテゴリ（任意）</summary>
        public string Category { get; set; }

        /// <summary>メモ・備考</summary>
        public string Note { get; set; }

        // ---- 予定 ----

        /// <summary>予定所要時間（分）</summary>
        public int PlannedMinutes { get; set; }

        /// <summary>予定開始時刻（HH:mm、任意）</summary>
        public string PlannedStartTime { get; set; }

        /// <summary>予定終了時刻（HH:mm、任意）</summary>
        public string PlannedEndTime { get; set; }

        // ---- 実績 ----

        /// <summary>実績開始日時</summary>
        public DateTime? ActualStartedAt { get; set; }

        /// <summary>実績終了日時</summary>
        public DateTime? ActualEndedAt { get; set; }

        /// <summary>一時停止中の累積実績時間（秒）</summary>
        public int AccumulatedSeconds { get; set; }

        /// <summary>ステータス</summary>
        public TaskStatus Status { get; set; }

        // ---- 計算プロパティ ----

        /// <summary>実績時間（分）—完了または一時停止後の合計</summary>
        public int ActualMinutes
        {
            get
            {
                if (Status == TaskStatus.Completed || Status == TaskStatus.Paused)
                    return AccumulatedSeconds / 60;
                return 0;
            }
        }

        /// <summary>予実差（分）正=早い、負=遅れ</summary>
        public int DiffMinutes => PlannedMinutes - ActualMinutes;
    }
}

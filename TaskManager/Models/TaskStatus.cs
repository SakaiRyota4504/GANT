namespace TaskManager.Models
{
    public enum TaskStatus
    {
        Pending = 0,    // 未着手
        InProgress = 1, // 進行中
        Paused = 2,     // 一時停止
        Completed = 3,  // 完了
        Skipped = 4     // スキップ
    }
}

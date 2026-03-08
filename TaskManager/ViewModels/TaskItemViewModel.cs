using System;
using System.Windows.Threading;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    /// <summary>
    /// 1タスクのVM。タイマー計測ロジックを持つ。
    /// </summary>
    public class TaskItemViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private DispatcherTimer _timer;

        // ---- バッキングフィールド ----
        private string _title;
        private string _category;
        private string _note;
        private int _plannedMinutes;
        private string _plannedStartTime;
        private string _plannedEndTime;
        private Models.TaskStatus _status;
        private int _accumulatedSeconds;
        private int _elapsedSeconds;  // タイマー稼働中の加算分
        private bool _isEditing;

        public TaskItem Model { get; }

        public TaskItemViewModel(TaskItem model, DatabaseService db)
        {
            Model = model;
            _db = db;

            _title = model.Title;
            _category = model.Category;
            _note = model.Note;
            _plannedMinutes = model.PlannedMinutes;
            _plannedStartTime = model.PlannedStartTime;
            _plannedEndTime = model.PlannedEndTime;
            _status = model.Status;
            _accumulatedSeconds = model.AccumulatedSeconds;

            // 進行中ステータスで起動した場合（クラッシュ復旧等）はタイマー再開
            if (_status == Models.TaskStatus.InProgress && model.ActualStartedAt.HasValue)
            {
                var elapsed = (int)(DateTime.Now - model.ActualStartedAt.Value).TotalSeconds;
                _elapsedSeconds = elapsed;
                StartTimer();
            }

            StartCommand = new RelayCommand(Start, () => CanStart);
            PauseCommand = new RelayCommand(Pause, () => Status == Models.TaskStatus.InProgress);
            CompleteCommand = new RelayCommand(Complete, () =>
                Status == Models.TaskStatus.InProgress || Status == Models.TaskStatus.Paused);
            ResetCommand = new RelayCommand(Reset, () => Status != Models.TaskStatus.Pending);
        }

        // ---- プロパティ ----

        public int Id => Model.Id;

        public string Title
        {
            get => _title;
            set
            {
                if (SetField(ref _title, value))
                {
                    Model.Title = value;
                    SaveToDb();
                }
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (SetField(ref _category, value))
                {
                    Model.Category = value;
                    SaveToDb();
                }
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (SetField(ref _note, value))
                {
                    Model.Note = value;
                    SaveToDb();
                }
            }
        }

        public int PlannedMinutes
        {
            get => _plannedMinutes;
            set
            {
                if (SetField(ref _plannedMinutes, value))
                {
                    Model.PlannedMinutes = value;
                    SaveToDb();
                    OnPropertyChanged(nameof(PlannedTimeDisplay));
                    OnPropertyChanged(nameof(DiffDisplay));
                }
            }
        }

        public string PlannedStartTime
        {
            get => _plannedStartTime;
            set
            {
                if (SetField(ref _plannedStartTime, value))
                {
                    Model.PlannedStartTime = value;
                    SaveToDb();
                }
            }
        }

        public string PlannedEndTime
        {
            get => _plannedEndTime;
            set
            {
                if (SetField(ref _plannedEndTime, value))
                {
                    Model.PlannedEndTime = value;
                    // 開始・終了が揃ったら予定分数を自動計算
                    TryComputePlannedMinutesFromTimes();
                    SaveToDb();
                }
            }
        }

        public Models.TaskStatus Status
        {
            get => _status;
            private set
            {
                if (SetField(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusLabel));
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(IsRunning));
                    CommandManager_Refresh();
                }
            }
        }

        public int AccumulatedSeconds
        {
            get => _accumulatedSeconds;
            private set
            {
                if (SetField(ref _accumulatedSeconds, value))
                {
                    OnPropertyChanged(nameof(ActualTimeDisplay));
                    OnPropertyChanged(nameof(ActualMinutes));
                    OnPropertyChanged(nameof(DiffDisplay));
                }
            }
        }

        /// <summary>現在表示中の実績秒数（タイマー動作中は随時更新）</summary>
        public int CurrentSeconds => _accumulatedSeconds + _elapsedSeconds;

        public int ActualMinutes => CurrentSeconds / 60;

        public bool IsRunning => Status == Models.TaskStatus.InProgress;

        public bool CanStart => Status == Models.TaskStatus.Pending || Status == Models.TaskStatus.Paused;

        public bool IsEditing
        {
            get => _isEditing;
            set => SetField(ref _isEditing, value);
        }

        // ---- 表示用 ----

        public string PlannedTimeDisplay
        {
            get
            {
                if (_plannedMinutes == 0) return "未設定";
                var h = _plannedMinutes / 60;
                var m = _plannedMinutes % 60;
                return h > 0 ? $"{h}時間{m:D2}分" : $"{m}分";
            }
        }

        public string ActualTimeDisplay
        {
            get
            {
                int sec = CurrentSeconds;
                var h = sec / 3600;
                var m = (sec % 3600) / 60;
                var s = sec % 60;
                return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
            }
        }

        public string DiffDisplay
        {
            get
            {
                if (_plannedMinutes == 0 || CurrentSeconds == 0) return "—";
                int diff = _plannedMinutes - ActualMinutes;
                if (diff > 0) return $"-{diff}分 (予定内)";
                if (diff == 0) return "ちょうど";
                return $"+{-diff}分 (超過)";
            }
        }

        public string StatusLabel
        {
            get
            {
                switch (Status)
                {
                    case Models.TaskStatus.Pending: return "未着手";
                    case Models.TaskStatus.InProgress: return "進行中";
                    case Models.TaskStatus.Paused: return "一時停止";
                    case Models.TaskStatus.Completed: return "完了";
                    case Models.TaskStatus.Skipped: return "スキップ";
                    default: return "";
                }
            }
        }

        // ---- コマンド ----

        public RelayCommand StartCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand CompleteCommand { get; }
        public RelayCommand ResetCommand { get; }

        private void Start()
        {
            _elapsedSeconds = 0;
            Model.ActualStartedAt = DateTime.Now;
            Model.Status = Models.TaskStatus.InProgress;
            Status = Models.TaskStatus.InProgress;
            StartTimer();
            SaveToDb();
        }

        private void Pause()
        {
            StopTimer();
            _accumulatedSeconds += _elapsedSeconds;
            _elapsedSeconds = 0;
            Model.AccumulatedSeconds = _accumulatedSeconds;
            Model.Status = Models.TaskStatus.Paused;
            Status = Models.TaskStatus.Paused;
            AccumulatedSeconds = _accumulatedSeconds;
            SaveToDb();
        }

        private void Complete()
        {
            StopTimer();
            _accumulatedSeconds += _elapsedSeconds;
            _elapsedSeconds = 0;
            Model.AccumulatedSeconds = _accumulatedSeconds;
            Model.ActualEndedAt = DateTime.Now;
            Model.Status = Models.TaskStatus.Completed;
            Status = Models.TaskStatus.Completed;
            AccumulatedSeconds = _accumulatedSeconds;
            SaveToDb();
        }

        private void Reset()
        {
            StopTimer();
            _elapsedSeconds = 0;
            _accumulatedSeconds = 0;
            Model.AccumulatedSeconds = 0;
            Model.ActualStartedAt = null;
            Model.ActualEndedAt = null;
            Model.Status = Models.TaskStatus.Pending;
            Status = Models.TaskStatus.Pending;
            AccumulatedSeconds = 0;
            SaveToDb();
        }

        // ---- タイマー ----

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _elapsedSeconds++;
                OnPropertyChanged(nameof(ActualTimeDisplay));
                OnPropertyChanged(nameof(CurrentSeconds));
                OnPropertyChanged(nameof(ActualMinutes));
                OnPropertyChanged(nameof(DiffDisplay));
            };
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        // ---- Helper ----

        private void TryComputePlannedMinutesFromTimes()
        {
            if (TimeSpan.TryParse(_plannedStartTime, out var start) &&
                TimeSpan.TryParse(_plannedEndTime, out var end))
            {
                var diff = end - start;
                if (diff.TotalMinutes > 0)
                {
                    _plannedMinutes = (int)diff.TotalMinutes;
                    Model.PlannedMinutes = _plannedMinutes;
                    OnPropertyChanged(nameof(PlannedMinutes));
                    OnPropertyChanged(nameof(PlannedTimeDisplay));
                    OnPropertyChanged(nameof(DiffDisplay));
                }
            }
        }

        private void SaveToDb()
        {
            if (Model.Id == 0)
                _db.InsertTask(Model);
            else
                _db.UpdateTask(Model);
        }

        private static void CommandManager_Refresh()
        {
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}

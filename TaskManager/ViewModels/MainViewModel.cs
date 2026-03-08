using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.Views;

namespace TaskManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private DateTime _selectedDate;
        private string _newTaskTitle = "";
        private string _newTaskCategory = "";
        private int _newTaskPlannedMinutes;
        private string _newTaskPlannedStart = "";
        private string _newTaskPlannedEnd = "";
        private TaskItemViewModel _selectedTask;

        public MainViewModel()
        {
            _db = new DatabaseService();
            Tasks = new ObservableCollection<TaskItemViewModel>();
            _selectedDate = DateTime.Today;

            LoadTasks();

            AddTaskCommand = new RelayCommand(AddTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
            DeleteTaskCommand = new RelayCommand(DeleteTask, () => SelectedTask != null);
            MoveUpCommand = new RelayCommand(MoveUp, () => SelectedTask != null && Tasks.IndexOf(SelectedTask) > 0);
            MoveDownCommand = new RelayCommand(MoveDown, () => SelectedTask != null && Tasks.IndexOf(SelectedTask) < Tasks.Count - 1);
            PreviousDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(1));
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);
            OpenReportCommand = new RelayCommand(OpenReport);
        }

        // ---- プロパティ ----

        public ObservableCollection<TaskItemViewModel> Tasks { get; }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetField(ref _selectedDate, value))
                {
                    OnPropertyChanged(nameof(DateLabel));
                    OnPropertyChanged(nameof(IsToday));
                    LoadTasks();
                }
            }
        }

        public string DateLabel => _selectedDate.ToString("yyyy年M月d日 (ddd)");

        public bool IsToday => _selectedDate.Date == DateTime.Today;

        public TaskItemViewModel SelectedTask
        {
            get => _selectedTask;
            set => SetField(ref _selectedTask, value);
        }

        // ---- 新規タスク入力 ----

        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set => SetField(ref _newTaskTitle, value);
        }

        public string NewTaskCategory
        {
            get => _newTaskCategory;
            set => SetField(ref _newTaskCategory, value);
        }

        public int NewTaskPlannedMinutes
        {
            get => _newTaskPlannedMinutes;
            set => SetField(ref _newTaskPlannedMinutes, value);
        }

        public string NewTaskPlannedStart
        {
            get => _newTaskPlannedStart;
            set
            {
                SetField(ref _newTaskPlannedStart, value);
                TryAutoComputeMinutes();
            }
        }

        public string NewTaskPlannedEnd
        {
            get => _newTaskPlannedEnd;
            set
            {
                SetField(ref _newTaskPlannedEnd, value);
                TryAutoComputeMinutes();
            }
        }

        // ---- 集計 ----

        public int TotalPlannedMinutes => Tasks.Sum(t => t.PlannedMinutes);
        public int TotalActualMinutes => Tasks.Sum(t => t.ActualMinutes);

        public string TotalPlannedDisplay => FormatMinutes(TotalPlannedMinutes);
        public string TotalActualDisplay => FormatMinutes(TotalActualMinutes);

        // ---- コマンド ----

        public RelayCommand AddTaskCommand { get; }
        public RelayCommand DeleteTaskCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand PreviousDayCommand { get; }
        public RelayCommand NextDayCommand { get; }
        public RelayCommand TodayCommand { get; }
        public RelayCommand OpenReportCommand { get; }

        // ---- メソッド ----

        private void LoadTasks()
        {
            Tasks.Clear();
            var items = _db.GetTasksByDate(_selectedDate.ToString("yyyy-MM-dd"));
            foreach (var item in items)
            {
                var vm = new TaskItemViewModel(item, _db);
                vm.PropertyChanged += (s, e) => RefreshTotals();
                Tasks.Add(vm);
            }
            RefreshTotals();
        }

        private void AddTask()
        {
            var model = new TaskItem
            {
                Date = _selectedDate.ToString("yyyy-MM-dd"),
                OrderIndex = Tasks.Count,
                Title = NewTaskTitle.Trim(),
                Category = string.IsNullOrWhiteSpace(NewTaskCategory) ? null : NewTaskCategory.Trim(),
                PlannedMinutes = NewTaskPlannedMinutes,
                PlannedStartTime = string.IsNullOrWhiteSpace(NewTaskPlannedStart) ? null : NewTaskPlannedStart.Trim(),
                PlannedEndTime = string.IsNullOrWhiteSpace(NewTaskPlannedEnd) ? null : NewTaskPlannedEnd.Trim(),
                Status = Models.TaskStatus.Pending
            };
            _db.InsertTask(model);

            var vm = new TaskItemViewModel(model, _db);
            vm.PropertyChanged += (s, e) => RefreshTotals();
            Tasks.Add(vm);

            // 入力欄リセット
            NewTaskTitle = "";
            NewTaskCategory = "";
            NewTaskPlannedMinutes = 0;
            NewTaskPlannedStart = "";
            NewTaskPlannedEnd = "";

            RefreshTotals();
        }

        private void DeleteTask()
        {
            if (SelectedTask == null) return;

            var result = MessageBox.Show(
                $"「{SelectedTask.Title}」を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _db.DeleteTask(SelectedTask.Id);
            Tasks.Remove(SelectedTask);
            SelectedTask = null;
            ReIndex();
            RefreshTotals();
        }

        private void MoveUp()
        {
            if (SelectedTask == null) return;
            int idx = Tasks.IndexOf(SelectedTask);
            if (idx <= 0) return;
            Tasks.Move(idx, idx - 1);
            ReIndex();
        }

        private void MoveDown()
        {
            if (SelectedTask == null) return;
            int idx = Tasks.IndexOf(SelectedTask);
            if (idx >= Tasks.Count - 1) return;
            Tasks.Move(idx, idx + 1);
            ReIndex();
        }

        private void ReIndex()
        {
            for (int i = 0; i < Tasks.Count; i++)
                Tasks[i].Model.OrderIndex = i;
            _db.UpdateOrders(Tasks.Select(t => t.Model));
        }

        private void OpenReport()
        {
            var win = new ReportWindow(_db) { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }

        private void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalPlannedMinutes));
            OnPropertyChanged(nameof(TotalActualMinutes));
            OnPropertyChanged(nameof(TotalPlannedDisplay));
            OnPropertyChanged(nameof(TotalActualDisplay));
        }

        private void TryAutoComputeMinutes()
        {
            if (TimeSpan.TryParse(_newTaskPlannedStart, out var s) &&
                TimeSpan.TryParse(_newTaskPlannedEnd, out var e))
            {
                var diff = e - s;
                if (diff.TotalMinutes > 0)
                    NewTaskPlannedMinutes = (int)diff.TotalMinutes;
            }
        }

        private static string FormatMinutes(int minutes)
        {
            var h = minutes / 60;
            var m = minutes % 60;
            return h > 0 ? $"{h}時間{m:D2}分" : $"{m}分";
        }
    }
}

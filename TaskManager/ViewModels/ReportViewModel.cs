using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class ReportRow
    {
        public string Date { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public int PlannedMinutes { get; set; }
        public int ActualMinutes { get; set; }
        public int DiffMinutes => PlannedMinutes - ActualMinutes;
        public string PlannedDisplay => FormatMin(PlannedMinutes);
        public string ActualDisplay => FormatMin(ActualMinutes);
        public string DiffDisplay
        {
            get
            {
                if (PlannedMinutes == 0 || ActualMinutes == 0) return "—";
                int d = DiffMinutes;
                return d > 0 ? $"-{d}分" : d == 0 ? "±0" : $"+{-d}分";
            }
        }
        public string StatusLabel { get; set; }

        private static string FormatMin(int m)
        {
            var h = m / 60; var mm = m % 60;
            return h > 0 ? $"{h}:{mm:D2}" : $"{mm}分";
        }
    }

    public class CategorySummary
    {
        public string Category { get; set; }
        public int PlannedMinutes { get; set; }
        public int ActualMinutes { get; set; }
        public int TaskCount { get; set; }
        public string PlannedDisplay => FormatMin(PlannedMinutes);
        public string ActualDisplay => FormatMin(ActualMinutes);

        private static string FormatMin(int m)
        {
            var h = m / 60; var mm = m % 60;
            return h > 0 ? $"{h}:{mm:D2}" : $"{mm}分";
        }
    }

    public class ReportViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private int _reportType; // 0=週次, 1=月次, 2=カスタム
        private DateTime _referenceDate = DateTime.Today;
        private DateTime _customFrom = DateTime.Today.AddDays(-6);
        private DateTime _customTo = DateTime.Today;

        public ReportViewModel(DatabaseService db)
        {
            _db = db;
            Rows = new ObservableCollection<ReportRow>();
            CategorySummaries = new ObservableCollection<CategorySummary>();
            LoadCommand = new RelayCommand(Load);
            Load();
        }

        public int ReportType
        {
            get => _reportType;
            set
            {
                if (SetField(ref _reportType, value))
                    Load();
            }
        }

        public DateTime ReferenceDate
        {
            get => _referenceDate;
            set
            {
                if (SetField(ref _referenceDate, value))
                    Load();
            }
        }

        public DateTime CustomFrom
        {
            get => _customFrom;
            set => SetField(ref _customFrom, value);
        }

        public DateTime CustomTo
        {
            get => _customTo;
            set => SetField(ref _customTo, value);
        }

        public string PeriodLabel { get; private set; }
        public int TotalPlanned { get; private set; }
        public int TotalActual { get; private set; }
        public string TotalPlannedDisplay => FormatMin(TotalPlanned);
        public string TotalActualDisplay => FormatMin(TotalActual);
        public int CompletedCount { get; private set; }
        public int TotalCount { get; private set; }

        public ObservableCollection<ReportRow> Rows { get; }
        public ObservableCollection<CategorySummary> CategorySummaries { get; }

        public RelayCommand LoadCommand { get; }

        private void Load()
        {
            DateTime from, to;
            switch (_reportType)
            {
                case 0: // 週次（月曜始まり）
                    int diff = (int)_referenceDate.DayOfWeek - (int)DayOfWeek.Monday;
                    if (diff < 0) diff += 7;
                    from = _referenceDate.AddDays(-diff).Date;
                    to = from.AddDays(6);
                    PeriodLabel = $"{from:yyyy/MM/dd} 〜 {to:yyyy/MM/dd}（週次）";
                    break;
                case 1: // 月次
                    from = new DateTime(_referenceDate.Year, _referenceDate.Month, 1);
                    to = from.AddMonths(1).AddDays(-1);
                    PeriodLabel = $"{from:yyyy年M月}（月次）";
                    break;
                default: // カスタム
                    from = _customFrom.Date;
                    to = _customTo.Date;
                    PeriodLabel = $"{from:yyyy/MM/dd} 〜 {to:yyyy/MM/dd}（カスタム）";
                    break;
            }

            var tasks = _db.GetTasksByDateRange(
                from.ToString("yyyy-MM-dd"),
                to.ToString("yyyy-MM-dd"));

            Rows.Clear();
            foreach (var t in tasks)
            {
                Rows.Add(new ReportRow
                {
                    Date = t.Date,
                    Category = t.Category ?? "—",
                    Title = t.Title,
                    PlannedMinutes = t.PlannedMinutes,
                    ActualMinutes = t.AccumulatedSeconds / 60,
                    StatusLabel = StatusLabel(t.Status)
                });
            }

            // カテゴリ集計
            CategorySummaries.Clear();
            var groups = tasks.GroupBy(t => t.Category ?? "未分類");
            foreach (var g in groups.OrderBy(g => g.Key))
            {
                CategorySummaries.Add(new CategorySummary
                {
                    Category = g.Key,
                    PlannedMinutes = g.Sum(t => t.PlannedMinutes),
                    ActualMinutes = g.Sum(t => t.AccumulatedSeconds / 60),
                    TaskCount = g.Count()
                });
            }

            TotalPlanned = tasks.Sum(t => t.PlannedMinutes);
            TotalActual = tasks.Sum(t => t.AccumulatedSeconds / 60);
            CompletedCount = tasks.Count(t => t.Status == Models.TaskStatus.Completed);
            TotalCount = tasks.Count;

            OnPropertyChanged(nameof(PeriodLabel));
            OnPropertyChanged(nameof(TotalPlanned));
            OnPropertyChanged(nameof(TotalActual));
            OnPropertyChanged(nameof(TotalPlannedDisplay));
            OnPropertyChanged(nameof(TotalActualDisplay));
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        private static string StatusLabel(Models.TaskStatus s)
        {
            switch (s)
            {
                case Models.TaskStatus.Pending: return "未着手";
                case Models.TaskStatus.InProgress: return "進行中";
                case Models.TaskStatus.Paused: return "一時停止";
                case Models.TaskStatus.Completed: return "完了";
                case Models.TaskStatus.Skipped: return "スキップ";
                default: return "";
            }
        }

        private static string FormatMin(int m)
        {
            var h = m / 60; var mm = m % 60;
            return h > 0 ? $"{h}時間{mm:D2}分" : $"{mm}分";
        }
    }
}

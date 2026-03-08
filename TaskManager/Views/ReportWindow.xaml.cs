using System;
using System.Windows;
using System.Windows.Controls;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager.Views
{
    public partial class ReportWindow : Window
    {
        private readonly ReportViewModel _vm;

        public ReportWindow(DatabaseService db)
        {
            InitializeComponent();
            _vm = new ReportViewModel(db);
            DataContext = _vm;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;
            var rb = (RadioButton)sender;
            if (int.TryParse(rb.Tag?.ToString(), out int type))
                _vm.ReportType = type;
        }

        private void PrevPeriod_Click(object sender, RoutedEventArgs e)
        {
            switch (_vm.ReportType)
            {
                case 0: _vm.ReferenceDate = _vm.ReferenceDate.AddDays(-7); break;
                case 1: _vm.ReferenceDate = _vm.ReferenceDate.AddMonths(-1); break;
                default: _vm.ReferenceDate = _vm.ReferenceDate.AddDays(-7); break;
            }
        }

        private void NextPeriod_Click(object sender, RoutedEventArgs e)
        {
            switch (_vm.ReportType)
            {
                case 0: _vm.ReferenceDate = _vm.ReferenceDate.AddDays(7); break;
                case 1: _vm.ReferenceDate = _vm.ReferenceDate.AddMonths(1); break;
                default: _vm.ReferenceDate = _vm.ReferenceDate.AddDays(7); break;
            }
        }

        private void ResetPeriod_Click(object sender, RoutedEventArgs e)
        {
            _vm.ReferenceDate = DateTime.Today;
        }
    }
}

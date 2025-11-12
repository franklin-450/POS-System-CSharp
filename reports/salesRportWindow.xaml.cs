using SmartPOS.UI.Data;
using SmartPOS.UI.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SmartPOS.UI.Reports
{
    public partial class SalesReportWindow : Window
    {
        private ObservableCollection<SaleRecord> _allSales;

        public SalesReportWindow()
        {
            InitializeComponent();
            _allSales = SalesDatabase.SalesList;
            SalesGrid.ItemsSource = _allSales;
            UpdateSummary();
        }

        private void FilterByDate_Click(object sender, RoutedEventArgs e)
        {
            if (FilterDate.SelectedDate is DateTime date)
            {
                var filtered = _allSales.Where(s => s.Date.Date == date.Date);
                SalesGrid.ItemsSource = new ObservableCollection<SaleRecord>(filtered);
                UpdateSummary(filtered);
            }
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SalesGrid.ItemsSource = _allSales;
            FilterDate.SelectedDate = null;
            UpdateSummary();
        }

        private void UpdateSummary(IEnumerable<SaleRecord>? list = null)
        {
            var data = list?.ToList() ?? _allSales.ToList();

            TxtTotalTransactions.Text = data.Count.ToString();
            TxtTotalSales.Text = data.Sum(s => s.TotalAmount).ToString("N2");
            TxtLastSale.Text = data.OrderByDescending(s => s.Date)
                                   .FirstOrDefault()?.Date.ToString("dd MMM yyyy, HH:mm") ?? "—";
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using System;
using System.Collections.ObjectModel;

namespace SmartPOS.UI.Models
{
    public class SaleRecord
    {
        public DateTime Date { get; set; }
        public string Cashier { get; set; } = "Admin";
        public string PaymentMethod { get; set; } = "Cash";
        public double TotalAmount { get; set; }
        public ObservableCollection<CartItem> Items { get; set; } = new();
    }
}

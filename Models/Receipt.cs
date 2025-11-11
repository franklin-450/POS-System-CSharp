using System;
using System.Collections.ObjectModel;
using System.Text;

namespace SmartPOS.UI.Models
{
    public class Receipt
    {
        public string ReceiptNumber { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime Date { get; set; } = DateTime.Now;
        public ObservableCollection<CartItem> Items { get; set; } = new();
        public double Subtotal { get; set; }
        public double Tax { get; set; }
        public double Total { get; set; }

        public string GenerateTextReceipt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== SMARTPOS ==========");
            sb.AppendLine($"Receipt #: {ReceiptNumber}");
            sb.AppendLine($"Date: {Date:yyyy-MM-dd HH:mm}");
            sb.AppendLine("--------------------------------");
            foreach (var i in Items)
                sb.AppendLine($"{i.Product.Name} x{i.Quantity}   {i.TotalPrice,10:N2}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Subtotal:   {Subtotal,10:N2}");
            sb.AppendLine($"VAT (16%):  {Tax,10:N2}");
            sb.AppendLine($"TOTAL:      {Total,10:N2}");
            sb.AppendLine("================================");
            sb.AppendLine("   THANK YOU FOR SHOPPING!   ");
            sb.AppendLine("================================");
            return sb.ToString();
        }
    }
}

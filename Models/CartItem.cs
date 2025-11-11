using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartPOS.UI.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        public Product Product { get; set; }
        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public double TotalPrice => Product.Price * Quantity;

        public CartItem(Product p) => Product = p;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartPOS.UI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Product> _products = new();
        private ObservableCollection<CartItem> _cartItems = new();
        private string? _searchQuery;
        private string _currentUserDisplay = "Cashier: Admin";
        private const double TaxRate = 0.16;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set { _cartItems = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalDisplay)); }
        }

        public string? SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); }
        }

        public string CurrentUserDisplay
        {
            get => _currentUserDisplay;
            set { _currentUserDisplay = value; OnPropertyChanged(); }
        }

        public double Subtotal => CartItems.Sum(c => c.TotalPrice);
        public double Tax => Subtotal * TaxRate;
        public double TotalWithTax => Subtotal + Tax;
        public string TotalDisplay => $"Subtotal: Ksh {Subtotal:N2}\nTax: Ksh {Tax:N2}\nTotal: Ksh {TotalWithTax:N2}";

        // Commands
        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand CmdCheckout { get; }
        public ICommand CmdCheckoutCash { get; }
        public ICommand CmdHold { get; }

        // Example barcodes
        private readonly Dictionary<string, string> BarcodeMap = new()
        {
            { "8901234567890", "Coca Cola 500ml" },
            { "8901234567891", "Bread - Sliced" },
            { "8901234567892", "Milk 1L" }
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Products = new ObservableCollection<Product>(ProductDatabase.DefaultList);
            CartItems = new ObservableCollection<CartItem>();
            CartItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalDisplay));

            AddToCartCommand = new RelayCommand(obj => { if (obj is Product p) AddToCart(p); });
            RemoveFromCartCommand = new RelayCommand(obj => { if (obj is CartItem item) RemoveFromCart(item); });
            IncreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, 1); });
            DecreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, -1); });
            CmdCheckout = new RelayCommand(_ => Checkout("Card"));
            CmdCheckoutCash = new RelayCommand(_ => Checkout("Cash"));
            CmdHold = new RelayCommand(_ => HoldOrder());
        }

        // 🟢 Toast notifications (fade in/out smartly)
        private async void ShowToast(string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    ToastText.Text = message;
                    ToastPanel.Opacity = 0;
                });

                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromSeconds(0.25));
                ToastPanel.BeginAnimation(OpacityProperty, fadeIn);

                await Task.Delay(2500);

                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromSeconds(0.45));
                ToastPanel.BeginAnimation(OpacityProperty, fadeOut);
            }
            catch { /* ignore animation issues */ }
        }

        // 🧾 Barcode & Search logic
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string code = (SearchQuery ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code)) return;

            if (BarcodeMap.TryGetValue(code, out var productName))
            {
                var product = ProductDatabase.DefaultList.FirstOrDefault(p => p.Name == productName);
                if (product != null)
                {
                    AddToCart(product);
                    ShowToast($"✅ Scanned: {product.Name} added to cart");
                }
                else
                    ShowToast("❌ Barcode not found.");

                SearchQuery = string.Empty;
                OnPropertyChanged(nameof(SearchQuery));
            }
            else
            {
                FilterProductsByQuery(code);
            }
        }

        private void FilterProductsByQuery(string q)
        {
            Products = string.IsNullOrWhiteSpace(q)
                ? new ObservableCollection<Product>(ProductDatabase.DefaultList)
                : new ObservableCollection<Product>(
                    ProductDatabase.DefaultList.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        // 🛒 Cart management
        private void AddToCart(Product product)
        {
            var existing = CartItems.FirstOrDefault(c => c.Product.Name == product.Name);
            if (existing != null)
                existing.Quantity++;
            else
                CartItems.Add(new CartItem(product));

            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void RemoveFromCart(CartItem item)
        {
            if (CartItems.Contains(item))
                CartItems.Remove(item);
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void ChangeQuantity(CartItem item, int delta)
        {
            item.Quantity += delta;
            if (item.Quantity <= 0)
                RemoveFromCart(item);
            OnPropertyChanged(nameof(TotalDisplay));
        }

        // 💳 Checkout + auto-print
        private async void Checkout(string method)
        {
            if (!CartItems.Any())
            {
                ShowToast("🛑 Cart is empty.");
                return;
            }

            var receipt = GenerateReceipt();

            try
            {
                await Task.Delay(500); // small delay for smooth UI
                PrintReceipt(receipt); // auto print
                ShowToast($"✅ {method} payment complete. Printing receipt...");
            }
            catch (Exception ex)
            {
                ShowToast($"⚠️ Print failed: {ex.Message}");
            }

            CartItems.Clear();
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void HoldOrder()
        {
            ShowToast("⏸️ Order held for later.");
        }

        // 🖨️ Auto printing (no prompt)
        private void PrintReceipt(Receipt receipt)
        {
            string text = receipt.GenerateTextReceipt();

            var tb = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };

            var pd = new PrintDialog();
            pd.PrintVisual(tb, "SmartPOS Receipt");
        }

        // 🧾 Generate receipt object
        private Receipt GenerateReceipt() => new()
        {
            Items = new ObservableCollection<CartItem>(
                CartItems.Select(ci => new CartItem(ci.Product) { Quantity = ci.Quantity })),
            Subtotal = Subtotal,
            Tax = Tax,
            Total = TotalWithTax
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // 📦 Models
    public class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string ImagePath { get; set; }
        public string PriceDisplay => $"{Price:N2}";
        public int StockQty { get; set; } = 100;
        public Product(string name, double price, string img) { Name = name; Price = price; ImagePath = img; }
    }

    public class CartItem : INotifyPropertyChanged
    {
        public Product Product { get; set; }
        private int _quantity = 1;
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); } }
        public double TotalPrice => Product.Price * Quantity;
        public CartItem(Product p) { Product = p; }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public static class ProductDatabase
    {
public static readonly ObservableCollection<Product> DefaultList = new()
{
    new Product("Coca Cola 500ml", 80, "pack://application:,,,/Images/coke.png"),
    new Product("Bread - Sliced", 120, "pack://application:,,,/Images/bread.png"),
    new Product("Milk 1L", 100, "pack://application:,,,/Images/milk.png"),
    new Product("Rice 2kg", 300, "pack://application:,,,/Images/rice.png"),
    new Product("Cooking Oil 1L", 450, "pack://application:,,,/Images/oil.png"),
    new Product("Sugar 1kg", 250, "pack://application:,,,/Images/sugar.png"),
    new Product("Soap Bar", 150, "pack://application:,,,/Images/soap.png"),
    new Product("Toothpaste", 200, "pack://application:,,,/Images/toothpaste.png"),
};

    }

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

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _can;
        public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public RelayCommand(Action<object?> exec, Predicate<object?>? can = null) { _execute = exec; _can = can; }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _execute(p);
    }
}

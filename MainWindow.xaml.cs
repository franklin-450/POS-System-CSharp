using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SmartPOS.UI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Product> _products = new();
        private ObservableCollection<CartItem> _cartItems = new();
        private string? _searchQuery;
        private string _currentUserDisplay = "Cashier: Admin";

        private const double TaxRate = 0.16; // 16% VAT

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
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                FilterProducts();
            }
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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Products = new ObservableCollection<Product>(ProductDatabase.DefaultList);
            CartItems = new ObservableCollection<CartItem>();

            AddToCartCommand = new RelayCommand(obj => { if (obj is Product p) AddToCart(p); });
            RemoveFromCartCommand = new RelayCommand(obj => { if (obj is CartItem item) RemoveFromCart(item); });
            IncreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, 1); });
            DecreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, -1); });
            CmdCheckout = new RelayCommand(_ => CheckoutCard());
            CmdCheckoutCash = new RelayCommand(_ => CheckoutCash());
            CmdHold = new RelayCommand(_ => HoldOrder());
        }

        private void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                Products = new ObservableCollection<Product>(ProductDatabase.DefaultList);
            }
            else
            {
                var filtered = ProductDatabase.DefaultList
                    .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Products = new ObservableCollection<Product>(filtered);
            }
        }

        private void AddToCart(Product product)
        {
            var existing = CartItems.FirstOrDefault(c => c.Product.Name == product.Name);
            if (existing != null)
            {
                existing.Quantity++;
                OnPropertyChanged(nameof(CartItems));
            }
            else
            {
                CartItems.Add(new CartItem(product));
            }
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void RemoveFromCart(CartItem item)
        {
            if (CartItems.Contains(item))
            {
                CartItems.Remove(item);
                OnPropertyChanged(nameof(TotalDisplay));
            }
        }

        private void ChangeQuantity(CartItem item, int change)
        {
            item.Quantity += change;
            if (item.Quantity <= 0)
                RemoveFromCart(item);
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void CheckoutCard()
        {
            if (!CartItems.Any())
            {
                MessageBox.Show("Cart is empty.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generate receipt
            var receipt = GenerateReceipt();
            MessageBox.Show(receipt.GenerateTextReceipt(), "Receipt", MessageBoxButton.OK);

            // TODO: Deduct stock quantities here
            // foreach (var item in CartItems)
            //     item.Product.StockQty -= item.Quantity;

            CartItems.Clear();
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void CheckoutCash()
        {
            if (!CartItems.Any())
            {
                MessageBox.Show("Cart is empty.", "Checkout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generate receipt
            var receipt = GenerateReceipt();
            MessageBox.Show(receipt.GenerateTextReceipt(), "Receipt", MessageBoxButton.OK);

            // TODO: Deduct stock quantities here
            // foreach (var item in CartItems)
            //     item.Product.StockQty -= item.Quantity;

            CartItems.Clear();
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void HoldOrder()
        {
            MessageBox.Show("Order placed on hold. You can resume it later.",
                            "Hold Order", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) FilterProducts();
        }

        // Generate receipt
        private Receipt GenerateReceipt()
        {
            return new Receipt
            {
                Items = new ObservableCollection<CartItem>(CartItems),
                Subtotal = Subtotal,
                Tax = Tax,
                Total = TotalWithTax
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    // Product model
    public class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string ImagePath { get; set; }
        public string PriceDisplay => $"{Price:N2}";
        // Optional stock for future inventory management
        public int StockQty { get; set; } = 100;

        public Product(string name, double price, string imagePath)
        {
            Name = name;
            Price = price;
            ImagePath = imagePath;
        }
    }

    // Cart item tracks quantity
    public class CartItem : INotifyPropertyChanged
    {
        public Product Product { get; set; }
        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
        }
        public double TotalPrice => Product.Price * Quantity;

        public CartItem(Product product)
        {
            Product = product;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    // Product database
    public static class ProductDatabase
    {
        public static readonly ObservableCollection<Product> DefaultList = new()
        {
            new Product("Coca Cola 500ml", 80, "Images/coke.png"),
            new Product("Bread - Sliced", 120, "Images/bread.png"),
            new Product("Milk 1L", 100, "Images/milk.png"),
            new Product("Rice 2kg", 300, "Images/rice.png"),
            new Product("Cooking Oil 1L", 450, "Images/oil.png"),
            new Product("Sugar 1kg", 250, "Images/sugar.png"),
            new Product("Soap Bar", 150, "Images/soap.png"),
            new Product("Toothpaste", 200, "Images/toothpaste.png"),
        };
    }

    // Receipt class
    public class Receipt
    {
        public string ReceiptNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        public DateTime Date { get; set; } = DateTime.Now;
        public ObservableCollection<CartItem> Items { get; set; } = new();
        public double Subtotal { get; set; }
        public double Tax { get; set; }
        public double Total { get; set; }

        public string GenerateTextReceipt()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== SMARTPOS RECEIPT ===");
            sb.AppendLine($"Receipt #: {ReceiptNumber}");
            sb.AppendLine($"Date: {Date}");
            sb.AppendLine("-----------------------");

            foreach (var item in Items)
                sb.AppendLine($"{item.Product.Name} x{item.Quantity} - {item.TotalPrice:N2}");

            sb.AppendLine("-----------------------");
            sb.AppendLine($"Subtotal: {Subtotal:N2}");
            sb.AppendLine($"Tax: {Tax:N2}");
            sb.AppendLine($"TOTAL: {Total:N2}");
            sb.AppendLine("=======================");
            sb.AppendLine("Thank you for shopping!");
            return sb.ToString();
        }
    }

    // RelayCommand for WPF
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}

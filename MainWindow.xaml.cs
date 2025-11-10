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
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.IO;

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

        private readonly Dictionary<string, string> BarcodeMap = new()
        {
            { "8901234567890", "Coca Cola 500ml" },
            { "8901234567891", "Bread - Sliced" },
            { "8901234567892", "Milk 1L" }
        };

        private UdpClient? _udpListener;
        private readonly int BroadcastPort = 50000;
        private readonly Guid _instanceId = Guid.NewGuid();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Load products from ProductDatabase
            ProductDatabase.Load();
            Products = new ObservableCollection<Product>(ProductDatabase.DefaultList);

            CartItems = new ObservableCollection<CartItem>();
            CartItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalDisplay));

            // Commands
            AddToCartCommand = new RelayCommand(obj => { if (obj is Product p) AddToCart(p); });
            RemoveFromCartCommand = new RelayCommand(obj => { if (obj is CartItem item) RemoveFromCart(item); });
            IncreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, 1); });
            DecreaseQuantityCommand = new RelayCommand(obj => { if (obj is CartItem item) ChangeQuantity(item, -1); });
            CmdCheckout = new RelayCommand(_ => Checkout("Card"));
            CmdCheckoutCash = new RelayCommand(_ => Checkout("Cash"));
            CmdHold = new RelayCommand(_ => HoldOrder());

            // Start UDP listener
            StartUdpListener();
        }

        // Toast notifications
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
            catch { }
        }

        // Add Product button
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var entryWindow = new ProductEntryWindow { Owner = this };
            if (entryWindow.ShowDialog() == true)
            {
                var newProduct = new Product(entryWindow.ProductName, entryWindow.Price, entryWindow.ImagePath);
                ProductDatabase.Add(newProduct);
                Products.Add(newProduct);
                ShowToast($"✅ Product '{entryWindow.ProductName}' added successfully!");
                BroadcastProduct(entryWindow);
            }
        }

        // Barcode search
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
                else ShowToast("❌ Barcode not found.");

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

        // Cart management
        private void AddToCart(Product product)
        {
            var existing = CartItems.FirstOrDefault(c => c.Product.Name == product.Name);
            if (existing != null) existing.Quantity++;
            else CartItems.Add(new CartItem(product));
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void RemoveFromCart(CartItem item)
        {
            if (CartItems.Contains(item)) CartItems.Remove(item);
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void ChangeQuantity(CartItem item, int delta)
        {
            item.Quantity += delta;
            if (item.Quantity <= 0) RemoveFromCart(item);
            OnPropertyChanged(nameof(TotalDisplay));
        }

        // Checkout
        private async void Checkout(string method)
        {
            if (!CartItems.Any()) { ShowToast("🛑 Cart is empty."); return; }

            var receipt = GenerateReceipt();

            try
            {
                await Task.Delay(500);
                PrintReceipt(receipt);
                ShowToast($"✅ {method} payment complete. Printing receipt...");
            }
            catch (Exception ex)
            {
                ShowToast($"⚠️ Print failed: {ex.Message}");
            }

            CartItems.Clear();
            OnPropertyChanged(nameof(TotalDisplay));
        }

        private void HoldOrder() => ShowToast("⏸️ Order held for later.");

        // Auto printing
        private void PrintReceipt(Receipt receipt)
        {
            var tb = new TextBlock
            {
                Text = receipt.GenerateTextReceipt(),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
            var pd = new PrintDialog();
            pd.PrintVisual(tb, "SmartPOS Receipt");
        }

        private Receipt GenerateReceipt() => new()
        {
            Items = new ObservableCollection<CartItem>(CartItems.Select(ci => new CartItem(ci.Product) { Quantity = ci.Quantity })),
            Subtotal = Subtotal,
            Tax = Tax,
            Total = TotalWithTax
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // UDP broadcast & listener
        private void BroadcastProduct(ProductEntryWindow entryWindow)
        {
            try
            {
                var udpClient = new UdpClient { EnableBroadcast = true };
                var dto = new
                {
                    InstanceId = _instanceId,
                    Name = entryWindow.ProductName,
                    Barcode = entryWindow.Barcode,
                    Price = entryWindow.Price,
                    VAT = entryWindow.VAT,
                    ImageFileName = Path.GetFileName(entryWindow.ImagePath),
                    ImageBase64 = File.Exists(entryWindow.ImagePath)
                        ? Convert.ToBase64String(File.ReadAllBytes(entryWindow.ImagePath))
                        : null
                };
                var json = JsonSerializer.Serialize(dto);
                var bytes = Encoding.UTF8.GetBytes(json);
                udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, BroadcastPort));
                udpClient.Close();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowToast($"Broadcast failed: {ex.Message}"));
            }
        }

        private void StartUdpListener()
        {
            try
            {
                _udpListener = new UdpClient(BroadcastPort) { EnableBroadcast = true };
                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var result = await _udpListener.ReceiveAsync();
                            var json = Encoding.UTF8.GetString(result.Buffer);
                            ProcessIncomingProduct(json);
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowToast($"UDP listener failed: {ex.Message}"));
            }
        }

        private void ProcessIncomingProduct(string json)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ProductBroadcastDto>(json);
                if (dto == null || dto.InstanceId == _instanceId) return;

                string? localRelative = null;
                if (!string.IsNullOrWhiteSpace(dto.ImageBase64) && !string.IsNullOrWhiteSpace(dto.ImageFileName))
                {
                    var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);
                    var dest = Path.Combine(imagesDir, dto.ImageFileName);
                    if (!File.Exists(dest))
                        File.WriteAllBytes(dest, Convert.FromBase64String(dto.ImageBase64));
                    localRelative = Path.Combine("Images", dto.ImageFileName);
                }

                Dispatcher.Invoke(() =>
                {
                    var exists = ProductDatabase.DefaultList.Any(p => p.Name == dto.Name);
                    if (!exists)
                    {
                        ProductDatabase.DefaultList.Add(new Product(dto.Name ?? "Unknown", dto.Price, localRelative ?? ""));
                        ShowToast($"Product received: {dto.Name}");
                    }
                });
            }
            catch { }
        }

        private class ProductBroadcastDto
        {
            public Guid InstanceId { get; set; }
            public string? Name { get; set; }
            public string? Barcode { get; set; }
            public double Price { get; set; }
            public double VAT { get; set; }
            public string? Description { get; set; }
            public string? ImageFileName { get; set; }
            public string? ImageBase64 { get; set; }
        }
    }

    // Cart & Product models
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

    // Receipt & Command
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

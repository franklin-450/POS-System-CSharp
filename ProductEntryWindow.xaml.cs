using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SmartPOS.UI
{
    public partial class ProductEntryWindow : Window
    {
        // 🔹 Public properties for MainWindow access
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public double Price { get; set; }
        public double VAT { get; set; }
        public string ImagePath { get; set; } = string.Empty;

        // 🔹 Internal fields
        private readonly string ImagesFolder;
        private readonly Guid InstanceId = Guid.NewGuid();

        public ProductEntryWindow()
        {
            InitializeComponent();

            ImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);
        }

        // 🖼️ Browse image
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                ImagePath = dlg.FileName;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(ImagePath);
                    bitmap.EndInit();
                    bitmap.Freeze(); // safe for cross-thread use
                    imgPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 🧹 Clear image preview
        private void btnClearImage_Click(object sender, RoutedEventArgs e)
        {
            ImagePath = string.Empty;
            imgPreview.Source = null;
        }

        // 💾 Save product and broadcast
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter product name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtPrice.Text, out var price))
            {
                MessageBox.Show("Please enter a valid price.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(txtVAT.Text, out var vat)) vat = 16;

            // ✅ Create DTO
            var dto = new ProductBroadcastDto
            {
                InstanceId = InstanceId,
                Name = txtName.Text.Trim(),
                Barcode = txtBarcode.Text.Trim(),
                Price = price,
                VAT = vat,
                Description = txtDescription?.Text?.Trim() ?? ""
            };

            // ✅ Handle image copy + encode asynchronously
            if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(ImagePath);
                    var destPath = Path.Combine(ImagesFolder, fileName);

                    await Task.Run(() => File.Copy(ImagePath, destPath, overwrite: true));
                    dto.ImageFileName = fileName;
                    dto.ImageBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(destPath));
                    ImagePath = destPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Image processing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // ✅ Save locally to product list for instant use
            var relativePath = dto.ImageFileName != null ? Path.Combine("Images", dto.ImageFileName) : "";
            // Add product to database (will auto-save to JSON)
ProductDatabase.Add(new Product(dto.Name ?? "Unnamed", dto.Price, relativePath));

            // ✅ Broadcast product to all on LAN
            try
            {
                var json = JsonSerializer.Serialize(dto);
                await BroadcastJsonAsync(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Network broadcast failed: {ex.Message}", "Network Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // ✅ Set return data for MainWindow
            ProductName = dto.Name ?? string.Empty;
            Barcode = dto.Barcode ?? string.Empty;
            Price = dto.Price;
            VAT = dto.VAT;

            MessageBox.Show("✅ Product saved and broadcasted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        // ❌ Cancel button
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // 🌐 Send UDP broadcast
        private async Task BroadcastJsonAsync(string json)
        {
            await Task.Run(() =>
            {
                using var client = new UdpClient();
                client.EnableBroadcast = true;
                var bytes = Encoding.UTF8.GetBytes(json);
                var endpoint = new IPEndPoint(IPAddress.Broadcast, 50000);
                client.Send(bytes, bytes.Length, endpoint);
            });
        }

        // 📦 DTO for network product transfer
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
}

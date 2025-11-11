using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace SmartPOS.UI.Models
{
    public class Product
    {
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string PriceDisplay => $"{Price:N2}";
        public int StockQty { get; set; } = 100;

        public Product(string name, double price, string img, string barcode = "")
        {
            Name = name;
            Price = price;
            ImagePath = img;
            Barcode = barcode;
        }

        public Product() { }

        // ❌ This property should not be serialized
        [JsonIgnore]
        public BitmapImage? ImageBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return null;

                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagePath);
                if (!File.Exists(fullPath)) return null;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}

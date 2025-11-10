using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SmartPOS.UI
{
    public static class ProductDatabase
    {
        private static readonly string DataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products.json");

        public static ObservableCollection<Product> DefaultList { get; private set; } = new();

        /// <summary>
        /// Load products from JSON file
        /// </summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    var products = JsonSerializer.Deserialize<ObservableCollection<Product>>(json);
                    if (products != null) DefaultList = products;
                }
            }
            catch
            {
                DefaultList = new ObservableCollection<Product>();
            }
        }

        /// <summary>
        /// Save current products to JSON file
        /// </summary>
        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(DefaultList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFile, json);
            }
            catch
            {
                // optionally log or ignore
            }
        }

        /// <summary>
        /// Add a new product to the database and save
        /// </summary>
        public static void Add(Product product)
        {
            if (product == null) return;

            // Avoid duplicates by name
            var exists = DefaultList.Any(p => p.Name == product.Name);
            if (!exists)
            {
                DefaultList.Add(product);
                Save();
            }
        }

        /// <summary>
        /// Optional: remove a product
        /// </summary>
        public static void Remove(Product product)
        {
            if (product == null) return;

            if (DefaultList.Contains(product))
            {
                DefaultList.Remove(product);
                Save();
            }
        }
    }
}

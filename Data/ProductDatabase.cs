using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using SmartPOS.UI.Models;

namespace SmartPOS.UI.Data
{
    public static class ProductDatabase
    {
        public static readonly ObservableCollection<Product> DefaultList = new();

        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var list = JsonSerializer.Deserialize<List<Product>>(json);

                    if (list != null && list.Count > 0)
                    {
                        DefaultList.Clear();
                        foreach (var p in list)
                            DefaultList.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Failed to load products: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(DefaultList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Failed to save products: {ex.Message}");
            }
        }

        public static void Add(Product product)
        {
            if (product == null) return;

            // Avoid duplicates
            var exists = DefaultList.Any(p => p.Name == product.Name);
            if (!exists)
            {
                DefaultList.Add(product);
                Save();
            }
        }
    }
}

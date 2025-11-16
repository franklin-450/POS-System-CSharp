using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using SmartPOS.UI.Models;
using SmartPOS.UI.Helpers;

namespace SmartPOS.UI.Data
{
    public static class ProductDatabase
    {
        public static readonly ObservableCollection<Product> DefaultList = new();

        private static readonly string FilePath = Path.Combine(LocalStorage.AppFolder, "products.json");
        private static readonly string ImagesFolder = Path.Combine(LocalStorage.AppFolder, "Images");

        static ProductDatabase()
        {
            // Ensure Images folder exists
            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);

            // Ensure products.json exists
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "[]");
        }

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
                        {
                            // Make sure image path is relative to AppData\Images
                            if (!string.IsNullOrWhiteSpace(p.ImagePath))
                                p.ImagePath = Path.Combine(ImagesFolder, Path.GetFileName(p.ImagePath));

                            DefaultList.Add(p);
                        }
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
                // Ensure image is stored in AppData Images folder
                if (!string.IsNullOrWhiteSpace(product.ImagePath) && File.Exists(product.ImagePath))
                {
                    string dest = Path.Combine(ImagesFolder, Path.GetFileName(product.ImagePath));
                    if (!File.Exists(dest))
                        File.Copy(product.ImagePath, dest, true);

                    product.ImagePath = dest;
                }

                DefaultList.Add(product);
                Save();
            }
        }
    }
}

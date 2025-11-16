using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using SmartPOS.UI.Models;

namespace SmartPOS.UI.Data
{
    public static class SalesDatabase
    {
        // AppData folder
        private static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartPOS");

        private static readonly string FilePath = Path.Combine(AppFolder, "sales_data.json");

        public static ObservableCollection<SaleRecord> SalesList { get; private set; } = new();

        static SalesDatabase()
        {
            // Ensure AppData directory exists
            if (!Directory.Exists(AppFolder))
                Directory.CreateDirectory(AppFolder);

            // Ensure file exists
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "[]");
        }

        public static void Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath);
                    SalesList = JsonSerializer.Deserialize<ObservableCollection<SaleRecord>>(json)
                                ?? new ObservableCollection<SaleRecord>();
                }
                catch
                {
                    SalesList = new ObservableCollection<SaleRecord>();
                }
            }
        }

        public static void Add(SaleRecord sale)
        {
            SalesList.Add(sale);
            Save();
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(SalesList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Optional: handle errors silently or log
            }
        }
    }
}

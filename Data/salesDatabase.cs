using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using SmartPOS.UI.Models;

namespace SmartPOS.UI.Data
{
    public static class SalesDatabase
    {
        private static readonly string FilePath = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sales_data.json");

        public static ObservableCollection<SaleRecord> SalesList { get; private set; } = new();

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
            var json = JsonSerializer.Serialize(SalesList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}

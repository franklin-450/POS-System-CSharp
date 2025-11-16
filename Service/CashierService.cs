using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SmartPOS.UI.Models;

namespace SmartPOS.UI.Services
{
    public static class CashierService
    {
        // Use AppData folder
        private static readonly string AppFolder = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPOS");

        private static readonly string FilePath = Path.Combine(AppFolder, "cashiers.json");

        static CashierService()
        {
            // Ensure AppData folder exists
            if (!Directory.Exists(AppFolder))
                Directory.CreateDirectory(AppFolder);

            // Ensure file exists
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "[]");
        }

        // Get all cashiers
        public static List<Cashier> GetAll()
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<Cashier>>(json) ?? new List<Cashier>();
            }
            catch
            {
                return new List<Cashier>();
            }
        }

        // Save all cashiers
        public static void SaveAll(List<Cashier> cashiers)
        {
            string json = JsonSerializer.Serialize(cashiers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        // Add a cashier
        public static void Add(Cashier cashier)
        {
            var list = GetAll();
            list.Add(cashier);
            SaveAll(list);
        }
    }
}

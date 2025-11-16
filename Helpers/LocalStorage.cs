using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SmartPOS.UI.Models;

namespace SmartPOS.UI.Helpers
{
    public static class LocalStorage
    {
        // Always use AppData
        public static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartPOS");

        private static readonly string FilePath = Path.Combine(AppFolder, "localdata.txt");
        private static readonly string CashierFilePath = Path.Combine(AppFolder, "cashiers.json");

        static LocalStorage()
        {
            // Ensure AppData directory exists
            if (!Directory.Exists(AppFolder))
                Directory.CreateDirectory(AppFolder);

            // Ensure localdata.txt exists
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "");

            // Ensure cashiers.json exists in AppData
            if (!File.Exists(CashierFilePath))
            {
                // Optional: initialize with default empty array if no embedded default
                File.WriteAllText(CashierFilePath, "[]");
            }

            // Ensure Images folder exists
            string imagesDir = Path.Combine(AppFolder, "Images");
            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);
        }

        public static void Set(string key, string value)
        {
            File.WriteAllText(FilePath, $"{key}={value}");
        }

        public static string Get(string key)
        {
            if (!File.Exists(FilePath))
                return string.Empty;

            string content = File.ReadAllText(FilePath);
            if (content.StartsWith(key + "="))
                return content.Substring(key.Length + 1);

            return string.Empty;
        }

        public static List<CashierModel> LoadCashiers()
        {
            try
            {
                string json = File.ReadAllText(CashierFilePath);
                return JsonSerializer.Deserialize<List<CashierModel>>(json)
                       ?? new List<CashierModel>();
            }
            catch
            {
                return new List<CashierModel>();
            }
        }

        public static void SaveCashiers(List<CashierModel> cashiers)
        {
            string json = JsonSerializer.Serialize(cashiers,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(CashierFilePath, json);
        }
    }
}

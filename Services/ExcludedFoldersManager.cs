using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganiser.Services
{
    public class ExcludedFoldersManager
    {
        private readonly string appDataPath;
        private readonly string filePath;

        public List<string> ExcludedFolders { get; private set; } = new List<string>();

        public ExcludedFoldersManager()
        {
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataOrganiser");
            filePath = Path.Combine(appDataPath, "excludedfolders.json");

            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    ExcludedFolders = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                ExcludedFolders = new List<string>();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(ExcludedFolders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void AddFolder(string folderName)
        {
            if (!ExcludedFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
            {
                ExcludedFolders.Add(folderName);
                SaveSettings();
            }
        }

        public void RemoveFolder(string folderName)
        {
            if (ExcludedFolders.RemoveAll(f => f.Equals(folderName, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SaveSettings();
            }
        }
    }
}

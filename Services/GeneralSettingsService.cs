using DataOrganiser.Models;
using System.IO;
using System.Text.Json;

namespace DataOrganiser.Services
{
    public class GeneralSettingsService
    {
        private readonly string appDataPath;
        private readonly string filePath;

        private GeneralSettingsModel _settings = new();

        public GeneralSettingsService()
        {
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataOrganiser");
            filePath = Path.Combine(appDataPath, "generalsettings.json");

            Initialise();
            LoadSettings();
        }

        private void Initialise()
        {
            Directory.CreateDirectory(appDataPath);

            if (!File.Exists(filePath))
            {
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    SaveSettings();
                    return;
                }

                _settings = JsonSerializer.Deserialize<GeneralSettingsModel>(json) ?? new GeneralSettingsModel();
            }
            catch
            {
                _settings = new GeneralSettingsModel();
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            string json = JsonSerializer.Serialize(
                _settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(filePath, json);
        }

        public void EditRecentDump(string value)
        {
            _settings.RecentDumpDuration = value;
            SaveSettings();
        }

        public DateTime GetRecentDump()
        {
            string value = _settings.RecentDumpDuration;

            return value switch
            {
                "1 hour" => DateTime.Now.AddHours(-1),
                "5 hours" => DateTime.Now.AddHours(-5),
                "12 hours" => DateTime.Now.AddHours(-12),
                "1 day" => DateTime.Now.AddDays(-1),
                "2 days" => DateTime.Now.AddDays(-2),
                "5 days" => DateTime.Now.AddDays(-5),
                "7 days" => DateTime.Now.AddDays(-7),
                "14 days" => DateTime.Now.AddDays(-14),
                _ => DateTime.Now.AddDays(-14)
            };
        }

        //i know this is dumb
        public string GetRecentDumpDuration()
        {
            return _settings.RecentDumpDuration;
        }
    }
}
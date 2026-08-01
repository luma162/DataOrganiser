using DataOrganiser.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace DataOrganiser.Services
{
    public class ExtensionGroupService
    {
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataOrganiser", "extensiongroups.json");

        private readonly ObservableCollection<ExtensionGroupModel> _groups = new();

        public ExtensionGroupService()
        {
            Load();
        }

        public List<ExtensionGroupModel> GetEnabledGroups()
        {
            return _groups.Where(g => g.IsEnabled).ToList();
        }

        public List<ExtensionGroupModel> GetAllExtensionGroups()
        {
            return _groups.ToList();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_groups));
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var data = JsonSerializer.Deserialize<List<ExtensionGroupModel>>(
                    File.ReadAllText(SettingsPath));

                if (data == null)
                    return;

                _groups.Clear();

                foreach (var group in data)
                    _groups.Add(group);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading extension groups: {ex.Message}");
            }
        }

        public void DeleteGroup(ExtensionGroupModel group)
        {
            _groups.Remove(group);
            Save();
        }
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace DataOrganiser.Models
{
    public class ExtensionGroupManager
    {
        public ExtensionGroupManager()
        {
            Load();
        }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DataOrganiser", "extensiongroups.json");

        private static bool _isLoading = false;

        //public static ObservableCollection<ExtensionGroup> Groups { get; } = new ObservableCollection<ExtensionGroup>
        //{
        //    new ExtensionGroup { Name = "Image Files", Extensions = new List<string> { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" } },
        //    new ExtensionGroup { Name = "Video Files", Extensions = new List<string> { ".mp4", ".avi", ".mov", ".mkv", ".wmv" } },
        //    new ExtensionGroup { Name = "Audio Files", Extensions = new List<string> { ".mp3", ".wav", ".aac", ".flac", ".ogg" } },
        //    new ExtensionGroup { Name = "Programming Files", Extensions = new List<string> { ".c", ".cpp", ".cs", ".java", ".py", ".js", ".ts", ".rb", ".php", ".swift", ".go", ".rs", ".kt", ".kts", ".scala", ".vb", ".vbs", ".m", ".mm", ".dart", ".pl", ".lua", ".sh", ".bat", ".ps1", ".r", ".jl", ".fs", ".fsx", ".f90", ".f95", ".asm", ".s", ".sql", ".groovy", ".clj", ".cljs", ".coffee", ".hx", ".nim", ".vala", ".ml", ".mli", ".d", ".ex", ".exs", ".erl", ".hrl", ".tcl", ".scm", ".lisp", ".lsp", ".ada", ".bas", ".zsh", ".awk" } },
        //};

        public static ObservableCollection<ExtensionGroup> Groups { get; } = new ObservableCollection<ExtensionGroup>
        {

        };

        public static IEnumerable<ExtensionGroup> EnabledGroups => Groups.Where(g => g.IsEnabled);

        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Groups));
        }

        public static void Load()
        {
            _isLoading = true;
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var data = JsonSerializer.Deserialize<List<ExtensionGroup>>(File.ReadAllText(SettingsPath));
                if (data == null) return;
                Groups.Clear();
                foreach (var group in data)
                    Groups.Add(group);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public static bool IsLoading => _isLoading;

        private class ExtensionGroupState
        {
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
}
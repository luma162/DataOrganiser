using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataOrganiser
{
    public partial class GeneralSettings : System.Windows.Controls.UserControl
    {
        private readonly string appDataPath;
        private readonly string filePath;
        public string DumpDuraion;

        public GeneralSettings()
        {
            InitializeComponent();
            appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataOrganiser");
            filePath = System.IO.Path.Combine(appDataPath, "generalsettings.json");

            LoadSettings();
        }

        public void RecentDumpDuration_SelectionChanged()
        {
            return;
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
                    //DumpDuration = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                //DumpDuration = new List<string>();
            }
        }
    }
}

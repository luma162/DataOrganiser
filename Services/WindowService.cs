using DataOrganiser.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser.Services
{
    public class WindowService
    {
        public WindowService()
        {

        }

        public void Show()
        {
            var settingsWindow = new Settings();
            settingsWindow.DataContext = new SettingsViewModel();
            settingsWindow.Show();
        }
    }
}

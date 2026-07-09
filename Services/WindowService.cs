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
        private ExtensionGroupService _extensionGroupService;
        private ExcludedFoldersService _excludeFoldersService;
        private GeneralSettingsService _generalSettingsService;

        public WindowService(ExtensionGroupService extensionGroupService, ExcludedFoldersService excludedFoldersService, GeneralSettingsService generalSettingsService)
        {
            _extensionGroupService = extensionGroupService;
            _excludeFoldersService = excludedFoldersService;
            _generalSettingsService = generalSettingsService;
        }

        public void Show()
        {
            var settingsWindow = new Settings();
            settingsWindow.DataContext = new SettingsViewModel(_extensionGroupService, _excludeFoldersService, _generalSettingsService);
            settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
            settingsWindow.Show();
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganiser.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace DataOrganiser.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        //private Indexer _indexer;
        private ExtensionGroupService _extensionGroupService;
        private ExcludedFoldersService _excludeFoldersService;
        private GeneralSettingsService _generalSettingsService;

        public ObservableCollection<string> ExcludedFolders { get; } = new();
        [ObservableProperty] private string? excludedFolderInput;
        //private FileOperationsService _fileOperationsService;

        [ObservableProperty] private object? currentPage;
        [ObservableProperty] private string? recentDumpSelection;

        public List<string> RecentDumpOptions { get; } =
        [
            "1 hour",
            "5 hours",
            "12 hours",
            "1 day",
            "2 days",
            "5 days",
            "7 days",
            "14 days"
        ];

        public SettingsViewModel(ExtensionGroupService extensionGroupService, ExcludedFoldersService excludedFoldersService, GeneralSettingsService generalSettingsService)
        {
            _extensionGroupService = extensionGroupService;
            _excludeFoldersService = excludedFoldersService;
            _generalSettingsService = generalSettingsService;

            foreach (var folder in _excludeFoldersService.ExcludedFolders)
            {
                ExcludedFolders.Add(folder);
            }

            recentDumpSelection = _generalSettingsService.GetRecentDumpDuration();

            CurrentPage = new GeneralSettings();
            //CurrentPage = new ExcludedFolderSettings();
        }

        [RelayCommand]
        private void SideBarContentClick(string page)
        {
            switch (page)
            {
                case "General":
                    CurrentPage = new GeneralSettings();
                    break;

                case "ExcludedFolders":
                    CurrentPage = new ExcludedFolderSettings();
                    break;


                case "ExtensionGroups":
                    CurrentPage = new ExtensionGroups();
                    break;
            }
        }

        partial void OnRecentDumpSelectionChanged(string? value)
        {
            if (value == null)
                return; 

            _generalSettingsService.EditRecentDump(value);
        }

        [RelayCommand]
        private void AddExcludedFolder()
        {
            if (string.IsNullOrWhiteSpace(ExcludedFolderInput))
                return;

            _excludeFoldersService.AddFolder(ExcludedFolderInput);


            if (!ExcludedFolders.Contains(ExcludedFolderInput, StringComparer.OrdinalIgnoreCase))
            {
                ExcludedFolders.Add(ExcludedFolderInput);
            }


            ExcludedFolderInput = "";
        }


        [RelayCommand]
        private void RemoveFolder(string folder)
        {
            _excludeFoldersService.RemoveFolder(folder);

            ExcludedFolders.Remove(folder);
        }
    }
}

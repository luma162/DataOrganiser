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
using DataOrganiser.Models;
using DataOrganiser.Views.Settings;

namespace DataOrganiser.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        //private Indexer _indexer;
        private ExtensionGroupService _extensionGroupService;
        private ExcludedFoldersService _excludeFoldersService;
        private GeneralSettingsService _generalSettingsService;

        public ObservableCollection<string> ExcludedFolders { get; } = new();
        public ObservableCollection<ExtensionGroupModel> ExtensionGroups { get; } = new();

        [ObservableProperty] private string? excludedFolderInput;
        //private FileOperationsService _fileOperationsService;

        [ObservableProperty] private object? currentPage;
        [ObservableProperty] private string? recentDumpSelection;
        [ObservableProperty] private bool isEditingGroup;
        [ObservableProperty] private ExtensionGroupModel? editingGroup;

        [ObservableProperty] private string extensionInput = string.Empty;

        public string ManageGroupTitle => IsEditingGroup ? "Edit Extension Group" : "Add Extension Group";

        public string ManageGroupButtonText => IsEditingGroup ? "Save Changes" : "Add Group";
        
        private ExtensionGroupModel? originalEditingGroup;

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

            foreach (var group in _extensionGroupService.GetAllExtensionGroups())
            {
                ExtensionGroups.Add(group);
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

        [RelayCommand]
        private void AddGroupClick()
        {
            IsEditingGroup = false;

            EditingGroup = new ExtensionGroupModel
            {
                Name = "",
                Extensions = new ObservableCollection<string>(),
                IsEnabled = true
            };

            var manageExtensionGroupsWindow = new ManageExtensionGroups();
            manageExtensionGroupsWindow.DataContext = this;
            manageExtensionGroupsWindow.Owner = System.Windows.Application.Current.MainWindow;
            manageExtensionGroupsWindow.Show();
        }

        [RelayCommand]
        private void DeleteGroupClick(ExtensionGroupModel group)
        {
            _extensionGroupService.DeleteGroup(group);

            ExtensionGroups.Remove(group);
        }

        [RelayCommand]
        private void EditGroupClick(ExtensionGroupModel group)
        {
            IsEditingGroup = true;

            EditingGroup = new ExtensionGroupModel
            {
                Name = group.Name,
                Extensions = new ObservableCollection<string>(group.Extensions),
                IsEnabled = group.IsEnabled
            };

            var manageExtensionGroupsWindow = new ManageExtensionGroups();
            manageExtensionGroupsWindow.DataContext = this;
            manageExtensionGroupsWindow.Owner = System.Windows.Application.Current.MainWindow;
            manageExtensionGroupsWindow.Show();
        }

        partial void OnIsEditingGroupChanged(bool value)
        {
            OnPropertyChanged(nameof(ManageGroupTitle));
            OnPropertyChanged(nameof(ManageGroupButtonText));
        }

        [RelayCommand]
        private void AddExtension()
        {
            if (EditingGroup == null)
                return;

            if (string.IsNullOrWhiteSpace(ExtensionInput))
                return;

            string extension = ExtensionInput.Trim();

            if (!extension.StartsWith("."))
                extension = "." + extension;

            if (!EditingGroup.Extensions.Contains(extension))
            {
                EditingGroup.Extensions.Add(extension);
            }

            ExtensionInput = string.Empty;
        }

        [RelayCommand]
        private void SaveGroup()
        {
            if (EditingGroup == null || string.IsNullOrWhiteSpace(EditingGroup.Name))
                return;

            if (IsEditingGroup)
            {
                _extensionGroupService.Save();
            }
            else
            {
                ExtensionGroups.Add(EditingGroup);
                _extensionGroupService.AddGroup(EditingGroup);
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganiser.Models;
using DataOrganiser.Services;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataOrganiser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private Indexer _indexer;
    private ExtensionGroupService _extensionGroupService;
    private FileOperationsService _fileOperationsService;
    private WindowService _windowService;

    public BulkObservableCollection<IndexedFile> IndexedFiles { get; } = new();
    public BulkObservableCollection<IndexedFolder> IndexedFolders { get; } = new();

    public ObservableCollection<ExtensionButtonModel> ExtensionButtons { get; } = new();
    public ObservableCollection<ExtensionGroupModel> EnabledExtensionGroups {  get; } = new();

    public ListCollectionView FilteredFiles { get; }
    public ListCollectionView FilteredFolders { get; }
    public ListCollectionView FilteredExtensionButtons { get; }

    [ObservableProperty] private string? currentDirectory;
    [ObservableProperty] private Visibility currentDirectoryVisibility;
    [ObservableProperty] private Visibility loadingOverlayVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility searchBarVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility extensionSearchVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility clearButtonVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility fileSearchVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility deleteButtonVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility moveButtonVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility copyButtonVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility recentDumpButtonVisibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility extensionGroupsVisibility = Visibility.Collapsed;

    [ObservableProperty] private bool recentDumpButtonSelected;

    [ObservableProperty] private string? extensionSearchText;
    [ObservableProperty] private string? fileSearchText;

    private string? _scannedDir;
    private ExtensionButtonModel? _allButton;

    private const string AllExtensionKey = "__ALL__"; 

    public MainViewModel(Indexer indexer, FileOperationsService fileOperationsService, ExtensionGroupService extensionGroupService, WindowService windowService)
    {
        _indexer = indexer;
        _fileOperationsService = fileOperationsService;
        _extensionGroupService = extensionGroupService;
        _windowService = windowService;

        FilteredFiles = new ListCollectionView(IndexedFiles);
        FilteredFolders = new ListCollectionView(IndexedFolders);
        FilteredExtensionButtons = new ListCollectionView(ExtensionButtons);

        FilteredFiles.Filter = FilterFiles;
        FilteredFolders.Filter = FilterFolders;
        FilteredExtensionButtons.Filter = FilterExtensionButtons;
    }

    [RelayCommand]
    private async Task ScanButton()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        _scannedDir = dialog.SelectedPath;

        IndexedFiles.Clear();
        IndexedFolders.Clear();

        CurrentDirectory = $"Current Directory: {_scannedDir}";
        CurrentDirectoryVisibility = Visibility.Visible;

        LoadingOverlayVisibility = Visibility.Visible;

        var filesBag = new ConcurrentBag<IndexedFile>();
        var foldersBag = new ConcurrentBag<IndexedFolder>();

        try
        {
            await Task.Run(() =>
            {
                _indexer.IndexDirectory(_scannedDir, filesBag, foldersBag);
            });

            IndexedFiles.Clear();
            IndexedFiles.AddRange(filesBag);

            IndexedFolders.Clear();
            IndexedFolders.AddRange(foldersBag);

            PopulateExtensionButtons();
            PopulateExtensionGroups();

            SearchBarVisibility = Visibility.Visible;
            ExtensionSearchVisibility = Visibility.Visible;
            ClearButtonVisibility = Visibility.Visible;
            FileSearchVisibility = Visibility.Visible;
            DeleteButtonVisibility = Visibility.Visible;
            MoveButtonVisibility = Visibility.Visible;
            CopyButtonVisibility = Visibility.Visible;
            RecentDumpButtonVisibility = Visibility.Visible;
            ExtensionGroupsVisibility = Visibility.Visible;
        }
        finally
        {
            LoadingOverlayVisibility = Visibility.Collapsed;
        }
    }

    private void PopulateExtensionButtons()
    {
        ExtensionButtons.Clear();

        _allButton = new ExtensionButtonModel
        {
            Extension = AllExtensionKey,
            Text = "All",
            IsSelected = true
        };

        ExtensionButtons.Add(_allButton);

        var extensions = IndexedFiles
            .Select(f => f.Extension?.ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

        foreach (var ext in extensions)
        {
            ExtensionButtons.Add(new ExtensionButtonModel
            {
                Extension = ext,
                Text = ext
            });
        }
    }

    private void PopulateExtensionGroups()
    {
        EnabledExtensionGroups.Clear();

        foreach (var group in _extensionGroupService.GetEnabledGroups())
        {
            EnabledExtensionGroups.Add(group);
        }
    }

    private void UpdateDataGrid()
    {
        FilteredFiles.Refresh();
        FilteredFolders.Refresh();
    }

    private bool FilterExtensionButtons(object obj)
    {
        if (obj is not ExtensionButtonModel extensionButton)
            return false;

        if (string.IsNullOrWhiteSpace(ExtensionSearchText))
            return true;

        return extensionButton.Text.Contains(ExtensionSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterFiles(object obj)
    {
        if (obj is not IndexedFile file)
            return false;


        bool matchesExtension;

        if (_allButton == null)
        {
            matchesExtension = true;
        }
        else if (_allButton.IsSelected)
        {
            matchesExtension = true;
        }
        else
        {
            bool foundMatchingSelectedExtension = false;

            foreach (var extensionButton in ExtensionButtons)
            {
                bool buttonIsSelected = extensionButton.IsSelected;

                string? fileExtensionLower = null;
                if (file.Extension != null)
                {
                    fileExtensionLower = file.Extension.ToLower();
                }

                bool extensionsMatch = (extensionButton.Extension == fileExtensionLower);

                if (buttonIsSelected && extensionsMatch)
                {
                    foundMatchingSelectedExtension = true;
                    break;
                }
            }
            matchesExtension = foundMatchingSelectedExtension;
        }


        bool matchesSearch;
        bool searchBoxIsEmpty = string.IsNullOrWhiteSpace(FileSearchText);
        
        if (searchBoxIsEmpty)
        {
            matchesSearch = true;
        }
        else
        {
            bool nameContainsSearchText;

            if (file.Name == null)
            {
                nameContainsSearchText = false;
            }
            else
            {
                nameContainsSearchText = file.Name.Contains(FileSearchText, StringComparison.OrdinalIgnoreCase);
            }

            matchesSearch = nameContainsSearchText;
        }

        bool recentDump;
        var time = DateTime.Now.AddDays(-14);
        // get recent dump option from settings
        // using placeholder value of -14 days for now
        if (RecentDumpButtonSelected)
        {
            recentDump = file.Created >= time;
        }
        else
        {
            recentDump = true;
        }

        return matchesExtension && matchesSearch && recentDump;
    }

    private bool FilterFolders(object obj)
    {
        if (obj is not IndexedFolder folder)
            return false;

        bool matchesSearch = string.IsNullOrWhiteSpace(FileSearchText)
            || (folder.Name?.Contains(FileSearchText, StringComparison.OrdinalIgnoreCase) ?? false);

        return matchesSearch;
    }

    [RelayCommand]
    private void ExtensionClick(ExtensionButtonModel item)
    {
        if (item.Extension == AllExtensionKey)
        {
            foreach (var e in ExtensionButtons)
                e.IsSelected = e == item;

            UpdateDataGrid();
            return;
        }

        if (item.IsSelected)
        {
            if (_allButton is not null)
                _allButton.IsSelected = false;
        }
        else
        {
            bool anySelected = ExtensionButtons
                .Any(e => e.Extension != AllExtensionKey && e.IsSelected);

            if (!anySelected && _allButton is not null)
                _allButton.IsSelected = true;
        }

        UpdateDataGrid();
    }


    [RelayCommand]
    private async Task RefreshButtonClick()
    {
        if (_scannedDir is null)
        {
            System.Windows.MessageBox.Show("No directory to refresh. Please scan a folder first.");
            return;
        }

        CurrentDirectory = $"Current Directory: {_scannedDir}";
        CurrentDirectoryVisibility = Visibility.Visible;
        LoadingOverlayVisibility = Visibility.Visible;

        var filesBag = new ConcurrentBag<IndexedFile>();
        var foldersBag = new ConcurrentBag<IndexedFolder>();

        try
        {
            await Task.Run(() => _indexer.IndexDirectory(_scannedDir, filesBag, foldersBag));

            IndexedFiles.Clear();
            IndexedFiles.AddRange(filesBag);

            IndexedFolders.Clear();
            IndexedFolders.AddRange(foldersBag);

            PopulateExtensionButtons();
        }
        finally
        {
            LoadingOverlayVisibility = Visibility.Collapsed;
        }
    }

    [RelayCommand]
    private async Task CopyButtonClick()
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();

        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var copyDir = dialog.SelectedPath;

        _fileOperationsService.Copy(selectedFiles, selectedFolders, copyDir);

        bool copiedIntoScannedDir = _scannedDir is not null
            && copyDir.StartsWith(_scannedDir, StringComparison.OrdinalIgnoreCase);

        if (copiedIntoScannedDir)
        {
            await RefreshButtonClick();
        }
    }

    [RelayCommand]
    private async Task MoveButtonClick()
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();

        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        if (dialog.SelectedPath == _scannedDir)
        {
            System.Windows.MessageBox.Show("Cannot move files / folders into the same directory");
            return;
        }

        var moveDir = dialog.SelectedPath;

        _fileOperationsService.Move(selectedFiles, selectedFolders, moveDir);

        await RefreshButtonClick();
    }

    [RelayCommand]
    private void DeleteButtonClick() 
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();

        var result = _fileOperationsService.Delete(selectedFiles, selectedFolders);

        foreach(var file in result.RemovedFiles)
        {
            FilteredFiles.Remove(file);
        }

        foreach(var folder in result.RemovedFolders)
        {
             FilteredFolders.Remove(folder);
        }

        UpdateDataGrid();
    }

    [RelayCommand]
    private void RecentDumpButtonClick() 
    {
        UpdateDataGrid();
    }

    [RelayCommand]
    private void ClearButtonClick()
    {
        ExtensionSearchText = "";
    }

    [RelayCommand]
    private void ExtensionGroupClick(ExtensionGroupModel group)
    {
        if (group == null)
            return;

        if (_allButton is not null)
            _allButton.IsSelected = false;

        foreach (string ext in group.Extensions)
        {
            var matchingButton = ExtensionButtons.FirstOrDefault(b => string.Equals(b.Extension, ext, StringComparison.OrdinalIgnoreCase));

            if (matchingButton is not null)
            {
                matchingButton.IsSelected = true;
            }
        }

        UpdateDataGrid();
    }

    [RelayCommand]
    private void ScanFileDirectoryClick() { }

    [RelayCommand]
    private void SettingsButtonClick()
    {
        _windowService.Show();
    }

    [RelayCommand]
    private void OpenFileLocationClick() { }

    [RelayCommand]
    private void OpenFileClick() { }

    [RelayCommand]
    private void OpenFolderLocationClick() { }

    [RelayCommand]
    private void ScanFolderDirectoryClick() { }

    [RelayCommand]
    private void FileDataGridDoubleClick() { }


    partial void OnExtensionSearchTextChanged(string? value)
    {
        FilteredExtensionButtons.Refresh();
    }

    partial void OnFileSearchTextChanged(string? value)
    {
        UpdateDataGrid();
    }

    private List<IndexedFile> GetSelectedFiles()
    {
        return IndexedFiles.Where(f => f.IsSelected).ToList();
    }

    private List<IndexedFolder> GetSelectedFolders()
    {
        return IndexedFolders.Where(f => f.IsSelected).ToList();
    }
}
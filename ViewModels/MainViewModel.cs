using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganiser.Models;
using DataOrganiser.Services;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataOrganiser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ExcludedFoldersManager _excludedFoldersManager;
    private Indexer _indexer;

    public BulkObservableCollection<IndexedFile> IndexedFiles { get; } = new();
    public BulkObservableCollection<IndexedFolder> IndexedFolders { get; } = new();

    public ObservableCollection<ExtensionButtonModel> ExtensionButtons { get; } = new();

    public ListCollectionView FilteredFiles { get; }
    public ListCollectionView FilteredFolders { get; }

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

    public MainViewModel(Indexer indexer)
    {
        _indexer = indexer;

        FilteredFiles = new ListCollectionView(IndexedFiles);
        FilteredFolders = new ListCollectionView(IndexedFolders);

        FilteredFiles.Filter = FilterFiles;
        FilteredFolders.Filter = FilterFolders;
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

    private void UpdateDataGrid()
    {
        FilteredFiles.Refresh();
        FilteredFolders.Refresh();
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

        await ScanDirectoryAsync(_scannedDir);
    }

    private async Task ScanDirectoryAsync(string path)
    {
        CurrentDirectory = $"Current Directory: {path}";
        CurrentDirectoryVisibility = Visibility.Visible;
        LoadingOverlayVisibility = Visibility.Visible;

        var filesBag = new ConcurrentBag<IndexedFile>();
        var foldersBag = new ConcurrentBag<IndexedFolder>();

        try
        {
            await Task.Run(() => _indexer.IndexDirectory(path, filesBag, foldersBag));

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
    private void CopyButtonClick() 
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();

        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        string targetPath = dialog.SelectedPath;

        foreach (var file in selectedFiles)
        {
            try
            {
                string destPath = Path.Combine(targetPath, file.Name);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                string ext = Path.GetExtension(file.Name);
                int count = 1;

                while (File.Exists(destPath))
                {
                    destPath = Path.Combine(targetPath, $"{fileNameWithoutExt} ({count}){ext}");
                    count++;
                }

                File.Copy(file.FullPath, destPath);
                file.IsSelected = false;
            }
            catch (Exception ex)
            { System.Windows.MessageBox.Show($"Failed to copy folder: {file.Name}\n{ex.Message}"); }
        }

        foreach (var folder in selectedFolders)
        {
            try
            {
                string destPath = Path.Combine(targetPath, folder.Name);
                int count = 1;
                while (Directory.Exists(destPath))
                {
                    destPath = Path.Combine(targetPath, $"{folder.Name} ({count})");
                    count++;
                }
                CopyDirectory(folder.FullPath, destPath);
                folder.IsSelected = false;
            }
            catch (Exception ex)
            { System.Windows.MessageBox.Show($"Failed to copy folder: {folder.Name}\n{ex.Message}"); }
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            try 
            { 
                File.Copy(file, destFile, true);
                //TODO edge case handling - user copies to the same dir as scanned, view needs to be updated
                //IndexedFiles.Add(file);
            }
            catch { }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }

        UpdateDataGrid();
    }

    [RelayCommand]
    private void MoveButtonClick() 
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();
    }

    [RelayCommand]
    private void DeleteButtonClick() 
    {
        List<IndexedFile> selectedFiles = GetSelectedFiles();
        List<IndexedFolder> selectedFolders = GetSelectedFolders();

        if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
        {
            System.Windows.MessageBox.Show("No files or folders selected for deletion.");
            return;
        }

        string folderWarning = selectedFolders.Count > 0 ? "\n\nWarning: Deleting a folder will also delete all its contents (files and subfolders)." : "";
        if (System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {selectedFiles.Count} file(s) and {selectedFolders.Count} folder(s)?{folderWarning}",
                "Confirm Delete",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var file in selectedFiles)
        {
            try
            {
                File.Delete(file.FullPath);
                FilteredFiles.Remove(file);
            }
            catch(Exception ex) 
            {
                System.Windows.MessageBox.Show($"Failed to delete file: {file.Name}\n{ex.Message}");
            }
        }
        foreach (var folder in selectedFolders)
        {
            try
            {
                Directory.Delete(folder.FullPath);
                FilteredFiles.Remove(folder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete file: {folder.Name}\n{ex.Message}");
            }
        }

        UpdateDataGrid();
    }

    [RelayCommand]
    private void RecentDumpButtonClick() 
    {
        UpdateDataGrid();
    }

    [RelayCommand]
    private void ClearButtonClick() { }

    [RelayCommand]
    private void ScanFileDirectoryClick() { }

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganiser.Models;
using DataOrganiser.Services;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataOrganiser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    //private readonly FileSystemScanner _scanner;
    //private readonly DialogueService _dialogueService;
    private readonly ExcludedFoldersManager _excludedFoldersManager;
    private Indexer _indexer;

    public ObservableCollection<IndexedFile> IndexedFiles { get; } = new();
    public ObservableCollection<IndexedFolder> IndexedFolders { get; } = new();
    public ObservableCollection<ExtensionButtonModel> ExtensionButtons { get; } = new();

    public ListCollectionView FilteredFiles { get; }
    public ListCollectionView FilteredFolders { get; }




    //scan related observations
    [ObservableProperty]
    private string? currentDirectory;
    [ObservableProperty]
    private Visibility currentDirectoryVisibility;
    [ObservableProperty]
    private Visibility loadingOverlayVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility searchBarVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility extensionSearchVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility clearButtonVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility fileSearchVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility deleteButtonVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility moveButtonVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility copyButtonVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility recentDumpButtonVisibility = Visibility.Collapsed;
    [ObservableProperty]
    private Visibility extensionGroupsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string? extensionSearchText;
    [ObservableProperty]
    private string? fileSearchText;


    //private static readonly System.Windows.Media.Brush BackgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(12, 56, 128));
    //private static readonly System.Windows.Media.Brush BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 31, 46));

    private string? _scannedDir;

    //public MainViewModel(Indexer indexer)
    //{
    //    _indexer = indexer;
    //    FilteredFiles = new ListCollectionView(IndexedFiles);
    //    FilteredFolders = new ListCollectionView(IndexedFolders);

    //    //FilteredFiles = new ListCollectionView(IndexedFiles);
    //    //OnPropertyChanged(nameof(FilteredFiles));
    //    //FilteredFiles.Refresh();

    //    //FilteredFolders = new ListCollectionView(IndexedFolders);
    //    //OnPropertyChanged(nameof(FilteredFolders));
    //    //FilteredFolders.Refresh();
    //}

    // #0C3880 selected extension colour

    public MainViewModel(Indexer indexer)
    {
        _indexer = indexer;

        FilteredFiles = new ListCollectionView(IndexedFiles);
        FilteredFolders = new ListCollectionView(IndexedFolders);
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
            foreach (var f in filesBag)
                IndexedFiles.Add(f);

            IndexedFolders.Clear();
            foreach (var f in foldersBag)
                IndexedFolders.Add(f);

            FilteredFiles.Refresh();
            FilteredFolders.Refresh();

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

        Console.WriteLine($"Files: {IndexedFiles.Count}");
        Console.WriteLine($"Folders: {IndexedFolders.Count}");
    }

    private void PopulateExtensionButtons()
    {
        ExtensionButtons.Clear();

        ExtensionButtons.Add(new ExtensionButtonModel
        {
            Extension = "__ALL__",
            Text = "All",
            IsSelected = true
        });

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

    [RelayCommand]
    private void ExtensionClick(ExtensionButtonModel item)
    {
        if (item.Extension == "__ALL__")
        {
            foreach (var e in ExtensionButtons)
                e.IsSelected = false;

            item.IsSelected = true;
            return;
        }

        var allButton = ExtensionButtons.FirstOrDefault(e => e.Extension == "__ALL__");
        if (allButton is not null) allButton.IsSelected = false;

        item.IsSelected = !item.IsSelected;

        bool anyExtensionSelected = ExtensionButtons.Any(e => e.Extension != "__ALL__" && e.IsSelected);
        if (!anyExtensionSelected && allButton is not null)
            allButton.IsSelected = true;
    }

    //private void UpdateEnabledGroups()
    //{
    //    var enabled = ExtensionGroupManager.Groups.Where(g => g.IsEnabled).ToList();
    //    EnabledExtensionGroups.Clear();
    //    foreach (var group in enabled)
    //        EnabledExtensionGroups.Add(group);
    //    OnPropertyChanged(nameof(EnabledExtensionGroups));
    //}

    [RelayCommand]
    private void SettingsButtonClick()
    {

    }

    [RelayCommand]
    private void RefreshButtonClick()
    {
        
    }

    [RelayCommand]
    private void CopyButtonClick()
    {

    }

    [RelayCommand]
    private void MoveButtonClick()
    {

    }

    [RelayCommand]
    private void DeleteButtonClick()
    {

    }

    [RelayCommand]
    private void RecentDumpButtonClick()
    {

    }

    [RelayCommand]
    private void ClearButtonClick()
    {

    }

    [RelayCommand]
    private void ScanFileDirectoryClick()
    {

    }

    [RelayCommand]
    private void OpenFileLocationClick()
    {

    }

    [RelayCommand]
    private void OpenFileClick()
    {

    }

    [RelayCommand]
    private void OpenFolderLocationClick()
    {

    }

    [RelayCommand]
    private void ScanFolderDirectoryClick()
    {

    }

    [RelayCommand]
    private void FileDataGridDoubleClick()
    {

    }

    partial void OnExtensionSearchTextChanged(string? value)
    {
        
    }

    partial void OnFileSearchTextChanged(string? value)
    {

    }
}


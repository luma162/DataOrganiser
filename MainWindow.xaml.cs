using DataOrganiser.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DataOrganiser;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<IndexedFile> IndexedFiles { get; set; } = new();
    public ObservableCollection<IndexedFile> FilteredFiles { get; set; } = new();
    public ObservableCollection<IndexedFolder> FilteredFolders { get; set; } = new();
    public ObservableCollection<IndexedFolder> IndexedFolders { get; set; } = new();
    private readonly HashSet<string> _selectedExtensions = new();

    private static readonly System.Windows.Media.Color DarkModeColor = System.Windows.Media.Color.FromRgb(35, 35, 37); // #232325
    //private static readonly System.Windows.Media.Color DarkModeColor = System.Windows.Media.Color.FromRgb(25, 31, 46); // #191f2e

    private static readonly System.Windows.Media.Color BlueSelectedColor = System.Windows.Media.Color.FromRgb(45, 137, 239); // #2D89EF
    //private static readonly System.Windows.Media.Color BackgroundColor = System.Windows.Media.Color.FromRgb(44, 63, 102); // #2c3f66
    private static readonly System.Windows.Media.Color BackgroundColor = System.Windows.Media.Color.FromRgb(12, 56, 128); // #2c3f66
    

    //private static readonly System.Windows.Media.Color BorderBrush = System.Windows.Media.Color.FromRgb(59, 59, 80); // #1E64C8
    private static readonly System.Windows.Media.Color BorderBrush = System.Windows.Media.Color.FromRgb(25, 31, 46);


    public ListCollectionView FilteredFilesView { get; private set; }
    public ListCollectionView FilteredFoldersView { get; private set; }
    private FileSystemIndexer _indexer;
    private ExcludedFoldersManager _excludedFoldersManager;

    private Settings? settingsWindow = null;

    public ObservableCollection<ExtensionGroup> EnabledExtensionGroups { get; set; }

    public ICommand FilterByGroupCommand { get; }

    public ObservableCollection<IndexedFile> AllFiles { get; set; }

    private string? _lastScannedRootDirectory;

    private int toggle = 0;

    public MainWindow()
    {
        InitializeComponent();
        FilteredFilesView = new ListCollectionView(IndexedFiles);
        FilteredFoldersView = new ListCollectionView(IndexedFolders);
        FileDataGrid.ItemsSource = FilteredFilesView;
        FolderDataGrid.ItemsSource = FilteredFoldersView;

        // Example: Load your files here
        AllFiles = new ObservableCollection<IndexedFile>(); // Fill this with your indexed files
        FilteredFiles = new ObservableCollection<IndexedFile>(AllFiles);

        FilterByGroupCommand = new RelayCommand<ExtensionGroup>(FilterByGroup);

        // Initialize and subscribe to changes
        EnabledExtensionGroups = new ObservableCollection<ExtensionGroup>(
            ExtensionGroupManager.Groups.Where(g => g.IsEnabled)
        );
        ExtensionGroupManager.Groups.CollectionChanged += (s, e) => UpdateEnabledGroups();
        foreach (var group in ExtensionGroupManager.Groups)
            group.PropertyChanged += (s, e) => { if (e.PropertyName == "IsEnabled") UpdateEnabledGroups(); };

        DataContext = this;
    }
    
    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        _excludedFoldersManager = new ExcludedFoldersManager();
        List<string> ExcludedFolders = _excludedFoldersManager.ExcludedFolders;
        _indexer = new FileSystemIndexer(ExcludedFolders);

        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _lastScannedRootDirectory = dialog.SelectedPath;
            IndexedFiles.Clear();
            FilteredFiles.Clear();
            FilteredFolders.Clear();
            UpdateCurrentDirectoryText(dialog.SelectedPath);

            var filesBag = new ConcurrentBag<IndexedFile>();
            var foldersBag = new ConcurrentBag<IndexedFolder>();

            LoadingOverlay.Visibility = Visibility.Visible;

            await Task.Run(() => _indexer.ScanDirectoryParallel(dialog.SelectedPath, filesBag, foldersBag));

            Dispatcher.Invoke(() =>
            {
                var sortedFiles = filesBag.OrderByDescending(f => f.Modified).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                var sortedFolders = foldersBag.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();

                IndexedFiles.Clear();
                foreach (var f in sortedFiles)
                    IndexedFiles.Add(f);

                FilteredFiles.Clear();
                foreach (var f in IndexedFiles)
                    FilteredFiles.Add(f);

                IndexedFolders.Clear();
                foreach (var folder in sortedFolders)
                    IndexedFolders.Add(folder);

                FilteredFolders.Clear();
                foreach (var folder in IndexedFolders)
                    FilteredFolders.Add(folder);

                //SearchTextBlock.Visibility = Visibility.Visible;
                ExtensionSearchTextBox.Visibility = Visibility.Visible;
                ClearButton.Visibility = Visibility.Visible;
                
                //FileSearchTextBlock.Visibility = Visibility.Visible;
                FileSearchTextBox.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;
                MoveButton.Visibility = Visibility.Visible;
                CopyButton.Visibility = Visibility.Visible;
                RecentDumpButton.Visibility = Visibility.Visible;

                PopulateExtensionButtons();

                // Show extension groups panel after scan
                ExtensionGroupsPanel.Visibility = Visibility.Visible;

                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _excludedFoldersManager = new ExcludedFoldersManager();
        List<string> ExcludedFolders = _excludedFoldersManager.ExcludedFolders;
        _indexer = new FileSystemIndexer(ExcludedFolders); 
        if (string.IsNullOrEmpty(_lastScannedRootDirectory) || !Directory.Exists(_lastScannedRootDirectory))
        {
            System.Windows.MessageBox.Show("No directory to refresh. Please scan a folder first.");
            return;
        }

        var filesBag = new ConcurrentBag<IndexedFile>();
        var foldersBag = new ConcurrentBag<IndexedFolder>();
        await Task.Run(() => _indexer.ScanDirectoryParallel(_lastScannedRootDirectory, filesBag, foldersBag));

        Dispatcher.Invoke(() =>
        {
            var sortedFiles = filesBag.OrderByDescending(f => f.Modified).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var sortedFolders = foldersBag.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();

            IndexedFiles.Clear();
            foreach (var f in sortedFiles)
                IndexedFiles.Add(f);

            FilteredFiles.Clear();
            foreach (var f in IndexedFiles)
                FilteredFiles.Add(f);

            IndexedFolders.Clear();
            foreach (var folder in sortedFolders)
                IndexedFolders.Add(folder);

            FilteredFolders.Clear();
            foreach (var folder in IndexedFolders)
                FilteredFolders.Add(folder);

            PopulateExtensionButtons();

            LoadingOverlay.Visibility = Visibility.Collapsed;
        });
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ExtensionSearchTextBox.Clear();
    }

    private async void ScanFileDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _excludedFoldersManager = new ExcludedFoldersManager();
        List<string> ExcludedFolders = _excludedFoldersManager.ExcludedFolders;
        _indexer = new FileSystemIndexer(ExcludedFolders);
        FileSearchTextBox.Text = string.Empty;
        // run scan function but using directory of the selected file
        if (FileDataGrid.SelectedItem is IndexedFile file)
        {
            _lastScannedRootDirectory = Path.GetDirectoryName(file.FullPath);
            if (!string.IsNullOrEmpty(_lastScannedRootDirectory) && Directory.Exists(_lastScannedRootDirectory))
            {
                IndexedFiles.Clear();
                FilteredFiles.Clear();
                FilteredFolders.Clear();
                UpdateCurrentDirectoryText(_lastScannedRootDirectory);

                var filesBag = new ConcurrentBag<IndexedFile>();
                var foldersBag = new ConcurrentBag<IndexedFolder>();

                await Task.Run(() => _indexer.ScanDirectoryParallel(_lastScannedRootDirectory, filesBag, foldersBag));

                Dispatcher.Invoke(() =>
                {
                    var sortedFiles = filesBag.OrderByDescending(f => f.Modified).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    var sortedFolders = foldersBag.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();

                    IndexedFiles.Clear();
                    foreach (var f in sortedFiles)
                        IndexedFiles.Add(f);

                    FilteredFiles.Clear();
                    foreach (var f in IndexedFiles)
                        FilteredFiles.Add(f);

                    IndexedFolders.Clear();
                    foreach (var folder in sortedFolders)
                        IndexedFolders.Add(folder);

                    FilteredFolders.Clear();
                    foreach (var folder in IndexedFolders)
                        FilteredFolders.Add(folder);

                    PopulateExtensionButtons();

                    LoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Selected file's directory does not exist.");
            }
        }
    }

    private void ScanFolderDirectoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _excludedFoldersManager = new ExcludedFoldersManager();
        List<string> ExcludedFolders = _excludedFoldersManager.ExcludedFolders;
        _indexer = new FileSystemIndexer(ExcludedFolders);
        FileSearchTextBox.Text = string.Empty;
        if (FolderDataGrid.SelectedItem is IndexedFolder folder)
        {
            _lastScannedRootDirectory = folder.FullPath;
            if (!string.IsNullOrEmpty(_lastScannedRootDirectory) && Directory.Exists(_lastScannedRootDirectory))
            {
                IndexedFiles.Clear();
                FilteredFiles.Clear();
                FilteredFolders.Clear();
                UpdateCurrentDirectoryText(_lastScannedRootDirectory);
                var filesBag = new ConcurrentBag<IndexedFile>();
                var foldersBag = new ConcurrentBag<IndexedFolder>();
                LoadingOverlay.Visibility = Visibility.Visible;
                Task.Run(() => _indexer.ScanDirectoryParallel(_lastScannedRootDirectory, filesBag, foldersBag)).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var sortedFiles = filesBag.OrderByDescending(f => f.Modified).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        var sortedFolders = foldersBag.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        IndexedFiles.Clear();
                        foreach (var f in sortedFiles)
                            IndexedFiles.Add(f);
                        FilteredFiles.Clear();
                        foreach (var f in IndexedFiles)
                            FilteredFiles.Add(f);
                        IndexedFolders.Clear();
                        foreach (var folder in sortedFolders)
                            IndexedFolders.Add(folder);
                        FilteredFolders.Clear();
                        foreach (var folder in IndexedFolders)
                            FilteredFolders.Add(folder);
                        PopulateExtensionButtons();
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                    });
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Selected folder does not exist.");
            }
        }
    }

    private static readonly Dictionary<string, int> ExtensionPriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        // Highest priority (most common/general purpose)
        {".pdf", 1},
        {".docx", 1}, {".doc", 1},
        {".xlsx", 1}, {".xls", 1},
        {".pptx", 1}, {".ppt", 1},
        {".txt", 1},
        {".jpg", 1}, {".jpeg", 1}, {".png", 1}, {".gif", 1},
        {".mp3", 1}, {".mp4", 1}, 
        {".zip", 1}, {".exe", 1},
        {".webm", 1}, {".svg", 1}, {".ico", 1}, {".psd", 1},

        // Medium priority (still common but less frequent)
        {".avi", 2}, {".mov", 2}, {".mkv", 2}, {".wmv", 2},
        {".wav", 2}, {".flac", 2}, {".aac", 2},
        {".rar", 2}, {".7z", 2}, {".tar", 2}, {".gz", 2},
        {".csv", 2}, {".md", 2}, {".iso", 2},
        {".html", 2}, {".css", 2}, {".js", 2}, {".lua", 2}, {".luac", 2}, {".py", 2}, {".c", 2}, {".cpp", 2}, {".cc", 2}, {".cxx", 2}, {".h", 2}, {".hpp", 2}, {".hh", 2}, {".java", 2}, {".class", 2}, {".jar", 2}, {".cs", 2}, {".rb", 2}, {".go", 2}, {".rs", 2}, {".php", 2}, {".swift", 2}, {".kt", 2}, {".kts", 2}, {".ktm", 2}, {".m", 2}, {".mm", 2}, {".ts", 2}, {".sh", 2}, {".bash", 2}, {".zsh", 2}, {".bat", 2}, {".cmd", 2}, {".ps1", 2}, {".r", 2}, {".jl", 2}, {".dart", 2}, {".clj", 2}, {".cljs", 2}, {".cljc", 2}, {".scala", 2}, {".sc", 2}, {".ex", 2}, {".exs", 2}, {".erl", 2}, {".hrl", 2}, {".fs", 2}, {".fsx", 2}, {".fsi", 2}, {".vb", 2}, {".sql", 2}, {".asm", 2}, {".s", 2}, {".v", 2}, {".vh", 2}, {".sv", 2}, {".svh", 2}, {".pas", 2}, {".pp", 2}, {".d", 2}, {".nim", 2}, {".groovy", 2}, {".gvy", 2}, {".gy", 2}, {".rkt", 2}, {".scm", 2}, {".ss", 2}, {".ml", 2}, {".mli", 2}, {".pro", 2}, {".tcl", 2}, {".ada", 2}, {".adb", 2}, {".ads", 2}, {".vbs", 2}, {".lisp", 2}, {".lsp", 2}, {".cl", 2}, {".awk", 2}, {".m4", 2}, {".applescript", 2}, {".scpt", 2}, {".purs", 2}, {".re", 2}, {".rei", 2}, {".b", 2}, {".cob", 2}, {".cbl", 2}, {".cpy", 2}, {".bas", 2}, {".f", 2}, {".for", 2}, {".f90", 2}, {".f95", 2}, {".tsx", 2}, {".jsx", 2}, {".hx", 2}, {".idris", 2}, {".agda", 2}, {".dhall", 2}, {".nix", 2}, {".stan", 2}, {".sas", 2}, {".drl", 2}, {".mjs", 2}, {".cjs", 2},


        // Lower priority (less common or niche)
        {".bmp", 3}, {".tiff", 3},
        {".flv", 3}, 
        {".epub", 3},
        {".dmg", 3},
        {".rtf", 3},
        {".dll", 3},
        {".json", 3}, {".xml", 3}, {".yaml", 3}, {".yml", 3},
        {".log", 3}, {".bak", 3}, {".tmp", 3}, {".cache", 3}, {".db", 3}, {".sqlite", 3},
        {".torrent", 3}, {".apk", 3}, {".ipa", 3}, {".appx", 3}, {".msi", 3}, {".pkg", 3}, {".deb", 3}, {".rpm", 3},
        {".vmdk", 3}, {".vdi", 3}, {".ova", 3}, {".ovf", 3},
        {".ai", 3}, {".indd", 3}, {".xd", 3}, {".sketch", 3}, {".fig", 3}, {".c4d", 3}, {".blend", 3},
        {".ttf", 3}, {".otf", 3}, {".woff", 3}, {".woff2", 3}, {".eot", 3}, {".svgz", 3},
        {".srt", 3}, {".vtt", 3}, {".m4a", 3}, {".m4v", 3}, {".3gp", 3}, {".3g2", 3},
        {".opus", 3}, {".aiff", 3}, {".au", 3}, {".mid", 3}, {".midi", 3},
        {".cpl", 3}, {".msc", 3}, {".lnk", 3}, {".url", 3}, {".torrent.meta", 3},
        {".part", 3}, {".crdownload", 3}, {".aria2", 3}, {".download", 3}, {".temp", 3},
        {".old", 3}, {".orig", 3}, {".swp", 3}, {".swo", 3}, {".swn", 3},
        {".pid", 3}, {".seed", 3}, {".version", 3 }, {".lock", 3}, {".pid.lock", 3},
        {".sav", 3}, {".save", 3}, {".backup", 3}, {".oldfile", 3},
    };

    private void PopulateExtensionButtons()
    {
        ExtensionButtonsPanel.Children.Clear();

        // Add "All" button (always dark)
        var allBtn = new System.Windows.Controls.Button
        {
            Content = "All",
            Margin = new Thickness(4),
            Padding = new Thickness(10, 5, 10, 5),
            Height = 30,
            MinWidth = 45,
            Tag = "__ALL__"
        };
        allBtn.Style = (Style)FindResource("FlatButtonStyle");
        allBtn.Background = new SolidColorBrush(BackgroundColor);
        allBtn.BorderBrush = new SolidColorBrush(BorderBrush);
        allBtn.Click += ExtensionButton_Click;
        ExtensionButtonsPanel.Children.Add(allBtn);

        var extensions = IndexedFiles.Select(f => f.Extension.ToLower()).Distinct();

        var shortExts = extensions
            .Where(ext => ext.Length <= 5)
            .OrderBy(ext => ExtensionPriority.ContainsKey(ext) ? ExtensionPriority[ext] : 99)
            .ThenBy(ext => ext)
            .ToList();

        var longExts = extensions
            .Where(ext => ext.Length > 5)
            .OrderBy(ext => ExtensionPriority.ContainsKey(ext) ? ExtensionPriority[ext] : 99)
            .ThenBy(ext => ext)
            .ToList();

        var orderedExtensions = shortExts.Concat(longExts);

        foreach (var ext in orderedExtensions)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = ext,
                Tag = ext,
                Margin = new Thickness(4),
                Padding = new Thickness(13, 5, 13, 5),
                Height = 30,
                MinWidth = 45
            };
            btn.Style = (Style)FindResource("FlatButtonStyle");
            btn.Background = _selectedExtensions.Contains(ext)
                ? new SolidColorBrush(BackgroundColor)
                : new SolidColorBrush(BorderBrush);
            btn.BorderBrush = new SolidColorBrush(BorderBrush);
            btn.Click += ExtensionButton_Click;
            ExtensionButtonsPanel.Children.Add(btn);
        }
    }

    // eventually change this to read from json file
    //private static readonly string[] ExcludedFolders = new[]
    //{
    //    "$Recycle.Bin",
        
    //};

    //public List<string> ExcludedFolders { get; set; } = new List<string>
    //{
    //    "$Recycle.Bin",
    //};

    private void FileDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FileDataGrid.SelectedItem is IndexedFile file)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{file.FullPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open Explorer: {ex.Message}");
            }
        }
    }

    private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (FileDataGrid.SelectedItem is IndexedFile file)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open file: {ex.Message}");
            }
        }
    }

    private void OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (FileDataGrid.SelectedItem is IndexedFile file)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{file.FullPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open Explorer: {ex.Message}");
            }
        }
    }

    private void OpenFolderLocationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (FolderDataGrid.SelectedItem is IndexedFolder folder)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open folder: {folder.Name}\n{ex.Message}");
            }
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFiles = FilteredFiles.Where(f => f.IsSelected).ToList();
        var selectedFolders = FilteredFolders.Where(f => f.IsSelected).ToList();

        if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
        {
            System.Windows.MessageBox.Show("No files or folders selected for deletion.");
            return;
        }

        string folderWarning = selectedFolders.Count > 0
    ? "\n\nWarning: Deleting a folder will also delete all its contents (files and subfolders)."
    : "";

        if (System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {selectedFiles.Count} file(s) and {selectedFolders.Count} folder(s)?{folderWarning}",
                "Confirm Delete",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        // Delete files
        foreach (var file in selectedFiles)
        {
            try
            {
                File.Delete(file.FullPath);
                IndexedFiles.Remove(file);
                FilteredFiles.Remove(file);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete file: {file.Name}\n{ex.Message}");
            }
        }

        // Delete folders recursively
        foreach (var folder in selectedFolders)
        {
            try
            {
                Directory.Delete(folder.FullPath, true);
                FilteredFolders.Remove(folder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete folder: {folder.Name}\n{ex.Message}");
            }
        }
    }

    private void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFiles = FilteredFiles.Where(f => f.IsSelected).ToList();
        var selectedFolders = FilteredFolders.Where(f => f.IsSelected).ToList();

        if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
        {
            System.Windows.MessageBox.Show("No files or folders selected for moving.");
            return;
        }
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string targetPath = dialog.SelectedPath;

            // Move files
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
                    File.Move(file.FullPath, destPath);
                    IndexedFiles.Remove(file);
                    FilteredFiles.Remove(file);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to move file: {file.Name}\n{ex.Message}");
                }
            }

            // Move folders
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
                    Directory.Move(folder.FullPath, destPath);
                    FilteredFolders.Remove(folder);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to move folder: {folder.Name}\n{ex.Message}");
                }
            }
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFiles = FilteredFiles.Where(f => f.IsSelected).ToList();
        var selectedFolders = FilteredFolders.Where(f => f.IsSelected).ToList();

        if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
        {
            System.Windows.MessageBox.Show("No files or folders selected for copying.");
            return;
        }
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string targetPath = dialog.SelectedPath;

            // Copy files
            foreach (var file in selectedFiles)
            {
                try
                {
                    string destPath = Path.Combine(targetPath, file.Name);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                    string ext = Path.GetExtension(file.Name);
                    int count = 1;

                    // Find a unique file name if one already exists
                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(targetPath, $"{fileNameWithoutExt} ({count}){ext}");
                        count++;
                    }

                    File.Copy(file.FullPath, destPath);
                    file.IsSelected = false;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to copy file: {file.Name}\n{ex.Message}");
                }
            }

            // Copy folders recursively
            foreach (var folder in selectedFolders)
            {
                try
                {
                    // Find a unique destination folder name if needed
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
                {
                    System.Windows.MessageBox.Show($"Failed to copy folder: {folder.Name}\n{ex.Message}");
                }
            }
        }
    }

    private void RecentDumpButton_Click(object sender, RoutedEventArgs e)
    {
        var twoWeeksAgo = DateTime.Now.AddDays(-14);

        if (toggle == 0)
        {
            FilteredFilesView.Filter = obj =>
            {
                if (obj is not IndexedFile file) return false;
                return file.Modified >= twoWeeksAgo;
            };
            FilteredFilesView.Refresh();

            FilteredFoldersView.Filter = obj =>
            {
                if (obj is not IndexedFolder folder) return false;
                return folder.Created >= twoWeeksAgo;
            };
            FilteredFoldersView.Refresh();

            toggle = 1;
        }
        else if (toggle == 1)
        {
            FilteredFilesView.Filter = null;
            FilteredFilesView.Refresh();

            FilteredFoldersView.Filter = null;
            FilteredFoldersView.Refresh();

            toggle = 0;
        }
    }

    // Helper method for recursive folder copy
    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private void UpdateCurrentDirectoryText(string yourDirectoryPath)
    {
        if (CurrentDirectoryTextBlock != null)
        {
            CurrentDirectoryTextBlock.Visibility = Visibility.Visible;
            CurrentDirectoryTextBlock.Text = "Current Directory: " + yourDirectoryPath;
            CurrentDirectoryBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#232a3a"));
        }
        else
        {
            throw new InvalidOperationException("CurrentDirectoryTextBlock is not defined in the XAML.");
        }
    }

    private void ExtensionSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string search = ExtensionSearchTextBox.Text.Trim().ToLower();

        // If search is empty, show all extensions
        if (string.IsNullOrEmpty(search))
        {
            PopulateExtensionButtons();
            return;
        }

        // Filter extensions based on search
        var filteredExtensions = IndexedFiles
            .Select(f => f.Extension.ToLower())
            .Distinct()
            .Where(ext => ext.Contains(search))
            .OrderBy(ext => ExtensionPriority.ContainsKey(ext) ? ExtensionPriority[ext] : 99)
            .ThenBy(ext => ext);

        ExtensionButtonsPanel.Children.Clear();

        // Add "All" button (always dark)
        var allBtn = new System.Windows.Controls.Button
        {
            Content = "All",
            Margin = new Thickness(4),
            Padding = new Thickness(13, 5, 13, 5),
            Height = 30,
            MinWidth = 45,
            Tag = "__ALL__"
        };
        allBtn.Style = (Style)FindResource("FlatButtonStyle");
        allBtn.Background = new SolidColorBrush(BackgroundColor);
        allBtn.BorderBrush = new SolidColorBrush(BorderBrush);
        allBtn.Click += ExtensionButton_Click;
        ExtensionButtonsPanel.Children.Add(allBtn);

        foreach (var ext in filteredExtensions)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = ext,
                Tag = ext,
                Margin = new Thickness(4),
                Padding = new Thickness(13, 5, 13, 5),
                Height = 30,
                MinWidth = 45
            };
            btn.Style = (Style)FindResource("FlatButtonStyle");
            btn.Background = _selectedExtensions.Contains(ext)
                ? new SolidColorBrush(BackgroundColor)
                : new SolidColorBrush(BorderBrush);
            btn.BorderBrush = new SolidColorBrush(BorderBrush);
            btn.Click += ExtensionButton_Click;
            ExtensionButtonsPanel.Children.Add(btn);
        }
    }

    private void FileSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFileFilter();
        UpdateFolderFilter();
    }

    private void ExtensionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            var ext = btn.Tag as string;
            if (ext == "__ALL__")
            {
                _selectedExtensions.Clear();
            }
            else
            {
                if (_selectedExtensions.Contains(ext))
                    _selectedExtensions.Remove(ext);
                else
                    _selectedExtensions.Add(ext);
            }

            // Prune extensions that aren't present in the current scan
            PruneSelectedExtensions();

            // Update all button backgrounds and borders
            foreach (System.Windows.Controls.Button b in ExtensionButtonsPanel.Children)
            {
                var tag = b.Tag as string;
                if (tag == "__ALL__")
                {
                    b.Background = new SolidColorBrush(BackgroundColor); // always dark
                    b.BorderBrush = new SolidColorBrush(BorderBrush);
                }
                else
                {
                    b.Background = _selectedExtensions.Contains(tag)
                        ? new SolidColorBrush(BackgroundColor) // selected blue
                        : new SolidColorBrush(BorderBrush); // dark for unselected
                    b.BorderBrush = new SolidColorBrush(BorderBrush);
                }
            }

            UpdateFileFilter();
        }
    }

    private void FilterFilesByExtensions()
    {
        UpdateFileFilter();
    }

    private void UpdateFileFilter()
    {
        string search = FileSearchTextBox.Text.Trim().ToLower();
        bool hasExtensionFilter = _selectedExtensions.Count > 0;

        FilteredFilesView.Filter = obj =>
        {
            if (obj is not IndexedFile file) return false;

            bool matchesExtension = !hasExtensionFilter || _selectedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
            bool matchesName = string.IsNullOrEmpty(search) || (file.Name != null && file.Name.ToLower().Contains(search));
            return matchesExtension && matchesName;
        };
        FilteredFilesView.Refresh();
    }

    private void UpdateFolderFilter()
    {
        string search = FileSearchTextBox.Text.Trim().ToLower();
        FilteredFoldersView.Filter = obj =>
        {
            if (obj is not IndexedFolder folder) return false;
            return string.IsNullOrEmpty(search) || (folder.Name != null && folder.Name.ToLower().Contains(search));
        };
        FilteredFoldersView.Refresh();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (settingsWindow == null)
        {
            settingsWindow = new Settings();
            settingsWindow.Owner = this;
            settingsWindow.Closed += (s, args) => settingsWindow = null;
            settingsWindow.Show();
        }
        else
        {
            settingsWindow.Activate();
            return;
        }
    }

    private void FilterByGroup(ExtensionGroup group)
    {
        var filtered = AllFiles.Where(f => group.Extensions.Contains(f.Extension.ToLower())).ToList();
        FilteredFiles.Clear();
        foreach (var file in filtered)
            FilteredFiles.Add(file);
    }

    private void UpdateEnabledGroups()
    {
        var enabled = ExtensionGroupManager.Groups.Where(g => g.IsEnabled).ToList();
        EnabledExtensionGroups.Clear();
        foreach (var group in enabled)
            EnabledExtensionGroups.Add(group);
        OnPropertyChanged(nameof(EnabledExtensionGroups));
    }

    private void ExtensionGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is ExtensionGroup group)
        {
            // Check if all group extensions are already selected
            bool allSelected = group.Extensions.All(ext => _selectedExtensions.Contains(ext.ToLower()));

            if (allSelected)
            {
                // Remove all group extensions from the selected set (untick)
                foreach (var ext in group.Extensions)
                    _selectedExtensions.Remove(ext.ToLower());
            }
            else
            {
                // Add all group extensions to the selected set (tick)
                foreach (var ext in group.Extensions)
                    _selectedExtensions.Add(ext.ToLower());
            }

            // Prune extensions that aren't present in the current scan
            PruneSelectedExtensions();

            // Update extension button visuals
            UpdateExtensionButtonStates();

            // Filter files by the selected extensions
            FilterFilesByExtensions();
        }
    }

    // Helper to update extension button backgrounds
    private void UpdateExtensionButtonStates()
    {
        foreach (System.Windows.Controls.Button b in ExtensionButtonsPanel.Children)
        {
            var tag = b.Tag as string;
            if (tag == "__ALL__")
            {
                b.Background = new SolidColorBrush(BackgroundColor);
                b.BorderBrush = new SolidColorBrush(BorderBrush);
            }
            else
            {
                b.Background = _selectedExtensions.Contains(tag)
                    ? new SolidColorBrush(BackgroundColor)
                    : new SolidColorBrush(BorderBrush);
                b.BorderBrush = new SolidColorBrush(BorderBrush);
            }
        }
    }

    private void RefreshExtensionGroupReferences()
    {
        EnabledExtensionGroups = new ObservableCollection<ExtensionGroup>(
            ExtensionGroupManager.Groups.Where(g => g.IsEnabled)
        );
        ExtensionGroupManager.Groups.CollectionChanged += (s, e) => UpdateEnabledGroups();
        foreach (var group in ExtensionGroupManager.Groups)
            group.PropertyChanged += (s, e) => { if (e.PropertyName == "IsEnabled") UpdateEnabledGroups(); };
        OnPropertyChanged(nameof(EnabledExtensionGroups));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void PruneSelectedExtensions()
    {
        var presentExtensions = IndexedFiles.Select(f => f.Extension.ToLower()).ToHashSet();
        _selectedExtensions.RemoveWhere(ext => !presentExtensions.Contains(ext));
    }
}

// Simple RelayCommand implementation
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    public RelayCommand(Action<T> execute) => _execute = execute;
    public bool CanExecute(object parameter) => true;
    public void Execute(object parameter) => _execute((T)parameter);
    public event EventHandler CanExecuteChanged;
}

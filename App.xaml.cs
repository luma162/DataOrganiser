using DataOrganiser.Models;
using DataOrganiser.Services;
using DataOrganiser.ViewModels;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace DataOrganiser;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        //var scanner = new FileSystemScanner();
        //var dialogueService = new DialogueService();
        var excludedFoldersManager = new ExcludedFoldersManager();
        var indexer = new Indexer(excludedFoldersManager);
        var mainViewModel = new MainViewModel(indexer);

        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainWindow.Show();

    }
}




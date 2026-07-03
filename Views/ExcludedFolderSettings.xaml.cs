using DataOrganiser.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
    public partial class ExcludedFolderSettings : System.Windows.Controls.UserControl
    {
        //private List<string> _excludedFolders;
        private ExcludedFoldersManager _excludedFoldersManager;
        public ObservableCollection<string> ExcludedFolders { get; set; }

        public ExcludedFolderSettings(List<string> excludedFolders)
        {
            InitializeComponent();
            _excludedFoldersManager = new ExcludedFoldersManager();
            ExcludedFolders = new ObservableCollection<string>(_excludedFoldersManager.ExcludedFolders ?? new List<string>());
            DataContext = this;
            //this._excludedFolders = excludedFolders ?? new List<string>();
        }

        public ExcludedFolderSettings()
        {
            InitializeComponent();
            _excludedFoldersManager = new ExcludedFoldersManager();
            ExcludedFolders = new ObservableCollection<string>(_excludedFoldersManager.ExcludedFolders ?? new List<string>());
            DataContext = this;
        }

        private void AddExcludedFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ExcludedFoldersTextBox.Text) && !ExcludedFolders.Contains(ExcludedFoldersTextBox.Text))
            {
                _excludedFoldersManager.AddFolder(ExcludedFoldersTextBox.Text);
                ExcludedFolders.Add(ExcludedFoldersTextBox.Text);
                ExcludedFoldersTextBox.Clear();
            }
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string folder)
            {
                if (ExcludedFolders.Contains(folder))
                {
                    ExcludedFolders.Remove(folder);
                    _excludedFoldersManager.RemoveFolder(folder); // Also update JSON
                }
            }
        }


        private void ExcludedFoldersTextBox_TextBoxChanged(object sender, TextChangedEventArgs e)
        {
            return;
        }
    }
}

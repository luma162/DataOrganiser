using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganiser.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private Indexer _indexer;
        private ExtensionGroupService _extensionGroupService;
        private FileOperationsService _fileOperationsService;

        public SettingsViewModel()
        {

        }
    }
}

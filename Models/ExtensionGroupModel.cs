using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganiser.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DataOrganiser.Models
{
    public partial class ExtensionGroupModel : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> extensions = new();

        [ObservableProperty]
        private bool isEnabled;

        public string ExtensionsDisplay => string.Join(", ", Extensions);

        public ExtensionGroupModel()
        {
            Extensions.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ExtensionsDisplay));
            };
        }
    }
}
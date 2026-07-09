using DataOrganiser.Services;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganiser.Models
{
    public partial class ExtensionGroupModel : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private List<string> extensions = new();

        [ObservableProperty]
        private bool isEnabled;

        public string ExtensionsDisplay => string.Join(", ", extensions);
    }
}
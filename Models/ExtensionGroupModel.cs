using DataOrganiser.Services;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataOrganiser.Models
{
    public class ExtensionGroupModel : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Name { get; set; }
        public List<string> Extensions { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }

        public string ExtensionsDisplay => string.Join(", ", Extensions);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
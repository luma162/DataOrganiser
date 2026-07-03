using System.Collections.Generic;
using System.ComponentModel;

namespace DataOrganiser.Models
{
    public class ExtensionGroup : INotifyPropertyChanged
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
                    if (!DataOrganiser.Models.ExtensionGroupManager.IsLoading)
                        DataOrganiser.Models.ExtensionGroupManager.Save();
                }
            }
        }

        public string ExtensionsDisplay => string.Join(", ", Extensions);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
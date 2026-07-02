using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser
{
    public class IndexedFolder : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public string Name { get; set; }
        public string FullPath { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

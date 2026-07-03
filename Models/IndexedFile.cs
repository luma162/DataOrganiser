using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DataOrganiser
{
    public class IndexedFile : INotifyPropertyChanged
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
        public string Extension { get; set; }
        public double SizeBytes { get; set; }
        public double SizeKB => Math.Round(SizeBytes / 1024, 2);
        public double SizeMB => Math.Round(SizeBytes / (1024 * 1024), 2);
        public string SizeMBDisplay => $"{SizeMB} MB";
        public double SizeGB => Math.Round(SizeBytes / (1024 * 1024 * 1024), 2);
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

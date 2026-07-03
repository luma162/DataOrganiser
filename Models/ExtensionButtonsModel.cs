using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser.Models
{
    public class ExtensionButtonModel
    {
        public string Extension { get; set; }
        public string Text { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
            }
        }
    }
}

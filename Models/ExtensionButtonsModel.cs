using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganiser.Models
{
    public partial class ExtensionButtonModel : ObservableObject
    {
        public string Extension { get; set; } = "";
        public string Text { get; set; } = "";

        [ObservableProperty]
        private bool isSelected;
    }
}

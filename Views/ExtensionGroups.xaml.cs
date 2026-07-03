using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using DataOrganiser.Models;

namespace DataOrganiser
{
    public partial class ExtensionGroups : System.Windows.Controls.UserControl
    {
        public ExtensionGroups()
        {
            InitializeComponent();
            DataContext = ExtensionGroupManager.Groups;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            return;
            // open edit window
            // for edit window:
            // access extensiongroupmanager and start to decode the json file
            // allow changes by use of entering extension name and add button
            // have remove button for each extension in the group
            // each add or remove updates the json file (no need for save button)
        }
    }
}

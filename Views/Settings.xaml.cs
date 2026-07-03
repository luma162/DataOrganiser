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
using System.Windows.Shapes;

namespace DataOrganiser
{
    public partial class Settings : Window
    {
        private readonly List<(Border Border, UIElement Content)> _sidebarItems;

        private static readonly System.Windows.Media.Brush SelectedBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 137, 239)); // #2D89EF
        public Settings()
        {

            InitializeComponent();
            
            var button1Content = new ExcludedFolderSettings();
            var button2Content = new ExtensionGroups();
            var button3Content = new GeneralSettings();

            _sidebarItems = new List<(Border, UIElement)>
            {
                (Border1, button1Content),
                (Border2, button2Content),
                (Border3, button3Content)
            };

            Border3.BorderBrush = SelectedBrush;
            ContentArea.Content = button3Content;
        }

        private void SidebarBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder)
            {
                foreach (var (border, content) in _sidebarItems)
                {
                    border.BorderBrush = border == clickedBorder ? SelectedBrush : System.Windows.Media.Brushes.Transparent;
                    if (border == clickedBorder)
                        ContentArea.Content = content;
                }
            }
        }
    }
}

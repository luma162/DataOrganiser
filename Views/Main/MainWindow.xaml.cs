using DataOrganiser.Models;
using DataOrganiser.Services;
using DataOrganiser.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DataOrganiser;

public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();

    }

    private void FileDataGridDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        
    }

}

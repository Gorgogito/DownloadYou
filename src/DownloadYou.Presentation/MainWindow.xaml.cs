using System.Windows;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

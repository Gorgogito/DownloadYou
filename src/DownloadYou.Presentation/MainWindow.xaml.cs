using System.Windows;
using DownloadYou.Presentation.ViewModels;
using Microsoft.Win32;

namespace DownloadYou.Presentation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Carpeta de destino",
            InitialDirectory = _viewModel.TargetFolder
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.TargetFolder = dialog.FolderName;
        }
    }
}

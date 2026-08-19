using System.Windows;
using DownloadYou.Presentation.ViewModels;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
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

using System.Windows;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.ViewModels;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly SettingsService _settingsService;

    public MainWindow(MainViewModel viewModel, SettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = settingsService;
        DataContext = viewModel;

        if (_settingsService.Current.ThemePreference == ThemePreference.System)
        {
            SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
        }

        _settingsService.SettingsChanged += settings => ApplySystemThemeWatch(settings.ThemePreference);
    }

    private void ApplySystemThemeWatch(ThemePreference preference)
    {
        // UnWatch exige que la ventana ya esté cargada; en el arranque (constructor) alcanza con
        // no llamar a Watch cuando la preferencia no es System, sin necesidad de desuscribir nada.
        if (preference == ThemePreference.System)
        {
            SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
        }
        else if (IsLoaded)
        {
            SystemThemeWatcher.UnWatch(this);
        }
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

    private void BrowseSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Carpeta de descargas por defecto",
            InitialDirectory = _viewModel.Settings.DownloadFolder
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.Settings.SetDownloadFolder(dialog.FolderName);
        }
    }
}

using DownloadYou.Domain.Enums;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation;

/// <summary>Aplica un ThemePreference guardado usando la API real de WPF-UI (compartido entre el arranque y la vista previa en vivo de Configuración).</summary>
public static class ThemeApplier
{
    public static void Apply(ThemePreference preference)
    {
        switch (preference)
        {
            case ThemePreference.Light:
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica, true);
                break;
            case ThemePreference.Dark:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                break;
        }
    }
}

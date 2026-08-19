using System.Text.RegularExpressions;

namespace DownloadYou.Application.Services;

/// <summary>
/// Resuelve la plantilla de nombre de archivo del usuario y sanea el resultado
/// (caracteres inválidos en Windows, longitud máxima) — ver §10 y §12 del documento
/// de arquitectura. Nunca se interpola el título del video sin pasar por aquí.
/// </summary>
public static partial class FileNameTemplateEngine
{
    private const int MaxBaseNameLength = 150;
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public static string Resolve(string template, string title, string author, string quality, string ext)
    {
        var raw = template
            .Replace("{title}", title)
            .Replace("{author}", author)
            .Replace("{quality}", quality)
            .Replace("{ext}", ext);

        return Sanitize(raw);
    }

    public static string Sanitize(string fileName)
    {
        // Ojo: NO usar Path.GetFileNameWithoutExtension/GetExtension aquí — tratan '/' y '\'
        // como separadores de ruta y truncan silenciosamente todo lo anterior. El texto de
        // entrada (título del video, etc.) todavía no es un segmento de ruta válido, así que
        // el reemplazo de caracteres inválidos debe ir primero, con manipulación de string pura.
        var cleaned = fileName;
        foreach (var invalidChar in InvalidChars)
        {
            cleaned = cleaned.Replace(invalidChar, '_');
        }

        cleaned = WhitespaceRun().Replace(cleaned, " ").Trim();

        var lastDot = cleaned.LastIndexOf('.');
        var (baseName, ext) = lastDot > 0 ? (cleaned[..lastDot], cleaned[lastDot..]) : (cleaned, string.Empty);

        if (baseName.Length > MaxBaseNameLength)
        {
            baseName = baseName[..MaxBaseNameLength].TrimEnd();
        }

        return (string.IsNullOrEmpty(baseName) ? "descarga" : baseName) + ext;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}

using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

public static class DestinationPathResolver
{
    /// <summary>
    /// "No descargar" para Skip ya lo decide DownloadService antes de arrancar la descarga
    /// (ve si el destino ya existe). Este método solo corre después de descargar/convertir,
    /// así que si llega acá con Skip y el destino ya existe (carrera con otro proceso desde
    /// el chequeo inicial), se trata igual que Rename para no perder ni pisar el archivo.
    /// </summary>
    public static string ResolveCollision(string path, ExistingFileBehavior behavior)
    {
        if (behavior == ExistingFileBehavior.Overwrite || !File.Exists(path))
        {
            return path;
        }

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{baseName} ({i}){ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}

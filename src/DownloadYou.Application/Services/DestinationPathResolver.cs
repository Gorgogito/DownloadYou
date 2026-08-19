using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

public static class DestinationPathResolver
{
    /// <summary>
    /// Skip se trata como Rename hasta que la Fase 9 (Configuración) lo exponga en la UI;
    /// decidir "no descargar" pertenece a un paso previo a la descarga, no a este.
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

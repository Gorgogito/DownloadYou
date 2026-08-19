namespace DownloadYou.Application.Abstractions;

public sealed class ExternalToolNotFoundException(ExternalTool tool, IReadOnlyList<string> searchedLocations)
    : Exception($"No se encontró el ejecutable de '{tool}'. Ubicaciones revisadas: {string.Join(", ", searchedLocations)}")
{
    public ExternalTool Tool { get; } = tool;
    public IReadOnlyList<string> SearchedLocations { get; } = searchedLocations;
}

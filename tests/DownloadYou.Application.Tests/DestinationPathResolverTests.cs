using DownloadYou.Application.Services;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class DestinationPathResolverTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("dy-resolver-").FullName;

    [Fact]
    public void ResolveCollision_ReturnsSamePath_WhenNothingExistsYet()
    {
        var path = Path.Combine(_dir, "video.mp4");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Rename);

        Assert.Equal(path, result);
    }

    [Fact]
    public void ResolveCollision_Overwrite_ReturnsSamePath_EvenWhenFileExists()
    {
        var path = Path.Combine(_dir, "video.mp4");
        File.WriteAllText(path, "existente");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Overwrite);

        Assert.Equal(path, result);
    }

    [Fact]
    public void ResolveCollision_Rename_AppendsIndexTwo_WhenFileExists()
    {
        var path = Path.Combine(_dir, "video.mp4");
        File.WriteAllText(path, "existente");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Rename);

        Assert.Equal(Path.Combine(_dir, "video (2).mp4"), result);
    }

    [Fact]
    public void ResolveCollision_Rename_SkipsIndexesAlreadyTaken()
    {
        var path = Path.Combine(_dir, "video.mp4");
        File.WriteAllText(path, "existente");
        File.WriteAllText(Path.Combine(_dir, "video (2).mp4"), "existente");
        File.WriteAllText(Path.Combine(_dir, "video (3).mp4"), "existente");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Rename);

        Assert.Equal(Path.Combine(_dir, "video (4).mp4"), result);
    }

    [Fact]
    public void ResolveCollision_Skip_BehavesLikeRename_AsARaceConditionFallback()
    {
        // El caso normal de Skip ya lo intercepta DownloadService antes de descargar;
        // esto solo cubre la carrera rara en la que el archivo aparece justo después.
        var path = Path.Combine(_dir, "video.mp4");
        File.WriteAllText(path, "existente");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Skip);

        Assert.Equal(Path.Combine(_dir, "video (2).mp4"), result);
    }

    [Fact]
    public void ResolveCollision_PreservesDirectoryAndExtension()
    {
        var path = Path.Combine(_dir, "Mi Video [1080p].mp4");
        File.WriteAllText(path, "existente");

        var result = DestinationPathResolver.ResolveCollision(path, ExistingFileBehavior.Rename);

        Assert.Equal(_dir, Path.GetDirectoryName(result));
        Assert.Equal(".mp4", Path.GetExtension(result));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}

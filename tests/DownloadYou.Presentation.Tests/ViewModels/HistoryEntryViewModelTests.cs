using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation.Tests.ViewModels;

public class HistoryEntryViewModelTests
{
    private static HistoryRecord BuildRecord(
        DownloadKind kind = DownloadKind.Video, JobStatus status = JobStatus.Completed, bool isFavorite = false) => new(
        Guid.NewGuid(), "https://youtu.be/x", "Mi Video", DateTimeOffset.UtcNow, kind,
        "137", "1080p", @"C:\Videos\Mi Video.mp4", status, TimeSpan.FromSeconds(30), isFavorite);

    [Theory]
    [InlineData(DownloadKind.Video, "Video")]
    [InlineData(DownloadKind.AudioMp3, "Audio MP3")]
    public void KindLabel_ReflectsDownloadKind(DownloadKind kind, string expected)
    {
        var vm = new HistoryEntryViewModel(BuildRecord(kind: kind));

        Assert.Equal(expected, vm.KindLabel);
    }

    [Theory]
    [InlineData(JobStatus.Completed, "Completado")]
    [InlineData(JobStatus.Failed, "Con error")]
    [InlineData(JobStatus.Canceled, "Cancelado")]
    [InlineData(JobStatus.Downloading, "Downloading")]
    public void StatusLabel_MapsKnownStatuses_AndFallsBackToRawName(JobStatus status, string expected)
    {
        var vm = new HistoryEntryViewModel(BuildRecord(status: status));

        Assert.Equal(expected, vm.StatusLabel);
    }

    [Fact]
    public void IsCompleted_TrueOnlyWhenStatusIsCompleted()
    {
        Assert.True(new HistoryEntryViewModel(BuildRecord(status: JobStatus.Completed)).IsCompleted);
        Assert.False(new HistoryEntryViewModel(BuildRecord(status: JobStatus.Failed)).IsCompleted);
    }

    [Fact]
    public void IsFavorite_InitializedFromRecord() =>
        Assert.True(new HistoryEntryViewModel(BuildRecord(isFavorite: true)).IsFavorite);

    [Fact]
    public void Id_And_Title_DelegateToRecord()
    {
        var record = BuildRecord();
        var vm = new HistoryEntryViewModel(record);

        Assert.Equal(record.Id, vm.Id);
        Assert.Equal(record.Title, vm.Title);
    }
}

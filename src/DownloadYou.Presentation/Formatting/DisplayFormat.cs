namespace DownloadYou.Presentation.Formatting;

public static class DisplayFormat
{
    public static string Duration(TimeSpan duration) =>
        duration.Hours > 0 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"mm\:ss");

    public static string Speed(double? bytesPerSecond) =>
        bytesPerSecond is null ? string.Empty : $"{Size(bytesPerSecond.Value)}/s";

    public static string Size(long? downloaded, long? total)
    {
        if (downloaded is null)
        {
            return string.Empty;
        }

        return total is null
            ? Size(downloaded.Value)
            : $"{Size(downloaded.Value)} / {Size(total.Value)}";
    }

    public static string Size(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }

    public static string Eta(TimeSpan? eta) => eta is null ? string.Empty : Duration(eta.Value);
}

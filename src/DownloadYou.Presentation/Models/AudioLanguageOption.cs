namespace DownloadYou.Presentation.Models;

/// <summary>Una pista de idioma de audio ofrecida por el video analizado (ver MainViewModel.AvailableAudioLanguages).</summary>
public sealed record AudioLanguageOption(string Code, string DisplayName, bool IsOriginal);

namespace DownloadYou.Presentation.Formatting;

/// <summary>Nombres legibles para los códigos de idioma que yt-dlp reporta en pistas de audio multi-idioma (doblajes).</summary>
public static class LanguageNames
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "Inglés",
        ["es"] = "Español",
        ["es-419"] = "Español (Latinoamérica)",
        ["es-es"] = "Español (España)",
        ["fr"] = "Francés",
        ["pt"] = "Portugués",
        ["pt-br"] = "Portugués (Brasil)",
        ["de"] = "Alemán",
        ["it"] = "Italiano",
        ["ru"] = "Ruso",
        ["ja"] = "Japonés",
        ["ko"] = "Coreano",
        ["zh"] = "Chino",
        ["zh-hans"] = "Chino (simplificado)",
        ["zh-hant"] = "Chino (tradicional)",
        ["ar"] = "Árabe",
        ["hi"] = "Hindi",
        ["tr"] = "Turco",
        ["pl"] = "Polaco",
        ["nl"] = "Neerlandés",
        ["sv"] = "Sueco",
        ["id"] = "Indonesio",
        ["th"] = "Tailandés",
        ["vi"] = "Vietnamita",
        ["uk"] = "Ucraniano",
        ["cs"] = "Checo",
        ["el"] = "Griego",
        ["he"] = "Hebreo",
        ["ro"] = "Rumano",
        ["hu"] = "Húngaro",
    };

    /// <summary>Devuelve el nombre legible del idioma, o el código crudo si no está en la lista conocida.</summary>
    public static string Resolve(string code) => Names.TryGetValue(code, out var name) ? name : code;
}

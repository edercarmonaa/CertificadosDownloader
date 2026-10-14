namespace CertificadosDownloader.Services;

internal static class OutputFileName
{
    public static string ParaFolio(string? folio, string extension, int certificadoId)
    {
        var invalidos = Path.GetInvalidFileNameChars().Append('/').Append('\\').ToHashSet();
        var nombre = new string((folio ?? string.Empty)
            .Trim()
            .Select(caracter => invalidos.Contains(caracter) ? '_' : caracter)
            .ToArray())
            .Trim('.', ' ');

        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = $"certificado_{certificadoId}";
        }

        return $"{nombre[..Math.Min(nombre.Length, 120)]}{extension}";
    }
}

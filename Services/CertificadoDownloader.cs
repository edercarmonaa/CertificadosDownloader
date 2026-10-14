using System.Text.Json;
using CertificadosDownloader.Models;

namespace CertificadosDownloader.Services;

public class CertificadoDownloader
{
    private readonly HttpClient _httpClient;
    private readonly string _carpetaDestino;
    private readonly string _logPath;
    private readonly Uri _urlValidacion;
    private readonly Uri _urlImpresion;
    private readonly Uri _urlCalificaciones;
    private readonly IReadOnlySet<int> _nivelesConCalificaciones;
    private readonly List<Certificado> _certificadosExitosos = new();

    public CertificadoDownloader(
        string carpetaDestino,
        Uri urlValidacion,
        Uri urlImpresion,
        Uri urlCalificaciones,
        HttpClient httpClient,
        IReadOnlySet<int> nivelesConCalificaciones)
    {
        _carpetaDestino = carpetaDestino;
        _logPath = Path.Combine(_carpetaDestino, "errores.log");
        _urlValidacion = urlValidacion;
        _urlImpresion = urlImpresion;
        _urlCalificaciones = urlCalificaciones;
        _httpClient = httpClient;
        _nivelesConCalificaciones = nivelesConCalificaciones;

        if (!Directory.Exists(_carpetaDestino))
        {
            Directory.CreateDirectory(_carpetaDestino);
        }
    }

    public async Task ProcesarAsync(List<RegistroEntrada> registros, CancellationToken cancellationToken)
    {
        _certificadosExitosos.Clear();
        var numeroRegistro = 0;
        foreach (var registro in registros)
        {
            cancellationToken.ThrowIfCancellationRequested();
            numeroRegistro++;
            Console.WriteLine($"Consultando registro {numeroRegistro} de {registros.Count}.");

            var url = SevRequestUrlBuilder.ConParametros(
                _urlValidacion,
                ("CURP", string.Empty),
                ("busqueda", "F"),
                ("cicloEscolarId", registro.CicloEscolarId),
                ("folio", registro.Folio),
                ("imprime", "true"),
                ("nivelEducativoId", registro.NivelEducativoId),
                ("nombre", string.Empty),
                ("primerApellido", string.Empty),
                ("segundoApellido", string.Empty));

            try
            {
                using var response = await _httpClient.PostAsync(url, null, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Respuesta HTTP fallida.", null, response.StatusCode);
                }

                var resultado = JsonSerializer.Deserialize<RespuestaValidaDatos>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (resultado is { Exito: true, Respuesta: { CertificadoId: > 0 } cert })
                {
                    var pdfUrl = SevRequestUrlBuilder.ConParametros(
                        _urlImpresion,
                        ("certificadoId", cert.CertificadoId),
                        ("folioCertificado", cert.Folio),
                        ("nivel", cert.NivelEducativoId),
                        ("anioInicio", cert.AnioInicio),
                        ("anioFin", cert.AnioFin));

                    var archivoNombre = Path.Combine(_carpetaDestino, OutputFileName.ParaFolio(cert.Folio, ".pdf", cert.CertificadoId));
                    if (File.Exists(archivoNombre))
                    {
                        Console.WriteLine("PDF ya existe; se intentaran obtener las calificaciones.");
                    }
                    else
                    {
                        using var pdfResponse = await _httpClient.GetAsync(pdfUrl, cancellationToken);
                        pdfResponse.EnsureSuccessStatusCode();
                        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                        if (!EsPdf(pdfBytes))
                        {
                            throw new InvalidDataException("SEV no devolvio un archivo PDF valido.");
                        }

                        await File.WriteAllBytesAsync(archivoNombre, pdfBytes, cancellationToken);
                        Console.WriteLine("PDF guardado.");
                    }

                    var extractor = new CalificacionExtractor(_carpetaDestino, _urlCalificaciones, _httpClient, _nivelesConCalificaciones);
                    var calificaciones = await extractor.ExtraerYGuardarCSVAsync(cert.Folio, cert.NivelEducativoId, cert.CicloEscolar, cert.CertificadoId, cancellationToken);
                    if (calificaciones.Aplica && !calificaciones.Exitoso)
                    {
                        RegistrarError($"Registro {numeroRegistro}: {calificaciones.Error}");
                    }

                    _certificadosExitosos.Add(cert);
                }
                else
                {
                    var mensaje = $"Registro {numeroRegistro}: SEV no devolvio un certificado valido para los datos indicados.";
                    Console.WriteLine(mensaje);
                    RegistrarError(mensaje);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var mensaje = $"Registro {numeroRegistro}: {SafeErrorMessages.ParaUsuario(ex)}";
                Console.WriteLine(mensaje);
                RegistrarError(mensaje);
            }
        }

        if (_certificadosExitosos.Count > 0)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var archivoJson = Path.Combine(_carpetaDestino, $"certificados_{fecha}.json");

            var json = JsonSerializer.Serialize(_certificadosExitosos, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(archivoJson, json, cancellationToken);
            Console.WriteLine("Archivo JSON consolidado guardado en la carpeta de salida.");
        }
    }

    private void RegistrarError(string mensaje)
    {
        var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}{Environment.NewLine}";
        File.AppendAllText(_logPath, log);
    }

    private static bool EsPdf(byte[] contenido) =>
        contenido.Length >= 5 && contenido.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
}

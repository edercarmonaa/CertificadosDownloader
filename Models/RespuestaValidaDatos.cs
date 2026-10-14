namespace CertificadosDownloader.Models;

public class RespuestaValidaDatos
{
    public bool CumpleHorario { get; set; }
    public bool Exito { get; set; }
    public bool Advertencia { get; set; }
    public string? MensajeError { get; set; }
    public Certificado? Respuesta { get; set; }
}

public class Certificado
{
    public int CertificadoId { get; set; }
    public string Folio { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Autoridad { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string CicloEscolar { get; set; } = "";
    public int AnioInicio { get; set; }
    public int AnioFin { get; set; }
    public int NivelEducativoId { get; set; }
    public double Promedio { get; set; }
    public bool Imprime { get; set; }
}

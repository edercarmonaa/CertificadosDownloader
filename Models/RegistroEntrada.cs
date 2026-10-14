namespace CertificadosDownloader.Models;

public class RegistroEntrada
{
    public required string Folio { get; set; }
    public required string Nivel { get; set; }
    public required int Inicio { get; set; }
    public required int Fin { get; set; }

    public int CicloEscolarId { get; set; }
    public int NivelEducativoId { get; set; }
}


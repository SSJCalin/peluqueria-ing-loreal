namespace PeluqueriaApp.Modelos;

// Un hueco bloqueado (descanso, ausencia...) de una profesional.
public class Bloqueo
{
    public int Id { get; set; }

    public DateOnly Fecha { get; set; }
    public int InicioMinutos { get; set; }
    public int DuracionMinutos { get; set; }

    public string Categoria { get; set; } = "";
    public string Profesional { get; set; } = "";
    public string Motivo { get; set; } = "";
}
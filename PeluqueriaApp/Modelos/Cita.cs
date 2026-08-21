namespace PeluqueriaApp.Modelos;

// Una reserva de un cliente con una profesional para un servicio.
public class Cita
{
    public int Id { get; set; }

    public DateOnly Fecha { get; set; }         // solo la fecha (sin hora)
    public int InicioMinutos { get; set; }      // hora de inicio en minutos desde medianoche
                                                // (ej. 13:00 = 780, 13:30 = 810)

    public string Categoria { get; set; } = ""; // "peluqueria" o "manicura"
    public string Profesional { get; set; } = "";
    public int ServicioId { get; set; }         // qué servicio del catálogo es

    public string Cliente { get; set; } = "";
    public string Telefono { get; set; } = "";

    public string Estado { get; set; } = "confirmada"; // "confirmada", "atendida", "cancelada"
}
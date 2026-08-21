namespace PeluqueriaApp.Modelos;

// Un servicio del catálogo (ej. "Lavar + Cortar", 45 min, 18 €).
public class Servicio
{
    public int Id { get; set; }                 // identificador único (lo pone la base de datos)
    public string Nombre { get; set; } = "";
    public int DuracionMinutos { get; set; }    // 15, 30, 45, 60, 75...
    public decimal Precio { get; set; }
    public string Categoria { get; set; } = ""; // "peluqueria" o "manicura"
}
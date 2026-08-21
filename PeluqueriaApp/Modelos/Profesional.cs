namespace PeluqueriaApp.Modelos;

// Una profesional del salon.
//
// NOVEDAD: cada una trabaja unos dias concretos de la semana, no todas a la vez.
//   Reyes            -> lunes y miercoles
//   Teresa Salvador  -> martes, jueves y viernes
//   Teresa Chavez    -> martes y jueves (manicura)
//
// Los dias se guardan como texto separado por comas usando la numeracion
// de .NET (DayOfWeek): 1=lunes, 2=martes, 3=miercoles, 4=jueves, 5=viernes.
// Ejemplo: Reyes -> "1,3"
public class Profesional
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";   // "peluqueria" o "manicura"

    // Dias que trabaja, en formato "1,3" o "2,4,5"
    public string DiasTrabajo { get; set; } = "";

    // Devuelve true si esta profesional trabaja ese dia de la semana.
    public bool TrabajaEl(DayOfWeek dia)
    {
        int numero = (int)dia;   // domingo=0, lunes=1 ... sabado=6
        return DiasTrabajo
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Any(d => int.TryParse(d, out var n) && n == numero);
    }

    // Texto legible para mostrar en pantalla, ej: "Lunes, Miercoles"
    public string DiasEnTexto()
    {
        var nombres = new Dictionary<int, string>
        {
            [1] = "Lunes", [2] = "Martes", [3] = "Miercoles",
            [4] = "Jueves", [5] = "Viernes"
        };

        var lista = DiasTrabajo
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => int.TryParse(d.Trim(), out var n) && nombres.ContainsKey(n) ? nombres[n] : null)
            .Where(t => t is not null);

        return string.Join(", ", lista);
    }
}
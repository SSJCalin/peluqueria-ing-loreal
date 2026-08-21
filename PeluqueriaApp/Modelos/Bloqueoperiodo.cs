namespace PeluqueriaApp.Modelos;

// Un periodo de ausencia de una profesional: vacaciones, medico, asuntos
// propios... Puede ser de un solo dia o de varias semanas.
//
// A diferencia de "Bloqueo" (que tapa un hueco concreto de 15-60 min dentro
// de un dia), esto tapa dias completos o medios dias en un rango de fechas.
public class BloqueoPeriodo
{
    public int Id { get; set; }

    public string Profesional { get; set; } = "";
    public string Categoria { get; set; } = "";

    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }        // incluida: si es el mismo dia, inicio = fin

    // "completo"  -> todo el horario de ese dia
    // "primera"   -> primera mitad del horario (ej. 13:00-15:00 en peluqueria)
    // "segunda"   -> segunda mitad del horario (ej. 15:00-17:00 en peluqueria)
    public string Franja { get; set; } = "completo";

    public string Motivo { get; set; } = "";

    // Quien lo creo (para saber quien registro la ausencia)
    public string CreadoPor { get; set; } = "";

    // True si este periodo cubre la fecha indicada.
    public bool CubreFecha(DateOnly fecha)
        => fecha >= FechaInicio && fecha <= FechaFin;

    // Cuantos dias abarca (contando ambos extremos)
    public int Dias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;

    public string FranjaEnTexto() => Franja switch
    {
        "primera" => "Primera mitad del dia",
        "segunda" => "Segunda mitad del dia",
        _ => "Dia completo"
    };
}
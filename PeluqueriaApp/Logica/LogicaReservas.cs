using PeluqueriaApp.Modelos;

namespace PeluqueriaApp.Logica;

// Horario de apertura de cada categoria.
public class ConfigCategoria
{
    public string Nombre { get; set; } = "";
    public int InicioMinutos { get; set; }
    public int FinMinutos { get; set; }
}

// Un hueco candidato para reservar.
public record Hueco(int InicioMinutos, string Hora, bool Disponible);

public static class LogicaReservas
{
    // Horarios de apertura. La CAPACIDAD ya no es un numero fijo: depende de
    // cuantas profesionales trabajen ese dia concreto de la semana.
    public static readonly Dictionary<string, ConfigCategoria> Categorias = new()
    {
        ["peluqueria"] = new ConfigCategoria { Nombre = "Peluquería", InicioMinutos = 13 * 60, FinMinutos = 17 * 60 },
        ["manicura"]   = new ConfigCategoria { Nombre = "Manicura",   InicioMinutos = 13 * 60, FinMinutos = 16 * 60 },
    };

    public static string AHora(int minutos) => $"{minutos / 60:D2}:{minutos % 60:D2}";

    public static bool SeSolapan(int inicioA, int finA, int inicioB, int finB)
        => inicioA < finB && inicioB < finA;

    // Punto medio del horario, para los bloqueos de medio dia.
    public static int MedioDia(string categoria)
    {
        var cfg = Categorias[categoria];
        return cfg.InicioMinutos + (cfg.FinMinutos - cfg.InicioMinutos) / 2;
    }

    // Convierte un bloqueo de periodo en el tramo de minutos que tapa ese dia.
    public static (int Inicio, int Fin) TramoDelPeriodo(BloqueoPeriodo periodo, string categoria)
    {
        var cfg = Categorias[categoria];
        int medio = MedioDia(categoria);

        return periodo.Franja switch
        {
            "primera" => (cfg.InicioMinutos, medio),
            "segunda" => (medio, cfg.FinMinutos),
            _ => (cfg.InicioMinutos, cfg.FinMinutos),   // dia completo
        };
    }

    // ----------------------------------------------------------------------
    //  DISPONIBILIDAD DE UNA PROFESIONAL EN UN DIA
    // ----------------------------------------------------------------------

    // Profesionales que trabajan ese dia de la semana Y no estan de baja/vacaciones
    // el dia completo. Es la base de todo el calculo.
    public static List<Profesional> ProfesionalesDisponibles(
        IEnumerable<Profesional> todas,
        DateOnly fecha,
        IEnumerable<BloqueoPeriodo> periodos)
    {
        return todas
            .Where(p => p.TrabajaEl(fecha.DayOfWeek))
            .Where(p => !periodos.Any(per =>
                per.Profesional == p.Nombre
                && per.CubreFecha(fecha)
                && per.Franja == "completo"))
            .ToList();
    }

    // ¿Esta categoria abre ese dia? (si no trabaja nadie, esta cerrada)
    public static bool CategoriaAbierta(
        IEnumerable<Profesional> profesionalesCategoria,
        DateOnly fecha,
        IEnumerable<BloqueoPeriodo> periodos)
        => ProfesionalesDisponibles(profesionalesCategoria, fecha, periodos).Any();

    // ----------------------------------------------------------------------
    //  CALCULO DE HUECOS
    // ----------------------------------------------------------------------

    // Un hueco esta disponible si AL MENOS UNA de las profesionales que
    // trabajan ese dia lo tiene libre.
    public static List<Hueco> CalcularHuecos(
        string categoria,
        DateOnly fecha,
        int duracionServicio,
        IEnumerable<Profesional> profesionalesCategoria,
        IEnumerable<Cita> citasDelDia,
        IEnumerable<Bloqueo> bloqueosDelDia,
        IEnumerable<BloqueoPeriodo> periodos)
    {
        var cfg = Categorias[categoria];
        var resultado = new List<Hueco>();

        var disponibles = ProfesionalesDisponibles(profesionalesCategoria, fecha, periodos);

        // Si no trabaja nadie ese dia, no hay ningun hueco (categoria cerrada).
        if (disponibles.Count == 0) return resultado;

        var citas = citasDelDia.Where(c => c.Estado != "cancelada").ToList();
        var bloqueos = bloqueosDelDia.ToList();
        var periodosLista = periodos.ToList();

        for (int t = cfg.InicioMinutos; t + duracionServicio <= cfg.FinMinutos; t += 15)
        {
            int fin = t + duracionServicio;

            bool alguienLibre = disponibles.Any(p =>
                ProfesionalLibre(p.Nombre, categoria, fecha, t, duracionServicio,
                                 citas, bloqueos, periodosLista));

            resultado.Add(new Hueco(t, AHora(t), alguienLibre));
        }

        return resultado;
    }

    // ----------------------------------------------------------------------
    //  COMPROBACION DE UNA PROFESIONAL CONCRETA
    // ----------------------------------------------------------------------

    // ¿Esta profesional puede coger esta cita? Comprueba, en este orden:
    //   1. que trabaje ese dia de la semana
    //   2. que no este de vacaciones/baja (periodo)
    //   3. que no choque con otra cita suya
    //   4. que no choque con un bloqueo puntual suyo
    public static bool ProfesionalLibre(
        string profesional,
        string categoria,
        DateOnly fecha,
        int inicio,
        int duracion,
        IEnumerable<Cita> citasDelDia,
        IEnumerable<Bloqueo> bloqueosDelDia,
        IEnumerable<BloqueoPeriodo> periodos,
        IEnumerable<Profesional>? plantilla = null,
        int? idCitaExcluida = null)
    {
        int fin = inicio + duracion;

        // 1. Trabaja ese dia (si nos han pasado la plantilla para comprobarlo)
        if (plantilla is not null)
        {
            var ficha = plantilla.FirstOrDefault(p => p.Nombre == profesional);
            if (ficha is null || !ficha.TrabajaEl(fecha.DayOfWeek)) return false;
        }

        // 2. Periodos de ausencia (vacaciones, medico...)
        foreach (var per in periodos.Where(p => p.Profesional == profesional && p.CubreFecha(fecha)))
        {
            var (pIni, pFin) = TramoDelPeriodo(per, categoria);
            if (SeSolapan(inicio, fin, pIni, pFin)) return false;
        }

        // 3. Otras citas suyas
        bool chocaCita = citasDelDia.Any(c =>
            c.Id != idCitaExcluida
            && c.Profesional == profesional
            && c.Estado != "cancelada"
            && SeSolapan(inicio, fin, c.InicioMinutos, c.InicioMinutos + DuracionCita(c)));
        if (chocaCita) return false;

        // 4. Bloqueos puntuales suyos (descansos cortos)
        bool chocaBloqueo = bloqueosDelDia.Any(b =>
            b.Profesional == profesional
            && SeSolapan(inicio, fin, b.InicioMinutos, b.InicioMinutos + b.DuracionMinutos));

        return !chocaBloqueo;
    }

    // ----------------------------------------------------------------------
    //  DURACIONES DE LOS SERVICIOS
    // ----------------------------------------------------------------------
    private static Dictionary<int, int> _duracionesServicio = new();

    public static void RegistrarDuraciones(Dictionary<int, int> duracionesPorServicioId)
        => _duracionesServicio = duracionesPorServicioId;

    private static int DuracionCita(Cita c)
        => _duracionesServicio.TryGetValue(c.ServicioId, out var d) ? d : 30;
}
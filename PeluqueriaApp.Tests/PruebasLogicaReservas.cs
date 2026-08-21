using PeluqueriaApp.Logica;
using PeluqueriaApp.Modelos;
using Xunit;

namespace PeluqueriaApp.Tests;

// Pruebas del nuevo modelo de horarios:
//   Reyes           -> lunes (1) y miercoles (3)
//   Teresa Salvador -> martes (2), jueves (4) y viernes (5)
//   Teresa Chavez   -> martes (2) y jueves (4), manicura
public class PruebasLogicaReservas
{
    // ---- Plantilla de profesionales igual que en la base de datos ----
    private static List<Profesional> Peluqueras() => new()
    {
        new Profesional { Nombre = "Reyes", Categoria = "peluqueria", DiasTrabajo = "1,3" },
        new Profesional { Nombre = "Teresa Salvador", Categoria = "peluqueria", DiasTrabajo = "2,4,5" },
    };

    private static List<Profesional> Manicuristas() => new()
    {
        new Profesional { Nombre = "Teresa Chávez", Categoria = "manicura", DiasTrabajo = "2,4" },
    };

    // Fechas conocidas (agosto 2026): 3=lunes, 4=martes, 5=miercoles, 6=jueves, 7=viernes
    private static readonly DateOnly Lunes = new(2026, 8, 3);
    private static readonly DateOnly Martes = new(2026, 8, 4);
    private static readonly DateOnly Miercoles = new(2026, 8, 5);
    private static readonly DateOnly Jueves = new(2026, 8, 6);
    private static readonly DateOnly Viernes = new(2026, 8, 7);

    private static Cita CrearCita(string profesional, string categoria, int servicioId,
                                  DateOnly fecha, int inicio, string estado = "confirmada")
        => new Cita
        {
            Profesional = profesional, Categoria = categoria, ServicioId = servicioId,
            Fecha = fecha, InicioMinutos = inicio, Estado = estado
        };

    // Servicio 1 = 30 min, 2 = 45 min, 3 = 75 min
    private static void PrepararDuraciones()
        => LogicaReservas.RegistrarDuraciones(new Dictionary<int, int> { [1] = 30, [2] = 45, [3] = 75 });

    private static readonly List<Cita> SinCitas = new();
    private static readonly List<Bloqueo> SinBloqueos = new();
    private static readonly List<BloqueoPeriodo> SinPeriodos = new();

    // ==================================================================
    //  DIAS DE TRABAJO
    // ==================================================================

    [Fact]
    public void Reyes_trabaja_lunes_y_miercoles_pero_no_martes()
    {
        var reyes = Peluqueras().First(p => p.Nombre == "Reyes");
        Assert.True(reyes.TrabajaEl(DayOfWeek.Monday));
        Assert.True(reyes.TrabajaEl(DayOfWeek.Wednesday));
        Assert.False(reyes.TrabajaEl(DayOfWeek.Tuesday));
        Assert.False(reyes.TrabajaEl(DayOfWeek.Friday));
    }

    [Fact]
    public void TeresaSalvador_trabaja_martes_jueves_y_viernes()
    {
        var teresa = Peluqueras().First(p => p.Nombre == "Teresa Salvador");
        Assert.True(teresa.TrabajaEl(DayOfWeek.Tuesday));
        Assert.True(teresa.TrabajaEl(DayOfWeek.Thursday));
        Assert.True(teresa.TrabajaEl(DayOfWeek.Friday));
        Assert.False(teresa.TrabajaEl(DayOfWeek.Monday));
    }

    [Fact]
    public void Manicura_solo_abre_martes_y_jueves()
    {
        PrepararDuraciones();

        Assert.True(LogicaReservas.CategoriaAbierta(Manicuristas(), Martes, SinPeriodos));
        Assert.True(LogicaReservas.CategoriaAbierta(Manicuristas(), Jueves, SinPeriodos));

        Assert.False(LogicaReservas.CategoriaAbierta(Manicuristas(), Lunes, SinPeriodos));
        Assert.False(LogicaReservas.CategoriaAbierta(Manicuristas(), Miercoles, SinPeriodos));
        Assert.False(LogicaReservas.CategoriaAbierta(Manicuristas(), Viernes, SinPeriodos));
    }

    [Fact]
    public void Manicura_un_lunes_no_ofrece_ningun_hueco()
    {
        PrepararDuraciones();
        var huecos = LogicaReservas.CalcularHuecos(
            "manicura", Lunes, 30, Manicuristas(), SinCitas, SinBloqueos, SinPeriodos);

        Assert.Empty(huecos);
    }

    // ==================================================================
    //  CAPACIDAD: ahora es 1 por dia, no 2 (nunca coinciden)
    // ==================================================================

    [Fact]
    public void Peluqueria_un_lunes_una_sola_cita_ya_ocupa_el_hueco()
    {
        PrepararDuraciones();
        // El lunes solo trabaja Reyes: si tiene cita a las 13:00, no queda nadie mas
        var citas = new List<Cita> { CrearCita("Reyes", "peluqueria", 1, Lunes, 780) };

        var huecos = LogicaReservas.CalcularHuecos(
            "peluqueria", Lunes, 30, Peluqueras(), citas, SinBloqueos, SinPeriodos);

        var h1300 = huecos.Find(h => h.Hora == "13:00");
        Assert.NotNull(h1300);
        Assert.False(h1300!.Disponible);
    }

    [Fact]
    public void Una_cita_de_Reyes_no_afecta_al_martes_de_Teresa()
    {
        PrepararDuraciones();
        // Reyes ocupada el lunes; el martes trabaja Teresa Salvador y esta libre
        var citas = new List<Cita> { CrearCita("Reyes", "peluqueria", 1, Lunes, 780) };

        var huecos = LogicaReservas.CalcularHuecos(
            "peluqueria", Martes, 30, Peluqueras(), citas, SinBloqueos, SinPeriodos);

        var h1300 = huecos.Find(h => h.Hora == "13:00");
        Assert.True(h1300!.Disponible);
    }

    // ==================================================================
    //  HORARIO DE CIERRE
    // ==================================================================

    [Fact]
    public void Servicio_de_75_min_no_cabe_despues_de_las_1545_en_peluqueria()
    {
        PrepararDuraciones();
        // Peluqueria cierra a las 17:00 -> el ultimo inicio posible es 15:45
        var huecos = LogicaReservas.CalcularHuecos(
            "peluqueria", Lunes, 75, Peluqueras(), SinCitas, SinBloqueos, SinPeriodos);

        Assert.Contains(huecos, h => h.Hora == "15:45");
        Assert.DoesNotContain(huecos, h => h.Hora == "16:00");
    }

    [Fact]
    public void Servicio_de_75_min_no_cabe_despues_de_las_1445_en_manicura()
    {
        PrepararDuraciones();
        // Manicura cierra a las 16:00 -> el ultimo inicio posible es 14:45
        var huecos = LogicaReservas.CalcularHuecos(
            "manicura", Martes, 75, Manicuristas(), SinCitas, SinBloqueos, SinPeriodos);

        Assert.Contains(huecos, h => h.Hora == "14:45");
        Assert.DoesNotContain(huecos, h => h.Hora == "15:00");
    }

    // ==================================================================
    //  SOLAPAMIENTOS
    // ==================================================================

    [Fact]
    public void No_se_puede_solapar_pero_si_encadenar_al_terminar()
    {
        PrepararDuraciones();
        // Reyes tiene 45 min desde las 13:00 (780-825) un lunes
        var citas = new List<Cita> { CrearCita("Reyes", "peluqueria", 2, Lunes, 780) };

        // A las 13:15 se solapa -> NO
        Assert.False(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Lunes, 795, 30, citas, SinBloqueos, SinPeriodos, Peluqueras()));

        // A las 13:45, justo al acabar -> SI
        Assert.True(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Lunes, 825, 30, citas, SinBloqueos, SinPeriodos, Peluqueras()));
    }

    [Fact]
    public void Una_profesional_no_puede_coger_cita_en_un_dia_que_no_trabaja()
    {
        PrepararDuraciones();
        // Reyes no trabaja los martes
        Assert.False(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Martes, 780, 30, SinCitas, SinBloqueos, SinPeriodos, Peluqueras()));

        // Pero si los lunes
        Assert.True(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Lunes, 780, 30, SinCitas, SinBloqueos, SinPeriodos, Peluqueras()));
    }

    [Fact]
    public void Un_bloqueo_puntual_ocupa_el_hueco()
    {
        PrepararDuraciones();
        var bloqueos = new List<Bloqueo>
        {
            new Bloqueo { Profesional = "Reyes", Categoria = "peluqueria",
                          Fecha = Lunes, InicioMinutos = 900, DuracionMinutos = 30 }
        };

        Assert.False(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Lunes, 900, 30, SinCitas, bloqueos, SinPeriodos, Peluqueras()));
    }

    [Fact]
    public void Cita_cancelada_no_cuenta_como_ocupacion()
    {
        PrepararDuraciones();
        var citas = new List<Cita>
        {
            CrearCita("Reyes", "peluqueria", 1, Lunes, 780, estado: "cancelada")
        };

        Assert.True(LogicaReservas.ProfesionalLibre(
            "Reyes", "peluqueria", Lunes, 780, 30, citas, SinBloqueos, SinPeriodos, Peluqueras()));
    }

    // ==================================================================
    //  PERIODOS DE AUSENCIA (vacaciones, medico...)
    // ==================================================================

    [Fact]
    public void Vacaciones_de_dia_completo_cierran_la_categoria_ese_dia()
    {
        PrepararDuraciones();
        // Teresa Chavez de vacaciones el martes -> manicura cerrada ese dia
        var periodos = new List<BloqueoPeriodo>
        {
            new BloqueoPeriodo { Profesional = "Teresa Chávez", Categoria = "manicura",
                                 FechaInicio = Martes, FechaFin = Martes,
                                 Franja = "completo", Motivo = "Médico" }
        };

        var huecos = LogicaReservas.CalcularHuecos(
            "manicura", Martes, 30, Manicuristas(), SinCitas, SinBloqueos, periodos);

        Assert.Empty(huecos);
        Assert.False(LogicaReservas.CategoriaAbierta(Manicuristas(), Martes, periodos));
    }

    [Fact]
    public void Un_periodo_de_varios_dias_cubre_todos_los_dias_del_rango()
    {
        var periodo = new BloqueoPeriodo
        {
            Profesional = "Reyes", FechaInicio = new DateOnly(2026, 8, 3),
            FechaFin = new DateOnly(2026, 8, 14), Franja = "completo"
        };

        Assert.True(periodo.CubreFecha(new DateOnly(2026, 8, 3)));    // primer dia
        Assert.True(periodo.CubreFecha(new DateOnly(2026, 8, 10)));   // en medio
        Assert.True(periodo.CubreFecha(new DateOnly(2026, 8, 14)));   // ultimo dia
        Assert.False(periodo.CubreFecha(new DateOnly(2026, 8, 2)));   // antes
        Assert.False(periodo.CubreFecha(new DateOnly(2026, 8, 15)));  // despues
        Assert.Equal(12, periodo.Dias);
    }

    [Fact]
    public void Medio_dia_solo_bloquea_su_mitad()
    {
        PrepararDuraciones();
        // Peluqueria 13:00-17:00 -> el medio dia son las 15:00
        // Reyes ausente la PRIMERA mitad del lunes (13:00-15:00)
        var periodos = new List<BloqueoPeriodo>
        {
            new BloqueoPeriodo { Profesional = "Reyes", Categoria = "peluqueria",
                                 FechaInicio = Lunes, FechaFin = Lunes,
                                 Franja = "primera", Motivo = "Médico" }
        };

        var huecos = LogicaReservas.CalcularHuecos(
            "peluqueria", Lunes, 30, Peluqueras(), SinCitas, SinBloqueos, periodos);

        // Por la manana (primera mitad) no hay hueco
        Assert.False(huecos.Find(h => h.Hora == "13:00")!.Disponible);
        Assert.False(huecos.Find(h => h.Hora == "14:30")!.Disponible);

        // A partir de las 15:00 si
        Assert.True(huecos.Find(h => h.Hora == "15:00")!.Disponible);
        Assert.True(huecos.Find(h => h.Hora == "16:00")!.Disponible);
    }

    [Fact]
    public void El_medio_dia_se_calcula_bien_en_cada_categoria()
    {
        // Peluqueria 13:00-17:00 -> medio = 15:00 (900 min)
        Assert.Equal(900, LogicaReservas.MedioDia("peluqueria"));
        // Manicura 13:00-16:00 -> medio = 14:30 (870 min)
        Assert.Equal(870, LogicaReservas.MedioDia("manicura"));
    }

    [Fact]
    public void Vacaciones_de_una_profesional_no_afectan_a_la_otra()
    {
        PrepararDuraciones();
        // Reyes de vacaciones toda la semana, pero Teresa Salvador trabaja el martes
        var periodos = new List<BloqueoPeriodo>
        {
            new BloqueoPeriodo { Profesional = "Reyes", Categoria = "peluqueria",
                                 FechaInicio = Lunes, FechaFin = Viernes, Franja = "completo" }
        };

        // El lunes (dia de Reyes) la peluqueria queda cerrada
        Assert.Empty(LogicaReservas.CalcularHuecos(
            "peluqueria", Lunes, 30, Peluqueras(), SinCitas, SinBloqueos, periodos));

        // El martes (dia de Teresa) sigue abierta con normalidad
        var huecosMartes = LogicaReservas.CalcularHuecos(
            "peluqueria", Martes, 30, Peluqueras(), SinCitas, SinBloqueos, periodos);
        Assert.NotEmpty(huecosMartes);
        Assert.True(huecosMartes.Find(h => h.Hora == "13:00")!.Disponible);
    }
}
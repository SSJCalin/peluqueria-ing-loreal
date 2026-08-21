using Microsoft.EntityFrameworkCore;
using PeluqueriaApp.Datos;
using PeluqueriaApp.Logica;
using PeluqueriaApp.Modelos;

namespace PeluqueriaApp.Servicios;

// Puente entre la base de datos y la logica de reservas.
// Las paginas hablan con esta clase, no directamente con la base de datos.
public class ServicioCitas
{
    private readonly ContextoBD _bd;

    public ServicioCitas(ContextoBD bd)
    {
        _bd = bd;
    }

    // ============================================================
    //  CATALOGO Y PLANTILLA
    // ============================================================

    public async Task<List<Servicio>> ObtenerServiciosAsync(string categoria)
        => await _bd.Servicios
            .Where(s => s.Categoria == categoria)
            .OrderBy(s => s.DuracionMinutos)
            .ToListAsync();

    public async Task<List<Servicio>> ObtenerTodosLosServiciosAsync()
        => await _bd.Servicios.OrderBy(s => s.Categoria).ThenBy(s => s.Nombre).ToListAsync();

    public async Task<Servicio?> ObtenerServicioAsync(int id)
        => await _bd.Servicios.FindAsync(id);

    public async Task<List<Profesional>> ObtenerProfesionalesAsync(string categoria)
        => await _bd.Profesionales
            .Where(p => p.Categoria == categoria)
            .ToListAsync();

    public async Task<List<Profesional>> ObtenerTodasLasProfesionalesAsync()
        => await _bd.Profesionales.OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ToListAsync();

    // Profesionales que trabajan ese dia y no estan de vacaciones/baja.
    public async Task<List<Profesional>> ObtenerProfesionalesDelDiaAsync(string categoria, DateOnly fecha)
    {
        var todas = await ObtenerProfesionalesAsync(categoria);
        var periodos = await ObtenerPeriodosDeFechaAsync(fecha);
        return LogicaReservas.ProfesionalesDisponibles(todas, fecha, periodos);
    }

    // ¿Abre esta categoria ese dia?
    public async Task<bool> CategoriaAbiertaAsync(string categoria, DateOnly fecha)
        => (await ObtenerProfesionalesDelDiaAsync(categoria, fecha)).Any();

    private async Task PrepararDuracionesAsync()
    {
        var duraciones = await _bd.Servicios.ToDictionaryAsync(s => s.Id, s => s.DuracionMinutos);
        LogicaReservas.RegistrarDuraciones(duraciones);
    }

    // ============================================================
    //  RESERVA PUBLICA
    // ============================================================

    // IMPORTANTE (RGPD): solo devuelve hora + disponible, nunca datos de clientes.
    public async Task<List<Hueco>> ObtenerHuecosAsync(string categoria, DateOnly fecha, int servicioId)
    {
        await PrepararDuracionesAsync();

        var servicio = await _bd.Servicios.FindAsync(servicioId);
        if (servicio is null) return new List<Hueco>();

        var profesionales = await ObtenerProfesionalesAsync(categoria);

        var citas = await _bd.Citas
            .Where(c => c.Fecha == fecha && c.Categoria == categoria && c.Estado != "cancelada")
            .ToListAsync();

        var bloqueos = await _bd.Bloqueos
            .Where(b => b.Fecha == fecha && b.Categoria == categoria)
            .ToListAsync();

        var periodos = await ObtenerPeriodosDeFechaAsync(fecha, categoria);

        return LogicaReservas.CalcularHuecos(
            categoria, fecha, servicio.DuracionMinutos,
            profesionales, citas, bloqueos, periodos);
    }

    public async Task<(bool Exito, string Mensaje)> CrearCitaAsync(
        string categoria, DateOnly fecha, int inicioMinutos, int servicioId,
        string cliente, string telefono)
    {
        await PrepararDuracionesAsync();

        var servicio = await _bd.Servicios.FindAsync(servicioId);
        if (servicio is null) return (false, "El servicio seleccionado no existe.");

        var profesionales = await ObtenerProfesionalesAsync(categoria);
        var periodos = await ObtenerPeriodosDeFechaAsync(fecha, categoria);

        // Solo las que trabajan ese dia y no estan ausentes
        var disponibles = LogicaReservas.ProfesionalesDisponibles(profesionales, fecha, periodos);
        if (disponibles.Count == 0)
            return (false, "Ese dia no hay servicio en esta categoria. Elige otro dia.");

        var citas = await _bd.Citas
            .Where(c => c.Fecha == fecha && c.Categoria == categoria && c.Estado != "cancelada")
            .ToListAsync();

        var bloqueos = await _bd.Bloqueos
            .Where(b => b.Fecha == fecha && b.Categoria == categoria)
            .ToListAsync();

        var libre = disponibles.FirstOrDefault(p =>
            LogicaReservas.ProfesionalLibre(p.Nombre, categoria, fecha, inicioMinutos,
                                           servicio.DuracionMinutos, citas, bloqueos,
                                           periodos, profesionales));

        if (libre is null)
            return (false, "Ese horario acaba de ocuparse. Por favor, elige otra hora.");

        _bd.Citas.Add(new Cita
        {
            Fecha = fecha,
            InicioMinutos = inicioMinutos,
            Categoria = categoria,
            Profesional = libre.Nombre,
            ServicioId = servicioId,
            Cliente = cliente,
            Telefono = telefono,
            Estado = "confirmada"
        });

        await _bd.SaveChangesAsync();
        return (true, $"Cita confirmada con {libre.Nombre}.");
    }

    // ============================================================
    //  GESTION (PANEL)
    // ============================================================

    public async Task<List<Cita>> ObtenerCitasDelDiaAsync(DateOnly fecha)
        => await _bd.Citas
            .Where(c => c.Fecha == fecha)
            .OrderBy(c => c.InicioMinutos)
            .ToListAsync();

    public async Task<List<Bloqueo>> ObtenerBloqueosDelDiaAsync(DateOnly fecha)
        => await _bd.Bloqueos
            .Where(b => b.Fecha == fecha)
            .OrderBy(b => b.InicioMinutos)
            .ToListAsync();

    public async Task<List<Cita>> ObtenerCitasEntreAsync(DateOnly desde, DateOnly hasta)
        => await _bd.Citas
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta)
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMinutos)
            .ToListAsync();

    public async Task<List<Bloqueo>> ObtenerBloqueosEntreAsync(DateOnly desde, DateOnly hasta)
        => await _bd.Bloqueos
            .Where(b => b.Fecha >= desde && b.Fecha <= hasta)
            .ToListAsync();

    public async Task<Cita?> ObtenerCitaAsync(int id)
        => await _bd.Citas.FindAsync(id);

    public async Task<(bool Exito, string Mensaje)> CrearCitaPanelAsync(
        string categoria, string profesional, DateOnly fecha, int inicioMinutos,
        int servicioId, string cliente, string telefono)
    {
        await PrepararDuracionesAsync();

        var servicio = await _bd.Servicios.FindAsync(servicioId);
        if (servicio is null) return (false, "El servicio no existe.");

        var profesionales = await ObtenerProfesionalesAsync(categoria);
        var citas = await ObtenerCitasDelDiaAsync(fecha);
        var bloqueos = await ObtenerBloqueosDelDiaAsync(fecha);
        var periodos = await ObtenerPeriodosDeFechaAsync(fecha, categoria);

        // Comprobamos primero si trabaja ese dia, para dar un mensaje claro
        var ficha = profesionales.FirstOrDefault(p => p.Nombre == profesional);
        if (ficha is null)
            return (false, $"{profesional} no pertenece a esta categoria.");
        if (!ficha.TrabajaEl(fecha.DayOfWeek))
            return (false, $"{profesional} no trabaja los {NombreDia(fecha.DayOfWeek)}. " +
                           $"Trabaja: {ficha.DiasEnTexto()}.");

        var ausencia = periodos.FirstOrDefault(p => p.Profesional == profesional && p.CubreFecha(fecha));
        if (ausencia is not null && ausencia.Franja == "completo")
            return (false, $"{profesional} esta ausente ese dia ({ausencia.Motivo}).");

        if (!LogicaReservas.ProfesionalLibre(profesional, categoria, fecha, inicioMinutos,
                                            servicio.DuracionMinutos, citas, bloqueos,
                                            periodos, profesionales))
            return (false, $"{profesional} ya tiene algo en ese horario.");

        _bd.Citas.Add(new Cita
        {
            Fecha = fecha,
            InicioMinutos = inicioMinutos,
            Categoria = categoria,
            Profesional = profesional,
            ServicioId = servicioId,
            Cliente = cliente,
            Telefono = telefono,
            Estado = "confirmada"
        });

        await _bd.SaveChangesAsync();
        return (true, "Cita creada.");
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarCitaAsync(
        int id, string categoria, string profesional, DateOnly fecha, int inicioMinutos,
        int servicioId, string cliente, string telefono)
    {
        await PrepararDuracionesAsync();

        var cita = await _bd.Citas.FindAsync(id);
        if (cita is null) return (false, "La cita no existe.");

        var servicio = await _bd.Servicios.FindAsync(servicioId);
        if (servicio is null) return (false, "El servicio no existe.");

        var profesionales = await ObtenerProfesionalesAsync(categoria);
        var citas = await ObtenerCitasDelDiaAsync(fecha);
        var bloqueos = await ObtenerBloqueosDelDiaAsync(fecha);
        var periodos = await ObtenerPeriodosDeFechaAsync(fecha, categoria);

        var ficha = profesionales.FirstOrDefault(p => p.Nombre == profesional);
        if (ficha is null)
            return (false, $"{profesional} no pertenece a esta categoria.");
        if (!ficha.TrabajaEl(fecha.DayOfWeek))
            return (false, $"{profesional} no trabaja los {NombreDia(fecha.DayOfWeek)}. " +
                           $"Trabaja: {ficha.DiasEnTexto()}.");

        // Excluimos la propia cita para que no choque consigo misma
        if (!LogicaReservas.ProfesionalLibre(profesional, categoria, fecha, inicioMinutos,
                                            servicio.DuracionMinutos, citas, bloqueos,
                                            periodos, profesionales, id))
            return (false, $"{profesional} ya tiene algo en ese horario.");

        cita.Categoria = categoria;
        cita.Profesional = profesional;
        cita.Fecha = fecha;
        cita.InicioMinutos = inicioMinutos;
        cita.ServicioId = servicioId;
        cita.Cliente = cliente;
        cita.Telefono = telefono;

        await _bd.SaveChangesAsync();
        return (true, "Cita actualizada.");
    }

    public async Task CambiarEstadoCitaAsync(int id, string estado)
    {
        var cita = await _bd.Citas.FindAsync(id);
        if (cita is null) return;
        cita.Estado = estado;
        await _bd.SaveChangesAsync();
    }

    public async Task EliminarCitaAsync(int id)
    {
        var cita = await _bd.Citas.FindAsync(id);
        if (cita is null) return;
        _bd.Citas.Remove(cita);
        await _bd.SaveChangesAsync();
    }

    // ============================================================
    //  BLOQUEOS PUNTUALES (descansos cortos dentro de un dia)
    // ============================================================

    public async Task<(bool Exito, string Mensaje)> CrearBloqueoAsync(
        string categoria, string profesional, DateOnly fecha,
        int inicioMinutos, int duracion, string motivo)
    {
        await PrepararDuracionesAsync();

        var profesionales = await ObtenerProfesionalesAsync(categoria);
        var citas = await ObtenerCitasDelDiaAsync(fecha);
        var bloqueos = await ObtenerBloqueosDelDiaAsync(fecha);
        var periodos = await ObtenerPeriodosDeFechaAsync(fecha, categoria);

        var ficha = profesionales.FirstOrDefault(p => p.Nombre == profesional);
        if (ficha is not null && !ficha.TrabajaEl(fecha.DayOfWeek))
            return (false, $"{profesional} no trabaja los {NombreDia(fecha.DayOfWeek)}, " +
                           "no hace falta bloquear ese dia.");

        if (!LogicaReservas.ProfesionalLibre(profesional, categoria, fecha, inicioMinutos,
                                            duracion, citas, bloqueos, periodos, profesionales))
            return (false, $"{profesional} ya tiene algo en ese horario.");

        _bd.Bloqueos.Add(new Bloqueo
        {
            Fecha = fecha,
            InicioMinutos = inicioMinutos,
            DuracionMinutos = duracion,
            Categoria = categoria,
            Profesional = profesional,
            Motivo = motivo
        });

        await _bd.SaveChangesAsync();
        return (true, "Hueco bloqueado.");
    }

    public async Task EliminarBloqueoAsync(int id)
    {
        var bloqueo = await _bd.Bloqueos.FindAsync(id);
        if (bloqueo is null) return;
        _bd.Bloqueos.Remove(bloqueo);
        await _bd.SaveChangesAsync();
    }

    // ============================================================
    //  PERIODOS DE AUSENCIA (vacaciones, medico...)
    // ============================================================

    // Todos los periodos que cubren una fecha concreta.
    public async Task<List<BloqueoPeriodo>> ObtenerPeriodosDeFechaAsync(
        DateOnly fecha, string? categoria = null)
    {
        var consulta = _bd.BloqueosPeriodo
            .Where(p => p.FechaInicio <= fecha && p.FechaFin >= fecha);

        if (categoria is not null)
            consulta = consulta.Where(p => p.Categoria == categoria);

        return await consulta.ToListAsync();
    }

    // Periodos que se solapan con un rango de fechas (para la vista de semana).
    public async Task<List<BloqueoPeriodo>> ObtenerPeriodosEntreAsync(DateOnly desde, DateOnly hasta)
        => await _bd.BloqueosPeriodo
            .Where(p => p.FechaInicio <= hasta && p.FechaFin >= desde)
            .OrderBy(p => p.FechaInicio)
            .ToListAsync();

    // Todos los periodos futuros o en curso, para la pantalla de gestion.
    public async Task<List<BloqueoPeriodo>> ObtenerPeriodosVigentesAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return await _bd.BloqueosPeriodo
            .Where(p => p.FechaFin >= hoy)
            .OrderBy(p => p.FechaInicio)
            .ToListAsync();
    }

    public async Task<(bool Exito, string Mensaje)> CrearPeriodoAsync(
        string profesional, string categoria, DateOnly desde, DateOnly hasta,
        string franja, string motivo, string creadoPor)
    {
        if (hasta < desde)
            return (false, "La fecha de fin no puede ser anterior a la de inicio.");

        var dias = hasta.DayNumber - desde.DayNumber + 1;
        if (dias > 400)
            return (false, "El periodo es demasiado largo (mas de un ano).");

        // Avisamos si hay citas ya reservadas dentro del periodo
        var citasAfectadas = await _bd.Citas
            .Where(c => c.Profesional == profesional
                        && c.Fecha >= desde && c.Fecha <= hasta
                        && c.Estado == "confirmada")
            .CountAsync();

        _bd.BloqueosPeriodo.Add(new BloqueoPeriodo
        {
            Profesional = profesional,
            Categoria = categoria,
            FechaInicio = desde,
            FechaFin = hasta,
            Franja = franja,
            Motivo = motivo,
            CreadoPor = creadoPor
        });

        await _bd.SaveChangesAsync();

        var mensaje = $"Ausencia registrada: {dias} dia(s).";
        if (citasAfectadas > 0)
            mensaje += $" ATENCION: hay {citasAfectadas} cita(s) ya reservadas en esas fechas " +
                       "que habria que reubicar o avisar.";

        return (true, mensaje);
    }

    public async Task EliminarPeriodoAsync(int id)
    {
        var periodo = await _bd.BloqueosPeriodo.FindAsync(id);
        if (periodo is null) return;
        _bd.BloqueosPeriodo.Remove(periodo);
        await _bd.SaveChangesAsync();
    }

    // Citas confirmadas que caen dentro de un periodo (para avisar antes de crearlo).
    public async Task<List<Cita>> ObtenerCitasEnRangoDeProfesionalAsync(
        string profesional, DateOnly desde, DateOnly hasta)
        => await _bd.Citas
            .Where(c => c.Profesional == profesional
                        && c.Fecha >= desde && c.Fecha <= hasta
                        && c.Estado == "confirmada")
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMinutos)
            .ToListAsync();

    // ============================================================
    //  UTILIDAD
    // ============================================================

    public static string NombreDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miercoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sabado",
        _ => "domingo",
    };
}
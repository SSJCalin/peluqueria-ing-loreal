using Microsoft.EntityFrameworkCore;
using PeluqueriaApp.Modelos;

namespace PeluqueriaApp.Datos;

// El "contexto" es el puente entre tus fichas de C# y la base de datos.
// Cada propiedad DbSet<> de aquí abajo se convierte en una TABLA en SQLite.
public class ContextoBD : DbContext
{
    public DbSet<BloqueoPeriodo> BloqueosPeriodo { get; set; }
    public ContextoBD(DbContextOptions<ContextoBD> opciones) : base(opciones) 
    
    {
    }

    // Estas cuatro líneas crean cuatro tablas: Servicios, Profesionales, Citas y Bloqueos.
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Profesional> Profesionales { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<Bloqueo> Bloqueos { get; set; }

    public DbSet<Usuario> Usuarios { get; set; }
    
}
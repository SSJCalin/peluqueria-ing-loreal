using PeluqueriaApp.Modelos;
using Microsoft.Extensions.Configuration;

namespace PeluqueriaApp.Datos;

// Rellena la base de datos con los datos fijos (servicios, profesionales y
// usuarios) la primera vez. No duplica: si ya hay datos, no los repite.
//
// Ademas REPARA datos que existan pero esten incompletos, como los dias de
// trabajo de las profesionales (que se anadieron despues de crearlas).
public static class Sembrador
{
    // Dias de trabajo de cada profesional.
    // 1=lunes, 2=martes, 3=miercoles, 4=jueves, 5=viernes
    private static readonly Dictionary<string, string> DiasPorProfesional = new()
    {
        ["Reyes"] = "1,3",              // lunes y miercoles
        ["Teresa Salvador"] = "2,4,5",  // martes, jueves y viernes
        ["Teresa Chávez"] = "2,4",      // martes y jueves (manicura)
    };

        public static void Sembrar(ContextoBD bd, IConfiguration? configuracion = null)
    {
        // --- PROFESIONALES ---
        if (!bd.Profesionales.Any())
        {
            bd.Profesionales.AddRange(
                new Profesional { Nombre = "Reyes", Categoria = "peluqueria", DiasTrabajo = DiasPorProfesional["Reyes"] },
                new Profesional { Nombre = "Teresa Salvador", Categoria = "peluqueria", DiasTrabajo = DiasPorProfesional["Teresa Salvador"] },
                new Profesional { Nombre = "Teresa Chávez", Categoria = "manicura", DiasTrabajo = DiasPorProfesional["Teresa Chávez"] }
            );
        }
        else
        {
            // Ya existian (de antes de anadir los dias de trabajo): les
            // rellenamos los dias si los tienen vacios.
            foreach (var prof in bd.Profesionales.ToList())
            {
                if (string.IsNullOrWhiteSpace(prof.DiasTrabajo)
                    && DiasPorProfesional.TryGetValue(prof.Nombre, out var dias))
                {
                    prof.DiasTrabajo = dias;
                }
            }
        }

        // --- SERVICIOS ---
        if (!bd.Servicios.Any())
        {
            bd.Servicios.AddRange(
                // Peluqueria
                new Servicio { Nombre = "Peinar", DuracionMinutos = 30, Precio = 11.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Lavar + Peinar", DuracionMinutos = 45, Precio = 15.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Lavar + Peinar + Kérastase", DuracionMinutos = 45, Precio = 21.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Corte caballero", DuracionMinutos = 30, Precio = 15.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Corte caballero + masaje Fusio Scrub", DuracionMinutos = 45, Precio = 16.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Lavar + Cortar", DuracionMinutos = 45, Precio = 18.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Lavar + Cortar + Peinar", DuracionMinutos = 60, Precio = 27.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Semi-recogido (lavar y peinar incl.)", DuracionMinutos = 30, Precio = 20.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Recogido (lavar y peinar incl.)", DuracionMinutos = 45, Precio = 24.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Corte de flequillo", DuracionMinutos = 15, Precio = 6.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Diseño de cejas", DuracionMinutos = 15, Precio = 7.0m, Categoria = "peluqueria" },
                new Servicio { Nombre = "Lavar + Cortar + Peinar + Fusio Dose", DuracionMinutos = 75, Precio = 32.0m, Categoria = "peluqueria" },
                // Manicura
                new Servicio { Nombre = "Manicura tradicional caballero", DuracionMinutos = 30, Precio = 10.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Manicura tradicional caballero + Spa", DuracionMinutos = 45, Precio = 14.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Manicura tradicional", DuracionMinutos = 30, Precio = 13.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Manicura tradicional + Spa", DuracionMinutos = 45, Precio = 17.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Semipermanente", DuracionMinutos = 30, Precio = 15.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Semipermanente + Spa", DuracionMinutos = 45, Precio = 19.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Permanente sin retirada", DuracionMinutos = 30, Precio = 18.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Permanente con retirada", DuracionMinutos = 45, Precio = 23.0m, Categoria = "manicura" },
                new Servicio { Nombre = "Retirar permanente (sin manicura posterior)", DuracionMinutos = 15, Precio = 5.0m, Categoria = "manicura" }
            );
        }

                // --- USUARIOS DEL PANEL ---
        // Las contrasenas NO estan en el codigo: se leen de
        // appsettings.Development.json (que Git ignora).
        if (!bd.Usuarios.Any() && configuracion is not null)
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>();
            var vacio = new Usuario();

            // Nombre de usuario -> nombre completo que se muestra en pantalla
            var nombresCompletos = new Dictionary<string, string>
            {
                ["reyes"] = "Reyes",
                ["TereSalv"] = "Teresa Salvador",
                ["Terech"] = "Teresa Chávez",
                ["AdminspING"] = "Administración",
            };

            var seccion = configuracion.GetSection("UsuariosIniciales");

            foreach (var (usuario, nombreCompleto) in nombresCompletos)
            {
                var contrasena = seccion[usuario];

                if (string.IsNullOrWhiteSpace(contrasena))
                {
                    // Sin contrasena configurada no se crea el usuario.
                    // Revisa appsettings.Development.json
                    continue;
                }

                bd.Usuarios.Add(new Usuario
                {
                    NombreUsuario = usuario,
                    NombreCompleto = nombreCompleto,
                    HashContrasena = hasher.HashPassword(vacio, contrasena)
                });
            }
        }

        bd.SaveChanges();
    }
}
namespace PeluqueriaApp.Modelos;

// Un usuario del panel de administracion (las 3 profesionales + administracion).
// La contrasena NUNCA se guarda tal cual: se guarda su "hash" (cifrado
// irreversible). Ni siquiera mirando la base de datos se puede leer.
public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string HashContrasena { get; set; } = "";
}
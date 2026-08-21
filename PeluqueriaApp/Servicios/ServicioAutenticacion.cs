using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeluqueriaApp.Datos;
using PeluqueriaApp.Modelos;

namespace PeluqueriaApp.Servicios;

// Se encarga de cifrar contrasenas y de comprobar si un login es valido.
public class ServicioAutenticacion
{
    private readonly ContextoBD _bd;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public ServicioAutenticacion(ContextoBD bd)
    {
        _bd = bd;
    }

    // Convierte una contrasena en su hash (cifrado irreversible).
    public string CifrarContrasena(string contrasena)
        => _hasher.HashPassword(new Usuario(), contrasena);

    // Comprueba usuario + contrasena. Devuelve el usuario si es correcto, null si no.
    public async Task<Usuario?> ValidarAsync(string nombreUsuario, string contrasena)
    {
        var usuario = await _bd.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

        if (usuario is null) return null;

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.HashContrasena, contrasena);

        return resultado == PasswordVerificationResult.Success
            || resultado == PasswordVerificationResult.SuccessRehashNeeded
            ? usuario
            : null;
    }
}
using System.ComponentModel.DataAnnotations;

namespace Reciclame.Api.Contracts;

public sealed record SolicitudRegistro(
    [Required, MinLength(2), MaxLength(100)] string Nombre,
    [Required, EmailAddress] string Correo,
    [Required, MinLength(8)] string Contrasena,
    [Required] string Rol);

public sealed record SolicitudInicioSesion(
    [Required, EmailAddress] string Correo,
    [Required] string Contrasena);

public sealed record RespuestaAutenticacion(string Token, RespuestaUsuario Usuario);
public sealed record RespuestaUsuario(Guid Id, string Nombre, string Correo, string Rol, int Puntos, int Nivel);
public sealed record SolicitudCrearReciclaje([Required] string Material, [Range(0.1, 1000)] decimal PesoKg);
public sealed record SolicitudCrearRetiro(
    [Required, MaxLength(180)] string Direccion,
    [Required, MaxLength(200)] string Materiales,
    DateTime ProgramadoPara,
    [MaxLength(300)] string? Notas);
public sealed record SolicitudVananga([Required, MaxLength(300)] string Mensaje);

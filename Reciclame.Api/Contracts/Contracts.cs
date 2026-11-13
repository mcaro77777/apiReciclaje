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
public sealed record RespuestaUsuario(
    Guid Id,
    string Nombre,
    string Correo,
    string Rol,
    int Puntos,
    int Nivel,
    int Experiencia = 0,
    int EcoMonedas = 0,
    int RachaSemanas = 0,
    string? Sector = null);
public sealed record SolicitudCrearReciclaje(
    [Required] string Material,
    [Range(0.1, 25)] decimal PesoKg,
    Guid? PuntoLimpioId = null,
    [MaxLength(120)] string? CodigoVerificacion = null);
public sealed record SolicitudValidarReciclaje(
    bool Aprobar,
    [Range(0.1, 25)] decimal? PesoVerificadoKg,
    [MaxLength(300)] string? Motivo);
public sealed record SolicitudCrearRetiro(
    [Required, MaxLength(180)] string Direccion,
    [Required, MaxLength(200)] string Materiales,
    DateTime ProgramadoPara,
    [MaxLength(300)] string? Notas);
public sealed record SolicitudVananga([Required, MaxLength(300)] string Mensaje);

public sealed record ActualizarSectorRequest([Required, MaxLength(80)] string Sector);
public sealed record SolicitudCompletarContenido([MaxLength(200)] string? Respuesta);
public sealed record SolicitudPuntoLimpio(
    [Required, MaxLength(100)] string Nombre,
    [Required, MaxLength(180)] string Direccion,
    [Required, MaxLength(200)] string Materiales,
    [Range(-90, 90)] double Latitud,
    [Range(-180, 180)] double Longitud,
    [Required, MaxLength(100)] string Horario,
    [RegularExpression("open|closed|full|maintenance")] string EstadoOperativo,
    [Range(0, 100)] int? CapacidadPorcentaje,
    [MinLength(6), MaxLength(120)] string? CodigoVerificacion);
public sealed record SolicitudActualizarCanje(
    [RegularExpression("requested|used|cancelled|expired")] string Estado);

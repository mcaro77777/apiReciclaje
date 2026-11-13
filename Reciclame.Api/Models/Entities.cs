using System.ComponentModel.DataAnnotations;

namespace Reciclame.Api.Models;

public static class Roles
{
    public const string Residente = "resident";
    public const string Turista = "tourist";
    public const string Municipal = "municipal";
}

public static class EstadosReciclaje
{
    public const string Pendiente = "pending";
    public const string Validado = "validated";
    public const string Rechazado = "rejected";
}

public sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public required string Nombre { get; set; }
    [MaxLength(180)] public required string Correo { get; set; }
    [MaxLength(500)] public required string HashContrasena { get; set; }
    [MaxLength(20)] public string Rol { get; set; } = Roles.Residente;
    [MaxLength(80)] public string? Sector { get; set; }
    /// <summary>Saldo canjeable. Se conserva como Puntos para compatibilidad con clientes 1.x.</summary>
    public int Puntos { get; set; }
    /// <summary>Progreso historico; nunca disminuye al canjear recompensas.</summary>
    public int Experiencia { get; set; }
    public int Nivel { get; set; } = 1;
    public int RachaSemanas { get; set; }
    public DateTime? UltimaActividadEn { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public ICollection<RegistroReciclaje> RegistrosReciclaje { get; set; } = [];
}

public sealed class RegistroReciclaje
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [MaxLength(30)] public required string Material { get; set; }
    public decimal PesoKg { get; set; }
    public int PuntosGanados { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = EstadosReciclaje.Pendiente;
    public Guid? PuntoLimpioId { get; set; }
    public PuntoLimpio? PuntoLimpio { get; set; }
    public Guid? ValidadoPorId { get; set; }
    public DateTime? ValidadoEn { get; set; }
    [MaxLength(300)] public string? MotivoRechazo { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public sealed class Recompensa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public required string Nombre { get; set; }
    [MaxLength(300)] public required string Descripcion { get; set; }
    [MaxLength(40)] public required string Categoria { get; set; }
    [MaxLength(40)] public required string Icono { get; set; }
    public int CostoPuntos { get; set; }
    public int Stock { get; set; }
    public bool Activa { get; set; } = true;
    [MaxLength(120)] public string? Socio { get; set; }
    [MaxLength(180)] public string? Ubicacion { get; set; }
    [MaxLength(400)] public string? Terminos { get; set; }
    public DateTime? ValidaHasta { get; set; }
}

public sealed class Canje
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Guid RecompensaId { get; set; }
    public Recompensa? Recompensa { get; set; }
    public int PuntosGastados { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = "requested";
    [MaxLength(20)] public required string Codigo { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? VenceEn { get; set; }
    public DateTime? UsadoEn { get; set; }
}

public sealed class SolicitudRetiro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [MaxLength(180)] public required string Direccion { get; set; }
    [MaxLength(200)] public required string Materiales { get; set; }
    public DateTime ProgramadoPara { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = "scheduled";
    [MaxLength(300)] public string? Notas { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public sealed class PuntoLimpio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public required string Nombre { get; set; }
    [MaxLength(180)] public required string Direccion { get; set; }
    [MaxLength(200)] public required string Materiales { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    [MaxLength(100)] public required string Horario { get; set; }
    public bool Abierto { get; set; } = true;
    [MaxLength(20)] public string EstadoOperativo { get; set; } = "open";
    public int? CapacidadPorcentaje { get; set; }
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? CodigoVerificacionHash { get; set; }
}

public sealed class ContenidoEducativo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public required string Titulo { get; set; }
    [MaxLength(500)] public required string Descripcion { get; set; }
    [MaxLength(30)] public required string Tipo { get; set; }
    [MaxLength(40)] public required string Categoria { get; set; }
    public int DuracionMinutos { get; set; }
    public int PuntosRecompensa { get; set; }
    [MaxLength(500)] public string? ContenidoUrl { get; set; }
    [MaxLength(200)] public string? RespuestaCorrecta { get; set; }
}

public sealed class Mision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public required string Titulo { get; set; }
    [MaxLength(400)] public required string Descripcion { get; set; }
    [MaxLength(30)] public required string Tipo { get; set; }
    [MaxLength(30)] public string? Material { get; set; }
    public decimal Objetivo { get; set; }
    public int RecompensaExperiencia { get; set; }
    public int RecompensaMonedas { get; set; }
    public DateTime IniciaEn { get; set; }
    public DateTime TerminaEn { get; set; }
    public bool Activa { get; set; } = true;
}

public sealed class ProgresoMision
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Guid MisionId { get; set; }
    public Mision? Mision { get; set; }
    public decimal Progreso { get; set; }
    public bool Completada { get; set; }
    public bool Reclamada { get; set; }
    public DateTime? CompletadaEn { get; set; }
    public DateTime? ReclamadaEn { get; set; }
}

public sealed class Insignia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public required string Codigo { get; set; }
    [MaxLength(100)] public required string Nombre { get; set; }
    [MaxLength(300)] public required string Descripcion { get; set; }
    [MaxLength(40)] public required string Icono { get; set; }
    [MaxLength(30)] public required string Criterio { get; set; }
    public decimal Umbral { get; set; }
}

public sealed class UsuarioInsignia
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Guid InsigniaId { get; set; }
    public Insignia? Insignia { get; set; }
    public DateTime ObtenidaEn { get; set; } = DateTime.UtcNow;
}

public sealed class ProgresoEducativo
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Guid ContenidoEducativoId { get; set; }
    public ContenidoEducativo? ContenidoEducativo { get; set; }
    public int ExperienciaGanada { get; set; }
    public int MonedasGanadas { get; set; }
    public DateTime CompletadoEn { get; set; } = DateTime.UtcNow;
}

public sealed class MetaComunitaria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public required string Titulo { get; set; }
    [MaxLength(300)] public required string Descripcion { get; set; }
    public decimal ObjetivoKg { get; set; }
    public DateTime IniciaEn { get; set; }
    public DateTime TerminaEn { get; set; }
    public bool Activa { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;

namespace Reciclame.Api.Models;

public static class Roles
{
    public const string Residente = "resident";
    public const string Turista = "tourist";
    public const string Municipal = "municipal";
}

public sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public required string Nombre { get; set; }
    [MaxLength(180)] public required string Correo { get; set; }
    [MaxLength(500)] public required string HashContrasena { get; set; }
    [MaxLength(20)] public string Rol { get; set; } = Roles.Residente;
    public int Puntos { get; set; }
    public int Nivel { get; set; } = 1;
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
    [MaxLength(20)] public string Estado { get; set; } = "validated";
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
}

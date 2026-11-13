using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize(Roles = Roles.Municipal)]
[Route("api/admin")]
public sealed class AdminController(
    AppDbContext db,
    GamificationService gamification,
    PasswordService passwords) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult> Overview()
    {
        var validated = db.RegistrosReciclaje.Where(x => x.Estado == EstadosReciclaje.Validado);
        var materialSummary = await validated.GroupBy(x => x.Material)
            .Select(x => new { material = x.Key, kg = x.Sum(y => y.PesoKg), entregas = x.Count() })
            .OrderByDescending(x => x.kg).ToListAsync();
        return Ok(new
        {
            usuarios = await db.Usuarios.CountAsync(x => x.Rol != Roles.Municipal),
            kgReciclados = await validated.SumAsync(x => (decimal?)x.PesoKg) ?? 0,
            reciclajesPendientes = await db.RegistrosReciclaje.CountAsync(x => x.Estado == EstadosReciclaje.Pendiente),
            retirosPendientes = await db.SolicitudesRetiro.CountAsync(x => x.Estado == "scheduled"),
            canjesPendientes = await db.Canjes.CountAsync(x => x.Estado == "requested"),
            porMaterial = materialSummary,
            reciclajesRecientes = await db.RegistrosReciclaje.Include(x => x.Usuario)
                .OrderByDescending(x => x.CreadoEn).Take(10)
                .Select(x => new { x.Id, x.Material, x.PesoKg, x.PuntosGanados, x.Estado, x.CreadoEn, usuario = x.Usuario!.Nombre })
                .ToListAsync(),
            solicitudesRecientes = await db.SolicitudesRetiro.Include(x => x.Usuario)
                .OrderByDescending(x => x.CreadoEn).Take(10)
                .Select(x => new { x.Id, x.Direccion, x.Materiales, x.ProgramadoPara, x.Estado, usuario = x.Usuario!.Nombre })
                .ToListAsync()
        });
    }

    [HttpGet("recycling/pending")]
    public async Task<ActionResult> PendingRecycling() => Ok(await db.RegistrosReciclaje
        .Where(x => x.Estado == EstadosReciclaje.Pendiente)
        .Include(x => x.Usuario).Include(x => x.PuntoLimpio)
        .OrderBy(x => x.CreadoEn)
        .Select(x => new
        {
            x.Id, x.Material, x.PesoKg, puntosEstimados = x.PuntosGanados, x.CreadoEn,
            usuario = x.Usuario!.Nombre,
            puntoLimpio = x.PuntoLimpio == null ? null : x.PuntoLimpio.Nombre
        }).ToListAsync());

    [HttpPost("recycling/{id:guid}/validate")]
    public async Task<ActionResult> ValidateRecycling(Guid id, SolicitudValidarReciclaje request)
    {
        var record = await db.RegistrosReciclaje.Include(x => x.Usuario).SingleOrDefaultAsync(x => x.Id == id);
        if (record is null) return NotFound();
        if (record.Estado != EstadosReciclaje.Pendiente)
            return Conflict(new ProblemDetails { Title = "Este reciclaje ya fue revisado." });

        if (!request.Aprobar)
        {
            record.Estado = EstadosReciclaje.Rechazado;
            record.MotivoRechazo = string.IsNullOrWhiteSpace(request.Motivo)
                ? "La entrega no pudo ser verificada."
                : request.Motivo.Trim();
            record.ValidadoPorId = User.UsuarioId();
            record.ValidadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Ok(new { record.Id, record.Estado, record.MotivoRechazo });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (request.PesoVerificadoKg is not null) record.PesoKg = request.PesoVerificadoKg.Value;
        record.PuntosGanados = gamification.CalculatePoints(record.Material, record.PesoKg);
        record.Estado = EstadosReciclaje.Validado;
        record.ValidadoPorId = User.UsuarioId();
        record.ValidadoEn = DateTime.UtcNow;
        record.MotivoRechazo = null;
        var user = record.Usuario!;
        gamification.Award(user, record.PuntosGanados, record.PuntosGanados);
        await gamification.AdvanceMissionsAsync(user.Id, "recycle", record.PesoKg, record.Material);
        if (record.PuntoLimpioId is not null) await gamification.AdvanceMissionsAsync(user.Id, "visit_point");
        await db.SaveChangesAsync();
        await gamification.AwardBadgesAsync(user.Id);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new
        {
            record.Id, record.Estado, record.PesoKg, record.PuntosGanados,
            usuario = user.ToResponse()
        });
    }

    [HttpGet("clean-points")]
    public async Task<ActionResult> CleanPoints() => Ok(await db.PuntosLimpios.OrderBy(x => x.Nombre)
        .Select(x => new
        {
            x.Id, x.Nombre, x.Direccion, x.Materiales, x.Latitud, x.Longitud, x.Horario,
            x.Abierto, x.EstadoOperativo, x.CapacidadPorcentaje, x.UltimaActualizacion,
            tieneCodigoVerificacion = x.CodigoVerificacionHash != null
        }).ToListAsync());

    [HttpPost("clean-points")]
    public async Task<ActionResult> CreateCleanPoint(SolicitudPuntoLimpio request)
    {
        var point = BuildCleanPoint(request);
        db.PuntosLimpios.Add(point);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(CleanPoints), new { point.Id });
    }

    [HttpPut("clean-points/{id:guid}")]
    public async Task<ActionResult> UpdateCleanPoint(Guid id, SolicitudPuntoLimpio request)
    {
        var point = await db.PuntosLimpios.FindAsync(id);
        if (point is null) return NotFound();
        ApplyCleanPoint(point, request);
        await db.SaveChangesAsync();
        return Ok(new { point.Id, point.Nombre, point.EstadoOperativo, point.UltimaActualizacion });
    }

    [HttpDelete("clean-points/{id:guid}")]
    public async Task<IActionResult> DeleteCleanPoint(Guid id)
    {
        var point = await db.PuntosLimpios.FindAsync(id);
        if (point is null) return NotFound();
        db.PuntosLimpios.Remove(point);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("redemptions/{id:guid}")]
    public async Task<ActionResult> UpdateRedemption(Guid id, SolicitudActualizarCanje request)
    {
        var redemption = await db.Canjes.FindAsync(id);
        if (redemption is null) return NotFound();
        redemption.Estado = request.Estado;
        redemption.UsadoEn = request.Estado == "used" ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        return Ok(new { redemption.Id, redemption.Codigo, redemption.Estado, redemption.UsadoEn });
    }

    [HttpGet("redemptions")]
    public async Task<ActionResult> Redemptions([FromQuery] string? status = null)
    {
        var query = db.Canjes.Include(x => x.Usuario).Include(x => x.Recompensa).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Estado == status);
        return Ok(await query.OrderByDescending(x => x.CreadoEn).Take(100)
            .Select(x => new
            {
                x.Id, x.Codigo, x.Estado, x.PuntosGastados, x.CreadoEn, x.VenceEn, x.UsadoEn,
                usuario = x.Usuario == null ? string.Empty : x.Usuario.Nombre,
                recompensa = x.Recompensa == null ? string.Empty : x.Recompensa.Nombre
            }).ToListAsync());
    }

    private PuntoLimpio BuildCleanPoint(SolicitudPuntoLimpio request)
    {
        var point = new PuntoLimpio
        {
            Nombre = request.Nombre.Trim(), Direccion = request.Direccion.Trim(),
            Materiales = request.Materiales.Trim(), Latitud = request.Latitud,
            Longitud = request.Longitud, Horario = request.Horario.Trim()
        };
        ApplyCleanPoint(point, request);
        return point;
    }

    private void ApplyCleanPoint(PuntoLimpio point, SolicitudPuntoLimpio request)
    {
        point.Nombre = request.Nombre.Trim();
        point.Direccion = request.Direccion.Trim();
        point.Materiales = request.Materiales.Trim();
        point.Latitud = request.Latitud;
        point.Longitud = request.Longitud;
        point.Horario = request.Horario.Trim();
        point.EstadoOperativo = request.EstadoOperativo;
        point.Abierto = request.EstadoOperativo == "open";
        point.CapacidadPorcentaje = request.CapacidadPorcentaje;
        point.UltimaActualizacion = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.CodigoVerificacion))
            point.CodigoVerificacionHash = passwords.Hash(request.CodigoVerificacion.Trim());
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/recycling")]
public sealed class RecyclingController(
    AppDbContext db,
    GamificationService gamification,
    PasswordService passwords) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List() => Ok(await db.RegistrosReciclaje
        .Where(x => x.UsuarioId == User.UsuarioId())
        .OrderByDescending(x => x.CreadoEn)
        .Select(x => new
        {
            x.Id, x.Material, x.PesoKg, x.PuntosGanados, x.Estado, x.PuntoLimpioId,
            puntoLimpio = x.PuntoLimpio == null ? null : x.PuntoLimpio.Nombre,
            x.ValidadoEn, x.MotivoRechazo, x.CreadoEn
        }).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(SolicitudCrearReciclaje request)
    {
        var material = request.Material.Trim().ToLowerInvariant();
        if (!gamification.IsSupportedMaterial(material))
            return BadRequest(new ProblemDetails { Title = "Material no admitido." });
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        if (usuario is null) return NotFound();

        var dayStart = DateTime.UtcNow.Date;
        var kgToday = await db.RegistrosReciclaje
            .Where(x => x.UsuarioId == usuario.Id && x.CreadoEn >= dayStart && x.Estado != EstadosReciclaje.Rechazado)
            .SumAsync(x => (decimal?)x.PesoKg) ?? 0;
        if (kgToday + request.PesoKg > 50)
            return Conflict(new ProblemDetails { Title = "Alcanzaste el limite diario de 50 kg. Solicita validacion municipal para una entrega mayor." });

        var duplicateSince = DateTime.UtcNow.AddMinutes(-2);
        if (await db.RegistrosReciclaje.AnyAsync(x => x.UsuarioId == usuario.Id &&
            x.Material == material && x.PesoKg == request.PesoKg && x.CreadoEn >= duplicateSince))
            return Conflict(new ProblemDetails { Title = "Esta entrega ya fue registrada recientemente." });

        PuntoLimpio? point = null;
        if (request.PuntoLimpioId is not null)
        {
            point = await db.PuntosLimpios.FindAsync(request.PuntoLimpioId.Value);
            if (point is null) return BadRequest(new ProblemDetails { Title = "El punto limpio seleccionado no existe." });
            if (!point.Abierto || point.EstadoOperativo != "open")
                return Conflict(new ProblemDetails { Title = "El punto limpio seleccionado no está operativo en este momento." });
        }

        var puntos = gamification.CalculatePoints(material, request.PesoKg);
        var codeValid = point?.CodigoVerificacionHash is not null &&
                        !string.IsNullOrWhiteSpace(request.CodigoVerificacion) &&
                        passwords.Verify(request.CodigoVerificacion.Trim(), point.CodigoVerificacionHash);
        var registro = new RegistroReciclaje
        {
            UsuarioId = usuario.Id,
            Material = material,
            PesoKg = request.PesoKg,
            PuntosGanados = puntos,
            PuntoLimpioId = point?.Id,
            Estado = codeValid ? EstadosReciclaje.Validado : EstadosReciclaje.Pendiente,
            ValidadoEn = codeValid ? DateTime.UtcNow : null
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        db.RegistrosReciclaje.Add(registro);
        if (codeValid)
        {
            gamification.Award(usuario, puntos, puntos);
            await gamification.AdvanceMissionsAsync(usuario.Id, "recycle", request.PesoKg, material);
            if (point is not null) await gamification.AdvanceMissionsAsync(usuario.Id, "visit_point");
        }
        await db.SaveChangesAsync();
        if (codeValid)
        {
            await gamification.AwardBadgesAsync(usuario.Id);
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(List), new
        {
            registro.Id,
            registro.Material,
            registro.PesoKg,
            registro.PuntosGanados,
            puntosEstimados = puntos,
            puntosPendientes = codeValid ? 0 : puntos,
            registro.Estado,
            registro.PuntoLimpioId,
            registro.CreadoEn,
            puntosTotales = usuario.Puntos,
            ecoMonedas = usuario.Puntos,
            experiencia = usuario.Experiencia,
            nivel = usuario.Nivel
        });
    }
}

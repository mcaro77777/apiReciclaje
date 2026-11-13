using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/rewards")]
public sealed class RewardsController(AppDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> List() => Ok(await db.Recompensas
        .Where(x => x.Activa && (x.ValidaHasta == null || x.ValidaHasta >= DateTime.UtcNow))
        .OrderBy(x => x.CostoPuntos)
        .Select(x => new
        {
            x.Id, x.Nombre, x.Descripcion, x.Categoria, x.Icono, x.CostoPuntos,
            x.Stock, x.Socio, x.Ubicacion, x.Terminos, x.ValidaHasta
        }).ToListAsync());

    [HttpGet("history")]
    public async Task<ActionResult> History() => Ok(await db.Canjes
        .Where(x => x.UsuarioId == User.UsuarioId())
        .Include(x => x.Recompensa)
        .OrderByDescending(x => x.CreadoEn)
        .Select(x => new
        {
            x.Id, x.Codigo, x.Estado, x.PuntosGastados, x.CreadoEn, x.VenceEn, x.UsadoEn,
            recompensaId = x.RecompensaId,
            recompensa = x.Recompensa == null ? string.Empty : x.Recompensa.Nombre,
            socio = x.Recompensa == null ? null : x.Recompensa.Socio,
            ubicacion = x.Recompensa == null ? null : x.Recompensa.Ubicacion
        }).ToListAsync());

    [HttpPost("{id:guid}/redeem")]
    public async Task<ActionResult> Redeem(Guid id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        var recompensa = await db.Recompensas.FindAsync(id);
        if (usuario is null || recompensa is null || !recompensa.Activa) return NotFound();
        if (recompensa.Stock <= 0) return Conflict(new ProblemDetails { Title = "Recompensa sin stock." });
        if (recompensa.ValidaHasta is not null && recompensa.ValidaHasta < DateTime.UtcNow)
            return Conflict(new ProblemDetails { Title = "Esta recompensa ya no está vigente." });
        if (usuario.Puntos < recompensa.CostoPuntos) return Conflict(new ProblemDetails { Title = "No tienes puntos suficientes." });

        usuario.Puntos -= recompensa.CostoPuntos;
        recompensa.Stock--;
        var canje = new Canje
        {
            UsuarioId = usuario.Id, RecompensaId = recompensa.Id, PuntosGastados = recompensa.CostoPuntos,
            Codigo = $"RAP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            VenceEn = recompensa.ValidaHasta is { } validUntil && validUntil < DateTime.UtcNow.AddDays(30)
                ? validUntil
                : DateTime.UtcNow.AddDays(30)
        };
        db.Canjes.Add(canje);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new
        {
            canje.Id, canje.Codigo, canje.Estado, canje.VenceEn,
            puntosRestantes = usuario.Puntos, ecoMonedas = usuario.Puntos,
            experiencia = usuario.Experiencia, nivel = usuario.Nivel
        });
    }
}

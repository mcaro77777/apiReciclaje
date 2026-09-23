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
    public async Task<ActionResult> List() => Ok(await db.Recompensas.Where(x => x.Activa).OrderBy(x => x.CostoPuntos).ToListAsync());

    [HttpPost("{id:guid}/redeem")]
    public async Task<ActionResult> Redeem(Guid id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        var recompensa = await db.Recompensas.FindAsync(id);
        if (usuario is null || recompensa is null || !recompensa.Activa) return NotFound();
        if (recompensa.Stock <= 0) return Conflict(new ProblemDetails { Title = "Recompensa sin stock." });
        if (usuario.Puntos < recompensa.CostoPuntos) return Conflict(new ProblemDetails { Title = "No tienes puntos suficientes." });

        usuario.Puntos -= recompensa.CostoPuntos;
        recompensa.Stock--;
        var canje = new Canje
        {
            UsuarioId = usuario.Id, RecompensaId = recompensa.Id, PuntosGastados = recompensa.CostoPuntos,
            Codigo = $"RAP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
        };
        db.Canjes.Add(canje);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { canje.Id, canje.Codigo, canje.Estado, puntosRestantes = usuario.Puntos });
    }
}

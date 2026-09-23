using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/recycling")]
public sealed class RecyclingController(AppDbContext db) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, int> PointsPerKg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["plastico"] = 120, ["vidrio"] = 80, ["aluminio"] = 180,
        ["papel"] = 60, ["organico"] = 40
    };

    [HttpGet]
    public async Task<ActionResult> List() => Ok(await db.RegistrosReciclaje
        .Where(x => x.UsuarioId == User.UsuarioId()).OrderByDescending(x => x.CreadoEn).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(SolicitudCrearReciclaje request)
    {
        var material = request.Material.Trim().ToLowerInvariant();
        if (!PointsPerKg.TryGetValue(material, out var multiplier))
            return BadRequest(new ProblemDetails { Title = "Material no admitido." });
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        if (usuario is null) return NotFound();
        var puntos = Math.Max(1, (int)Math.Round(request.PesoKg * multiplier));
        var registro = new RegistroReciclaje
        {
            UsuarioId = usuario.Id, Material = material, PesoKg = request.PesoKg, PuntosGanados = puntos
        };
        usuario.Puntos += puntos;
        usuario.Nivel = Math.Max(1, usuario.Puntos / 500 + 1);
        db.RegistrosReciclaje.Add(registro);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { registro.Id, registro.Material, registro.PesoKg, registro.PuntosGanados, registro.CreadoEn, puntosTotales = usuario.Puntos, nivel = usuario.Nivel });
    }
}

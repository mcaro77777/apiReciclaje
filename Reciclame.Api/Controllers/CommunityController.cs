using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;

namespace Reciclame.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController(AppDbContext db) : ControllerBase
{
    private static readonly string[] Sectors =
    [
        "Tahai",
        "Moeroa",
        "Hanga Roa Centro",
        "Mataveri",
        "Vai a Repa"
    ];

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.UsuarioId();
        var currentSector = await db.Usuarios
            .Where(x => x.Id == userId)
            .Select(x => x.Sector)
            .SingleAsync(cancellationToken);

        var totals = await db.RegistrosReciclaje
            .Where(x => x.Estado == EstadosReciclaje.Validado && x.Usuario!.Sector != null)
            .GroupBy(x => x.Usuario!.Sector!)
            .Select(group => new
            {
                sector = group.Key,
                puntos = group.Sum(x => x.PuntosGanados),
                kg = group.Sum(x => x.PesoKg)
            })
            .ToListAsync(cancellationToken);
        var members = await db.Usuarios
            .Where(x => x.Sector != null && x.Rol == Roles.Residente)
            .GroupBy(x => x.Sector!)
            .Select(group => new { sector = group.Key, miembros = group.Count() })
            .ToListAsync(cancellationToken);

        var ranking = Sectors
            .Select(sector => new
            {
                nombre = sector,
                puntos = totals.FirstOrDefault(x => x.sector == sector)?.puntos ?? 0,
                kg = totals.FirstOrDefault(x => x.sector == sector)?.kg ?? 0,
                miembros = members.FirstOrDefault(x => x.sector == sector)?.miembros ?? 0,
                propio = sector == currentSector
            })
            .OrderByDescending(x => x.puntos)
            .ThenByDescending(x => x.kg)
            .ThenBy(x => x.nombre)
            .ToList();

        return Ok(new { sectorActual = currentSector, sectores = ranking });
    }

    [HttpPut("sector")]
    public async Task<ActionResult> UpdateSector(
        ActualizarSectorRequest request,
        CancellationToken cancellationToken)
    {
        var sector = Sectors.FirstOrDefault(
            value => string.Equals(value, request.Sector.Trim(), StringComparison.OrdinalIgnoreCase));
        if (sector is null)
            return BadRequest(new ProblemDetails { Title = "Selecciona un sector válido de Rapa Nui." });

        var user = await db.Usuarios.SingleAsync(x => x.Id == User.UsuarioId(), cancellationToken);
        if (user.Rol != Roles.Residente)
            return Forbid();
        user.Sector = sector;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

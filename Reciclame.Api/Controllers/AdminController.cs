using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Data;
using Reciclame.Api.Models;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize(Roles = Roles.Municipal)]
[Route("api/admin")]
public sealed class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult> Overview() => Ok(new
    {
        usuarios = await db.Usuarios.CountAsync(),
        kgReciclados = await db.RegistrosReciclaje.SumAsync(x => (decimal?)x.PesoKg) ?? 0,
        retirosPendientes = await db.SolicitudesRetiro.CountAsync(x => x.Estado == "scheduled"),
        canjesPendientes = await db.Canjes.CountAsync(x => x.Estado == "requested"),
        reciclajesRecientes = await db.RegistrosReciclaje.Include(x => x.Usuario).OrderByDescending(x => x.CreadoEn)
            .Take(10).Select(x => new { x.Id, x.Material, x.PesoKg, x.PuntosGanados, x.CreadoEn, usuario = x.Usuario!.Nombre }).ToListAsync()
    });
}

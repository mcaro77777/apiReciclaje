using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/pickups")]
public sealed class PickupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List() => Ok(await db.SolicitudesRetiro
        .Where(x => x.UsuarioId == User.UsuarioId()).OrderByDescending(x => x.ProgramadoPara).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(SolicitudCrearRetiro request)
    {
        if (request.ProgramadoPara < DateTime.UtcNow.AddHours(12))
            return BadRequest(new ProblemDetails { Title = "Programa el retiro con al menos 12 horas de anticipacion." });
        var retiro = new SolicitudRetiro
        {
            UsuarioId = User.UsuarioId(), Direccion = request.Direccion.Trim(), Materiales = request.Materiales.Trim(),
            ProgramadoPara = request.ProgramadoPara.ToUniversalTime(), Notas = request.Notas?.Trim()
        };
        db.SolicitudesRetiro.Add(retiro);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), retiro);
    }
}

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
[Route("api/users")]
public sealed class UsersController(AppDbContext db, GamificationService gamification) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<RespuestaUsuario>> Me()
    {
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        return usuario is null ? NotFound() : Ok(usuario.ToResponse());
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard()
    {
        var id = User.UsuarioId();
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        var registros = await db.RegistrosReciclaje.Where(x => x.UsuarioId == id).ToListAsync();
        var validados = registros.Where(x => x.Estado == EstadosReciclaje.Validado).ToList();
        var pendientes = registros.Where(x => x.Estado == EstadosReciclaje.Pendiente).ToList();
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return Ok(new
        {
            usuario = usuario.ToResponse(),
            kgTotales = validados.Sum(x => x.PesoKg),
            kgValidados = validados.Sum(x => x.PesoKg),
            kgPendientes = pendientes.Sum(x => x.PesoKg),
            kgMensuales = validados.Where(x => x.CreadoEn >= inicioMes).Sum(x => x.PesoKg),
            cantidadReciclajes = validados.Count,
            reciclajesPendientes = pendientes.Count,
            co2EvitadoKg = validados.Sum(x => gamification.CalculateCo2(x.Material, x.PesoKg)),
            puntosSiguienteNivel = usuario.Nivel * 500,
            recientes = registros.OrderByDescending(x => x.CreadoEn).Take(4).Select(x => new
            {
                x.Id,
                x.Material,
                x.PesoKg,
                x.PuntosGanados,
                x.Estado,
                x.CreadoEn
            })
        });
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var id = User.UsuarioId();
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Canjes.Where(x => x.UsuarioId == id).ExecuteDeleteAsync();
        await db.ProgresosMision.Where(x => x.UsuarioId == id).ExecuteDeleteAsync();
        await db.ProgresosEducativos.Where(x => x.UsuarioId == id).ExecuteDeleteAsync();
        await db.UsuariosInsignias.Where(x => x.UsuarioId == id).ExecuteDeleteAsync();
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }
}

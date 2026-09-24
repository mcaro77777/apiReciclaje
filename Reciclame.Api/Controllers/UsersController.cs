using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/users")]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<RespuestaUsuario>> Me()
    {
        var usuario = await db.Usuarios.FindAsync(User.UsuarioId());
        return usuario is null ? NotFound() : Ok(new RespuestaUsuario(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol, usuario.Puntos, usuario.Nivel));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard()
    {
        var id = User.UsuarioId();
        var usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        var registros = await db.RegistrosReciclaje.Where(x => x.UsuarioId == id).ToListAsync();
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return Ok(new
        {
            usuario = new RespuestaUsuario(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol, usuario.Puntos, usuario.Nivel),
            kgTotales = registros.Sum(x => x.PesoKg),
            kgMensuales = registros.Where(x => x.CreadoEn >= inicioMes).Sum(x => x.PesoKg),
            cantidadReciclajes = registros.Count,
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
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;

namespace Reciclame.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ContentController(AppDbContext db) : ControllerBase
{
    [AllowAnonymous, HttpGet("clean-points")]
    public async Task<ActionResult> CleanPoints() => Ok(await db.PuntosLimpios.OrderBy(x => x.Nombre).ToListAsync());

    [AllowAnonymous, HttpGet("education")]
    public async Task<ActionResult> Education() => Ok(await db.ContenidosEducativos.OrderByDescending(x => x.PuntosRecompensa).ToListAsync());

    [Authorize, HttpPost("vananga")]
    public ActionResult Vananga(SolicitudVananga request)
    {
        var mensaje = request.Mensaje.ToLowerInvariant();
        var respuesta = mensaje.Contains("hola") ? "Iorana! Asi saludamos en rapanui."
            : mensaje.Contains("gracias") ? "Maururu significa gracias."
            : mensaje.Contains("mar") || mensaje.Contains("oceano") ? "Moana significa oceano: cuidarlo es parte de nuestra responsabilidad."
            : "Iorana. Puedo ayudarte con palabras basicas y consejos para cuidar Rapa Nui.";
        return Ok(new { respuesta, simulado = true });
    }
}

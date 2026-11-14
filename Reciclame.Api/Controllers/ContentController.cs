using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ContentController(AppDbContext db, GamificationService gamification) : ControllerBase
{
    [AllowAnonymous, HttpGet("clean-points")]
    public async Task<ActionResult> CleanPoints() => Ok(await db.PuntosLimpios
        .OrderBy(x => x.Nombre)
        .Select(x => new
        {
            x.Id, x.Nombre, x.Direccion, x.Materiales, x.Latitud, x.Longitud, x.Horario,
            x.Abierto, x.EstadoOperativo, x.CapacidadPorcentaje, x.UltimaActualizacion
        }).ToListAsync());

    [AllowAnonymous, HttpGet("education")]
    public async Task<ActionResult> Education() => Ok(await db.ContenidosEducativos
        .OrderByDescending(x => x.PuntosRecompensa)
        .Select(x => new
        {
            x.Id, x.Titulo, x.Descripcion, x.Tipo, x.Categoria, x.DuracionMinutos,
            x.PuntosRecompensa, x.ContenidoUrl
        }).ToListAsync());

    [Authorize, HttpPost("education/{id:guid}/complete")]
    public async Task<ActionResult> CompleteEducation(
        Guid id,
        SolicitudCompletarContenido request,
        CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<ActionResult>(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var userId = User.UsuarioId();
            if (await db.ProgresosEducativos.AnyAsync(
                    x => x.UsuarioId == userId && x.ContenidoEducativoId == id, cancellationToken))
                return Conflict(new ProblemDetails { Title = "Este contenido ya fue completado." });

            var content = await db.ContenidosEducativos.FindAsync([id], cancellationToken);
            var user = await db.Usuarios.FindAsync([userId], cancellationToken);
            if (content is null || user is null) return NotFound();
            if (content.Tipo.Equals("quiz", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(content.RespuestaCorrecta) &&
                Normalize(request.Respuesta) != Normalize(content.RespuestaCorrecta))
            {
                return BadRequest(new ProblemDetails { Title = "La respuesta no es correcta. Inténtalo nuevamente." });
            }

            var reward = Math.Max(0, content.PuntosRecompensa);
            db.ProgresosEducativos.Add(new ProgresoEducativo
            {
                UsuarioId = userId,
                ContenidoEducativoId = id,
                ExperienciaGanada = reward,
                MonedasGanadas = reward
            });
            gamification.Award(user, reward, reward);
            await gamification.AdvanceMissionsAsync(userId, "education", cancellationToken: cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await gamification.AwardBadgesAsync(userId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { contenidoId = id, experienciaGanada = reward, ecoMonedasGanadas = reward, usuario = user.ToResponse() });
        });
    }

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

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

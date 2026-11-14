using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Data;
using Reciclame.Api.Extensions;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Controllers;

[ApiController, Authorize]
[Route("api/gamification")]
public sealed class GamificationController(AppDbContext db, GamificationService gamification) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Overview(CancellationToken cancellationToken)
    {
        var userId = User.UsuarioId();
        var user = await db.Usuarios.FindAsync([userId], cancellationToken);
        if (user is null) return NotFound();

        var now = DateTime.UtcNow;
        var missions = await db.Misiones
            .Where(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now)
            .OrderBy(x => x.TerminaEn)
            .ToListAsync(cancellationToken);
        var missionIds = missions.Select(x => x.Id).ToArray();
        var progressByMission = await db.ProgresosMision
            .Where(x => x.UsuarioId == userId && missionIds.Contains(x.MisionId))
            .ToDictionaryAsync(x => x.MisionId, cancellationToken);

        var earnedBadges = await db.UsuariosInsignias
            .Where(x => x.UsuarioId == userId)
            .ToDictionaryAsync(x => x.InsigniaId, x => x.ObtenidaEn, cancellationToken);
        var badges = await db.Insignias.OrderBy(x => x.Umbral).ToListAsync(cancellationToken);
        var completedEducationIds = await db.ProgresosEducativos
            .Where(x => x.UsuarioId == userId)
            .Select(x => x.ContenidoEducativoId)
            .ToListAsync(cancellationToken);

        var communityGoal = await db.MetasComunitarias
            .Where(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now)
            .OrderBy(x => x.TerminaEn)
            .FirstOrDefaultAsync(cancellationToken);
        decimal communityKg = 0;
        if (communityGoal is not null)
        {
            communityKg = await db.RegistrosReciclaje
                .Where(x => x.Estado == EstadosReciclaje.Validado &&
                            x.ValidadoEn >= communityGoal.IniciaEn && x.ValidadoEn <= communityGoal.TerminaEn)
                .SumAsync(x => (decimal?)x.PesoKg, cancellationToken) ?? 0;
        }

        return Ok(new
        {
            usuario = user.ToResponse(),
            experienciaSiguienteNivel = user.Nivel * 500,
            misiones = missions.Select(mission =>
            {
                progressByMission.TryGetValue(mission.Id, out var progress);
                return new
                {
                    mission.Id,
                    mission.Titulo,
                    mission.Descripcion,
                    mission.Tipo,
                    mission.Material,
                    mission.Objetivo,
                    progreso = progress?.Progreso ?? 0,
                    completada = progress?.Completada ?? false,
                    reclamada = progress?.Reclamada ?? false,
                    mission.RecompensaExperiencia,
                    mission.RecompensaMonedas,
                    mission.TerminaEn
                };
            }),
            insignias = badges.Select(badge => new
            {
                badge.Id,
                badge.Codigo,
                badge.Nombre,
                badge.Descripcion,
                badge.Icono,
                obtenida = earnedBadges.ContainsKey(badge.Id),
                obtenidaEn = earnedBadges.GetValueOrDefault(badge.Id)
            }),
            contenidosCompletados = completedEducationIds,
            metaComunitaria = communityGoal is null ? null : new
            {
                communityGoal.Id,
                communityGoal.Titulo,
                communityGoal.Descripcion,
                communityGoal.ObjetivoKg,
                progresoKg = communityKg,
                porcentaje = communityGoal.ObjetivoKg <= 0
                    ? 0
                    : Math.Min(100, Math.Round(communityKg / communityGoal.ObjetivoKg * 100, 1)),
                communityGoal.TerminaEn
            }
        });
    }

    [HttpPost("missions/{id:guid}/claim")]
    public async Task<ActionResult> ClaimMission(Guid id, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<ActionResult>(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var userId = User.UsuarioId();
            var progress = await db.ProgresosMision
                .Include(x => x.Mision)
                .SingleOrDefaultAsync(x => x.UsuarioId == userId && x.MisionId == id, cancellationToken);
            if (progress is null || progress.Mision is null)
                return NotFound(new ProblemDetails { Title = "La misión no tiene progreso registrado." });
            if (!progress.Completada)
                return Conflict(new ProblemDetails { Title = "Completa la misión antes de reclamarla." });
            if (progress.Reclamada)
                return Conflict(new ProblemDetails { Title = "Esta recompensa ya fue reclamada." });

            var user = await db.Usuarios.FindAsync([userId], cancellationToken);
            if (user is null) return NotFound();
            progress.Reclamada = true;
            progress.ReclamadaEn = DateTime.UtcNow;
            gamification.Award(user, progress.Mision.RecompensaMonedas, progress.Mision.RecompensaExperiencia);
            await db.SaveChangesAsync(cancellationToken);
            await gamification.AwardBadgesAsync(userId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new
            {
                progress.MisionId,
                progress.Reclamada,
                experienciaGanada = progress.Mision.RecompensaExperiencia,
                ecoMonedasGanadas = progress.Mision.RecompensaMonedas,
                usuario = user.ToResponse()
            });
        });
    }
}

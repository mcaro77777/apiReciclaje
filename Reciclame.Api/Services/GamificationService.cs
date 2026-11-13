using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Data;
using Reciclame.Api.Models;

namespace Reciclame.Api.Services;

public sealed class GamificationService(AppDbContext db)
{
    private static readonly IReadOnlyDictionary<string, int> PointsPerKg =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["plastico"] = 120,
            ["vidrio"] = 80,
            ["aluminio"] = 180,
            ["papel"] = 60,
            ["organico"] = 40
        };

    private static readonly IReadOnlyDictionary<string, decimal> Co2PerKg =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["plastico"] = 1.7m,
            ["vidrio"] = 0.31m,
            ["aluminio"] = 9.1m,
            ["papel"] = 1.1m,
            ["organico"] = 0.25m
        };

    public bool IsSupportedMaterial(string material) => PointsPerKg.ContainsKey(material);

    public int CalculatePoints(string material, decimal weightKg) =>
        Math.Max(1, (int)Math.Round(weightKg * PointsPerKg[material]));

    public int CalculateLevel(int experience) => Math.Max(1, experience / 500 + 1);

    public decimal CalculateCo2(string material, decimal weightKg) =>
        Math.Round(weightKg * (Co2PerKg.TryGetValue(material, out var factor) ? factor : 0.5m), 2);

    public void Award(Usuario user, int coins, int experience)
    {
        user.Puntos = Math.Max(0, user.Puntos + coins);
        user.Experiencia = Math.Max(0, user.Experiencia + experience);
        user.Nivel = CalculateLevel(user.Experiencia);
        TouchStreak(user, DateTime.UtcNow);
    }

    public void TouchStreak(Usuario user, DateTime activityUtc)
    {
        var activityWeek = StartOfWeek(activityUtc);
        if (user.UltimaActividadEn is null)
        {
            user.RachaSemanas = 1;
        }
        else
        {
            var previousWeek = StartOfWeek(user.UltimaActividadEn.Value);
            var weeks = (activityWeek - previousWeek).Days / 7;
            if (weeks == 1) user.RachaSemanas = Math.Max(1, user.RachaSemanas + 1);
            else if (weeks > 1) user.RachaSemanas = 1;
        }

        if (user.UltimaActividadEn is null || activityUtc > user.UltimaActividadEn)
            user.UltimaActividadEn = activityUtc;
    }

    public async Task AdvanceMissionsAsync(
        Guid userId,
        string eventType,
        decimal value = 1,
        string? material = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var missions = await db.Misiones
            .Where(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now)
            .ToListAsync(cancellationToken);

        foreach (var mission in missions)
        {
            decimal increment;
            if (eventType == "recycle" && mission.Tipo == "recycle_kg") increment = value;
            else if (eventType == "recycle" && mission.Tipo == "recycle_count") increment = 1;
            else if (eventType == "recycle" && mission.Tipo == "recycle_material" &&
                     string.Equals(mission.Material, material, StringComparison.OrdinalIgnoreCase)) increment = 1;
            else if (eventType == "education" && mission.Tipo == "education") increment = 1;
            else if (eventType == "visit_point" && mission.Tipo == "visit_point") increment = 1;
            else continue;

            var progress = await db.ProgresosMision.FindAsync([userId, mission.Id], cancellationToken);
            if (progress is null)
            {
                progress = new ProgresoMision { UsuarioId = userId, MisionId = mission.Id };
                db.ProgresosMision.Add(progress);
            }

            if (progress.Completada) continue;
            progress.Progreso = Math.Min(mission.Objetivo, progress.Progreso + increment);
            if (progress.Progreso >= mission.Objetivo)
            {
                progress.Completada = true;
                progress.CompletadaEn = now;
            }
        }
    }

    public async Task AwardBadgesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Usuarios.FindAsync([userId], cancellationToken);
        if (user is null) return;

        var validated = db.RegistrosReciclaje.Where(x =>
            x.UsuarioId == userId && x.Estado == EstadosReciclaje.Validado);
        var recyclingCount = await validated.CountAsync(cancellationToken);
        var kg = await validated.SumAsync(x => (decimal?)x.PesoKg, cancellationToken) ?? 0;
        var educationCount = await db.ProgresosEducativos.CountAsync(x => x.UsuarioId == userId, cancellationToken);
        var earnedIds = (await db.UsuariosInsignias.Where(x => x.UsuarioId == userId)
            .Select(x => x.InsigniaId).ToListAsync(cancellationToken)).ToHashSet();
        var badges = await db.Insignias.Where(x => !earnedIds.Contains(x.Id)).ToListAsync(cancellationToken);

        foreach (var badge in badges)
        {
            var value = badge.Criterio switch
            {
                "recycling_count" => recyclingCount,
                "kg_total" => kg,
                "education_count" => educationCount,
                "streak_weeks" => user.RachaSemanas,
                _ => 0
            };
            if (value < badge.Umbral) continue;
            db.UsuariosInsignias.Add(new UsuarioInsignia
            {
                UsuarioId = userId,
                InsigniaId = badge.Id
            });
        }
    }

    private static DateTime StartOfWeek(DateTime value)
    {
        var utcDate = value.ToUniversalTime().Date;
        var daysSinceMonday = ((int)utcDate.DayOfWeek + 6) % 7;
        return utcDate.AddDays(-daysSinceMonday);
    }
}

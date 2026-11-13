using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Data;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Tests;

public sealed class GamificationServiceTests
{
    private readonly GamificationService _service = new(
        new AppDbContext(new DbContextOptions<AppDbContext>()));

    [Theory]
    [InlineData("plastico", 2, 240)]
    [InlineData("vidrio", 1.5, 120)]
    [InlineData("aluminio", 0.5, 90)]
    [InlineData("papel", 3, 180)]
    [InlineData("organico", 2, 80)]
    public void CalculatePoints_UsesMaterialMultiplier(string material, double weight, int expected)
    {
        Assert.Equal(expected, _service.CalculatePoints(material, (decimal)weight));
    }

    [Fact]
    public void Award_SeparatesSpendableBalanceFromHistoricalExperience()
    {
        var user = NewUser();

        _service.Award(user, coins: 125, experience: 625);

        Assert.Equal(125, user.Puntos);
        Assert.Equal(625, user.Experiencia);
        Assert.Equal(2, user.Nivel);
        Assert.Equal(1, user.RachaSemanas);
        Assert.NotNull(user.UltimaActividadEn);
    }

    [Fact]
    public void TouchStreak_IncrementsOncePerConsecutiveWeekAndResetsAfterGap()
    {
        var user = NewUser();
        var monday = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        _service.TouchStreak(user, monday);
        _service.TouchStreak(user, monday.AddDays(2));
        Assert.Equal(1, user.RachaSemanas);

        _service.TouchStreak(user, monday.AddDays(7));
        Assert.Equal(2, user.RachaSemanas);

        _service.TouchStreak(user, monday.AddDays(28));
        Assert.Equal(1, user.RachaSemanas);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(499, 1)]
    [InlineData(500, 2)]
    [InlineData(1500, 4)]
    public void CalculateLevel_UsesFiveHundredExperienceSteps(int experience, int expected)
    {
        Assert.Equal(expected, _service.CalculateLevel(experience));
    }

    private static Usuario NewUser() => new()
    {
        Nombre = "Persona de prueba",
        Correo = "prueba@example.com",
        HashContrasena = "hash"
    };
}

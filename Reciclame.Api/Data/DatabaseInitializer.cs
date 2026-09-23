using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var servicioContrasenas = scope.ServiceProvider.GetRequiredService<PasswordService>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Usuarios.AnyAsync())
        {
            db.Usuarios.AddRange(
                new Usuario
                {
                    Nombre = "Moana Riroroko", Correo = "residente@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Residente,
                    Puntos = 1280, Nivel = 4
                },
                new Usuario
                {
                    Nombre = "Ana Visitante", Correo = "turista@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Turista,
                    Puntos = 320, Nivel = 2
                },
                new Usuario
                {
                    Nombre = "Equipo Municipal", Correo = "municipal@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Municipal,
                    Puntos = 0, Nivel = 1
                });
            await db.SaveChangesAsync();
        }

        if (!await db.RegistrosReciclaje.AnyAsync())
        {
            var residente = await db.Usuarios.SingleAsync(x => x.Correo == "residente@reciclame.cl");
            db.RegistrosReciclaje.AddRange(
                new RegistroReciclaje { UsuarioId = residente.Id, Material = "plastico", PesoKg = 2.4m, PuntosGanados = 288, CreadoEn = DateTime.UtcNow.AddDays(-1) },
                new RegistroReciclaje { UsuarioId = residente.Id, Material = "vidrio", PesoKg = 4.8m, PuntosGanados = 384, CreadoEn = DateTime.UtcNow.AddDays(-5) },
                new RegistroReciclaje { UsuarioId = residente.Id, Material = "papel", PesoKg = 1.7m, PuntosGanados = 102, CreadoEn = DateTime.UtcNow.AddDays(-10) });
        }

        if (!await db.Recompensas.AnyAsync())
        {
            db.Recompensas.AddRange(
                new Recompensa { Nombre = "Compost local", Descripcion = "Bolsa de compost producido en Orito.", Categoria = "Huerto", Icono = "compost", CostoPuntos = 450, Stock = 18 },
                new Recompensa { Nombre = "Almácigo nativo", Descripcion = "Planta nativa para recuperar jardines de la isla.", Categoria = "Naturaleza", Icono = "plant", CostoPuntos = 700, Stock = 12 },
                new Recompensa { Nombre = "Retiro de escombros", Descripcion = "Retiro municipal coordinado para tu domicilio.", Categoria = "Municipal", Icono = "truck", CostoPuntos = 1200, Stock = 8 },
                new Recompensa { Nombre = "Botella reutilizable", Descripcion = "Botella durable libre de plásticos de un solo uso.", Categoria = "Comunidad", Icono = "bottle", CostoPuntos = 900, Stock = 25 });
        }

        if (!await db.PuntosLimpios.AnyAsync())
        {
            db.PuntosLimpios.AddRange(
                new PuntoLimpio { Nombre = "Centro de reciclaje Orito", Direccion = "Camino a Orito", Materiales = "Vidrio, plástico, aluminio, papel y cartón", Latitud = -27.1356, Longitud = -109.4058, Horario = "Lun a Sáb · 08:30–17:30" },
                new PuntoLimpio { Nombre = "Punto limpio Hanga Roa", Direccion = "Av. Atamu Tekena", Materiales = "Vidrio, plástico y latas", Latitud = -27.1478, Longitud = -109.4294, Horario = "Todos los días · 08:00–20:00" },
                new PuntoLimpio { Nombre = "Punto limpio Tahai", Direccion = "Sector Tahai", Materiales = "Plástico, papel y cartón", Latitud = -27.1397, Longitud = -109.4393, Horario = "Lun a Vie · 09:00–18:00" });
        }

        if (!await db.ContenidosEducativos.AnyAsync())
        {
            db.ContenidosEducativos.AddRange(
                new ContenidoEducativo { Titulo = "Limpio, seco y separado", Descripcion = "Aprende a preparar tus residuos para que puedan ser valorizados.", Tipo = "video", Categoria = "Reciclaje", DuracionMinutos = 4, PuntosRecompensa = 20 },
                new ContenidoEducativo { Titulo = "Desafío del plástico", Descripcion = "Pon a prueba lo que sabes sobre plásticos de un solo uso.", Tipo = "quiz", Categoria = "Trivia", DuracionMinutos = 3, PuntosRecompensa = 40 },
                new ContenidoEducativo { Titulo = "Cuidemos el moana", Descripcion = "Conoce el impacto de los residuos en el océano que rodea Rapa Nui.", Tipo = "article", Categoria = "Territorio", DuracionMinutos = 5, PuntosRecompensa = 25 });
        }

        await db.SaveChangesAsync();
    }
}

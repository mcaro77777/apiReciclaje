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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSchemaUpgrader.UpgradeAsync(db);

        var seedDemoUsers = environment.IsDevelopment() || configuration.GetValue<bool>("Seed:DemoUsers");

        if (seedDemoUsers && !await db.Usuarios.AnyAsync())
        {
            db.Usuarios.AddRange(
                new Usuario
                {
                    Nombre = "Moana Riroroko", Correo = "residente@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Residente,
                    Puntos = 1280, Experiencia = 1780, Nivel = 4, RachaSemanas = 3,
                    Sector = "Hanga Roa Centro"
                },
                new Usuario
                {
                    Nombre = "Ana Visitante", Correo = "turista@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Turista,
                    Puntos = 320, Experiencia = 520, Nivel = 2, RachaSemanas = 1
                },
                new Usuario
                {
                    Nombre = "Equipo Municipal", Correo = "municipal@reciclame.cl",
                    HashContrasena = servicioContrasenas.Hash("Reciclame123!"), Rol = Roles.Municipal,
                    Puntos = 0, Nivel = 1
                });
            await db.SaveChangesAsync();
        }

        if (seedDemoUsers && !await db.RegistrosReciclaje.AnyAsync())
        {
            var residente = await db.Usuarios.SingleOrDefaultAsync(x => x.Correo == "residente@reciclame.cl");
            if (residente is not null)
            {
                db.RegistrosReciclaje.AddRange(
                    new RegistroReciclaje { UsuarioId = residente.Id, Material = "plastico", PesoKg = 2.4m, PuntosGanados = 288, Estado = EstadosReciclaje.Validado, ValidadoEn = DateTime.UtcNow.AddDays(-1), CreadoEn = DateTime.UtcNow.AddDays(-1) },
                    new RegistroReciclaje { UsuarioId = residente.Id, Material = "vidrio", PesoKg = 4.8m, PuntosGanados = 480, Estado = EstadosReciclaje.Validado, ValidadoEn = DateTime.UtcNow.AddDays(-5), CreadoEn = DateTime.UtcNow.AddDays(-5) },
                    new RegistroReciclaje { UsuarioId = residente.Id, Material = "papel", PesoKg = 1.7m, PuntosGanados = 136, Estado = EstadosReciclaje.Validado, ValidadoEn = DateTime.UtcNow.AddDays(-10), CreadoEn = DateTime.UtcNow.AddDays(-10) });
            }
        }

        if (!await db.Recompensas.AnyAsync())
        {
            db.Recompensas.AddRange(
                new Recompensa { Nombre = "Compost local", Descripcion = "Bolsa de compost producido en Orito.", Categoria = "Huerto", Icono = "compost", CostoPuntos = 800, Stock = 18, Socio = "Centro de reciclaje Orito", Ubicacion = "Camino a Orito", Terminos = "Un canje por persona al mes." },
                new Recompensa { Nombre = "Plantas para tu jardín", Descripcion = "Planta nativa para recuperar jardines de la isla.", Categoria = "Naturaleza", Icono = "plant", CostoPuntos = 1000, Stock = 12, Socio = "Vivero comunitario", Ubicacion = "Hanga Roa" },
                new Recompensa { Nombre = "Retiro de escombros", Descripcion = "Retiro municipal coordinado para tu domicilio.", Categoria = "Municipal", Icono = "truck", CostoPuntos = 5000, Stock = 8, Socio = "Municipalidad" },
                new Recompensa { Nombre = "Kit de bolsas reutilizables", Descripcion = "Bolsas durables para evitar plásticos de un solo uso.", Categoria = "Comunidad", Icono = "bottle", CostoPuntos = 400, Stock = 25, Socio = "Red de comercios circulares" });
        }

        // Completa catalogos creados por la version 1.x sin reemplazar datos
        // administrados posteriormente por el municipio.
        var compost = await db.Recompensas.FirstOrDefaultAsync(x => x.Nombre == "Compost local");
        if (compost is not null)
        {
            compost.CostoPuntos = 800;
            compost.Socio ??= "Centro de reciclaje Orito";
            compost.Ubicacion ??= "Camino a Orito";
            compost.Terminos ??= "Un canje por persona al mes, sujeto a stock.";
        }
        var plant = await db.Recompensas.FirstOrDefaultAsync(x => x.Nombre == "Almácigo nativo" || x.Nombre == "Plantas para tu jardín");
        if (plant is not null)
        {
            plant.Nombre = "Plantas para tu jardín";
            plant.CostoPuntos = 1000;
        }
        var debris = await db.Recompensas.FirstOrDefaultAsync(x => x.Nombre == "Retiro de escombros");
        if (debris is not null) debris.CostoPuntos = 5000;
        var bottle = await db.Recompensas.FirstOrDefaultAsync(x => x.Nombre == "Botella reutilizable" || x.Nombre == "Kit de bolsas reutilizables");
        if (bottle is not null)
        {
            bottle.Nombre = "Kit de bolsas reutilizables";
            bottle.Descripcion = "Bolsas durables para evitar plásticos de un solo uso.";
            bottle.CostoPuntos = 400;
            bottle.Socio ??= "Red de comercios circulares";
            bottle.Ubicacion ??= "Hanga Roa";
        }

        if (!await db.PuntosLimpios.AnyAsync())
        {
            db.PuntosLimpios.AddRange(
                new PuntoLimpio { Nombre = "Centro de reciclaje Orito", Direccion = "Camino a Orito", Materiales = "Vidrio, plástico, aluminio, papel y cartón", Latitud = -27.1356, Longitud = -109.4058, Horario = "Lun a Sáb · 08:30–17:30" },
                new PuntoLimpio { Nombre = "Punto limpio Hanga Roa", Direccion = "Av. Atamu Tekena", Materiales = "Vidrio, plástico y latas", Latitud = -27.1478, Longitud = -109.4294, Horario = "Todos los días · 08:00–20:00" },
                new PuntoLimpio { Nombre = "Punto limpio Tahai", Direccion = "Sector Tahai", Materiales = "Plástico, papel y cartón", Latitud = -27.1397, Longitud = -109.4393, Horario = "Lun a Vie · 09:00–18:00" });
        }
        await db.SaveChangesAsync();
        var defaultCleanPointCode = configuration["CleanPoints:DefaultVerificationCode"]?.Trim();
        if (!string.IsNullOrWhiteSpace(defaultCleanPointCode) && defaultCleanPointCode.Length >= 6)
        {
            foreach (var point in await db.PuntosLimpios.Where(x => x.CodigoVerificacionHash == null).ToListAsync())
                point.CodigoVerificacionHash = servicioContrasenas.Hash(defaultCleanPointCode);
        }

        if (!await db.ContenidosEducativos.AnyAsync())
        {
            db.ContenidosEducativos.AddRange(
                new ContenidoEducativo { Titulo = "Limpio, seco y separado", Descripcion = "Aprende a preparar tus residuos para que puedan ser valorizados.", Tipo = "video", Categoria = "Reciclaje", DuracionMinutos = 4, PuntosRecompensa = 20 },
                new ContenidoEducativo { Titulo = "Desafío del plástico", Descripcion = "Pon a prueba lo que sabes sobre plásticos de un solo uso.", Tipo = "quiz", Categoria = "Trivia", DuracionMinutos = 3, PuntosRecompensa = 40, RespuestaCorrecta = "450 años" },
                new ContenidoEducativo { Titulo = "Cuidemos el moana", Descripcion = "Conoce el impacto de los residuos en el océano que rodea Rapa Nui.", Tipo = "article", Categoria = "Territorio", DuracionMinutos = 5, PuntosRecompensa = 25 },
                new ContenidoEducativo { Titulo = "Reduce durante tu visita", Descripcion = "Consejos simples para generar menos residuos en la isla.", Tipo = "video", Categoria = "Turismo responsable", DuracionMinutos = 3, PuntosRecompensa = 20 },
                new ContenidoEducativo { Titulo = "Separación responsable", Descripcion = "Identifica el contenedor correcto para cada material.", Tipo = "quiz", Categoria = "Trivia", DuracionMinutos = 4, PuntosRecompensa = 40 });
        }
        var plasticQuiz = await db.ContenidosEducativos.FirstOrDefaultAsync(x => x.Tipo == "quiz");
        if (plasticQuiz is not null) plasticQuiz.RespuestaCorrecta ??= "450 años";
        if (!await db.ContenidosEducativos.AnyAsync(x => x.Titulo == "Reduce durante tu visita"))
            db.ContenidosEducativos.Add(new ContenidoEducativo { Titulo = "Reduce durante tu visita", Descripcion = "Consejos simples para generar menos residuos en la isla.", Tipo = "video", Categoria = "Turismo responsable", DuracionMinutos = 3, PuntosRecompensa = 20 });
        if (!await db.ContenidosEducativos.AnyAsync(x => x.Titulo == "Separación responsable"))
            db.ContenidosEducativos.Add(new ContenidoEducativo { Titulo = "Separación responsable", Descripcion = "Identifica el contenedor correcto para cada material.", Tipo = "quiz", Categoria = "Trivia", DuracionMinutos = 4, PuntosRecompensa = 40 });

        if (!await db.Insignias.AnyAsync())
        {
            db.Insignias.AddRange(
                new Insignia { Codigo = "primer_aporte", Nombre = "Primer aporte", Descripcion = "Validaste tu primer reciclaje.", Icono = "spark", Criterio = "recycling_count", Umbral = 1 },
                new Insignia { Codigo = "moana_10", Nombre = "Guardián del moana", Descripcion = "Reciclaste 10 kg validados.", Icono = "ocean", Criterio = "kg_total", Umbral = 10 },
                new Insignia { Codigo = "aprendiz_3", Nombre = "Aprendiz circular", Descripcion = "Completaste tres contenidos educativos.", Icono = "school", Criterio = "education_count", Umbral = 3 },
                new Insignia { Codigo = "racha_4", Nombre = "Constancia umanga", Descripcion = "Participaste durante cuatro semanas consecutivas.", Icono = "flame", Criterio = "streak_weeks", Umbral = 4 });
        }

        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(7).AddTicks(-1);
        if (!await db.Misiones.AnyAsync(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now))
        {
            db.Misiones.AddRange(
                new Mision { Titulo = "Semana sin plástico", Descripcion = "Valida dos entregas de plástico.", Tipo = "recycle_material", Material = "plastico", Objetivo = 2, RecompensaExperiencia = 100, RecompensaMonedas = 60, IniciaEn = weekStart, TerminaEn = weekEnd },
                new Mision { Titulo = "Cinco kilos por la isla", Descripcion = "Suma 5 kg de reciclaje validado esta semana.", Tipo = "recycle_kg", Objetivo = 5, RecompensaExperiencia = 140, RecompensaMonedas = 80, IniciaEn = weekStart, TerminaEn = weekEnd },
                new Mision { Titulo = "Aprende y comparte", Descripcion = "Completa dos contenidos educativos.", Tipo = "education", Objetivo = 2, RecompensaExperiencia = 80, RecompensaMonedas = 40, IniciaEn = weekStart, TerminaEn = weekEnd },
                new Mision { Titulo = "Ruta circular", Descripcion = "Registra una entrega en un punto limpio.", Tipo = "visit_point", Objetivo = 1, RecompensaExperiencia = 60, RecompensaMonedas = 30, IniciaEn = weekStart, TerminaEn = weekEnd });
        }

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        if (!await db.MetasComunitarias.AnyAsync(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now))
        {
            db.MetasComunitarias.Add(new MetaComunitaria
            {
                Titulo = "Umanga por una isla limpia",
                Descripcion = "Entre toda la comunidad, reciclemos 2.000 kg este mes.",
                ObjetivoKg = 2000,
                IniciaEn = monthStart,
                TerminaEn = monthEnd
            });
        }

        var activeGoal = await db.MetasComunitarias
            .FirstOrDefaultAsync(x => x.Activa && x.IniciaEn <= now && x.TerminaEn >= now);
        if (activeGoal is not null)
        {
            activeGoal.Descripcion = "Entre toda la comunidad, reciclemos 2.000 kg este mes.";
            activeGoal.ObjetivoKg = 2000;
        }

        await db.SaveChangesAsync();
    }
}

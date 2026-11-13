using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Reciclame.Api.Data;

/// <summary>
/// Evoluciona instalaciones creadas con EnsureCreated (version 1.x) sin borrar datos.
/// Todas las operaciones son idempotentes y compatibles con MySQL 8 en Easypanel.
/// </summary>
public static class DatabaseSchemaUpgrader
{
    public static async Task UpgradeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await AddColumnAsync(db, "Usuarios", "Experiencia", "INT NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnAsync(db, "Usuarios", "RachaSemanas", "INT NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnAsync(db, "Usuarios", "UltimaActividadEn", "DATETIME(6) NULL", cancellationToken);
        await AddColumnAsync(db, "Usuarios", "Sector", "VARCHAR(80) NULL", cancellationToken);

        await AddColumnAsync(db, "RegistrosReciclaje", "PuntoLimpioId", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL", cancellationToken);
        await AddColumnAsync(db, "RegistrosReciclaje", "ValidadoPorId", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL", cancellationToken);
        await AddColumnAsync(db, "RegistrosReciclaje", "ValidadoEn", "DATETIME(6) NULL", cancellationToken);
        await AddColumnAsync(db, "RegistrosReciclaje", "MotivoRechazo", "VARCHAR(300) NULL", cancellationToken);

        await AddColumnAsync(db, "Recompensas", "Socio", "VARCHAR(120) NULL", cancellationToken);
        await AddColumnAsync(db, "Recompensas", "Ubicacion", "VARCHAR(180) NULL", cancellationToken);
        await AddColumnAsync(db, "Recompensas", "Terminos", "VARCHAR(400) NULL", cancellationToken);
        await AddColumnAsync(db, "Recompensas", "ValidaHasta", "DATETIME(6) NULL", cancellationToken);
        await AddColumnAsync(db, "Canjes", "VenceEn", "DATETIME(6) NULL", cancellationToken);
        await AddColumnAsync(db, "Canjes", "UsadoEn", "DATETIME(6) NULL", cancellationToken);

        await AddColumnAsync(db, "PuntosLimpios", "EstadoOperativo", "VARCHAR(20) NOT NULL DEFAULT 'open'", cancellationToken);
        await AddColumnAsync(db, "PuntosLimpios", "CapacidadPorcentaje", "INT NULL", cancellationToken);
        await AddColumnAsync(db, "PuntosLimpios", "UltimaActualizacion", "DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)", cancellationToken);
        await AddColumnAsync(db, "PuntosLimpios", "CodigoVerificacionHash", "VARCHAR(500) NULL", cancellationToken);

        await AddColumnAsync(db, "ContenidosEducativos", "ContenidoUrl", "VARCHAR(500) NULL", cancellationToken);
        await AddColumnAsync(db, "ContenidosEducativos", "RespuestaCorrecta", "VARCHAR(200) NULL", cancellationToken);

        foreach (var statement in CreateTables)
            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE `Usuarios` SET `Experiencia` = GREATEST(`Puntos`, (`Nivel` - 1) * 500) " +
            "WHERE `Experiencia` = 0 AND (`Puntos` > 0 OR `Nivel` > 1)",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE `Usuarios` SET `Nivel` = GREATEST(1, FLOOR(`Experiencia` / 500) + 1)",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE `PuntosLimpios` SET `EstadoOperativo` = 'closed' " +
            "WHERE `Abierto` = 0 AND `EstadoOperativo` = 'open'",
            cancellationToken);
    }

    private static async Task AddColumnAsync(
        AppDbContext db,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(db, table, column, cancellationToken)) return;
        // table, column and definition only come from the hard-coded calls in
        // UpgradeAsync. SQL identifiers cannot be passed as DB parameters.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}",
            cancellationToken);
#pragma warning restore EF1002
    }

    private static async Task<bool> ColumnExistsAsync(
        AppDbContext db,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column
                """;
            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@table";
            tableParameter.Value = table;
            command.Parameters.Add(tableParameter);
            var columnParameter = command.CreateParameter();
            columnParameter.ParameterName = "@column";
            columnParameter.Value = column;
            command.Parameters.Add(columnParameter);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static readonly string[] CreateTables =
    [
        """
        CREATE TABLE IF NOT EXISTS `Misiones` (
          `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `Titulo` VARCHAR(120) NOT NULL, `Descripcion` VARCHAR(400) NOT NULL,
          `Tipo` VARCHAR(30) NOT NULL, `Material` VARCHAR(30) NULL,
          `Objetivo` DECIMAL(10,2) NOT NULL, `RecompensaExperiencia` INT NOT NULL,
          `RecompensaMonedas` INT NOT NULL, `IniciaEn` DATETIME(6) NOT NULL,
          `TerminaEn` DATETIME(6) NOT NULL, `Activa` TINYINT(1) NOT NULL DEFAULT 1,
          PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ProgresosMision` (
          `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `MisionId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `Progreso` DECIMAL(10,2) NOT NULL DEFAULT 0, `Completada` TINYINT(1) NOT NULL DEFAULT 0,
          `Reclamada` TINYINT(1) NOT NULL DEFAULT 0, `CompletadaEn` DATETIME(6) NULL,
          `ReclamadaEn` DATETIME(6) NULL, PRIMARY KEY (`UsuarioId`, `MisionId`),
          CONSTRAINT `FK_ProgresosMision_Usuarios` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE,
          CONSTRAINT `FK_ProgresosMision_Misiones` FOREIGN KEY (`MisionId`) REFERENCES `Misiones` (`Id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `Insignias` (
          `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `Codigo` VARCHAR(50) NOT NULL, `Nombre` VARCHAR(100) NOT NULL,
          `Descripcion` VARCHAR(300) NOT NULL, `Icono` VARCHAR(40) NOT NULL,
          `Criterio` VARCHAR(30) NOT NULL, `Umbral` DECIMAL(10,2) NOT NULL,
          PRIMARY KEY (`Id`), UNIQUE KEY `IX_Insignias_Codigo` (`Codigo`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `UsuariosInsignias` (
          `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `InsigniaId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `ObtenidaEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
          PRIMARY KEY (`UsuarioId`, `InsigniaId`),
          CONSTRAINT `FK_UsuariosInsignias_Usuarios` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE,
          CONSTRAINT `FK_UsuariosInsignias_Insignias` FOREIGN KEY (`InsigniaId`) REFERENCES `Insignias` (`Id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `ProgresosEducativos` (
          `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `ContenidoEducativoId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `ExperienciaGanada` INT NOT NULL, `MonedasGanadas` INT NOT NULL,
          `CompletadoEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
          PRIMARY KEY (`UsuarioId`, `ContenidoEducativoId`),
          CONSTRAINT `FK_ProgresosEducativos_Usuarios` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE,
          CONSTRAINT `FK_ProgresosEducativos_Contenidos` FOREIGN KEY (`ContenidoEducativoId`) REFERENCES `ContenidosEducativos` (`Id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS `MetasComunitarias` (
          `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
          `Titulo` VARCHAR(120) NOT NULL, `Descripcion` VARCHAR(300) NOT NULL,
          `ObjetivoKg` DECIMAL(10,2) NOT NULL, `IniciaEn` DATETIME(6) NOT NULL,
          `TerminaEn` DATETIME(6) NOT NULL, `Activa` TINYINT(1) NOT NULL DEFAULT 1,
          PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """
    ];
}

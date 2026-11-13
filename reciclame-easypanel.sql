-- Reciclame - esquema inicial para MySQL 8 / EasyPanel
-- Ejecutar con un usuario administrador de MySQL.
-- El script es idempotente: puede ejecutarse nuevamente sin duplicar los datos semilla.

CREATE DATABASE IF NOT EXISTS `reciclame`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `reciclame`;

CREATE TABLE IF NOT EXISTS `Usuarios` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nombre` VARCHAR(100) NOT NULL,
  `Correo` VARCHAR(180) NOT NULL,
  `HashContrasena` VARCHAR(500) NOT NULL,
  `Rol` VARCHAR(20) NOT NULL DEFAULT 'resident',
  `Sector` VARCHAR(80) NULL,
  `Puntos` INT NOT NULL DEFAULT 0,
  `Nivel` INT NOT NULL DEFAULT 1,
  `CreadoEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Usuarios_Correo` (`Correo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Recompensas` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nombre` VARCHAR(100) NOT NULL,
  `Descripcion` VARCHAR(300) NOT NULL,
  `Categoria` VARCHAR(40) NOT NULL,
  `Icono` VARCHAR(40) NOT NULL,
  `CostoPuntos` INT NOT NULL,
  `Stock` INT NOT NULL,
  `Activa` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `PuntosLimpios` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nombre` VARCHAR(100) NOT NULL,
  `Direccion` VARCHAR(180) NOT NULL,
  `Materiales` VARCHAR(200) NOT NULL,
  `Latitud` DOUBLE NOT NULL,
  `Longitud` DOUBLE NOT NULL,
  `Horario` VARCHAR(100) NOT NULL,
  `Abierto` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ContenidosEducativos` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Titulo` VARCHAR(120) NOT NULL,
  `Descripcion` VARCHAR(500) NOT NULL,
  `Tipo` VARCHAR(30) NOT NULL,
  `Categoria` VARCHAR(40) NOT NULL,
  `DuracionMinutos` INT NOT NULL,
  `PuntosRecompensa` INT NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `RegistrosReciclaje` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Material` VARCHAR(30) NOT NULL,
  `PesoKg` DECIMAL(8,2) NOT NULL,
  `PuntosGanados` INT NOT NULL,
  `Estado` VARCHAR(20) NOT NULL DEFAULT 'validated',
  `CreadoEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_RegistrosReciclaje_UsuarioId` (`UsuarioId`),
  CONSTRAINT `FK_RegistrosReciclaje_Usuarios_UsuarioId`
    FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Canjes` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `RecompensaId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PuntosGastados` INT NOT NULL,
  `Estado` VARCHAR(20) NOT NULL DEFAULT 'requested',
  `Codigo` VARCHAR(20) NOT NULL,
  `CreadoEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Canjes_Codigo` (`Codigo`),
  KEY `IX_Canjes_UsuarioId` (`UsuarioId`),
  KEY `IX_Canjes_RecompensaId` (`RecompensaId`),
  CONSTRAINT `FK_Canjes_Usuarios_UsuarioId`
    FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Canjes_Recompensas_RecompensaId`
    FOREIGN KEY (`RecompensaId`) REFERENCES `Recompensas` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `SolicitudesRetiro` (
  `Id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UsuarioId` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Direccion` VARCHAR(180) NOT NULL,
  `Materiales` VARCHAR(200) NOT NULL,
  `ProgramadoPara` DATETIME(6) NOT NULL,
  `Estado` VARCHAR(20) NOT NULL DEFAULT 'scheduled',
  `Notas` VARCHAR(300) NULL,
  `CreadoEn` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_SolicitudesRetiro_UsuarioId` (`UsuarioId`),
  CONSTRAINT `FK_SolicitudesRetiro_Usuarios_UsuarioId`
    FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Usuarios iniciales. Todos usan temporalmente la clave: Reciclame123!
INSERT IGNORE INTO `Usuarios`
  (`Id`, `Nombre`, `Correo`, `HashContrasena`, `Rol`, `Puntos`, `Nivel`, `CreadoEn`)
VALUES
  ('11111111-1111-4111-8111-111111111111', 'Moana Riroroko', 'residente@reciclame.cl',
   '100000.bLW5EkdgtSxY6HBlV8tyUg==.Bd4ecfjXEJ23zMh7wDj/LFQbwyKmfJbRW7qzWb0KNzg=',
   'resident', 1280, 4, UTC_TIMESTAMP(6)),
  ('22222222-2222-4222-8222-222222222222', 'Ana Visitante', 'turista@reciclame.cl',
   '100000.H57Z1rQSL2fyo5zhPcmCDw==.dsFoWYUvGKiQDyHo4x0Y6J/uV28PB5xewqxC2F52XzA=',
   'tourist', 320, 2, UTC_TIMESTAMP(6)),
  ('33333333-3333-4333-8333-333333333333', 'Equipo Municipal', 'municipal@reciclame.cl',
   '100000.TfoSzplaRjiCxJTaekOQ3w==.dCspP+BV8HKh1ZmwjKTO7b5bAPIxz8p0J19j34fmrUM=',
   'municipal', 0, 1, UTC_TIMESTAMP(6));

INSERT IGNORE INTO `RegistrosReciclaje`
  (`Id`, `UsuarioId`, `Material`, `PesoKg`, `PuntosGanados`, `Estado`, `CreadoEn`)
VALUES
  ('10000000-0000-4000-8000-000000000001', '11111111-1111-4111-8111-111111111111',
   'plastico', 2.40, 288, 'validated', UTC_TIMESTAMP(6) - INTERVAL 1 DAY),
  ('10000000-0000-4000-8000-000000000002', '11111111-1111-4111-8111-111111111111',
   'vidrio', 4.80, 384, 'validated', UTC_TIMESTAMP(6) - INTERVAL 5 DAY),
  ('10000000-0000-4000-8000-000000000003', '11111111-1111-4111-8111-111111111111',
   'papel', 1.70, 102, 'validated', UTC_TIMESTAMP(6) - INTERVAL 10 DAY);

INSERT IGNORE INTO `Recompensas`
  (`Id`, `Nombre`, `Descripcion`, `Categoria`, `Icono`, `CostoPuntos`, `Stock`, `Activa`)
VALUES
  ('20000000-0000-4000-8000-000000000001', 'Compost local',
   'Bolsa de compost producido en Orito.', 'Huerto', 'compost', 450, 18, 1),
  ('20000000-0000-4000-8000-000000000002', 'Almácigo nativo',
   'Planta nativa para recuperar jardines de la isla.', 'Naturaleza', 'plant', 700, 12, 1),
  ('20000000-0000-4000-8000-000000000003', 'Retiro de escombros',
   'Retiro municipal coordinado para tu domicilio.', 'Municipal', 'truck', 1200, 8, 1),
  ('20000000-0000-4000-8000-000000000004', 'Botella reutilizable',
   'Botella durable libre de plásticos de un solo uso.', 'Comunidad', 'bottle', 900, 25, 1);

INSERT IGNORE INTO `PuntosLimpios`
  (`Id`, `Nombre`, `Direccion`, `Materiales`, `Latitud`, `Longitud`, `Horario`, `Abierto`)
VALUES
  ('30000000-0000-4000-8000-000000000001', 'Centro de reciclaje Orito',
   'Camino a Orito', 'Vidrio, plástico, aluminio, papel y cartón', -27.1356, -109.4058,
   'Lun a Sáb · 08:30–17:30', 1),
  ('30000000-0000-4000-8000-000000000002', 'Punto limpio Hanga Roa',
   'Av. Atamu Tekena', 'Vidrio, plástico y latas', -27.1478, -109.4294,
   'Todos los días · 08:00–20:00', 1),
  ('30000000-0000-4000-8000-000000000003', 'Punto limpio Tahai',
   'Sector Tahai', 'Plástico, papel y cartón', -27.1397, -109.4393,
   'Lun a Vie · 09:00–18:00', 1);

INSERT IGNORE INTO `ContenidosEducativos`
  (`Id`, `Titulo`, `Descripcion`, `Tipo`, `Categoria`, `DuracionMinutos`, `PuntosRecompensa`)
VALUES
  ('40000000-0000-4000-8000-000000000001', 'Limpio, seco y separado',
   'Aprende a preparar tus residuos para que puedan ser valorizados.',
   'video', 'Reciclaje', 4, 20),
  ('40000000-0000-4000-8000-000000000002', 'Desafío del plástico',
   'Pon a prueba lo que sabes sobre plásticos de un solo uso.',
   'quiz', 'Trivia', 3, 40),
  ('40000000-0000-4000-8000-000000000003', 'Cuidemos el moana',
   'Conoce el impacto de los residuos en el océano que rodea Rapa Nui.',
   'article', 'Territorio', 5, 25);

-- Comprobación rápida del resultado.
SELECT 'Usuarios' AS `Tabla`, COUNT(*) AS `Filas` FROM `Usuarios`
UNION ALL SELECT 'RegistrosReciclaje', COUNT(*) FROM `RegistrosReciclaje`
UNION ALL SELECT 'Recompensas', COUNT(*) FROM `Recompensas`
UNION ALL SELECT 'Canjes', COUNT(*) FROM `Canjes`
UNION ALL SELECT 'SolicitudesRetiro', COUNT(*) FROM `SolicitudesRetiro`
UNION ALL SELECT 'PuntosLimpios', COUNT(*) FROM `PuntosLimpios`
UNION ALL SELECT 'ContenidosEducativos', COUNT(*) FROM `ContenidosEducativos`;

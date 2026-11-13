# Reciclame API

API REST de Reciclame para Rapa Nui, construida con ASP.NET Core 8, Entity
Framework Core, MySQL 8 y JWT. Incluye reciclaje con validación, eco-monedas y
experiencia separadas, misiones, insignias, meta comunitaria, educación,
recompensas, equipos por sector, puntos limpios y panel municipal.

## Despliegue en Easypanel

El `Dockerfile` publica `Reciclame.Api` y escucha en el puerto `8080`.

Configuración de la fuente:

- Repositorio: `mcaro77777/apiReciclaje`
- Rama: `main`
- Ruta de compilación: `/`
- Puerto de la aplicación: `8080`
- Comprobación de salud: `GET /health`

Variables mínimas del servicio:

```env
ConnectionStrings__MySql=Server=base-datos_reciclame-mysql;Port=3306;Database=reciclame;User=reciclame;Password=TU_PASSWORD;TreatTinyAsBoolean=true;SslMode=Preferred
Jwt__Key=UN_SECRETO_ALEATORIO_DE_AL_MENOS_64_CARACTERES
Jwt__Issuer=Reciclame.Api
Jwt__Audience=Reciclame.Flutter
Jwt__ExpirationMinutes=480
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Seed__DemoUsers=false
Mapbox__PublicToken=pk.TOKEN_PUBLICO_RESTRINGIDO
CleanPoints__DefaultVerificationCode=CODIGO_PRIVADO_DE_6_O_MAS_CARACTERES
RateLimiting__PermitLimit=120
Swagger__Enabled=false
DataProtection__KeysPath=/data/keys
```

`Mapbox__PublicToken` debe ser un token público (`pk.`) restringido a las APIs
necesarias. La app obtiene ese token mediante `GET /api/config/mapbox`; nunca se
debe exponer un token secreto (`sk.`). Para conservar las claves de protección
entre despliegues, monte un volumen persistente en `/data/keys`.

`Cors__AllowedOriginsCsv` solo es necesario si habrá un cliente web. Acepta una
lista separada por comas, por ejemplo:

```env
Cors__AllowedOriginsCsv=https://app.reciclame.cl,https://admin.reciclame.cl
```

La app móvil nativa no depende de CORS. MySQL debe utilizar el host interno de
Easypanel y su puerto `3306` no debe publicarse en Internet.

### Orden seguro de implementación

1. Cree un respaldo verificable de la base MySQL antes del primer despliegue de
   esta versión.
2. Configure los secretos y un volumen persistente para `/data/keys`.
3. Haga push a `main` e implemente el servicio desde Easypanel.
4. Espere a que `GET /health` responda `200` con `status: healthy`.
5. Compruebe `GET /api/clean-points`, `GET /api/education` y
   `GET /api/config/mapbox`.
6. Inicie sesión con una cuenta real, registre una entrega pequeña, valide el
   flujo municipal y confirme misiones/recompensas antes de publicar Android.

Si el contenedor no llega a estado saludable, conserve la base y vuelva a la
imagen anterior; no restaure ni borre tablas como primer intento.

## Base de datos y actualización 1.x → 1.1

`reciclame-easypanel.sql` permite crear una instalación inicial. En cada inicio,
la API ejecuta `EnsureCreated` y luego `DatabaseSchemaUpgrader`, que de forma
idempotente:

- agrega experiencia, racha y campos de validación;
- agrega datos operativos de puntos limpios y detalles de recompensas;
- crea las tablas de misiones, insignias, progreso educativo y meta comunitaria;
- conserva los registros existentes y calcula la experiencia inicial a partir
  de los puntos/nivel anteriores.

No es necesario ejecutar manualmente un script de migración para actualizar una
base 1.x, pero el respaldo previo sigue siendo obligatorio. El primer arranque
puede tardar unos segundos más mientras comprueba el esquema.

En producción mantenga siempre `Seed__DemoUsers=false`. Esta opción evita crear
nuevos usuarios de muestra, pero no elimina cuentas que ya existían. Si una
instalación antigua contiene las cuentas `residente@reciclame.cl`,
`turista@reciclame.cl` o `municipal@reciclame.cl` con la contraseña publicada de
demostración, elimínelas solo después de confirmar que son datos de prueba y de
crear el respaldo. No habilite el seed en un entorno público.

El código configurado en `CleanPoints__DefaultVerificationCode` se guarda con
hash y solo se aplica a puntos que todavía no tienen código. Entréguelo únicamente
al personal autorizado y rótelo desde la gestión municipal si se divulga.

## Endpoints principales

Públicos:

- `POST /api/auth/register` y `POST /api/auth/login`
- `GET /api/clean-points` y `GET /api/education`
- `GET /api/config/mapbox` y `GET /health`
- `GET /privacidad` y `GET /eliminar-cuenta`

Con JWT de usuario:

- `GET /api/users/me`, `GET /api/users/dashboard`, `DELETE /api/users/me`
- `GET|POST /api/recycling`
- `GET /api/gamification`
- `GET /api/community` y `PUT /api/community/sector`
- `POST /api/gamification/missions/{id}/claim`
- `POST /api/education/{id}/complete`
- `GET /api/rewards`, `GET /api/rewards/history`
- `POST /api/rewards/{id}/redeem`
- `GET|POST /api/pickups` y `POST /api/vananga`

Solo rol `municipal`:

- `GET /api/admin/overview`
- `GET /api/admin/recycling/pending`
- `POST /api/admin/recycling/{id}/validate`
- CRUD en `/api/admin/clean-points`
- `GET /api/admin/redemptions`
- `PATCH /api/admin/redemptions/{id}`

Los reciclajes sin código válido quedan `pending` y no suman saldo ni experiencia
hasta que un usuario municipal los aprueba. Los límites antiabuso son 50 kg por
día por usuario, bloqueo de duplicados recientes y limitación global de
solicitudes (120 por minuto de forma predeterminada).

## Google Play y eliminación de cuenta

- Política de privacidad: `/privacidad`
- Eliminación de cuenta y datos: `/eliminar-cuenta`

La eliminación dentro de la app usa `DELETE /api/users/me` con JWT. La página
pública autentica al usuario y utiliza el mismo endpoint.

## Desarrollo y verificación

```powershell
dotnet restore .\Reciclame.sln
dotnet build .\Reciclame.sln --no-restore
dotnet test .\Reciclame.sln --no-build
dotnet run --project .\Reciclame.Api
```

Swagger está disponible en desarrollo. En producción solo se expone si se cambia
deliberadamente `Swagger__Enabled=true`; manténgalo desactivado en el servicio
público.

Las contraseñas de MySQL, la clave JWT, el código de validación y cualquier token
se configuran como secretos de Easypanel y nunca se guardan en Git.

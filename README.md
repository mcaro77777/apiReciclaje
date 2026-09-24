# Reciclame API

API REST de Reciclame construida con ASP.NET Core 8, Entity Framework Core,
MySQL, JWT y Swagger.

## Despliegue en EasyPanel

La aplicacion se construye con el `Dockerfile` de la raiz del repositorio y
escucha en el puerto `8080`.

Variables del servicio:

```env
ConnectionStrings__MySql=Server=base-datos_reciclame-mysql;Port=3306;Database=reciclame;User=reciclame;Password=TU_PASSWORD;TreatTinyAsBoolean=true;SslMode=Preferred
Jwt__Key=UN_SECRETO_ALEATORIO_DE_AL_MENOS_64_CARACTERES
Jwt__Issuer=Reciclame.Api
Jwt__Audience=Reciclame.Flutter
Jwt__ExpirationMinutes=480
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

Configuracion de la fuente en EasyPanel:

- Repositorio: `mcaro77777/apiReciclaje`
- Rama: `main`
- Ruta de compilacion: `/`
- Puerto de la aplicacion: `8080`
- Comprobacion de salud: `/health`

Paginas publicas requeridas por Google Play:

- Politica de privacidad: `/privacidad`
- Eliminacion de cuenta y datos: `/eliminar-cuenta`

La eliminacion dentro de la app utiliza `DELETE /api/users/me` con un token
JWT valido. La pagina publica autentica al usuario y utiliza el mismo endpoint.

La conexion utiliza el host interno de MySQL; no necesita exponer el puerto
3306 a Internet.

## Base de datos

El archivo `reciclame-easypanel.sql` crea las tablas y los datos iniciales en
espanol. La API comprueba el esquema al iniciar mediante Entity Framework Core.

## Desarrollo local

```powershell
dotnet run --project .\Reciclame.Api
```

Swagger queda disponible en `http://localhost:5080/swagger` al usar el perfil
HTTP. En produccion se publica bajo `/swagger` en el dominio configurado.

Usuarios de demostracion, con clave inicial `Reciclame123!`:

- `residente@reciclame.cl`
- `turista@reciclame.cl`
- `municipal@reciclame.cl`

Las contrasenas de MySQL y JWT deben configurarse solo como secretos de
EasyPanel y nunca guardarse en Git.

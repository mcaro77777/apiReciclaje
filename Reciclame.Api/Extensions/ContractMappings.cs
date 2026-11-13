using Reciclame.Api.Contracts;
using Reciclame.Api.Models;

namespace Reciclame.Api.Extensions;

public static class ContractMappings
{
    public static RespuestaUsuario ToResponse(this Usuario user) => new(
        user.Id,
        user.Nombre,
        user.Correo,
        user.Rol,
        user.Puntos,
        user.Nivel,
        user.Experiencia,
        user.Puntos,
        user.RachaSemanas,
        user.Sector);
}

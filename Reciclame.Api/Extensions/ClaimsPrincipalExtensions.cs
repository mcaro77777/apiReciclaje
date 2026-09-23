using System.Security.Claims;

namespace Reciclame.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid UsuarioId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Reciclame.Api.Models;

namespace Reciclame.Api.Services;

public sealed class TokenService(IConfiguration configuration)
{
    public string Create(Usuario usuario)
    {
        var key = configuration["Jwt:Key"]!;
        var expiration = configuration.GetValue("Jwt:ExpirationMinutes", 480);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(expiration), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

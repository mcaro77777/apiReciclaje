using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Contracts;
using Reciclame.Api.Data;
using Reciclame.Api.Models;
using Reciclame.Api.Services;

namespace Reciclame.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, PasswordService passwords, TokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RespuestaAutenticacion>> Register(SolicitudRegistro request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(x => x.Correo == correo))
            return Conflict(new ProblemDetails { Title = "El correo ya esta registrado." });
        if (request.Rol is not (Roles.Residente or Roles.Turista))
            return BadRequest(new ProblemDetails { Title = "El perfil seleccionado no es valido." });

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(), Correo = correo,
            HashContrasena = passwords.Hash(request.Contrasena), Rol = request.Rol
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return Created("api/users/me", CrearRespuesta(usuario));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<RespuestaAutenticacion>> Login(SolicitudInicioSesion request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Correo == correo);
        if (usuario is null || !passwords.Verify(request.Contrasena, usuario.HashContrasena))
            return Unauthorized(new ProblemDetails { Title = "Correo o contrasena incorrectos." });
        return Ok(CrearRespuesta(usuario));
    }

    private RespuestaAutenticacion CrearRespuesta(Usuario usuario) => new(tokens.Create(usuario),
        new RespuestaUsuario(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol, usuario.Puntos, usuario.Nivel));
}

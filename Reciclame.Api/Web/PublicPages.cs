namespace Reciclame.Api.Web;

public static class PublicPages
{
    public const string AccountDeletion = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Eliminar cuenta - Reciclame</title>
  <style>
    body{font-family:system-ui,sans-serif;background:#f5f4fa;color:#27233f;margin:0;padding:24px}
    main{max-width:620px;margin:40px auto;background:#fff;padding:32px;border-radius:16px;box-shadow:0 8px 30px #27233f18}
    h1{margin-top:0} label{display:block;font-weight:650;margin:18px 0 6px}
    input{box-sizing:border-box;width:100%;padding:12px;border:1px solid #aaa;border-radius:8px;font-size:16px}
    button{margin-top:22px;background:#b3261e;color:#fff;border:0;border-radius:8px;padding:13px 18px;font-weight:700;cursor:pointer}
    .notice{background:#f2efff;padding:16px;border-radius:10px;line-height:1.5} #result{margin-top:18px;font-weight:650}
  </style>
</head>
<body><main>
  <h1>Eliminar cuenta de Reciclame</h1>
  <p>Este formulario permite eliminar de forma inmediata tu cuenta y los datos personales asociados.</p>
  <div class="notice"><strong>Se eliminarán:</strong> perfil, nombre, correo, eco-monedas, experiencia, nivel, registros de reciclaje, progreso de misiones y contenidos, insignias, solicitudes de retiro, notas y canjes. No conservamos una copia de estos datos después de la eliminación.</div>
  <form id="delete-form">
    <label for="email">Correo de la cuenta</label>
    <input id="email" type="email" autocomplete="username" required>
    <label for="password">Contraseña</label>
    <input id="password" type="password" autocomplete="current-password" required minlength="8">
    <button type="submit">Eliminar mi cuenta y datos</button>
  </form>
  <p id="result" role="status"></p>
  <p><a href="/privacidad">Consultar la política de privacidad</a></p>
  <script>
    document.getElementById('delete-form').addEventListener('submit', async (event) => {
      event.preventDefault();
      const result = document.getElementById('result');
      const button = event.target.querySelector('button');
      const correo = document.getElementById('email').value;
      const contrasena = document.getElementById('password').value;
      button.disabled = true;
      result.textContent = 'Verificando la cuenta...';
      try {
        const login = await fetch('/api/auth/login', {
          method: 'POST', headers: {'Content-Type':'application/json'},
          body: JSON.stringify({correo, contrasena})
        });
        if (!login.ok) throw new Error('El correo o la contraseña no son correctos.');
        const auth = await login.json();
        const deletion = await fetch('/api/users/me', {
          method: 'DELETE', headers: {'Authorization': `Bearer ${auth.token}`}
        });
        if (!deletion.ok) throw new Error('No fue posible eliminar la cuenta. Intenta nuevamente.');
        event.target.remove();
        result.textContent = 'Tu cuenta y los datos asociados fueron eliminados correctamente.';
      } catch (error) {
        result.textContent = error.message;
        button.disabled = false;
      }
    });
  </script>
</main></body></html>
""";

    public const string PrivacyPolicy = """
<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Política de privacidad - Reciclame</title>
<style>body{font-family:system-ui,sans-serif;color:#27233f;line-height:1.6;margin:0;padding:24px}main{max-width:760px;margin:auto}h1,h2{line-height:1.2}</style></head>
<body><main><h1>Política de privacidad de Reciclame</h1><p>Última actualización: 28 de octubre de 2026.</p>
<h2>Datos recopilados</h2><p>Recopilamos nombre, correo electrónico, identificador de cuenta, tipo de perfil, eco-monedas, experiencia, nivel, racha semanal, registros de reciclaje, progreso de misiones y contenidos, insignias y canjes. Cuando solicitas un retiro también recopilamos dirección, materiales, fecha y notas.</p>
<h2>Ubicación y mapas</h2><p>La ubicación del dispositivo se solicita únicamente cuando pulsas “Usar mi ubicación”, para mostrar y ordenar centros de acopio cercanos. Reciclame no envía ni almacena esa ubicación en su API. El mapa utiliza Mapbox, que recibe las solicitudes técnicas necesarias para entregar el servicio conforme a su propia <a href="https://www.mapbox.com/legal/privacy" rel="noopener noreferrer">política de privacidad</a>.</p>
<h2>Finalidad</h2><p>Los datos se utilizan para crear y administrar la cuenta, validar actividades de reciclaje, calcular impacto ambiental, asignar experiencia, eco-monedas, misiones e insignias, gestionar recompensas y coordinar retiros.</p>
<h2>Protección y terceros</h2><p>La información se transmite mediante HTTPS y se almacena en la infraestructura contratada por Reciclame. No vendemos datos ni los utilizamos para publicidad personalizada. Solo intervienen proveedores técnicos necesarios para operar la API, la base de datos y el mapa.</p>
<h2>Conservación y eliminación</h2><p>Los datos se conservan mientras la cuenta permanezca activa. El usuario puede eliminar inmediatamente su cuenta y datos asociados mediante la página de eliminación.</p>
<p><a href="/eliminar-cuenta">Solicitar la eliminación de cuenta y datos</a></p>
</main></body></html>
""";
}

Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Http
Imports Registro.Datos
Imports Registro.Entidades
Imports Registro.Negocio

<RoutePrefix("api/setup")>
Public Class SetupController
    Inherits ApiController

    <HttpPost>
    <Route("admin")>
    Public Function CrearAdmin(<FromBody> solicitud As SetupAdminRequest) As IHttpActionResult
        If HttpContext.Current Is Nothing OrElse Not HttpContext.Current.Request.IsLocal Then Return StatusCode(HttpStatusCode.Forbidden)
        If solicitud Is Nothing OrElse solicitud.Persona Is Nothing OrElse Not PasswordHasher.EsValida(solicitud.Password) Then Return BadRequest("Datos de administrador no válidos.")
        If String.IsNullOrWhiteSpace(solicitud.Persona.Nombre) OrElse String.IsNullOrWhiteSpace(solicitud.Persona.Correo) Then Return BadRequest("Nombre y correo son obligatorios.")
        Dim dao As New PersonaDAO()
        If dao.ContarAdministradoresActivos() > 0 Then Return Content(HttpStatusCode.Conflict, New With {.mensaje = "Ya existe un administrador activo."})
        Dim idRol = dao.ObtenerIdRol("Administrador")
        If Not idRol.HasValue Then Return InternalServerError(New InvalidOperationException("No existe el rol Administrador."))
        Dim hash As Byte() = Nothing
        Dim salt As Byte() = Nothing
        PasswordHasher.CrearHash(solicitud.Password, hash, salt)
        solicitud.Persona.IdRol = idRol.Value
        solicitud.Persona.PasswordHash = hash
        solicitud.Persona.PasswordSalt = salt
        solicitud.Persona.Correo = solicitud.Persona.Correo.Trim().ToLowerInvariant()
        If Not Regex.IsMatch(solicitud.Persona.Correo, "^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant) Then Return BadRequest("El correo no tiene un formato válido.")
        If dao.ExisteCorreo(solicitud.Persona.Correo) Then Return Content(HttpStatusCode.Conflict, New With {.mensaje = "El correo ya está registrado."})
        solicitud.Persona.Activo = True
        solicitud.Persona.Estatus = "Activo"
        solicitud.Persona.FechaRegistro = DateTime.Now
        solicitud.Persona.AceptoPrivacidad = True
        solicitud.Persona.FechaAceptacion = DateTime.Now
        Return Ok(New With {.id = dao.InsertarAdministrador(solicitud.Persona), .mensaje = "Administrador creado."})
    End Function
End Class

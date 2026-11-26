Imports System
Imports System.Net
Imports System.Security.Claims
Imports System.Web.Http
Imports Registro.Datos
Imports Registro.Entidades
Imports Registro.Negocio

<RoutePrefix("api/auth")>
Public Class AuthController
    Inherits ApiController

    Private ReadOnly _personaDAO As New PersonaDAO()
    Private ReadOnly _personaBO As New PersonaBO()
    Private ReadOnly _tokenService As New JwtTokenService()

    <HttpGet>
    <Route("tipos-cuenta")>
    Public Function TiposCuenta() As IHttpActionResult
        Return Ok(New Object() {New With {.nombre = "Cliente"}, New With {.nombre = "Empleado"}})
    End Function

    <HttpPost>
    <Route("registro")>
    Public Function Registro(<FromBody> solicitud As RegistroRequest) As IHttpActionResult
        If solicitud Is Nothing OrElse solicitud.Persona Is Nothing Then Return BadRequest("Los datos de registro son obligatorios.")
        Dim resultado = _personaBO.RegistrarPublico(New RegistroAutenticacion With {
            .Persona = solicitud.Persona,
            .Password = solicitud.Password,
            .Descriptores = If(solicitud.Descriptores, New List(Of Single())())
        })
        If Not resultado.Exitoso Then Return Content(HttpStatusCode.BadRequest, New With {.errores = resultado.Errores})
        Return Content(HttpStatusCode.Created, New With {.mensaje = "Registro enviado, pendiente de aprobación", .id = resultado.IdGenerado})
    End Function

    <HttpPost>
    <Route("login")>
    Public Function Login(<FromBody> solicitud As LoginRequest) As IHttpActionResult
        If solicitud Is Nothing OrElse String.IsNullOrWhiteSpace(solicitud.Correo) OrElse String.IsNullOrEmpty(solicitud.Password) Then
            Return Content(HttpStatusCode.Unauthorized, New With {.mensaje = "Credenciales inválidas"})
        End If
        Dim correo = solicitud.Correo.Trim().ToLowerInvariant()
        Dim persona = _personaDAO.ObtenerParaAutenticacion(correo)
        If persona Is Nothing Then Return Content(HttpStatusCode.Unauthorized, New With {.mensaje = "Credenciales inválidas"})
        If persona.BloqueadoHasta.HasValue AndAlso persona.BloqueadoHasta.Value > DateTime.Now Then Return Content(HttpStatusCode.Unauthorized, New With {.mensaje = "Credenciales inválidas"})
        If Not PasswordHasher.Verificar(solicitud.Password, persona.PasswordHash, persona.PasswordSalt) Then
            Dim intentos = persona.IntentosFallidos + 1
            Dim bloqueo As Nullable(Of DateTime) = Nothing
            If intentos >= 5 Then bloqueo = DateTime.Now.AddMinutes(15)
            _personaDAO.ActualizarIntentoLogin(correo, If(intentos >= 5, 0, intentos), bloqueo)
            Return Content(HttpStatusCode.Unauthorized, New With {.mensaje = "Credenciales inválidas"})
        End If
        If Not String.Equals(persona.Estatus, "Activo", StringComparison.OrdinalIgnoreCase) Then Return Content(HttpStatusCode.Forbidden, New With {.mensaje = "La cuenta está " & persona.Estatus.ToLowerInvariant()})
        Dim rol = _personaDAO.ObtenerNombreRol(persona.IdRol)
        _personaDAO.RegistrarAcceso(persona.IdPersona)
        Dim token = _tokenService.Crear(persona, rol)
        Return Ok(New With {.token = token.Item1, .expiraEn = token.Item2, .usuario = New With {.id = persona.IdPersona, .nombre = persona.Nombre, .rol = rol, .estatus = persona.Estatus}})
    End Function

    <Authorize>
    <HttpGet>
    <Route("yo")>
    Public Function Yo() As IHttpActionResult
        Dim identidad = TryCast(User.Identity, ClaimsIdentity)
        Dim claim = If(identidad Is Nothing, Nothing, identidad.FindFirst(ClaimTypes.NameIdentifier))
        If claim Is Nothing Then Return Unauthorized()
        Dim persona = _personaDAO.ObtenerPorId(Convert.ToInt32(claim.Value))
        If persona Is Nothing Then Return NotFound()
        Return Ok(New With {.id = persona.IdPersona, .nombre = persona.Nombre, .correo = persona.Correo, .idRol = persona.IdRol, .activo = persona.Activo, .estatus = persona.Estatus, .foto = persona.Foto})
    End Function
End Class

Imports System
Imports System.Net
Imports System.Security.Claims
Imports System.Web.Http
Imports Registro.Datos

<RoutePrefix("api/admin")>
<Authorize(Roles:="Administrador")>
Public Class AdminController
    Inherits ApiController

    Private ReadOnly _dao As New PersonaDAO()

    <HttpGet>
    <Route("resumen")>
    Public Function Resumen() As IHttpActionResult
        Return Ok(_dao.ObtenerResumenAdmin())
    End Function

    <HttpGet>
    <Route("usuarios/{id:int}")>
    Public Function ObtenerDetalle(id As Integer) As IHttpActionResult
        Dim resultadoDetalle As Registro.Entidades.UsuarioDetalle = _dao.ObtenerDetalleAdmin(id)
        If resultadoDetalle Is Nothing Then Return NotFound()
        Return Ok(resultadoDetalle)
    End Function

    <HttpGet>
    <Route("usuarios")>
    Public Function Usuarios(Optional rol As String = Nothing, Optional estatus As String = Nothing, Optional buscar As String = Nothing, Optional pagina As Integer = 1, Optional tamano As Integer = 20) As IHttpActionResult
        pagina = Math.Max(1, pagina)
        tamano = Math.Min(100, Math.Max(1, tamano))
        Dim total As Integer = 0
        Return Ok(New With {.datos = _dao.ObtenerUsuariosAdmin(rol, estatus, buscar, pagina, tamano, total), .pagina = pagina, .tamano = tamano, .total = total})
    End Function

    <HttpPut>
    <Route("usuarios/{id:int}/estatus")>
    Public Function CambiarEstatus(id As Integer, <FromBody> solicitud As EstatusRequest) As IHttpActionResult
        If solicitud Is Nothing OrElse Not New String() {"Activo", "Rechazado", "Inactivo"}.Contains(solicitud.Estatus) Then Return BadRequest("El estatus no es válido.")
        Dim identidad = TryCast(User.Identity, ClaimsIdentity)
        Dim claim = If(identidad Is Nothing, Nothing, identidad.FindFirst(ClaimTypes.NameIdentifier))
        If claim Is Nothing Then Return Unauthorized()
        Dim idAdministrador = Convert.ToInt32(claim.Value)
        If id = idAdministrador AndAlso solicitud.Estatus = "Inactivo" Then Return BadRequest("Un administrador no puede desactivarse a sí mismo.")
        If solicitud.Estatus = "Inactivo" AndAlso _dao.ContarAdministradoresActivos() <= 1 Then Return BadRequest("No se puede desactivar al último administrador activo.")
        Try
            _dao.ActualizarEstatus(id, solicitud.Estatus, idAdministrador)
            Return Ok(New With {.mensaje = "Estatus actualizado."})
        Catch ex As Exception
            Return InternalServerError(ex)
        End Try
    End Function
End Class

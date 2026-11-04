Imports System.Net
Imports System.Web.Http
Imports Registro.Entidades
Imports Registro.Negocio

<RoutePrefix("api/personas")>
Public Class PersonasController
    Inherits ApiController

    Private ReadOnly _negocio As New PersonaBO()

    <HttpGet>
    <Route("")>
    Public Function Listar(Optional rol As Nullable(Of Integer) = Nothing,
                           Optional buscar As String = Nothing) As IHttpActionResult
        Return Ok(_negocio.Listar(rol, buscar))
    End Function

    <HttpGet>
    <Route("{id:int}")>
    Public Function ObtenerPorId(id As Integer) As IHttpActionResult
        Dim persona = _negocio.ObtenerPorId(id)
        If persona Is Nothing Then
            Return NotFound()
        End If

        Return Ok(persona)
    End Function

    <HttpPost>
    <Route("")>
    Public Function Registrar(<FromBody> persona As Persona) As IHttpActionResult
        Dim resultado = _negocio.Registrar(persona)
        If resultado.Exitoso Then
            Return Ok(resultado)
        End If

        Return Content(HttpStatusCode.BadRequest, resultado)
    End Function

    <HttpPut>
    <Route("{id:int}")>
    Public Function Actualizar(id As Integer, <FromBody> persona As Persona) As IHttpActionResult
        Dim resultado = _negocio.Actualizar(id, persona)
        If resultado.Exitoso Then
            Return Ok(resultado)
        End If

        Return Content(HttpStatusCode.BadRequest, resultado)
    End Function

    <HttpDelete>
    <Route("{id:int}")>
    Public Function Eliminar(id As Integer) As IHttpActionResult
        Dim resultado = _negocio.Eliminar(id)
        If resultado.Exitoso Then
            Return Ok(resultado)
        End If

        Return Content(HttpStatusCode.BadRequest, resultado)
    End Function
End Class

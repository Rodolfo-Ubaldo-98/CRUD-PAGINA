Imports System.Net
Imports System.Web.Http
Imports Registro.Negocio

<RoutePrefix("api")>
Public Class RostrosController
    Inherits ApiController

    Private ReadOnly _rostroBO As New RostroBO()

    <HttpPost>
    <Route("personas/{id:int}/rostro")>
    Public Function RegistrarMuestra(id As Integer, solicitud As RostroRequest) As IHttpActionResult
        If solicitud Is Nothing Then
            Return BadRequest("El cuerpo de la solicitud es obligatorio.")
        End If

        Dim resultado = _rostroBO.RegistrarMuestra(id, solicitud.Descriptor)
        If Not resultado.Exitoso Then
            Return Content(HttpStatusCode.BadRequest, New With {.errores = resultado.Errores})
        End If

        Return Created(String.Empty, New With {.idRostro = resultado.IdGenerado})
    End Function

    <HttpPost>
    <Route("rostro/identificar")>
    Public Function Identificar(solicitud As RostroRequest) As IHttpActionResult
        If solicitud Is Nothing Then
            Return BadRequest("El cuerpo de la solicitud es obligatorio.")
        End If

        Dim resultado = _rostroBO.Identificar(solicitud.Descriptor)
        If Not resultado.Exitoso Then
            Return Content(HttpStatusCode.BadRequest, New With {.errores = resultado.Errores})
        End If
        If Not resultado.Reconocido Then
            Return NotFound()
        End If

        Return Ok(New With {
            .idPersona = resultado.IdPersona.Value,
            .nombre = resultado.Nombre,
            .distancia = resultado.Distancia.Value
        })
    End Function
End Class

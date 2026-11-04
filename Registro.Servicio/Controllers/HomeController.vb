Imports Mvc = System.Web.Mvc
Imports WebApi = System.Web.Http
Imports Registro.Datos

Public Class HomeController
    Inherits Mvc.Controller

    Public Function Index() As Mvc.ActionResult
        Return View()
    End Function
End Class

<WebApi.RoutePrefix("api")>
Public Class CatalogosController
    Inherits WebApi.ApiController

    Private ReadOnly _catalogo As New CatalogoDAO()

    <WebApi.HttpGet>
    <WebApi.Route("roles")>
    Public Function ObtenerRoles() As WebApi.IHttpActionResult
        Return Ok(_catalogo.ObtenerRoles())
    End Function

    <WebApi.HttpGet>
    <WebApi.Route("estados")>
    Public Function ObtenerEstados() As WebApi.IHttpActionResult
        Return Ok(_catalogo.ObtenerEstados())
    End Function

    <WebApi.HttpGet>
    <WebApi.Route("estados/{id:int}/municipios")>
    Public Function ObtenerMunicipios(id As Integer) As WebApi.IHttpActionResult
        Return Ok(_catalogo.ObtenerMunicipiosPorEstado(id))
    End Function

    <WebApi.HttpGet>
    <WebApi.Route("municipios/{id:int}/localidades")>
    Public Function ObtenerLocalidades(id As Integer) As WebApi.IHttpActionResult
        Return Ok(_catalogo.ObtenerLocalidadesPorMunicipio(id))
    End Function
End Class

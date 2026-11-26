Imports System
Imports System.Configuration
Imports System.IdentityModel.Tokens.Jwt
Imports System.Net.Http
Imports System.Security.Claims
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web
Imports Microsoft.IdentityModel.Tokens

''' <summary>Valida tokens Bearer sin depender de OWIN.</summary>
Public Class JwtAuthenticationHandler
    Inherits DelegatingHandler

    Protected Overrides Async Function SendAsync(request As HttpRequestMessage, cancellationToken As CancellationToken) As Task(Of HttpResponseMessage)
        Dim encabezado = request.Headers.Authorization
        If encabezado IsNot Nothing AndAlso String.Equals(encabezado.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) Then
            Dim identidad = Validar(encabezado.Parameter)
            If identidad IsNot Nothing Then
                Thread.CurrentPrincipal = New ClaimsPrincipal(identidad)
                If HttpContext.Current IsNot Nothing Then HttpContext.Current.User = Thread.CurrentPrincipal
            End If
        End If
        Return Await MyBase.SendAsync(request, cancellationToken)
    End Function

    Private Shared Function Validar(token As String) As ClaimsIdentity
        Try
            Dim clave = ConfigurationManager.AppSettings("JwtSecret")
            Dim emisor = ConfigurationManager.AppSettings("JwtIssuer")
            Dim parametros As New TokenValidationParameters With {
                .ValidateIssuerSigningKey = True,
                .IssuerSigningKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                .ValidateIssuer = True,
                .ValidIssuer = emisor,
                .ValidateAudience = True,
                .ValidAudience = emisor,
                .ValidateLifetime = True,
                .ClockSkew = TimeSpan.FromMinutes(1)
            }
            Dim principal = New JwtSecurityTokenHandler().ValidateToken(token, parametros, Nothing)
            Return TryCast(principal.Identity, ClaimsIdentity)
        Catch
            Return Nothing
        End Try
    End Function
End Class

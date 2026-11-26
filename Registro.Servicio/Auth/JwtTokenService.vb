Imports System
Imports System.Configuration
Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports Microsoft.IdentityModel.Tokens
Imports Registro.Entidades

''' <summary>Emite tokens JWT firmados con HS256 usando configuración externa.</summary>
Public Class JwtTokenService
    Public Function Crear(persona As Persona, nombreRol As String) As Tuple(Of String, DateTime)
        Dim clave = ConfigurationManager.AppSettings("JwtSecret")
        Dim emisor = ConfigurationManager.AppSettings("JwtIssuer")
        Dim horas As Integer = Convert.ToInt32(ConfigurationManager.AppSettings("JwtExpirationHours"))
        If String.IsNullOrWhiteSpace(clave) OrElse clave.Length < 32 Then Throw New ConfigurationErrorsException("JwtSecret debe tener al menos 32 caracteres.")

        Dim expiracion = DateTime.UtcNow.AddHours(horas)
        Dim credenciales = New SigningCredentials(New SymmetricSecurityKey(Text.Encoding.UTF8.GetBytes(clave)), SecurityAlgorithms.HmacSha256)
        Dim claims = New List(Of Claim) From {
            New Claim(ClaimTypes.NameIdentifier, persona.IdPersona.ToString()),
            New Claim(ClaimTypes.Name, persona.Nombre),
            New Claim(ClaimTypes.Email, persona.Correo),
            New Claim(ClaimTypes.Role, nombreRol),
            New Claim("estatus", persona.Estatus)
        }
        Dim token = New JwtSecurityToken(emisor, emisor, claims, DateTime.UtcNow, expiracion, credenciales)
        Return Tuple.Create(New JwtSecurityTokenHandler().WriteToken(token), expiracion)
    End Function
End Class

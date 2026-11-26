Imports Registro.Entidades

Public Class RegistroRequest
    Public Property Persona As Persona
    Public Property Password As String
    Public Property Descriptores As List(Of Single())
End Class

Public Class LoginRequest
    Public Property Correo As String
    Public Property Password As String
End Class

Public Class SetupAdminRequest
    Public Property Persona As Persona
    Public Property Password As String
End Class
